namespace Ditto.Tests;

// public sealed class PageFileTests {
//     [Theory]
//     [InlineData("/input/index.html", "/output/index.html", "/", "index")]
//     [InlineData("/input/about/index.html", "/output/about/index.html", "/about/", "about")]
//     [InlineData("/input/blog/post1.html", "/output/blog/post1/index.html", "/blog/post1/", "blog/post1")]
//     [InlineData("/input/contact.html", "/output/contact/index.html", "/contact/", "contact")]
//     public void HtmlFile_Index_WorksAsExpected(string inputPath, string outputPath, string path, string pageName) {
//         var viewType = ViewType.Html;

//         var pageFile = new PageFile(
//             InputPath: inputPath,
//             OutputPath: outputPath,
//             Path: path,
//             PageName: pageName,
//             ViewType: viewType);

//         Assert.Equal(inputPath, pageFile.InputPath);
//         Assert.Equal(outputPath, pageFile.OutputPath);
//         Assert.Equal(path, pageFile.Path);
//         Assert.Equal(pageName, pageFile.PageName);
//         Assert.Equal(viewType, pageFile.ViewType);
//     }

//     [Theory]
//     [InlineData("/input/index.md", "/output/index.html", "/", "index")]
//     [InlineData("/input/about/index.md", "/output/about/index.html", "/about/", "about")]
//     [InlineData("/input/blog/post1.md", "/output/blog/post1/index.html", "/blog/post1/", "blog/post1")]
//     [InlineData("/input/contact.md", "/output/contact/index.html", "/contact/", "contact")]
//     public void MarkdownFile_Index_WorksAsExpected(string inputPath, string outputPath, string path, string pageName) {
//         var viewType = ViewType.Markdown;

//         var pageFile = new PageFile(
//             InputPath: inputPath,
//             OutputPath: outputPath,
//             Path: path,
//             PageName: pageName,
//             ViewType: viewType);

//         Assert.Equal(inputPath, pageFile.InputPath);
//         Assert.Equal(outputPath, pageFile.OutputPath);
//         Assert.Equal(path, pageFile.Path);
//         Assert.Equal(pageName, pageFile.PageName);
//         Assert.Equal(viewType, pageFile.ViewType);
//     }
// }

public sealed class PageFileLoaderTests {
    [Fact]
    public void LoadFiles_ReturnsPageFiles_WhenValidInput() {
        var pageFileLoader = new PageFileLoader(Shared.BasePath, Shared.OutputPath);
        var pageFiles = pageFileLoader.LoadFiles();

        Assert.NotNull(pageFiles);
        Assert.NotEmpty(pageFiles);
        Assert.Equal(4, pageFiles.Count);
        Assert.All(pageFiles, pageFile => {
            Assert.NotNull(pageFile.InputPath);
            Assert.NotNull(pageFile.OutputPath);
            Assert.NotNull(pageFile.Path);
            Assert.NotNull(pageFile.PageName);

            Assert.True(File.Exists(pageFile.InputPath));
            Assert.Equal("index.html", Path.GetFileName(pageFile.OutputPath)); // every page file should be named index.html
            // Assert.Contains(pageFile.PageName, pageFile.OutputPath);

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
        Assert.Equal(expectedPath, PageFileHelper.GetPath(basePath, filePath));
    }

    [Theory]
    [InlineData(@"C:\users\ditto\website\src", @"C:\users\ditto\website\src\index.html", "index")]
    [InlineData(@"C:\users\ditto\website\src", @"C:\users\ditto\website\src\about.html", "about")]
    [InlineData(@"C:\users\ditto\website\src", @"C:\users\ditto\website\src\blog\post1.html", "blog/post1")]
    [InlineData(@"/users/ditto/website/src", @"/users/ditto/website/src/index.html", "index")]
    [InlineData(@"/users/ditto/website/src", @"/users/ditto/website/src/about.html", "about")]
    [InlineData(@"/users/ditto/website/src", @"/users/ditto/website/src/blog/post1.html", "blog/post1")]
    public void GetPageName_ReturnsExpected(string basePath, string filePath, string expected) {
        Assert.Equal(expected, PageFileHelper.GetPageName(basePath, filePath));
    }

    [Theory]
    [InlineData("about.md", ViewType.Markdown)]
    [InlineData("contact.html", ViewType.Html)]
    public void GetViewType_ReturnsExpectedViewType(string fileName, ViewType expectedViewType) {
        var viewType = PageFileHelper.GetViewType(fileName);
        Assert.Equal(expectedViewType, viewType);
    }
}
