namespace Ditto.Tests;

public sealed class PageFileLoaderTests {
    [Fact]
    public void LoadFiles_ReturnsPageFiles_WhenValidInput() {
        var pageFileLoader = new PageFileLoader(Shared.BasePath, Shared.OutputPath);
        var pageFiles = pageFileLoader.LoadFiles();

        Assert.NotNull(pageFiles);
        Assert.NotEmpty(pageFiles);
        Assert.Equal(5, pageFiles.Count);
        Assert.All(pageFiles, pageFile => {
            Assert.NotNull(pageFile.InputPath);
            Assert.True(File.Exists(pageFile.InputPath));
            Assert.NotNull(pageFile.OutputPath);
            Assert.NotNull(pageFile.Path);
            Assert.StartsWith("/", pageFile.Path);
            Assert.False(string.IsNullOrWhiteSpace(pageFile.PageName));
            Assert.Equal("index.html", Path.GetFileName(pageFile.OutputPath)); // every page file should be named index.html
            Assert.StartsWith(Shared.OutputPath, pageFile.OutputPath);

            if (Path.GetRelativePath(Shared.BasePath, pageFile.InputPath) == "index.html") {
                // the index page should map to the output path directly
                Assert.Equal("index.html", Path.GetRelativePath(Shared.OutputPath, pageFile.OutputPath));
            }
        });
    }
}

public sealed class PageFileHelperTests {
    [Theory]
    [InlineData(@"C:\users\ditto\website\src", @"C:\users\ditto\website\src\index.html", "/")]
    [InlineData(@"C:\users\ditto\website\src", @"C:\users\ditto\website\src\about.html", "/about/")]
    [InlineData(@"C:\users\ditto\website\src", @"C:\users\ditto\website\src\blog\post1.html", "/blog/post1/")]
    [InlineData(@"/users/ditto/website/src", @"/users/ditto/website/src/index.html", "/")]
    [InlineData(@"/users/ditto/website/src", @"/users/ditto/website/src/about.html", "/about/")]
    [InlineData(@"/users/ditto/website/src", @"/users/ditto/website/src/blog/post1.html", "/blog/post1/")]
    public void GetPath_ReturnsExpectedPath(string basePath, string filePath, string expectedPath) {
        Assert.Equal(expectedPath, PageFileLoader.FileHelper.GetPath(basePath, filePath));
    }

    [Theory]
    [InlineData(@"C:\users\ditto\website\src", @"C:\users\ditto\website\src\index.html", "index")]
    [InlineData(@"C:\users\ditto\website\src", @"C:\users\ditto\website\src\about.html", "about")]
    [InlineData(@"C:\users\ditto\website\src", @"C:\users\ditto\website\src\blog\post1.html", "blog/post1")]
    [InlineData(@"/users/ditto/website/src", @"/users/ditto/website/src/index.html", "index")]
    [InlineData(@"/users/ditto/website/src", @"/users/ditto/website/src/about.html", "about")]
    [InlineData(@"/users/ditto/website/src", @"/users/ditto/website/src/blog/post1.html", "blog/post1")]
    public void GetPageName_ReturnsExpected(string basePath, string filePath, string expected) {
        Assert.Equal(expected, PageFileLoader.FileHelper.GetPageName(basePath, filePath));
    }

    [Theory]
    [InlineData("about.md", ViewType.Markdown)]
    [InlineData("contact.html", ViewType.Html)]
    [InlineData("post", ViewType.Html)]
    public void GetViewType_ReturnsExpectedViewType(string fileName, ViewType expectedViewType) {
        Assert.Equal(expectedViewType, PageFileLoader.FileHelper.GetViewType(fileName));
    }

    [Fact]
    public void LoadFiles_SkipsLayoutsAndPartials() {
        var pageFileLoader = new PageFileLoader(Shared.BasePath, Shared.OutputPath);
        var pageFiles = pageFileLoader.LoadFiles();

        Assert.DoesNotContain(pageFiles, pageFile => pageFile.InputPath.StartsWith("_layouts"));
        Assert.DoesNotContain(pageFiles, pageFile => pageFile.InputPath.StartsWith("_partials"));
    }

    // [Fact]
    // public void LoadFiles_IgnoresUnsupportedExtensions() {
    //     var pageFileLoader = new PageFileLoader(Shared.BasePath, Shared.OutputPath);

    //     // Create a mock file with an unsupported extension
    //     var unsupportedFile = Path.Combine(Shared.BasePath, "unsupported.txt");
    //     File.WriteAllText(unsupportedFile, "unsupported content");

    //     var pageFiles = pageFileLoader.LoadFiles();

    //     Assert.DoesNotContain(pageFiles, pageFile => pageFile.InputPath == unsupportedFile);

    //     // Cleanup
    //     File.Delete(unsupportedFile);
    // }

    // [Theory]
    // [InlineData("index.html", "index.html")]
    // [InlineData("about.html", "about/index.html")]
    // // [InlineData("blog/post1.html", "blog/post1/index.html")]
    // public void LoadFiles_GeneratesCorrectOutputPath(string inputFile, string expectedOutputFile) {
    //     var pageFileLoader = new PageFileLoader(Shared.BasePath, Shared.OutputPath);

    //     // Create a mock file
    //     var inputFilePath = Path.Combine(Shared.BasePath, inputFile);
    //     File.WriteAllText(inputFilePath, "content");

    //     var pageFiles = pageFileLoader.LoadFiles();

    //     var pageFile = pageFiles.FirstOrDefault(pf => pf.InputPath == inputFilePath);
    //     Assert.NotNull(pageFile);
    //     Assert.Equal(Path.Combine(Shared.OutputPath, expectedOutputFile), pageFile.OutputPath);

    //     // Cleanup
    //     File.Delete(inputFilePath);
    // }

    // [Fact]
    // public void LoadFiles_ReturnsEmptyList_WhenNoFilesExist() {
    //     var pageFileLoader = new PageFileLoader(Shared.BasePath, Shared.OutputPath);

    //     // Ensure the directory is empty
    //     foreach (var file in Directory.GetFiles(Shared.BasePath)) {
    //         File.Delete(file);
    //     }

    //     var pageFiles = pageFileLoader.LoadFiles();

    //     Assert.NotNull(pageFiles);
    //     Assert.Empty(pageFiles);
    // }

    [Theory]
    [InlineData("file", ViewType.Html)] // No extension defaults to HTML
    [InlineData("file.markdown", ViewType.Html)] // Unusual extension defaults to HTML
    [InlineData("file.md", ViewType.Markdown)] // Markdown file
    [InlineData("file.html", ViewType.Html)] // HTML file
    public void GetViewType_HandlesEdgeCases(string fileName, ViewType expectedViewType) {
        Assert.Equal(expectedViewType, PageFileLoader.FileHelper.GetViewType(fileName));
    }
}
