namespace Ditto.Tests;

public sealed class PageParserTests {
    private readonly PageParser _pageParser = new(Shared.SiteConfig);

    [Fact]
    public async Task Parse_ReturnsPage_WhenValidInput() {
        var pageContent = """
            ---
            title = "Test Page"
            description = "This is a test page."
            layout = "test-template"
            tags = ["test", "sample"]
            published = 2023-10-05
            ---
            <h1>{{title}}</h1>
            <h2>{{description}}</h2>
            <p>This is the content of the test page.</p>
            """;

        var pageFile = new PageFile(
            InputPath: Path.ChangeExtension(Path.GetRandomFileName(), "html"),
            OutputPath: Path.ChangeExtension(Path.GetRandomFileName(), "html"),
            Path: "/about/valid-page/",
            PageName: "valid-page",
            ViewType: ViewType.Html);

        using var input = new StringReader(pageContent);
        var pageResult = await _pageParser.Parse(input, pageFile);

        if(!pageResult.TryGet(out var page)) {
            Assert.Fail("Page parsing failed with errors");
            return;
        }

        Assert.Equal(pageFile.Path, page.Path);
        Assert.Equal(pageFile.PageName, page.Slug);
        Assert.Equal("/about/valid-page/", page.Path);
        Assert.Equal("valid-page", page.Slug);
        Assert.Equal($"{Shared.SiteConfig.BaseUrl}{page.Path}", page.Url);
        Assert.Equal("Test Page", page.PageTitle);
        Assert.Equal($"Test Page{Shared.SiteConfig.TitleSeparator}{Shared.SiteConfig.Title}", page.Title);
        Assert.Equal("This is a test page.", page.Description);
        Assert.Equal(new DateTime(2023, 10, 5), page.Published);

        Assert.NotNull(page.Data);
        Assert.DoesNotContain("title", page.Data.Keys);
        Assert.DoesNotContain("description", page.Data.Keys);
        Assert.DoesNotContain("layout", page.Data.Keys);
        Assert.DoesNotContain("tags", page.Data.Keys);
        Assert.DoesNotContain("published", page.Data.Keys);

        Assert.Equal(2, page.Tags.Length);

        Assert.Equal(pageFile.PageName, page.View.Name);
        Assert.Equal("test-template", page.View.LayoutName);
        Assert.Equal(ViewType.Html, page.View.Type);

        var result = """
            <h1>{{title}}</h1>
            <h2>{{description}}</h2>
            <p>This is the content of the test page.</p>
            """;

        Assert.Equal(result, page.View.Content);
    }

    [Fact]
    public async Task Parse_ReturnsPage_WithDefaultLayout() {
        var pageContent = """
            ---
            title = "Test Page"
            description = "This is a test page."
            ---
            <h1>{{title}}</h1>
            <h2>{{description}}</h2>
            <p>This is the content of the test page.</p>
            """;


        var pageFile = new PageFile(
            InputPath: Path.ChangeExtension(Path.GetRandomFileName(), "html"),
            OutputPath: Path.ChangeExtension(Path.GetRandomFileName(), "html"),
            Path: "/valid-page/",
            PageName: "valid-page",
            ViewType: ViewType.Html);

        using var input = new StringReader(pageContent);

        var pageResult = await _pageParser.Parse(input, pageFile);

        if(!pageResult.TryGet(out var page)) {
            Assert.Fail("Page parsing failed with errors");
            return;
        }

        Assert.Equal(pageFile.Path, page.Path);
        Assert.Equal($"{Shared.SiteConfig.BaseUrl}{page.Path}", page.Url);
        Assert.Equal("Test Page", page.PageTitle);
        Assert.Equal($"Test Page{Shared.SiteConfig.TitleSeparator}{Shared.SiteConfig.Title}", page.Title);
        Assert.Equal("This is a test page.", page.Description);
        Assert.Null(page.Published);

        Assert.NotNull(page.Data);
        Assert.DoesNotContain("title", page.Data.Keys);
        Assert.DoesNotContain("description", page.Data.Keys);
        Assert.DoesNotContain("layout", page.Data.Keys);
        Assert.DoesNotContain("tags", page.Data.Keys);
        Assert.DoesNotContain("published", page.Data.Keys);

        Assert.Empty(page.Tags);

        Assert.Equal(pageFile.PageName, page.View.Name);
        Assert.Equal(Website.DefaultLayoutName, page.View.LayoutName);
        Assert.Equal(ViewType.Html, page.View.Type);

        var result = """
            <h1>{{title}}</h1>
            <h2>{{description}}</h2>
            <p>This is the content of the test page.</p>
            """;

        Assert.Equal(result, page.View.Content);
    }

    [Fact]
    public async Task Parse_ReturnsPage_WithNoFrontMatter() {
        var pageContent = """
            <h1>No Front Matter</h1>
            <p>This page has no front matter.</p>
            """;

        var pageFile = new PageFile(
            InputPath: Path.ChangeExtension(Path.GetRandomFileName(), "html"),
            OutputPath: Path.ChangeExtension(Path.GetRandomFileName(), "html"),
            Path: "/valid-page/",
            PageName: "valid-page",
            ViewType: ViewType.Html);

        var input = new StringReader(pageContent);

        var pageResult = await _pageParser.Parse(input, pageFile);

        if(!pageResult.TryGet(out var page)) {
            Assert.Fail("Page parsing failed with errors");
            return;
        }

        Assert.Equal(pageFile.Path, page.Path);
        Assert.Equal($"{Shared.SiteConfig.BaseUrl}{page.Path}", page.Url);

        Assert.Null(page.PageTitle);
        Assert.Equal(Shared.SiteConfig.Title, page.Title);
        Assert.Equal(Shared.SiteConfig.Description, page.Description);
        Assert.Empty(page.Tags);
        Assert.Null(page.Published);

        Assert.NotNull(page.Data);
        Assert.Empty(page.Data);

        Assert.Equal(pageFile.PageName, page.View.Name);
        Assert.Equal(Website.DefaultLayoutName, page.View.LayoutName);
        Assert.Equal(ViewType.Html, page.View.Type);

        var result = """
            <h1>No Front Matter</h1>
            <p>This page has no front matter.</p>
            """;

        Assert.Equal(result, page.View.Content);
    }
}

public sealed class UrlHelperTests {
    [Theory]
    [InlineData("https://example.com", "/about/", "https://example.com/about/")]
    [InlineData("https://example.com/", "/about/", "https://example.com/about/")]
    [InlineData("https://example.com", "about/", "https://example.com/about/")]
    [InlineData("https://example.com/", "about/", "https://example.com/about/")]
    [InlineData("https://example.com/base", "/about/", "https://example.com/base/about/")]
    [InlineData("https://example.com/base/", "/about/", "https://example.com/base/about/")]
    [InlineData("https://example.com/base", "about/", "https://example.com/base/about/")]
    [InlineData("https://example.com/base/", "about/", "https://example.com/base/about/")]
    public void Combine_WorksAsExpected(string baseUrl, string relativePath, string expected) {
        var result = UrlHelper.Combine(baseUrl, relativePath);
        Assert.Equal(expected, result);
    }
}
