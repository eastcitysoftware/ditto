namespace Ditto.Tests;

public sealed class PageWriterTests {
    [Fact]
    public async Task WritePage_CreatesValidPageFile() {
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

        var layouts = await new DummyLayoutsLoader().LoadLayouts();
        var partials = await new DummyPartialsLoader().LoadPartials();

        var pageWriter = new PageWriter(layouts, partials);
        using var output = new StringWriter();
        await pageWriter.Render(page, output);
        var result = output.ToString();
        var expected = $"""
            <h1>Test Page{Shared.SiteConfig.TitleSeparator}{Shared.SiteConfig.Title}</h1>
            <h2>This is a test page.</h2>
            <p>This is the content of the test page.</p>
            """;
        Assert.Equal(expected, result);
    }

    public sealed class DummyLayoutsLoader : ILayoutLoader {
        public Task<Layouts> LoadLayouts() =>
            Task.FromResult(new Layouts());
    }

    public sealed class DummyPartialsLoader : IPartialLoader {
        public Task<Partials> LoadPartials() =>
            Task.FromResult(new Partials());
    }
}


public sealed class LayoutLoaderTests {
    [Fact]
    public async Task LoadLayouts_ReturnsLayouts_WhenValidInput() {
        var layoutLoader = new LayoutLoader(Shared.BasePath);
        var layouts = await layoutLoader.LoadLayouts();
        Assert.NotNull(layouts);
        Assert.NotEmpty(layouts.Names);
        Assert.Contains("default", layouts.Names);
        Assert.Contains("test-template", layouts.Names);
    }
}

public sealed class PartialLoaderTests {
    [Fact]
    public async Task LoadPartials_ReturnsPartials_WhenValidInput() {
        var partialLoader = new PartialLoader(Shared.BasePath);
        var partials = await partialLoader.LoadPartials();
        Assert.NotNull(partials);
        Assert.NotEmpty(partials.Names);
        Assert.Contains("header", partials.Names);
        Assert.Contains("footer", partials.Names);
    }
}
