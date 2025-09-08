using System.CommandLine;
using Danom;
using Ditto;

var command =
    new RootCommand(
        description: "ditto, static webite generator with hot reload");

var inputArgument =
    new Argument<string>(
        name: "input") {
        Description = "The absolute path to website directory containing website.toml"
    };

var outputOption =
    new System.CommandLine.Option<string>("output", ["--output", "-o"]) {
        Description = "The output directory for the generated files",
        Required = true
    };

command.Arguments.Add(inputArgument);
command.Options.Add(outputOption);
command.SetAction(ExecuteCommand);
command.Parse(args).Invoke();

async Task ExecuteCommand(ParseResult parseResult) {
    var basePath = parseResult.GetValue(inputArgument) ?? ".";
    var outputPath = parseResult.GetValue(outputOption);

    if (string.IsNullOrEmpty(basePath)) {
        Console.WriteLine("Input directory is required.");
        return;
    }

    if (string.IsNullOrEmpty(outputPath)) {
        Console.WriteLine("Output directory is required.");
        return;
    }

    Console.WriteLine($"Generating website from '{basePath}' to '{outputPath}'...");

    var modelValuesParser = new ModelValuesParser();

    var siteConfigLoader = new SiteConfigLoader(basePath, new SiteConfigParser(modelValuesParser));
    var siteConfigResult = await siteConfigLoader.Load();

    if (siteConfigResult.TryGetError(out var e) && e is ResultErrors errors) {
        Console.WriteLine("Failed to load site configuration:");
        foreach (var error in errors) {
            Console.WriteLine($"  * Error: {error}");
        }
        return;
    }

    var viewLoader = new ViewLoader(basePath);
    var layouts = await viewLoader.LoadViews(Website.LayoutsDirectory);

    if (layouts.Names.Count == 0) {
        Console.WriteLine($"No layouts found in the source directory. Must include at least the '{Website.DefaultLayoutName}' layout.");
        return;
    }

    var partials = await viewLoader.LoadViews(Website.PartialsDirectory);
    var templateRender = new ViewRenderer(partials);
    var documentProcessor = new DocumentProcessor();
    var sw = System.Diagnostics.Stopwatch.StartNew();

    var result = await siteConfigResult.BindAsync(siteConfig => {
        var generator = new WebsiteGenerator(
            pageFileLoader: new PageFileLoader(basePath, outputPath),
            pageParser: new PageParser(modelValuesParser, siteConfig),
            viewEngine: new ViewEngine(templateRender, documentProcessor, layouts));

        return generator.Generate(basePath, outputPath, siteConfig);
    });

    sw.Stop();
    result.Match(
        ok: _ => Console.WriteLine($"Website generated successfully, took {sw}."),
        error: errors => {
            Console.WriteLine("Errors occurred during website generation:");
            foreach (var error in errors) {
                Console.WriteLine($"  * Error: {error}");
            }
        });
}
