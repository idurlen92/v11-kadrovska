using System.Globalization;
using System.Text;

namespace Kadrovska.Api.Infrastruktura;

/// <summary>
/// Slanje e-pošte. U lokalnom okruženju poruke se ne šalju, nego spremaju kao tekstne datoteke
/// <c>&lt;korisnik&gt;-&lt;vrijeme&gt;.txt</c> u mapu <c>App:MailboxDir</c>.
/// </summary>
public sealed class Posta(PostavkeAplikacije postavke, ILogger<Posta> logger)
{
    private readonly Lock _zakljucavanje = new();

    public void Posalji(string username, string email, string predmet, string tijelo)
    {
        var poruka = new StringBuilder()
            .AppendLine($"Od: {Aplikacija.Naziv} <noreply@{Aplikacija.Naziv.ToLowerInvariant()}.example>")
            .AppendLine($"Za: {email}")
            .AppendLine($"Predmet: {predmet}")
            .AppendLine($"Datum: {DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)}")
            .AppendLine()
            .AppendLine(tijelo)
            .ToString();

        lock (_zakljucavanje)
        {
            Directory.CreateDirectory(postavke.DirektorijPoste);
            var oznaka = DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            var putanja = Path.Combine(postavke.DirektorijPoste, $"{username}-{oznaka}.txt");
            for (var i = 1; File.Exists(putanja); i++)
            {
                putanja = Path.Combine(postavke.DirektorijPoste, $"{username}-{oznaka}-{i}.txt");
            }
            File.WriteAllText(putanja, poruka, new UTF8Encoding(false));
        }

        logger.LogInformation("Poslana poruka \"{Predmet}\" korisniku {Username}", predmet, username);
    }
}
