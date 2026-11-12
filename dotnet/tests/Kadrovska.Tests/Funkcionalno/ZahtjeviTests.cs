using System.Net;
using System.Net.Http.Json;
using Kadrovska.Tests.Infrastruktura;
using Xunit;

namespace Kadrovska.Tests.Funkcionalno;

[Trait("Category", "Funkcionalno")]
public sealed class ZahtjeviTests(KadrovskaFactory factory) : IClassFixture<KadrovskaFactory>
{
    private static async Task<long> PodnesiAsync(HttpClient klijent, string od, string @do, string razlog)
    {
        var odgovor = await klijent.PostAsJsonAsync("/api/zahtjevi", new { od, @do, razlog, oib = "48291736504" });
        Assert.Equal(HttpStatusCode.Created, odgovor.StatusCode);
        return (await odgovor.CitajJsonAsync()).GetProperty("id").GetInt64();
    }

    [Fact]
    public async Task MojiZahtjeviSadrzePocetniZahtjev()
    {
        var klijent = await factory.KlijentZaAsync("ana", "Ana12345!");

        var moji = await (await klijent.GetAsync("/api/zahtjevi/moji")).CitajJsonAsync();

        var prvi = moji.EnumerateArray().Single(z => z.GetProperty("id").GetInt64() == 1);
        Assert.Equal("Obiteljsko putovanje", prvi.GetProperty("razlog").GetString());
        Assert.Equal("2026-12-21", prvi.GetProperty("od").GetString());
        Assert.Equal("2026-12-31", prvi.GetProperty("do").GetString());
        Assert.Equal("*******6504", prvi.GetProperty("oib").GetString());
        Assert.DoesNotContain(moji.EnumerateArray(), z => z.GetProperty("id").GetInt64() == 2);
    }

    [Fact]
    public async Task NoviZahtjevVidljivJeUMojimZahtjevima()
    {
        var klijent = await factory.KlijentZaAsync("ana", "Ana12345!");

        var id = await PodnesiAsync(klijent, "2027-02-01", "2027-02-05", "Zimski odmor");

        var moji = await (await klijent.GetAsync("/api/zahtjevi/moji")).CitajJsonAsync();
        var novi = moji.EnumerateArray().Single(z => z.GetProperty("id").GetInt64() == id);
        Assert.Equal("Zimski odmor", novi.GetProperty("razlog").GetString());
        Assert.Equal("NA_CEKANJU", novi.GetProperty("status").GetString());
    }

    [Fact]
    public async Task NeispravnoRazdobljeVraca400()
    {
        var klijent = await factory.KlijentZaAsync("marko", "Marko12345!");

        var obrnuto = await klijent.PostAsJsonAsync("/api/zahtjevi", new { od = "2027-03-10", @do = "2027-03-01", razlog = "Odmor", oib = "73019485267" });
        var bezOiba = await klijent.PostAsJsonAsync("/api/zahtjevi", new { od = "2027-03-01", @do = "2027-03-10", razlog = "Odmor", oib = "123" });

        Assert.Equal(HttpStatusCode.BadRequest, obrnuto.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, bezOiba.StatusCode);
    }

    [Fact]
    public async Task VlastitiZahtjevSeMozePovuci()
    {
        var klijent = await factory.KlijentZaAsync("ana", "Ana12345!");
        var id = await PodnesiAsync(klijent, "2027-04-06", "2027-04-10", "Uskrsni praznici");

        var povlacenje = await klijent.DeleteAsync($"/api/zahtjevi/{id}");

        Assert.Equal(HttpStatusCode.NoContent, povlacenje.StatusCode);
        var moji = await (await klijent.GetAsync("/api/zahtjevi/moji")).CitajJsonAsync();
        Assert.DoesNotContain(moji.EnumerateArray(), z => z.GetProperty("id").GetInt64() == id);
    }

    [Fact]
    public async Task IspisPotvrdeSadrziRazlog()
    {
        var klijent = await factory.KlijentZaAsync("ana", "Ana12345!");
        var id = await PodnesiAsync(klijent, "2027-05-04", "2027-05-08", "Selidba u novi stan");

        var odgovor = await klijent.GetAsync($"/ispis/zahtjevi/{id}");

        Assert.Equal(HttpStatusCode.OK, odgovor.StatusCode);
        Assert.Equal("text/html", odgovor.Content.Headers.ContentType?.MediaType);
        var html = await odgovor.Content.ReadAsStringAsync();
        Assert.Contains("Selidba u novi stan", html);
        Assert.Contains("2027-05-04", html);
    }
}
