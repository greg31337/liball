using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace yaml;

/// <summary>
/// Provides configurable instance-based YAML processing for dependency injection and reuse.
/// </summary>
/// <remarks>
/// <see cref="Configure"/> copies supplied options so later caller mutations do not affect this processor.
/// </remarks>
public class YamlProcessor : IYamlProcessor
{
    internal YamlOptions Options;

    /// <summary>
    /// Initializes a processor with library-default options.
    /// </summary>
    public YamlProcessor()
    {
        Options = new YamlOptions();
    }

    /// <summary>
    /// Creates a processor with an independent copy of the supplied options.
    /// </summary>
    /// <param name="options">The initial settings; pass null to use library defaults.</param>
    /// <returns>The resulting YAML processor.</returns>
    internal static YamlProcessor Create(YamlOptions? options)
    {
        YamlProcessor processor = new YamlProcessor();
        processor.Options = Yaml.EffectiveOptions(options);
        return processor;
    }

    /// <summary>
    /// Replaces the settings used by later operations with an independent copy.
    /// </summary>
    /// <param name="options">The settings to copy; pass null to restore library defaults.</param>
    public void Configure(YamlOptions? options)
    {
        Options = Yaml.EffectiveOptions(options);
    }

    /// <summary>
    /// Returns an independent copy of the settings used by this processor.
    /// </summary>
    /// <returns>The copied processor settings.</returns>
    public YamlOptions GetOptions()
    {
        return Yaml.EffectiveOptions(Options);
    }

    /// <summary>
    /// Parses the first YAML document and throws when processing reports an error.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <returns>The parsed YAML document.</returns>
    public YamlDocument Parse(string text)
    {
        YamlParseResult result = new YamlParseResult();
        TryParse(text, result);
        ThrowIfProcessingFailed(result);
        return Yaml.ResultDocument(result);
    }

    /// <summary>
    /// Decodes UTF-8 or BOM-marked UTF-16 bytes and parses the first YAML document, throwing when processing reports an error.
    /// </summary>
    /// <param name="bytes">The encoded YAML bytes to process.</param>
    /// <returns>The parsed YAML document.</returns>
    public YamlDocument ParseBytes(ReadOnlySpan<byte> bytes)
    {
        YamlParseResult result = new YamlParseResult();
        TryParseBytes(bytes, result);
        ThrowIfProcessingFailed(result);
        return Yaml.ResultDocument(result);
    }

    /// <summary>
    /// Parses every document in a YAML stream and throws when processing reports an error.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <returns>The parsed YAML stream.</returns>
    public YamlStream ParseAll(string text)
    {
        YamlParseResult result = new YamlParseResult();
        TryParseAll(text, result);
        ThrowIfProcessingFailed(result);
        return Yaml.ResultStream(result);
    }

    /// <summary>
    /// Decodes UTF-8 or BOM-marked UTF-16 bytes and parses every YAML document, throwing when processing reports an error.
    /// </summary>
    /// <param name="bytes">The encoded YAML bytes to process.</param>
    /// <returns>The parsed YAML stream.</returns>
    public YamlStream ParseAllBytes(ReadOnlySpan<byte> bytes)
    {
        YamlParseResult result = new YamlParseResult();
        TryParseAllBytes(bytes, result);
        ThrowIfProcessingFailed(result);
        return Yaml.ResultStream(result);
    }

    /// <summary>
    /// Parses the first YAML document and reports YAML diagnostics through the supplied result.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when the operation succeeds; otherwise false.</returns>
    public bool TryParse(string text, YamlParseResult? result)
    {
        if (result == null)
        {
            return false;
        }

        result.Reset();
        Task<YamlComposerResult> task = Task.Run(() => Yaml.ComposeText(text, Options));
        WaitForComposerTask(task);
        if (!task.IsCompletedSuccessfully)
        {
            result.AddFailure("YAMLPROC001", TaskFailure(task.Exception));
            return false;
        }

        YamlComposerResult composer = task.Result;
        Yaml.CopyDiagnostics(composer.Diagnostics, result);
        result.Success = composer.Success && !result.HasErrors();
        if (result.Success)
        {
            result.SetValue(Yaml.FirstDocument(composer.Stream));
        }

        return result.Success;
    }

