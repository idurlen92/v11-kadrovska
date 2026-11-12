using System.Diagnostics.CodeAnalysis;

namespace Kadrovska.Api.Infrastruktura;

/// <summary>Osobni identifikacijski broj (11 znamenki).</summary>
public static class Oib
{
    public static bool JeIspravan([NotNullWhen(true)] string? oib) =>
        oib is { Length: 11 } && oib.All(char.IsAsciiDigit);

    /// <summary>Prikaz bez prvih znamenki, npr. <c>*******6504</c>.</summary>
    public static string Maskiraj(string oib) =>
        oib.Length <= 4 ? new string('*', oib.Length) : new string('*', oib.Length - 4) + oib[^4..];
}
