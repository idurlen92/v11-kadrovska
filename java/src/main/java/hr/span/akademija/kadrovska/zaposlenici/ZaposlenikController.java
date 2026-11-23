package hr.span.akademija.kadrovska.zaposlenici;

import hr.span.akademija.kadrovska.platforma.ApiException;
import hr.span.akademija.kadrovska.platforma.PrijavljeniKorisnik;

import java.util.Arrays;
import java.util.List;
import java.util.Locale;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

/** Imenik zaposlenika. */
@RestController
@RequestMapping("/api/zaposlenici")
public class ZaposlenikController {

    public record Zaposlenik(long id, String ime, String prezime, String odjel, String email) {
    }

    private static final RowMapper<Zaposlenik> MAPPER = (rs, i) -> new Zaposlenik(rs.getLong("id"),
            rs.getString("ime"), rs.getString("prezime"), rs.getString("odjel"), rs.getString("email"));


    private final JdbcTemplate jdbc;

    public ZaposlenikController(JdbcTemplate jdbc) {
        this.jdbc = jdbc;
    }

    /** Popis zaposlenika, npr. {@code GET /api/zaposlenici?sort=prezime&smjer=asc}. */
    @GetMapping
    public List<Zaposlenik> popis(@RequestParam(defaultValue = "prezime") String sort,
                                  @RequestParam(defaultValue = "asc") String smjer,
                                  PrijavljeniKorisnik korisnik) {
        if( !Arrays.asList("id", "ime", "prezime", "odjel", "email").contains(sort.toLowerCase(Locale.ROOT))){
            throw ApiException.neispravno("Neispravan zahtjev");
        }
        if( !Arrays.asList("asc", "desc").contains(smjer.toLowerCase(Locale.ROOT))){
            throw ApiException.neispravno("Neispravan zahtjev");
        }
        String sql = "SELECT id, ime, prezime, odjel, email FROM zaposlenici ORDER BY " + sort + " " + smjer;
        return jdbc.query(sql, MAPPER);
    }
}
