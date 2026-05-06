#pragma warning disable VSEXTPREVIEW_CODELENS

using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Editor;
using Microsoft.VisualStudio.Extensibility.UI;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;
using VsJiraTicketTooltip.Core.Editor;
using VsJiraTicketTooltip.Core.Interfaces;

namespace VsJiraTicketTooltip.Editor;

/// <summary>
/// VisualCodeLens — label pokazuje klucze ticketów, kliknięcie rozwija popup z listą.
/// </summary>
internal class JiraTicketCodeLens : VisualCodeLens
{
    private readonly string[] _ticketKeys;
    private readonly ITicketDataService _ticketDataService;
    private readonly string _jiraInstanceUrl;
    private JiraTicketListData? _data;

    public JiraTicketCodeLens(string[] ticketKeys, ITicketDataService ticketDataService, string jiraInstanceUrl)
    {
        _ticketKeys = ticketKeys;
        _ticketDataService = ticketDataService;
        _jiraInstanceUrl = jiraInstanceUrl;
    }

    public override void Dispose() { }

    public override async Task<CodeLensLabel> GetLabelAsync(CodeElementContext codeElementContext, CancellationToken token)
    {
        var items = new List<JiraTicketItem>();
        foreach (var key in _ticketKeys)
        {
            var result = await _ticketDataService.GetTicketDataAsync(key, token);
            var content = TooltipContentBuilder.Build(key, result);
            var url = content.Url ?? $"{_jiraInstanceUrl}/browse/{key}";
            var title = content.IsError ? content.ErrorMessage : (content.Title ?? key);
            items.Add(new JiraTicketItem(key, title, url));
        }

        _data = new JiraTicketListData(items);

        // Label: 🎫 PROJ-1, PROJ-2 (zawsze wyświetlaj klucze)
        var label = "🎫 " + string.Join(", ", _ticketKeys);

        return new CodeLensLabel { Text = label, Tooltip = string.Join("\n", items.Select(i => $"{i.Key}: {i.Title}")) };
    }

    public override Task<IRemoteUserControl> GetVisualizationAsync(CodeElementContext codeElementContext, IClientContext clientContext, CancellationToken token)
    {
        _data ??= new JiraTicketListData([]);
        return Task.FromResult<IRemoteUserControl>(new JiraTicketListVisual(_data));
    }
}

/// <summary>
/// Dane dla popup listy ticketów. Zawiera listę i obsługuje komendy otwarcia.
/// </summary>
[DataContract]
internal class JiraTicketListData : NotifyPropertyChangedObject
{
    public JiraTicketListData(List<JiraTicketItem> items)
    {
        Items = items;
        OpenTicketCommand = new AsyncCommand(OpenTicketAsync);
    }

    [DataMember]
    public List<JiraTicketItem> Items { get; set; }

    /// <summary>
    /// Komenda wywoływana z XAML z parametrem = URL ticketu.
    /// </summary>
    [DataMember]
    public IAsyncCommand OpenTicketCommand { get; }

    private Task OpenTicketAsync(object? parameter, CancellationToken ct)
    {
        if (parameter is string url && !string.IsNullOrEmpty(url))
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// Pojedynczy ticket w liście.
/// </summary>
[DataContract]
internal class JiraTicketItem
{
    public JiraTicketItem(string key, string title, string url)
    {
        Key = key;
        Title = title;
        Url = url;
    }

    [DataMember] public string Key { get; set; }
    [DataMember] public string Title { get; set; }
    [DataMember] public string Url { get; set; }
}

internal class JiraTicketListVisual : RemoteUserControl
{
    public JiraTicketListVisual(JiraTicketListData data) : base(data) { }
}
