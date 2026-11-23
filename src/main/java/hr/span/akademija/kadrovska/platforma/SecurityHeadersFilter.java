package hr.span.akademija.kadrovska.platforma;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.core.Ordered;
import org.springframework.core.annotation.Order;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpMethod;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.util.List;

/**
 * CORS za frontend aplikaciju (npr. Vite na {@code http://localhost:5173}) koja API poziva s kolačićem
 * ili tokenom. Odgovara i na preflight zahtjeve ({@code OPTIONS}).
 */
@Component
@Order(Ordered.HIGHEST_PRECEDENCE + 1)
public class SecurityHeadersFilter extends OncePerRequestFilter {

    private static final String METODE = "GET, POST, PUT, DELETE, OPTIONS";
    private static final String ZAGLAVLJA = "Authorization, Content-Type";

    private final List<String> dopusteniOrigini;

    public SecurityHeadersFilter(AppProperties postavke) {
        this.dopusteniOrigini = postavke.dopusteniOrigini();
    }

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain chain)
            throws ServletException, IOException {
        response.setHeader("Content-Security-Policy", "default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'");
        response.setHeader("X-Content-Type-Options", "nosniff");
        response.setHeader("X-Frame-Options", "DENY");
        response.setHeader("Referrer-Policy", "no-referrer");
        chain.doFilter(request, response);
    }

    private boolean dopusten(String origin) {
        return origin.startsWith("http://") || origin.startsWith("https://");
    }
}
