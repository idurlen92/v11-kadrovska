package hr.span.akademija.kadrovska.funkcionalno;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.databind.JsonNode;
import hr.span.akademija.kadrovska.ApiTestBaza;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

@Tag("funkcionalno")
class ZahtjeviFunkcionalniTest extends ApiTestBaza {

    private List<Long> mojiZahtjevi(String token) {
        List<Long> id = new ArrayList<>();
        for (JsonNode z : get("/api/zahtjevi/moji", token).json()) {
            id.add(z.get("id").asLong());
        }
        return id;
    }

    private Odgovor podnesi(String token, String od, String doDatuma, String razlog, String oib) {
        return postJson("/api/zahtjevi", token, Map.of("od", od, "do", doDatuma, "razlog", razlog, "oib", oib));
    }

    @Test
    @DisplayName("Novi zahtjev za godišnji vraća 201 i vidljiv je u mojim zahtjevima")
    void noviZahtjev() {
        String token = prijava("ana", "Ana12345!");

        Odgovor odgovor = podnesi(token, "2027-07-01", "2027-07-14", "Ljetni odmor", "48291736504");

        assertThat(odgovor.status()).isEqualTo(201);
        JsonNode zahtjev = odgovor.json();
        assertThat(zahtjev.get("od").asText()).isEqualTo("2027-07-01");
        assertThat(zahtjev.get("do").asText()).isEqualTo("2027-07-14");
        assertThat(zahtjev.get("razlog").asText()).isEqualTo("Ljetni odmor");
        assertThat(zahtjev.get("status").asText()).isEqualTo("PODNESEN");
        assertThat(mojiZahtjevi(token)).contains(zahtjev.get("id").asLong());
    }

    @Test
    @DisplayName("Datum završetka prije početka vraća 400")
    void neispravnoRazdoblje() {
        String token = prijava("ana", "Ana12345!");

        assertThat(podnesi(token, "2027-07-14", "2027-07-01", "Odmor", "48291736504").status()).isEqualTo(400);
    }

    @Test
    @DisplayName("Neispravan OIB vraća 400")
    void neispravanOib() {
        String token = prijava("ana", "Ana12345!");

        assertThat(podnesi(token, "2027-07-01", "2027-07-02", "Odmor", "12AB").status()).isEqualTo(400);
    }

    @Test
    @DisplayName("Zaposlenik može povući vlastiti zahtjev")
    void povlacenjeVlastitogZahtjeva() {
        String token = prijava("marko", "Marko12345!");
        long id = podnesi(token, "2027-08-03", "2027-08-07", "Vjenčanje", "73019485267").json().get("id").asLong();

        Odgovor odgovor = delete("/api/zahtjevi/" + id, token);

        assertThat(odgovor.status()).isEqualTo(204);
        assertThat(mojiZahtjevi(token)).doesNotContain(id);
    }

    @Test
    @DisplayName("Potvrda zahtjeva za ispis sadrži razlog")
    void ispisVlastitogZahtjeva() {
        String token = prijava("ana", "Ana12345!");
        long id = podnesi(token, "2027-09-01", "2027-09-05", "Upis djeteta u školu", "48291736504")
                .json().get("id").asLong();

        Odgovor odgovor = get("/ispis/zahtjevi/" + id, token);

        assertThat(odgovor.status()).isEqualTo(200);
        assertThat(odgovor.zaglavlje("Content-Type")).startsWith("text/html");
        assertThat(odgovor.tijelo()).contains("Zahtjev za godišnji odmor").contains("Upis djeteta u školu");
    }
}
