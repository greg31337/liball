namespace svg;

/// <summary>Tokenizes bounded decoded XML while retaining exact raw source slices.</summary>
public class SvgXmlTokenizer : ISvgXmlTokenizer
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgXmlTokenizer()
    {
    }

    /// <summary>Tokenizes one complete decoded XML source.</summary>
    /// <param name="value">The decoded XML characters.</param>
    /// <param name="maxTokens">The maximum combined top-level, attribute, and nested-reference token count.</param>
    /// <param name="maxTokenCharacters">The maximum raw token character count.</param>
    /// <returns>A complete token stream or valid prefix and located failure.</returns>
    public SvgXmlTokenizeResult Tokenize(string? value, int maxTokens, int maxTokenCharacters)
    {
        if (value == null || maxTokens < 1 || maxTokenCharacters < 1)
        {
            return new SvgXmlTokenizeResult(false, SvgXmlTokenizeError.InvalidArgument, new System.Collections.Generic.List<SvgXmlToken>(), 0, 0, 0, "Tokenizer arguments are invalid.");
        }
        SvgXmlTokenizerState state = new SvgXmlTokenizerState(value, maxTokens, maxTokenCharacters);
        return state.Tokenize();
    }

    /// <summary>Attaches XML tokens and a located diagnostic to a decoded load result.</summary>
    /// <param name="result">The container-decoded load result.</param>
    /// <param name="options">The validated load options.</param>
    /// <returns>The same result after lexical tokenization.</returns>
    internal SvgLoadResult TokenizeLoadResult(SvgLoadResult result, SvgLoadOptions options)
    {
        if (result.Success && result.Source != null && result.Source.Kind == SvgLoadSourceKind.XmlReader) return new SvgXmlLoader().BuildReaderLoadResult(result, options);
        if (!result.Success || result.DecodedCharacters == null) return result;
        SvgXmlTokenizeResult tokenized = new SvgXmlTokenizer().Tokenize(result.DecodedCharacters, options.Limits.Tree.MaxNodes, options.Limits.Input.MaxXmlTokenCharacters);
        result.XmlTokens = tokenized.Tokens;
        if (tokenized.Success) return new SvgXmlLoader().ValidateDtdPolicyLoadResult(result, options);
        SvgDiagnosticCode code = tokenized.Error == SvgXmlTokenizeError.TokenLimitExceeded || tokenized.Error == SvgXmlTokenizeError.TokenTooLong ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.MalformedXml;
        SvgSourceUri sourceUri = new SvgSourceUri((result.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceUri);
        SvgSourceSpan span = new SvgSourceSpan(sourceUri, tokenized.ErrorOffset, tokenized.ErrorLine, tokenized.ErrorColumn, tokenized.ErrorOffset, tokenized.ErrorLine, tokenized.ErrorColumn);
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, tokenized.Message, span));
        result.Success = false;
        return result;
    }
}
