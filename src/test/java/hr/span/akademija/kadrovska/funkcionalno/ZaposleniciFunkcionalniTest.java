package hr.span.akademija.kadrovska.funkcionalno;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.databind.JsonNode;
import hr.span.akademija.kadrovska.ApiTestBaza;
import java.util.ArrayList;
import java.util.List;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

@Tag("funkcionalno")
class ZaposleniciFunkcionalniTest extends ApiTestBaza {

    private List<String> polje(Odgovor odgovor, String naziv) {
        List<String> vrijednosti = new ArrayList<>();
        for (JsonNode zaposlenik : odgovor.json()) {
            vrijednosti.add(zaposlenik.get(naziv).asText());
        }
        return vrijednosti;
    }

    @Test
    @DisplayName("Imenik sortiran po prezimenu uzlazno")
    void poPrezimenuUzlazno() {
        String token = prijava("ana", "Ana12345!");

        Odgovor odgovor = get("/api/zaposlenici?sort=prezime&smjer=asc", token);

        assertThat(odgovor.status()).isEqualTo(200);
        assertThat(polje(odgovor, "prezime"))
                .containsExactly("Anić", "Babić", "Horvat", "Jurić", "Marić", "Tomić", "Vidović", "Zorić");
    }

    @Test
    @DisplayName("Imenik sortiran po prezimenu silazno")
    void poPrezimenuSilazno() {
        String token = prijava("ana", "Ana12345!");

        Odgovor odgovor = get("/api/zaposlenici?sort=prezime&smjer=desc", token);

        assertThat(polje(odgovor, "prezime"))
                .containsExactly("Zorić", "Vidović", "Tomić", "Marić", "Jurić", "Horvat", "Babić", "Anić");
    }

    @Test
    @DisplayName("Imenik sortiran po imenu i po odjelu")
    void poImenuIOdjelu() {
        String token = prijava("ana", "Ana12345!");

        Odgovor poImenu = get("/api/zaposlenici?sort=ime&smjer=asc", token);
        Odgovor poOdjelu = get("/api/zaposlenici?sort=odjel&smjer=asc", token);

        assertThat(polje(poImenu, "ime"))
                .containsExactly("Ana", "Davor", "Ivan", "Lucija", "Maja", "Marko", "Sanja", "Tomislav");
        assertThat(polje(poOdjelu, "odjel")).isSorted().first().isEqualTo("Financije");
    }

    @Test
    @DisplayName("Zadano sortiranje je po prezimenu uzlazno")
    void zadanoSortiranje() {
        String token = prijava("ana", "Ana12345!");

        assertThat(polje(get("/api/zaposlenici", token), "prezime")).hasSize(8).first().isEqualTo("Anić");
    }

    @Test
    @DisplayName("Imenik traži prijavu")
    void traziPrijavu() {
        assertThat(get("/api/zaposlenici", null).status()).isEqualTo(401);
    }
}
