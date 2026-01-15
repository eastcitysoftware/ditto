using System.Text;
using CsToml.Error;
using Danom;

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
    Task<Result<Page, ResultErrors>> Parse(TextReader input, PageFile pageFile);
}

public sealed class PageParser(SiteConfig siteConfig) : IPageParser {
    public async Task<Result<Page, ResultErrors>> Parse(TextReader input, PageFile pageFile) {
        var line = await input.ReadLineAsync().ConfigureAwait(false);

        ModelValues? frontMatter = default;
        string? template = default;

        if (line is not null && line.StartsWith("---")) {
            var frontMatterResult = await ExtractFrontMatter(input);

            if (frontMatterResult.TryGetError(out var e)) {
                return Result<Page>.Error(e);
            }

            if (frontMatterResult.TryGet(out var fm)) {
                frontMatter = fm;
                template = await input.ReadToEndAsync().ConfigureAwait(false);
            }
        }
        else {
            template = string.Concat(line, Environment.NewLine, await input.ReadToEndAsync().ConfigureAwait(false));
        }

        if (template is null) {
            return Result<Page>.Error("Failed to read page template.");
        }

        var pageTitle = frontMatter?.GetString("title");

        var additionalData = frontMatter?.AsDictionary() ?? new Dictionary<string, object>();
        additionalData.Remove("title");
        additionalData.Remove("description");
        additionalData.Remove("tags");
        additionalData.Remove("published");
        additionalData.Remove("layout");

        return Result<Page>.Ok(new(
            Path: pageFile.Path,
            Slug: pageFile.PageName,
            Url: UrlHelper.Combine(siteConfig.BaseUrl, pageFile.Path),
            PageTitle: pageTitle,
            Title: GetTitle(pageTitle),
            Description: frontMatter?.GetString("description") ?? siteConfig.Description,
            Tags: frontMatter?.GetStringArray("tags") ?? [],
            Published: frontMatter?.GetDateTime("published"),
            Data: additionalData,
            View: new(pageFile.PageName, template, pageFile.ViewType, GetLayoutName(frontMatter))));
    }

    private static string GetLayoutName(ModelValues? frontMatter) =>
        frontMatter?.GetString("layout") is string layoutValue && !string.IsNullOrWhiteSpace(layoutValue)
            ? layoutValue
            : Website.DefaultLayoutName;

    private string GetTitle(string? pageTitle) =>
        !string.IsNullOrWhiteSpace(pageTitle)
            ? $"{pageTitle}{siteConfig.TitleSeparator}{siteConfig.Title}"
            : siteConfig.Title;

    private static async Task<Result<ModelValues, ResultErrors>> ExtractFrontMatter(TextReader input) {
        using var frontMatterStr = new StringWriter();
        string? line;
        while ((line = await input.ReadLineAsync().ConfigureAwait(false)) is not null) {
            if (line.StartsWith("---")) {
                // reached end of front matter
                break;
            }
            await frontMatterStr.WriteLineAsync(line);
        }

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(frontMatterStr.ToString()));

        var tomlResult = await ModelValuesParser.Parse(ms);

        if (tomlResult.TryGetError(out var tomlError)) {
            return Result<ModelValues>.Error(tomlError);
        }

        if (tomlResult.TryGet(out var modelValues)) {
            return Result<ModelValues>.Ok(modelValues);
        }

        return Result<ModelValues>.Error("Failed to parse front matter.");
    }

    internal static class UrlHelper {
        internal static string Combine(string baseUrl, string relativePath) {
            if (string.IsNullOrEmpty(baseUrl)) {
                return relativePath;
            }

            if (string.IsNullOrEmpty(relativePath)) {
                return baseUrl;
            }

            return string.Concat(
                baseUrl,
                "/",
                relativePath.EndsWith('/') ? relativePath.Trim('/') : relativePath,
                "/");
        }
    }
}
