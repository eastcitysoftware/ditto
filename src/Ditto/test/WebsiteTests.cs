using System.Text.RegularExpressions;

namespace Ditto.Tests;

public sealed class WebsiteGeneratorTests {
    [Fact]
    public async Task GenerateWebsite_CreatesOutputFiles_WhenValidInput() {
        var layoutLoader = new LayoutLoader(Shared.BasePath);
        var partialLoader = new PartialLoader(Shared.BasePath);
        var layouts = await layoutLoader.LoadLayouts();
        var partials = await partialLoader.LoadPartials();

        var generator = new WebsiteGenerator(
            pageFileLoader: new PageFileLoader(Shared.BasePath, Shared.OutputPath),
            pageParser: new PageParser(Shared.SiteConfig),
            pageWriter: new PageWriter(layouts, partials));

        await generator.Generate(Shared.BasePath, Shared.OutputPath);

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

        var indexFileResult = File.ReadAllText(indexFile);
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
        AssetFileContentsEqual(indexExpected, indexFileResult);

        var aboutFileResult = File.ReadAllText(aboutFile);
        var aboutExpected = """
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
                    <footer>
                        <p>&copy; Example Site</p>
                    </footer>
                </main>
            </body>
        </html>
        """;
        AssetFileContentsEqual(aboutExpected, aboutFileResult);
    }

    private void AssetFileContentsEqual(string expected, string result) {
        Assert.Equal(
            StripWhitespaceBetweenTags(expected),
            StripWhitespaceBetweenTags(result));
    }

    private string StripWhitespaceBetweenTags(string input) {
        return Regex.Replace(input, @"\s*(<[^>]+>)\s*", "$1");
    }
}
