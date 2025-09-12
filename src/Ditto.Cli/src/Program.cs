using System.CommandLine;

namespace Ditto.Cli;

public static class Program {
    public static void Main(string[] args) {
        var inputArgument =
            new Argument<string>(
                name: "input") {
                Description = "The absolute path to website directory containing website.toml"
            };

        var outputOption =
            new Option<string>("output", ["--output", "-o"]) {
                Description = "The output directory for the generated files",
                Required = true
            };

        var hotReloadOption =
            new Option<bool>("hot-reload", ["--hot-reload", "-hr"]) {
                Description = "Enable hot-reload, watches the input directory for changes and regenerates the website automatically",
                DefaultValueFactory = _ => false
            };

        var command = new RootCommand(
            description: "ditto, static webite generator with hot reload") {
            inputArgument,
            outputOption,
            hotReloadOption
        };

        command.SetAction(async parseResult => {
            var basePath = parseResult.GetValue(inputArgument) ?? ".";
            var outputPath = parseResult.GetValue(outputOption);
            var hotReload = parseResult.GetValue(hotReloadOption);

            if (string.IsNullOrEmpty(basePath)) {
                Console.WriteLine("Input directory is required.");
                return;
            }

            if (string.IsNullOrEmpty(outputPath)) {
                Console.WriteLine("Output directory is required.");
                return;
            }

            Console.WriteLine($"Generating website {(hotReload ? "using hot-reload": "")} from '{basePath}' to '{outputPath}'...");
            var generateWebsite = new GenerateWebsiteCommand(basePath, outputPath, hotReload);
            await generateWebsite.Execute();
        });

        command.Parse(args).Invoke();
    }
}
