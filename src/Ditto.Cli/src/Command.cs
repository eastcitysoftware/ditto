using Danom;

namespace Ditto.Cli;

internal sealed class GenerateWebsiteCommand(string basePath, string outputPath, bool hotReload) {
    public async Task Execute() {
        var siteConfigLoader = new SiteConfigLoader(basePath);
        var viewLoader = new ViewLoader(basePath);
        var pageFileLoader = new PageFileLoader(basePath, outputPath);

        // generate website once, then watch for changes if hotReload is enabled
        await GenerateWebsite(
            siteConfigLoader,
            viewLoader,
            pageFileLoader);

        if (hotReload) {
            using var watcher = new WebsiteFileWatcher(basePath);
            watcher.OnChangedAsync += async relativePath => {
                Console.WriteLine($"File change detected: {relativePath}");
                await GenerateWebsite(
                    siteConfigLoader,
                    viewLoader,
                    pageFileLoader);
            };

            Console.WriteLine("Press ESC to stop watching for file changes...");
            watcher.Start(() => {
                if (!Console.IsInputRedirected
                    && Console.KeyAvailable
                    && Console.ReadKey(true).Key == ConsoleKey.Escape) {
                    Console.WriteLine("Stopping file watcher...");
                    return false;
                }
                return true;
            });
        }
    }

    private async Task GenerateWebsite(
        SiteConfigLoader siteConfigLoader,
        ViewLoader viewLoader,
        PageFileLoader pageFileLoader) {
        var siteConfigResult = await siteConfigLoader.Load();

        if (siteConfigResult.TryGetError(out var e) && e is ResultErrors errors) {
            Console.WriteLine("Failed to load site configuration:");
            foreach (var error in errors) {
                Console.WriteLine($"  * Error: {error}");
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
            ok: _ => Console.WriteLine($"Website generated successfully, took {sw}."),
            error: errors => {
                Console.WriteLine("Errors occurred during website generation:");
                foreach (var error in errors) {
                    Console.WriteLine($"  * Error: {error}");
                }
            });
    }
}
