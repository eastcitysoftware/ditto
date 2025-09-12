using Markdig;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Ditto;

public interface IViewEngine {
    ValueTask<string> Render(Page page, IDictionary<string, object>? supplementalData);
}

public interface IViewRenderer {
    ValueTask<string> Render(string view, object viewModel, IDictionary<string, object>? supplementalData);
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
    public async ValueTask<string> Render(Page page, IDictionary<string, object>? supplementalData) {
        var viewModel = new PageViewModel(
            Path: page.Path,
            Url: page.Url,
            Title: page.Title,
            Description: page.Description);

        if (supplementalData is not null
            && !supplementalData.TryAdd("data", page.Data)) {
            supplementalData["data"] = page.Data;
        }

        var renderedView =  page.View with {
            Content = await viewRenderer.Render(
            view: page.View.Content,
            viewModel: viewModel,
            supplementalData: supplementalData)
        };

        foreach (var processor in viewProcessors) {
            renderedView = await processor.Process(renderedView);
        }

        if (layouts.Get(page.View.LayoutName) is View layout) {
            return await viewRenderer.Render(
                view: layout.Content,
                viewModel: new LayoutViewModel(
                    Title: viewModel.Title,
                    Description: viewModel.Description,
                    Content: renderedView.Content,
                    Head: default),
                supplementalData: supplementalData);
        }

        return renderedView.Content;
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

    public ValueTask<string> Render(string view, object viewModel, IDictionary<string, object>? supplementalData) {
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

        if (supplementalData is not null) {
            foreach (var x in supplementalData) {
                contextModel.Add(x.Key, x.Value);
            }
        }

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
    public async Task<ViewCollection> LoadViews(string subdirectory) {
        var viewPath = Path.Join(basePath, subdirectory);

        if (Directory.Exists(viewPath)) {
            var viewFiles = Directory.GetFiles(viewPath, string.Concat("*", Website.TemplateExtension), SearchOption.TopDirectoryOnly);

            var viewTasks = viewFiles.Select(async filePath => {
                var viewName = Path.GetFileNameWithoutExtension(filePath);
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

internal sealed record PageViewModel(
    string Path,
    string Url,
    string Title,
    string Description);

internal sealed record LayoutViewModel(
    string Title,
    string Description,
    string Content,
    string? Head = default);
