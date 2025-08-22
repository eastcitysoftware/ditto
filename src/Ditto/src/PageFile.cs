using Tomlyn;

namespace Ditto;

public sealed record PageFile(
    string InputPath,
    string OutputPath);

public interface IPageFileLoader {
    IReadOnlyList<PageFile> LoadFiles();
}

public sealed class PageFileLoader(string basePath, string outputPath) : IPageFileLoader {
    public IReadOnlyList<PageFile> LoadFiles() {
        var pages = new List<PageFile>();
        foreach (var filePath in Directory.GetFiles(basePath, "*.html", SearchOption.AllDirectories)) {
            var relativeFilePath = Path.GetRelativePath(basePath, filePath);

            // skip files in the layouts and partials directory
            if (relativeFilePath.StartsWith(Website.LayoutsDirectory, StringComparison.OrdinalIgnoreCase)
                || relativeFilePath.StartsWith(Website.PartialsDirectory, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            // If the file is named index.html, it should map to the output path
            // directly. Otherwise, a file named index.html should be placed in
            // a subdirectory with the same name as the original file.
            var outputFilePath = relativeFilePath == "index.html"
                ? Path.Join(outputPath, relativeFilePath)
                : Path.Join(
                    outputPath,
                    Path.GetDirectoryName(relativeFilePath),
                    Path.GetFileNameWithoutExtension(relativeFilePath),
                    "index.html");

            pages.Add(new(filePath, outputFilePath));
        }
        return pages;
    }
}
