package hr.span.akademija.kadrovska.zahtjevi;

import com.fasterxml.jackson.annotation.JsonProperty;
import hr.span.akademija.kadrovska.platforma.ApiException;
import hr.span.akademija.kadrovska.platforma.JdbcPomoc;
import hr.span.akademija.kadrovska.platforma.PrijavljeniKorisnik;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;
import java.util.regex.Pattern;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;
import org.springframework.stereotype.Service;

/** Zahtjevi za godišnji odmor. */
@Service
public class ZahtjevService {

    private static final Logger log = LoggerFactory.getLogger(ZahtjevService.class);
    private static final Pattern OIB = Pattern.compile("\\d{11}");

    public record NoviZahtjev(LocalDate od, @JsonProperty("do") LocalDate doDatuma, String razlog, String oib) {
    }

    public record Zahtjev(long id, String zaposlenik, LocalDate od, @JsonProperty("do") LocalDate doDatuma,
                          String razlog, String status, LocalDateTime kreirano) {
    }

    private static final RowMapper<Zahtjev> MAPPER = (rs, i) -> new Zahtjev(rs.getLong("id"),
            rs.getString("zaposlenik"), rs.getObject("datum_od", LocalDate.class),
            rs.getObject("datum_do", LocalDate.class), rs.getString("razlog"), rs.getString("status"),
            rs.getObject("kreirano", LocalDateTime.class));

    private static final String SELECT =
            "SELECT id, zaposlenik, datum_od, datum_do, razlog, status, kreirano FROM zahtjevi";

    private final JdbcTemplate jdbc;

    public ZahtjevService(JdbcTemplate jdbc) {
        this.jdbc = jdbc;
    }

    public Optional<Zahtjev> pronadi(long id) {
        return jdbc.query(SELECT + " WHERE id = ?", MAPPER, id).stream().findFirst();
    }

    public List<Zahtjev> zahtjeviZaposlenika(String username) {
        return jdbc.query(SELECT + " WHERE zaposlenik = ? ORDER BY datum_od, id", MAPPER, username);
    }

    public long kreiraj(NoviZahtjev zahtjev, PrijavljeniKorisnik korisnik) {
        if (zahtjev == null || zahtjev.od() == null || zahtjev.doDatuma() == null) {
            throw ApiException.neispravno("Razdoblje (od, do) je obavezno.");
        }
        if (zahtjev.doDatuma().isBefore(zahtjev.od())) {
            throw ApiException.neispravno("Datum završetka ne može biti prije datuma početka.");
        }
        if (zahtjev.razlog() == null || zahtjev.razlog().isBlank() || zahtjev.razlog().length() > 500) {
            throw ApiException.neispravno("Razlog je obavezan (najviše 500 znakova).");
        }
        if (zahtjev.oib() == null || !OIB.matcher(zahtjev.oib()).matches()) {
            throw ApiException.neispravno("OIB mora imati 11 znamenki.");
        }
        log.info("Novi zahtjev za godišnji odmor: {} (zaposlenik {})", zahtjev, korisnik.username());
        return JdbcPomoc.umetni(jdbc,
                "INSERT INTO zahtjevi (zaposlenik, datum_od, datum_do, razlog, oib, status, kreirano) "
                        + "VALUES (?, ?, ?, ?, ?, ?, ?)",
                korisnik.username(), zahtjev.od(), zahtjev.doDatuma(), zahtjev.razlog(), zahtjev.oib(), "PODNESEN",
                LocalDateTime.now());
    }

    public void obrisi(long id, PrijavljeniKorisnik korisnik) {
        Zahtjev zahtjev = pronadi(id)
                .orElseThrow(() -> ApiException.nijePronadeno("Zahtjev nije pronađen."));
        jdbc.update("DELETE FROM zahtjevi WHERE id = ?", zahtjev.id());
        log.info("Zahtjev {} je povučen (korisnik {})", id, korisnik.username());
    }
}
