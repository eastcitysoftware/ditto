namespace Ditto.Tests;

public sealed class SiteConfigTests {
    [Fact]
    public async Task Parse_ReturnsSiteConfig_WhenValidToml() {
        var toml = $"""
            base_url = 'https://example.com'
            title = 'Example Site'
            description = 'This is an example site.'
            title_separator = '{SiteConfig.DefaultTitleSeparator}'
        """;

        using var reader = new StringReader(toml);
        var parser = new SiteConfigParser();
        var config = await parser.Parse(reader);
        Assert.True(config.IsOk);

        if (config.TryGet(out var siteConfig)) {
            Assert.Equal("https://example.com", siteConfig.BaseUrl);
            Assert.Equal("Example Site", siteConfig.Title);
            Assert.Equal("This is an example site.", siteConfig.Description);
            Assert.Equal(SiteConfig.DefaultTitleSeparator, siteConfig.TitleSeparator);
        }
    }



    [Fact]
    public async Task Parse_ReturnsNull_WhenMissingRequiredFields() {
        var toml = @"
            title = 'Example Site'
        ";

        using var reader = new StringReader(toml);
        var parser = new SiteConfigParser();
        var config = await parser.Parse(reader);
        Assert.True(config.IsError);
    }
}

public sealed class SiteConfigLoaderTests {
    [Fact]
    public async Task Load_ReturnsSiteConfig_WhenFileExists() {
        var loader = new SiteConfigLoader(Shared.BasePath);
        var config = await loader.Load();
        Assert.True(config.IsOk);

        if (config.TryGet(out var siteConfig)) {
            Assert.Equal("https://example.com", siteConfig.BaseUrl);
            Assert.Equal("Example Site", siteConfig.Title);
            Assert.Equal("This is an example site.", siteConfig.Description);
            Assert.Equal(SiteConfig.DefaultTitleSeparator, siteConfig.TitleSeparator);
        }
    }

    [Fact]
    public async Task Load_ReturnsNull_WhenFileDoesNotExist() {
        var loader = new SiteConfigLoader("nonexistent.toml");
        var config = await loader.Load();
        Assert.True(config.IsError);
    }
}
