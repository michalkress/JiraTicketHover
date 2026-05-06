#pragma warning disable VSEXTPREVIEW_CODELENS
#pragma warning disable VSEXTPREVIEW_SETTINGS

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Editor;
using VsJiraTicketTooltip.Core.Interfaces;
using VsJiraTicketTooltip.Settings;

namespace VsJiraTicketTooltip.Editor;

/// <summary>
/// Provider CodeLens dla identyfikatorów ticketów Jira.
/// Zbiera wszystkie tickety z linii i tworzy jeden VisualCodeLens z listą.
/// </summary>
[VisualStudioContribution]
internal class JiraTicketCodeLensProvider : ExtensionPart, ICodeLensProvider
{
    private readonly ITicketDataService _ticketDataService;

    public JiraTicketCodeLensProvider(ITicketDataService ticketDataService)
    {
        _ticketDataService = ticketDataService;
    }

    public TextViewExtensionConfiguration TextViewExtensionConfiguration => new()
    {
        AppliesTo = [DocumentFilter.FromDocumentType(DocumentType.KnownValues.Code)],
    };

#pragma warning disable CEE0027
    public CodeLensProviderConfiguration CodeLensProviderConfiguration =>
        new("Jira Ticket");
#pragma warning restore CEE0027

    public async Task<CodeLens?> TryCreateCodeLensAsync(
        CodeElement codeElement,
        CodeElementContext codeElementContext,
        CancellationToken token)
    {
        if (codeElement.Kind != JiraTicketTaggerProvider.JiraTicketCodeElementKind)
            return null;

        var identifier = codeElement.UniqueIdentifier ?? string.Empty;
        if (string.IsNullOrEmpty(identifier))
            return null;

        var ticketKeys = identifier.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (ticketKeys.Length == 0)
            return null;

        // Pobierz Jira URL z ustawień
        string jiraUrl = "https://jira.atlassian.net";
        try
        {
            var settings = await Extensibility.Settings()
                .ReadEffectiveValuesAsync([JiraTicketSettings.JiraInstanceUrl], token);
            foreach (var kvp in settings)
            {
                var val = kvp.Value?.Value<string>() ?? string.Empty;
                if (!string.IsNullOrEmpty(val) && val.StartsWith("https://"))
                    jiraUrl = val;
            }
        }
        catch { /* użyj domyślnego */ }

        return new JiraTicketCodeLens(ticketKeys, _ticketDataService, jiraUrl);
    }
}
