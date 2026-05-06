#pragma warning disable VSEXTPREVIEW_SETTINGS

using VsJiraTicketTooltip.Core.Settings;
using VsJiraTicketTooltip.Settings.Settings;

namespace VsJiraTicketTooltip.Settings;

/// <summary>
/// Synchronizuje ustawienia z Visual Studio Settings API do Core SettingsObserver.
/// Reaguje na zmiany ustawień w Tools → Options → Jira Ticket Tooltip.
/// Klasy JiraTicketTooltipCategoryObserver i JiraTicketTooltipCategorySnapshot
/// są generowane przez SDK source generator na podstawie JiraTicketSettings.cs.
/// </summary>
internal class VsSettingsSynchronizer
{
    private readonly SettingsObserver _coreObserver;
    private readonly JiraTicketTooltipCategoryObserver _vsObserver;

    public VsSettingsSynchronizer(
        SettingsObserver coreObserver,
        JiraTicketTooltipCategoryObserver vsObserver)
    {
        _coreObserver = coreObserver;
        _vsObserver = vsObserver;

        _vsObserver.Changed += OnSettingsChangedAsync;
    }

    private Task OnSettingsChangedAsync(JiraTicketTooltipCategorySnapshot snapshot)
    {
        var newSettings = BuildSettings(snapshot);
        _coreObserver.OnSettingsChanged(newSettings);
        return Task.CompletedTask;
    }

    private static ExtensionSettings BuildSettings(JiraTicketTooltipCategorySnapshot snapshot)
    {
        return new ExtensionSettings
        {
            IsEnabled = snapshot.IsEnabled.ValueOrDefault(JiraTicketSettings.IsEnabled.DefaultValue),
            JiraInstanceUrl = snapshot.JiraInstanceUrl.ValueOrDefault(JiraTicketSettings.JiraInstanceUrl.DefaultValue),
            OAuthClientId = snapshot.OAuthClientId.ValueOrDefault(JiraTicketSettings.OAuthClientId.DefaultValue),
            ActiveProvider = snapshot.ActiveProvider.ValueOrDefault(JiraTicketSettings.ActiveProvider.DefaultValue),
        };
    }
}
