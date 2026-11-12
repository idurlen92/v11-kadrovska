using System.Text.Encodings.Web;
using Kadrovska.Api.Infrastruktura;
using Microsoft.AspNetCore.Mvc;

namespace Kadrovska.Api.Autentikacija;

/// <summary>HTML stranice: početna, prijava obrascem i postavljanje nove lozinke (poveznica iz e-pošte).</summary>
public sealed class StraniceController(KorisniciServis korisnici, SesijeKorisnika sesije, ResetLozinkeServis reset) : ControllerBase
{
    [HttpGet("/")]
    public IActionResult Pocetna()
    {
        var korisnik = HttpContext.TrenutniKorisnik();
        var stanje = korisnik is null
            ? """<p><a href="/prijava">Prijava</a></p>"""
            : $"""<p>Prijavljeni ste kao <strong>{H(korisnik.Username)}</strong>.</p>""";
        return HtmlStranica.Odgovor("Početna", $"""
            <h1>{Aplikacija.Naziv}</h1>
            <p>{Aplikacija.Opis}</p>
            {stanje}
            <p>API: <code>/api/health</code>, <code>/api/auth/login</code>, <code>/api/ja</code></p>
            """);
    }

    [HttpGet("/prijava")]
    public IActionResult PrijavaObrazac([FromQuery] string? returnUrl) =>
        HtmlStranica.Odgovor("Prijava", ObrazacPrijave(returnUrl, null));

    [HttpPost("/prijava")]
    public IActionResult Prijava([FromForm] string? username, [FromForm] string? password, [FromForm] string? returnUrl)
    {
        var korisnik = korisnici.Prijavi(new PrijavaZahtjev(username, password));
        if (korisnik is null)
        {
            return HtmlStranica.Odgovor("Prijava", ObrazacPrijave(returnUrl, "Neispravno korisničko ime ili lozinka."));
        }

        Response.Cookies.Append(SesijaMiddleware.NazivKolacica, sesije.Otvori(korisnik), new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
        });
        return Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }

    [HttpGet("/reset")]
    public IActionResult ResetObrazac([FromQuery] string? token) =>
        HtmlStranica.Odgovor("Nova lozinka", ObrazacNoveLozinke(token, null));

    [HttpPost("/reset")]
    public IActionResult Reset([FromForm] string? token, [FromForm] string? novaLozinka) =>
        reset.PromijeniLozinku(token, novaLozinka) switch
        {
            RezultatPromjeneLozinke.Promijenjena => HtmlStranica.Odgovor("Nova lozinka",
                """<h1>Lozinka je promijenjena</h1><p><a href="/prijava">Prijava</a></p>"""),
            RezultatPromjeneLozinke.PrekratkaLozinka => HtmlStranica.Odgovor("Nova lozinka",
                ObrazacNoveLozinke(token, "Lozinka mora imati najmanje 8 znakova."), StatusCodes.Status400BadRequest),
            _ => HtmlStranica.Odgovor("Nova lozinka",
                ObrazacNoveLozinke(token, "Poveznica je neispravna ili je istekla."), StatusCodes.Status400BadRequest),
        };

    private static string ObrazacPrijave(string? returnUrl, string? greska) => $"""
        <h1>Prijava</h1>
        {Poruka(greska)}
        <form method="post" action="/prijava">
        <label>Korisničko ime <input name="username" autocomplete="username" required></label>
        <label>Lozinka <input name="password" type="password" autocomplete="current-password" required></label>
        <input type="hidden" name="returnUrl" value="{H(returnUrl ?? string.Empty)}">
        <button type="submit">Prijavi se</button>
        </form>
        """;

    private static string ObrazacNoveLozinke(string? token, string? greska) => $"""
        <h1>Nova lozinka</h1>
        {Poruka(greska)}
        <form method="post" action="/reset">
        <input type="hidden" name="token" value="{H(token ?? string.Empty)}">
        <label>Nova lozinka <input name="novaLozinka" type="password" autocomplete="new-password" minlength="8" required></label>
        <button type="submit">Spremi</button>
        </form>
        """;

    private static string Poruka(string? greska) =>
        greska is null ? string.Empty : "<p class=\"greska\">" + H(greska) + "</p>";

    private static string H(string tekst) => HtmlEncoder.Default.Encode(tekst);
}
