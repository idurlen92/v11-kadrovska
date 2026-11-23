package hr.span.akademija.kadrovska.platforma;

public class StringUtils {

    public static String replaceNewlines(String input){
        if(input == null){
            return null;
        }
        return input.replace("\r\n", "_")
                .replace("\n", "_")
                .replace("\r", "–");
    }

}
