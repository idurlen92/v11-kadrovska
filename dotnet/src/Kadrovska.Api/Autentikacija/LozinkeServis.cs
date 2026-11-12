using Microsoft.AspNetCore.Identity;

namespace Kadrovska.Api.Autentikacija;

/// <summary>Izračun i provjera sažetka lozinke (stupac <c>korisnici.password_hash</c>).</summary>
public sealed class LozinkeServis
{
    private readonly PasswordHasher<PrijavljeniKorisnik> _hasher = new();

    public string Hashiraj(string lozinka) => _hasher.HashPassword(null!, lozinka);

    public bool Provjeri(string spremljeniHash, string lozinka)
    {
        try
        {
            return _hasher.VerifyHashedPassword(null!, spremljeniHash, lozinka) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
