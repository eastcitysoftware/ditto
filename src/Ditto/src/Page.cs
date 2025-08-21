using System.Text;
using Tomlyn;
using Tomlyn.Model;

namespace Ditto;

public sealed record Page(
    string Title,
    string Description,
    SiteConfig Site,
    IDictionary<string, object> Metadata,
    string Template,
    string Layout = Website.DefaultLayoutName);

public interface IPageParser {
    Task<Page> Parse(TextReader input);
}

public sealed class PageParser(SiteConfig siteConfig) : IPageParser {
    public async Task<Page> Parse(TextReader input) {
        var line = await input.ReadLineAsync();

        TomlTable? frontMatter = default;
        string? template;

        if (line is not null && line.StartsWith("---")) {
            frontMatter = await ExtractFrontMatter(input);
            template = input.ReadToEnd();
        }
        else {
            template = string.Concat(line, Environment.NewLine, input.ReadToEnd());
        }

        var title =
            frontMatter?.GetString("title") is string titleValue && !string.IsNullOrWhiteSpace(titleValue)
                ? string.Concat(titleValue, siteConfig.TitleSeparator, siteConfig.Title)
                : siteConfig.Title;

        return new(
            Title: title,
            Description: frontMatter?.GetString("description") ?? siteConfig.Description,
            Site: siteConfig,
            Metadata: frontMatter ?? [],
            Template: template,
            Layout: frontMatter?.GetString("layout") ?? Website.DefaultLayoutName);
    }

    private static async Task<TomlTable?> ExtractFrontMatter(TextReader input) {
        var frontMatterStr = new StringWriter();
        string? line;
        while ((line = await input.ReadLineAsync()) is not null) {
            if (line.StartsWith("---")) {
                // reached end of front matter
                break;
            }
            await frontMatterStr.WriteLineAsync(line);
        }

        var frontMatterBlock = frontMatterStr.ToString();
        return Toml.ToModel(frontMatterBlock);
    }
}
