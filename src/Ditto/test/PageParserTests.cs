namespace Ditto.Tests;

public sealed class PageParserTests {
    [Fact]
    public async Task Parse_ReturnsPage_WhenValidInput() {
        var pageContent = """
            ---
            title = "Test Page"
            description = "This is a test page."
            layout = "TestTemplate"
            ---
            <h1>{{title}}</h1>
            <h2>{{description}}</h2>
            <p>This is the content of the test page.</p>
            """;

        var input = new StringReader(pageContent);
        var pageParser = new PageParser(Shared.SiteConfig);
        var page = await pageParser.Parse(input);

        Assert.Equal($"Test Page - {Shared.SiteConfig.Title}", page.Title);
        Assert.Equal("This is a test page.", page.Description);
        Assert.Equal("TestTemplate", page.Layout);
        Assert.NotNull(page.Metadata);
        Assert.Contains("title", page.Metadata.Keys);
        Assert.Contains("description", page.Metadata.Keys);

        var expectedTemplate = """
            <h1>{{title}}</h1>
            <h2>{{description}}</h2>
            <p>This is the content of the test page.</p>
            """;

        Assert.Equal(expectedTemplate, page.Template);
    }

    [Fact]
    public async Task Parse_ReturnsPage_WithDefaultTemplate() {
        var pageContent = """
            ---
            title = "Test Page"
            description = "This is a test page."
            ---
            <h1>{{title}}</h1>
            <h2>{{description}}</h2>
            <p>This is the content of the test page.</p>
            """;

        var input = new StringReader(pageContent);
        var pageParser = new PageParser(Shared.SiteConfig);
        var page = await pageParser.Parse(input);

        Assert.Equal($"Test Page - {Shared.SiteConfig.Title}", page.Title);
        Assert.Equal("This is a test page.", page.Description);
        Assert.Equal(Website.DefaultLayoutName, page.Layout);
        Assert.NotNull(page.Metadata);
        Assert.Contains("title", page.Metadata.Keys);
        Assert.Contains("description", page.Metadata.Keys);

        var expectedTemplate = """
            <h1>{{title}}</h1>
            <h2>{{description}}</h2>
            <p>This is the content of the test page.</p>
            """;

        Assert.Equal(expectedTemplate, page.Template);
    }

    [Fact]
    public async Task Parse_ReturnsPage_WithNoFrontMatter() {
        var pageContent = """
            <h1>No Front Matter</h1>
            <p>This page has no front matter.</p>
            """;

        var input = new StringReader(pageContent);
        var pageParser = new PageParser(Shared.SiteConfig);
        var page = await pageParser.Parse(input);

        Assert.Equal(Shared.SiteConfig.Title, page.Title);
        Assert.Equal(Shared.SiteConfig.Description, page.Description);
        Assert.Equal(Website.DefaultLayoutName, page.Layout);
        Assert.NotNull(page.Metadata);
        Assert.Empty(page.Metadata);

        var expectedTemplate = """
            <h1>No Front Matter</h1>
            <p>This page has no front matter.</p>
            """;

        Assert.Equal(expectedTemplate, page.Template);
    }
}
