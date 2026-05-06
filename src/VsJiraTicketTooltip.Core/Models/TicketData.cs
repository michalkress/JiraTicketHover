namespace VsJiraTicketTooltip.Core.Models;

/// <summary>
/// Dane ticketu pobrane od providera.
/// </summary>
/// <param name="Key">Klucz ticketu w formacie XXXX-NUMBER (np. "ABC-123").</param>
/// <param name="Title">Tytuł ticketu.</param>
/// <param name="Url">Pełny URL do ticketu w systemie zewnętrznym.</param>
public record TicketData(
    string Key,
    string Title,
    string Url
);
