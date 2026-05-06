using System.Text.RegularExpressions;

namespace VsJiraTicketTooltip.Core.Editor;

/// <summary>
/// Czysta logika skanowania tekstu w poszukiwaniu identyfikatorów ticketów Jira.
/// Nie zawiera żadnych zależności od Visual Studio SDK — w pełni testowalna.
/// </summary>
public static class TicketIdentifierScanner
{
    // Kompilowany statycznie dla wydajności w hot path tagowania
    private static readonly Regex TicketPattern =
        new(@"\b([A-Z]+-[0-9]+)\b", RegexOptions.Compiled);

    /// <summary>
    /// Skanuje podany tekst w poszukiwaniu identyfikatorów ticketów w formacie <c>[A-Z]+-[0-9]+</c>.
    /// </summary>
    /// <param name="text">Tekst do przeskanowania (np. zawartość komentarza).</param>
    /// <returns>
    /// Lista dopasowań. Każde dopasowanie zawiera:
    /// <list type="bullet">
    ///   <item><description><c>Start</c> — indeks pierwszego znaku identyfikatora w tekście wejściowym</description></item>
    ///   <item><description><c>Length</c> — długość identyfikatora w znakach</description></item>
    ///   <item><description><c>Key</c> — sam identyfikator, np. <c>"ABC-123"</c></description></item>
    /// </list>
    /// </returns>
    public static IReadOnlyList<(int Start, int Length, string Key)> ScanForTicketIdentifiers(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<(int, int, string)>();

        var matches = TicketPattern.Matches(text);

        if (matches.Count == 0)
            return Array.Empty<(int, int, string)>();

        var result = new List<(int Start, int Length, string Key)>(matches.Count);

        foreach (Match match in matches)
        {
            // Grupa 1 zawiera identyfikator bez word boundary — używamy całego dopasowania
            result.Add((match.Index, match.Length, match.Value));
        }

        return result;
    }
}
