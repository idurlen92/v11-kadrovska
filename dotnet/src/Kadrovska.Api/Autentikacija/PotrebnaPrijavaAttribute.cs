using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kadrovska.Api.Autentikacija;

/// <summary>
/// Akcija je dostupna samo prijavljenom korisniku (inače 401). Ako su navedene uloge, korisnik mora
/// imati jednu od njih (inače 403).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PotrebnaPrijavaAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _uloge;

    public PotrebnaPrijavaAttribute(params string[] uloge)
    {
        _uloge = uloge;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var korisnik = context.HttpContext.TrenutniKorisnik();
        if (korisnik is null)
        {
            context.Result = new ObjectResult(new { greska = "Potrebna je prijava." }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }
        if (_uloge.Length > 0 && !_uloge.Contains(korisnik.Uloga, StringComparer.Ordinal))
        {
            context.Result = new ObjectResult(new { greska = "Nemate ovlasti za ovu radnju." }) { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
