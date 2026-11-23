package hr.span.akademija.kadrovska.funkcionalno;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.databind.JsonNode;
import hr.span.akademija.kadrovska.ApiTestBaza;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

@Tag("funkcionalno")
class UvozEdukacijaFunkcionalniTest extends ApiTestBaza {

    private static final String XML = """
            <?xml version="1.0" encoding="UTF-8"?>
            <edukacije>
              <edukacija>
                <naziv>Excel za početnike</naziv>
                <datum>2026-12-03</datum>
                <kapacitet>15</kapacitet>
              </edukacija>
            </edukacije>
            """;

    @Test
    @DisplayName("Voditelj uvozi edukacije iz XML-a")
    void voditeljUvozi() {
        String token = prijava("voditelj", "Ovlasteni12345!");

        Odgovor odgovor = posalji("POST", "/api/edukacije/uvoz", token, "application/xml", XML);

        assertThat(odgovor.status()).isEqualTo(200);
        JsonNode tijelo = odgovor.json();
        assertThat(tijelo.get("uvezeno").asInt()).isEqualTo(1);
        assertThat(tijelo.get("edukacije").get(0).get("naziv").asText()).isEqualTo("Excel za početnike");
        long id = tijelo.get("edukacije").get(0).get("id").asLong();
        boolean vidljiva = false;
        for (JsonNode e : get("/api/edukacije", token).json()) {
            vidljiva |= e.get("id").asLong() == id;
        }
        assertThat(vidljiva).isTrue();
    }

    @Test
    @DisplayName("Administrator može uvoziti edukacije")
    void administratorUvozi() {
        String token = prijava("admin", "Admin12345!");

        assertThat(posalji("POST", "/api/edukacije/uvoz", token, "application/xml", XML).status()).isEqualTo(200);
    }

    @Test
    @DisplayName("Korisnik s ulogom KORISNIK ne može uvoziti edukacije")
    void korisnikNeMozeUvoziti() {
        String token = prijava("ana", "Ana12345!");

        assertThat(posalji("POST", "/api/edukacije/uvoz", token, "application/xml", XML).status()).isEqualTo(403);
    }

    @Test
    @DisplayName("Neispravan XML vraća 400")
    void neispravanXml() {
        String token = prijava("voditelj", "Ovlasteni12345!");

        Odgovor odgovor = posalji("POST", "/api/edukacije/uvoz", token, "application/xml", "<edukacije><edukacija>");

        assertThat(odgovor.status()).isEqualTo(400);
    }
}
