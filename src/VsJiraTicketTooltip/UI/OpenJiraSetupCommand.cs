using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.ToolWindows;

namespace VsJiraTicketTooltip.UI;

/// <summary>
/// Komenda otwierająca okno konfiguracji Jiry.
/// Dostępna przez: Extensions → Jira Ticket Tooltip → Configure Jira Connection
/// </summary>
[VisualStudioContribution]
internal class OpenJiraSetupCommand : Command
{
    public override CommandConfiguration CommandConfiguration => new("%VsJiraTicketTooltip.OpenJiraSetupCommand.DisplayName%")
    {
        Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu],
        Icon = new(ImageMoniker.KnownValues.Settings, IconSettings.IconAndText),
    };

    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        await Extensibility.Shell().ShowToolWindowAsync<JiraSetupWindow>(activate: true, cancellationToken);
    }
}
