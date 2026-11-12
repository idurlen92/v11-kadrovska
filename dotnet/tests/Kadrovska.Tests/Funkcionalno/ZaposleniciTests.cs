using System.Net;
using System.Text.Json;
using Kadrovska.Tests.Infrastruktura;
using Xunit;

namespace Kadrovska.Tests.Funkcionalno;

[Trait("Category", "Funkcionalno")]
public sealed class ZaposleniciTests(KadrovskaFactory factory) : IClassFixture<KadrovskaFactory>
{
    private static List<string> Prezimena(JsonElement json) =>
        json.EnumerateArray().Select(z => z.GetProperty("prezime").GetString()!).ToList();

    [Fact]
    public async Task ZadanoSortiranjePoPrezimenuUzlazno()
    {
        var klijent = await factory.KlijentZaAsync("ana", "Ana12345!");

        var odgovor = await klijent.GetAsync("/api/zaposlenici");

        Assert.Equal(HttpStatusCode.OK, odgovor.StatusCode);
        var prezimena = Prezimena(await odgovor.CitajJsonAsync());
        Assert.Equal(8, prezimena.Count);
        Assert.Equal("Admin", prezimena[0]);
        Assert.Equal("Vidović", prezimena[^1]);
    }

    [Fact]
    public async Task SortiranjePoPrezimenuSilazno()
    {
        var klijent = await factory.KlijentZaAsync("ana", "Ana12345!");

        var prezimena = Prezimena(await (await klijent.GetAsync("/api/zaposlenici?sort=prezime&smjer=desc")).CitajJsonAsync());

        Assert.Equal(new[] { "Vidović", "Novak", "Marić", "Kovač", "Jurić", "Babić", "Anić", "Admin" }, prezimena);
    }

    [Fact]
    public async Task SortiranjePoImenuIOdjelu()
    {
        var klijent = await factory.KlijentZaAsync("ana", "Ana12345!");

        var poImenu = await (await klijent.GetAsync("/api/zaposlenici?sort=ime&smjer=asc")).CitajJsonAsync();
        var poOdjelu = await (await klijent.GetAsync("/api/zaposlenici?sort=odjel&smjer=asc")).CitajJsonAsync();

        Assert.Equal("Ana", poImenu[0].GetProperty("ime").GetString());
        Assert.Equal("Zoran", poImenu[7].GetProperty("ime").GetString());
        Assert.Equal("Financije", poOdjelu[0].GetProperty("odjel").GetString());
    }

    [Fact]
    public async Task ImenikZahtijevaPrijavu()
    {
        var odgovor = await factory.CreateClient().GetAsync("/api/zaposlenici");

        Assert.Equal(HttpStatusCode.Unauthorized, odgovor.StatusCode);
    }
}
