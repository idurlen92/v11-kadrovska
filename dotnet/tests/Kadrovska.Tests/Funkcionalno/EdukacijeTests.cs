using System.Net;
using Kadrovska.Tests.Infrastruktura;
using Xunit;

namespace Kadrovska.Tests.Funkcionalno;

[Trait("Category", "Funkcionalno")]
public sealed class EdukacijeTests(KadrovskaFactory factory) : IClassFixture<KadrovskaFactory>
{
    [Fact]
    public async Task PopisEdukacijaPrikazujeSlobodnaMjesta()
    {
        var klijent = await factory.KlijentZaAsync("ana", "Ana12345!");

        var edukacije = await (await klijent.GetAsync("/api/edukacije")).CitajJsonAsync();

        Assert.Equal(5, edukacije.GetArrayLength());
        var excel = edukacije.EnumerateArray().Single(e => e.GetProperty("id").GetInt64() == 2);
        Assert.Equal(0, excel.GetProperty("slobodno").GetInt32());
        var git = edukacije.EnumerateArray().Single(e => e.GetProperty("id").GetInt64() == 1);
        Assert.Equal(25, git.GetProperty("kapacitet").GetInt32());
    }

    [Fact]
    public async Task PrijavaNaEdukacijuIDvostrukaPrijava()
    {
        var klijent = await factory.KlijentZaAsync("marko", "Marko12345!");

        var prva = await klijent.PostAsync("/api/edukacije/4/prijava", null);
        var druga = await klijent.PostAsync("/api/edukacije/4/prijava", null);

        Assert.Equal(HttpStatusCode.Created, prva.StatusCode);
        Assert.Equal(4, (await prva.CitajJsonAsync()).GetProperty("edukacijaId").GetInt64());
        Assert.Equal(HttpStatusCode.Conflict, druga.StatusCode);
        var edukacije = await (await klijent.GetAsync("/api/edukacije")).CitajJsonAsync();
        var komunikacija = edukacije.EnumerateArray().Single(e => e.GetProperty("id").GetInt64() == 4);
        Assert.Equal(6, komunikacija.GetProperty("prijavljeno").GetInt32());
    }

    [Fact]
    public async Task PopunjenaEdukacijaVraca409()
    {
        var klijent = await factory.KlijentZaAsync("ana", "Ana12345!");

        var odgovor = await klijent.PostAsync("/api/edukacije/2/prijava", null);

        Assert.Equal(HttpStatusCode.Conflict, odgovor.StatusCode);
    }

    [Fact]
    public async Task NepostojecaEdukacijaVraca404()
    {
        var klijent = await factory.KlijentZaAsync("ana", "Ana12345!");

        var odgovor = await klijent.PostAsync("/api/edukacije/999/prijava", null);

        Assert.Equal(HttpStatusCode.NotFound, odgovor.StatusCode);
    }
}
