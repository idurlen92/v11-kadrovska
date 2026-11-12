using Microsoft.AspNetCore.Mvc;
using Kadrovska.Api.Autentikacija;
using Kadrovska.Api.Infrastruktura;

namespace Kadrovska.Api.Portal;

public enum FormatDokumenta
{
    Json,
    Pdf,
}

/// <summary>Platne liste zaposlenika: popis, prikaz (JSON/PDF) i preuzimanje datoteke.</summary>
[ApiController]
[Route("api/platne-liste")]
[PotrebnaPrijava]
public sealed class PlatneListeController(Baza baza, PostavkeAplikacije postavke, ILogger<PlatneListeController> logger) : ControllerBase
{
    [HttpGet("moje")]
    public IActionResult Moje()
    {
        var redci = baza.Upit(
            "SELECT id, razdoblje, datoteka FROM platne_liste WHERE zaposlenik = $z ORDER BY id", ("$z", HttpContext.Korisnik().Username));
        return Ok(redci.Select(r => new { id = r.Broj("id"), razdoblje = r.Tekst("razdoblje"), datoteka = r.Tekst("datoteka") }));
    }

    /// <summary><c>GET /api/platne-liste/{id}?format=json|pdf</c></summary>
    [HttpGet("{id:long}")]
    public IActionResult Dohvati(long id, [FromQuery] string? format)
    {
        var korisnik = HttpContext.Korisnik();
        var lista = baza.Upit("SELECT id, zaposlenik, razdoblje, datoteka FROM platne_liste WHERE id = $id", ("$id", id)).FirstOrDefault();
        if (lista is null)
        {
            return NotFound(new { greska = "Platna lista ne postoji." });
        }

        try
        {
            var oblik = Enum.Parse<FormatDokumenta>(format ?? string.Empty, ignoreCase: true);
            if (!SmijePristupiti(korisnik, lista))
            {
                return NotFound(new { greska = "Platna lista ne postoji." });
            }
            return Izvoz(lista, oblik);
        }
        catch (Exception ex)
        {
            // Ako izvoz u traženom formatu ne uspije, platna lista se prikazuje u zadanom obliku.
            logger.LogWarning(ex, "Izvoz platne liste {Id} nije uspio", id);
            return Ok(new
            {
                id = lista.Broj("id"),
                razdoblje = lista.Tekst("razdoblje"),
                datoteka = lista.Tekst("datoteka"),
                sadrzaj = ProcitajSadrzaj(lista),
                upozorenje = ex.Message,
            });
        }
    }

    /// <summary><c>GET /api/platne-liste/preuzmi?datoteka=placa-ana-2026-10.txt</c></summary>
    [HttpGet("preuzmi")]
    public IActionResult Preuzmi([FromQuery] string? datoteka)
    {
        var korisnik = HttpContext.Korisnik();
        if (string.IsNullOrWhiteSpace(datoteka))
        {
            return BadRequest(new { greska = "Naziv datoteke je obavezan." });
        }

        var zapis = baza.Upit(
            "SELECT datoteka FROM platne_liste WHERE datoteka = $d AND (zaposlenik = $z OR $admin = 1)",
            ("$d", datoteka), ("$z", korisnik.Username), ("$admin", korisnik.JeAdmin ? 1 : 0)).FirstOrDefault();
        if (zapis is null)
        {
            return NotFound(new { greska = "Datoteka ne postoji." });
        }
        var putanja = Path.GetFullPath(Path.Combine(postavke.DirektorijDokumenata, Path.GetFileName(zapis.Tekst("datoteka"))));
        if (!putanja.StartsWith(postavke.DirektorijDokumenata + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return NotFound(new { greska = "Datoteka ne postoji." });
        }

        if (!System.IO.File.Exists(putanja))
        {
            return NotFound(new { greska = "Datoteka ne postoji." });
        }
        logger.LogInformation("Korisnik {Username} preuzima datoteku {Datoteka}", korisnik.Username, Path.GetFileName(putanja));
        return File(System.IO.File.ReadAllBytes(putanja), "text/plain; charset=utf-8", Path.GetFileName(putanja));
    }

    private static bool SmijePristupiti(PrijavljeniKorisnik korisnik, Dictionary<string, object?> lista) =>
        korisnik.JeAdmin || lista.Tekst("zaposlenik") == korisnik.Username;

    private IActionResult Izvoz(Dictionary<string, object?> lista, FormatDokumenta oblik)
    {
        var sadrzaj = ProcitajSadrzaj(lista);
        return oblik switch
        {
            FormatDokumenta.Pdf => File(JednostavniPdf.Izradi($"Platna lista {lista.Tekst("razdoblje")}", sadrzaj), "application/pdf", $"platna-lista-{lista.Broj("id")}.pdf"),
            _ => Ok(new
            {
                id = lista.Broj("id"),
                razdoblje = lista.Tekst("razdoblje"),
                datoteka = lista.Tekst("datoteka"),
                sadrzaj,
            }),
        };
    }

    private string ProcitajSadrzaj(Dictionary<string, object?> lista) =>
        System.IO.File.ReadAllText(Path.Combine(postavke.DirektorijDokumenata, Path.GetFileName(lista.Tekst("datoteka"))));
}
