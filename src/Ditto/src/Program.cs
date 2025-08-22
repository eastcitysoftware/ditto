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

    var siteConfigLoader = new SiteConfigLoader(basePath);
    var siteConfig = await siteConfigLoader.Load();

    if (siteConfig.TryGetError(out var errors)) {
        Console.WriteLine("Failed to load site configuration:");
        foreach (var error in errors) {
            Console.WriteLine($"  * Error: {error}");
        }
        return;
    }

    var layoutLoader = new LayoutLoader(basePath);
    var layouts = await layoutLoader.LoadLayouts();

    if (layouts.Names.Count == 0) {
        Console.WriteLine($"No layouts found in the source directory. Must include at least the '{Website.DefaultLayoutName}' layout.");
        return;
    }

    var partialLoader = new PartialLoader(basePath);
    var partials = await partialLoader.LoadPartials();

    var sw = System.Diagnostics.Stopwatch.StartNew();
    var result = await siteConfig.BindAsync(config => {
        var generator = new WebsiteGenerator(
            pageFileLoader: new PageFileLoader(basePath, outputPath),
            pageParser: new PageParser(config),
            pageWriter: new PageWriter(layouts, partials));

        return generator.Generate(basePath, outputPath);
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
