using VsJiraTicketTooltip.Core.Settings;

namespace VsJiraTicketTooltip;

/// <summary>
/// Łączy ISettingsStore z SettingsObserver — subskrybuje zdarzenie SettingsChanged
/// i przekazuje nowe ustawienia do SettingsObserver.
/// W docelowej implementacji VSIX ISettingsStore będzie implementowany przez
/// VisualStudio.Extensibility Settings API.
/// </summary>
public class ExtensionSettingsConnector : IDisposable
{
    private readonly ISettingsStore _settingsStore;
    private readonly SettingsObserver _observer;

    public ExtensionSettingsConnector(ISettingsStore settingsStore, SettingsObserver observer)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        _settingsStore.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, ExtensionSettings newSettings)
    {
        _observer.OnSettingsChanged(newSettings);
    }

    public void Dispose()
    {
        _settingsStore.SettingsChanged -= OnSettingsChanged;
    }
}
