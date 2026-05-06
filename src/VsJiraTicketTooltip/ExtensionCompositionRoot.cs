using Microsoft.Extensions.Logging.Abstractions;
using VsJiraTicketTooltip.Core.Cache;
using VsJiraTicketTooltip.Core.Clock;
using VsJiraTicketTooltip.Core.Credentials;
using VsJiraTicketTooltip.Core.Interfaces;
using VsJiraTicketTooltip.Core.Jira;
using VsJiraTicketTooltip.Core.Providers;
using VsJiraTicketTooltip.Core.Services;
using VsJiraTicketTooltip.Core.Settings;

namespace VsJiraTicketTooltip;

/// <summary>
/// Composition root wtyczki. Tworzy i łączy wszystkie serwisy.
/// W docelowej implementacji VSIX ta klasa będzie wywoływana z klasy Extension
/// dziedziczącej po Microsoft.VisualStudio.Extensibility.Extension.
/// </summary>
public class ExtensionCompositionRoot
{
    public ITicketDataService TicketDataService { get; }
    public SettingsObserver SettingsObserver { get; }
    public IProviderRegistry ProviderRegistry { get; }

    public ExtensionCompositionRoot(ExtensionSettings initialSettings)
    {
        ArgumentNullException.ThrowIfNull(initialSettings);

        // 1. Utwórz ICredentialStore
#pragma warning disable CA1416 // Validate platform compatibility — WindowsCredentialStore jest tylko dla Windows
        var credentialStore = new WindowsCredentialStore();

        // 2. Utwórz ISystemClock
        var clock = new SystemClock();

        // 3. Utwórz ITicketCache
        var cache = new TicketCache(clock);

        // 4. Utwórz JiraOAuthService
        string clientSecret = credentialStore.TryLoad("VsJiraTicketTooltip/ClientSecret", out _, out var secret)
            ? secret ?? ""
            : "";
#pragma warning restore CA1416

        var oauthService = new JiraOAuthService(
            initialSettings.OAuthClientId,
            clientSecret,
            credentialStore);

        // 5. Utwórz JiraProvider
        var jiraProvider = new JiraProvider(
            oauthService,
            initialSettings.JiraInstanceUrl,
            NullLogger<JiraProvider>.Instance);

        // 6. Utwórz ProviderRegistry i zarejestruj Jira jako domyślny provider
        var registry = new ProviderRegistry();
        registry.Register(jiraProvider);
        ProviderRegistry = registry;

        // 7. Utwórz TicketDataService
        TicketDataService = new TicketDataService(
            cache,
            registry,
            NullLogger<TicketDataService>.Instance);

        // 8. Utwórz SettingsObserver
        SettingsObserver = new SettingsObserver(registry, credentialStore, cache, initialSettings);
    }
}
