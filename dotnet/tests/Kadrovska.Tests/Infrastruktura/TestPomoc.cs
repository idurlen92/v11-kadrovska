using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Kadrovska.Tests.Infrastruktura;

/// <summary>Pomoćne metode za testove: prijava, registracija, čitanje JSON odgovora.</summary>
public static class TestPomoc
{
    /// <summary>Prijava preko <c>POST /api/auth/login</c>; vraća token.</summary>
    public static async Task<string> PrijaviAsync(this HttpClient klijent, string username, string password)
    {
        var odgovor = await klijent.PostAsJsonAsync("/api/auth/login", new { username, password });
        Assert.Equal(HttpStatusCode.OK, odgovor.StatusCode);
        var json = await odgovor.CitajJsonAsync();
        return json.GetProperty("token").GetString()!;
    }

    /// <summary>Klijent koji svakom zahtjevu dodaje zaglavlje <c>Authorization: Bearer &lt;token&gt;</c>.</summary>
    public static async Task<HttpClient> KlijentZaAsync(this WebApplicationFactory<Program> factory, string username, string password)
    {
        var klijent = factory.CreateClient();
        var token = await klijent.PrijaviAsync(username, password);
        klijent.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return klijent;
    }

    /// <summary>Registrira novog korisnika (uloga KORISNIK).</summary>
    public static async Task RegistrirajAsync(this HttpClient klijent, string username, string password)
    {
        var odgovor = await klijent.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            password,
            email = $"{username}@primjer.hr",
            ime = "Test",
            prezime = "Korisnik",
        });
        Assert.Equal(HttpStatusCode.Created, odgovor.StatusCode);
    }

    public static async Task<JsonElement> CitajJsonAsync(this HttpResponseMessage odgovor)
    {
        var tekst = await odgovor.Content.ReadAsStringAsync();
        using var dokument = JsonDocument.Parse(tekst);
        return dokument.RootElement.Clone();
    }
}
