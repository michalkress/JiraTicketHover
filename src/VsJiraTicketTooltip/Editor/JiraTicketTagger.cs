#pragma warning disable VSEXTPREVIEW_TAGGERS
#pragma warning disable VSEXTPREVIEW_CODELENS

using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Extensibility.Editor;
using Microsoft.VisualStudio.Threading;

namespace VsJiraTicketTooltip.Editor;

internal class JiraTicketTagger : TextViewTagger<CodeLensTag>
{
    private readonly JiraTicketTaggerProvider _provider;
    private readonly Uri _documentUri;
    private readonly Regex _pattern;
    private readonly string[] _prefixes;
    private readonly AsyncSemaphore _semaphore = new(1);

    private ITextDocumentSnapshot? _currentSnapshot;
    private bool _needsUpdate;
    private bool _updateRunning;

    public JiraTicketTagger(JiraTicketTaggerProvider provider, Uri documentUri, Regex pattern, string[] prefixes)
    {
        _provider = provider;
        _documentUri = documentUri;
        _pattern = pattern;
        _prefixes = prefixes;
    }

    public override void Dispose()
    {
        _provider.RemoveTagger(_documentUri, this);
        _semaphore.Dispose();
        base.Dispose();
    }

    public async Task TextViewChangedAsync(ITextViewSnapshot textView, IReadOnlyList<TextEdit> edits, CancellationToken ct)
    {
        if (edits.Count == 0) return;
        using var sem = await _semaphore.EnterAsync(ct);
        var doc = textView.Document;
        if (_currentSnapshot is null || _currentSnapshot.RpcContract.Version < doc.RpcContract.Version)
        {
            _currentSnapshot = doc;
            if (!_needsUpdate) _ = RunCreateTagsAsync();
        }
    }

    protected override async Task RequestTagsAsync(NormalizedTextRangeCollection requestedRanges, bool recalculateAll, CancellationToken ct)
    {
        if (requestedRanges.Count == 0 || requestedRanges.TextDocumentSnapshot is null) return;
        using var sem = await _semaphore.EnterAsync(ct);
        var doc = requestedRanges.TextDocumentSnapshot;
        if (recalculateAll || _currentSnapshot is null || _currentSnapshot.RpcContract.Version < doc.RpcContract.Version)
        {
            _currentSnapshot = doc;
            if (!_needsUpdate) _ = RunCreateTagsAsync();
        }
    }

    private async Task RunCreateTagsAsync()
    {
        _needsUpdate = true;
        if (_updateRunning) return;
        _updateRunning = true;

        while (true)
        {
            ITextDocumentSnapshot document;
            using (var sem = await _semaphore.EnterAsync())
            {
                if (!_needsUpdate || _currentSnapshot is null) { _updateRunning = false; return; }
                _needsUpdate = false;
                document = _currentSnapshot;
            }
            await CreateTagsAsync(document);
        }
    }

    private async Task CreateTagsAsync(ITextDocumentSnapshot document)
    {
        var tags = new List<TaggedTrackingTextRange<CodeLensTag>>();

        foreach (var line in document.Lines)
        {
            var lineText = line.Text.CopyToString();
            var matches = _pattern.Matches(lineText);
            if (matches.Count == 0) continue;

            // Filtruj po prefiksach (jeśli ustawione)
            var ticketKeys = matches
                .Select(m => m.Groups[1].Value)
                .Where(key => _prefixes.Length == 0 || _prefixes.Any(p => key.StartsWith(p + "-", StringComparison.OrdinalIgnoreCase)))
                .Distinct()
                .ToArray();

            if (ticketKeys.Length == 0) continue;

            var identifier = string.Join("|", ticketKeys);

            tags.Add(new(
                new(document, line.Text.Start, line.Text.Length, TextRangeTrackingMode.ExtendForwardAndBackward),
                new(JiraTicketTaggerProvider.JiraTicketCodeElementKind)
                {
                    UniqueIdentifier = identifier,
                    Description = $"Jira: {string.Join(", ", ticketKeys)}",
                    DisplayBeforeCreatingCodeLenses = true,
                    Properties = new() { ["TicketKeys"] = identifier },
                }));
        }

        await UpdateTagsAsync([new(document, 0, document.Length)], tags, CancellationToken.None);
    }
}
