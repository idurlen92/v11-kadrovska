package hr.span.akademija.kadrovska.edukacije;

import java.time.Duration;
import java.util.concurrent.atomic.AtomicLong;
import org.springframework.stereotype.Component;

/**
 * Klijent sustava za e-učenje (LMS) u koji se upisuju polaznici edukacija. U lokalnom okruženju LMS
 * nije dostupan pa klijent nakon uobičajenog vremena odziva vraća lokalno generiranu oznaku upisa.
 */
@Component
public class SustavZaEdukacijeKlijent {

    private static final Duration VRIJEME_ODZIVA = Duration.ofMillis(150);

    private final AtomicLong brojac = new AtomicLong(3000);

    public String upisiPolaznika(long edukacijaId, String zaposlenik) {
        try {
            Thread.sleep(VRIJEME_ODZIVA);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            throw new IllegalStateException("Poziv prema LMS-u je prekinut", e);
        }
        return "LMS-" + edukacijaId + "-" + brojac.incrementAndGet();
    }
}
