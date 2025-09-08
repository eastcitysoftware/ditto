namespace Ditto.Tests;

public sealed class ViewEngineTests {
    [Fact]
    public async Task Render_ReturnsRenderedString_WithLayoutAndPartial() {
        var pageContent = """
            <h1>{{title}}</h1>
            <h2>{{description}}</h2>
            <p>This is the content of the test page.</p>
            """;

        var page = new Page(
            Path: "/test-page",
            Url: "https://example.com/test-page",
            Title: $"Test Page{Shared.SiteConfig.TitleSeparator}{Shared.SiteConfig.Title}",
            Description: "This is a test page.",
            Tags: [],
            Data: new Dictionary<string, object>(),
            View: new(Path.GetRandomFileName(), pageContent, ViewType.Html, "test-template"));

        var result = await Shared.TestViewEngine.Render(page, Shared.SupplementalData);

        var expected = $"""
            <html>
                <head>
                    <title>Test Page{Shared.SiteConfig.TitleSeparator}{Shared.SiteConfig.Title}</title>
                    <meta name="description" content="This is a test page.">
                </head>
                <body>
                    <header>Test Template</header>
                    <main>
                        <h1>Test Page{Shared.SiteConfig.TitleSeparator}{Shared.SiteConfig.Title}</h1>
                        <h2>This is a test page.</h2>
                        <p>This is the content of the test page.</p>
                    </main>
                    <footer>
                        <p>&copy; {Shared.SiteConfig.Title}</p>
                    </footer>
                </body>
            </html>
            """;

        Shared.AssertHtmlEqual(expected, result);
    }

    [Fact]
    public async Task Render_ReturnsRenderedString_ForMarkdown() {

        var pageContent = """
            # Test Page{{site.title_separator}}{{site.title}}

            This is a test page.

            This is the content of the test page.
            """;

        var page = new Page(
            Path: "/test-page",
            Url: "https://example.com/test-page",
            Title: $"Test Page{Shared.SiteConfig.TitleSeparator}{Shared.SiteConfig.Title}",
            Description: "This is a test page.",
            Tags: [],
            Data: new Dictionary<string, object>(),
            View: new(Path.GetRandomFileName(), pageContent, ViewType.Markdown, "test-template"));

        var result = await Shared.TestViewEngine.Render(page, Shared.SupplementalData);

        var expected = $"""
            <html>
                <head>
                    <title>Test Page{Shared.SiteConfig.TitleSeparator}{Shared.SiteConfig.Title}</title>
                    <meta name="description" content="This is a test page.">
                </head>
                <body>
                    <header>Test Template</header>
                    <main>
                        <h1>Test Page{Shared.SiteConfig.TitleSeparator}{Shared.SiteConfig.Title}</h1>
                        <p>This is a test page.</p>
                        <p>This is the content of the test page.</p>
                    </main>
                    <footer>
                        <p>&copy; {Shared.SiteConfig.Title}</p>
                    </footer>
                </body>
            </html>
            """;

        Shared.AssertHtmlEqual(expected, result);
    }
}

public sealed class ViewRendererTests {
    [Fact]
    public async Task Render_ReturnsRenderedString_ForValidViewAndModel() {
        var view = "Hello, {{name}}!";
        var model = new { name = "World" };
        var renderer = new ViewRenderer();

        var result = await renderer.Render(view, model, default);

        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public async Task Render_ReturnsView_WhenModelIsEmpty() {
        var view = "Hello, {{name}}!";
        var model = new { };
        var renderer = new ViewRenderer();

        var result = await renderer.Render(view, model, default);

        Assert.Equal("Hello, !", result);
    }

    [Fact]
    public async Task Render_ReturnsView_WithPartial_WhenPartialExists() {
        var partials = new ViewCollection(new Dictionary<string, View> {
            { "greeting", new View("greeting", "Hello, {{name}}!", ViewType.Html) }
        });
        var view = "{{ include 'greeting' }}";
        var model = new { name = "Alice" };
        var renderer = new ViewRenderer(partials);

        var result = await renderer.Render(view, model, default);
        Assert.Equal("Hello, Alice!", result);
    }

    [Fact]
    public async Task Render_DateOnly_ToDateTimeExtension_WorksInTemplate() {
        var view = "The date is {{ test_date | date_only.to_date_time | date.to_string '%F %T' }}.";
        var model = new { testDate = new DateOnly(2024, 6, 15) };
        var renderer = new ViewRenderer();

        var result = await renderer.Render(view, model, default);

        Assert.Equal("The date is 2024-06-15 00:00:00.", result);
    }
}

public sealed class ViewCollectionTests {
    [Fact]
    public void Get_ReturnsView_ForExistingName() {
        var viewCollection = new ViewCollection(
            new Dictionary<string, View>() {
                { "name", new("name", "content", ViewType.Html) }
            });

        var view = viewCollection.Get("name");
        Assert.NotNull(view);
        Assert.Equal("name", view?.Name);
        Assert.Equal("content", view?.Content);
    }

    [Fact]
    public void Get_ReturnsNull_ForNonExistingName() {
        var viewCollection = new ViewCollection(
            new Dictionary<string, View>() {
                { "name", new("name", "content", ViewType.Html) }
            });

        var view = viewCollection.Get("missing");
        Assert.Null(view);
    }
}

public sealed class ViewLoaderTests {
    [Fact]
    public async Task LoadViews_ReturnsLayoutViews_WhenValidInput() {
        var views = await Shared.TestViewLoader.LoadViews(Website.LayoutsDirectory);
        Assert.NotNull(views);
        Assert.NotEmpty(views.Names);
        Assert.Contains("default", views.Names);
        Assert.Contains("test-template", views.Names);
    }

    [Fact]
    public async Task LoadViews_ReturnsPartialViews_WhenValidInput() {
        var views = await Shared.TestViewLoader.LoadViews(Website.PartialsDirectory);
        Assert.NotNull(views);
        Assert.NotEmpty(views.Names);
        Assert.Contains("header", views.Names);
        Assert.Contains("footer", views.Names);
    }
}

public sealed class DocumentProcessorTests {
    [Fact]
    public async Task ProcessMarkdownAsync_ReturnsHtml_ForValidMarkdown() {
        var markdown = """
            # Title

            This is a **bold** text and this is *italic* text.

            - Item 1
            - Item 2
            - Item 3
            """;

        var result = await Shared.TestDocumentProcessor.Process(markdown);

        var expected = """
            <h1>Title</h1>
            <p>This is a <strong>bold</strong> text and this is <em>italic</em> text.</p>
            <ul>
            <li>Item 1</li>
            <li>Item 2</li>
            <li>Item 3</li>
            </ul>
            """;

        Shared.AssertHtmlEqual(expected, result);
    }

    [Fact]
    public async Task ProcessMarkdownAsync_ReturnsEmptyString_WhenInputIsEmpty() {
        var markdown = string.Empty;

        var result = await Shared.TestDocumentProcessor.Process(markdown);

        Assert.Equal(string.Empty, result);
    }
}
