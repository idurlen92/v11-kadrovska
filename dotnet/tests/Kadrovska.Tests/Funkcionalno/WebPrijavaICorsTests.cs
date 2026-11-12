using System.Net;
using System.Net.Http.Headers;
using Kadrovska.Tests.Infrastruktura;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Kadrovska.Tests.Funkcionalno;

[Trait("Category", "Funkcionalno")]
public sealed class WebPrijavaICorsTests(KadrovskaFactory factory) : IClassFixture<KadrovskaFactory>
{
    private HttpClient KlijentBezPreusmjeravanja() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });

    [Fact]
    public async Task PocetnaIObrazacPrijaveSuDostupni()
    {
        var klijent = KlijentBezPreusmjeravanja();

        var pocetna = await klijent.GetAsync("/");
        var obrazac = await klijent.GetAsync("/prijava?returnUrl=/moji");

        Assert.Equal(HttpStatusCode.OK, pocetna.StatusCode);
        Assert.Contains("Kadrovska", await pocetna.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, obrazac.StatusCode);
        var html = await obrazac.Content.ReadAsStringAsync();
        Assert.Contains("name=\"username\"", html);
        Assert.Contains("value=\"/moji\"", html);
    }

    [Fact]
    public async Task PrijavaObrascemPreusmjeravaNaReturnUrlIPostavljaKolacic()
    {
        var klijent = KlijentBezPreusmjeravanja();

        var odgovor = await klijent.PostAsync("/prijava", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = "ana",
            ["password"] = "Ana12345!",
            ["returnUrl"] = "/moji",
        }));

        Assert.Equal(HttpStatusCode.Found, odgovor.StatusCode);
        Assert.Equal("/moji", odgovor.Headers.Location?.OriginalString);
        var kolacic = Assert.Single(odgovor.Headers.GetValues("Set-Cookie"), c => c.StartsWith("SESSION=", StringComparison.Ordinal));
        Assert.Contains("httponly", kolacic, StringComparison.OrdinalIgnoreCase);

        // Kolačić vrijedi i za API.
        var zahtjev = new HttpRequestMessage(HttpMethod.Get, "/api/ja");
        zahtjev.Headers.Add("Cookie", kolacic.Split(';')[0]);
        var ja = await klijent.SendAsync(zahtjev);
        Assert.Equal(HttpStatusCode.OK, ja.StatusCode);
        Assert.Equal("ana", (await ja.CitajJsonAsync()).GetProperty("username").GetString());
    }

    [Fact]
    public async Task NeuspjelaPrijavaObrascemVracaObrazacSPorukom()
    {
        var odgovor = await KlijentBezPreusmjeravanja().PostAsync("/prijava", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = "ana",
            ["password"] = "pogresna",
            ["returnUrl"] = "/moji",
        }));

        Assert.Equal(HttpStatusCode.OK, odgovor.StatusCode);
        Assert.Contains("Neispravno korisni", await odgovor.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CorsDopustaKonfiguriraniOrigin()
    {
        var zahtjev = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        zahtjev.Headers.Add("Origin", "http://localhost:5173");

        var odgovor = await KlijentBezPreusmjeravanja().SendAsync(zahtjev);

        Assert.Equal(HttpStatusCode.OK, odgovor.StatusCode);
        Assert.Equal("http://localhost:5173", Assert.Single(odgovor.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task ZasticeniApiBezPrijaveVraca401()
    {
        var klijent = KlijentBezPreusmjeravanja();
        klijent.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "nepostojeci-token");

        var odgovor = await klijent.GetAsync("/api/ja");

        Assert.Equal(HttpStatusCode.Unauthorized, odgovor.StatusCode);
    }
}
