namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgFunctionValueParser"/>.</summary>
public interface ISvgFunctionValueParser
{
    /// <summary>Parses one complete function while preserving its raw content.</summary>
    /// <param name="value">The function text.</param>
    /// <param name="maxNameCharacters">The maximum raw name length.</param>
    /// <param name="maxContentCharacters">The maximum raw content length.</param>
    /// <returns>A parsed function or stable failure.</returns>
    SvgFunctionValueParseResult Parse(string? value, int maxNameCharacters, int maxContentCharacters);
}

/// <summary>Defines the public operations exposed by <see cref="SvgIriParser"/>.</summary>
public interface ISvgIriParser
{
    /// <summary>Parses a trimmed URI or Unicode IRI reference without resolving it.</summary>
    /// <param name="value">The reference text.</param>
    /// <param name="maxCharacters">The maximum trimmed reference length.</param>
    /// <returns>A preserved reference or stable failure.</returns>
    SvgIriParseResult ParseIri(string? value, int maxCharacters);
    /// <summary>Parses a nonempty fragment-only reference.</summary>
    /// <param name="value">The fragment reference beginning with a number sign.</param>
    /// <param name="maxCharacters">The maximum trimmed reference length.</param>
    /// <returns>A fragment-only reference or stable failure.</returns>
    SvgIriParseResult ParseFragment(string value, int maxCharacters);
    /// <summary>Parses one complete CSS <c>url()</c> reference with CSS escapes.</summary>
    /// <param name="value">The URL function text.</param>
    /// <param name="maxCharacters">The maximum decoded URL length.</param>
    /// <returns>A decoded URL reference or stable failure.</returns>
    SvgIriParseResult ParseUrl(string? value, int maxCharacters);
}

/// <summary>Defines the public operations exposed by <see cref="SvgMediaTypeParser"/>.</summary>
public interface ISvgMediaTypeParser
{
    /// <summary>Parses a complete media type.</summary>
    /// <param name="value">The media type text.</param>
    /// <param name="maxParameters">The maximum parameter count.</param>
    /// <param name="maxTokenCharacters">The maximum token or decoded quoted value length.</param>
    /// <returns>A parsed media type or stable failure.</returns>
    SvgMediaTypeParseResult Parse(string value, int maxParameters, int maxTokenCharacters);
}

/// <summary>Defines the public operations exposed by <see cref="SvgLanguageListParser"/>.</summary>
public interface ISvgLanguageListParser
{
    /// <summary>Parses a nonempty comma-separated system-language list.</summary>
    /// <param name="value">The language list text.</param>
    /// <param name="maxItems">The maximum language count.</param>
    /// <param name="maxTokenCharacters">The maximum tag length.</param>
    /// <returns>A normalized source-mapped list or stable failure.</returns>
    SvgTextTokenListParseResult Parse(string value, int maxItems, int maxTokenCharacters);
}

/// <summary>Defines the public operations exposed by <see cref="SvgFeatureStringParser"/>.</summary>
public interface ISvgFeatureStringParser
{
    /// <summary>Parses a nonempty required-features string list.</summary>
    /// <param name="value">The feature string list.</param>
    /// <param name="maxItems">The maximum feature count.</param>
    /// <param name="maxTokenCharacters">The maximum feature string length.</param>
    /// <returns>A preserved source-mapped list or stable failure.</returns>
    SvgTextTokenListParseResult Parse(string value, int maxItems, int maxTokenCharacters);
}
