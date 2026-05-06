using VsJiraTicketTooltip.Core.Models;

namespace VsJiraTicketTooltip.Core.Interfaces;

/// <summary>
/// LRU cache z TTL dla danych ticketów. Thread-safe.
/// Maksymalnie 500 wpisów, TTL 300 sekund.
/// </summary>
public interface ITicketCache
{
    /// <summary>
    /// Próbuje pobrać dane ticketu z cache.
    /// </summary>
    /// <param name="key">Klucz ticketu (np. "ABC-123").</param>
    /// <param name="data">Dane ticketu jeśli wpis istnieje i nie wygasł.</param>
    /// <returns>
    /// <c>true</c> jeśli wpis istnieje w cache i nie przekroczył TTL; <c>false</c> w przeciwnym razie.
    /// </returns>
    bool TryGet(string key, out TicketData? data);

    /// <summary>
    /// Zapisuje dane ticketu w cache. Jeśli cache jest pełny (500 wpisów),
    /// usuwa najdawniej używany wpis (LRU).
    /// </summary>
    /// <param name="key">Klucz ticketu (np. "ABC-123").</param>
    /// <param name="data">Dane ticketu do zapisania.</param>
    void Set(string key, TicketData data);

    /// <summary>
    /// Usuwa wpis o podanym kluczu z cache. Jeśli wpis nie istnieje, operacja jest ignorowana.
    /// </summary>
    /// <param name="key">Klucz ticketu do usunięcia.</param>
    void Invalidate(string key);

    /// <summary>
    /// Usuwa wszystkie wpisy z cache.
    /// </summary>
    void Clear();
}
