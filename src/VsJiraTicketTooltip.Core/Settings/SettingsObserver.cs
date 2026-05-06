using VsJiraTicketTooltip.Core.Interfaces;

namespace VsJiraTicketTooltip.Core.Settings;

/// <summary>
/// Obserwuje zmiany ustawień i reaguje na nie: unieważnia tokeny, czyści cache
/// i aktualizuje aktywnego providera.
/// </summary>
public class SettingsObserver
{
    // Klucze tokenów przechowywanych w ICredentialStore
    private const string AccessTokenKey = "VsJiraTicketTooltip/AccessToken";
    private const string RefreshTokenKey = "VsJiraTicketTooltip/RefreshToken";
    private const string TokenExpiryKey = "VsJiraTicketTooltip/TokenExpiry";
    private const string CloudIdKey = "VsJiraTicketTooltip/CloudId";

    private readonly IProviderRegistry _registry;
    private readonly ICredentialStore _credentialStore;
    private readonly ITicketCache _cache;
    private ExtensionSettings _currentSettings;

    /// <summary>
    /// Inicjalizuje obserwatora ustawień.
    /// </summary>
    /// <param name="registry">Rejestr providerów ticketów.</param>
    /// <param name="credentialStore">Magazyn poświadczeń (Windows Credential Manager).</param>
    /// <param name="cache">Cache danych ticketów.</param>
    /// <param name="initialSettings">Bieżące ustawienia przy starcie.</param>
    public SettingsObserver(
        IProviderRegistry registry,
        ICredentialStore credentialStore,
        ITicketCache cache,
        ExtensionSettings initialSettings)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _currentSettings = initialSettings ?? throw new ArgumentNullException(nameof(initialSettings));
    }

    /// <summary>
    /// Przetwarza zmianę ustawień. Wywołaj gdy ustawienia się zmienią.
    /// </summary>
    /// <remarks>
    /// Przy zmianie <see cref="ExtensionSettings.JiraInstanceUrl"/> lub
    /// <see cref="ExtensionSettings.OAuthClientId"/> unieważnia wszystkie tokeny
    /// w <see cref="ICredentialStore"/> i czyści cache.
    /// Przy zmianie <see cref="ExtensionSettings.ActiveProvider"/> aktualizuje
    /// aktywnego providera w <see cref="IProviderRegistry"/>.
    /// </remarks>
    /// <param name="newSettings">Nowe ustawienia.</param>
    public void OnSettingsChanged(ExtensionSettings newSettings)
    {
        ArgumentNullException.ThrowIfNull(newSettings);

        bool credentialsChanged =
            _currentSettings.JiraInstanceUrl != newSettings.JiraInstanceUrl ||
            _currentSettings.OAuthClientId != newSettings.OAuthClientId;

        if (credentialsChanged)
        {
            _credentialStore.Delete(AccessTokenKey);
            _credentialStore.Delete(RefreshTokenKey);
            _credentialStore.Delete(TokenExpiryKey);
            _credentialStore.Delete(CloudIdKey);
            _cache.Clear();
        }

        if (_currentSettings.ActiveProvider != newSettings.ActiveProvider)
        {
            _registry.SetActiveProvider(newSettings.ActiveProvider);
        }

        _currentSettings = newSettings;
    }

    /// <summary>
    /// Bieżące ustawienia wtyczki.
    /// </summary>
    public ExtensionSettings CurrentSettings => _currentSettings;
}
