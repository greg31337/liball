using System;
using System.Collections.Generic;

namespace yaml;

/// <summary>
/// Defines the low-level scanning, parsing, composition, schema, and emission pipeline.
/// </summary>
public interface IYamlPipeline
{
    /// <summary>Scans YAML text into tokens and diagnostics.</summary>
    /// <param name="text">The YAML text to scan.</param>
    /// <param name="options">The parser settings; pass null to use defaults.</param>
    /// <returns>The scanner result.</returns>
    public YamlScannerResult Scan(string? text, YamlParserOptions? options);

    /// <summary>Decodes and scans UTF-8 or BOM-marked UTF-16 YAML bytes into tokens and diagnostics.</summary>
    /// <param name="bytes">The encoded YAML bytes to scan.</param>
    /// <param name="options">The parser settings; pass null to use defaults.</param>
    /// <param name="security">The input limits; pass null to use defaults.</param>
    /// <returns>The scanner result, including an encoding diagnostic when decoding fails.</returns>
    public YamlScannerResult ScanBytes(ReadOnlySpan<byte> bytes, YamlParserOptions? options, YamlSecurityOptions? security);

    /// <summary>Scans YAML text and returns only its tokens.</summary>
    /// <param name="text">The YAML text to scan.</param>
    /// <param name="options">The parser settings; pass null to use defaults.</param>
    /// <returns>The tokens in source order.</returns>
    public List<YamlToken> ScanTokens(string? text, YamlParserOptions? options);

    /// <summary>Parses a token sequence into events and diagnostics.</summary>
    /// <param name="tokens">The tokens to parse.</param>
    /// <param name="options">The parser settings; pass null to use defaults.</param>
    /// <returns>The parser result.</returns>
    public YamlParserResult ParseTokens(IReadOnlyList<YamlToken>? tokens, YamlParserOptions? options);

    /// <summary>Parses the tokens and diagnostics in a scanner result.</summary>
    /// <param name="result">The scanner result to consume.</param>
    /// <param name="options">The parser settings; pass null to use defaults.</param>
    /// <returns>The parser result.</returns>
    public YamlParserResult ParseScannerResult(YamlScannerResult? result, YamlParserOptions? options);

    /// <summary>Decodes and parses UTF-8 or BOM-marked UTF-16 YAML bytes into events and diagnostics.</summary>
    /// <param name="bytes">The encoded YAML bytes to parse.</param>
    /// <param name="options">The parser settings; pass null to use defaults.</param>
    /// <param name="security">The input limits; pass null to use defaults.</param>
    /// <returns>The parser result, including an encoding diagnostic when decoding fails.</returns>
    public YamlParserResult ParseBytes(ReadOnlySpan<byte> bytes, YamlParserOptions? options, YamlSecurityOptions? security);

    /// <summary>Composes parser events into a mutable YAML stream.</summary>
    /// <param name="events">The parser events to compose.</param>
    /// <param name="options">The composer settings; pass null to use defaults.</param>
    /// <returns>The composer result.</returns>
    public YamlComposerResult ComposeEvents(IReadOnlyList<YamlEvent>? events, YamlComposerOptions? options);

    /// <summary>Composes a parser result into a mutable YAML stream.</summary>
    /// <param name="result">The parser result to consume.</param>
    /// <param name="options">The composer settings; pass null to use defaults.</param>
    /// <returns>The composer result.</returns>
    public YamlComposerResult ComposeParserResult(YamlParserResult? result, YamlComposerOptions? options);

    /// <summary>Resolves schema tags throughout a YAML stream.</summary>
    /// <param name="stream">The stream to update.</param>
    /// <param name="options">The schema settings; pass null to use defaults.</param>
    public void ResolveStream(YamlStream? stream, YamlSchemaOptions? options);

    /// <summary>Resolves schema tags throughout one YAML document.</summary>
    /// <param name="document">The document to update.</param>
    /// <param name="options">The schema settings; pass null to use defaults.</param>
    public void ResolveDocument(YamlDocument? document, YamlSchemaOptions? options);

    /// <summary>Resolves schema tags throughout one YAML node graph.</summary>
    /// <param name="node">The node graph to update.</param>
    /// <param name="options">The schema settings; pass null to use defaults.</param>
    public void ResolveNode(YamlNode? node, YamlSchemaOptions? options);

    /// <summary>Validates a YAML stream without changing it or throwing for validation failures.</summary>
    /// <param name="stream">The stream to validate.</param>
    /// <param name="options">The schema, composer, and security settings; pass null to use defaults.</param>
    /// <returns>A result containing success state and structured validation diagnostics.</returns>
    public YamlResult ValidateStream(YamlStream? stream, YamlOptions? options);

    /// <summary>Validates one YAML document without changing it or throwing for validation failures.</summary>
    /// <param name="document">The document to validate.</param>
    /// <param name="options">The schema, composer, and security settings; pass null to use defaults.</param>
    /// <returns>A result containing success state and structured validation diagnostics.</returns>
    public YamlResult ValidateDocument(YamlDocument? document, YamlOptions? options);

    /// <summary>Validates one YAML node graph without changing it or throwing for validation failures.</summary>
    /// <param name="node">The node graph to validate.</param>
    /// <param name="options">The schema, composer, and security settings; pass null to use defaults.</param>
    /// <returns>A result containing success state and structured validation diagnostics.</returns>
    public YamlResult ValidateNode(YamlNode? node, YamlOptions? options);

    /// <summary>Emits a YAML stream.</summary>
    /// <param name="stream">The stream to emit.</param>
    /// <param name="options">The emitter settings; pass null to use defaults.</param>
    /// <returns>The emitted YAML text.</returns>
    public string EmitStream(YamlStream? stream, YamlEmitterOptions? options);

    /// <summary>Emits one YAML document.</summary>
    /// <param name="document">The document to emit.</param>
    /// <param name="options">The emitter settings; pass null to use defaults.</param>
    /// <returns>The emitted YAML text.</returns>
    public string EmitDocument(YamlDocument? document, YamlEmitterOptions? options);

    /// <summary>Emits one YAML node graph.</summary>
    /// <param name="node">The node to emit.</param>
    /// <param name="options">The emitter settings; pass null to use defaults.</param>
    /// <returns>The emitted YAML text.</returns>
    public string EmitNode(YamlNode? node, YamlEmitterOptions? options);
}
