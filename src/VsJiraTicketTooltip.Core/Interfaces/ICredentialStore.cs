namespace VsJiraTicketTooltip.Core.Interfaces;

/// <summary>
/// Abstrakcja nad bezpiecznym magazynem poświadczeń (np. Windows Credential Manager).
/// Używana do przechowywania tokenów OAuth2 — nigdy w plikach na dysku ani w Settings Store.
/// </summary>
public interface ICredentialStore
{
    /// <summary>
    /// Zapisuje poświadczenia w magazynie.
    /// </summary>
    /// <param name="target">Unikalny identyfikator wpisu (np. "VsJiraTicketTooltip/AccessToken").</param>
    /// <param name="username">Nazwa użytkownika lub identyfikator konta.</param>
    /// <param name="secret">Sekret do zapisania (token, hasło itp.).</param>
    void Save(string target, string username, string secret);

    /// <summary>
    /// Próbuje załadować poświadczenia z magazynu.
    /// </summary>
    /// <param name="target">Unikalny identyfikator wpisu.</param>
    /// <param name="username">Załadowana nazwa użytkownika lub <c>null</c> jeśli wpis nie istnieje.</param>
    /// <param name="secret">Załadowany sekret lub <c>null</c> jeśli wpis nie istnieje.</param>
    /// <returns><c>true</c> jeśli wpis istnieje; <c>false</c> w przeciwnym razie.</returns>
    bool TryLoad(string target, out string? username, out string? secret);

    /// <summary>
    /// Usuwa wpis z magazynu. Jeśli wpis nie istnieje, operacja jest ignorowana.
    /// </summary>
    /// <param name="target">Unikalny identyfikator wpisu do usunięcia.</param>
    void Delete(string target);
}
