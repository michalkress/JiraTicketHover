namespace VsJiraTicketTooltip.Core.Interfaces;

/// <summary>
/// Abstrakcja providera ticketów. Każda implementacja reprezentuje zewnętrzny system
/// zarządzania zadaniami (np. Jira, GitHub Issues, Azure DevOps).
/// </summary>
public interface ITicketProvider
{
    /// <summary>
    /// Unikalna nazwa providera używana do identyfikacji w <see cref="IProviderRegistry"/>.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Pobiera dane ticketu z zewnętrznego systemu.
    /// </summary>
    /// <param name="ticketKey">Klucz ticketu w formacie XXXX-NUMBER (np. "ABC-123").</param>
    /// <param name="cancellationToken">Token anulowania operacji.</param>
    /// <returns>Dane ticketu lub wyjątek w przypadku błędu.</returns>
    Task<Models.TicketData> FetchAsync(string ticketKey, CancellationToken cancellationToken);
}
