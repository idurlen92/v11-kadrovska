package hr.span.akademija.kadrovska.security;

import hr.span.akademija.kadrovska.ApiTestBaza;
import hr.span.akademija.kadrovska.zahtjevi.ZahtjevService;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;

import java.util.Map;
import java.util.Optional;

import static org.assertj.core.api.AssertionsForClassTypes.assertThat;

@Tag("Security")
public class ZahtjeviControllerSecurityTest extends ApiTestBaza {

    @Autowired
    ZahtjevService zahtjevService;

    @Test
    public void brisanjeTudjegZahtjevaNijeMoguce(){
        String tokenAna = prijava("ana", "Ana12345!");
        long idZahtjevaAna = podnesi(tokenAna, "2026-12-01", "2026-12-02", "Edukacija", "12345678901").json().get("id").asLong();

        String tokenMarko = prijava("marko", "Marko12345!");
        Odgovor odgovor = delete("/api/zahtjevi/" + idZahtjevaAna, tokenMarko);

        assertThat(odgovor.status()).isEqualTo(HttpStatus.FORBIDDEN.value());
        Optional<ZahtjevService.Zahtjev> aninZahtjev = zahtjevService.pronadi(idZahtjevaAna);
        assertThat(aninZahtjev.isPresent()).isTrue();
    }

    private Odgovor podnesi(String token, String od, String doDatuma, String razlog, String oib) {
        return postJson("/api/zahtjevi", token, Map.of("od", od, "do", doDatuma, "razlog", razlog, "oib", oib));
    }

}
