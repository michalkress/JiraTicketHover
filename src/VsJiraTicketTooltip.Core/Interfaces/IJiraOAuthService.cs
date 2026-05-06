namespace VsJiraTicketTooltip.Core.Interfaces;

/// <summary>
/// Abstrakcja serwisu OAuth2 dla Atlassian Jira Cloud.
/// Umożliwia mockowanie w testach jednostkowych.
/// </summary>
public interface IJiraOAuthService
{
    /// <summary>
    /// Zapewnia, że token dostępu jest ważny. Odświeża go jeśli wygasł.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task EnsureValidTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca aktualny token dostępu.
    /// </summary>
    /// <returns>Token dostępu lub <c>null</c> jeśli nie istnieje.</returns>
    string? GetAccessToken();

    /// <summary>
    /// Zwraca Cloud ID z magazynu poświadczeń.
    /// </summary>
    /// <returns>Cloud ID lub <c>null</c> jeśli nie istnieje.</returns>
    string? GetCloudId();

    /// <summary>
    /// Wywołuje Jira API z aktualnym tokenem dostępu.
    /// </summary>
    /// <param name="url">URL endpointu API.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Odpowiedź HTTP.</returns>
    Task<HttpResponseMessage> CallJiraApiAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Odświeża token dostępu używając tokenu odświeżania.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns><c>true</c> jeśli token był ważny i nie wymagał odświeżenia; <c>false</c> jeśli odświeżono.</returns>
    Task<bool> RefreshAccessTokenAsync(CancellationToken cancellationToken = default);
}
