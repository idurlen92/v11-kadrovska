package hr.span.akademija.kadrovska.edukacije;

import hr.span.akademija.kadrovska.platforma.ApiException;
import hr.span.akademija.kadrovska.platforma.JdbcPomoc;
import hr.span.akademija.kadrovska.platforma.PrijavljeniKorisnik;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.List;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;
import org.springframework.stereotype.Service;

@Service
public class EdukacijaService {

    private static final Logger log = LoggerFactory.getLogger(EdukacijaService.class);

    public record Edukacija(long id, String naziv, LocalDate datum, int kapacitet, int prijavljeno, int slobodno) {
    }

    private static final RowMapper<Edukacija> MAPPER = (rs, i) -> new Edukacija(rs.getLong("id"),
            rs.getString("naziv"), rs.getObject("datum", LocalDate.class), rs.getInt("kapacitet"),
            rs.getInt("prijavljeno"), rs.getInt("kapacitet") - rs.getInt("prijavljeno"));

    private final JdbcTemplate jdbc;
    private final SustavZaEdukacijeKlijent lms;

    public EdukacijaService(JdbcTemplate jdbc, SustavZaEdukacijeKlijent lms) {
        this.jdbc = jdbc;
        this.lms = lms;
    }

    public List<Edukacija> sve() {
        return jdbc.query("SELECT id, naziv, datum, kapacitet, prijavljeno FROM edukacije ORDER BY datum, id", MAPPER);
    }

    public long prijavi(long edukacijaId, PrijavljeniKorisnik korisnik) {
        Integer postoji = jdbc.queryForObject("SELECT COUNT(*) FROM edukacije WHERE id = ?", Integer.class,
                edukacijaId);
        if (postoji == null || postoji == 0) {
            throw ApiException.nijePronadeno("Edukacija nije pronađena.");
        }
        Integer vecPrijavljen = jdbc.queryForObject(
                "SELECT COUNT(*) FROM prijave_edukacije WHERE edukacija_id = ? AND zaposlenik = ?", Integer.class,
                edukacijaId, korisnik.username());
        if (vecPrijavljen != null && vecPrijavljen > 0) {
            throw new ApiException(HttpStatus.CONFLICT, "Već ste prijavljeni na ovu edukaciju.");
        }
        Map<String, Object> stanje = jdbc.queryForMap(
                "SELECT kapacitet, prijavljeno FROM edukacije WHERE id = ?", edukacijaId);
        int kapacitet = ((Number) stanje.get("kapacitet")).intValue();
        int prijavljeno = ((Number) stanje.get("prijavljeno")).intValue();
        if (prijavljeno >= kapacitet) {
            throw new ApiException(HttpStatus.CONFLICT, "Nema slobodnih mjesta.");
        }
        String oznaka = lms.upisiPolaznika(edukacijaId, korisnik.username());
        jdbc.update("UPDATE edukacije SET prijavljeno = prijavljeno + 1 WHERE id = ?", edukacijaId);
        return spremi(edukacijaId, korisnik, oznaka);
    }

    private long spremi(long edukacijaId, PrijavljeniKorisnik korisnik, String oznaka) {
        long id = JdbcPomoc.umetni(jdbc,
                "INSERT INTO prijave_edukacije (edukacija_id, zaposlenik, kreirano, oznaka_lms) VALUES (?, ?, ?, ?)",
                edukacijaId, korisnik.username(), LocalDateTime.now(), oznaka);
        log.info("Zaposlenik {} prijavljen na edukaciju {}", korisnik.username(), edukacijaId);
        return id;
    }
}
