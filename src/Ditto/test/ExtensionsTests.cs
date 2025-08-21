using Tomlyn.Model;

namespace Ditto.Tests;

public sealed class TomlTableExtensionsTests {
    [Fact]
    public void GetString_ReturnsString_WhenKeyExistsAndValueIsString() {
        var table = new TomlTable {
            ["key"] = "value"
        };
        var result = table.GetString("key");
        Assert.Equal("value", result);
    }

    [Fact]
    public void GetString_ReturnsNull_WhenKeyDoesNotExist() {
        var table = new TomlTable();
        var result = table.GetString("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public void GetValue_ReturnsTypedValue_WhenKeyExistsAndValueIsOfType() {
        var table = new TomlTable {
            ["key"] = 42
        };
        var result = table.GetValue<int>("key");
        Assert.Equal(42, result);
    }

    [Fact]
    public void GetValue_ReturnsDefault_WhenKeyDoesNotExist() {
        var table = new TomlTable();
        var result = table.GetValue<int>("nonexistent");
        Assert.Equal(default, result);
    }
}
