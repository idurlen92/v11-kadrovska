namespace Kadrovska.Api.Infrastruktura;

/// <summary>Shema i početni podaci HR portala: zaposlenici, zahtjevi za godišnji, platne liste i edukacije.</summary>
public static class PocetniPodaci
{
    public const string Shema = """
        CREATE TABLE zaposlenici (
            id      INTEGER PRIMARY KEY,
            ime     TEXT NOT NULL,
            prezime TEXT NOT NULL,
            odjel   TEXT NOT NULL,
            email   TEXT NOT NULL
        );

        CREATE TABLE zahtjevi (
            id         INTEGER PRIMARY KEY AUTOINCREMENT,
            zaposlenik TEXT NOT NULL,
            od         TEXT NOT NULL,
            "do"       TEXT NOT NULL,
            razlog     TEXT NOT NULL,
            oib        TEXT NOT NULL,
            status     TEXT NOT NULL,
            kreirano   TEXT NOT NULL
        );

        CREATE TABLE platne_liste (
            id         INTEGER PRIMARY KEY AUTOINCREMENT,
            zaposlenik TEXT NOT NULL,
            razdoblje  TEXT NOT NULL,
            datoteka   TEXT NOT NULL
        );

        CREATE TABLE edukacije (
            id          INTEGER PRIMARY KEY AUTOINCREMENT,
            naziv       TEXT NOT NULL,
            datum       TEXT NOT NULL,
            kapacitet   INTEGER NOT NULL,
            prijavljeno INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE prijave_edukacije (
            id           INTEGER PRIMARY KEY AUTOINCREMENT,
            edukacija_id INTEGER NOT NULL REFERENCES edukacije(id),
            zaposlenik   TEXT NOT NULL,
            kreirano     TEXT NOT NULL,
            UNIQUE (edukacija_id, zaposlenik)
        );
        """;

    public static void Unesi(Baza baza)
    {
        const string zaposlenik = "INSERT INTO zaposlenici (id, ime, prezime, odjel, email) VALUES ($id, $i, $p, $o, $e)";
        baza.Izvrsi(zaposlenik, ("$id", 1), ("$i", "Ana"), ("$p", "Anić"), ("$o", "Prodaja"), ("$e", "ana@primjer.hr"));
        baza.Izvrsi(zaposlenik, ("$id", 2), ("$i", "Marko"), ("$p", "Marić"), ("$o", "Razvoj"), ("$e", "marko@primjer.hr"));
        baza.Izvrsi(zaposlenik, ("$id", 3), ("$i", "Zoran"), ("$p", "Vidović"), ("$o", "Razvoj"), ("$e", "voditelj@primjer.hr"));
        baza.Izvrsi(zaposlenik, ("$id", 4), ("$i", "Ivana"), ("$p", "Admin"), ("$o", "Uprava"), ("$e", "admin@primjer.hr"));
        baza.Izvrsi(zaposlenik, ("$id", 5), ("$i", "Lana"), ("$p", "Babić"), ("$o", "Financije"), ("$e", "lana.babic@primjer.hr"));
        baza.Izvrsi(zaposlenik, ("$id", 6), ("$i", "Goran"), ("$p", "Kovač"), ("$o", "Prodaja"), ("$e", "goran.kovac@primjer.hr"));
        baza.Izvrsi(zaposlenik, ("$id", 7), ("$i", "Petra"), ("$p", "Jurić"), ("$o", "Ljudski potencijali"), ("$e", "petra.juric@primjer.hr"));
        baza.Izvrsi(zaposlenik, ("$id", 8), ("$i", "Tin"), ("$p", "Novak"), ("$o", "Podrška"), ("$e", "tin.novak@primjer.hr"));

        const string zahtjev = """
            INSERT INTO zahtjevi (id, zaposlenik, od, "do", razlog, oib, status, kreirano)
            VALUES ($id, $z, $od, $do, $r, $o, $s, $k)
            """;
        baza.Izvrsi(zahtjev, ("$id", 1), ("$z", "ana"), ("$od", "2026-12-21"), ("$do", "2026-12-31"), ("$r", "Obiteljsko putovanje"),
            ("$o", "48291736504"), ("$s", "ODOBREN"), ("$k", "2026-10-12T10:00:00Z"));
        baza.Izvrsi(zahtjev, ("$id", 2), ("$z", "marko"), ("$od", "2027-01-04"), ("$do", "2027-01-08"), ("$r", "Odmor nakon projekta"),
            ("$o", "73019485267"), ("$s", "NA_CEKANJU"), ("$k", "2026-10-28T14:30:00Z"));

        const string platnaLista = "INSERT INTO platne_liste (id, zaposlenik, razdoblje, datoteka) VALUES ($id, $z, $r, $d)";
        baza.Izvrsi(platnaLista, ("$id", 1), ("$z", "ana"), ("$r", "2026-10"), ("$d", "placa-ana-2026-10.txt"));
        baza.Izvrsi(platnaLista, ("$id", 2), ("$z", "marko"), ("$r", "2026-10"), ("$d", "placa-marko-2026-10.txt"));

        const string edukacija = "INSERT INTO edukacije (id, naziv, datum, kapacitet, prijavljeno) VALUES ($id, $n, $d, $k, $p)";
        baza.Izvrsi(edukacija, ("$id", 1), ("$n", "Uvod u Git"), ("$d", "2026-11-20"), ("$k", 25), ("$p", 10));
        baza.Izvrsi(edukacija, ("$id", 2), ("$n", "Napredni Excel"), ("$d", "2026-11-25"), ("$k", 15), ("$p", 15));
        baza.Izvrsi(edukacija, ("$id", 3), ("$n", "Sigurnost na radu – zadnje mjesto"), ("$d", "2026-12-02"), ("$k", 20), ("$p", 19));
        baza.Izvrsi(edukacija, ("$id", 4), ("$n", "Komunikacijske vještine"), ("$d", "2026-12-09"), ("$k", 30), ("$p", 5));
        baza.Izvrsi(edukacija, ("$id", 5), ("$n", "Prva pomoć"), ("$d", "2026-12-16"), ("$k", 12), ("$p", 0));
    }

    /// <summary>Datoteke platnih lista koje se pri pokretanju stvaraju u mapi s dokumentima.</summary>
    public static readonly IReadOnlyDictionary<string, string> Dokumenti = new Dictionary<string, string>
    {
        ["placa-ana-2026-10.txt"] = """
            KADROVSKA d.o.o. – platna lista
            Razdoblje: 2026-10
            Zaposlenik: Ana Anić, Prodaja

            Bruto plaća: 2100.00 EUR
            Doprinosi iz plaće: 420.00 EUR
            Porez i prirez: 168.00 EUR
            Neto plaća: 1512.00 EUR
            Isplata na račun: HR12 2360 0001 1012 3456 7
            """,
        ["placa-marko-2026-10.txt"] = """
            KADROVSKA d.o.o. – platna lista
            Razdoblje: 2026-10
            Zaposlenik: Marko Marić, Razvoj

            Bruto plaća: 2600.00 EUR
            Doprinosi iz plaće: 520.00 EUR
            Porez i prirez: 249.60 EUR
            Neto plaća: 1830.40 EUR
            Isplata na račun: HR45 2484 0081 1098 7654 3
            """,
    };
}
