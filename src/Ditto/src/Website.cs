using Danom;

namespace Ditto;

public sealed class Website {
    public const string DefaultLayoutName = "default";
    public const string PagesDirectory = "_pages";
    public const string LayoutsDirectory = "_layouts";
    public const string PartialsDirectory = "_partials";
    public const string SiteConfigFileName = "website.toml";
}

public interface IWebsiteGenerator {
    Task<Result<Unit, ResultErrors>> Generate(string sourcePath, string outputPath);
}

public sealed class WebsiteGenerator(
    IPageFileLoader pageFileLoader,
    IPageParser pageParser,
    IPageWriter pageWriter) : IWebsiteGenerator {
    public async Task<Result<Unit, ResultErrors>> Generate(string sourcePath, string outputPath) {
        if (!Directory.Exists(sourcePath)) {
            return Result.Error($"Source path '{sourcePath}' does not exist.");
        }

        if (PathUtil.IsSystemPath(outputPath)) {
            return Result.Error("Output path cannot be a system directory.");
        }

        if (!Directory.Exists(outputPath)) {
            Directory.CreateDirectory(outputPath);
        }

        var siteConfigPath = Path.Combine(sourcePath, "website.toml");

        if (!File.Exists(siteConfigPath)) {
            return Result.Error($"Site configuration file '{siteConfigPath}' not found.");
        }

        var pageFiles = pageFileLoader.LoadFiles();

        if (pageFiles.Count == 0) {
            return Result.Error($"No page files found in the source directory '{sourcePath}'.");
        }

        var pageTasks = pageFiles.Select(async pageFile => {
            using var reader = new StreamReader(pageFile.InputPath);
            var page = await pageParser.Parse(reader);
            return (page, pageFile.OutputPath);
        });

        var pages = await Task.WhenAll(pageTasks);

        var pageRenderTasks = pages.Select(async pageTuple => {
            var (page, outputPath) = pageTuple;
            var outputDir = Path.GetDirectoryName(outputPath);
            if (outputDir is not null && !Directory.Exists(outputDir)) {
                Directory.CreateDirectory(outputDir);
            }

            using var writer = new StreamWriter(outputPath);
            await pageWriter.Render(page, writer);
        });

        await Task.WhenAll(pageRenderTasks);
        return Result.Ok();
    }
}
