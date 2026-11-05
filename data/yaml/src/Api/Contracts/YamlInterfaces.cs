using System;
using System.Collections.Generic;

namespace yaml;

/// <summary>
/// Defines the high-level YAML operations used by applications and injectable services.
/// </summary>
/// <remarks>
/// Implement this interface in tests when application code should be exercised without parsing or emitting YAML.
/// </remarks>
public interface IYamlProcessor
{
    /// <summary>
    /// Replaces the settings used by later operations with an independent copy.
    /// </summary>
    /// <param name="options">The settings to copy; pass null to restore library defaults.</param>
    public void Configure(YamlOptions? options);

    /// <summary>
    /// Returns an independent copy of the settings used by this processor.
    /// </summary>
    /// <returns>The copied processor settings.</returns>
    public YamlOptions GetOptions();

    /// <summary>
    /// Parses the first YAML document into the mutable document model.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <returns>The parsed YAML document.</returns>
    public YamlDocument Parse(string text);

    /// <summary>
    /// Decodes UTF-8 or BOM-marked UTF-16 bytes and parses the first YAML document.
    /// </summary>
    /// <param name="bytes">The encoded YAML bytes to process.</param>
    /// <returns>The parsed YAML document.</returns>
    public YamlDocument ParseBytes(ReadOnlySpan<byte> bytes);

    /// <summary>
    /// Parses every document in a YAML stream into the mutable document model.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <returns>The parsed YAML stream.</returns>
    public YamlStream ParseAll(string text);

    /// <summary>
    /// Decodes UTF-8 or BOM-marked UTF-16 bytes and parses every YAML document.
    /// </summary>
    /// <param name="bytes">The encoded YAML bytes to process.</param>
    /// <returns>The parsed YAML stream.</returns>
    public YamlStream ParseAllBytes(ReadOnlySpan<byte> bytes);

    /// <summary>
    /// Parses the first YAML document and reports YAML diagnostics through the supplied result.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when the operation succeeds; otherwise false.</returns>
    public bool TryParse(string text, YamlParseResult? result);

    /// <summary>
    /// Decodes and parses the first YAML document, reporting encoding and YAML diagnostics through the supplied result.
    /// </summary>
    /// <param name="bytes">The encoded YAML bytes to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when decoding and parsing succeed; otherwise false.</returns>
    public bool TryParseBytes(ReadOnlySpan<byte> bytes, YamlParseResult? result);

    /// <summary>
    /// Parses every YAML document and reports YAML diagnostics through the supplied result.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when the operation succeeds; otherwise false.</returns>
    public bool TryParseAll(string text, YamlParseResult? result);

    /// <summary>
    /// Decodes and parses every YAML document, reporting encoding and YAML diagnostics through the supplied result.
    /// </summary>
    /// <param name="bytes">The encoded YAML bytes to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when decoding and parsing succeed; otherwise false.</returns>
    public bool TryParseAllBytes(ReadOnlySpan<byte> bytes, YamlParseResult? result);

    /// <summary>
    /// Loads the first YAML document as dictionaries, lists, scalar CLR values, or null.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    public object? Load(string text);

    /// <summary>
    /// Decodes UTF-8 or BOM-marked UTF-16 bytes and loads the first YAML document.
    /// </summary>
    /// <param name="bytes">The encoded YAML bytes to process.</param>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    public object? LoadBytes(ReadOnlySpan<byte> bytes);

    /// <summary>
    /// Loads every YAML document as JSON-compatible CLR values.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <returns>The resulting values in document order.</returns>
    public List<object?> LoadAll(string text);

    /// <summary>
    /// Decodes UTF-8 or BOM-marked UTF-16 bytes and loads every YAML document.
    /// </summary>
    /// <param name="bytes">The encoded YAML bytes to process.</param>
    /// <returns>The resulting values in document order.</returns>
    public List<object?> LoadAllBytes(ReadOnlySpan<byte> bytes);

    /// <summary>
    /// Loads the first YAML document and reports YAML diagnostics through the supplied result.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when the operation succeeds; otherwise false.</returns>
    public bool TryLoad(string text, YamlParseResult? result);

    /// <summary>
    /// Decodes and loads the first YAML document, reporting encoding and YAML diagnostics through the supplied result.
    /// </summary>
    /// <param name="bytes">The encoded YAML bytes to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when decoding and loading succeed; otherwise false.</returns>
    public bool TryLoadBytes(ReadOnlySpan<byte> bytes, YamlParseResult? result);

    /// <summary>
    /// Converts a supported CLR value to YAML text.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The emitted YAML text.</returns>
    public string Dump(object? value);

    /// <summary>
    /// Converts a supported CLR value to YAML text and stores it in the supplied result.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="result">The result object to reset and populate.</param>
    /// <returns>True when the operation succeeds; otherwise false.</returns>
    public bool TryDump(object? value, YamlParseResult? result);

    /// <summary>
    /// Serializes a supported CLR value to YAML text.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The emitted YAML text.</returns>
    public string Serialize(object? value);

    /// <summary>
    /// Loads YAML and converts the result to the requested CLR type.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="targetType">The CLR type to create.</param>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    public object? Deserialize(string text, Type? targetType);

    /// <summary>
    /// Decodes YAML bytes and converts the loaded result to the requested CLR type.
    /// </summary>
    /// <param name="bytes">The encoded YAML bytes to process.</param>
    /// <param name="targetType">The CLR type to create.</param>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    public object? DeserializeBytes(ReadOnlySpan<byte> bytes, Type? targetType);
}

/// <summary>
/// Recognizes scalar text for a custom YAML tag.
/// </summary>
public interface IYamlScalarResolver
{
    /// <summary>
    /// Determines whether the supplied scalar belongs to the custom tag.
    /// </summary>
    /// <param name="value">The scalar text.</param>
    /// <param name="style">The scalar presentation style.</param>
    /// <returns>True when the scalar matches; otherwise false.</returns>
    public bool Matches(string value, YamlScalarStyle style);
}

/// <summary>
/// Converts a tagged YAML scalar to and from a CLR value.
/// </summary>
public interface IYamlScalarConverter
{
    /// <summary>
    /// Converts scalar text into a CLR value.
    /// </summary>
    /// <param name="value">The scalar text.</param>
    /// <returns>The converted CLR value.</returns>
    public object? Read(string value);

    /// <summary>
    /// Converts a CLR value into scalar text.
    /// </summary>
    /// <param name="value">The CLR value.</param>
    /// <returns>The converted scalar text.</returns>
    public string Write(object value);
}

/// <summary>
/// Converts a complete CLR value before object deserialization or serialization.
/// </summary>
public interface IYamlTypeConverter
{
    /// <summary>
    /// Converts a loaded value into the registered CLR type.
    /// </summary>
    /// <param name="value">The loaded value.</param>
    /// <returns>The converted CLR value.</returns>
    public object? Read(object value);

    /// <summary>
    /// Converts the registered CLR type into a YAML-compatible value.
    /// </summary>
    /// <param name="value">The CLR value.</param>
    /// <returns>The YAML-compatible value.</returns>
    public object? Write(object value);
}
