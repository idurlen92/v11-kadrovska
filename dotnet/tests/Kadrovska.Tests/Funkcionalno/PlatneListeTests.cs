using System.Net;
using System.Text;
using Kadrovska.Tests.Infrastruktura;
using Xunit;

namespace Kadrovska.Tests.Funkcionalno;

[Trait("Category", "Funkcionalno")]
public sealed class PlatneListeTests(KadrovskaFactory factory) : IClassFixture<KadrovskaFactory>
{
    [Fact]
    public async Task MojePlatneListeSadrzeSamoVlastite()
    {
        var klijent = await factory.KlijentZaAsync("ana", "Ana12345!");

        var liste = await (await klijent.GetAsync("/api/platne-liste/moje")).CitajJsonAsync();

        var lista = Assert.Single(liste.EnumerateArray());
        Assert.Equal("2026-10", lista.GetProperty("razdoblje").GetString());
        Assert.Equal("placa-ana-2026-10.txt", lista.GetProperty("datoteka").GetString());
    }

    [Fact]
    public async Task PreuzimanjeVlastiteDatoteke()
    {
        var klijent = await factory.KlijentZaAsync("ana", "Ana12345!");

        var odgovor = await klijent.GetAsync("/api/platne-liste/preuzmi?datoteka=placa-ana-2026-10.txt");

        Assert.Equal(HttpStatusCode.OK, odgovor.StatusCode);
        var sadrzaj = await odgovor.Content.ReadAsStringAsync();
        Assert.Contains("Zaposlenik: Ana Anić", sadrzaj);
        Assert.Contains("1512.00 EUR", sadrzaj);
    }

    [Fact]
    public async Task PlatnaListaUJsonFormatu()
    {
        var klijent = await factory.KlijentZaAsync("marko", "Marko12345!");

        var odgovor = await klijent.GetAsync("/api/platne-liste/2?format=json");

        Assert.Equal(HttpStatusCode.OK, odgovor.StatusCode);
        var json = await odgovor.CitajJsonAsync();
        Assert.Equal(2, json.GetProperty("id").GetInt64());
        Assert.Equal("2026-10", json.GetProperty("razdoblje").GetString());
        Assert.Contains("1830.40 EUR", json.GetProperty("sadrzaj").GetString());
    }

    [Fact]
    public async Task PlatnaListaUPdfFormatu()
    {
        var klijent = await factory.KlijentZaAsync("ana", "Ana12345!");

        var odgovor = await klijent.GetAsync("/api/platne-liste/1?format=pdf");

        Assert.Equal(HttpStatusCode.OK, odgovor.StatusCode);
        Assert.Equal("application/pdf", odgovor.Content.Headers.ContentType?.MediaType);
        var bajtovi = await odgovor.Content.ReadAsByteArrayAsync();
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(bajtovi, 0, 5));
        Assert.Contains("1512.00 EUR", Encoding.ASCII.GetString(bajtovi));
    }

    [Fact]
    public async Task AdministratorVidiSvePlatneListe()
    {
        var klijent = await factory.KlijentZaAsync("admin", "Admin12345!");

        var odgovor = await klijent.GetAsync("/api/platne-liste/1?format=json");

        Assert.Equal(HttpStatusCode.OK, odgovor.StatusCode);
    }
}
