package hr.span.akademija.kadrovska.funkcionalno;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.databind.JsonNode;
import hr.span.akademija.kadrovska.ApiTestBaza;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

@Tag("funkcionalno")
class EdukacijeFunkcionalniTest extends ApiTestBaza {

    /** Edukacija 3 je rezervirana za posebne provjere i ne koristi se u ovim testovima. */
    private static final long IZUZETA_EDUKACIJA = 3;

    private long novaEdukacija(int kapacitet) {
        String voditelj = prijava("voditelj", "Ovlasteni12345!");
        String xml = "<edukacije><edukacija><naziv>Radionica timskog rada</naziv><datum>2027-02-10</datum>"
                + "<kapacitet>" + kapacitet + "</kapacitet></edukacija></edukacije>";
        Odgovor odgovor = posalji("POST", "/api/edukacije/uvoz", voditelj, "application/xml", xml);
        assertThat(odgovor.status()).isEqualTo(200);
        return odgovor.json().get("edukacije").get(0).get("id").asLong();
    }

    private JsonNode edukacija(String token, long id) {
        for (JsonNode e : get("/api/edukacije", token).json()) {
            if (e.get("id").asLong() == id) {
                return e;
            }
        }
        throw new AssertionError("Edukacija " + id + " nije pronađena");
    }

    @Test
    @DisplayName("Popis edukacija prikazuje kapacitet i slobodna mjesta")
    void popisEdukacija() {
        String token = prijava("ana", "Ana12345!");

        Odgovor odgovor = get("/api/edukacije", token);

        assertThat(odgovor.status()).isEqualTo(200);
        assertThat(odgovor.json().size()).isGreaterThanOrEqualTo(5);
        for (JsonNode e : odgovor.json()) {
            assertThat(e.get("slobodno").asInt()).isEqualTo(e.get("kapacitet").asInt() - e.get("prijavljeno").asInt());
        }
        assertThat(edukacija(token, IZUZETA_EDUKACIJA).get("kapacitet").asInt()).isEqualTo(20);
    }

    @Test
    @DisplayName("Prijava na edukaciju vraća 201 i povećava broj prijavljenih")
    void prijavaNaEdukaciju() {
        long id = novaEdukacija(10);
        String token = prijava("ana", "Ana12345!");

        Odgovor odgovor = postJson("/api/edukacije/" + id + "/prijava", token, null);

        assertThat(odgovor.status()).isEqualTo(201);
        assertThat(odgovor.json().get("edukacijaId").asLong()).isEqualTo(id);
        assertThat(edukacija(token, id).get("prijavljeno").asInt()).isEqualTo(1);
    }

    @Test
    @DisplayName("Ponovna prijava istog zaposlenika vraća 409")
    void ponovnaPrijava() {
        long id = novaEdukacija(10);
        String token = prijava("marko", "Marko12345!");
        assertThat(postJson("/api/edukacije/" + id + "/prijava", token, null).status()).isEqualTo(201);

        Odgovor odgovor = postJson("/api/edukacije/" + id + "/prijava", token, null);

        assertThat(odgovor.status()).isEqualTo(409);
    }

    @Test
    @DisplayName("Prijava na popunjenu edukaciju vraća 409")
    void popunjenaEdukacija() {
        long id = novaEdukacija(1);
        assertThat(postJson("/api/edukacije/" + id + "/prijava", prijava("ana", "Ana12345!"), null).status())
                .isEqualTo(201);

        Odgovor odgovor = postJson("/api/edukacije/" + id + "/prijava", prijava("marko", "Marko12345!"), null);

        assertThat(odgovor.status()).isEqualTo(409);
        assertThat(odgovor.json().get("greska").asText()).isEqualTo("Nema slobodnih mjesta.");
    }

    @Test
    @DisplayName("Prijava na nepostojeću edukaciju vraća 404")
    void nepostojecaEdukacija() {
        String token = prijava("ana", "Ana12345!");

        assertThat(postJson("/api/edukacije/999999/prijava", token, null).status()).isEqualTo(404);
    }
}
