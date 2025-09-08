namespace Ditto.Tests;

public sealed class WebsiteGeneratorTests {
    [Fact]
    public async Task GenerateWebsite_CreatesOutputFiles_WhenValidInput() {
        var generator = new WebsiteGenerator(
            pageFileLoader: new PageFileLoader(Shared.BasePath, Shared.OutputPath),
            pageParser: new PageParser(Shared.ModelValuesParser, Shared.SiteConfig),
            viewEngine: Shared.TestViewEngine);

        var result = await generator.Generate(Shared.BasePath, Shared.OutputPath, Shared.SiteConfig);
        Assert.True(result.IsOk);

        Assert.True(Directory.Exists(Shared.OutputPath));

        var indexFile = Path.Join(Shared.OutputPath, "index.html");
        var aboutFile = Path.Join(Shared.OutputPath, "about", "index.html");

        var expectedFiles = new[] { indexFile, aboutFile,
            Path.Join(Shared.OutputPath, "posts", "1999-01-01-hello", "index.html"),
            Path.Join(Shared.OutputPath, "posts", "1999-01-02-hello-copy", "index.html"),
        };

        var actualFiles = Directory.GetFiles(Shared.OutputPath, "index.html", SearchOption.AllDirectories);

        Assert.Equal(expectedFiles.Length, actualFiles.Length);

        Assert.All(expectedFiles, expectedFile => {
            Assert.Contains(expectedFile, actualFiles);
            Assert.True(File.Exists(expectedFile));
        });

        var indexFileResult = await File.ReadAllTextAsync(indexFile);
        var indexExpected = """
        <html>
            <head>
                <title>Example Site</title>
                <meta name="description" content="This is an example site.">
            </head>
            <body>
                <h1>Example Site</h1>
                <h2>This is an example site.</h2>
                <p>This is the content of the test page.</p>
            </body>
        </html>
        """;
        Shared.AssertHtmlEqual(indexExpected, indexFileResult);

        var aboutFileResult = await File.ReadAllTextAsync(aboutFile);
        var aboutExpected = $"""
        <html>
            <head>
                <title>About Page - Example Site</title>
                <meta name="description" content="This is the about page description.">
            </head>
            <body>
                <header>Test Template</header>
                <main>
                    <h1>About Page - Example Site</h1>
                    <h2>This is the about page description.</h2>
                    <p>This is the content of the about page.</p>
                </main>
                <footer>
                    <p>&copy; {Shared.SiteConfig.Title}</p>
                </footer>
            </body>
        </html>
        """;

        Shared.AssertHtmlEqual(aboutExpected, aboutFileResult);
    }
}

public sealed class PageCollectionTests {
    [Fact]
    public void PageCollection_WorksAsExpected() {
        var pages = new List<Page> {
            CreatePage("blog/post1"),
            CreatePage("blog/post2"),
            CreatePage("blog/post3"),
            CreatePage("about/index"),
            CreatePage("contact"),
            CreatePage("index")
        };

        var pageCollection = PageCollection.CreateFromPages(pages);

        Assert.NotNull(pageCollection);
        Assert.Equal(3, pageCollection["blog"].Count);
        Assert.All(pageCollection["blog"], page => Assert.StartsWith("/blog/", page.Path));
    }

    private static Page CreatePage(string path) =>
        new(Path: $"/{path}",
            Url: $"https://example.com/{path}",
            Title: Path.GetRandomFileName(),
            Description: Path.GetRandomFileName(),
            Tags: [],
            Data: new Dictionary<string, object>(),
            View: new("valid-page", "<h1>{{title}}</h1>", ViewType.Html));
}

public sealed class PathHelperTests {
    [Theory]
    [InlineData("/some-folder", false)]
    [InlineData(@"C:\some-folder", false)]
    [InlineData(@"C:\Windows", true)]
    [InlineData(@"C:\Windows\System32", true)]
    [InlineData(@"C:\Program Files", true)]
    [InlineData("/bin", true)]
    [InlineData("/boot", true)]
    [InlineData("/dev", true)]
    [InlineData("/etc", true)]
    [InlineData("/lib", true)]
    [InlineData("/lib64", true)]
    [InlineData("/proc", true)]
    [InlineData("/root", true)]
    [InlineData("/sbin", true)]
    [InlineData("/sys", true)]
    [InlineData("/usr", true)]
    [InlineData("/var" , true)]
    public void IsSystemPath_ReturnsExpected(string filePath, bool expected) {
        Assert.Equal(expected, PathHelper.IsSystemPath(filePath));
    }
}
