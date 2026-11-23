package hr.span.akademija.kadrovska.security;

import hr.span.akademija.kadrovska.ApiTestBaza;
import hr.span.akademija.kadrovska.platforma.KorisnikService;
import org.junit.jupiter.api.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;
import org.springframework.transaction.annotation.Transactional;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.time.LocalDate;
import java.util.HexFormat;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatException;

@Tag("Security")
class AuthControllerSecurityTest extends ApiTestBaza {

    private static final String ORIGINAL_PASS = "Ana12345!";

    @Autowired
    private KorisnikService korisnikService;
    @Autowired
    private JdbcTemplate jdbcTemplate;

    @AfterEach
    void postaviOriginalnuLozinku(){
        korisnikService.promijeniLozinku("ana", ORIGINAL_PASS);
    }

    @Test
    void tokenZaResetSeNeMozePogoditi() throws NoSuchAlgorithmException{
        String loginToken = prijava("ana", "Ana12345!");

        Odgovor odgovorNaZahtjevZaReset = posalji("POST", "/api/auth/reset-zahtjev", loginToken, "application/json", "{\"username\": \"ana\"}");
        assertThat(odgovorNaZahtjevZaReset.status()).isEqualTo(HttpStatus.ACCEPTED.value());

        String pogodjeniToken = md5("ana" + LocalDate.now());
        Odgovor odgovorNaReset = posalji("POST", "/api/auth/reset", null, "application/json", "{\"token\": \"" + pogodjeniToken + "\",\"novaLozinka\": \"Anica123333!\"}");

        assertThat(odgovorNaReset.status()).isEqualTo(HttpStatus.BAD_REQUEST.value());

        Optional<KorisnikService.Korisnik> prijavljeniKorisnik = korisnikService.prijava(new KorisnikService.PrijavaZahtjev("ana", ORIGINAL_PASS));
        assertThat(prijavljeniKorisnik.isPresent()).isTrue();
    }


    @Test
    void tokenSeNeMozeIskoristitiVisePuta(){
        String loginToken = prijava("ana", "Ana12345!");

        Odgovor odgovorNaZahtjevZaReset = posalji("POST", "/api/auth/reset-zahtjev", loginToken, "application/json", "{\"username\": \"ana\"}");
        assertThat(odgovorNaZahtjevZaReset.status()).isEqualTo(HttpStatus.ACCEPTED.value());

        String token = jdbcTemplate.queryForList("SELECT token FROM reset_tokeni WHERE username = ? ORDER BY id DESC LIMIT 1", String.class, "ana").get(0);

        String novaLozinka = "Anica123333!";
        Odgovor odgovorNaReset = posalji("POST", "/api/auth/reset", null, "application/json", "{\"token\": \"" + token + "\",\"novaLozinka\": \"" + novaLozinka + "\"}");
        assertThat(odgovorNaReset.status()).isEqualTo(HttpStatus.OK.value());

        Odgovor drugiOdgovorNaReset = posalji("POST", "/api/auth/reset", null, "application/json", "{\"token\": \"" + token + "\",\"novaLozinka\": \"Anica14444444!\"}");
        assertThat(drugiOdgovorNaReset.status()).isEqualTo(HttpStatus.BAD_REQUEST.value());

        Optional<KorisnikService.Korisnik> prijavljeniKorisnik = korisnikService.prijava(new KorisnikService.PrijavaZahtjev("ana", novaLozinka));
        assertThat(prijavljeniKorisnik.isPresent()).isTrue();
    }


    String md5(String s) throws NoSuchAlgorithmException{
        MessageDigest md = MessageDigest.getInstance("MD5");
        return HexFormat.of().formatHex(md.digest(s.getBytes(StandardCharsets.UTF_8)));
    }

}
