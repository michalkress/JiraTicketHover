using VsJiraTicketTooltip.Core.Models;

namespace VsJiraTicketTooltip.Core.Editor;

/// <summary>
/// Buduje treść tooltipa na podstawie wyniku pobierania danych ticketu.
/// Czysta logika bez zależności od VS SDK — testowalny w izolacji.
/// </summary>
public static class TooltipContentBuilder
{
    /// <summary>
    /// Buduje <see cref="TooltipContent"/> na podstawie klucza ticketu i wyniku z <see cref="ITicketDataService"/>.
    /// </summary>
    /// <param name="ticketKey">Klucz ticketu w formacie XXXX-NUMBER (np. "ABC-123").</param>
    /// <param name="result">Wynik pobierania danych ticketu.</param>
    /// <returns>Gotowa treść tooltipa do wyświetlenia w edytorze.</returns>
    public static TooltipContent Build(string ticketKey, TicketDataResult result)
    {
        return result switch
        {
            TicketDataResult.Success s => new TooltipContent(
                ticketKey,
                s.Data.Title,
                s.Data.Url,
                false,
                string.Empty),

            TicketDataResult.NotFound => new TooltipContent(
                ticketKey,
                null,
                null,
                true,
                "Ticket not found"),

            TicketDataResult.Unauthorized => new TooltipContent(
                ticketKey,
                null,
                null,
                true,
                "Authorization required — please re-authorize in Tools → Options"),

            TicketDataResult.Timeout => new TooltipContent(
                ticketKey,
                null,
                null,
                true,
                "Request timed out"),

            TicketDataResult.ServiceError e => new TooltipContent(
                ticketKey,
                null,
                null,
                true,
                $"Service unavailable: {e.Message}"),

            TicketDataResult.ProviderNotConfigured => new TooltipContent(
                ticketKey,
                null,
                null,
                true,
                "No provider configured — please configure in Tools → Options"),

            _ => new TooltipContent(
                ticketKey,
                null,
                null,
                true,
                "Unknown error")
        };
    }
}
