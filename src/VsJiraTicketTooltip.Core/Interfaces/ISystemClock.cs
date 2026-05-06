namespace VsJiraTicketTooltip.Core.Interfaces;

/// <summary>
/// Abstrakcja nad <see cref="DateTime.UtcNow"/> umożliwiająca kontrolę czasu w testach.
/// Wstrzykiwana do komponentów zależnych od czasu (np. <c>TicketCache</c> dla TTL).
/// </summary>
public interface ISystemClock
{
    /// <summary>
    /// Zwraca aktualny czas UTC.
    /// </summary>
    DateTime UtcNow { get; }
}
