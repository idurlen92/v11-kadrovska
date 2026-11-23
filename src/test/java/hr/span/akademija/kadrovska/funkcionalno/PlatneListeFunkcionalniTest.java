package hr.span.akademija.kadrovska.funkcionalno;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.databind.JsonNode;
import hr.span.akademija.kadrovska.ApiTestBaza;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

@Tag("funkcionalno")
class PlatneListeFunkcionalniTest extends ApiTestBaza {

    @Test
    @DisplayName("Moje platne liste sadrže samo liste prijavljenog zaposlenika")
    void mojePlatneListe() {
        String token = prijava("ana", "Ana12345!");

        Odgovor odgovor = get("/api/platne-liste/moje", token);

        assertThat(odgovor.status()).isEqualTo(200);
        assertThat(odgovor.json().size()).isEqualTo(1);
        JsonNode lista = odgovor.json().get(0);
        assertThat(lista.get("id").asLong()).isEqualTo(1);
        assertThat(lista.get("razdoblje").asText()).isEqualTo("2026-10");
        assertThat(lista.get("datoteka").asText()).isEqualTo("placa-ana-2026-10.txt");
    }

    @Test
    @DisplayName("Preuzimanje vlastite platne liste")
    void preuzimanjeVlastiteDatoteke() {
        String token = prijava("ana", "Ana12345!");

        Odgovor odgovor = get("/api/platne-liste/preuzmi?datoteka=placa-ana-2026-10.txt", token);

        assertThat(odgovor.status()).isEqualTo(200);
        assertThat(odgovor.tijelo()).contains("Zaposlenik: Ana Anić").contains("Neto isplata");
    }

    @Test
    @DisplayName("Platna lista u JSON formatu sadrži podatke i sadržaj")
    void platnaListaJson() {
        String token = prijava("marko", "Marko12345!");

        Odgovor odgovor = get("/api/platne-liste/2?format=json", token);

        assertThat(odgovor.status()).isEqualTo(200);
        assertThat(odgovor.json().get("razdoblje").asText()).isEqualTo("2026-10");
        assertThat(odgovor.json().get("sadrzaj").asText()).contains("Zaposlenik: Marko Marić");
    }

    @Test
    @DisplayName("Platna lista u PDF formatu")
    void platnaListaPdf() {
        String token = prijava("ana", "Ana12345!");

        Odgovor odgovor = get("/api/platne-liste/1?format=pdf", token);

        assertThat(odgovor.status()).isEqualTo(200);
        assertThat(odgovor.zaglavlje("Content-Type")).startsWith("application/pdf");
        assertThat(odgovor.tijelo()).startsWith("%PDF");
    }

    @Test
    @DisplayName("Bez parametra format platna lista se vraća kao JSON")
    void zadaniFormat() {
        String token = prijava("ana", "Ana12345!");

        Odgovor odgovor = get("/api/platne-liste/1", token);

        assertThat(odgovor.status()).isEqualTo(200);
        assertThat(odgovor.json().get("sadrzaj").asText()).contains("Bruto plaća");
    }
}
