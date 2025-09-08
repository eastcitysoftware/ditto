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

        var modelValues = await Shared.ModelValuesParser.Parse(input);

        Assert.NotNull(modelValues);
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
    public async Task Parse_ReturnsEmptyModelValues_ForEmptyInput() {
        var input = new MemoryStream(Encoding.UTF8.GetBytes(""));

        var modelValues = await Shared.ModelValuesParser.Parse(input);

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
}
