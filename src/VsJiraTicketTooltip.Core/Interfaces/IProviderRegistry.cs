namespace VsJiraTicketTooltip.Core.Interfaces;

/// <summary>
/// Rejestr providerów ticketów. Przechowuje mapowanie nazwa → <see cref="ITicketProvider"/>
/// i udostępnia aktywnego providera na podstawie konfiguracji.
/// </summary>
public interface IProviderRegistry
{
    /// <summary>
    /// Rejestruje providera w rejestrze. Jeśli provider o tej samej nazwie już istnieje,
    /// zostaje zastąpiony.
    /// </summary>
    /// <param name="provider">Implementacja providera do zarejestrowania.</param>
    void Register(ITicketProvider provider);

    /// <summary>
    /// Zwraca aktualnie aktywnego providera.
    /// </summary>
    /// <returns>Aktywny provider.</returns>
    /// <exception cref="Exceptions.ProviderNotConfiguredException">
    /// Rzucany gdy żaden provider nie jest zarejestrowany lub żaden nie jest wybrany jako aktywny.
    /// </exception>
    ITicketProvider GetActiveProvider();

    /// <summary>
    /// Ustawia aktywnego providera na podstawie jego nazwy.
    /// </summary>
    /// <param name="providerName">Nazwa providera do aktywacji.</param>
    /// <exception cref="Exceptions.ProviderNotConfiguredException">
    /// Rzucany gdy provider o podanej nazwie nie jest zarejestrowany.
    /// </exception>
    void SetActiveProvider(string providerName);

    /// <summary>
    /// Zwraca listę nazw wszystkich zarejestrowanych providerów.
    /// </summary>
    IReadOnlyList<string> GetRegisteredProviderNames();
}
