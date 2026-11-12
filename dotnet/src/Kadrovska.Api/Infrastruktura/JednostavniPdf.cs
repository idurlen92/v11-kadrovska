using System.Globalization;
using System.Text;

namespace Kadrovska.Api.Infrastruktura;

/// <summary>Izrada jednostavnog PDF dokumenta (jedna stranica, tekst) bez vanjskih biblioteka.</summary>
public static class JednostavniPdf
{
    private const int NajviseRedaka = 50;

    public static byte[] Izradi(string naslov, string tekst)
    {
        var tok = new StringBuilder()
            .Append("BT\n/F1 16 Tf\n50 790 Td\n(").Append(Ocisti(naslov)).Append(") Tj\n")
            .Append("/F1 11 Tf\n0 -30 Td\n");
        foreach (var redak in tekst.Replace("\r", string.Empty).Split('\n').Take(NajviseRedaka))
        {
            tok.Append('(').Append(Ocisti(redak)).Append(") Tj\n0 -15 Td\n");
        }
        tok.Append("ET\n");
        var sadrzaj = tok.ToString();

        string[] objekti =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            $"<< /Length {sadrzaj.Length} >>\nstream\n{sadrzaj}endstream",
        ];

        var pdf = new StringBuilder("%PDF-1.4\n");
        var pomaci = new List<int>();
        for (var i = 0; i < objekti.Length; i++)
        {
            pomaci.Add(pdf.Length);
            pdf.Append(Broj(i + 1)).Append(" 0 obj\n").Append(objekti[i]).Append("\nendobj\n");
        }

        var xref = pdf.Length;
        pdf.Append("xref\n0 ").Append(Broj(objekti.Length + 1)).Append('\n')
            .Append("0000000000 65535 f \n");
        foreach (var pomak in pomaci)
        {
            pdf.Append(pomak.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        }
        pdf.Append("trailer\n<< /Size ").Append(Broj(objekti.Length + 1)).Append(" /Root 1 0 R >>\n")
            .Append("startxref\n").Append(Broj(xref)).Append("\n%%EOF\n");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static string Broj(int broj) => broj.ToString(CultureInfo.InvariantCulture);

    /// <summary>Samo ASCII znakovi (standardni PDF font); dijakritici se zamjenjuju osnovnim slovom.</summary>
    private static string Ocisti(string tekst)
    {
        var rezultat = new StringBuilder(tekst.Length);
        foreach (var znak in tekst)
        {
            var zamjena = znak switch
            {
                'č' or 'ć' => "c",
                'Č' or 'Ć' => "C",
                'đ' => "d",
                'Đ' => "D",
                'š' => "s",
                'Š' => "S",
                'ž' => "z",
                'Ž' => "Z",
                '–' or '—' => "-",
                '\\' or '(' or ')' => "\\" + znak,
                _ when znak >= ' ' && znak <= '~' => znak.ToString(),
                _ => "?",
            };
            rezultat.Append(zamjena);
        }
        return rezultat.ToString();
    }
}
