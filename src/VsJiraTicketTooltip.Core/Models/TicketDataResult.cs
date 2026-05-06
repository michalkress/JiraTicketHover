namespace VsJiraTicketTooltip.Core.Models;

/// <summary>
/// Discriminated union reprezentujący wynik pobierania danych ticketu.
/// Warstwa edytora nigdy nie widzi surowych wyjątków — wszystkie błędy są mapowane na ten typ.
/// </summary>
public abstract record TicketDataResult
{
    private TicketDataResult() { }

    /// <summary>
    /// Dane ticketu zostały pomyślnie pobrane.
    /// </summary>
    /// <param name="Data">Pobrane dane ticketu.</param>
    public sealed record Success(TicketData Data) : TicketDataResult;

    /// <summary>
    /// Ticket o podanym kluczu nie istnieje w systemie zewnętrznym (HTTP 404).
    /// </summary>
    /// <param name="Key">Klucz ticketu, który nie został znaleziony.</param>
    public sealed record NotFound(string Key) : TicketDataResult;

    /// <summary>
    /// Brak autoryzacji lub niewystarczające uprawnienia (HTTP 401/403 po próbie odświeżenia tokenu).
    /// </summary>
    public sealed record Unauthorized() : TicketDataResult;

    /// <summary>
    /// Żądanie przekroczyło limit czasu (5000ms).
    /// </summary>
    /// <param name="Key">Klucz ticketu, dla którego wystąpił timeout.</param>
    public sealed record Timeout(string Key) : TicketDataResult;

    /// <summary>
    /// Błąd po stronie serwisu zewnętrznego (HTTP 5xx lub błąd sieci).
    /// </summary>
    /// <param name="Message">Opis błędu.</param>
    public sealed record ServiceError(string Message) : TicketDataResult;

    /// <summary>
    /// Żaden provider nie jest skonfigurowany lub aktywny w <c>ProviderRegistry</c>.
    /// </summary>
    public sealed record ProviderNotConfigured() : TicketDataResult;
}
