namespace Kadrovska.Api.Autentikacija;

/// <summary>Korisnik prijavljen u trenutnom zahtjevu.</summary>
public sealed record PrijavljeniKorisnik(long Id, string Username, string Uloga)
{
    public bool JeAdmin => Uloga == Uloge.Admin;
}

public static class Uloge
{
    public const string Korisnik = "KORISNIK";
    public const string Admin = "ADMIN";
}

/// <summary>Trenutni korisnik u <see cref="HttpContext"/> (postavlja ga <see cref="SesijaMiddleware"/>).</summary>
public static class HttpContextKorisnikExtensions
{
    private const string Kljuc = "Autentikacija.Korisnik";

    public static PrijavljeniKorisnik? TrenutniKorisnik(this HttpContext context) =>
        context.Items.TryGetValue(Kljuc, out var vrijednost) ? vrijednost as PrijavljeniKorisnik : null;

    /// <summary>Za akcije označene s <see cref="PotrebnaPrijavaAttribute"/> korisnik sigurno postoji.</summary>
    public static PrijavljeniKorisnik Korisnik(this HttpContext context) =>
        context.TrenutniKorisnik() ?? throw new InvalidOperationException("Korisnik nije prijavljen.");

    internal static void PostaviKorisnika(this HttpContext context, PrijavljeniKorisnik korisnik) =>
        context.Items[Kljuc] = korisnik;
}
