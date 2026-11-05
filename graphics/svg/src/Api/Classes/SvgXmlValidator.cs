namespace svg;

/// <summary>Validates XML 1.0 lexical tokens and creates decoded normalized values without expanding declared entities.</summary>
public class SvgXmlValidator : ISvgXmlValidator
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgXmlValidator()
    {
    }

    /// <summary>Validates one tokenized source and populates decoded and normalized token fields.</summary>
    /// <param name="source">The exact decoded source used to produce the tokens.</param>
    /// <param name="tokens">The ordered source-preserving tokens.</param>
    /// <param name="maxNameCharacters">The maximum UTF-16 character count of one XML name.</param>
    /// <returns>A success result or the earliest located well-formedness failure.</returns>
    public SvgXmlValidationResult Validate(string? source, System.Collections.Generic.List<SvgXmlToken>? tokens, int maxNameCharacters)
    {
        if (source == null || tokens == null || maxNameCharacters < 1)
        {
            return new SvgXmlValidationResult(false, SvgXmlValidationError.InvalidArgument, 0, 0, 0, "XML validation arguments are invalid.");
        }
        SvgXmlValidationState state = new SvgXmlValidationState(source, tokens, maxNameCharacters);
        return state.Validate();
    }

    /// <summary>Validates a tokenized load result and attaches a located diagnostic on failure.</summary>
    /// <param name="result">The tokenized load result.</param>
    /// <param name="options">The validated load options.</param>
    /// <returns>The same load result after XML 1.0 validation.</returns>
    internal SvgLoadResult ValidateLoadResult(SvgLoadResult result, SvgLoadOptions options)
    {
        if (!result.Success || result.DecodedCharacters == null) return result;
        SvgXmlValidationResult validation = new SvgXmlValidator().Validate(result.DecodedCharacters, result.XmlTokens, options.Limits.Tree.MaxNameCharacters);
        if (validation.Success) return new SvgXmlNamespaceResolver().ResolveLoadResult(result, options);
        SvgDiagnosticCode code = validation.Error == SvgXmlValidationError.NameTooLong ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.MalformedXml;
        SvgSourceUri sourceUri = new SvgSourceUri((result.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceUri);
        SvgSourceSpan span = new SvgSourceSpan(sourceUri, validation.ErrorOffset, validation.ErrorLine, validation.ErrorColumn, validation.ErrorOffset, validation.ErrorLine, validation.ErrorColumn);
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, validation.Message, span));
        result.Success = false;
        return result;
    }
}
