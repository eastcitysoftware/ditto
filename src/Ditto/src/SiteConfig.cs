using Danom;

namespace Ditto;

public sealed record SiteConfig(
    string BaseUrl,
    string Title,
    string Description,
    string TitleSeparator = Website.DefaultTitleSeparator) {
}

public interface ISiteConfigParser {
    Task<Result<SiteConfig, ResultErrors>> Parse(Stream input);
}

public sealed class SiteConfigParser(IModelValuesParser modelValuesParser) : ISiteConfigParser {
    public async Task<Result<SiteConfig, ResultErrors>> Parse(Stream input) {
        var toml = await modelValuesParser.Parse(input);
        var baseUrl = toml?.GetString("base_url");
        var title = toml?.GetString("title");
        var description = toml?.GetString("description");

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
             TitleSeparator: toml?.GetString("title_separator") ?? Website.DefaultTitleSeparator));
    }
}

public interface ISiteConfigLoader {
    Task<Result<SiteConfig, ResultErrors>> Load();
}

public sealed class SiteConfigLoader(string basePath, ISiteConfigParser SiteConfigParser) : ISiteConfigLoader {
    public async Task<Result<SiteConfig, ResultErrors>> Load() {
        var siteConfigPath = Path.Join(basePath, Website.SiteConfigFileName);

        if (!File.Exists(siteConfigPath)) {
            return default;
        }

        using var reader = File.OpenRead(siteConfigPath);
        return await SiteConfigParser.Parse(reader);
    }
}
