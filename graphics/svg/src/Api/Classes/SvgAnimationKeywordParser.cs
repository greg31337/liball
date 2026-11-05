namespace svg;

/// <summary>Parses SVG animation enumeration keywords.</summary>
public class SvgAnimationKeywordParser : ISvgAnimationKeywordParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgAnimationKeywordParser()
    {
    }

    /// <summary>Parses <c>always</c>, <c>whenNotActive</c>, or <c>never</c>.</summary>
    /// <param name="value">The restart keyword.</param>
    /// <returns>A restart enum result.</returns>
    public SvgAnimationKeywordParseResult ParseRestart(string value)
    {
        string? text = new SvgAnimationKeywordParser().Trim(value);
        if (text == "always") return new SvgAnimationKeywordParser().Success((int)SvgAnimationRestart.Always, text);
        if (text == "whenNotActive") return new SvgAnimationKeywordParser().Success((int)SvgAnimationRestart.WhenNotActive, text);
        if (text == "never") return new SvgAnimationKeywordParser().Success((int)SvgAnimationRestart.Never, text);
        return new SvgAnimationKeywordParser().Failure(value);
    }

    /// <summary>Parses <c>remove</c> or <c>freeze</c>.</summary>
    /// <param name="value">The fill keyword.</param>
    /// <returns>A fill enum result.</returns>
    public SvgAnimationKeywordParseResult ParseFill(string value)
    {
        string? text = new SvgAnimationKeywordParser().Trim(value);
        if (text == "remove") return new SvgAnimationKeywordParser().Success((int)SvgAnimationFill.Remove, text);
        if (text == "freeze") return new SvgAnimationKeywordParser().Success((int)SvgAnimationFill.Freeze, text);
        return new SvgAnimationKeywordParser().Failure(value);
    }

    /// <summary>Parses <c>replace</c> or <c>sum</c>.</summary>
    /// <param name="value">The additive keyword.</param>
    /// <returns>An additive enum result.</returns>
    public SvgAnimationKeywordParseResult ParseAdditive(string value)
    {
        string? text = new SvgAnimationKeywordParser().Trim(value);
        if (text == "replace") return new SvgAnimationKeywordParser().Success((int)SvgAnimationAdditive.Replace, text);
        if (text == "sum") return new SvgAnimationKeywordParser().Success((int)SvgAnimationAdditive.Sum, text);
        return new SvgAnimationKeywordParser().Failure(value);
    }

    /// <summary>Parses <c>none</c> or <c>sum</c>.</summary>
    /// <param name="value">The accumulate keyword.</param>
    /// <returns>An accumulate enum result.</returns>
    public SvgAnimationKeywordParseResult ParseAccumulate(string value)
    {
        string? text = new SvgAnimationKeywordParser().Trim(value);
        if (text == "none") return new SvgAnimationKeywordParser().Success((int)SvgAnimationAccumulate.None, text);
        if (text == "sum") return new SvgAnimationKeywordParser().Success((int)SvgAnimationAccumulate.Sum, text);
        return new SvgAnimationKeywordParser().Failure(value);
    }

    /// <summary>Parses <c>discrete</c>, <c>linear</c>, <c>paced</c>, or <c>spline</c>.</summary>
    /// <param name="value">The calcMode keyword.</param>
    /// <returns>A calcMode enum result.</returns>
    public SvgAnimationKeywordParseResult ParseCalcMode(string value)
    {
        string? text = new SvgAnimationKeywordParser().Trim(value);
        if (text == "discrete") return new SvgAnimationKeywordParser().Success((int)SvgAnimationCalcMode.Discrete, text);
        if (text == "linear") return new SvgAnimationKeywordParser().Success((int)SvgAnimationCalcMode.Linear, text);
        if (text == "paced") return new SvgAnimationKeywordParser().Success((int)SvgAnimationCalcMode.Paced, text);
        if (text == "spline") return new SvgAnimationKeywordParser().Success((int)SvgAnimationCalcMode.Spline, text);
        return new SvgAnimationKeywordParser().Failure(value);
    }

    /// <summary>Parses <c>auto</c>, <c>CSS</c>, or <c>XML</c>.</summary>
    /// <param name="value">The attributeType keyword.</param>
    /// <returns>An attributeType enum result.</returns>
    public SvgAnimationKeywordParseResult ParseAttributeType(string value)
    {
        string? text = new SvgAnimationKeywordParser().Trim(value);
        if (text == "auto") return new SvgAnimationKeywordParser().Success((int)SvgAnimationAttributeType.Auto, text);
        if (text == "CSS") return new SvgAnimationKeywordParser().Success((int)SvgAnimationAttributeType.Css, text);
        if (text == "XML") return new SvgAnimationKeywordParser().Success((int)SvgAnimationAttributeType.Xml, text);
        return new SvgAnimationKeywordParser().Failure(value);
    }
string? Trim(string? value)
    {
        if (value == null) return null;
        int start = 0;
        int end = value.Length;
        while (start < end && SvgSpanCharacters.IsSvgWhitespace(value[start])) start = start + 1;
        while (end > start && SvgSpanCharacters.IsSvgWhitespace(value[end - 1])) end = end - 1;
        return value.Substring(start, end - start);
    }
SvgAnimationKeywordParseResult Success(int value, string text)
    {
        return new SvgAnimationKeywordParseResult(true, SvgTimingParseError.None, value, text, -1);
    }
SvgAnimationKeywordParseResult Failure(string? value)
    {
        if (value == null) return new SvgAnimationKeywordParseResult(false, SvgTimingParseError.InvalidArgument, 0, "", 0);
        int offset = 0;
        while (offset < value.Length && SvgSpanCharacters.IsSvgWhitespace(value[offset])) offset = offset + 1;
        SvgTimingParseError error = offset == value.Length ? SvgTimingParseError.ExpectedValue : SvgTimingParseError.InvalidSpecifier;
        return new SvgAnimationKeywordParseResult(false, error, 0, "", offset);
    }
}
