package hr.span.akademija.kadrovska.platforma;

import hr.span.akademija.kadrovska.platforma.KorisnikService.Korisnik;
import hr.span.akademija.kadrovska.platforma.KorisnikService.PrijavaZahtjev;

import java.net.HttpURLConnection;
import java.net.InetAddress;
import java.net.URI;
import java.net.URL;
import java.util.Map;
import java.util.Optional;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseCookie;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestParam;

/** HTML stranice: početna, prijava obrascem (postavlja kolačić SESSION) i promjena lozinke tokenom. */
@Controller
public class PrijavaController {

    private static final Logger log = LoggerFactory.getLogger(PrijavaController.class);

    private final KorisnikService korisnici;
    private final TokenStore tokeni;
    private final ResetLozinkeService reset;
    private final AppProperties postavke;

    public PrijavaController(KorisnikService korisnici, TokenStore tokeni, ResetLozinkeService reset,
                             AppProperties postavke) {
        this.korisnici = korisnici;
        this.tokeni = tokeni;
        this.reset = reset;
        this.postavke = postavke;
    }

    @GetMapping("/")
    public ResponseEntity<String> pocetna() {
        String tijelo = """
                <h1>%s</h1>
                <p>Interni HR portal: imenik zaposlenika, godišnji odmori, platne liste i edukacije.</p>
                <ul>
                <li><a href="/prijava">Prijava</a></li>
                <li><a href="/api/health">Stanje sustava</a></li>
                </ul>
                """.formatted(Html.escape(postavke.naziv()));
        return Html.odgovor(Html.stranica(postavke.naziv(), tijelo));
    }

    @GetMapping("/prijava")
    public ResponseEntity<String> obrazac(@RequestParam(required = false) String returnUrl) {
        return Html.odgovor(obrazacPrijave(returnUrl, null));
    }

    @PostMapping("/prijava")
    public ResponseEntity<String> prijava(@RequestParam(required = false) String username,
                                          @RequestParam(required = false) String password,
                                          @RequestParam(required = false) String returnUrl) {
        if(!allowedURL(returnUrl)){
            throw ApiException.neispravno("Neispravan zahtjev");
        }
        Optional<Korisnik> korisnik = korisnici.prijava(new PrijavaZahtjev(username, password));
        if (korisnik.isEmpty()) {
            return Html.odgovor(obrazacPrijave(returnUrl, "Neispravno korisničko ime ili lozinka."));
        }
        String token = tokeni.izdaj(korisnik.get().prijavljeni());
        ResponseCookie kolacic = ResponseCookie.from(AuthFilter.KOLACIC, token)
                .httpOnly(true)
                .sameSite("Lax")
                .path("/")
                .build();
        return ResponseEntity.status(HttpStatus.FOUND)
                .header(HttpHeaders.LOCATION, odrediste(returnUrl))
                .header(HttpHeaders.SET_COOKIE, kolacic.toString())
                .build();
    }

    private boolean allowedURL(String returnUrl) {
        if(returnUrl == null){
            return true;
        }
        if(returnUrl.startsWith("/")){
            return true;
        }
        try {
            URI uri= URI.create(returnUrl);
            if(!"http".equals(uri.getScheme()) && !"https".equals(uri.getScheme())){
                return false;
            }
            String host = uri.getHost();
            if(!"localhost".equals(host)){
                return false;
            }

            InetAddress[] addresses = InetAddress.getAllByName(host);
            for (InetAddress address : addresses) {
                if (isBlocked(address)) {
                    return false;
                }
            }

            return true;
        }
        catch (Exception e){
            log.error("Invalid URL: {}", e.getMessage());
            return false;
        }

    }

    @GetMapping("/reset")
    public ResponseEntity<String> obrazacZaLozinku(@RequestParam(required = false) String token) {
        return Html.odgovor(obrazacNoveLozinke(token, null));
    }

    @PostMapping("/reset")
    public ResponseEntity<String> novaLozinka(@RequestParam(required = false) String token,
                                              @RequestParam(required = false) String novaLozinka) {
        boolean promijenjena;
        try {
            promijenjena = reset.postaviNovuLozinku(token, novaLozinka);
        } catch (ApiException e) {
            return Html.odgovor(obrazacNoveLozinke(token, e.getMessage()));
        }
        if (!promijenjena) {
            return Html.odgovor(obrazacNoveLozinke(token, "Token nije valjan ili je istekao."));
        }
        return Html.odgovor(Html.stranica("Lozinka je promijenjena",
                "<h1>Lozinka je promijenjena</h1>\n<p><a href=\"/prijava\">Prijava</a></p>"));
    }

    private String obrazacPrijave(String returnUrl, String poruka) {
        String tijelo = """
                <h1>Prijava</h1>
                %s
                <form method="post" action="/prijava">
                <label>Korisničko ime <input name="username" autocomplete="username" required></label>
                <label>Lozinka <input name="password" type="password" autocomplete="current-password" required></label>
                <input type="hidden" name="returnUrl" value="%s">
                <p><button type="submit">Prijavi se</button></p>
                </form>
                """.formatted(poruka == null ? "" : "<p class=\"poruka\">" + Html.escape(poruka) + "</p>",
                Html.escape(returnUrl));
        return Html.stranica("Prijava – " + postavke.naziv(), tijelo);
    }

    private String obrazacNoveLozinke(String token, String poruka) {
        String tijelo = """
                <h1>Nova lozinka</h1>
                %s
                <form method="post" action="/reset">
                <input type="hidden" name="token" value="%s">
                <label>Nova lozinka <input name="novaLozinka" type="password" autocomplete="new-password" required></label>
                <p><button type="submit">Spremi</button></p>
                </form>
                """.formatted(poruka == null ? "" : "<p class=\"poruka\">" + Html.escape(poruka) + "</p>",
                Html.escape(token));
        return Html.stranica("Nova lozinka – " + postavke.naziv(), tijelo);
    }

    private static String odrediste(String returnUrl) {
        if (returnUrl == null || returnUrl.isBlank()) {
            return "/";
        }
        return returnUrl;
    }

    private boolean isBlocked(InetAddress address) {
        if (address.isLoopbackAddress()       // 127.0.0.0/8, ::1
                || address.isAnyLocalAddress() // 0.0.0.0, ::
                || address.isLinkLocalAddress()  // 169.254/16 (cloud metadata), fe80::/10
                || address.isSiteLocalAddress()) { // 10/8, 172.16/12, 192.168/16
            return true;
        }
        byte[] bytes = address.getAddress();
        // IPv6 Unique Local Address fc00::/7 (fc.. i fd..).
        if (bytes.length == 16) {
            int first = bytes[0] & 0xFF;
            return (first & 0xFE) == 0xFC;
        }
        return false;
    }

}
