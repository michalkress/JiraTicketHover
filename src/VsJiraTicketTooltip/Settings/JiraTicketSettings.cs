#pragma warning disable VSEXTPREVIEW_SETTINGS

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Settings;

namespace VsJiraTicketTooltip.Settings;

/// <summary>
/// Definicje ustawień wtyczki Jira Ticket Tooltip.
/// Widoczne w Tools → Options → Jira Ticket Tooltip.
/// </summary>
internal static class JiraTicketSettings
{
    /// <summary>
    /// Kategoria główna ustawień w Tools → Options.
    /// </summary>
    [VisualStudioContribution]
    internal static SettingCategory JiraTicketTooltipCategory { get; } =
        new("jiraTicketTooltip", "%VsJiraTicketTooltip.Settings.Category.DisplayName%")
        {
            Description = "%VsJiraTicketTooltip.Settings.Category.Description%",
            GenerateObserverClass = true,
        };

    /// <summary>
    /// Włącz/wyłącz wtyczkę.
    /// </summary>
    [VisualStudioContribution]
    internal static Setting.Boolean IsEnabled { get; } =
        new("isEnabled", "%VsJiraTicketTooltip.Settings.IsEnabled.DisplayName%", JiraTicketTooltipCategory, defaultValue: true)
        {
            Description = "%VsJiraTicketTooltip.Settings.IsEnabled.Description%",
        };

    /// <summary>
    /// URL instancji Jira (np. https://mycompany.atlassian.net).
    /// </summary>
    [VisualStudioContribution]
    internal static Setting.String JiraInstanceUrl { get; } =
        new("jiraInstanceUrl", "%VsJiraTicketTooltip.Settings.JiraInstanceUrl.DisplayName%", JiraTicketTooltipCategory, defaultValue: "")
        {
            Description = "%VsJiraTicketTooltip.Settings.JiraInstanceUrl.Description%",
        };

    /// <summary>
    /// OAuth2 Client ID z Atlassian Developer Console.
    /// </summary>
    [VisualStudioContribution]
    internal static Setting.String OAuthClientId { get; } =
        new("oauthClientId", "%VsJiraTicketTooltip.Settings.OAuthClientId.DisplayName%", JiraTicketTooltipCategory, defaultValue: "")
        {
            Description = "%VsJiraTicketTooltip.Settings.OAuthClientId.Description%",
        };

    /// <summary>
    /// Aktywny provider ticketów.
    /// </summary>
    [VisualStudioContribution]
    internal static Setting.Enum ActiveProvider { get; } =
        new(
            "activeProvider",
            "%VsJiraTicketTooltip.Settings.ActiveProvider.DisplayName%",
            JiraTicketTooltipCategory,
            [new EnumSettingEntry("Jira", "%VsJiraTicketTooltip.Settings.ActiveProvider.Jira%")],
            defaultValue: "Jira")
        {
            Description = "%VsJiraTicketTooltip.Settings.ActiveProvider.Description%",
        };

    /// <summary>
    /// Prefiksy projektów rozdzielone przecinkiem (np. PROJ, PR, AMU).
    /// Tylko tickety z tymi prefiksami będą rozpoznawane.
    /// Puste = rozpoznawaj wszystkie wzorce [A-Z]+-[0-9]+.
    /// </summary>
    [VisualStudioContribution]
    internal static Setting.String ProjectPrefixes { get; } =
        new("projectPrefixes", "%VsJiraTicketTooltip.Settings.ProjectPrefixes.DisplayName%", JiraTicketTooltipCategory, defaultValue: "")
        {
            Description = "%VsJiraTicketTooltip.Settings.ProjectPrefixes.Description%",
        };
}
