namespace VsJiraTicketTooltip.Core.Exceptions;

/// <summary>
/// Wyjątek rzucany przez <c>ICredentialStore</c> gdy operacja na Windows Credential Manager
/// zakończy się błędem Win32.
/// </summary>
public class CredentialStoreException : InvalidOperationException
{
    /// <summary>
    /// Kod błędu Win32 zwrócony przez API.
    /// </summary>
    public int Win32ErrorCode { get; }

    public CredentialStoreException(string message, int win32ErrorCode)
        : base($"{message} (Win32 Error: {win32ErrorCode})")
    {
        Win32ErrorCode = win32ErrorCode;
    }

    public CredentialStoreException(string message, int win32ErrorCode, Exception innerException)
        : base($"{message} (Win32 Error: {win32ErrorCode})", innerException)
    {
        Win32ErrorCode = win32ErrorCode;
    }
}
