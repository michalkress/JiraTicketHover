using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;

namespace VsJiraTicketTooltip.UI;

/// <summary>
/// Tool Window do konfiguracji połączenia z Jirą.
/// Otwierany przez: Extensions → Configure Jira Connection...
/// </summary>
[VisualStudioContribution]
internal class JiraSetupWindow : ToolWindow
{
    public JiraSetupWindow()
    {
        Title = "Jira Ticket Tooltip — Setup";
    }

    public override ToolWindowConfiguration ToolWindowConfiguration => new()
    {
        Placement = ToolWindowPlacement.Floating,
        AllowAutoCreation = false,
    };

    public override Task<IRemoteUserControl> GetContentAsync(CancellationToken cancellationToken)
    {
        var data = new JiraSetupWindowData(Extensibility);
        return Task.FromResult<IRemoteUserControl>(new JiraSetupWindowControl(data));
    }
}
