using System.Text.RegularExpressions;

namespace Ditto.Tests;

public static class Shared {
    public static string BasePath => Path.Join(AppContext.BaseDirectory, "assets", "pages");
    public static string OutputPath => Path.Join(AppContext.BaseDirectory, "assets", "output");

    public static SiteConfig SiteConfig => new(
        BaseUrl: "https://example.com",
        Title: "Example Site",
        Description: "This is an example site.",
        TitleSeparator: Website.DefaultTitleSeparator,
        Data: new Dictionary<string, object>());

    public static IViewLoader TestViewLoader => new ViewLoader(BasePath);

    public static IViewRenderer TestViewRenderer {
        get {
            var partials = TestViewLoader.LoadViews(Website.PartialsDirectory).Result;
            return new ViewRenderer(partials);
        }
    }

    public static IViewProcessor TestMarkdownProcessor =>
        new MarkdownProcessor();

    public static IViewEngine TestViewEngine {
        get {
            var layouts = TestViewLoader.LoadViews(Website.LayoutsDirectory).Result;
            return new ViewEngine(TestViewRenderer, [TestMarkdownProcessor], layouts);
        }
    }

    public static void AssertHtmlEqual(string expected, string result) {
        Assert.Equal(
            StripWhitespaceBetweenTags(NormalizeLineEndings(expected)),
            StripWhitespaceBetweenTags(NormalizeLineEndings(result)));

        static string StripWhitespaceBetweenTags(string input) {
            return Regex.Replace(input, @"\s*(<[^>]+>)\s*", "$1");
        }

        static string NormalizeLineEndings(string input) {
            return input.Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}
