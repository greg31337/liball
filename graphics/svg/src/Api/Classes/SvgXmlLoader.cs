namespace svg;

/// <summary>Constructs bounded tagged documents from validated XML tokens or caller-owned XML readers.</summary>
public class SvgXmlLoader : ISvgXmlLoader
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgXmlLoader()
    {
    }

    /// <summary>Builds a document from a validated and namespace-resolved token stream.</summary>
    /// <param name="source">The exact decoded source.</param>
    /// <param name="tokens">The ordered validated and namespace-resolved tokens.</param>
    /// <param name="options">The validated load options and tree limits.</param>
    /// <param name="sourceUri">The source URI text, or an empty string.</param>
    /// <returns>A complete document or a located construction failure.</returns>
    public SvgXmlBuildResult Build(string? source, System.Collections.Generic.List<SvgXmlToken>? tokens, SvgLoadOptions? options, string? sourceUri)
    {
        if (source == null || tokens == null || options == null || sourceUri == null || options.BaseUri == null || options.Limits == null || options.Limits.Tree == null)
        {
            return new SvgXmlBuildResult(false, SvgXmlBuildError.InvalidArgument, null, 0, 0, 0, "XML construction arguments are invalid.");
        }
        SvgSecurityLimitValidation limits = options.Limits.Validate();
        if (!limits.Success || options.DtdPolicy < SvgDtdPolicy.Prohibit || options.DtdPolicy > SvgDtdPolicy.PreserveOnly)
        {
            return new SvgXmlBuildResult(false, SvgXmlBuildError.InvalidArgument, null, 0, 0, 0, "XML construction options are invalid.");
        }
        SvgXmlBuildState state = new SvgXmlBuildState(source, tokens, options, sourceUri);
        return state.Build();
    }

    /// <summary>Builds and attaches a document to one validated tokenized load result.</summary>
    /// <param name="result">The validated tokenized load result.</param>
    /// <param name="options">The validated load options.</param>
    /// <returns>The same result after document construction.</returns>
    internal SvgLoadResult BuildLoadResult(SvgLoadResult result, SvgLoadOptions options)
    {
        if (!result.Success || result.DecodedCharacters == null) return result;
        SvgXmlBuildResult built = new SvgXmlLoader().Build(result.DecodedCharacters, result.XmlTokens, options, (result.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceUri);
        return new SvgXmlLoader().AttachBuildResult(result, built, options);
    }

    /// <summary>Rejects prohibited doctypes before semantic validation can inspect later references.</summary>
    /// <param name="result">The tokenized load result.</param>
    /// <param name="options">The validated load options.</param>
    /// <returns>A policy failure or the result of XML 1.0 validation.</returns>
    internal SvgLoadResult ValidateDtdPolicyLoadResult(SvgLoadResult result, SvgLoadOptions options)
    {
        if (!result.Success || options.DtdPolicy != SvgDtdPolicy.Prohibit) return new SvgXmlValidator().ValidateLoadResult(result, options);
        int index = 0;
        while (index < result.XmlTokens.Count)
        {
            SvgXmlToken token = result.XmlTokens[index];
            if (token.Kind == SvgXmlTokenKind.Doctype)
            {
                SvgXmlBuildResult prohibited = new SvgXmlBuildResult(false, SvgXmlBuildError.DtdProhibited, null, token.StartOffset, token.StartLine, token.StartColumn, "Document type declarations are prohibited by the configured DTD policy.");
                return new SvgXmlLoader().AttachBuildResult(result, prohibited, options);
            }
            index = index + 1;
        }
        return new SvgXmlValidator().ValidateLoadResult(result, options);
    }

    /// <summary>Consumes a caller-owned XML reader and attaches its bounded document.</summary>
    /// <param name="result">The staged XML reader load result.</param>
    /// <param name="options">The validated load options.</param>
    /// <returns>The same result after reader construction.</returns>
    internal SvgLoadResult BuildReaderLoadResult(SvgLoadResult result, SvgLoadOptions options)
    {
        if (!result.Success || result.Source == null || result.Source.XmlReader == null) return result;
        SvgXmlReaderBuildState state = new SvgXmlReaderBuildState(result.Source.XmlReader, options, result.Source.SourceUri);
        SvgXmlBuildResult built = state.Build();
        return new SvgXmlLoader().AttachBuildResult(result, built, options);
    }
SvgLoadResult AttachBuildResult(SvgLoadResult result, SvgXmlBuildResult built, SvgLoadOptions options)
    {
        if (built.Success)
        {
            int diagnosticIndex = 0;
            while (diagnosticIndex < result.Diagnostics.Count)
            {
                (built.Document ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Diagnostics.Add(result.Diagnostics[diagnosticIndex]);
                diagnosticIndex = diagnosticIndex + 1;
            }
            result.Document = built.Document;
            return SvgDocumentRecognition.Apply(result, options);
        }
        SvgDiagnosticCode code = new SvgXmlLoader().IsLimitError(built.Error) ? SvgDiagnosticCode.InputLimitExceeded : built.Error == SvgXmlBuildError.Cancelled ? SvgDiagnosticCode.Cancelled : SvgDiagnosticCode.MalformedXml;
        SvgSourceUri sourceUri = new SvgSourceUri((result.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceUri);
        SvgSourceSpan span = new SvgSourceSpan(sourceUri, built.ErrorOffset, built.ErrorLine, built.ErrorColumn, built.ErrorOffset, built.ErrorLine, built.ErrorColumn);
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, built.Message, span));
        result.Document = null;
        result.Success = false;
        return result;
    }
bool IsLimitError(SvgXmlBuildError error)
    {
        return error == SvgXmlBuildError.DepthLimitExceeded || error == SvgXmlBuildError.NodeLimitExceeded ||
            error == SvgXmlBuildError.ElementAttributeLimitExceeded || error == SvgXmlBuildError.TotalAttributeLimitExceeded ||
            error == SvgXmlBuildError.TextLimitExceeded || error == SvgXmlBuildError.NameLimitExceeded;
    }
}
