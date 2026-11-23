package hr.span.akademija.kadrovska;

import hr.span.akademija.kadrovska.platforma.KorisnikService;
import hr.span.akademija.kadrovska.platforma.Pohrana;
import java.time.LocalDate;
import java.time.LocalDateTime;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.CommandLineRunner;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Component;

/**
 * Početni podaci (baza je u memoriji pa se puni pri svakom pokretanju). Lozinke se spremaju istom
 * metodom kao pri registraciji.
 */
@Component
public class PocetniPodaci implements CommandLineRunner {

    private static final Logger log = LoggerFactory.getLogger(PocetniPodaci.class);

    private final JdbcTemplate jdbc;
    private final KorisnikService korisnici;
    private final Pohrana pohrana;

    public PocetniPodaci(JdbcTemplate jdbc, KorisnikService korisnici, Pohrana pohrana) {
        this.jdbc = jdbc;
        this.korisnici = korisnici;
        this.pohrana = pohrana;
    }

    @Override
    public void run(String... args) throws Exception {
        pohrana.pripremi();
        zapisiDatoteke();
        Integer postojeci = jdbc.queryForObject("SELECT COUNT(*) FROM korisnici", Integer.class);
        if (postojeci != null && postojeci > 0) {
            return;
        }
        korisnici.kreiraj("ana", "Ana12345!", "ana@primjer.hr", "Ana", "Anić", "KORISNIK", "48291736504");
        korisnici.kreiraj("marko", "Marko12345!", "marko@primjer.hr", "Marko", "Marić", "KORISNIK", "73019485267");
        korisnici.kreiraj("voditelj", "Ovlasteni12345!", "voditelj@primjer.hr", "Tomislav", "Tomić", "VODITELJ",
                "50617283940");
        korisnici.kreiraj("admin", "Admin12345!", "admin@primjer.hr", "Ivana", "Admin", "ADMIN", "61728394051");

        zaposlenik("Ana", "Anić", "Financije", "ana@primjer.hr");
        zaposlenik("Marko", "Marić", "Prodaja", "marko@primjer.hr");
        zaposlenik("Tomislav", "Tomić", "Ljudski potencijali", "voditelj@primjer.hr");
        zaposlenik("Lucija", "Babić", "Informatika", "lucija.babic@primjer.hr");
        zaposlenik("Davor", "Jurić", "Informatika", "davor.juric@primjer.hr");
        zaposlenik("Sanja", "Horvat", "Pravna služba", "sanja.horvat@primjer.hr");
        zaposlenik("Ivan", "Zorić", "Prodaja", "ivan.zoric@primjer.hr");
        zaposlenik("Maja", "Vidović", "Financije", "maja.vidovic@primjer.hr");

        zahtjev("ana", "2026-12-21", "2026-12-31", "Obiteljsko putovanje", "48291736504");
        zahtjev("marko", "2027-01-05", "2027-01-09", "Selidba", "73019485267");

        platnaLista("ana", "2026-10", "placa-ana-2026-10.txt");
        platnaLista("marko", "2026-10", "placa-marko-2026-10.txt");

        edukacija("Uvod u GDPR", "2026-12-02", 30, 5);
        edukacija("Excel za napredne", "2026-12-04", 15, 3);
        edukacija("Sigurnost na radu – zadnje mjesto", "2026-12-09", 20, 19);
        edukacija("Komunikacijske vještine", "2026-12-11", 25, 0);
        edukacija("Prva pomoć", "2027-01-14", 12, 2);
        log.info("Početni podaci su učitani");
    }

    private void zapisiDatoteke() throws Exception {
        pohrana.zapisiAkoNePostoji("placa-ana-2026-10.txt", """
                Kadrovska d.o.o. – platna lista za razdoblje 2026-10
                Zaposlenik: Ana Anić (Financije)
                Bruto plaća: 1.850,00 EUR
                Doprinosi za mirovinsko osiguranje: 370,00 EUR
                Porez na dohodak: 152,40 EUR
                Neto isplata: 1.327,60 EUR
                Isplata na račun: HR12 2340 0091 1100 0000 1
                """);
        pohrana.zapisiAkoNePostoji("placa-marko-2026-10.txt", """
                Kadrovska d.o.o. – platna lista za razdoblje 2026-10
                Zaposlenik: Marko Marić (Prodaja)
                Bruto plaća: 1.640,00 EUR
                Doprinosi za mirovinsko osiguranje: 328,00 EUR
                Porez na dohodak: 121,80 EUR
                Neto isplata: 1.190,20 EUR
                Isplata na račun: HR45 2484 0081 1200 0000 2
                """);
    }

    private void zaposlenik(String ime, String prezime, String odjel, String email) {
        jdbc.update("INSERT INTO zaposlenici (ime, prezime, odjel, email) VALUES (?, ?, ?, ?)",
                ime, prezime, odjel, email);
    }

    private void zahtjev(String zaposlenik, String od, String doDatuma, String razlog, String oib) {
        jdbc.update("INSERT INTO zahtjevi (zaposlenik, datum_od, datum_do, razlog, oib, status, kreirano) "
                        + "VALUES (?, ?, ?, ?, ?, ?, ?)", zaposlenik, LocalDate.parse(od), LocalDate.parse(doDatuma),
                razlog, oib, "PODNESEN", LocalDateTime.now());
    }

    private void platnaLista(String zaposlenik, String razdoblje, String datoteka) {
        jdbc.update("INSERT INTO platne_liste (zaposlenik, razdoblje, datoteka) VALUES (?, ?, ?)",
                zaposlenik, razdoblje, datoteka);
    }

    private void edukacija(String naziv, String datum, int kapacitet, int prijavljeno) {
        jdbc.update("INSERT INTO edukacije (naziv, datum, kapacitet, prijavljeno) VALUES (?, ?, ?, ?)",
                naziv, LocalDate.parse(datum), kapacitet, prijavljeno);
    }
}
