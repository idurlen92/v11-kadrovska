using System.Text.Encodings.Web;
using Kadrovska.Api.Autentikacija;
using Kadrovska.Api.Infrastruktura;
using Microsoft.AspNetCore.Mvc;

namespace Kadrovska.Api.Portal;

/// <summary>HTML potvrde za ispis.</summary>
[PotrebnaPrijava]
public sealed class IspisController(Baza baza) : ControllerBase
{
    [HttpGet("/ispis/zahtjevi/{id:long}")]
    public IActionResult Zahtjev(long id)
    {
        var korisnik = HttpContext.Korisnik();
        var z = baza.Upit("""
            SELECT z.id, z.zaposlenik, z.od, z."do", z.razlog, z.status, z.kreirano, k.ime, k.prezime
            FROM zahtjevi z LEFT JOIN korisnici k ON k.username = z.zaposlenik
            WHERE z.id = $id
            """, ("$id", id)).FirstOrDefault();
        if (z is null || (z.Tekst("zaposlenik") != korisnik.Username && !korisnik.JeAdmin))
        {
            return NotFound(new { greska = "Zahtjev ne postoji." });
        }

        var tijelo = $"""
            <h1>Zahtjev za godišnji odmor br. {z.Broj("id")}</h1>
            <table>
            <tr><th>Zaposlenik</th><td>{H(z.Tekst("ime"))} {H(z.Tekst("prezime"))} ({H(z.Tekst("zaposlenik"))})</td></tr>
            <tr><th>Razdoblje</th><td>{H(z.Tekst("od"))} – {H(z.Tekst("do"))}</td></tr>
            <tr><th>Razlog</th><td>{H(z.Tekst("razlog"))}</td></tr>
            <tr><th>Status</th><td>{H(z.Tekst("status"))}</td></tr>
            <tr><th>Podneseno</th><td>{H(z.Tekst("kreirano"))}</td></tr>
            </table>
            <p>Potpis voditelja: ______________________</p>
            """;
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'";
        return HtmlStranica.Odgovor($"Zahtjev za godišnji {id}", tijelo);
    }

    private static string H(string tekst) => HtmlEncoder.Default.Encode(tekst);
}
