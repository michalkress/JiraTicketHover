namespace VsJiraTicketTooltip.Core.Settings;

/// <summary>
/// POCO model ustawień wtyczki. Nie zawiera sekretu OAuth2 — jest on przechowywany
/// wyłącznie w <see cref="VsJiraTicketTooltip.Core.Interfaces.ICredentialStore"/>.
/// </summary>
public class ExtensionSettings
{
    /// <summary>
    /// Czy wtyczka jest włączona. Domyślnie: true.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// URL instancji Jira (np. "https://mycompany.atlassian.net"). Musi być HTTPS.
    /// </summary>
    public string JiraInstanceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Identyfikator klienta OAuth2.
    /// </summary>
    public string OAuthClientId { get; set; } = string.Empty;

    /// <summary>
    /// Flaga informująca, że sekret OAuth2 jest skonfigurowany w Credential Store.
    /// Sekret nigdy nie jest przechowywany w tym modelu.
    /// </summary>
    public bool IsClientSecretConfigured { get; set; } = false;

    /// <summary>
    /// Nazwa aktywnego providera ticketów. Domyślnie: "Jira".
    /// </summary>
    public string ActiveProvider { get; set; } = "Jira";
}
