using System.Net;
using System.Net.Http.Json;
using Kadrovska.Tests.Infrastruktura;
using Xunit;

namespace Kadrovska.Tests.Funkcionalno;

[Trait("Category", "Funkcionalno")]
public sealed class PrijavaTests(KadrovskaFactory factory) : IClassFixture<KadrovskaFactory>
{
    [Fact]
    public async Task HealthVracaUp()
    {
        var odgovor = await factory.CreateClient().GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, odgovor.StatusCode);
        Assert.Equal("UP", (await odgovor.CitajJsonAsync()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task IspravnaPrijavaVracaTokenIUlogu()
    {
        var odgovor = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { username = "voditelj", password = "Ovlasteni12345!" });

        Assert.Equal(HttpStatusCode.OK, odgovor.StatusCode);
        var json = await odgovor.CitajJsonAsync();
        Assert.False(string.IsNullOrEmpty(json.GetProperty("token").GetString()));
        Assert.Equal("voditelj", json.GetProperty("username").GetString());
        Assert.Equal("VODITELJ", json.GetProperty("uloga").GetString());
    }

    [Fact]
    public async Task PogresnaLozinkaVraca401()
    {
        var odgovor = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { username = "ana", password = "pogresna" });

        Assert.Equal(HttpStatusCode.Unauthorized, odgovor.StatusCode);
        Assert.Equal("Neispravno korisničko ime ili lozinka.", (await odgovor.CitajJsonAsync()).GetProperty("greska").GetString());
    }

    [Fact]
    public async Task JaVracaPodatkePrijavljenogKorisnika()
    {
        var klijent = await factory.KlijentZaAsync("ana", "Ana12345!");

        var json = await (await klijent.GetAsync("/api/ja")).CitajJsonAsync();

        Assert.Equal("ana", json.GetProperty("username").GetString());
        Assert.Equal("Ana", json.GetProperty("ime").GetString());
        Assert.Equal("Anić", json.GetProperty("prezime").GetString());
        Assert.Equal("ana@primjer.hr", json.GetProperty("email").GetString());
        Assert.Equal("KORISNIK", json.GetProperty("uloga").GetString());
    }

    [Fact]
    public async Task RegistracijaIPrijavaNovogKorisnika()
    {
        var klijent = factory.CreateClient();
        await klijent.RegistrirajAsync("novi.zaposlenik", "Lozinka123!");

        var token = await klijent.PrijaviAsync("novi.zaposlenik", "Lozinka123!");

        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public async Task ZauzetoKorisnickoImeIKratkaLozinkaSeOdbijaju()
    {
        var klijent = factory.CreateClient();

        var zauzeto = await klijent.PostAsJsonAsync("/api/auth/register", new { username = "marko", password = "Lozinka123!" });
        var kratka = await klijent.PostAsJsonAsync("/api/auth/register", new { username = "kratka.lozinka", password = "abc" });

        Assert.Equal(HttpStatusCode.Conflict, zauzeto.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, kratka.StatusCode);
    }
}
