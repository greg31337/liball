namespace json;

/// <summary>Contains policies that control JSON output.</summary>
public class JsonWriterOptions
{
    /// <summary>The whitespace formatting applied to output.</summary>
    public JsonFormatting Formatting;

    /// <summary>The escaping policy applied to string content.</summary>
    public JsonStringEscapeMode StringEscapeMode;

    /// <summary>The ordering applied to object properties.</summary>
    public JsonPropertyOrder PropertyOrder;

    /// <summary>The policy applied to duplicate decoded object names.</summary>
    public JsonDuplicateKeyPolicy DuplicateKeyPolicy;

    /// <summary>The policy applied to UTF-16 surrogate code units.</summary>
    public JsonUnicodePolicy UnicodePolicy;

    /// <summary>The framing mode applied to output.</summary>
    public JsonInputMode OutputMode;

    /// <summary>Escapes solidus characters in addition to required JSON escapes.</summary>
    public bool EscapeSolidus;

    /// <summary>Uses uppercase hexadecimal digits in Unicode escapes.</summary>
    public bool UseUppercaseHexadecimalEscapes;

    /// <summary>Skips validation of publicly mutable document model values before writing.</summary>
    public bool SkipModelValidation;

    /// <summary>Omits the otherwise-default space after a name separator in pretty output.</summary>
    public bool OmitSpaceAfterColon;

    /// <summary>The indentation text, or <c>null</c> to use <see cref="JsonDefaults.Indentation"/>.</summary>
    public string? Indentation;

    /// <summary>The line ending, or <c>null</c> to use <see cref="JsonDefaults.NewLine"/>.</summary>
    public string? NewLine;

    /// <summary>The resource limits, or <c>null</c> to use all values from <see cref="JsonDefaults"/>.</summary>
    public JsonLimits? Limits;
}
