using System.Numerics;
using CsToml;
using CsToml.Error;
using Danom;

namespace Ditto;

public static class ModelValuesParser {
    public static async Task<Result<ModelValues, ResultErrors>> Parse(Stream input) {
        try {
            var toml = await CsTomlSerializer.DeserializeAsync<TomlDocument>(input);

            if (toml is null) {
                return Result<ModelValues>.Error("TOML document is empty or invalid.");
            }

            return Result<ModelValues>.Ok(new(toml));
        }
        catch (CsTomlSerializeException) {
            return Result<ModelValues>.Error("Detected invalid TOML format.");
        }
    }
}

public sealed class ModelValues {
    private readonly TomlDocument _toml;

    internal ModelValues(TomlDocument toml) {
        _toml = toml;
    }

    public IDictionary<string, object> AsDictionary() =>
        _toml.ToDictionary<string, object>();

    public string? GetString(string name) =>
        _toml.RootNode[name].TryGetString(out var x) ? x : null;

    public short? GetInt16(string name) =>
        GetNumber<short>(name);

    public int? GetInt32(string name) =>
        GetNumber<int>(name);

    public long? GetInt64(string name) =>
        _toml.RootNode[name].TryGetInt64(out var x) ? x : null;

    public double? GetDouble(string name) =>
        _toml.RootNode[name].TryGetDouble(out var x) ? x : null;

    public float? GetFloat(string name) =>
        GetNumber<float>(name);

    public bool? GetBool(string name) =>
        _toml.RootNode[name].TryGetBool(out var x) ? x : null;

    public DateTime? GetDateTime(string name) =>
        _toml.RootNode[name].TryGetDateTime(out var x) ? x : null;

    public DateTimeOffset? GetDateTimeOffset(string name) =>
        _toml.RootNode[name].TryGetDateTimeOffset(out var x) ? x : null;

    public DateOnly? GetDateOnly(string name) =>
        _toml.RootNode[name].TryGetDateOnly(out var x) ? x : null;

    public TimeOnly? GetTimeOnly(string name) =>
        _toml.RootNode[name].TryGetTimeOnly(out var x) ? x : null;

    public object? GetObject(string name) =>
        _toml.RootNode[name].TryGetObject(out var x) ? x : null;

    private T? GetNumber<T>(string name) where T : struct, INumberBase<T> =>
        _toml.RootNode[name].TryGetNumber<T>(out var x) ? x : null;

    public string[] GetStringArray(string name) =>
        _toml.RootNode[name].TryGetArray(out var x) ? [.. x.Select(x => x.GetString())] : [];

    public short[] GetInt16Array(string name) =>
        _toml.RootNode[name].TryGetArray(out var x) ? [.. x.Select(x => x.GetNumber<short>())] : [];

    public int[] GetInt32Array(string name) =>
        _toml.RootNode[name].TryGetArray(out var x) ? [.. x.Select(x => x.GetNumber<int>())] : [];

    public long[] GetInt64Array(string name) =>
        _toml.RootNode[name].TryGetArray(out var x) ? [.. x.Select(x => x.GetNumber<long>())] : [];

    public float[] GetFloatArray(string name) =>
        _toml.RootNode[name].TryGetArray(out var x) ? [.. x.Select(x => x.GetNumber<float>())] : [];

    public double[] GetDoubleArray(string name) =>
        _toml.RootNode[name].TryGetArray(out var x) ? [.. x.Select(x => x.GetNumber<double>())] : [];

    public DateOnly[] GetDateOnlyArray(string name) =>
        _toml.RootNode[name].TryGetArray(out var x) ? [.. x.Select(x => x.GetDateOnly())] : [];

    public DateTime[] GetDateTimeArray(string name) =>
        _toml.RootNode[name].TryGetArray(out var x) ? [.. x.Select(x => x.GetDateTime())] : [];

    public DateTimeOffset[] GetDateTimeOffsetArray(string name) =>
        _toml.RootNode[name].TryGetArray(out var x) ? [.. x.Select(x => x.GetDateTimeOffset())] : [];

    public TimeOnly[] GetTimeOnlyArray(string name) =>
        _toml.RootNode[name].TryGetArray(out var x) ? [.. x.Select(x => x.GetTimeOnly())] : [];
}
