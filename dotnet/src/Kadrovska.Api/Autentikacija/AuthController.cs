using Microsoft.AspNetCore.Mvc;

namespace Kadrovska.Api.Autentikacija;

/// <summary>Prijava, registracija i obnova lozinke preko API-ja; podaci o prijavljenom korisniku.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(KorisniciServis korisnici, SesijeKorisnika sesije, ResetLozinkeServis reset) : ControllerBase
{
    public sealed record RegistracijaZahtjev(string? Username, string? Password, string? Email, string? Ime, string? Prezime);

    public sealed record ResetZahtjev(string? Username);

    public sealed record NovaLozinkaZahtjev(string? Token, string? NovaLozinka);

    [HttpPost("login")]
    public IActionResult Login([FromBody] PrijavaZahtjev zahtjev)
    {
        var korisnik = korisnici.Prijavi(zahtjev);
        if (korisnik is null)
        {
            return Unauthorized(new { greska = "Neispravno korisničko ime ili lozinka." });
        }
        return Ok(new { token = sesije.Otvori(korisnik), username = korisnik.Username, uloga = korisnik.Uloga });
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegistracijaZahtjev zahtjev)
    {
        if (!KorisniciServis.JeIspravnoKorisnickoIme(zahtjev.Username))
        {
            return BadRequest(new { greska = "Korisničko ime mora imati 3–40 znakova (slova, brojke, točka, crtica, podvlaka)." });
        }
        if (zahtjev.Password is null || zahtjev.Password.Length < ResetLozinkeServis.NajmanjaDuljinaLozinke)
        {
            return BadRequest(new { greska = "Lozinka mora imati najmanje 8 znakova." });
        }

        var id = korisnici.Registriraj(zahtjev.Username, zahtjev.Password,
            zahtjev.Email?.Trim() ?? string.Empty, zahtjev.Ime?.Trim() ?? string.Empty, zahtjev.Prezime?.Trim() ?? string.Empty);
        if (id is null)
        {
            return Conflict(new { greska = "Korisničko ime je zauzeto." });
        }
        return StatusCode(StatusCodes.Status201Created, new { id, username = zahtjev.Username });
    }

    [HttpPost("reset-zahtjev")]
    public IActionResult ResetZahtjevAkcija([FromBody] ResetZahtjev zahtjev)
    {
        reset.ZatraziPromjenu(zahtjev.Username);
        return StatusCode(StatusCodes.Status202Accepted,
            new { poruka = "Ako korisnik postoji, poslali smo upute na e-mail." });
    }

    [HttpPost("reset")]
    public IActionResult Reset([FromBody] NovaLozinkaZahtjev zahtjev) =>
        reset.PromijeniLozinku(zahtjev.Token, zahtjev.NovaLozinka) switch
        {
            RezultatPromjeneLozinke.Promijenjena => Ok(new { poruka = "Lozinka je promijenjena." }),
            RezultatPromjeneLozinke.PrekratkaLozinka => BadRequest(new { greska = "Lozinka mora imati najmanje 8 znakova." }),
            _ => BadRequest(new { greska = "Token je neispravan ili je istekao." }),
        };

    [HttpGet("/api/health")]
    public IActionResult Health() => Ok(new { status = "UP" });

    [HttpGet("/api/ja")]
    [PotrebnaPrijava]
    public IActionResult Ja()
    {
        var korisnik = korisnici.PronadjiPoId(HttpContext.Korisnik().Id);
        if (korisnik is null)
        {
            return NotFound(new { greska = "Korisnik ne postoji." });
        }
        return Ok(new
        {
            id = korisnik.Id,
            username = korisnik.Username,
            ime = korisnik.Ime,
            prezime = korisnik.Prezime,
            email = korisnik.Email,
            uloga = korisnik.Uloga,
        });
    }
}
