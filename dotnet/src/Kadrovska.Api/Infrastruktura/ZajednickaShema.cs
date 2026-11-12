namespace Kadrovska.Api.Infrastruktura;

/// <summary>Tablice zajedničke svim modulima: korisnici, obnova lozinke i postavke obavijesti.</summary>
public static class ZajednickaShema
{
    public const string Sql = """
        CREATE TABLE korisnici (
            id            INTEGER PRIMARY KEY AUTOINCREMENT,
            username      TEXT NOT NULL UNIQUE,
            password_hash TEXT NOT NULL,
            email         TEXT NOT NULL,
            ime           TEXT NOT NULL,
            prezime       TEXT NOT NULL,
            uloga         TEXT NOT NULL,
            oib           TEXT
        );

        CREATE TABLE reset_tokeni (
            token       TEXT NOT NULL,
            korisnik_id INTEGER NOT NULL,
            kreirano    TEXT NOT NULL
        );

        CREATE TABLE postavke_obavijesti (
            korisnik_id INTEGER PRIMARY KEY,
            podsjetnik  INTEGER NOT NULL,
            sati_prije  INTEGER NOT NULL,
            kanal       TEXT NOT NULL
        );
        """;
}
