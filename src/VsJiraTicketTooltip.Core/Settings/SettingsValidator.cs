namespace VsJiraTicketTooltip.Core.Settings;

/// <summary>
/// Walidator ustawień wtyczki. Wszystkie metody są statyczne — brak stanu.
/// </summary>
public static class SettingsValidator
{
    /// <summary>
    /// Waliduje URL instancji Jira. Akceptuje tylko HTTPS URL z niepustym hostem.
    /// </summary>
    /// <param name="url">URL do walidacji.</param>
    /// <returns>
    /// <c>true</c> jeśli URL jest poprawnym HTTPS URL z niepustym hostem; <c>false</c> w przeciwnym razie.
    /// </returns>
    public static bool ValidateJiraInstanceUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttps
            && !string.IsNullOrEmpty(uri.Host);
    }

    /// <summary>
    /// Waliduje wszystkie ustawienia. Zwraca listę błędów (pusta lista = brak błędów).
    /// </summary>
    /// <param name="settings">Ustawienia do walidacji.</param>
    /// <returns>Lista komunikatów błędów. Pusta jeśli ustawienia są poprawne.</returns>
    public static IReadOnlyList<string> Validate(ExtensionSettings settings)
    {
        var errors = new List<string>();

        if (!ValidateJiraInstanceUrl(settings.JiraInstanceUrl))
            errors.Add("Jira Instance URL must be a valid HTTPS URL.");

        if (string.IsNullOrWhiteSpace(settings.OAuthClientId))
            errors.Add("OAuth Client ID is required.");

        return errors.AsReadOnly();
    }
}
