package hr.span.akademija.kadrovska.edukacije;

import hr.span.akademija.kadrovska.platforma.ApiException;
import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import javax.xml.parsers.DocumentBuilder;
import javax.xml.parsers.DocumentBuilderFactory;
import javax.xml.parsers.ParserConfigurationException;
import org.springframework.stereotype.Component;
import org.w3c.dom.Document;
import org.w3c.dom.Element;
import org.w3c.dom.NodeList;
import org.xml.sax.SAXException;
import org.xml.sax.helpers.DefaultHandler;

/**
 * Čita XML s planom edukacija:
 * <pre>
 * &lt;edukacije&gt;&lt;edukacija&gt;&lt;naziv&gt;Excel za početnike&lt;/naziv&gt;&lt;datum&gt;2026-12-03&lt;/datum&gt;
 * &lt;kapacitet&gt;15&lt;/kapacitet&gt;&lt;/edukacija&gt;&lt;/edukacije&gt;
 * </pre>
 */
@Component
public class UvozEdukacija {

    public record Stavka(String naziv, LocalDate datum, int kapacitet) {
    }

    public List<Stavka> procitaj(byte[] xml) {
        Document dokument;
        try {
            DocumentBuilder builder = tvornica().newDocumentBuilder();
            builder.setErrorHandler(new DefaultHandler());
            dokument = builder.parse(new ByteArrayInputStream(xml));
        } catch (ParserConfigurationException | SAXException | IOException e) {
            throw ApiException.neispravno("Neispravan XML.");
        }
        NodeList elementi = dokument.getElementsByTagName("edukacija");
        List<Stavka> stavke = new ArrayList<>();
        for (int i = 0; i < elementi.getLength(); i++) {
            Element edukacija = (Element) elementi.item(i);
            try {
                stavke.add(new Stavka(
                        tekst(edukacija, "naziv"),
                        LocalDate.parse(tekst(edukacija, "datum")),
                        Integer.parseInt(tekst(edukacija, "kapacitet"))));
            } catch (RuntimeException e) {
                throw ApiException.neispravno("Neispravni podaci u edukaciji " + (i + 1) + ".");
            }
        }
        return stavke;
    }

    private static DocumentBuilderFactory tvornica() throws ParserConfigurationException {
        DocumentBuilderFactory tvornica = DocumentBuilderFactory.newInstance();
        tvornica.setNamespaceAware(true);
        tvornica.setFeature("http://apache.org/xml/features/disallow-doctype-decl", true);
        tvornica.setFeature("http://xml.org/sax/features/external-general-entities", false);
        tvornica.setFeature("http://xml.org/sax/features/external-parameter-entities", false);
        tvornica.setXIncludeAware(false);
        tvornica.setExpandEntityReferences(false);
        return tvornica;
    }

    private static String tekst(Element roditelj, String naziv) {
        NodeList elementi = roditelj.getElementsByTagName(naziv);
        String vrijednost = elementi.getLength() == 0 ? null : elementi.item(0).getTextContent().trim();
        if (vrijednost == null || vrijednost.isEmpty()) {
            throw new IllegalArgumentException("Nedostaje element " + naziv);
        }
        return vrijednost;
    }
}
