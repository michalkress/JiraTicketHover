using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VsJiraTicketTooltip.Core.Interfaces;

namespace VsJiraTicketTooltip.Core.Jira;

/// <summary>
/// Serwis obsługujący autoryzację OAuth2 dla Atlassian Jira Cloud.
/// Przechowuje tokeny bezpiecznie w <see cref="ICredentialStore"/> (Windows Credential Manager).
/// </summary>
public class JiraOAuthService : IJiraOAuthService
{
    // Klucze w CredentialStore
    private const string AccessTokenKey = "VsJiraTicketTooltip/AccessToken";
    private const string RefreshTokenKey = "VsJiraTicketTooltip/RefreshToken";
    private const string TokenExpiryKey = "VsJiraTicketTooltip/TokenExpiry";
    private const string CloudIdKey = "VsJiraTicketTooltip/CloudId";

    // Bufor 5 minut przed wygaśnięciem tokenu
    private static readonly TimeSpan TokenExpiryBuffer = TimeSpan.FromMinutes(5);

    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly ICredentialStore _credentialStore;
    private readonly string _redirectUri;
    private readonly HttpClient _httpClient;
    private readonly ILogger<JiraOAuthService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Inicjalizuje serwis OAuth2 dla Jira.
    /// </summary>
    /// <param name="clientId">OAuth2 Client ID z Atlassian Developer Console.</param>
    /// <param name="clientSecret">OAuth2 Client Secret.</param>
    /// <param name="credentialStore">Magazyn poświadczeń do bezpiecznego przechowywania tokenów.</param>
    /// <param name="redirectUri">URI przekierowania po autoryzacji (domyślnie: http://localhost:9089/callback).</param>
    /// <param name="logger">Logger do rejestrowania zdarzeń OAuth2 (opcjonalny).</param>
    public JiraOAuthService(
        string clientId,
        string clientSecret,
        ICredentialStore credentialStore,
        string redirectUri = "http://localhost:9089/callback",
        ILogger<JiraOAuthService>? logger = null)
        : this(clientId, clientSecret, credentialStore, new HttpClient(), redirectUri, logger)
    {
    }

    /// <summary>
    /// Inicjalizuje serwis OAuth2 dla Jira z niestandardowym <see cref="HttpClient"/> (do testów).
    /// </summary>
    public JiraOAuthService(
        string clientId,
        string clientSecret,
        ICredentialStore credentialStore,
        HttpClient httpClient,
        string redirectUri = "http://localhost:9089/callback",
        ILogger<JiraOAuthService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(clientId);
        ArgumentNullException.ThrowIfNull(clientSecret);
        ArgumentNullException.ThrowIfNull(credentialStore);
        ArgumentNullException.ThrowIfNull(httpClient);

        _clientId = clientId;
        _clientSecret = clientSecret;
        _credentialStore = credentialStore;
        _httpClient = httpClient;
        _redirectUri = redirectUri;
        _logger = logger ?? NullLogger<JiraOAuthService>.Instance;
    }

    /// <summary>
    /// Generuje kryptograficznie losowy string stanu (CSRF protection).
    /// </summary>
    /// <returns>Losowy string Base64Url o długości 32 bajtów.</returns>
    public string GenerateState()
    {
        byte[] randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Zwraca URL autoryzacji OAuth2 wraz z wygenerowanym parametrem <c>state</c>.
    /// </summary>
    /// <returns>Tuple zawierający URL autoryzacji i wartość state do weryfikacji CSRF.</returns>
    public (string AuthorizationUrl, string State) GetAuthorizationUrl()
    {
        string state = GenerateState();

        var queryParams = HttpUtility.ParseQueryString(string.Empty);
        queryParams["audience"] = "api.atlassian.com";
        queryParams["client_id"] = _clientId;
        queryParams["scope"] = "read:jira-work offline_access";
        queryParams["redirect_uri"] = _redirectUri;
        queryParams["state"] = state;
        queryParams["response_type"] = "code";
        queryParams["prompt"] = "consent";

        string url = $"https://auth.atlassian.com/authorize?{queryParams}";
        return (url, state);
    }

    /// <summary>
    /// Wymienia kod autoryzacyjny na tokeny dostępu i odświeżania.
    /// Tokeny są zapisywane w <see cref="ICredentialStore"/>.
    /// </summary>
    /// <param name="code">Kod autoryzacyjny z callbacku OAuth2.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <exception cref="HttpRequestException">Gdy żądanie HTTP zakończy się błędem.</exception>
    public async Task ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);

