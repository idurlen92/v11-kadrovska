using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Kadrovska.Api.Infrastruktura;
using Microsoft.Data.Sqlite;

namespace Kadrovska.Api.Autentikacija;

public sealed record PrijavaZahtjev(string? Username, string? Password);

/// <summary>Redak tablice <c>korisnici</c>.</summary>
public sealed record Korisnik(long Id, string Username, string PasswordHash, string Email, string Ime, string Prezime, string Uloga);

/// <summary>Korisnički računi: prijava, registracija i promjena lozinke.</summary>
public sealed class KorisniciServis(Baza baza, LozinkeServis lozinke, ILogger<KorisniciServis> logger)
{
    private const string Stupci = "id, username, password_hash, email, ime, prezime, uloga";
    private static readonly Regex DozvoljenoKorisnickoIme = new("^[A-Za-z0-9._-]{3,40}$", RegexOptions.CultureInvariant);

    public static bool JeIspravnoKorisnickoIme([NotNullWhen(true)] string? username) =>
        username is not null && DozvoljenoKorisnickoIme.IsMatch(username);

    public Korisnik? PronadjiPoKorisnickomImenu(string username) =>
        baza.Upit($"SELECT {Stupci} FROM korisnici WHERE username = $u", ("$u", username))
            .Select(Mapiraj)
            .FirstOrDefault();

    public Korisnik? PronadjiPoId(long id) =>
        baza.Upit($"SELECT {Stupci} FROM korisnici WHERE id = $id", ("$id", id))
            .Select(Mapiraj)
            .FirstOrDefault();

    public PrijavljeniKorisnik? Prijavi(PrijavaZahtjev zahtjev)
    {
        logger.LogInformation("Zahtjev za prijavu: {Zahtjev}", zahtjev);
        var korisnik = string.IsNullOrEmpty(zahtjev.Username) ? null : PronadjiPoKorisnickomImenu(zahtjev.Username);
        if (korisnik is null || string.IsNullOrEmpty(zahtjev.Password) || !lozinke.Provjeri(korisnik.PasswordHash, zahtjev.Password))
        {
            return null;
        }

        logger.LogInformation("Korisnik {Username} je prijavljen", korisnik.Username);
        return new PrijavljeniKorisnik(korisnik.Id, korisnik.Username, korisnik.Uloga);
    }

    /// <summary>Registracija novog korisnika s ulogom KORISNIK; <c>null</c> ako je korisničko ime zauzeto.</summary>
    public long? Registriraj(string username, string lozinka, string email, string ime, string prezime)
    {
        if (PronadjiPoKorisnickomImenu(username) is not null)
        {
            return null;
        }
        try
        {
            var id = baza.Umetni(
                "INSERT INTO korisnici (username, password_hash, email, ime, prezime, uloga) VALUES ($u, $h, $e, $i, $p, $r)",
                ("$u", username), ("$h", lozinke.Hashiraj(lozinka)), ("$e", email), ("$i", ime), ("$p", prezime), ("$r", Uloge.Korisnik));
            logger.LogInformation("Registriran je novi korisnik {Username}", username);
            return id;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return null;
        }
    }

    public void DodajPocetnogKorisnika(long id, string username, string lozinka, string uloga, string ime, string prezime, string email, string oib) =>
        baza.Izvrsi(
            "INSERT INTO korisnici (id, username, password_hash, email, ime, prezime, uloga, oib) VALUES ($id, $u, $h, $e, $i, $p, $r, $o)",
            ("$id", id), ("$u", username), ("$h", lozinke.Hashiraj(lozinka)), ("$e", email), ("$i", ime), ("$p", prezime), ("$r", uloga), ("$o", oib));

    public void PostaviLozinku(SqliteConnection veza, SqliteTransaction? transakcija, long korisnikId, string novaLozinka) =>
        Baza.Izvrsi(veza, transakcija, "UPDATE korisnici SET password_hash = $h WHERE id = $id",
            ("$h", lozinke.Hashiraj(novaLozinka)), ("$id", korisnikId));

    private static Korisnik Mapiraj(Dictionary<string, object?> redak) => new(
        redak.Broj("id"), redak.Tekst("username"), redak.Tekst("password_hash"), redak.Tekst("email"),
        redak.Tekst("ime"), redak.Tekst("prezime"), redak.Tekst("uloga"));
}
