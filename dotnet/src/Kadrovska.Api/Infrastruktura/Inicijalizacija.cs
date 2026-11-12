using System.Text;
using Kadrovska.Api.Autentikacija;

namespace Kadrovska.Api.Infrastruktura;

/// <summary>
/// Priprema okruženja pri pokretanju: mape za dokumente i poštu, shema baze, početni korisnici,
/// podaci i dokumenti. Baza je u memoriji, pa svako pokretanje kreće od istog stanja.
/// </summary>
public static class Inicijalizacija
{
    public static void Pokreni(IServiceProvider services)
    {
        var postavke = services.GetRequiredService<PostavkeAplikacije>();
        var baza = services.GetRequiredService<Baza>();
        var korisnici = services.GetRequiredService<KorisniciServis>();

        Directory.CreateDirectory(postavke.DirektorijDokumenata);
        Directory.CreateDirectory(postavke.DirektorijPoste);

        baza.Izvrsi(ZajednickaShema.Sql);
        baza.Izvrsi(PocetniPodaci.Shema);

        korisnici.DodajPocetnogKorisnika(1, "ana", "Ana12345!", Uloge.Korisnik, "Ana", "Anić", "ana@primjer.hr", "48291736504");
        korisnici.DodajPocetnogKorisnika(2, "marko", "Marko12345!", Uloge.Korisnik, "Marko", "Marić", "marko@primjer.hr", "73019485267");
        korisnici.DodajPocetnogKorisnika(3, Aplikacija.PovlasteniKorisnik, "Ovlasteni12345!", Aplikacija.PovlastenaUloga,
            Aplikacija.PovlasteniIme, Aplikacija.PovlasteniPrezime, $"{Aplikacija.PovlasteniKorisnik}@primjer.hr", "50617283940");
        korisnici.DodajPocetnogKorisnika(4, "admin", "Admin12345!", Uloge.Admin, "Ivana", "Admin", "admin@primjer.hr", "61728394051");

        PocetniPodaci.Unesi(baza);

        foreach (var (naziv, sadrzaj) in PocetniPodaci.Dokumenti)
        {
            var putanja = Path.Combine(postavke.DirektorijDokumenata, naziv);
            if (!File.Exists(putanja))
            {
                File.WriteAllText(putanja, sadrzaj, new UTF8Encoding(false));
            }
        }
    }
}
