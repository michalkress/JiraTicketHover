#pragma warning disable VSEXTPREVIEW_TAGGERS
#pragma warning disable VSEXTPREVIEW_CODELENS
#pragma warning disable VSEXTPREVIEW_SETTINGS

using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Editor;
using VsJiraTicketTooltip.Settings;

namespace VsJiraTicketTooltip.Editor;

[VisualStudioContribution]
internal class JiraTicketTaggerProvider : ExtensionPart,
    ITextViewTaggerProvider<CodeLensTag>,
    ITextViewChangedListener
{
    internal static readonly CodeElementKind JiraTicketCodeElementKind = "JiraTicket";

    private static readonly Regex TicketPattern =
        new(@"\b([A-Z]+-[0-9]+)\b", RegexOptions.Compiled);

    private readonly object _lock = new();
    private readonly Dictionary<Uri, List<JiraTicketTagger>> _taggers = new();
    private string[] _prefixes = [];

    public TextViewExtensionConfiguration TextViewExtensionConfiguration => new()
    {
        AppliesTo = [DocumentFilter.FromDocumentType(DocumentType.KnownValues.Code)],
    };

    public async Task<TextViewTagger<CodeLensTag>> CreateTaggerAsync(
        ITextViewSnapshot textView,
        CancellationToken cancellationToken)
    {
        // Odczytaj prefiksy z ustawień
        try
        {
            var settings = await Extensibility.Settings()
                .ReadEffectiveValuesAsync([JiraTicketSettings.ProjectPrefixes], cancellationToken);
            foreach (var kvp in settings)
            {
                var val = kvp.Value?.Value<string>() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(val))
                {
                    _prefixes = val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(p => p.ToUpperInvariant())
                        .ToArray();
                }
            }
        }
        catch { /* puste = wszystkie */ }

        var tagger = new JiraTicketTagger(this, textView.Document.Uri, TicketPattern, _prefixes);
        lock (_lock)
        {
            if (!_taggers.TryGetValue(textView.Document.Uri, out var list))
            {
                list = [];
                _taggers[textView.Document.Uri] = list;
            }
            list.Add(tagger);
        }
        return tagger;
    }

    public async Task TextViewChangedAsync(TextViewChangedArgs args, CancellationToken cancellationToken)
    {
        List<Task> tasks;
        lock (_lock)
        {
            if (!_taggers.TryGetValue(args.AfterTextView.Uri, out var list))
                return;
            tasks = list
                .Select(t => t.TextViewChangedAsync(args.AfterTextView, args.Edits, cancellationToken))
                .ToList();
        }
        await Task.WhenAll(tasks);
    }

    internal void RemoveTagger(Uri documentUri, JiraTicketTagger tagger)
    {
        lock (_lock)
        {
            if (_taggers.TryGetValue(documentUri, out var list))
            {
                list.Remove(tagger);
                if (list.Count == 0)
                    _taggers.Remove(documentUri);
            }
        }
    }
}
