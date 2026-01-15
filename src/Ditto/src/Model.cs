using System.Numerics;
using CsToml;
using CsToml.Error;
using CsToml.Values;
using Danom;

namespace Ditto;

internal static class ModelValuesParser {
    internal static async Task<Result<ModelValues, ResultErrors>> Parse(Stream input) {
        if (input is null) {
            return Result<ModelValues>.Error("Input stream is null.");
        }

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

internal sealed class ModelValues(TomlDocument toml) {
    private readonly TomlDocument _toml = toml;

    internal static ModelValues Empty { get; } = new(new TomlDocument());

    internal IDictionary<string, object> AsDictionary() =>
        _toml.ToDictionary<string, object>();

    internal string? GetString(string name) =>
        _toml.RootNode[name].TryGetString(out var value) ? value : null;

    internal short? GetInt16(string name) =>
        TryGetNumber<short>(name, out var value) ? value : null;

    internal int? GetInt32(string name) =>
        TryGetNumber<int>(name, out var value) ? value : null;

    internal long? GetInt64(string name) =>
        TryGetNumber<long>(name, out var value) ? value : null;

    internal double? GetDouble(string name) =>
        TryGetNumber<double>(name, out var value) ? value : null;

    internal float? GetFloat(string name) =>
        TryGetNumber<float>(name, out var value) ? value : null;

    internal bool? GetBool(string name) =>
        _toml.RootNode[name].TryGetBool(out var value) ? value : null;

    internal DateTime? GetDateTime(string name) =>
        _toml.RootNode[name].TryGetDateTime(out var value) ? value : null;

    internal DateTimeOffset? GetDateTimeOffset(string name) =>
        _toml.RootNode[name].TryGetDateTimeOffset(out var value) ? value : null;

    internal DateOnly? GetDateOnly(string name) =>
        _toml.RootNode[name].TryGetDateOnly(out var value) ? value : null;

    internal TimeOnly? GetTimeOnly(string name) =>
        _toml.RootNode[name].TryGetTimeOnly(out var value) ? value : null;

    internal object? GetObject(string name) =>
        _toml.RootNode[name].TryGetObject(out var value) ? value : null;

    internal string[] GetStringArray(string name) =>
        TryGetArray(name, x => x.GetString(), out var value) ? value : [];

    internal short[] GetInt16Array(string name) =>
        TryGetArray(name, x => x.GetNumber<short>(), out var value) ? value : [];

    internal int[] GetInt32Array(string name) =>
        TryGetArray(name, x => x.GetNumber<int>(), out var value) ? value : [];

    internal long[] GetInt64Array(string name) =>
        TryGetArray(name, x => x.GetNumber<long>(), out var value) ? value : [];

    internal float[] GetFloatArray(string name) =>
        TryGetArray(name, x => x.GetNumber<float>(), out var value) ? value : [];

    internal double[] GetDoubleArray(string name) =>
        TryGetArray(name, x => x.GetNumber<double>(), out var value) ? value : [];

    internal DateOnly[] GetDateOnlyArray(string name) =>
        TryGetArray(name, x => x.GetDateOnly(), out var value) ? value : [];

    internal DateTime[] GetDateTimeArray(string name) =>
        TryGetArray(name, x => x.GetDateTime(), out var value) ? value : [];

    internal DateTimeOffset[] GetDateTimeOffsetArray(string name) =>
        TryGetArray(name, x => x.GetDateTimeOffset(), out var value) ? value : [];

    internal TimeOnly[] GetTimeOnlyArray(string name) =>
        TryGetArray(name, x => x.GetTimeOnly(), out var value) ? value : [];

    private bool TryGetArray<T>(string name, Func<TomlValue, T> converter, out T[] value) {
        if (_toml.RootNode[name].TryGetArray(out var x)) {
            value = [.. x.Select(converter)];
            return true;
        }
        value = [];
        return false;
    }

    private bool TryGetNumber<T>(string name, out T value) where T : struct, INumberBase<T> =>
        _toml.RootNode[name].TryGetNumber(out value);

}
