namespace Ditto.Cli;

internal static class Print {
    public static void Error(string message) {
        WriteColoredMessage("ERROR", ConsoleColor.Red);
        Console.Write(" ");
        Console.Write(message);
        Console.WriteLine();
    }

    public static void Warning(string message) {
        WriteColoredMessage("WARNING", ConsoleColor.Yellow);
        Console.Write(" ");
        Console.Write(message);
        Console.WriteLine();
    }

    private static void WriteColoredMessage(string message, ConsoleColor color) {
        var originalColor = Console.BackgroundColor;
        Console.BackgroundColor = color;
        Console.Write(message);
        Console.BackgroundColor = originalColor;
    }

    public static void Info(string message) {
        Console.WriteLine(message);
    }
}