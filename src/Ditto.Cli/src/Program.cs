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

        var watchOption =
            new Option<bool>("watch", ["--watch"]) {
                Description = "Enable watch, watches the input directory for changes and regenerates the website automatically",
                DefaultValueFactory = _ => false
            };

        var serverOption =
            new Option<bool>("serve", ["--serve"]) {
                Description = "Enable built-in web server",
                DefaultValueFactory = _ => false
            };

        var portOption =
            new Option<int>("port", ["--port", "-p"]) {
                Description = "Set the port for the built-in web server (default: 8080)",
                DefaultValueFactory = _ => 8080
            };

        var command = new RootCommand(
            description: "ditto, static webite generator with hot reload") {
            inputArgument,
            outputOption,
            watchOption,
            serverOption,
            portOption
        };

        command.SetAction(async parseResult => {
            var basePath = parseResult.GetValue(inputArgument) ?? ".";
            var outputPath = parseResult.GetValue(outputOption);
            var watchEnabled = parseResult.GetValue(watchOption);
            var serverEnabled = parseResult.GetValue(serverOption);
            var port = parseResult.GetValue(portOption);

            if (string.IsNullOrEmpty(basePath)) {
                Print.Error("Input directory is required.");
                return;
            }

            if (string.IsNullOrEmpty(outputPath)) {
                Print.Error("Output directory is required.");
                return;
            }

            Print.Info($"Generating website {(watchEnabled ? "using watch" : "")} from '{basePath}' to '{outputPath}'...");
            var generateWebsite = new GenerateWebsiteCommand(basePath, outputPath, watchEnabled, serverEnabled, port);

            try {
                await generateWebsite.Execute();
            }
            catch (Exception ex) {
                Print.Error("An error occurred while generating the website:\n");
                Print.Error(ex.ToString());
                throw;
            }
        });

        command.Parse(args).Invoke();
    }
}
