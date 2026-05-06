namespace VsJiraTicketTooltip.Core.Settings;

/// <summary>
/// Interfejs do odczytu i zapisu ustawień wtyczki.
/// Implementacja może korzystać z Visual Studio Settings API lub innego mechanizmu persystencji.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Wczytuje ustawienia z magazynu. Zwraca domyślne ustawienia jeśli nie zostały jeszcze zapisane.
    /// </summary>
    ExtensionSettings Load();

    /// <summary>
    /// Zapisuje ustawienia w magazynie.
    /// </summary>
    /// <param name="settings">Ustawienia do zapisania.</param>
    void Save(ExtensionSettings settings);

    /// <summary>
    /// Zdarzenie wywoływane po zmianie ustawień (np. przez użytkownika w Options dialog).
    /// </summary>
    event EventHandler<ExtensionSettings>? SettingsChanged;
}