    /// <summary>
    /// Decodes and parses the first YAML document, reporting encoding and YAML diagnostics through the supplied result.
    /// </summary>
    /// <param name="bytes">The encoded YAML bytes to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when decoding and parsing succeed; otherwise false.</returns>
    public bool TryParseBytes(ReadOnlySpan<byte> bytes, YamlParseResult? result)
    {
        if (result == null)
        {
            return false;
        }

        result.Reset();
        byte[] input = bytes.ToArray();
        Task<YamlComposerResult> task = Task.Run(() => Yaml.ComposeBytes(input, Options));
        WaitForComposerTask(task);
        if (!task.IsCompletedSuccessfully)
        {
            result.AddFailure("YAMLPROC001", TaskFailure(task.Exception));
            return false;
        }

        YamlComposerResult composer = task.Result;
        Yaml.CopyDiagnostics(composer.Diagnostics, result);
        result.Success = composer.Success && !result.HasErrors();
        if (result.Success)
        {
            result.SetValue(Yaml.FirstDocument(composer.Stream));
        }

        return result.Success;
    }

    /// <summary>
    /// Parses every YAML document and reports YAML diagnostics through the supplied result.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when the operation succeeds; otherwise false.</returns>
    public bool TryParseAll(string text, YamlParseResult? result)
    {
        if (result == null)
        {
            return false;
        }

        result.Reset();
        Task<YamlComposerResult> task = Task.Run(() => Yaml.ComposeText(text, Options));
        WaitForComposerTask(task);
        if (!task.IsCompletedSuccessfully)
        {
            result.AddFailure("YAMLPROC001", TaskFailure(task.Exception));
            return false;
        }

        YamlComposerResult composer = task.Result;
        Yaml.CopyDiagnostics(composer.Diagnostics, result);
        result.Success = composer.Success && !result.HasErrors();
        if (result.Success)
        {
            result.SetValue(composer.Stream);
        }

        return result.Success;
    }

    /// <summary>
    /// Decodes and parses every YAML document, reporting encoding and YAML diagnostics through the supplied result.
    /// </summary>
    /// <param name="bytes">The encoded YAML bytes to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when decoding and parsing succeed; otherwise false.</returns>
    public bool TryParseAllBytes(ReadOnlySpan<byte> bytes, YamlParseResult? result)
    {
        if (result == null)
        {
            return false;
        }

        result.Reset();
        byte[] input = bytes.ToArray();
        Task<YamlComposerResult> task = Task.Run(() => Yaml.ComposeBytes(input, Options));
        WaitForComposerTask(task);
        if (!task.IsCompletedSuccessfully)
        {
            result.AddFailure("YAMLPROC001", TaskFailure(task.Exception));
            return false;
        }

        YamlComposerResult composer = task.Result;
        Yaml.CopyDiagnostics(composer.Diagnostics, result);
        result.Success = composer.Success && !result.HasErrors();
        if (result.Success)
        {
            result.SetValue(composer.Stream);
        }

        return result.Success;
    }

    /// <summary>
    /// Loads the first YAML document as dictionaries, lists, scalar CLR values, or null.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    public object? Load(string text)
    {
        YamlParseResult result = new YamlParseResult();
        TryLoad(text, result);
        ThrowIfProcessingFailed(result);
        return result.Value;
    }

    /// <summary>
    /// Decodes UTF-8 or BOM-marked UTF-16 bytes and loads the first YAML document.
    /// </summary>
    /// <param name="bytes">The encoded YAML bytes to process.</param>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    public object? LoadBytes(ReadOnlySpan<byte> bytes)
    {
        YamlParseResult result = new YamlParseResult();
        TryLoadBytes(bytes, result);
        ThrowIfProcessingFailed(result);
        return result.Value;
    }

