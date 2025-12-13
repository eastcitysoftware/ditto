namespace Ditto;

public sealed record PageFile(
    string InputPath,
    string OutputPath,
    string Path,
    string PageName,
    ViewType ViewType);

public interface IPageFileLoader {
    IReadOnlyList<PageFile> LoadFiles();
}

public sealed class PageFileLoader(string basePath, string outputPath) : IPageFileLoader {
    private readonly string _indexFileName = string.Concat("index", Website.TemplateExtension);
    public IReadOnlyList<PageFile> LoadFiles() {
        var pages = new List<PageFile>();
        var filePaths =
            Directory.GetFiles(basePath, string.Concat("*", Website.TemplateExtension), SearchOption.AllDirectories)
                .Union(Directory.GetFiles(basePath, "*.md", SearchOption.AllDirectories));

        foreach (var filePath in filePaths) {
            var relativeFilePath = Path.GetRelativePath(basePath, filePath);

            // skip files in the layouts and partials directory
            if (relativeFilePath.StartsWith(Website.LayoutsDirectory, StringComparison.OrdinalIgnoreCase)
                || relativeFilePath.StartsWith(Website.PartialsDirectory, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            // If the file is named index.html, it should map to the output path
            // directly. Otherwise, a file named index.html should be placed in
            // a subdirectory with the same name as the original file.
            var outputFilePath = Equals(relativeFilePath, _indexFileName)
                ? Path.Join(outputPath, relativeFilePath)
                : Path.Join(
                    outputPath,
                    Path.GetDirectoryName(relativeFilePath),
                    Path.GetFileNameWithoutExtension(relativeFilePath),
                    _indexFileName);

            pages.Add(new(
                InputPath: filePath,
                OutputPath: outputFilePath,
                Path: PageFileHelper.GetPath(basePath, filePath),
                PageName: PageFileHelper.GetPageName(basePath, filePath),
                ViewType: PageFileHelper.GetViewType(filePath)));
        }

        return pages;
    }
}

internal static class PageFileHelper {
    internal static string GetPageName(string basePath, string filePath) {
        var pageName = "index";
        if (GetRelativePath(basePath, filePath) is string relativePath
            && !Equals(relativePath, "index.html")
            && !Equals(relativePath, filePath)) {
            // strip extension, replace backslashes with forward slashes and
            // prepend + append forward slashes
            pageName = relativePath[..^Path.GetExtension(relativePath).Length].Replace("\\", "/");
        }
        return pageName;
    }

    internal static string GetPath(string basePath, string filePath) {
        var path = "/";
        if (GetPageName(basePath, filePath) is string pageName
            && !Equals("index", pageName)) {
            // strip extension, replace backslashes with forward slashes and
            // prepend + append forward slashes
            path = string.Concat("/", pageName, "/");
        }
        return path;
    }

    internal static ViewType GetViewType(string filePath) =>
        string.Equals(Path.GetExtension(filePath), ".md", StringComparison.OrdinalIgnoreCase)
            ? ViewType.Markdown
            : ViewType.Html;

    private static string? GetRelativePath(string basePath, string filePath) {
        // we define our own method here to avoid issues with Path.GetRelativePath
        // on different platforms
        if (filePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)) {
            return filePath[basePath.Length..].TrimStart('/', '\\');
        }
        return default;
    }
}
