using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Kadrovska.Tests.Infrastruktura;
using Xunit;

namespace Kadrovska.Tests.Funkcionalno;

[Trait("Category", "Funkcionalno")]
public sealed class ResetLozinkeTests(KadrovskaFactory factory) : IClassFixture<KadrovskaFactory>
{
    [Fact]
    public async Task ZahtjevZaNepostojecegKorisnikaVracaIstiOdgovor()
    {
        var odgovor = await factory.CreateClient().PostAsJsonAsync("/api/auth/reset-zahtjev", new { username = "nepostojeci" });

        Assert.Equal(HttpStatusCode.Accepted, odgovor.StatusCode);
        Assert.Equal("Ako korisnik postoji, poslali smo upute na e-mail.",
            (await odgovor.CitajJsonAsync()).GetProperty("poruka").GetString());
    }

    [Fact]
    public async Task CijeliTokObnoveLozinke()
    {
        var klijent = factory.CreateClient();
        await klijent.RegistrirajAsync("zaboravna.ivana", "StaraLozinka1!");

        var zahtjev = await klijent.PostAsJsonAsync("/api/auth/reset-zahtjev", new { username = "zaboravna.ivana" });
        Assert.Equal(HttpStatusCode.Accepted, zahtjev.StatusCode);

        var poruka = Assert.Single(Directory.GetFiles(factory.DirektorijPoste, "zaboravna.ivana-*.txt"));
        var tekst = await File.ReadAllTextAsync(poruka);
        Assert.Contains("/reset?token=", tekst);
        var token = Regex.Match(tekst, @"Token: (\S+)").Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(token));

        var reset = await klijent.PostAsJsonAsync("/api/auth/reset", new { token, novaLozinka = "NovaLozinka1!" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Equal("Lozinka je promijenjena.", (await reset.CitajJsonAsync()).GetProperty("poruka").GetString());

        var novaPrijava = await klijent.PostAsJsonAsync("/api/auth/login", new { username = "zaboravna.ivana", password = "NovaLozinka1!" });
        var staraPrijava = await klijent.PostAsJsonAsync("/api/auth/login", new { username = "zaboravna.ivana", password = "StaraLozinka1!" });
        Assert.Equal(HttpStatusCode.OK, novaPrijava.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, staraPrijava.StatusCode);
    }

    [Fact]
    public async Task NeispravanTokenVraca400()
    {
        var odgovor = await factory.CreateClient().PostAsJsonAsync("/api/auth/reset", new { token = "abc", novaLozinka = "NovaLozinka1!" });

        Assert.Equal(HttpStatusCode.BadRequest, odgovor.StatusCode);
    }
}
