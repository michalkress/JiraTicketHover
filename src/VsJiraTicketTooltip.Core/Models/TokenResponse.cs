namespace VsJiraTicketTooltip.Core.Models;

/// <summary>
/// Odpowiedź z endpointu token OAuth2 Atlassian Jira.
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// Token dostępu Bearer używany do autoryzacji żądań API.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Token odświeżania używany do uzyskania nowego tokenu dostępu bez interakcji użytkownika.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Czas ważności tokenu dostępu w sekundach.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Identyfikator instancji Jira Cloud (Atlassian Cloud ID).
    /// </summary>
    public string? CloudId { get; set; }
}
