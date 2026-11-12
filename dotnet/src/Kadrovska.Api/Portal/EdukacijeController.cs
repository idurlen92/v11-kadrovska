using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Kadrovska.Api.Autentikacija;
using Kadrovska.Api.Infrastruktura;
using Microsoft.AspNetCore.Mvc;

namespace Kadrovska.Api.Portal;

/// <summary>Interne edukacije: popis, prijava na edukaciju i uvoz novih edukacija iz XML-a.</summary>
[ApiController]
[Route("api/edukacije")]
[PotrebnaPrijava]
public sealed class EdukacijeController(Baza baza, SustavZaUcenjeKlijent sustavZaUcenje, ILogger<EdukacijeController> logger) : ControllerBase
{
    private const int NajvecaVelicinaXml = 100_000;
    private const string FormatDatuma = "yyyy-MM-dd";

    private sealed record NovaEdukacija(string Naziv, string Datum, int Kapacitet);

    [HttpGet]
    public IActionResult Popis()
    {
        var redci = baza.Upit("SELECT id, naziv, datum, kapacitet, prijavljeno FROM edukacije ORDER BY datum, id");
        return Ok(redci.Select(r => new
        {
            id = r.Broj("id"),
            naziv = r.Tekst("naziv"),
            datum = r.Tekst("datum"),
            kapacitet = r.Broj("kapacitet"),
            prijavljeno = r.Broj("prijavljeno"),
            slobodno = Math.Max(0, r.Broj("kapacitet") - r.Broj("prijavljeno")),
        }));
    }

    [HttpPost("{id:long}/prijava")]
    public async Task<IActionResult> Prijava(long id)
    {
        var korisnik = HttpContext.Korisnik();
        var kreirano = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        const string unosPrijave = "INSERT INTO prijave_edukacije (edukacija_id, zaposlenik, kreirano) VALUES ($e, $z, $k)";

        using var veza = baza.Otvori();
        if (Baza.Upit(veza, null, "SELECT id FROM prijave_edukacije WHERE edukacija_id = $e AND zaposlenik = $z",
                ("$e", id), ("$z", korisnik.Username)).Count > 0)
        {
            return Conflict(new { greska = "Već ste prijavljeni na ovu edukaciju." });
        }

        var edukacija = Baza.Upit(veza, null,
            "SELECT kapacitet, prijavljeno FROM edukacije WHERE id = $id", ("$id", id)).FirstOrDefault();
        if (edukacija is null)
        {
            return NotFound(new { greska = "Edukacija ne postoji." });
        }
        if (edukacija.Broj("prijavljeno") >= edukacija.Broj("kapacitet"))
        {
            return Conflict(new { greska = "Nema slobodnih mjesta." });
        }

        await sustavZaUcenje.UpisiPolaznikaAsync(id, korisnik.Username, HttpContext.RequestAborted);

        Baza.Izvrsi(veza, null, "UPDATE edukacije SET prijavljeno = prijavljeno + 1 WHERE id = $id", ("$id", id));
        var prijavaId = Baza.Umetni(veza, null, unosPrijave, ("$e", id), ("$z", korisnik.Username), ("$k", kreirano));

        logger.LogInformation("Zaposlenik {Username} prijavljen je na edukaciju {Id}", korisnik.Username, id);
        return StatusCode(StatusCodes.Status201Created, new { id = prijavaId, edukacijaId = id, kreirano });
    }

    /// <summary>
    /// Uvoz edukacija: <c>&lt;edukacije&gt;&lt;edukacija&gt;&lt;naziv&gt;…&lt;/naziv&gt;&lt;datum&gt;2026-12-03&lt;/datum&gt;
    /// &lt;kapacitet&gt;15&lt;/kapacitet&gt;&lt;/edukacija&gt;&lt;/edukacije&gt;</c>
    /// </summary>
    [HttpPost("uvoz")]
    [PotrebnaPrijava(Aplikacija.PovlastenaUloga, Uloge.Admin)]
    public async Task<IActionResult> Uvoz()
    {
        string xml;
        using (var citac = new StreamReader(Request.Body, Encoding.UTF8))
        {
            xml = await citac.ReadToEndAsync(HttpContext.RequestAborted);
        }
        if (xml.Length > NajvecaVelicinaXml)
        {
            return BadRequest(new { greska = "Datoteka je prevelika." });
        }

        List<NovaEdukacija> edukacije;
        try
        {
            edukacije = ProcitajXml(xml);
        }
        catch (Exception ex) when (ex is XmlException or FormatException or OverflowException)
        {
            logger.LogInformation("Uvoz edukacija nije uspio: {Poruka}", ex.Message);
            return BadRequest(new { greska = "Neispravan XML." });
        }

        var uvezene = new List<object>();
        using (var veza = baza.Otvori())
        using (var transakcija = veza.BeginTransaction())
        {
            foreach (var edukacija in edukacije)
            {
                var id = Baza.Umetni(veza, transakcija,
                    "INSERT INTO edukacije (naziv, datum, kapacitet, prijavljeno) VALUES ($n, $d, $k, 0)",
                    ("$n", edukacija.Naziv), ("$d", edukacija.Datum), ("$k", edukacija.Kapacitet));
                uvezene.Add(new { id, naziv = edukacija.Naziv });
            }
            transakcija.Commit();
        }

        logger.LogInformation("Korisnik {Username} je uvezao {Broj} edukacija", HttpContext.Korisnik().Username, uvezene.Count);
        return Ok(new { uvezeno = uvezene.Count, edukacije = uvezene });
    }

    private static List<NovaEdukacija> ProcitajXml(string xml)
    {
        var postavke = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreWhitespace = true,
        };
        using var citac = XmlReader.Create(new StringReader(xml), postavke);
        var dokument = XDocument.Load(citac);
        if (dokument.Root is not { Name.LocalName: "edukacije" } korijen)
        {
            throw new FormatException("Očekivan je korijenski element <edukacije>.");
        }

        var edukacije = new List<NovaEdukacija>();
        foreach (var element in korijen.Elements("edukacija"))
        {
            var naziv = Obavezno(element, "naziv");
            var datum = DateOnly.ParseExact(Obavezno(element, "datum"), FormatDatuma, CultureInfo.InvariantCulture);
            var kapacitet = int.Parse(Obavezno(element, "kapacitet"), NumberStyles.Integer, CultureInfo.InvariantCulture);
            if (naziv.Length > 200 || kapacitet is < 1 or > 500)
            {
                throw new FormatException("Neispravni podaci edukacije.");
            }
            edukacije.Add(new NovaEdukacija(naziv, datum.ToString(FormatDatuma, CultureInfo.InvariantCulture), kapacitet));
        }
        if (edukacije.Count == 0)
        {
            throw new FormatException("XML ne sadrži nijednu edukaciju.");
        }
        return edukacije;
    }

    private static string Obavezno(XElement element, string naziv)
    {
        var vrijednost = ((string?)element.Element(naziv))?.Trim();
        return string.IsNullOrEmpty(vrijednost) ? throw new FormatException($"Nedostaje element <{naziv}>.") : vrijednost;
    }
}
