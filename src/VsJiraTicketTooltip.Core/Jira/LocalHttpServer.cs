using System.Net;

namespace VsJiraTicketTooltip.Core.Jira;

/// <summary>
/// Prosty lokalny serwer HTTP nasłuchujący na callbacku OAuth2.
/// Używany do przechwycenia kodu autoryzacyjnego po przekierowaniu z Jira.
/// </summary>
public class LocalHttpServer : IDisposable
{
    private readonly HttpListener _listener;
    private bool _disposed;

    /// <summary>
    /// Inicjalizuje serwer nasłuchujący na podanym URL.
    /// </summary>
    /// <param name="listenUrl">URL do nasłuchiwania, np. "http://localhost:9089/".</param>
    public LocalHttpServer(string listenUrl)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(listenUrl.EndsWith('/') ? listenUrl : listenUrl + "/");
    }

    /// <summary>
    /// Uruchamia serwer i czeka na jedno żądanie HTTP, zwracając parametry query string.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Słownik parametrów query string z callbacku OAuth2.</returns>
    public async Task<Dictionary<string, string>> WaitForCallbackAsync(CancellationToken cancellationToken = default)
    {
        _listener.Start();
        try
        {
            using var registration = cancellationToken.Register(() => _listener.Stop());

            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            // Wyślij odpowiedź do przeglądarki
            var response = context.Response;
            const string responseHtml = """
                <html>
                <body>
                <h2>Authorization complete</h2>
                <p>You can close this window and return to Visual Studio.</p>
                </body>
                </html>
                """;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseHtml);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "text/html; charset=utf-8";
            await response.OutputStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            response.OutputStream.Close();

            // Parsuj parametry query string
            var queryParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var query = context.Request.QueryString;
            foreach (string? key in query.AllKeys)
            {
                if (key is not null)
                {
                    queryParams[key] = query[key] ?? string.Empty;
                }
            }

            return queryParams;
        }
        finally
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
            }
            _listener.Close();
            _disposed = true;
        }
    }
}
