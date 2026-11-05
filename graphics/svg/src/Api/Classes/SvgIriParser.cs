namespace svg;

/// <summary>Parses bounded URI, IRI, fragment, and CSS URL references.</summary>
public class SvgIriParser : ISvgIriParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgIriParser()
    {
    }

    /// <summary>Parses a trimmed URI or Unicode IRI reference without resolving it.</summary>
    /// <param name="value">The reference text.</param>
    /// <param name="maxCharacters">The maximum trimmed reference length.</param>
    /// <returns>A preserved reference or stable failure.</returns>
    public SvgIriParseResult ParseIri(string? value, int maxCharacters)
    {
        if (value == null || maxCharacters < 1) return new SvgIriParser().Failure(SvgIriParseError.InvalidArgument, 0);
        int start = SvgIriLexical.SkipWhitespace(value, 0);
        int end = SvgIriLexical.TrimEnd(value, start);
        if (start == end) return new SvgIriParser().Failure(SvgIriParseError.ExpectedValue, start);
        if (end - start > maxCharacters) return new SvgIriParser().Failure(SvgIriParseError.TokenTooLong, start + maxCharacters);
        int invalid = SvgIriLexical.ValidateIri(value, start, end);
        if (invalid >= 0)
        {
            SvgIriParseError error = value[invalid] == '%' ? SvgIriParseError.InvalidPercentEscape : SvgIriParseError.InvalidCharacter;
            return new SvgIriParser().Failure(error, invalid);
        }
        string text = value.Substring(start, end - start);
        return new SvgIriParser().Success(new SvgIriReference(text, false, value, start, end));
    }

    /// <summary>Parses a nonempty fragment-only reference.</summary>
    /// <param name="value">The fragment reference beginning with a number sign.</param>
    /// <param name="maxCharacters">The maximum trimmed reference length.</param>
    /// <returns>A fragment-only reference or stable failure.</returns>
    public SvgIriParseResult ParseFragment(string value, int maxCharacters)
    {
        SvgIriParseResult result = new SvgIriParser().ParseIri(value, maxCharacters);
        if (!result.Success) return result;
        if (!(result.Reference ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IsFragmentOnly || result.Reference.Fragment.Length == 0 || result.Reference.Fragment.IndexOf('#') >= 0)
        {
            return new SvgIriParser().Failure(SvgIriParseError.InvalidFragment, result.Reference.StartOffset);
        }
        return result;
    }

    /// <summary>Parses one complete CSS <c>url()</c> reference with CSS escapes.</summary>
    /// <param name="value">The URL function text.</param>
    /// <param name="maxCharacters">The maximum decoded URL length.</param>
    /// <returns>A decoded URL reference or stable failure.</returns>
    public SvgIriParseResult ParseUrl(string? value, int maxCharacters)
    {
        if (value == null || maxCharacters < 1) return new SvgIriParser().Failure(SvgIriParseError.InvalidArgument, 0);
        int contentMaximum = maxCharacters > 268435455 ? 2147483647 : maxCharacters * 8;
        SvgFunctionValueParseResult function = new SvgFunctionValueParser().Parse(value, 32, contentMaximum);
        if (!function.Success) return new SvgIriParser().Failure(function.Error, function.ErrorOffset);
        if (!string.Equals((function.Function ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Name, "url", System.StringComparison.OrdinalIgnoreCase)) return new SvgIriParser().Failure(SvgIriParseError.InvalidFunction, function.Function.StartOffset);
        SvgDecodedTextResult decoded = SvgIriLexical.DecodeUrlContent(function.Function.Content, function.Function.ContentOffset, maxCharacters);
        if (!decoded.Success) return new SvgIriParser().Failure(decoded.Error, decoded.ErrorOffset);
        int invalid = SvgIriLexical.ValidateDecodedUrl(decoded.Value);
        if (invalid >= 0)
        {
            SvgIriParseError error = decoded.Value[invalid] == '%' ? SvgIriParseError.InvalidPercentEscape : SvgIriParseError.InvalidCharacter;
            return new SvgIriParser().Failure(error, function.Function.ContentOffset + invalid);
        }
        SvgIriReference reference = new SvgIriReference(decoded.Value, true, value, function.Function.StartOffset, function.Function.EndOffset);
        return new SvgIriParser().Success(reference);
    }
SvgIriParseResult Success(SvgIriReference reference)
    {
        return new SvgIriParseResult(true, SvgIriParseError.None, reference, -1);
    }
SvgIriParseResult Failure(SvgIriParseError error, int offset)
    {
        return new SvgIriParseResult(false, error, null, offset);
    }
}
