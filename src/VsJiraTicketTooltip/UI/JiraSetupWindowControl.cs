using Microsoft.VisualStudio.Extensibility.UI;

namespace VsJiraTicketTooltip.UI;

/// <summary>
/// Remote UI control dla okna konfiguracji Jiry.
/// </summary>
internal class JiraSetupWindowControl : RemoteUserControl
{
    public JiraSetupWindowControl(JiraSetupWindowData dataContext)
        : base(dataContext)
    {
    }
}
