using System.Text;

namespace Ditto;

public sealed record Page(
    string Path,
    string Slug,
    string Url,
    string? PageTitle,
    string Title,
    string Description,
    string[] Tags,
    DateTime? Published,
    IDictionary<string, object> Data,
    View View);

public interface IPageParser {
    Task<Page> Parse(TextReader input, PageFile pageFile);
}

public sealed class PageParser(SiteConfig siteConfig) : IPageParser {
    public async Task<Page> Parse(TextReader input, PageFile pageFile) {
        var line = await input.ReadLineAsync();

        ModelValues? frontMatter = default;
        string? template;

        if (line is not null && line.StartsWith("---")) {
            frontMatter = await ExtractFrontMatter(input);
            template = await input.ReadToEndAsync();
        }
        else {
            template = string.Concat(line, Environment.NewLine, await input.ReadToEndAsync());
        }

        var pageTitle = frontMatter?.GetString("title");

        var additionalData = frontMatter?.AsDictionary() ?? new Dictionary<string, object>();
        additionalData.Remove("title");
        additionalData.Remove("description");
        additionalData.Remove("tags");
        additionalData.Remove("published");
        additionalData.Remove("layout");

        return new(
            Path: pageFile.Path,
            Slug: pageFile.PageName,
            Url: UrlHelper.Combine(siteConfig.BaseUrl, pageFile.Path),
            PageTitle: pageTitle,
            Title: GetTitle(pageTitle),
            Description: frontMatter?.GetString("description") ?? siteConfig.Description,
            Tags: frontMatter?.GetStringArray("tags") ?? [],
            Published: frontMatter?.GetDateTime("published"),
            Data: additionalData,
            View: new(pageFile.PageName, template, pageFile.ViewType, GetLayoutName(frontMatter)));
    }

    private static string GetLayoutName(ModelValues? frontMatter) =>
        frontMatter?.GetString("layout") is string layoutValue && !string.IsNullOrWhiteSpace(layoutValue)
            ? layoutValue
            : Website.DefaultLayoutName;

    private string GetTitle(string? pageTitle) =>
        pageTitle is string titleValue && !string.IsNullOrWhiteSpace(titleValue)
            ? string.Concat(titleValue, siteConfig.TitleSeparator, siteConfig.Title)
            : siteConfig.Title;

    private static async Task<ModelValues?> ExtractFrontMatter(TextReader input) {
        using var frontMatterStr = new StringWriter();
        string? line;
        while ((line = await input.ReadLineAsync()) is not null) {
            if (line.StartsWith("---")) {
                // reached end of front matter
                break;
            }
            await frontMatterStr.WriteLineAsync(line);
        }

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(frontMatterStr.ToString()));

        return await ModelValuesParser.Parse(ms);
    }
}

internal static class UrlHelper {
    internal static string Combine(string baseUrl, string relativePath) {
        if (string.IsNullOrEmpty(baseUrl)) return relativePath;
        if (string.IsNullOrEmpty(relativePath)) return baseUrl;

        return string.Concat(
            baseUrl.TrimEnd('/'),
            "/",
            relativePath.Trim('/'),
            "/");
    }
}
