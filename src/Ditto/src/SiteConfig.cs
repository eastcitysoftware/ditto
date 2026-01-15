using CsToml.Error;
using Danom;

namespace Ditto;

public sealed record SiteConfig(
    string BaseUrl,
    string Title,
    string Description,
    string TitleSeparator,
    IDictionary<string, object> Data);

public interface ISiteConfigLoader {
    Task<Result<SiteConfig, ResultErrors>> Load();
}

public static class SiteConfigParser {
    public static async Task<Result<SiteConfig, ResultErrors>> Parse(Stream input) {
        var tomlResult = await ModelValuesParser.Parse(input);

        if (tomlResult.TryGetError(out var tomlError)) {
            return Result<SiteConfig>.Error(tomlError);
        }

        if (!tomlResult.TryGet(out var toml)) {
            return Result<SiteConfig>.Error("Site configuration failure: Could not parse.");
        }

        var baseUrl = toml.GetString("base_url")?.TrimEnd('/');
        var title = toml.GetString("title");
        var description = toml.GetString("description");

        if (string.IsNullOrWhiteSpace(baseUrl)) {
            return Result<SiteConfig>.Error("Site configuration is missing required field: base_url.");
        }

        if (!Uri.TryCreate(baseUrl, new(), out var verifiedBaseUrl)) {
            return Result<SiteConfig>.Error("Site configuration field 'base_url' is not a valid absolute URL.");
        }

        if (string.IsNullOrWhiteSpace(title)) {
            return Result<SiteConfig>.Error("Site configuration is missing required field: title.");
        }

        if (string.IsNullOrWhiteSpace(description)) {
            return Result<SiteConfig>.Error("Site configuration is missing required field: description.");
        }

        return Result<SiteConfig>.Ok(new(
             BaseUrl: verifiedBaseUrl.AbsoluteUri,
             Title: title,
             Description: description,
             TitleSeparator: toml.GetString("title_separator") ?? Website.DefaultTitleSeparator,
             Data: toml.AsDictionary() ?? new Dictionary<string, object>()));
    }
}

public sealed class SiteConfigLoader(string basePath) : ISiteConfigLoader {
    public async Task<Result<SiteConfig, ResultErrors>> Load() {
        var siteConfigPath = Path.Join(basePath, Website.SiteConfigFileName);

        if (!File.Exists(siteConfigPath)) {
            return Result<SiteConfig>.Error($"Site configuration file not found at path: {siteConfigPath}");
        }

        using var reader = File.OpenRead(siteConfigPath);
        return await SiteConfigParser.Parse(reader);
    }
}
