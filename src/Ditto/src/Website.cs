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

        if (!Directory.Exists(outputPath)) {
            Directory.CreateDirectory(outputPath);
        }

        var pageFiles = pageFileLoader.LoadFiles();

        if (pageFiles.Count == 0) {
            return Result.Error($"No page files found in the source directory '{sourcePath}'.");
        }

        var pageTasks = pageFiles.Select(async pageFile => {
            using var input = new StreamReader(pageFile.InputPath);
            var page = await pageParser.Parse(input, pageFile);
            return (page, pageFile.OutputPath);
        });

        var pages = await Task.WhenAll(pageTasks);

        var pageCollections = PageCollection.CreateFromPages(pages.Select(x => x.page));

        var pageRenderTasks = pages.Select(async pageTuple => {
            var (page, outputPath) = pageTuple;
            var outputDir = Path.GetDirectoryName(outputPath);

            if (outputDir is not null && !Directory.Exists(outputDir)) {
                Directory.CreateDirectory(outputDir);
            }

            var renderedContent = await viewEngine.Render(
                page: page,
                supplementalData: new Dictionary<string, object>() {
                    ["site"] = siteConfig,
                    ["collections"] = pageCollections
                });

            using var writer = new StreamWriter(outputPath);
            await writer.WriteAsync(renderedContent);
        });

        await Task.WhenAll(pageRenderTasks);
        return Result.Ok();
    }
}

public static class PageCollection {
    public static Dictionary<string, List<PageInfo>> CreateFromPages(IEnumerable<PageInfo> pages) {
        var pageDict = new Dictionary<string, List<PageInfo>>();

        // collections are derived from the first segment of the url
        // only pages with n > 1 subpages are included in collections
        foreach (var page in pages) {
            if (page.Path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) is string[] segments
                && segments.Length > 1) {
                var collectionName = segments[0];

                if (pageDict.TryGetValue(collectionName, out var value)) {
                    pageDict[collectionName].Add(page);
                }
                else {
                    pageDict[collectionName] = [page];
                }
            }
        }

        return pageDict;
    }
}

internal static class PathHelper {
    private static readonly HashSet<string> _systemPaths = [
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        "/bin", "/boot", "/dev", "/etc", "/lib", "/lib64", "/proc", "/root", "/sbin", "/sys", "/usr", "/var" // Common Linux/macOS system directories
    ];

    internal static bool IsSystemPath(string pathToTest) =>
        _systemPaths.Any(path => string.Equals(path, pathToTest, StringComparison.OrdinalIgnoreCase));
}