    /// <summary>
    /// Loads every YAML document as JSON-compatible CLR values.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <returns>The resulting values in document order.</returns>
    public List<object?> LoadAll(string text)
    {
        YamlStream stream = ParseAll(text);
        Task<List<object?>> task = Task.Run(() => LoadStreamValues(stream));
        WaitForListTask(task);
        if (!task.IsCompletedSuccessfully)
        {
            throw ProcessorException("YAML object conversion failed.", TaskFailure(task.Exception));
        }

        return task.Result;
    }

    /// <summary>
    /// Decodes UTF-8 or BOM-marked UTF-16 bytes and loads every YAML document.
    /// </summary>
    /// <param name="bytes">The encoded YAML bytes to process.</param>
    /// <returns>The resulting values in document order.</returns>
    public List<object?> LoadAllBytes(ReadOnlySpan<byte> bytes)
    {
        YamlStream stream = ParseAllBytes(bytes);
        Task<List<object?>> task = Task.Run(() => LoadStreamValues(stream));
        WaitForListTask(task);
        if (!task.IsCompletedSuccessfully)
        {
            throw ProcessorException("YAML object conversion failed.", TaskFailure(task.Exception));
        }

        return task.Result;
    }

    /// <summary>
    /// Loads the first YAML document and reports YAML diagnostics through the supplied result.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when the operation succeeds; otherwise false.</returns>
    public bool TryLoad(string text, YamlParseResult? result)
    {
        if (result == null)
        {
            return false;
        }

        result.Reset();
        YamlParseResult parse = new YamlParseResult();
        if (!TryParse(text, parse))
        {
            Yaml.CopyDiagnostics(parse.Diagnostics, result);
            result.Failure = parse.Failure;
            result.Success = false;
            return false;
        }

        Yaml.CopyDiagnostics(parse.Diagnostics, result);
        YamlDocument document = Yaml.ResultDocument(parse);
        Task<object?> task = Task.Run(() => Yaml.NodeToConfiguredValue(document.Root, Options));
        WaitForObjectTask(task);
        if (!task.IsCompletedSuccessfully)
        {
            result.AddFailure("YAMLO001", TaskFailure(task.Exception));
            return false;
        }

        result.SetValue(task.Result);
        result.Success = !result.HasErrors();
        return result.Success;
    }

    /// <summary>
    /// Decodes and loads the first YAML document, reporting encoding and YAML diagnostics through the supplied result.
    /// </summary>
    /// <param name="bytes">The encoded YAML bytes to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when decoding and loading succeed; otherwise false.</returns>
    public bool TryLoadBytes(ReadOnlySpan<byte> bytes, YamlParseResult? result)
    {
        if (result == null)
        {
            return false;
        }

        result.Reset();
        YamlParseResult parse = new YamlParseResult();
        if (!TryParseBytes(bytes, parse))
        {
            Yaml.CopyDiagnostics(parse.Diagnostics, result);
            result.Failure = parse.Failure;
            result.Success = false;
            return false;
        }

        Yaml.CopyDiagnostics(parse.Diagnostics, result);
        YamlDocument document = Yaml.ResultDocument(parse);
        Task<object?> task = Task.Run(() => Yaml.NodeToConfiguredValue(document.Root, Options));
        WaitForObjectTask(task);
        if (!task.IsCompletedSuccessfully)
        {
            result.AddFailure("YAMLO001", TaskFailure(task.Exception));
            return false;
        }

        result.SetValue(task.Result);
        result.Success = !result.HasErrors();
        return result.Success;
    }

    /// <summary>
    /// Converts a supported CLR value to YAML text.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The emitted YAML text.</returns>
    public string Dump(object? value)
    {
        YamlParseResult result = new YamlParseResult();
        TryDump(value, result);
        ThrowIfProcessingFailed(result);
        string? text = result.Value as string;
        return text == null ? "" : text;
    }

