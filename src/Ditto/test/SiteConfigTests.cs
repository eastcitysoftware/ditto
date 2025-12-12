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
            contact_email = 'info@eastcitysoftware.com'
        """;

        using var file = new MemoryStream(Encoding.UTF8.GetBytes(toml));
        var config = await SiteConfigParser.Parse(file);
        Assert.True(config.IsOk);

        if (config.TryGet(out var siteConfig)) {
            Assert.Equal("https://example.com/", siteConfig.BaseUrl);
            Assert.Equal("Example Site", siteConfig.Title);
            Assert.Equal("This is an example site.", siteConfig.Description);
            Assert.Equal(Website.DefaultTitleSeparator, siteConfig.TitleSeparator);
            Assert.True(siteConfig.Data.ContainsKey("contact_email"));
            Assert.Equal("info@eastcitysoftware.com", siteConfig.Data["contact_email"]);
        }
    }

    [Fact]
    public async Task Parse_ReturnsNull_WhenMissingRequiredFields() {
        var toml = @"
            title = 'Example Site'
        ";

        using var input = new MemoryStream(Encoding.UTF8.GetBytes(toml));
        var config = await SiteConfigParser.Parse(input);
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
            Assert.Equal("https://example.com/", siteConfig.BaseUrl);
            Assert.Equal("Example Site", siteConfig.Title);
            Assert.Equal("This is an example site.", siteConfig.Description);
            Assert.Equal(Website.DefaultTitleSeparator, siteConfig.TitleSeparator);
        }
    }

    [Fact]
    public async Task Load_ReturnsNull_WhenFileDoesNotExist() {
        var loader = new SiteConfigLoader("nonexistent.toml");
        var config = await loader.Load();
        Assert.True(config.IsError);
    }
}
