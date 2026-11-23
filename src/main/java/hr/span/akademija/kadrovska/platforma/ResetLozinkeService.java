package hr.span.akademija.kadrovska.platforma;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.HexFormat;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Service;

/** Zaboravljena lozinka: token se šalje e-poštom, a njime se postavlja nova lozinka. */
@Service
public class ResetLozinkeService {

    private final JdbcTemplate jdbc;
    private final KorisnikService korisnici;
    private final PostaService posta;

    public ResetLozinkeService(JdbcTemplate jdbc, KorisnikService korisnici, PostaService posta) {
        this.jdbc = jdbc;
        this.korisnici = korisnici;
        this.posta = posta;
    }

    public void zatrazi(String username) {
        Optional<KorisnikService.Korisnik> korisnik = korisnici.pronadi(username);
        if (korisnik.isEmpty()) {
            return;
        }
        String inputZaSazetak = username + UUID.randomUUID().toString();
        String token = sazetak("SHA-256", inputZaSazetak);
        jdbc.update("INSERT INTO reset_tokeni (username, token) VALUES (?, ?)", username, token);
        posta.posaljiUputeZaLozinku(korisnik.get(), token);
    }

    public boolean postaviNovuLozinku(String token, String novaLozinka) {
        KorisnikService.provjeriLozinku(novaLozinka);
        if (token == null || token.isBlank()) {
            return false;
        }
        List<String> vlasnici = jdbc.queryForList(
                "SELECT username FROM reset_tokeni WHERE token = ? AND iskoristen = false", String.class, token.trim());
        if (vlasnici.isEmpty()) {
            return false;
        }
        korisnici.promijeniLozinku(vlasnici.get(0), novaLozinka);
        jdbc.update("UPDATE reset_tokeni SET iskoristen = true WHERE token = ?", token);
        return true;
    }

    private static String sazetak(String algoritam, String vrijednost) {
        try {
            MessageDigest md = MessageDigest.getInstance(algoritam);
            return HexFormat.of().formatHex(md.digest(vrijednost.getBytes(StandardCharsets.UTF_8)));
        } catch (NoSuchAlgorithmException e) {
            throw new IllegalStateException(algoritam + " nije dostupan", e);
        }
    }
}
