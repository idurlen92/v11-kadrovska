using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace Kadrovska.Api.Autentikacija;

/// <summary>Pristupni tokeni prijavljenih korisnika (32 nasumična bajta, Base64Url), čuvaju se u memoriji.</summary>
public sealed class SesijeKorisnika
{
    private readonly ConcurrentDictionary<string, PrijavljeniKorisnik> _sesije = new(StringComparer.Ordinal);

    public string Otvori(PrijavljeniKorisnik korisnik)
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        _sesije[token] = korisnik;
        return token;
    }

    public PrijavljeniKorisnik? Pronadji(string token) =>
        _sesije.TryGetValue(token, out var korisnik) ? korisnik : null;
}
