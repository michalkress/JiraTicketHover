using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VsJiraTicketTooltip.Core.Interfaces;
using VsJiraTicketTooltip.Core.Models;

namespace VsJiraTicketTooltip.Core.Providers;

/// <summary>
/// Implementacja <see cref="ITicketProvider"/> dla Atlassian Jira Cloud.
/// Pobiera dane ticketu przez Jira REST API v3 z autoryzacją OAuth2.
/// </summary>
public class JiraProvider : ITicketProvider
{
    private const int TimeoutMs = 5000;

    private readonly IJiraOAuthService _oauthService;
    private readonly string _jiraInstanceUrl;
    private readonly ILogger<JiraProvider> _logger;

    /// <inheritdoc />
    public string ProviderName => "Jira";

    /// <summary>
    /// Inicjalizuje providera Jira.
    /// </summary>
    /// <param name="oauthService">Serwis OAuth2 do autoryzacji żądań.</param>
    /// <param name="jiraInstanceUrl">Bazowy URL instancji Jira (np. https://mycompany.atlassian.net).</param>
    /// <param name="logger">Logger do rejestrowania zdarzeń.</param>
    public JiraProvider(IJiraOAuthService oauthService, string jiraInstanceUrl, ILogger<JiraProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(oauthService);
        ArgumentNullException.ThrowIfNull(jiraInstanceUrl);
        ArgumentNullException.ThrowIfNull(logger);

        _oauthService = oauthService;
        _jiraInstanceUrl = jiraInstanceUrl.TrimEnd('/');
        _logger = logger;
    }

    /// <inheritdoc />
    /// <exception cref="KeyNotFoundException">Gdy ticket o podanym kluczu nie istnieje (HTTP 404).</exception>
    /// <exception cref="UnauthorizedAccessException">Gdy autoryzacja nie powiodła się nawet po odświeżeniu tokenu.</exception>
    /// <exception cref="HttpRequestException">Gdy serwer zwrócił błąd 5xx.</exception>
    /// <exception cref="OperationCanceledException">Gdy żądanie zostało anulowane lub przekroczyło timeout.</exception>
    public async Task<TicketData> FetchAsync(string ticketKey, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticketKey);

        _logger.LogDebug("Fetching ticket {TicketKey} from Jira", ticketKey);

        // 1. Upewnij się, że token jest ważny
        await _oauthService.EnsureValidTokenAsync(cancellationToken).ConfigureAwait(false);

        // 2. Pobierz Cloud ID
        string? cloudId = _oauthService.GetCloudId();
        if (string.IsNullOrEmpty(cloudId))
        {
            throw new InvalidOperationException("Cloud ID is not available. Please re-authorize the application.");
        }

        // 3. Zbuduj URL API
        string apiUrl = $"https://api.atlassian.com/ex/jira/{cloudId}/rest/api/3/issue/{ticketKey}?fields=summary";

        // 4. Wywołaj API z timeoutem 5000ms
        HttpResponseMessage response = await CallWithTimeoutAsync(apiUrl, cancellationToken).ConfigureAwait(false);

        // 5. Mapuj odpowiedź HTTP
        return await HandleResponseAsync(response, ticketKey, apiUrl, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> CallWithTimeoutAsync(string url, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(TimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        return await _oauthService.CallJiraApiAsync(url, linkedCts.Token).ConfigureAwait(false);
    }

    private async Task<TicketData> HandleResponseAsync(
        HttpResponseMessage response,
        string ticketKey,
        string apiUrl,
        CancellationToken cancellationToken)
    {
        int statusCode = (int)response.StatusCode;

        if (statusCode == 200)
        {
            return await ParseSuccessResponseAsync(response, ticketKey, cancellationToken).ConfigureAwait(false);
        }

        if (statusCode is 401 or 403)
        {
            return await HandleUnauthorizedAsync(ticketKey, apiUrl, response.StatusCode, cancellationToken)
                .ConfigureAwait(false);
        }

        if (statusCode == 404)
        {
            _logger.LogWarning("Ticket {TicketKey} not found in Jira", ticketKey);
            throw new KeyNotFoundException($"Ticket not found: {ticketKey}");
        }

        if (statusCode >= 500)
        {
            string errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError("Jira API returned server error {StatusCode} for ticket {TicketKey}: {Body}",
                statusCode, ticketKey, errorBody);
            throw new HttpRequestException(
                $"Jira API returned server error {statusCode} for ticket {ticketKey}.",
                null,
                response.StatusCode);
        }

        // Nieoczekiwany status
        string unexpectedBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogError("Jira API returned unexpected status {StatusCode} for ticket {TicketKey}: {Body}",
            statusCode, ticketKey, unexpectedBody);
        throw new HttpRequestException(
            $"Jira API returned unexpected status {statusCode} for ticket {ticketKey}.",
            null,
            response.StatusCode);
    }

    private async Task<TicketData> HandleUnauthorizedAsync(
        string ticketKey,
        string apiUrl,
        HttpStatusCode originalStatus,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Received {StatusCode} for ticket {TicketKey}, attempting token refresh",
            (int)originalStatus, ticketKey);

        // Odśwież token
        await _oauthService.RefreshAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        // Jeden retry
        HttpResponseMessage retryResponse = await CallWithTimeoutAsync(apiUrl, cancellationToken).ConfigureAwait(false);

        if (retryResponse.StatusCode == HttpStatusCode.OK)
        {
            return await ParseSuccessResponseAsync(retryResponse, ticketKey, cancellationToken).ConfigureAwait(false);
        }

        if (retryResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _logger.LogError("Jira API returned {StatusCode} after token refresh for ticket {TicketKey}",
                (int)retryResponse.StatusCode, ticketKey);
            throw new UnauthorizedAccessException(
                $"Jira API returned {(int)retryResponse.StatusCode} after token refresh for ticket {ticketKey}.");
        }

        // Inny błąd po retry — obsłuż normalnie
        return await HandleResponseAsync(retryResponse, ticketKey, apiUrl, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TicketData> ParseSuccessResponseAsync(
        HttpResponseMessage response,
        string ticketKey,
        CancellationToken cancellationToken)
    {
        string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        using var document = JsonDocument.Parse(json);
        string summary = document.RootElement
            .GetProperty("fields")
            .GetProperty("summary")
            .GetString() ?? string.Empty;

        string ticketUrl = $"{_jiraInstanceUrl}/browse/{ticketKey}";

        _logger.LogDebug("Successfully fetched ticket {TicketKey}: {Summary}", ticketKey, summary);

        return new TicketData(ticketKey, summary, ticketUrl);
    }
}
