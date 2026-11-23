package hr.span.akademija.kadrovska.security;

import hr.span.akademija.kadrovska.ApiTestBaza;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;
import org.springframework.http.HttpStatus;

import static org.assertj.core.api.Assertions.assertThat;

@Tag("Security")
public class PlatnaListaSecurityTest extends ApiTestBaza {

    @Test
    void nepodrzaniFormatNeSadrziStackTrace(){
        String tokenAna = prijava("ana", "Ana12345!");

        Odgovor odgovor = get("/api/platne-liste/1?format=yuea", tokenAna);
        assertThat(odgovor.status()).isEqualTo(HttpStatus.BAD_REQUEST.value());
        assertThat(odgovor.json().toString()).doesNotContain("Exception");
    }

}
