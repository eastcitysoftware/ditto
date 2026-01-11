using System.Net;

namespace Ditto.Cli.Tests;

public class DevelopmentHttpServerTests {
    private const string Prefix = "http://localhost:56565/";

    [Fact]
    public async Task DevelopmentHttpServer_StartsAndStops() {
        using var output = new StringWriter();
        Console.SetOut(output);

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        using var server = new DevelopmentHttpServer(Prefix, tempDir);
        server.Start();

        // Allow some time for the server to start
        await Task.Delay(500);

        Assert.NotNull(server);

        server.Stop();
        Directory.Delete(tempDir);
    }

    [Fact]
    public async Task DevelopmentHttpServer_ServesFiles() {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var testFilePath = Path.Combine(tempDir, "test.txt");
        var testFileContent = "Hello, Ditto!";
        await File.WriteAllTextAsync(testFilePath, testFileContent);

        using var server = new DevelopmentHttpServer(Prefix, tempDir);
        server.Start();

        // Allow some time for the server to start
        await Task.Delay(500);

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"{Prefix}test.txt");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(testFileContent, content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);

        server.Stop();
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task DevelopmentHttpServer_HandlesFileNotFound() {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        using var server = new DevelopmentHttpServer(Prefix, tempDir);
        server.Start();

        // Allow some time for the server to start
        await Task.Delay(500);

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"{Prefix}nonexistent.txt");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        server.Stop();
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task DevelopmentHttpServer_ServesIndexForChildDirectoryRequest() {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var childDir = Path.Combine(tempDir, "child");
        Directory.CreateDirectory(childDir);
        var indexFilePath = Path.Combine(childDir, "index.html");
        var indexFileContent = "<h1>Welcome to Ditto</h1>";
        await File.WriteAllTextAsync(indexFilePath, indexFileContent);

        using var server = new DevelopmentHttpServer(Prefix, tempDir);
        server.Start();

        // Allow some time for the server to start
        await Task.Delay(500);

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"{Prefix}child/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(indexFileContent, content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        server.Stop();
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task DevelopmentHttpServer_HandlesAllDefinedFileTypes() {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var fileTypes = new Dictionary<string, string> {
            { "test.txt", "Just some text." },
            { "test.json", "{ \"key\": \"value\" }" },
            { "text.xml", "<root></root>" },
            { "test.html", "<html></html>" },
            { "test.css", "body { }" },
            { "test.js", "console.log('Hello');" },
            { "test.png", Convert.ToBase64String(new byte[] { 137, 80, 78, 71 }) }, // PNG header bytes
            { "test.jpg", Convert.ToBase64String(new byte[] { 255, 216, 255 }) }, // JPG header bytes
            { "test.gif", Convert.ToBase64String(new byte[] { 71, 73, 70, 56 }) }, // GIF header bytes
            { "test.svg", "<svg></svg>" }
        };

        foreach (var (fileName, content) in fileTypes) {
            var filePath = Path.Combine(tempDir, fileName);
            if (fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".gif")) {
                var bytes = Convert.FromBase64String(content);
                await File.WriteAllBytesAsync(filePath, bytes);
            }
            else {
                await File.WriteAllTextAsync(filePath, content);
            }
        }

        using var server = new DevelopmentHttpServer(Prefix, tempDir);
        server.Start();

        // Allow some time for the server to start
        await Task.Delay(500);

        using var httpClient = new HttpClient();

        foreach (var (fileName, content) in fileTypes) {
            var response = await httpClient.GetAsync($"{Prefix}{fileName}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            if (fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".gif")) {
                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                var originalBytes = Convert.FromBase64String(content);
                Assert.Equal(originalBytes, responseBytes);
            }
            else {
                var responseContent = await response.Content.ReadAsStringAsync();
                Assert.Equal(content, responseContent);
            }
        }

        server.Stop();
        Directory.Delete(tempDir, true);

    }
}
