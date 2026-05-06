using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using VsJiraTicketTooltip.Core.Interfaces;

namespace VsJiraTicketTooltip.Core.Credentials;

/// <summary>
/// Implementacja ICredentialStore używająca DPAPI do szyfrowania danych na dysku.
/// Obsługuje duże wartości (tokeny JWT) bez limitu 2560 bajtów Windows Credential Manager.
/// Pliki są szyfrowane kluczem bieżącego użytkownika — nie można ich odczytać na innym koncie.
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsCredentialStore : ICredentialStore
{
    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VsJiraTicketTooltip",
        "credentials");

    public void Save(string target, string username, string secret)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(secret);

        Directory.CreateDirectory(StorePath);

        var plainBytes = Encoding.UTF8.GetBytes($"{username}\n{secret}");
        var encrypted = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

        File.WriteAllBytes(GetFilePath(target), encrypted);
    }

    public bool TryLoad(string target, out string? username, out string? secret)
    {
        ArgumentNullException.ThrowIfNull(target);
        username = null;
        secret = null;

        var filePath = GetFilePath(target);
        if (!File.Exists(filePath))
            return false;

        try
        {
            var encrypted = File.ReadAllBytes(filePath);
            var plainBytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            var plain = Encoding.UTF8.GetString(plainBytes);

            var sep = plain.IndexOf('\n');
            if (sep < 0)
            {
                username = plain;
                secret = string.Empty;
            }
            else
            {
                username = plain[..sep];
                secret = plain[(sep + 1)..];
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Delete(string target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var filePath = GetFilePath(target);
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    private static string GetFilePath(string target)
    {
        var safe = string.Concat(target.Select(c =>
            Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return Path.Combine(StorePath, safe + ".dat");
    }
}
