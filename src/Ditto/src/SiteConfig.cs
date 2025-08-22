using Danom;
using Tomlyn;

namespace Ditto;

public sealed record SiteConfig(
    string BaseUrl,
    string Title,
    string Description,
    string TitleSeparator = SiteConfig.DefaultTitleSeparator) {
    public const string DefaultTitleSeparator = " - ";
}

public interface ISiteConfigParser {
    Task<Result<SiteConfig, ResultErrors>> Parse(TextReader input);
}

public sealed class SiteConfigParser : ISiteConfigParser {
    public async Task<Result<SiteConfig, ResultErrors>> Parse(TextReader input) {
        var toml = Toml.ToModel(await input.ReadToEndAsync());
        var baseUrl = toml?.GetString("base_url");
        var title = toml?.GetString("title");
        var description = toml?.GetString("description");

        if (string.IsNullOrWhiteSpace(baseUrl)) {
            return Result<SiteConfig>.Error("Site configuration is missing required field: base_url.");
        }

        if (string.IsNullOrWhiteSpace(title)) {
            return Result<SiteConfig>.Error("Site configuration is missing required field: title.");
        }

        if (string.IsNullOrWhiteSpace(description)) {
            return Result<SiteConfig>.Error("Site configuration is missing required field: description.");
        }

        return Result<SiteConfig>.Ok(new(
             BaseUrl: baseUrl,
             Title: title,
             Description: description,
             TitleSeparator: toml?.GetString("title_separator") ?? SiteConfig.DefaultTitleSeparator));
    }
}

public interface ISiteConfigLoader {
    Task<Result<SiteConfig, ResultErrors>> Load();
}

public sealed class SiteConfigLoader(string basePath) : ISiteConfigLoader {
    private readonly SiteConfigParser _parser = new();

    public async Task<Result<SiteConfig, ResultErrors>> Load() {
        var siteConfigPath = Path.Join(basePath, Website.SiteConfigFileName);

        if (!File.Exists(siteConfigPath)) {
            return default;
        }

        using var reader = new StreamReader(siteConfigPath);
        return await _parser.Parse(reader);
    }
}
