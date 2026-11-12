package hr.span.akademija.kadrovska.zahtjevi;

import hr.span.akademija.kadrovska.platforma.ApiException;
import hr.span.akademija.kadrovska.platforma.AppProperties;
import hr.span.akademija.kadrovska.platforma.Html;
import hr.span.akademija.kadrovska.platforma.PrijavljeniKorisnik;
import hr.span.akademija.kadrovska.zahtjevi.ZahtjevService.Zahtjev;
import java.time.format.DateTimeFormatter;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.util.HtmlUtils;

/** Potvrda zahtjeva za godišnji odmor za ispis (HTML). */
@Controller
public class IspisController {

    private static final DateTimeFormatter DATUM = DateTimeFormatter.ofPattern("dd.MM.yyyy.");

    private final ZahtjevService zahtjevi;
    private final AppProperties postavke;

    public IspisController(ZahtjevService zahtjevi, AppProperties postavke) {
        this.zahtjevi = zahtjevi;
        this.postavke = postavke;
    }

    @GetMapping("/ispis/zahtjevi/{id}")
    public ResponseEntity<String> potvrda(@PathVariable long id, PrijavljeniKorisnik korisnik) {
        Zahtjev z = zahtjevi.pronadi(id)
                .filter(x -> korisnik.isAdmin() || x.zaposlenik().equals(korisnik.username()))
                .orElseThrow(() -> ApiException.nijePronadeno("Zahtjev nije pronađen."));
        String html = """
                <!DOCTYPE html>
                <html lang="hr">
                <head>
                <meta charset="UTF-8">
                <title>Zahtjev za godišnji odmor br. %d</title>
                <style>
                body { font-family: system-ui, sans-serif; max-width: 40rem; margin: 2rem auto; }
                dt { font-weight: bold; margin-top: .5rem; }
                </style>
                </head>
                <body>
                <h1>%s</h1>
                <h2>Zahtjev za godišnji odmor br. %d</h2>
                <dl>
                <dt>Zaposlenik</dt><dd>%s</dd>
                <dt>Razdoblje</dt><dd>%s – %s</dd>
                <dt>Razlog</dt><dd>%s</dd>
                <dt>Status</dt><dd>%s</dd>
                </dl>
                <p>Potpis zaposlenika: ____________________</p>
                <p>Odobrio (voditelj): ____________________</p>
                </body>
                </html>
                """.formatted(z.id(), tekst(postavke.naziv()), z.id(), tekst(z.zaposlenik()),
                z.od() == null ? "" : DATUM.format(z.od()), z.doDatuma() == null ? "" : DATUM.format(z.doDatuma()),
                tekst(z.razlog()), tekst(z.status()));
        return ResponseEntity.ok()
                .contentType(Html.TEXT_HTML_UTF8)
                .header("Content-Security-Policy", "default-src 'none'; style-src 'unsafe-inline'")
                .body(html);
    }

    private static String tekst(String vrijednost) {
        return vrijednost == null ? "" : HtmlUtils.htmlEscape(vrijednost, "UTF-8");
    }
}
