using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Ditto;

public sealed record LayoutModel(
    string Title,
    string Description,
    string Content,
    string? Head = default);

public sealed record PageModel(
    string Title,
    string Description,
    SiteConfig Site,
    IDictionary<string, object> Data);

public interface IPageWriter {
    Task Render(Page page, TextWriter output);
}

public sealed class PageWriter(Layouts layouts, Partials partials) : IPageWriter {
    private readonly ITemplateLoader _templateLoader = new TemplateLoader(partials);

    public async Task Render(Page page, TextWriter output) {
        if (string.IsNullOrWhiteSpace(page.Template)) {
            return;
        }

        var template = Template.Parse(page.Template);
        if (template.HasErrors) {
            return;
        }

        var pageModel = new PageModel(
            Title: page.Title,
            Description: page.Description,
            Site: page.Site,
            Data: page.Metadata);

        var context = new TemplateContext {
            TemplateLoader = _templateLoader
        };

        var model = new ScriptObject();
        model.Import(pageModel);
        context.PushGlobal(model);

        var pageContent = await template.RenderAsync(context);

        var layoutModel = new LayoutModel(
            Title: pageModel.Title,
            Description: pageModel.Description,
            Content: pageContent);

        context.PopGlobal();
        model.Import(layoutModel);
        context.PushGlobal(model);

        var layout = layouts.Get(page.Layout) ?? layouts.Get(Website.DefaultLayoutName);

        if (layout is not null) {
            await output.WriteAsync(await layout.RenderAsync(context));
        }
        else {
            await output.WriteAsync(pageContent);
        }
    }

    public sealed class TemplateLoader(Partials partials) : ITemplateLoader {
        public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) {
            return partials.Exists(templateName) ? templateName : string.Empty;
        }

        public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath) {
            return partials.Get(templatePath) ?? string.Empty;
        }

        public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath) {
            return ValueTask.FromResult(Load(context, callerSpan, templatePath));
        }
    }
}


public sealed class Layouts() {
    private readonly Dictionary<string, Template> _layouts = [];

    internal Layouts(Dictionary<string, Template> layouts) : this() {
        _layouts = layouts;
    }

    public IReadOnlyList<string> Names => [.. _layouts.Keys.Select(x => x)];

    public Template? Get(string name) {
        return _layouts.TryGetValue(name, out var content) ? content : null;
    }
}

public interface ILayoutLoader {
    Task<Layouts> LoadLayouts();
}

public sealed class LayoutLoader(string basePath) : ILayoutLoader {
    public async Task<Layouts> LoadLayouts() {
        var layouts = new Dictionary<string, Template>();
        var layoutPath = Path.Combine(basePath, Website.LayoutsDirectory);

        if (Directory.Exists(layoutPath)) {
            foreach (var filePath in Directory.GetFiles(layoutPath, "*.html", SearchOption.TopDirectoryOnly)) {
                var layoutName = Path.GetFileNameWithoutExtension(filePath);
                var layoutContent = await File.ReadAllTextAsync(filePath);
                var template = Template.Parse(layoutContent);
                layouts.Add(layoutName, template);
            }
        }

        return new Layouts(layouts);
    }
}

public interface IPartialLoader {
    Task<Partials> LoadPartials();
}

public sealed class PartialLoader(string basePath) : IPartialLoader {
    public async Task<Partials> LoadPartials() {
        var partials = new Dictionary<string, string>();
        var partialPath = Path.Combine(basePath, Website.PartialsDirectory);

        if (Directory.Exists(partialPath)) {
            foreach (var filePath in Directory.GetFiles(partialPath, "*.html", SearchOption.TopDirectoryOnly)) {
                var partialName = Path.GetFileNameWithoutExtension(filePath);
                var partialContent = await File.ReadAllTextAsync(filePath);
                partials.Add(partialName, partialContent);
            }
        }

        return new Partials(partials);
    }
}

public sealed class Partials() {
    private readonly Dictionary<string, string> _partials = [];

    internal Partials(Dictionary<string, string> partials) : this() {
        _partials = partials;
    }

    public IReadOnlyList<string> Names => [.. _partials.Keys.Select(x => x)];


    public bool Exists(string name) {
        return _partials.ContainsKey(name);
    }

    public string? Get(string name) {
        return _partials.TryGetValue(name, out var content) ? content : null;
    }
}
