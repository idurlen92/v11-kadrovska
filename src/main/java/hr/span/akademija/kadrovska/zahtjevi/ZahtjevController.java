package hr.span.akademija.kadrovska.zahtjevi;

import hr.span.akademija.kadrovska.platforma.ApiException;
import hr.span.akademija.kadrovska.platforma.PrijavljeniKorisnik;
import hr.span.akademija.kadrovska.zahtjevi.ZahtjevService.NoviZahtjev;
import hr.span.akademija.kadrovska.zahtjevi.ZahtjevService.Zahtjev;
import java.util.List;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/zahtjevi")
public class ZahtjevController {

    private final ZahtjevService zahtjevi;

    public ZahtjevController(ZahtjevService zahtjevi) {
        this.zahtjevi = zahtjevi;
    }

    @PostMapping
    public ResponseEntity<Zahtjev> kreiraj(@RequestBody NoviZahtjev zahtjev, PrijavljeniKorisnik korisnik) {
        long id = zahtjevi.kreiraj(zahtjev, korisnik);
        Zahtjev novi = zahtjevi.pronadi(id).orElseThrow(() -> ApiException.nijePronadeno("Zahtjev nije pronađen."));
        return ResponseEntity.status(HttpStatus.CREATED).body(novi);
    }

    @GetMapping("/moji")
    public List<Zahtjev> moji(PrijavljeniKorisnik korisnik) {
        return zahtjevi.zahtjeviZaposlenika(korisnik.username());
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<Void> obrisi(@PathVariable long id, PrijavljeniKorisnik korisnik) {
        zahtjevi.obrisi(id, korisnik);
        return ResponseEntity.noContent().build();
    }
}
