using VsJiraTicketTooltip.Core.Models;

namespace VsJiraTicketTooltip.Core.Interfaces;

/// <summary>
/// Fasada łącząca cache z rejestrem providerów. Jedyny punkt wejścia dla warstwy edytora
/// do pobierania danych ticketów.
/// </summary>
public interface ITicketDataService
{
    /// <summary>
    /// Pobiera dane ticketu — najpierw sprawdza cache, przy cache miss odpytuje aktywnego providera.
    /// </summary>
    /// <param name="ticketKey">Klucz ticketu w formacie XXXX-NUMBER (np. "ABC-123").</param>
    /// <param name="cancellationToken">Token anulowania operacji.</param>
    /// <returns>
    /// Discriminated union reprezentujący wynik: sukces z danymi lub jeden z wariantów błędu.
    /// </returns>
    Task<TicketDataResult> GetTicketDataAsync(string ticketKey, CancellationToken cancellationToken);
}
