package hr.span.akademija.kadrovska.platneliste;

import hr.span.akademija.kadrovska.platforma.ApiException;
import hr.span.akademija.kadrovska.platforma.Pohrana;
import hr.span.akademija.kadrovska.platforma.PrijavljeniKorisnik;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.NoSuchFileException;
import java.nio.file.Path;
import java.util.Arrays;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Optional;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ContentDisposition;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/platne-liste")
public class PlatnaListaController {

    private static final Logger log = LoggerFactory.getLogger(PlatnaListaController.class);
    private static final MediaType TEXT_PLAIN_UTF8 = new MediaType("text", "plain", StandardCharsets.UTF_8);

    public record PlatnaLista(long id, String zaposlenik, String razdoblje, String datoteka) {
    }

    public enum Format {
        JSON, PDF;

        static Optional<Format> procitaj(String vrijednost) {
            return Arrays.stream(values()).filter(f -> f.name().equalsIgnoreCase(vrijednost.trim())).findFirst();
        }
    }

    private static final RowMapper<PlatnaLista> MAPPER = (rs, i) -> new PlatnaLista(rs.getLong("id"),
            rs.getString("zaposlenik"), rs.getString("razdoblje"), rs.getString("datoteka"));

    private final JdbcTemplate jdbc;
    private final Pohrana pohrana;

    public PlatnaListaController(JdbcTemplate jdbc, Pohrana pohrana) {
        this.jdbc = jdbc;
        this.pohrana = pohrana;
    }

    @GetMapping("/moje")
    public List<PlatnaLista> moje(PrijavljeniKorisnik korisnik) {
        return jdbc.query("SELECT id, zaposlenik, razdoblje, datoteka FROM platne_liste WHERE zaposlenik = ? "
                + "ORDER BY razdoblje DESC, id", MAPPER, korisnik.username());
    }

    /** Platna lista u JSON obliku (podaci i sadržaj) ili kao PDF: {@code GET /api/platne-liste/1?format=pdf}. */
    @GetMapping("/{id}")
    public ResponseEntity<?> platnaLista(@PathVariable long id, @RequestParam(required = false) String format,
                                   PrijavljeniKorisnik korisnik) throws IOException {
        String trazeni = format == null ? "JSON" : format.trim().toUpperCase(Locale.ROOT);
        boolean validanFormat = Arrays.stream(Format.values()).anyMatch(f -> f.toString().equals(trazeni));
        if(!validanFormat){
            throw ApiException.neispravno("Neispravan format");
        }

        PlatnaLista lista = pronadi(id).orElseThrow(() -> ApiException.nijePronadeno("Platna lista nije pronađena."));
        Format odabrani = Format.valueOf(trazeni);
        String napomena = null;
        if (!korisnik.isAdmin() && !lista.zaposlenik().equals(korisnik.username())) {
            throw ApiException.nijePronadeno("Platna lista nije pronađena.");
        }
        return prikazi(lista, odabrani, napomena);
    }

    /** Preuzimanje datoteke platne liste: {@code GET /api/platne-liste/preuzmi?datoteka=placa-ana-2026-10.txt}. */
    @GetMapping("/preuzmi")
    public ResponseEntity<byte[]> preuzmi(@RequestParam String datoteka, PrijavljeniKorisnik korisnik)
            throws IOException {
        PlatnaLista lista = jdbc.query(
                        "SELECT id, zaposlenik, razdoblje, datoteka FROM platne_liste WHERE datoteka = ?",
                        MAPPER, datoteka).stream()
                .filter(p -> korisnik.isAdmin() || p.zaposlenik().equals(korisnik.username()))
                .findFirst()
                .orElseThrow(() -> ApiException.nijePronadeno("Datoteka nije pronađena."));
        Path korijen = pohrana.dokumenti().toAbsolutePath().normalize();
        Path putanja = korijen.resolve(lista.datoteka()).normalize();
        if (!putanja.startsWith(korijen)) {
            throw ApiException.neispravno("Neispravan naziv datoteke.");
        }
        if (!Files.isRegularFile(putanja)) {
            throw ApiException.nijePronadeno("Datoteka nije pronađena.");
        }
        byte[] sadrzaj = Files.readAllBytes(putanja);
        ContentDisposition privitak = ContentDisposition.attachment()
                .filename(putanja.getFileName().toString(), StandardCharsets.UTF_8)
                .build();
        return ResponseEntity.ok()
                .contentType(TEXT_PLAIN_UTF8)
                .header(HttpHeaders.CONTENT_DISPOSITION, privitak.toString())
                .body(sadrzaj);
    }

    private Optional<PlatnaLista> pronadi(long id) {
        return jdbc.query("SELECT id, zaposlenik, razdoblje, datoteka FROM platne_liste WHERE id = ?", MAPPER, id)
                .stream().findFirst();
    }

    private ResponseEntity<?> prikazi(PlatnaLista lista, Format format, String napomena) throws IOException {
        String sadrzaj;
        try {
            sadrzaj = Files.readString(pohrana.dokumenti().resolve(lista.datoteka()), StandardCharsets.UTF_8);
        } catch (NoSuchFileException e) {
            throw ApiException.nijePronadeno("Datoteka platne liste nije pronađena.");
        }
        if (format == Format.PDF) {
            byte[] pdf = PdfDokument.izradi("Platna lista " + lista.razdoblje(), sadrzaj.lines().toList());
            return ResponseEntity.ok()
                    .contentType(MediaType.APPLICATION_PDF)
                    .header(HttpHeaders.CONTENT_DISPOSITION, "inline; filename=\"platna-lista-" + lista.id() + ".pdf\"")
                    .body(pdf);
        }
        Map<String, Object> tijelo = new LinkedHashMap<>();
        tijelo.put("id", lista.id());
        tijelo.put("zaposlenik", lista.zaposlenik());
        tijelo.put("razdoblje", lista.razdoblje());
        tijelo.put("datoteka", lista.datoteka());
        tijelo.put("sadrzaj", sadrzaj);
        if (napomena != null) {
            tijelo.put("napomena", napomena);
        }
        return ResponseEntity.ok(tijelo);
    }
}
