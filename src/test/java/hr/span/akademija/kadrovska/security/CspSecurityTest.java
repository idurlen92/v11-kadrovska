package hr.span.akademija.kadrovska.security;

import hr.span.akademija.kadrovska.ApiTestBaza;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

@Tag("Security")
public class CspSecurityTest extends ApiTestBaza{

    @Test
    public void cspHeadersAdded(){
        Odgovor odgovor = posalji("GET", "/prijava", null, null, null);
        assertThat(odgovor.zaglavlja().allValues("Content-Security-Policy")).isNotEmpty();
    }

}
