package hr.span.akademija.kadrovska.security;

import hr.span.akademija.kadrovska.ApiTestBaza;
import hr.span.akademija.kadrovska.zaposlenici.ZaposlenikController;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;

import java.io.UnsupportedEncodingException;
import java.net.URLEncoder;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.InstanceOfAssertFactories.MAP;

@Tag("Security")
public class ZaposleniciSecurityTest extends ApiTestBaza {

    @Autowired
    private JdbcTemplate jdbcTemplate;

    @Test
    public void umetnutiSqlUSortuSeNeIzvrsavaIVracaBadRequestI() throws UnsupportedEncodingException {
        String token = prijava("ana", "Ana12345!");
        String umetnutiSql = "1; INSERT INTO zaposlenici (id, ime, prezime, odjel, email) VALUES (200, 'ivan', 'durlen', 'black-hat', 'ivan@mail.com');--";

        Odgovor odgovor = get("/api/zaposlenici?sort=" + URLEncoder.encode(umetnutiSql, "UTF-8") + "&smjer=asc", token);
        assertThat(odgovor.status()).isEqualTo(HttpStatus.BAD_REQUEST.value());

        Optional<ZaposlenikController.Zaposlenik> kreiraniZaposlenik = dohvatiZaposlenika(200);
        assertThat(kreiraniZaposlenik.isPresent()).isFalse();
    }


    @Test
    public void umetnutiSqlUSmjeruSeNeIzvrsavaIVracaBadRequest() throws UnsupportedEncodingException {
        String token = prijava("ana", "Ana12345!");
        String umetnutiSql = "ASC; INSERT INTO zaposlenici (id, ime, prezime, odjel, email) VALUES (200, 'ivan', 'durlen', 'black-hat', 'ivan@mail.com');--";

        Odgovor odgovor = get("/api/zaposlenici?sort=ime&smjer=" + URLEncoder.encode(umetnutiSql, "UTF-8"), token);
        assertThat(odgovor.status()).isEqualTo(HttpStatus.BAD_REQUEST.value());

        Optional<ZaposlenikController.Zaposlenik> kreiraniZaposlenik = dohvatiZaposlenika(200);
        assertThat(kreiraniZaposlenik.isPresent()).isFalse();
    }


    private Optional<ZaposlenikController.Zaposlenik> dohvatiZaposlenika(long id){
        RowMapper<ZaposlenikController.Zaposlenik>  mapper = (rs, i) -> new ZaposlenikController.Zaposlenik(rs.getLong("id"),
                rs.getString("ime"), rs.getString("prezime"), rs.getString("odjel"), rs.getString("email"));
        String sql = "SELECT id, ime, prezime, odjel, email FROM zaposlenici WHERE id = ?";
        return jdbcTemplate.query(sql, mapper, id).stream().findFirst();
    }

}
