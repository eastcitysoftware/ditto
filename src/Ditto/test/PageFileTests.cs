namespace Ditto.Tests;

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
            Assert.True(File.Exists(pageFile.InputPath));
            Assert.Equal("index.html", Path.GetFileName(pageFile.OutputPath)); // every page file should be named index.html

            if (Path.GetRelativePath(Shared.BasePath, pageFile.InputPath) == "index.html") {
                // the index page should map to the output path directly
                Assert.Equal("index.html", Path.GetRelativePath(Shared.OutputPath, pageFile.OutputPath));
            }
        });
    }
}
