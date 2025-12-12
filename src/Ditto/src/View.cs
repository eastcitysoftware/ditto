using Markdig;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Ditto;

public interface IViewEngine {
    ValueTask<string> Render(Page page, SiteConfig siteConfig, IEnumerable<PageCollection> collections);
}

public interface IViewRenderer {
    ValueTask<string> Render(string view, object viewModel);
}

public interface IViewLoader {
    Task<ViewCollection> LoadViews(string subdirectory);
}

public interface IViewProcessor {
    ValueTask<View> Process(View view);
}

public sealed class ViewEngine(
    IViewRenderer viewRenderer,
    IEnumerable<IViewProcessor> viewProcessors,
    ViewCollection layouts) : IViewEngine {
    private ScriptObject? _siteModel;
    private ScriptObject? _collectionsModel;
    public async ValueTask<string> Render(Page page, SiteConfig siteConfig, IEnumerable<PageCollection> collections) {
        _siteModel ??= CreateSiteModel(siteConfig);
        _collectionsModel ??= CreateCollectionsModel(collections);

        var viewModel = CreatePageModel(page);
        viewModel.Add("site", _siteModel);
        viewModel.Add("collections", _collectionsModel);

        var renderedView = page.View with {
            Content = await viewRenderer.Render(
            view: page.View.Content,
            viewModel: viewModel)
        };

        foreach (var processor in viewProcessors) {
            renderedView = await processor.Process(renderedView);
        }

        if (layouts.Get(page.View.LayoutName) is View layout) {
            viewModel.Add("content", renderedView.Content);
            return await viewRenderer.Render(
                view: layout.Content,
                viewModel: viewModel);
        }

        return renderedView.Content;
    }

    private static ScriptObject CreatePageModel(Page page) {
        var pageModelData = new ScriptObject();
        pageModelData.Import(page.Data);

        return new ScriptObject() {
            ["path"] = page.Path,
            ["slug"] = page.Slug,
            ["url"] = page.Url,
            ["title"] = page.Title,
            ["page_title"] = page.PageTitle,
            ["description"] = page.Description,
            ["tags"] = page.Tags,
            ["published"] = page.Published,
            ["data"] = pageModelData
        };
    }

    private static ScriptObject CreateSiteModel(SiteConfig siteConfig) {
        var siteModelData = new ScriptObject();
        siteModelData.Import(siteConfig.Data);

        return new ScriptObject() {
            ["base_url"] = siteConfig.BaseUrl,
            ["title"] = siteConfig.Title,
            ["description"] = siteConfig.Description,
            ["title_separator"] = siteConfig.TitleSeparator,
            ["data"] = siteModelData
        };
    }

    private static ScriptObject CreateCollectionsModel(IEnumerable<PageCollection> collections) {
        var collectionsModel = new ScriptObject();
        foreach(var collection in collections) {
            var pageModels = collection.Pages.Select(CreatePageModel);
            collectionsModel.Add(collection.Name, pageModels);
        }
        return collectionsModel;
    }
}

public sealed class ViewRenderer(ViewCollection? partials = default) : IViewRenderer {
    private readonly ITemplateLoader _viewLoader = new PartialsLoader(partials);

    private static ScriptObject DateOnlyFunctions {
        get {
            var container = new ScriptObject();
            container.Import("to_date_time", (DateOnly date) => date.ToDateTime(new(0, 0, 0)));
            return container;
        }
    }

    public ValueTask<string> Render(string view, object viewModel) {
        if (string.IsNullOrWhiteSpace(view)) {
            return ValueTask.FromResult(string.Empty);
        }

        var parsedTemplate = Template.Parse(view);
        if (parsedTemplate.HasErrors) {
            return ValueTask.FromResult(string.Empty);
        }

        var context = new TemplateContext {
            TemplateLoader = _viewLoader
        };

        var contextModel = new ScriptObject {
            { "date_only", DateOnlyFunctions }
        };

        contextModel.Import(viewModel);
        context.PushGlobal(contextModel);

        return parsedTemplate.RenderAsync(context);
    }

    public sealed class PartialsLoader(ViewCollection? partials) : ITemplateLoader {
        public string GetPath(TemplateContext context, SourceSpan callerSpan, string viewName) =>
            partials?.Names.Contains(viewName) ?? false ? viewName : string.Empty;

        public string Load(TemplateContext context, SourceSpan callerSpan, string viewPath) =>
            partials?.Get(viewPath)?.Content ?? string.Empty;

        public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string viewPath) =>
            ValueTask.FromResult(Load(context, callerSpan, viewPath));
    }
}

public sealed class ViewLoader(string basePath) : IViewLoader {
    private readonly EnumerationOptions _options = new EnumerationOptions {
        RecurseSubdirectories = true,
        MaxRecursionDepth = 5,
        IgnoreInaccessible = true,
        MatchCasing = MatchCasing.CaseInsensitive
    };

    public async Task<ViewCollection> LoadViews(string subdirectory) {
        var viewPath = Path.Join(basePath, subdirectory);

        if (Directory.Exists(viewPath)) {
            var viewFiles = Directory.GetFiles(
                path: viewPath,
                searchPattern: string.Concat("*", Website.TemplateExtension),
                enumerationOptions: _options);

            var viewTasks = viewFiles.Select(async filePath => {
                // if this is a partial in a subdirectory, preserve the subdirectory in the name
                // e.g. _partials/title.html -> title
                // e.g. _partials/posts/title.html -> posts/title
                // e.g. _parials/posts/abc/def/ghi.html -> posts/abc/def/ghi

                var viewName = Path.GetFileNameWithoutExtension(filePath);
                var parentDirectory = Path.GetDirectoryName(filePath);

                if (Path.GetFileName(parentDirectory) != subdirectory) {
                    var relativePath = Path.GetRelativePath(viewPath, filePath); // e.g. posts/title.html or posts/abc/def/ghi.html
                    viewName = string.Join("/",
                        Path.GetDirectoryName(relativePath)?
                            .Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/'),
                        Path.GetFileNameWithoutExtension(filePath));
                }

                var content = await File.ReadAllTextAsync(filePath);
                return new View(viewName, content, ViewType.Html);
            });

            var views = await Task.WhenAll(viewTasks);

            return new(views.ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase));
        }

        return new([]);
    }
}

public sealed class MarkdownProcessor : IViewProcessor {
    public ValueTask<View> Process(View view) =>
        view.Type != ViewType.Markdown
            ? ValueTask.FromResult(view)
            : ValueTask.FromResult(view with { Content = Markdown.ToHtml(view.Content) });
}

public sealed class ViewCollection() {
    private readonly Dictionary<string, View> _views = [];

    public ViewCollection(Dictionary<string, View> views) : this() =>
        _views = views;

    public IReadOnlyList<string> Names => [.. _views.Keys.Select(x => x)];

    public View? Get(string name) =>
        _views.TryGetValue(name, out var content) ? content : null;
}

public enum ViewType {
    Html,
    Markdown
}

public sealed record View(
    string Name,
    string Content,
    ViewType Type,
    string LayoutName = Website.DefaultLayoutName);
