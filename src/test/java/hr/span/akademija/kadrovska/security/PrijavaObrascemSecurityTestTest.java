package hr.span.akademija.kadrovska.security;

import hr.span.akademija.kadrovska.ApiTestBaza;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;
import org.springframework.http.HttpStatus;

import java.util.Map;

import static org.assertj.core.api.Assertions.assertThat;

@Tag("Security")
public class PrijavaObrascemSecurityTestTest extends ApiTestBaza {

    @Test
    public void prijavaNeVodiNaBiloKojuStranicuVanDomene(){
        Odgovor odgovor = postForma("/prijava",
                Map.of("username", "ana", "password", "Ana12345!", "returnUrl", "https://www.google.com"));
        assertThat(odgovor.status()).isEqualTo(HttpStatus.BAD_REQUEST.value());
        assertThat(odgovor.zaglavlja().firstValue("location")).isNotEqualTo("https://www.google.com");
    }


    @Test
    public void prijavaNeRedirectaNaNepodrzanuShemu(){
        Odgovor odgovor = postForma("/prijava",
                Map.of("username", "ana", "password", "Ana12345!", "returnUrl", "file:///etc/passwd"));
        assertThat(odgovor.status()).isEqualTo(HttpStatus.BAD_REQUEST.value());
        assertThat(odgovor.zaglavlja().firstValue("location")).isNotEqualTo("file:///etc/passwd");
    }


    @Test
    public void prijavaNeRedirectaNaLoopback(){
        Odgovor odgovor = postForma("/prijava",
                Map.of("username", "ana", "password", "Ana12345!", "returnUrl", "http://127.0.0.1:8080/reset"));
        assertThat(odgovor.status()).isEqualTo(HttpStatus.BAD_REQUEST.value());
        assertThat(odgovor.zaglavlja().firstValue("location")).isNotEqualTo("http://127.0.0.1:8080/reset");
    }




}
