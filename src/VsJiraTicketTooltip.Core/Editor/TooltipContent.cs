namespace VsJiraTicketTooltip.Core.Editor;

/// <summary>
/// Wynik budowania treści tooltipa — POCO bez zależności od VS SDK.
/// Przekazywany z warstwy Core do warstwy edytora (JiraQuickInfoSource).
/// </summary>
/// <param name="TicketKey">Klucz ticketu w formacie XXXX-NUMBER (np. "ABC-123").</param>
/// <param name="Title">Tytuł ticketu; null gdy wystąpił błąd.</param>
/// <param name="Url">Pełny URL do ticketu; null gdy wystąpił błąd.</param>
/// <param name="IsError">True gdy wynik reprezentuje błąd (brak danych lub problem z pobieraniem).</param>
/// <param name="ErrorMessage">Czytelny komunikat błędu; pusty string gdy brak błędu.</param>
public record TooltipContent(
    string TicketKey,
    string? Title,
    string? Url,
    bool IsError,
    string ErrorMessage
);
