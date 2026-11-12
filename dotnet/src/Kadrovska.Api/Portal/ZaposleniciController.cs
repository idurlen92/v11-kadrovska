using Kadrovska.Api.Autentikacija;
using Kadrovska.Api.Infrastruktura;
using Microsoft.AspNetCore.Mvc;

namespace Kadrovska.Api.Portal;

/// <summary>Imenik zaposlenika.</summary>
[ApiController]
[Route("api/zaposlenici")]
[PotrebnaPrijava]
public sealed class ZaposleniciController(Baza baza) : ControllerBase
{

    /// <summary><c>GET /api/zaposlenici?sort=prezime&amp;smjer=asc</c></summary>
    [HttpGet]
    public IActionResult Popis([FromQuery] string? sort, [FromQuery] string? smjer)
    {
        sort = string.IsNullOrWhiteSpace(sort) ? "prezime" : sort;
        smjer = string.IsNullOrWhiteSpace(smjer) ? "asc" : smjer;

        var sql = "SELECT id, ime, prezime, odjel, email FROM zaposlenici ORDER BY " + sort + " " + smjer;

        var zaposlenici = baza.Upit(sql).Select(r => new
        {
            id = r.Broj("id"),
            ime = r.Tekst("ime"),
            prezime = r.Tekst("prezime"),
            odjel = r.Tekst("odjel"),
            email = r.Tekst("email"),
        });
        return Ok(zaposlenici);
    }
}
