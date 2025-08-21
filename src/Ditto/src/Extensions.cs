using Tomlyn.Model;

namespace Ditto;

public static class PathUtil {
    private static HashSet<string> _systemPaths = [
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "/bin", "/boot", "/dev", "/etc", "/lib", "/lib64", "/proc", "/root", "/sbin", "/sys", "/usr", "/var" // Common Linux/macOS system directories
    ];

    public static bool IsSystemPath(string pathToTest) =>
        _systemPaths.Any(path => string.Equals(path, pathToTest, StringComparison.OrdinalIgnoreCase));
}

public static class TomlTableExtensions {
    public static string? GetString(this TomlTable table, string key) {
        if (table.TryGetValue(key, out var value) && value is string str && !string.IsNullOrWhiteSpace(str)) {
            return str;
        }
        return null;
    }

    public static T? GetValue<T>(this TomlTable table, string key) {
        if (table.TryGetValue(key, out var value) && value is T typedValue) {
            return typedValue;
        }
        return default;
    }
}
