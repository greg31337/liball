using System;
using System.Globalization;

namespace html;
/// <summary>Identifies a stable parsing, validation, limit, or conversion condition reported by the library.</summary>
public enum HtmlDiagnosticCode
{
    /// <summary>Indicates that no option or value applies.</summary>
    None = 0,
    /// <summary>Reports a null character encountered in HTML input.</summary>
    InputNullCharacter = 1001,
    /// <summary>Reports an invalid Unicode scalar encountered in input.</summary>
    InputInvalidScalar = 1002,
    /// <summary>Reports that a caller-provided input source violated its read contract.</summary>
    InputReadFailed = 1003,
    /// <summary>Reports that normalized input exceeded a configured limit.</summary>
    InputLimitExceeded = 1099,
    /// <summary>Reports an encoding label that the decoder does not support.</summary>
    EncodingUnsupportedLabel = 1101,
    /// <summary>Reports a malformed byte sequence replaced during decoding.</summary>
    EncodingInvalidSequence = 1102,
    /// <summary>Reports a malformed or invalid HTML character reference.</summary>
    CharacterReferenceInvalid = 2001,
    /// <summary>Reports a character reference accepted without its terminating semicolon.</summary>
    CharacterReferenceMissingSemicolon = 2002,
    /// <summary>Reports a character that triggered tokenizer error recovery.</summary>
    TokenizerUnexpectedCharacter = 3001,
    /// <summary>Reports a duplicate attribute removed from a start tag.</summary>
    TokenizerDuplicateAttribute = 3002,
    /// <summary>Reports a malformed comment handled by tokenizer recovery.</summary>
    TokenizerMalformedComment = 3003,
    /// <summary>Reports a malformed document type handled by tokenizer recovery.</summary>
    TokenizerMalformedDoctype = 3004,
    /// <summary>Reports a token handled through tree-construction error recovery.</summary>
    TreeBuilderUnexpectedToken = 4001,
    /// <summary>Reports that tree construction exceeded the configured depth.</summary>
    TreeBuilderDepthExceeded = 4002,
    /// <summary>Reports invalid selector syntax.</summary>
    SelectorInvalidSyntax = 5001,
    /// <summary>Reports a valid selector feature that is not implemented.</summary>
    SelectorUnsupportedFeature = 5002,
    /// <summary>Reports that selector compilation or matching exceeded a complexity limit.</summary>
    SelectorComplexityExceeded = 5003,
    /// <summary>Reports a URL that could not be parsed or normalized safely.</summary>
    UrlInvalid = 6001,
    /// <summary>Reports a URL scheme rejected by policy.</summary>
    UrlUnsafeScheme = 6002,
    /// <summary>Reports malformed HTML microsyntax input.</summary>
    MicrosyntaxInvalid = 6501,
    /// <summary>Reports that HTML microsyntax processing exceeded a configured limit.</summary>
    MicrosyntaxLimitExceeded = 6502,
    /// <summary>Reports that serialization exceeded a configured output limit.</summary>
    SerializationLimitExceeded = 7001,
    /// <summary>Reports an element removed by sanitizer policy.</summary>
    SanitizerRemovedElement = 8001,
    /// <summary>Reports an attribute removed by sanitizer policy.</summary>
    SanitizerRemovedAttribute = 8002,
    /// <summary>Reports a URL-bearing value rejected by sanitizer policy.</summary>
    SanitizerUnsafeUrl = 8003,
    /// <summary>Reports that sanitization exceeded a security-relevant resource limit.</summary>
    SanitizerLimitExceeded = 8099,
    /// <summary>Reports that plain-text conversion reached a configured limit.</summary>
    TextConversionLimitExceeded = 9001,
    /// <summary>Reports that Markdown conversion reached a configured limit.</summary>
    MarkdownConversionLimitExceeded = 9101,
    /// <summary>Reports invalid Markdown conversion settings.</summary>
    MarkdownInvalidConfiguration = 9102,
    /// <summary>Reports a Markdown selector rule that could not be compiled.</summary>
    MarkdownInvalidRule = 9103,
    /// <summary>Reports an element not handled by the configured Markdown rules.</summary>
    MarkdownUnknownElement = 9104,
    /// <summary>Reports malformed or unsupported JSON-LD metadata.</summary>
    MetadataInvalidJsonLd = 10001,
    /// <summary>Reports that no usable article content could be extracted.</summary>
    ArticleExtractionFailed = 10101,
    /// <summary>Reports that the coordinated processing pipeline reached a shared limit.</summary>
    PipelineLimitExceeded = 11001,
    /// <summary>Reports a general configured resource limit violation.</summary>
    ResourceLimitExceeded = 12001,
    /// <summary>Reports that an operation ended without producing a complete result.</summary>
    OperationIncomplete = 12002,
    /// <summary>Reports that cancellation stopped an operation before completion.</summary>
    OperationCancelled = 12003,
    /// <summary>Reports an operation failure for which no more specific diagnostic applies.</summary>
    OperationFailed = 12004
}
