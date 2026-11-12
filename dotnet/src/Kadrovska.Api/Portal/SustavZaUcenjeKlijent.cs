namespace Kadrovska.Api.Portal;

/// <summary>
/// Klijent sustava za e-učenje (LMS) vanjskog izvođača edukacija: zaposlenik prijavljen na edukaciju upisuje se
/// i u LMS. U lokalnom okruženju poziv se simulira uz uobičajeno mrežno kašnjenje.
/// </summary>
public sealed class SustavZaUcenjeKlijent(ILogger<SustavZaUcenjeKlijent> logger)
{
    private static readonly TimeSpan Kasnjenje = TimeSpan.FromMilliseconds(150);

    public async Task UpisiPolaznikaAsync(long edukacijaId, string zaposlenik, CancellationToken cancellationToken)
    {
        await Task.Delay(Kasnjenje, cancellationToken);
        logger.LogInformation("LMS: zaposlenik {Zaposlenik} upisan na edukaciju {EdukacijaId}", zaposlenik, edukacijaId);
    }
}
