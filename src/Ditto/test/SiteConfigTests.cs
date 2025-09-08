using System.Text;

namespace Ditto.Tests;

public sealed class SiteConfigTests {
    [Fact]
    public async Task Parse_ReturnsSiteConfig_WhenValidToml() {
        var toml = $"""
            base_url = 'https://example.com'
            title = 'Example Site'
            description = 'This is an example site.'
            title_separator = '{Website.DefaultTitleSeparator}'
        """;

        using var file = new MemoryStream(Encoding.UTF8.GetBytes(toml));
        var parser = new SiteConfigParser(Shared.ModelValuesParser);
        var config = await parser.Parse(file);
        Assert.True(config.IsOk);

        if (config.TryGet(out var siteConfig)) {
            Assert.Equal("https://example.com/", siteConfig.BaseUrl);
            Assert.Equal("Example Site", siteConfig.Title);
            Assert.Equal("This is an example site.", siteConfig.Description);
            Assert.Equal(Website.DefaultTitleSeparator, siteConfig.TitleSeparator);
        }
    }

    [Fact]
    public async Task Parse_ReturnsNull_WhenMissingRequiredFields() {
        var toml = @"
            title = 'Example Site'
        ";

        using var input = new MemoryStream(Encoding.UTF8.GetBytes(toml));
        var parser = new SiteConfigParser(Shared.ModelValuesParser);
        var config = await parser.Parse(input);
        Assert.True(config.IsError);
    }
}

public sealed class SiteConfigLoaderTests {
    private readonly ISiteConfigParser _siteConfigParser = new SiteConfigParser(Shared.ModelValuesParser);

    [Fact]
    public async Task Load_ReturnsSiteConfig_WhenFileExists() {
        var loader = new SiteConfigLoader(Shared.BasePath, _siteConfigParser);
        var config = await loader.Load();
        Assert.True(config.IsOk);

        if (config.TryGet(out var siteConfig)) {
            Assert.Equal("https://example.com/", siteConfig.BaseUrl);
            Assert.Equal("Example Site", siteConfig.Title);
            Assert.Equal("This is an example site.", siteConfig.Description);
            Assert.Equal(Website.DefaultTitleSeparator, siteConfig.TitleSeparator);
        }
    }

    [Fact]
    public async Task Load_ReturnsNull_WhenFileDoesNotExist() {
        var loader = new SiteConfigLoader("nonexistent.toml", _siteConfigParser);
        var config = await loader.Load();
        Assert.True(config.IsError);
    }
}
