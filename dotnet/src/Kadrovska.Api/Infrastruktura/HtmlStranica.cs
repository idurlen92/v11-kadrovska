using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;

namespace Kadrovska.Api.Infrastruktura;

/// <summary>Zajednički izgled jednostavnih HTML stranica (prijava, potvrde za ispis).</summary>
public static class HtmlStranica
{
    public static ContentResult Odgovor(string naslov, string tijelo, int status = StatusCodes.Status200OK) => new()
    {
        Content = Izradi(naslov, tijelo),
        ContentType = "text/html; charset=utf-8",
        StatusCode = status,
    };

    public static string Izradi(string naslov, string tijelo) => $$"""
        <!DOCTYPE html>
        <html lang="hr">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>{{HtmlEncoder.Default.Encode(naslov)}} – {{Aplikacija.Naziv}}</title>
        <style>
        body { font-family: system-ui, sans-serif; max-width: 44rem; margin: 2rem auto; padding: 0 1rem; color: #1f2933; }
        h1 { font-size: 1.5rem; }
        table { border-collapse: collapse; width: 100%; }
        th, td { text-align: left; padding: .4rem .6rem; border-bottom: 1px solid #d9e2ec; vertical-align: top; }
        form { display: grid; gap: .6rem; max-width: 20rem; }
        .greska { color: #b42318; }
        .podnozje { margin-top: 2rem; font-size: .85rem; color: #627d98; }
        </style>
        </head>
        <body>
        {{tijelo}}
        <p class="podnozje">{{Aplikacija.Naziv}}</p>
        </body>
        </html>
        """;
}
