using System.Globalization;
using Kadrovska.Api.Autentikacija;
using Kadrovska.Api.Infrastruktura;
using Microsoft.AspNetCore.Mvc;

namespace Kadrovska.Api.Portal;

/// <summary>Zahtjevi za godišnji odmor prijavljenog zaposlenika.</summary>
[ApiController]
[Route("api/zahtjevi")]
[PotrebnaPrijava]
public sealed class ZahtjeviController(Baza baza, ILogger<ZahtjeviController> logger) : ControllerBase
{
    private const string FormatDatuma = "yyyy-MM-dd";
    private const int NajduziRazlog = 500;

    public sealed record NoviZahtjev(string? Od, string? Do, string? Razlog, string? Oib);

    [HttpGet("moji")]
    public IActionResult Moji()
    {
        var redci = baza.Upit("""
            SELECT id, od, "do", razlog, oib, status, kreirano FROM zahtjevi WHERE zaposlenik = $z ORDER BY id
            """, ("$z", HttpContext.Korisnik().Username));
        return Ok(redci.Select(r => new
        {
            id = r.Broj("id"),
            od = r.Tekst("od"),
            @do = r.Tekst("do"),
            razlog = r.Tekst("razlog"),
            status = r.Tekst("status"),
            oib = Oib.Maskiraj(r.Tekst("oib")),
            kreirano = r.Tekst("kreirano"),
        }));
    }

    [HttpPost]
    public IActionResult Podnesi([FromBody] NoviZahtjev zahtjev)
    {
        var korisnik = HttpContext.Korisnik();
        logger.LogInformation("Novi zahtjev za godišnji: {Zahtjev}, zaposlenik {Username}", zahtjev, korisnik.Username);
        if (!DateOnly.TryParseExact(zahtjev.Od, FormatDatuma, CultureInfo.InvariantCulture, DateTimeStyles.None, out var od)
            || !DateOnly.TryParseExact(zahtjev.Do, FormatDatuma, CultureInfo.InvariantCulture, DateTimeStyles.None, out var doDatuma)
            || od > doDatuma)
        {
            return BadRequest(new { greska = "Neispravno razdoblje (od i do u obliku yyyy-MM-dd, od ne smije biti nakon do)." });
        }
        if (!Oib.JeIspravan(zahtjev.Oib))
        {
            return BadRequest(new { greska = "OIB mora imati 11 znamenki." });
        }
        var razlog = (zahtjev.Razlog ?? string.Empty).Trim();
        if (razlog.Length is 0 or > NajduziRazlog)
        {
            return BadRequest(new { greska = "Razlog je obavezan (najviše 500 znakova)." });
        }

        var kreirano = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        var id = baza.Umetni("""
            INSERT INTO zahtjevi (zaposlenik, od, "do", razlog, oib, status, kreirano)
            VALUES ($z, $od, $do, $r, $o, 'NA_CEKANJU', $k)
            """,
            ("$z", korisnik.Username), ("$od", od.ToString(FormatDatuma, CultureInfo.InvariantCulture)),
            ("$do", doDatuma.ToString(FormatDatuma, CultureInfo.InvariantCulture)), ("$r", razlog), ("$o", zahtjev.Oib), ("$k", kreirano));

        return StatusCode(StatusCodes.Status201Created, new
        {
            id,
            od = od.ToString(FormatDatuma, CultureInfo.InvariantCulture),
            @do = doDatuma.ToString(FormatDatuma, CultureInfo.InvariantCulture),
            razlog,
            status = "NA_CEKANJU",
            kreirano,
        });
    }

    /// <summary>Povlačenje zahtjeva.</summary>
    [HttpDelete("{id:long}")]
    public IActionResult Povuci(long id)
    {
        var korisnik = HttpContext.Korisnik();
        var zapis = baza.Upit("SELECT id, zaposlenik FROM zahtjevi WHERE id = $id", ("$id", id)).FirstOrDefault();
        if (zapis is null)
        {
            return NotFound(new { greska = "Zahtjev ne postoji." });
        }

        baza.Izvrsi("DELETE FROM zahtjevi WHERE id = $id", ("$id", id));
        logger.LogInformation("Korisnik {Username} je povukao zahtjev {Id}", korisnik.Username, id);
        return NoContent();
    }
}
