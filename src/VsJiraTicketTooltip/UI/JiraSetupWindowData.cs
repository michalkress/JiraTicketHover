#pragma warning disable VSEXTPREVIEW_SETTINGS

using System.Runtime.Serialization;
using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.UI;
using VsJiraTicketTooltip.Core.Credentials;
using VsJiraTicketTooltip.Core.Jira;
using VsJiraTicketTooltip.Settings;

namespace VsJiraTicketTooltip.UI;

/// <summary>
/// ViewModel dla okna konfiguracji połączenia z Jirą.
/// </summary>
[DataContract]
internal class JiraSetupWindowData : NotifyPropertyChangedObject
{
    private const string ClientSecretKey = "VsJiraTicketTooltip/ClientSecret";

    private readonly VisualStudioExtensibility _extensibility;

    private string _clientSecret = string.Empty;
    private string _statusMessage = "Enter your OAuth2 Client Secret and click Connect.";
    private bool _isConnecting;
    private bool _isConnected;

    public JiraSetupWindowData(VisualStudioExtensibility extensibility)
    {
        _extensibility = Requires.NotNull(extensibility);
        ConnectCommand = new AsyncCommand(ConnectAsync);
        DisconnectCommand = new AsyncCommand(DisconnectAsync);

#pragma warning disable CA1416
        var store = new WindowsCredentialStore();
        if (store.TryLoad("VsJiraTicketTooltip/AccessToken", out _, out var token) && !string.IsNullOrEmpty(token))
        {
            IsConnected = true;
            StatusMessage = "✅ Connected to Jira. Click Disconnect to remove credentials.";
        }
#pragma warning restore CA1416
    }

    [DataMember] public IAsyncCommand ConnectCommand { get; }
    [DataMember] public IAsyncCommand DisconnectCommand { get; }

    [DataMember]
    public string ClientSecret
    {
        get => _clientSecret;
        set => SetProperty(ref _clientSecret, value);
    }

    [DataMember]
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    [DataMember]
    public bool IsConnecting
    {
        get => _isConnecting;
        set => SetProperty(ref _isConnecting, value);
    }

    [DataMember]
    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    private async Task ConnectAsync(object? _, CancellationToken cancellationToken)
    {
        // Odczytaj ustawienia przez Settings API
        var settingsManager = _extensibility.Settings();
        var snapshot = await settingsManager
            .ReadEffectiveValuesAsync(
                [JiraTicketSettings.JiraInstanceUrl, JiraTicketSettings.OAuthClientId],
                cancellationToken);

        string jiraUrl = string.Empty;
        string clientId = string.Empty;

        foreach (var kvp in snapshot)
        {
            var value = kvp.Value?.Value<string>() ?? string.Empty;
            // SettingIdentifier nie ma FullId — użyj ToString() do identyfikacji
            var keyStr = kvp.Key.ToString() ?? string.Empty;
            if (keyStr.Contains("jiraInstanceUrl"))
                jiraUrl = value;
            else if (keyStr.Contains("oauthClientId"))
                clientId = value;
        }

        if (string.IsNullOrWhiteSpace(jiraUrl))
        {
            StatusMessage = "❌ Set Jira Instance URL in Tools → Options → Jira Ticket Tooltip first.";
            return;
        }
        if (string.IsNullOrWhiteSpace(clientId))
        {
            StatusMessage = "❌ Set OAuth2 Client ID in Tools → Options → Jira Ticket Tooltip first.";
            return;
        }
        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            StatusMessage = "❌ Enter your OAuth2 Client Secret.";
            return;
        }

        IsConnecting = true;
        StatusMessage = "🔄 Opening browser for authorization...";

        try
        {
#pragma warning disable CA1416
            var store = new WindowsCredentialStore();
            store.Save(ClientSecretKey, "jira_oauth", ClientSecret);

            var oauth = new JiraOAuthService(clientId, ClientSecret, store);
            var (authUrl, state) = oauth.GetAuthorizationUrl();

            using var server = new LocalHttpServer("http://localhost:9089/");

            // Upewnij się że serwer nasłuchuje ZANIM otworzymy przeglądarkę
            // WaitForCallbackAsync wywołuje _listener.Start() wewnętrznie,
            // więc musimy uruchomić go jako Task i dać mu chwilę na start
            var callbackTask = server.WaitForCallbackAsync(cancellationToken);
            await Task.Delay(500, cancellationToken); // Daj serwerowi czas na start

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(authUrl) { UseShellExecute = true });

            StatusMessage = "🔄 Waiting for callback on localhost:9089...";
            var query = await callbackTask;

            if (!query.TryGetValue("code", out var code))
            { StatusMessage = "❌ No authorization code received."; return; }

            if (!query.TryGetValue("state", out var retState) || retState != state)
            { StatusMessage = "❌ State mismatch — possible CSRF attack."; return; }

            StatusMessage = "🔄 Exchanging code for tokens...";
            await oauth.ExchangeCodeForTokenAsync(code, cancellationToken);
#pragma warning restore CA1416

            IsConnected = true;
            ClientSecret = string.Empty;
            StatusMessage = "✅ Successfully connected to Jira!";
        }
        catch (OperationCanceledException) { StatusMessage = "⚠️ Cancelled."; }
        catch (Exception ex) { StatusMessage = $"❌ {ex.Message}"; }
        finally { IsConnecting = false; }
    }

    private Task DisconnectAsync(object? _, CancellationToken cancellationToken)
    {
#pragma warning disable CA1416
        var store = new WindowsCredentialStore();
        foreach (var key in new[] { ClientSecretKey, "VsJiraTicketTooltip/AccessToken",
            "VsJiraTicketTooltip/RefreshToken", "VsJiraTicketTooltip/TokenExpiry", "VsJiraTicketTooltip/CloudId" })
            store.Delete(key);
#pragma warning restore CA1416

        IsConnected = false;
        StatusMessage = "Disconnected. Enter Client Secret and click Connect.";
        return Task.CompletedTask;
    }
}