    /// <summary>
    /// Converts a supported CLR value to YAML text and stores it in the supplied result.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when the operation succeeds; otherwise false.</returns>
    public bool TryDump(object? value, YamlParseResult? result)
    {
        if (result == null)
        {
            return false;
        }

        result.Reset();
        Task<string> task = Task.Run(() => DumpValue(value));
        WaitForStringTask(task);
        if (!task.IsCompletedSuccessfully)
        {
            result.AddFailure("YAMLO002", TaskFailure(task.Exception));
            return false;
        }

        result.SetValue(task.Result);
        result.Success = true;
        return true;
    }

    /// <summary>
    /// Serializes a supported CLR value to YAML text.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The emitted YAML text.</returns>
    public string Serialize(object? value)
    {
        return Dump(value);
    }

    /// <summary>
    /// Loads YAML and converts the result to the requested CLR type.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="targetType">The CLR type to create.</param>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    public object? Deserialize(string text, Type? targetType)
    {
        YamlOptions effective = Yaml.EffectiveOptions(Options);
        object? value = Load(text);
        Task<object?> task = Task.Run(() => Yaml.ConvertLoadedValue(value, targetType, effective.Object));
        WaitForObjectTask(task);
        if (!task.IsCompletedSuccessfully)
        {
            throw ProcessorException("YAML object conversion failed.", TaskFailure(task.Exception));
        }

        return task.Result;
    }

    /// <summary>
    /// Decodes YAML bytes and converts the loaded result to the requested CLR type.
    /// </summary>
    /// <param name="bytes">The encoded YAML bytes to process.</param>
    /// <param name="targetType">The CLR type to create.</param>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    public object? DeserializeBytes(ReadOnlySpan<byte> bytes, Type? targetType)
    {
        YamlOptions effective = Yaml.EffectiveOptions(Options);
        object? value = LoadBytes(bytes);
        Task<object?> task = Task.Run(() => Yaml.ConvertLoadedValue(value, targetType, effective.Object));
        WaitForObjectTask(task);
        if (!task.IsCompletedSuccessfully)
        {
            throw ProcessorException("YAML object conversion failed.", TaskFailure(task.Exception));
        }

        return task.Result;
    }

    internal List<object?> LoadStreamValues(YamlStream stream)
    {
        List<object?> values = new List<object?>();
        int index = 0;
        while (index < stream.Documents.Count)
        {
            values.Add(Yaml.NodeToConfiguredValue(stream.Documents[index].Root, Options));
            index = index + 1;
        }

        return values;
    }

    internal string DumpValue(object? value)
    {
        YamlOptions effective = Yaml.EffectiveOptions(Options);
        YamlDocument document = Yaml.DocumentFromConfiguredValue(value, effective);
        return YamlEmitter.EmitDocument(document, effective.Emitter);
    }

    internal static void WaitForComposerTask(Task<YamlComposerResult> task)
    {
        SpinWait.SpinUntil(() => task.IsCompleted);
    }

    internal static void WaitForObjectTask(Task<object?> task)
    {
        SpinWait.SpinUntil(() => task.IsCompleted);
    }

    internal static void WaitForListTask(Task<List<object?>> task)
    {
        SpinWait.SpinUntil(() => task.IsCompleted);
    }

    internal static void WaitForStringTask(Task<string> task)
    {
        SpinWait.SpinUntil(() => task.IsCompleted);
    }

    internal static Exception? TaskFailure(AggregateException? aggregate)
    {
        if (aggregate == null)
        {
            return null;
        }

        return aggregate.GetBaseException();
    }

    internal static YamlException ProcessorException(string message, Exception? failure)
    {
        YamlException? yamlFailure = failure as YamlException;
        if (yamlFailure != null)
        {
            return yamlFailure;
        }

        string detail = message == "" ? "YAML processing failed." : message;
        if (failure != null && failure.Message != "" && !detail.Contains(failure.Message))
        {
            detail = detail + " " + failure.Message;
        }

        return new YamlException(detail, failure);
    }

    internal void ThrowIfProcessingFailed(YamlParseResult? result)
    {
        if (result != null && result.Success && !result.HasErrors())
        {
            return;
        }

        string message = result == null ? "YAML processing failed." : result.FormatDiagnostics();
        Exception? failure = result == null ? null : result.Failure;
        throw ProcessorException(message, failure);
    }
}
