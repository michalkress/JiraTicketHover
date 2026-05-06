using VsJiraTicketTooltip.Core.Interfaces;

namespace VsJiraTicketTooltip.Core.Clock;

/// <summary>
/// Konkretna implementacja <see cref="ISystemClock"/> zwracająca rzeczywisty czas UTC.
/// </summary>
public sealed class SystemClock : ISystemClock
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
