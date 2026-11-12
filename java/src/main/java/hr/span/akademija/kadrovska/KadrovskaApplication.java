package hr.span.akademija.kadrovska;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.properties.ConfigurationPropertiesScan;

@SpringBootApplication
@ConfigurationPropertiesScan
public class KadrovskaApplication {

    public static void main(String[] args) {
        SpringApplication.run(KadrovskaApplication.class, args);
    }
}
