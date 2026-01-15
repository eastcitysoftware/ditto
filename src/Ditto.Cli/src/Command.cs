using Danom;

namespace Ditto.Cli;

internal sealed class GenerateWebsiteCommand(
    string basePath,
    string outputPath,
    bool watchEnabled,
    bool serverEnabled,
    int port) {
    public async Task Execute() {
        var siteConfigLoader = new SiteConfigLoader(basePath);
        var viewLoader = new ViewLoader(basePath);
        var pageFileLoader = new PageFileLoader(basePath, outputPath);

        // generate website once, then watch for changes if hotReload is enabled
        await BuildWebsite(
            siteConfigLoader,
            viewLoader,
            pageFileLoader);

        DevelopmentHttpServer? httpServer = default;

        if (serverEnabled) {
            var prefix = $"http://localhost:{port}/";
            httpServer = new DevelopmentHttpServer(prefix, outputPath);
            httpServer.Start();
        }

        if (watchEnabled) {
            using var watcher = new FileWatcher(basePath, [".md", ".html", ".toml"]);
            watcher.OnChangedAsync += async relativePath => {
                Print.Info($"File change detected: {relativePath.Path}");
                await BuildWebsite(
                    siteConfigLoader,
                    viewLoader,
                    pageFileLoader);
            };

            Print.Info("Press ESC to stop watching for file changes...");
            watcher.Start(() => {
                if (!Console.IsInputRedirected
                    && Console.KeyAvailable
                    && Console.ReadKey(true).Key == ConsoleKey.Escape) {
                    Print.Info("Stopping file watcher...");
                    return false;
                }
                return true;
            });
        }

        httpServer?.Stop();
        httpServer?.Dispose();
    }

    private async Task BuildWebsite(
        SiteConfigLoader siteConfigLoader,
        ViewLoader viewLoader,
        PageFileLoader pageFileLoader) {
        var siteConfigResult = await siteConfigLoader.Load();

        if (siteConfigResult.TryGetError(out var e) && e is ResultErrors errors) {
            Print.Error("Failed to load site configuration:");
            foreach (var error in errors) {
                Print.Error($"  * Error: {error}");
            }
            return;
        }

        var layouts = await viewLoader.LoadViews(Website.LayoutsDirectory);
        var partials = await viewLoader.LoadViews(Website.PartialsDirectory);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var result = await siteConfigResult.BindAsync(siteConfig => {
            var generator = new WebsiteGenerator(
                pageFileLoader: pageFileLoader,
                pageParser: new PageParser(siteConfig),
                viewEngine: new ViewEngine(
                    new ViewRenderer(partials),
                    [new MarkdownProcessor()],
                    layouts));

            return generator.Generate(basePath, outputPath, siteConfig);
        });

        sw.Stop();
        result.Match(
            ok: _ => Print.Info($"Website generated successfully, took {sw}."),
            error: errors => {
                Print.Error("Errors occurred during website generation:");
                foreach (var error in errors) {
                    Print.Error($"  * Error: {error}");
                }
            });
    }
}
