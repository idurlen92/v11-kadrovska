package hr.span.akademija.kadrovska.edukacije;

import hr.span.akademija.kadrovska.edukacije.EdukacijaService.Edukacija;
import hr.span.akademija.kadrovska.platforma.ApiException;
import hr.span.akademija.kadrovska.platforma.JdbcPomoc;
import hr.span.akademija.kadrovska.platforma.PrijavljeniKorisnik;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/edukacije")
public class EdukacijaController {

    private static final Logger log = LoggerFactory.getLogger(EdukacijaController.class);

    private final EdukacijaService edukacije;
    private final UvozEdukacija uvoz;
    private final JdbcTemplate jdbc;

    public EdukacijaController(EdukacijaService edukacije, UvozEdukacija uvoz, JdbcTemplate jdbc) {
        this.edukacije = edukacije;
        this.uvoz = uvoz;
        this.jdbc = jdbc;
    }

    @GetMapping
    public List<Edukacija> popis(PrijavljeniKorisnik korisnik) {
        return edukacije.sve();
    }

    @PostMapping("/{id}/prijava")
    public ResponseEntity<Map<String, Object>> prijava(@PathVariable long id, PrijavljeniKorisnik korisnik) {
        long prijavaId = edukacije.prijavi(id, korisnik);
        Map<String, Object> tijelo = new LinkedHashMap<>();
        tijelo.put("id", prijavaId);
        tijelo.put("edukacijaId", id);
        tijelo.put("zaposlenik", korisnik.username());
        return ResponseEntity.status(HttpStatus.CREATED).body(tijelo);
    }

    /** Uvoz plana edukacija iz XML-a (voditelj ili administrator). */
    @PostMapping("/uvoz")
    @Transactional
    public Map<String, Object> uvezi(@RequestBody byte[] xml, PrijavljeniKorisnik korisnik) {
        if (!korisnik.imaUlogu("VODITELJ", "ADMIN")) {
            throw ApiException.zabranjeno();
        }
        List<UvozEdukacija.Stavka> stavke = uvoz.procitaj(xml);
        for (UvozEdukacija.Stavka s : stavke) {
            if (s.kapacitet() < 1 || s.kapacitet() > 500) {
                throw ApiException.neispravno("Kapacitet edukacije mora biti između 1 i 500.");
            }
        }
        List<Map<String, Object>> uvezene = new ArrayList<>();
        for (UvozEdukacija.Stavka s : stavke) {
            long id = JdbcPomoc.umetni(jdbc,
                    "INSERT INTO edukacije (naziv, datum, kapacitet, prijavljeno) VALUES (?, ?, ?, 0)",
                    s.naziv(), s.datum(), s.kapacitet());
            Map<String, Object> edukacija = new LinkedHashMap<>();
            edukacija.put("id", id);
            edukacija.put("naziv", s.naziv());
            uvezene.add(edukacija);
        }
        log.info("Korisnik {} uvezao je {} edukacija", korisnik.username(), uvezene.size());
        Map<String, Object> odgovor = new LinkedHashMap<>();
        odgovor.put("uvezeno", uvezene.size());
        odgovor.put("edukacije", uvezene);
        return odgovor;
    }
}
