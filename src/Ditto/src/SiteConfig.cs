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
    Task<SiteConfig?> Parse(TextReader input);
}

public sealed class SiteConfigParser : ISiteConfigParser {
    public async Task<SiteConfig?> Parse(TextReader input) {
        var toml = Toml.ToModel(await input.ReadToEndAsync());
        if (toml?.GetString("base_url") is string baseUrl
            && toml?.GetString("title") is string title
            && toml?.GetString("description") is string description) {
            return new(
                BaseUrl: baseUrl,
                Title: title,
                Description: description,
                TitleSeparator: toml.GetString("title_separator") ?? SiteConfig.DefaultTitleSeparator);
        }
        return default;
    }
}

public interface ISiteConfigLoader {
    Task<SiteConfig?> Load();
}

public sealed class SiteConfigLoader(string basePath) : ISiteConfigLoader {
    private readonly SiteConfigParser _parser = new();

    public async Task<SiteConfig?> Load() {
        var siteConfigPath = Path.Join(basePath, Website.SiteConfigFileName);

        if (!File.Exists(siteConfigPath)) {
            return default;
        }

        using var reader = new StreamReader(siteConfigPath);
        return await _parser.Parse(reader);
    }
}