        _logger.LogDebug("Exchanging authorization code for tokens");

        var requestBody = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", _redirectUri)
        });

        var response = await _httpClient
            .PostAsync("https://auth.atlassian.com/oauth/token", requestBody, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError("Token exchange failed with status {StatusCode}", (int)response.StatusCode);
            throw new HttpRequestException(
                $"Token exchange failed with status {(int)response.StatusCode}: {errorContent}",
                null,
                response.StatusCode);
        }

        var tokenResponse = await response.Content
            .ReadFromJsonAsync<OAuthTokenResponse>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (tokenResponse is null)
        {
            _logger.LogError("Token exchange returned empty response");
            throw new HttpRequestException("Token exchange returned empty response.");
        }

        await StoreTokensAsync(tokenResponse, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("OAuth2 authorization completed successfully");
    }

    /// <summary>
    /// Odświeża token dostępu używając tokenu odświeżania.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns><c>true</c> jeśli token był ważny i nie wymagał odświeżenia; <c>false</c> jeśli odświeżono.</returns>
    /// <exception cref="InvalidOperationException">Gdy brak tokenu odświeżania.</exception>
    /// <exception cref="HttpRequestException">Gdy żądanie HTTP zakończy się błędem.</exception>
    public async Task<bool> RefreshAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Sprawdź czy token jest jeszcze ważny
        if (_credentialStore.TryLoad(TokenExpiryKey, out _, out string? expiryStr)
            && expiryStr is not null
            && DateTime.TryParse(expiryStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime expiry)
            && expiry - TokenExpiryBuffer > DateTime.UtcNow)
        {
            return true; // Token ważny, nie trzeba odświeżać
        }

        // Pobierz refresh token
        if (!_credentialStore.TryLoad(RefreshTokenKey, out _, out string? refreshToken)
            || string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("No refresh token available — re-authorization required");
            throw new InvalidOperationException(
                "No refresh token available. Please re-authorize the application.");
        }

        _logger.LogDebug("Access token expired, refreshing using refresh token");

        var requestBody = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var response = await _httpClient
            .PostAsync("https://auth.atlassian.com/oauth/token", requestBody, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError("Token refresh failed with status {StatusCode}", (int)response.StatusCode);
            throw new HttpRequestException(
                $"Token refresh failed with status {(int)response.StatusCode}: {errorContent}",
                null,
                response.StatusCode);
        }

        var tokenResponse = await response.Content
            .ReadFromJsonAsync<OAuthTokenResponse>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (tokenResponse is null)
        {
            _logger.LogError("Token refresh returned empty response");
            throw new HttpRequestException("Token refresh returned empty response.");
        }

        await StoreTokensAsync(tokenResponse, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Access token refreshed successfully");
        return false;
    }

    /// <summary>
    /// Zapewnia, że token dostępu jest ważny. Odświeża go jeśli wygasł.
    /// Jeśli refresh token jest nieważny, inicjuje nowy flow OAuth2.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <exception cref="InvalidOperationException">Gdy brak tokenów i nie można zainicjować flow.</exception>
    public async Task EnsureValidTokenAsync(CancellationToken cancellationToken = default)
    {
        // Sprawdź czy mamy access token
        if (!_credentialStore.TryLoad(AccessTokenKey, out _, out string? accessToken)
            || string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("No access token available — authorization required");
            throw new InvalidOperationException(
                "No access token available. Please authorize the application first.");
        }

        // Sprawdź czy token jest ważny (z buforem 5 minut)
        if (_credentialStore.TryLoad(TokenExpiryKey, out _, out string? expiryStr)
            && expiryStr is not null
            && DateTime.TryParse(expiryStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime expiry)
            && expiry - TokenExpiryBuffer > DateTime.UtcNow)
        {
            return; // Token ważny
        }

        // Token wygasł — spróbuj odświeżyć
        try
        {
            await RefreshAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // Brak refresh tokenu — wymagana ponowna autoryzacja
            _logger.LogWarning("Access token expired and no refresh token available — re-authorization required");
            throw new InvalidOperationException(
                "Access token expired and no refresh token is available. Please re-authorize the application.");
        }
        catch (HttpRequestException ex) when (
            ex.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            // Refresh token nieważny — wymagana ponowna autoryzacja
            // Usuń stare tokeny
            _credentialStore.Delete(AccessTokenKey);
            _credentialStore.Delete(RefreshTokenKey);
            _credentialStore.Delete(TokenExpiryKey);
            _credentialStore.Delete(CloudIdKey);

            _logger.LogWarning("Refresh token expired or invalid — re-authorization required");
            throw new InvalidOperationException(
                "Refresh token is expired or invalid. Please re-authorize the application.", ex);
        }
    }

    /// <summary>
    /// Zwraca aktualny token dostępu z magazynu poświadczeń.
    /// </summary>
    /// <returns>Token dostępu lub <c>null</c> jeśli nie istnieje.</returns>
    public string? GetAccessToken()
    {
        _credentialStore.TryLoad(AccessTokenKey, out _, out string? accessToken);
        return accessToken;
    }

    /// <summary>
    /// Zwraca Cloud ID z magazynu poświadczeń.
    /// </summary>
    /// <returns>Cloud ID lub <c>null</c> jeśli nie istnieje.</returns>
    public string? GetCloudId()
    {
        _credentialStore.TryLoad(CloudIdKey, out _, out string? cloudId);
        return cloudId;
    }

    /// <summary>
    /// Wywołuje Jira API z aktualnym tokenem dostępu.
    /// </summary>
    /// <param name="url">URL endpointu API.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Odpowiedź HTTP.</returns>
    public async Task<HttpResponseMessage> CallJiraApiAsync(string url, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(url);

        string? accessToken = GetAccessToken();
        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogError("No access token available when calling Jira API — call EnsureValidTokenAsync first");
            throw new InvalidOperationException("No access token available. Call EnsureValidTokenAsync first.");
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Pobiera podsumowanie (tytuł) ticketu Jira.
    /// </summary>
    /// <param name="cloudId">Atlassian Cloud ID.</param>
    /// <param name="issueKey">Klucz ticketu, np. "ABC-123".</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Tytuł ticketu.</returns>
    public async Task<string> GetIssueSummaryAsync(
        string cloudId,
        string issueKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cloudId);
        ArgumentNullException.ThrowIfNull(issueKey);

        await EnsureValidTokenAsync(cancellationToken).ConfigureAwait(false);

        string url = $"https://api.atlassian.com/ex/jira/{cloudId}/rest/api/3/issue/{issueKey}?fields=summary";
        var response = await CallJiraApiAsync(url, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return document.RootElement
            .GetProperty("fields")
            .GetProperty("summary")
            .GetString() ?? string.Empty;
    }

    private async Task StoreTokensAsync(OAuthTokenResponse tokenResponse, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            _credentialStore.Save(AccessTokenKey, "jira_oauth", tokenResponse.AccessToken);
        }

        if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
        {
            _credentialStore.Save(RefreshTokenKey, "jira_oauth", tokenResponse.RefreshToken);
        }

        // Oblicz i zapisz czas wygaśnięcia
        DateTime expiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        _credentialStore.Save(TokenExpiryKey, "jira_oauth", expiry.ToString("O"));

        // Pobierz Cloud ID jeśli nie jest jeszcze zapisany
        if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            string? existingCloudId = GetCloudId();
            if (string.IsNullOrEmpty(existingCloudId))
            {
                try
                {
                    string? cloudId = await FetchCloudIdAsync(tokenResponse.AccessToken, cancellationToken)
                        .ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(cloudId))
                    {
                        _credentialStore.Save(CloudIdKey, "jira_oauth", cloudId);
                    }
                }
                catch
                {
                    // Ignoruj błędy pobierania Cloud ID — można spróbować później
                }
            }
        }
    }

    private async Task<string?> FetchCloudIdAsync(string accessToken, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.atlassian.com/oauth/token/accessible-resources");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (document.RootElement.ValueKind == JsonValueKind.Array
            && document.RootElement.GetArrayLength() > 0)
        {
            return document.RootElement[0].GetProperty("id").GetString();
        }

        return null;
    }

    /// <summary>
    /// Wewnętrzny model odpowiedzi z endpointu token OAuth2.
    /// </summary>
    private sealed class OAuthTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }
}
