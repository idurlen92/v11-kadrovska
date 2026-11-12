using System.Net;
using System.Text;
using Kadrovska.Tests.Infrastruktura;
using Xunit;

namespace Kadrovska.Tests.Funkcionalno;

[Trait("Category", "Funkcionalno")]
public sealed class UvozIPostavkeTests(KadrovskaFactory factory) : IClassFixture<KadrovskaFactory>
{
    private const string NoveEdukacije = """
        <?xml version="1.0" encoding="utf-8"?>
        <edukacije>
          <edukacija>
            <naziv>Zaštita osobnih podataka</naziv>
            <datum>2026-12-03</datum>
            <kapacitet>15</kapacitet>
          </edukacija>
          <edukacija>
            <naziv>Upravljanje vremenom</naziv>
            <datum>2026-12-17</datum>
            <kapacitet>20</kapacitet>
          </edukacija>
        </edukacije>
        """;

    private static StringContent Xml(string xml) => new(xml, Encoding.UTF8, "application/xml");

    [Fact]
    public async Task VoditeljUvoziEdukacijeIzXmla()
    {
        var klijent = await factory.KlijentZaAsync("voditelj", "Ovlasteni12345!");

        var odgovor = await klijent.PostAsync("/api/edukacije/uvoz", Xml(NoveEdukacije));

        Assert.Equal(HttpStatusCode.OK, odgovor.StatusCode);
        var json = await odgovor.CitajJsonAsync();
        Assert.Equal(2, json.GetProperty("uvezeno").GetInt32());
        var prva = json.GetProperty("edukacije")[0];
        Assert.True(prva.GetProperty("id").GetInt64() > 5);
        Assert.Equal("Zaštita osobnih podataka", prva.GetProperty("naziv").GetString());
    }

    [Fact]
    public async Task NeispravanXmlVraca400()
    {
        var klijent = await factory.KlijentZaAsync("admin", "Admin12345!");

        var odgovor = await klijent.PostAsync("/api/edukacije/uvoz", Xml("<edukacije><edukacija>"));

        Assert.Equal(HttpStatusCode.BadRequest, odgovor.StatusCode);
    }

    [Fact]
    public async Task ZaposlenikNeMozeUvoziti()
    {
        var klijent = await factory.KlijentZaAsync("ana", "Ana12345!");

        var odgovor = await klijent.PostAsync("/api/edukacije/uvoz", Xml(NoveEdukacije));

        Assert.Equal(HttpStatusCode.Forbidden, odgovor.StatusCode);
    }

    [Fact]
    public async Task PostavkeObavijestiUYamlu()
    {
        var klijent = await factory.KlijentZaAsync("marko", "Marko12345!");
        const string yaml = """
            podsjetnik: true
            satiPrije: 48
            kanal: sms
            """;

        var odgovor = await klijent.PostAsync("/api/postavke", new StringContent(yaml, Encoding.UTF8, "application/yaml"));

        Assert.Equal(HttpStatusCode.OK, odgovor.StatusCode);
        var postavke = (await odgovor.CitajJsonAsync()).GetProperty("postavke");
        Assert.True(postavke.GetProperty("podsjetnik").GetBoolean());
        Assert.Equal(48, postavke.GetProperty("satiPrije").GetInt32());
        Assert.Equal("sms", postavke.GetProperty("kanal").GetString());

        var spremljeno = (await (await klijent.GetAsync("/api/postavke")).CitajJsonAsync()).GetProperty("postavke");
        Assert.Equal(48, spremljeno.GetProperty("satiPrije").GetInt32());
    }

    [Fact]
    public async Task NeispravnePostavkeVracaju400()
    {
        var klijent = await factory.KlijentZaAsync("ana", "Ana12345!");

        var odgovor = await klijent.PostAsync("/api/postavke", new StringContent("satiPrije: puno", Encoding.UTF8, "text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, odgovor.StatusCode);
    }
}
