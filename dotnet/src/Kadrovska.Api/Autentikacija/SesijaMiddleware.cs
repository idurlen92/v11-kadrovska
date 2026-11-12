namespace Kadrovska.Api.Autentikacija;

/// <summary>
/// Prepoznaje korisnika po tokenu iz zaglavlja <c>Authorization: Bearer &lt;token&gt;</c> ili iz kolačića
/// <c>SESSION</c>. Sam ne odbija zahtjeve – to radi <see cref="PotrebnaPrijavaAttribute"/>.
/// </summary>
public sealed class SesijaMiddleware(RequestDelegate next, SesijeKorisnika sesije)
{
    public const string NazivKolacica = "SESSION";
    private const string Bearer = "Bearer ";

    public Task InvokeAsync(HttpContext context)
    {
        var token = ProcitajToken(context.Request);
        if (token is not null && sesije.Pronadji(token) is { } korisnik)
        {
            context.PostaviKorisnika(korisnik);
        }
        return next(context);
    }

    private static string? ProcitajToken(HttpRequest request)
    {
        var zaglavlje = request.Headers.Authorization.ToString();
        if (zaglavlje.StartsWith(Bearer, StringComparison.OrdinalIgnoreCase))
        {
            return zaglavlje[Bearer.Length..].Trim();
        }
        return request.Cookies.TryGetValue(NazivKolacica, out var kolacic) && !string.IsNullOrEmpty(kolacic)
            ? kolacic
            : null;
    }
}
