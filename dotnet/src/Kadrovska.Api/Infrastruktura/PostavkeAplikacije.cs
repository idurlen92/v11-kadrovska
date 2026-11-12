namespace Kadrovska.Api.Infrastruktura;

/// <summary>Postavke aplikacije iz konfiguracije (appsettings.json, varijable okoline, testovi).</summary>
public sealed class PostavkeAplikacije
{
    public PostavkeAplikacije(IConfiguration configuration, IHostEnvironment environment)
    {
        DirektorijDokumenata = PunaPutanja(configuration["App:StorageDir"] ?? "./data/dokumenti", environment.ContentRootPath);
        DirektorijPoste = PunaPutanja(configuration["App:MailboxDir"] ?? "./data/posta", environment.ContentRootPath);
        DozvoljeniOrigini = configuration.GetSection("App:Cors:AllowedOrigins").Get<string[]>() ?? [];
        JavniUrl = (configuration["App:PublicUrl"] ?? "http://localhost:5080").TrimEnd('/');

        var connectionString = configuration.GetConnectionString("Default")
            ?? $"Data Source={Aplikacija.Naziv.ToLowerInvariant()}-{{guid}};Mode=Memory;Cache=Shared";
        ConnectionString = connectionString.Replace("{guid}", Guid.NewGuid().ToString("N"), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Mapa s dokumentima korisnika (apsolutna putanja).</summary>
    public string DirektorijDokumenata { get; }

    /// <summary>Mapa u koju se spremaju poslane e-poruke (apsolutna putanja).</summary>
    public string DirektorijPoste { get; }

    public string[] DozvoljeniOrigini { get; }

    /// <summary>Adresa aplikacije za poveznice u e-porukama.</summary>
    public string JavniUrl { get; }

    public string ConnectionString { get; }

    private static string PunaPutanja(string putanja, string osnova) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(putanja, osnova));
}
