using System.Text;

namespace Ditto.Tests;

public sealed class ModelValuesParserTests {
    [Fact]
    public async Task Parse_ReturnsModelValues_ForValidInput() {
        var input = new MemoryStream(Encoding.UTF8.GetBytes("""
            str = "value"
            int16 = 12345
            int32 = 123456789
            int64 = 12345678901234
            float = 123.45
            double = 123.456789
            date = 2024-01-01
            datetime = 2024-01-01T12:34:56Z
            datetime_offset = 2024-01-01T12:34:56+02:00
            time = 12:34:56
            flag = true
            unflag = false
            str_array = ["one", "two", "three"]
            int16_array = [1, 2, 3, 4, 5]
            int32_array = [1, 2, 3, 4, 5]
            int64_array = [1, 2, 3, 4, 5]
            float_array = [1.1, 2.2, 3.3]
            double_array = [1.11, 2.22, 3.33]
            date_array = [2024-01-01, 2024-02-02, 2024-03-03]
            datetime_array = [2024-01-01T12:00:00Z, 2024-02-02T13:00:00Z, 2024-03-03T14:00:00Z]
            datetime_offset_array = [2024-01-01T12:00:00+02:00, 2024-02-02T13:00:00+02:00, 2024-03-03T14:00:00+02:00]
            time_array = [12:00:00, 13:00:00, 14:00:00]
            """));

        var modelValuesResult = await ModelValuesParser.Parse(input);
        if (!modelValuesResult.TryGet(out var modelValues)) {
            Assert.Fail("Failed to parse ModelValues");
            return;
        }

        Assert.Equal("value", modelValues.GetString("str"));
        Assert.Equal((short)12345, modelValues.GetInt16("int16"));
        Assert.Equal(123456789, modelValues.GetInt32("int32"));
        Assert.Equal(12345678901234L, modelValues.GetInt64("int64"));
        Assert.Equal(123.456789, modelValues.GetDouble("double"));
        Assert.Equal(123.45f, modelValues.GetFloat("float"));
        Assert.Equal(new DateOnly(2024, 1, 1), modelValues.GetDateOnly("date"));
        Assert.Equal(new DateTime(2024, 1, 1, 12, 34, 56, DateTimeKind.Utc), modelValues.GetDateTime("datetime"));
        Assert.Equal(new DateTimeOffset(2024, 1, 1, 12, 34, 56, TimeSpan.FromHours(2)), modelValues.GetDateTimeOffset("datetime_offset"));
        Assert.Equal(new TimeOnly(12, 34, 56), modelValues.GetTimeOnly("time"));
        Assert.True(modelValues.GetBool("flag"));
        Assert.False(modelValues.GetBool("unflag"));
        Assert.Equal(new[] { "one", "two", "three" }, modelValues.GetStringArray("str_array"));
        Assert.Equal(new short[] { 1, 2, 3, 4, 5 }, modelValues.GetInt16Array("int16_array"));
        Assert.Equal([1, 2, 3, 4, 5], modelValues.GetInt32Array("int32_array"));
        Assert.Equal([1, 2, 3, 4, 5], modelValues.GetInt64Array("int64_array"));
        Assert.Equal([1.1f, 2.2f, 3.3f], modelValues.GetFloatArray("float_array"));
        Assert.Equal([1.11, 2.22, 3.33], modelValues.GetDoubleArray("double_array"));
        Assert.Equal([new DateOnly(2024, 1, 1), new DateOnly(2024, 2, 2), new DateOnly(2024, 3, 3)], modelValues.GetDateOnlyArray("date_array"));
        Assert.Equal([new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 2, 13, 0, 0, DateTimeKind.Utc), new DateTime(2024, 3, 3, 14, 0, 0, DateTimeKind.Utc)], modelValues.GetDateTimeArray("datetime_array"));
        Assert.Equal([new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)), new DateTimeOffset(2024, 2, 2, 13, 0, 0, TimeSpan.FromHours(2)), new DateTimeOffset(2024, 3, 3, 14, 0, 0, TimeSpan.FromHours(2))], modelValues.GetDateTimeOffsetArray("datetime_offset_array"));
        Assert.Equal([new TimeOnly(12, 0, 0), new TimeOnly(13, 0, 0), new TimeOnly(14, 0, 0)], modelValues.GetTimeOnlyArray("time_array"));
    }

    [Fact]
    public void Parse_ReturnsEmptyModelValues_ForEmptyInput() {
        var modelValues = ModelValues.Empty;

        Assert.NotNull(modelValues);
        Assert.Null(modelValues.GetString("str"));
        Assert.Null(modelValues.GetInt16("int16"));
        Assert.Null(modelValues.GetInt32("int32"));
        Assert.Null(modelValues.GetInt64("int64"));
        Assert.Null(modelValues.GetDouble("double"));
        Assert.Null(modelValues.GetFloat("float"));
        Assert.Null(modelValues.GetDateOnly("date"));
        Assert.Null(modelValues.GetDateTime("datetime"));
        Assert.Null(modelValues.GetDateTimeOffset("datetime_offset"));
        Assert.Null(modelValues.GetTimeOnly("time"));
        Assert.Null(modelValues.GetBool("flag"));
        Assert.Null(modelValues.GetBool("unflag"));
        Assert.Empty(modelValues.GetStringArray("str_array"));
        Assert.Empty(modelValues.GetInt16Array("int16_array"));
        Assert.Empty(modelValues.GetInt32Array("int32_array"));
        Assert.Empty(modelValues.GetInt64Array("int64_array"));
        Assert.Empty(modelValues.GetFloatArray("float_array"));
        Assert.Empty(modelValues.GetDoubleArray("double_array"));
        Assert.Empty(modelValues.GetDateOnlyArray("date_array"));
        Assert.Empty(modelValues.GetDateTimeArray("datetime_array"));
        Assert.Empty(modelValues.GetDateTimeOffsetArray("datetime_offset_array"));
        Assert.Empty(modelValues.GetTimeOnlyArray("time_array"));
    }

    [Fact]
    public async Task Parse_ReturnsError_ForInvalidModelInput() {
        var input = new MemoryStream(Encoding.UTF8.GetBytes("invalid_Model = ["));
        var modelValuesResult = await ModelValuesParser.Parse(input);
        Assert.False(modelValuesResult.IsOk);
    }

    [Fact]
    public async Task Parse_ReturnsError_ForNullInputStream() {
        Stream? input = null;
        var modelValuesResult = await ModelValuesParser.Parse(input!);
        Assert.False(modelValuesResult.IsOk);
    }

    [Fact]
    public async Task Parse_ReturnsOk_ForEmptyModelDocument() {
        var input = new MemoryStream(Encoding.UTF8.GetBytes(""));
        var modelValuesResult = await ModelValuesParser.Parse(input);
        Assert.True(modelValuesResult.IsOk);
    }

    [Fact]
    public void GetMethods_ReturnNullOrEmpty_ForNonExistentKeys() {
        var modelValues = ModelValues.Empty;

        Assert.Null(modelValues.GetString("non_existent_key"));
        Assert.Null(modelValues.GetInt16("non_existent_key"));
        Assert.Null(modelValues.GetInt32("non_existent_key"));
        Assert.Null(modelValues.GetInt64("non_existent_key"));
        Assert.Null(modelValues.GetDouble("non_existent_key"));
        Assert.Null(modelValues.GetFloat("non_existent_key"));
        Assert.Null(modelValues.GetBool("non_existent_key"));
        Assert.Null(modelValues.GetDateOnly("non_existent_key"));
        Assert.Null(modelValues.GetDateTime("non_existent_key"));
        Assert.Null(modelValues.GetDateTimeOffset("non_existent_key"));
        Assert.Null(modelValues.GetTimeOnly("non_existent_key"));
        Assert.Empty(modelValues.GetStringArray("non_existent_key"));
        Assert.Empty(modelValues.GetInt16Array("non_existent_key"));
        Assert.Empty(modelValues.GetInt32Array("non_existent_key"));
        Assert.Empty(modelValues.GetInt64Array("non_existent_key"));
        Assert.Empty(modelValues.GetFloatArray("non_existent_key"));
        Assert.Empty(modelValues.GetDoubleArray("non_existent_key"));
        Assert.Empty(modelValues.GetDateOnlyArray("non_existent_key"));
        Assert.Empty(modelValues.GetDateTimeArray("non_existent_key"));
        Assert.Empty(modelValues.GetDateTimeOffsetArray("non_existent_key"));
        Assert.Empty(modelValues.GetTimeOnlyArray("non_existent_key"));
    }

    [Fact]
    public async Task GetMethods_ReturnNull_ForTypeMismatch() {
        var input = new MemoryStream(Encoding.UTF8.GetBytes("""
            key = "string_value"
            """));

        var modelValuesResult = await ModelValuesParser.Parse(input);

        if (!modelValuesResult.TryGet(out var modelValues)) {
            Assert.Fail("Failed to parse ModelValues");
            return;
        }

        Assert.Null(modelValues.GetInt32("key"));
        Assert.Null(modelValues.GetBool("key"));
        Assert.Null(modelValues.GetDateTime("key"));
    }

    [Fact]
    public async Task GetMethods_HandleSpecialCharactersInKeys() {
        var input = new MemoryStream(Encoding.UTF8.GetBytes("""
            "key with spaces" = "value"
            """));

        var modelValuesResult = await ModelValuesParser.Parse(input);

        if (!modelValuesResult.TryGet(out var modelValues)) {
            Assert.Fail("Failed to parse ModelValues");
            return;
        }

        Assert.Equal("value", modelValues.GetString("key with spaces"));
    }

    [Fact]
    public async Task GetMethods_HandleUnicodeCharactersInKeys() {
        var input = new MemoryStream(Encoding.UTF8.GetBytes("""
            "ключ-с-юникодом" = "значение"
            """));

        var modelValuesResult = await ModelValuesParser.Parse(input);

        if (!modelValuesResult.TryGet(out var modelValues)) {
            Assert.Fail("Failed to parse ModelValues");
            return;
        }

        Assert.Equal("значение", modelValues.GetString("ключ-с-юникодом"));
    }

    [Fact]
    public async Task Parse_Ignores_CommentsInModel() {
        var input = new MemoryStream(Encoding.UTF8.GetBytes("""
            # This is a comment
            key = "value" # Inline comment
            """));

        var modelValuesResult = await ModelValuesParser.Parse(input);

        if (!modelValuesResult.TryGet(out var modelValues)) {
            Assert.Fail("Failed to parse ModelValues");
            return;
        }

        Assert.Equal("value", modelValues.GetString("key"));
    }

    [Fact]
    public async Task Parse_Handles_WhitespaceInModel() {
        var input = new MemoryStream(Encoding.UTF8.GetBytes("""

            key    =    "value"

            """));

        var modelValuesResult = await ModelValuesParser.Parse(input);

        if (!modelValuesResult.TryGet(out var modelValues)) {
            Assert.Fail("Failed to parse ModelValues");
            return;
        }

        Assert.Equal("value", modelValues.GetString("key"));
    }

    [Fact]
    public async Task Parse_Handles_LargeModelInput() {
        var sb = new StringBuilder();
        for (int i = 0; i < 1000; i++) {
            sb.AppendLine($"key{i} = \"value{i}\"");
        }

        var input = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        var modelValuesResult = await ModelValuesParser.Parse(input);

        if (!modelValuesResult.TryGet(out var modelValues)) {
            Assert.Fail("Failed to parse ModelValues");
            return;
        }

        for (int i = 0; i < 1000; i++) {
            Assert.Equal($"value{i}", modelValues.GetString($"key{i}"));
        }
    }

    [Fact]
    public async Task Parse_Handles_SpecialCharactersInValues() {
        var input = new MemoryStream(Encoding.UTF8.GetBytes("""
            key_newline = "line1\nline2"
            key_tab = "column1\tcolumn2"
            key_quote = "He said, \"Hello!\""
            """));

        var modelValuesResult = await ModelValuesParser.Parse(input);

        if (!modelValuesResult.TryGet(out var modelValues)) {
            Assert.Fail("Failed to parse ModelValues");
            return;
        }

        Assert.Equal("line1\nline2", modelValues.GetString("key_newline"));
        Assert.Equal("column1\tcolumn2", modelValues.GetString("key_tab"));
        Assert.Equal("He said, \"Hello!\"", modelValues.GetString("key_quote"));
    }
}
