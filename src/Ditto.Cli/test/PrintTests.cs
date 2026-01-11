// namespace Ditto.Cli.Tests;

// public class PrintTests {
//     [Fact]
//     public void Error_PrintsErrorMessageInRed() {
//         using var output = new StringWriter();
//         Console.SetOut(output);

//         Print.Error("This is an error message");

//         var result = output.ToString();
//         Assert.Contains("ERROR", result);
//         Assert.Contains("This is an error message", result);
//     }

//     [Fact]
//     public void Warning_PrintsWarningMessageInYellow() {
//         using var output = new StringWriter();
//         Console.SetOut(output);

//         Print.Warning("This is a warning message");

//         var result = output.ToString();
//         Assert.Contains("WARNING", result);
//         Assert.Contains("This is a warning message", result);
//     }

//     [Fact]
//     public void Info_PrintsInfoMessage() {
//         using var output = new StringWriter();
//         Console.SetOut(output);

//         Print.Info("This is an info message");

//         var result = output.ToString();
//         Assert.Contains("This is an info message", result);
//     }

//     [Fact]
//     public void WriteColoredMessage_RestoresOriginalConsoleColor() {
//         using var output = new StringWriter();
//         Console.SetOut(output);

//         var originalColor = Console.BackgroundColor;

//         Print.Error("Test message");

//         Assert.Equal(originalColor, Console.BackgroundColor);
//     }
// }
