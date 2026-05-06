#pragma warning disable VSEXTPREVIEW_SETTINGS

using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Settings;
using VsJiraTicketTooltip.Core.Settings;
using VsJiraTicketTooltip.Settings;
using VsJiraTicketTooltip.UI;

namespace VsJiraTicketTooltip;

/// <summary>
/// Punkt wejścia wtyczki Jira Ticket Tooltip.
/// Inicjalizuje composition root i rejestruje serwisy w DI.
/// </summary>
[VisualStudioContribution]
public class JiraTicketTooltipExtension : Extension
{
    /// <inheritdoc />
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        Metadata = new(
            id: "VsJiraTicketTooltip.JiraTicketTooltip",
            version: ExtensionAssemblyVersion,
            publisherName: "VsJiraTicketTooltip",
            displayName: "Jira Ticket Tooltip",
            description: "Displays Jira ticket info in a CodeLens when hovering over ticket identifiers (e.g. ABC-123) in code comments.")
    };

    /// <inheritdoc />
    protected override void InitializeServices(IServiceCollection serviceCollection)
    {
        base.InitializeServices(serviceCollection);

        // Rejestruj obserwatorów ustawień (generowane przez SDK z GenerateObserverClass = true)
        serviceCollection.AddSettingsObservers();

        // Utwórz composition root z domyślnymi ustawieniami
        // Rzeczywiste ustawienia zostaną załadowane przez SettingsObserver po inicjalizacji
        var initialSettings = new ExtensionSettings();

#pragma warning disable CA1416
        var compositionRoot = new ExtensionCompositionRoot(initialSettings);
#pragma warning restore CA1416

        serviceCollection.AddSingleton(compositionRoot.TicketDataService);
        serviceCollection.AddSingleton(compositionRoot.SettingsObserver);
        serviceCollection.AddSingleton(compositionRoot.ProviderRegistry);

        // Synchronizuje ustawienia VS → Core SettingsObserver
        serviceCollection.AddSingleton<VsSettingsSynchronizer>();

        // UI — okno konfiguracji OAuth2
        serviceCollection.AddSingleton<JiraSetupWindowData>();

        //MessageBox.Show("dddd1");
    }
}
