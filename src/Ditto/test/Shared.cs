namespace Ditto.Tests;

public static class Shared {
    public static string BasePath => Path.Join(AppContext.BaseDirectory, "assets", "pages");
    public static string OutputPath => Path.Join(AppContext.BaseDirectory, "assets", "output");
    public static SiteConfig SiteConfig => new(
        BaseUrl: "https://example.com",
        Title: "Example Site",
        Description: "This is an example site.",
        TitleSeparator: SiteConfig.DefaultTitleSeparator);
}
