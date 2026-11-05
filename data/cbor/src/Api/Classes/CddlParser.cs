namespace cbor;

/// <summary>Parses CDDL source text into public source documents.</summary>
public class CddlParser : ICddlParser
{
    /// <summary>Creates a CDDL parser.</summary>
    public CddlParser()
    {
    }

    /// <summary>Parses a complete CDDL document or throws on failure.</summary>
    /// <param name="source">The CDDL source text.</param>
    /// <param name="options">The parsing policies and limits.</param>
    /// <returns>The parsed document.</returns>
    public CddlDocument ParseDocument(string source, CddlParseOptions options)
    {
        CddlParseResult result = TryParseDocument(source, options);
        CddlDocument? document = result.Document;
        if (!result.IsSuccess || document == null)
        {
            if (result.Diagnostics.Length > 0)
            {
                CddlDiagnostic diagnostic = result.Diagnostics[0];
                throw new CddlParseException(
                    diagnostic.Code,
                    diagnostic.Message,
                    diagnostic.Span
                );
            }
            throw new CddlParseException(
                "CDDL_PARSE_FAILED",
                "The CDDL document could not be parsed.",
                null
            );
        }
        return document;
    }

    /// <summary>Parses a CDDL document and returns recoverable diagnostics.</summary>
    /// <param name="source">The CDDL source text.</param>
    /// <param name="options">The parsing policies and limits.</param>
    /// <returns>The exception-less parse result.</returns>
    public CddlParseResult TryParseDocument(string source, CddlParseOptions options)
    {
        if (options == null)
        {
            CddlParseResult missingOptions = new CddlParseResult();
            CddlDiagnostic diagnostic = new CddlDiagnostic();
            diagnostic.Code = "CDDL_PARSE_OPTIONS_MISSING";
            diagnostic.Message = "CDDL parsing options are required.";
            missingOptions.Diagnostics = [diagnostic];
            return missingOptions;
        }
        if (source == null)
        {
            CddlParseResult missingSource = new CddlParseResult();
            CddlDiagnostic diagnostic = new CddlDiagnostic();
            diagnostic.Code = "CDDL_PARSE_SOURCE_MISSING";
            diagnostic.Message = "CDDL source text is required.";
            diagnostic.Span.SourceName = options.SourceName;
            missingSource.Diagnostics = [diagnostic];
            missingSource.Options = options;
            return missingSource;
        }
        CddlDocumentParser parser = new CddlDocumentParser(source, options);
        return parser.ParseSource();
    }
}
