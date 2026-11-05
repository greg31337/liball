namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgAnimationValueParser"/>.</summary>
public interface ISvgAnimationValueParser
{
    /// <summary>Parses a bounded monotonic keyTimes list in the unit interval.</summary>
    /// <param name="value">The keyTimes text.</param>
    /// <param name="maxItems">The maximum value count.</param>
    /// <param name="maxTokenCharacters">The maximum number token length.</param>
    /// <returns>A normalized value list or valid prefix and failure.</returns>
    SvgKeyValueListParseResult ParseKeyTimes(string value, int maxItems, int maxTokenCharacters);
    /// <summary>Parses a bounded keyPoints list in the unit interval.</summary>
    /// <param name="value">The keyPoints text.</param>
    /// <param name="maxItems">The maximum value count.</param>
    /// <param name="maxTokenCharacters">The maximum number token length.</param>
    /// <returns>A normalized value list or valid prefix and failure.</returns>
    SvgKeyValueListParseResult ParseKeyPoints(string value, int maxItems, int maxTokenCharacters);
    /// <summary>Parses semicolon-separated cubic Bézier keySplines.</summary>
    /// <param name="value">The keySplines text.</param>
    /// <param name="maxItems">The maximum spline count.</param>
    /// <param name="maxTokenCharacters">The maximum number token length.</param>
    /// <returns>A spline list or valid prefix and failure.</returns>
    SvgKeySplineListParseResult ParseKeySplines(string? value, int maxItems, int maxTokenCharacters);
}

/// <summary>Defines the public operations exposed by <see cref="SvgAnimationKeywordParser"/>.</summary>
public interface ISvgAnimationKeywordParser
{
    /// <summary>Parses <c>always</c>, <c>whenNotActive</c>, or <c>never</c>.</summary>
    /// <param name="value">The restart keyword.</param>
    /// <returns>A restart enum result.</returns>
    SvgAnimationKeywordParseResult ParseRestart(string value);
    /// <summary>Parses <c>remove</c> or <c>freeze</c>.</summary>
    /// <param name="value">The fill keyword.</param>
    /// <returns>A fill enum result.</returns>
    SvgAnimationKeywordParseResult ParseFill(string value);
    /// <summary>Parses <c>replace</c> or <c>sum</c>.</summary>
    /// <param name="value">The additive keyword.</param>
    /// <returns>An additive enum result.</returns>
    SvgAnimationKeywordParseResult ParseAdditive(string value);
    /// <summary>Parses <c>none</c> or <c>sum</c>.</summary>
    /// <param name="value">The accumulate keyword.</param>
    /// <returns>An accumulate enum result.</returns>
    SvgAnimationKeywordParseResult ParseAccumulate(string value);
    /// <summary>Parses <c>discrete</c>, <c>linear</c>, <c>paced</c>, or <c>spline</c>.</summary>
    /// <param name="value">The calcMode keyword.</param>
    /// <returns>A calcMode enum result.</returns>
    SvgAnimationKeywordParseResult ParseCalcMode(string value);
    /// <summary>Parses <c>auto</c>, <c>CSS</c>, or <c>XML</c>.</summary>
    /// <param name="value">The attributeType keyword.</param>
    /// <returns>An attributeType enum result.</returns>
    SvgAnimationKeywordParseResult ParseAttributeType(string value);
}
