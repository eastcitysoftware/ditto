using Danom;

namespace Ditto;

public static class Website {
    public const string DefaultLayoutName = "default";
    public const string DefaultTitleSeparator = " - ";
    public const string PagesDirectory = "_pages";
    public const string LayoutsDirectory = "_layouts";
    public const string PartialsDirectory = "_partials";
    public const string SiteConfigFileName = "website.toml";
    public const string TemplateExtension = ".html";
}

public interface IWebsiteGenerator {
    Task<Result<Unit, ResultErrors>> Generate(string sourcePath, string outputPath, SiteConfig siteConfig);
}

public sealed class WebsiteGenerator(
    IPageFileLoader pageFileLoader,
    IPageParser pageParser,
    IViewEngine viewEngine) : IWebsiteGenerator {
    public async Task<Result<Unit, ResultErrors>> Generate(string sourcePath, string outputPath, SiteConfig siteConfig) {
        if (!Directory.Exists(sourcePath)) {
            return Result.Error($"Source path '{sourcePath}' does not exist.");
        }

        if (PathHelper.IsSystemPath(outputPath)) {
            return Result.Error("Output path cannot be a system directory.");
        }

        Directory.CreateDirectory(outputPath);

        // load page files
        var pageFiles = pageFileLoader.LoadFiles();

        if (pageFiles.Count == 0) {
            return Result.Error($"No page files found in the source directory '{sourcePath}'.");
        }

        var continuationErrors = new ResultErrors();

        // delete any index.html files that aren't in the source
        var pageOutputPathIndex =
            new HashSet<string>(
                pageFiles.Select(x => x.OutputPath),
                StringComparer.OrdinalIgnoreCase);

        var filesToDelete = new List<string>();

        foreach (var file in Directory.EnumerateFiles(outputPath, "index.html", SearchOption.AllDirectories)) {
            var fullPath = Path.GetFullPath(file);
            if (!pageOutputPathIndex.Contains(fullPath)) {
                filesToDelete.Add(fullPath);
            }
        }

        Parallel.ForEach(filesToDelete, file => {
            try {
                File.Delete(file);
            }
            catch (Exception ex) {
                continuationErrors.Add(Path.GetRelativePath(sourcePath, file), $"Failed to delete file '{file}': {ex.Message}");
            }
        });

        // delete any empty directories
        var directoriesToDelete = new List<string>();
        foreach (var dir in Directory.GetDirectories(outputPath, "*", SearchOption.AllDirectories).OrderByDescending(x => x.Length)) {
            if (!Directory.EnumerateFileSystemEntries(dir).Any()) {
                directoriesToDelete.Add(dir);
            }
        }

        Parallel.ForEach(directoriesToDelete, x => {
            try {
                Directory.Delete(x);
            }
            catch (Exception ex) {
                continuationErrors.Add(Path.GetRelativePath(sourcePath, x), $"Failed to delete directory '{x}': {ex.Message}");
            }
        });

        // parse pages
        var pageTasks = pageFiles.Select(async pageFile => {
            using var input = new StreamReader(pageFile.InputPath);
            // using var stream = new FileStream(pageFile.InputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite & FileShare.Delete);
            // using var input = new StreamReader(stream);
            var page = await pageParser.Parse(input, pageFile).ConfigureAwait(false);
            return (page, pageFile.InputPath, pageFile.OutputPath);
        });

        var pageResults = await Task.WhenAll(pageTasks);

        var pages = new List<(Page page, string outputPath)>();

        foreach (var (page, inputPath, pageOutputPath) in pageResults) {
            if (page.TryGet(out var p)) {
                pages.Add((p, pageOutputPath));
            }
            else if (page.TryGetError(out var e) && e is ResultErrors errors) {
                continuationErrors.Add(Path.GetRelativePath(sourcePath, inputPath), errors.SelectMany(x => x.Errors));
            }
        }

        // create page collections
        var pageCollections = PageCollectionFactory.Create(pages.Select(x => x.page));

        // render pages
        var pageRenderTasks = pages.Select(async pageTuple => {
            var (page, outputPath) = pageTuple;
            var outputDir = Path.GetDirectoryName(outputPath);

            if (outputDir is not null) {
                Directory.CreateDirectory(outputDir);
            }

            var renderedContent = await viewEngine.Render(
                page: page,
                siteConfig: siteConfig,
                collections: pageCollections).ConfigureAwait(false);

            using var writer = new StreamWriter(outputPath);
            await writer.WriteAsync(renderedContent);
        });

        await Task.WhenAll(pageRenderTasks);

        if (continuationErrors.Any()) {
            return Result.Error(continuationErrors);
        }

        return Result.Ok();
    }

    internal static class PathHelper {
        internal static readonly HashSet<string> _systemPaths = [
            // Common Windows system directory
            @"C:\Windows", @"C:\Windows\System32", @"C:\Program Files", @"C:\Program Files (x86)",
            "C:/Windows", "C:/Windows/System32", "C:/Program Files", "C:/Program Files (x86)",
            // Common Linux/macOS system directories
            "/bin", "/boot", "/dev", "/etc", "/lib", "/lib64", "/proc", "/root", "/sbin", "/sys", "/usr", "/var"
        ];

        public static bool IsSystemPath(string pathToTest) =>
            _systemPaths.Any(path => string.Equals(path, pathToTest, StringComparison.OrdinalIgnoreCase));
    }
}
