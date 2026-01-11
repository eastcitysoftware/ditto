using System.Net;

namespace Ditto.Cli;

public sealed class DevelopmentHttpServer : IDisposable {
    private readonly HttpListener _listener;
    private readonly string _outputPath;

    public DevelopmentHttpServer(string prefix, string outputPath) {
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
        _outputPath = outputPath;
    }

    public void Start() {
        _listener.Start();
        Print.Info($"HTTP server started at {_listener.Prefixes.First()}");
        Task.Run(() => HandleRequests());
    }

    public void Stop() {
        _listener.Stop();
        Print.Info("HTTP server stopped.");
    }

    private async Task HandleRequests() {
        while (_listener.IsListening) {
            try {
                var context = await _listener.GetContextAsync();
                ProcessRequest(context);
            }
            catch (Exception ex) when (ex is HttpListenerException || ex is ObjectDisposedException) {
                // Listener was stopped, exit gracefully
                break;
            }
            catch (Exception ex) {
                Print.Error($"Error handling request: {ex}");
            }
        }
    }

    private void ProcessRequest(HttpListenerContext context) {
        if (!_listener.IsListening) {
            return;
        }

        var request = context.Request;
        var response = context.Response;

        try {
            if (request.Url is null) {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            if (request.Url.LocalPath.Contains("..")) {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            var filePath = GetFilePath(_outputPath, request.Url.LocalPath);

            if (File.Exists(filePath)) {
                var content = File.ReadAllBytes(filePath);
                response.ContentType = GetContentType(filePath);
                response.ContentLength64 = content.Length;
                response.Headers.Add("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");
                response.OutputStream.Write(content, 0, content.Length);
            }
            else {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                var errorMessage = "404 Not Found";
                var errorBytes = System.Text.Encoding.UTF8.GetBytes(errorMessage);
                response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
            }
        }
        catch (Exception ex) {
            Print.Error($"Error processing request: {ex}");
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
        finally {
            response.OutputStream.Close();
        }
    }

    private static string GetFilePath(string outputPath, string localPath) {
        if (string.IsNullOrEmpty(localPath.TrimStart('/'))) {
            // Root request, serve index.html
            return Path.Combine(outputPath, "index.html");
        }
        else if (localPath.EndsWith('/')) {
            // Directory request, serve index.html in that directory
            return Path.Combine(outputPath, localPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar), "index.html");
        }

        return Path.Combine(outputPath, localPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
    }

    private static string GetContentType(string filePath) {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch {
            ".txt" => "text/plain",
            ".xml" => "application/xml",
            ".json" => "application/json",
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream",
        };
    }

    public void Dispose() {
        _listener.Close();
    }
}
