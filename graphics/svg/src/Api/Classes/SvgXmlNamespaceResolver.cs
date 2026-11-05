namespace svg;

/// <summary>Resolves Namespaces in XML 1.0 scopes and assigns expanded names to validated lexical tokens.</summary>
public class SvgXmlNamespaceResolver : ISvgXmlNamespaceResolver
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgXmlNamespaceResolver()
    {
    }

    /// <summary>Processes namespace scopes for one validated source-preserving token stream.</summary>
    /// <param name="source">The exact decoded XML source.</param>
    /// <param name="tokens">The ordered validated XML tokens.</param>
    /// <returns>A success result or the earliest located namespace failure.</returns>
    public SvgXmlNamespaceResult Resolve(string? source, System.Collections.Generic.List<SvgXmlToken>? tokens)
    {
        if (source == null || tokens == null)
        {
            return new SvgXmlNamespaceResult(false, SvgXmlNamespaceError.InvalidArgument, 0, 0, 0, "XML namespace arguments are invalid.");
        }
        SvgXmlNamespaceState state = new SvgXmlNamespaceState(source, tokens);
        return state.Resolve();
    }

    /// <summary>Resolves a validated load result and attaches a located namespace diagnostic on failure.</summary>
    /// <param name="result">The validated load result.</param>
    /// <param name="options">The validated load options.</param>
    /// <returns>The same result after namespace processing.</returns>
    internal SvgLoadResult ResolveLoadResult(SvgLoadResult result, SvgLoadOptions options)
    {
        if (!result.Success || result.DecodedCharacters == null) return result;
        SvgXmlNamespaceResult resolved = new SvgXmlNamespaceResolver().Resolve(result.DecodedCharacters, result.XmlTokens);
        if (resolved.Success) return new SvgXmlLoader().BuildLoadResult(result, options);
        SvgSourceUri sourceUri = new SvgSourceUri((result.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceUri);
        SvgSourceSpan span = new SvgSourceSpan(sourceUri, resolved.ErrorOffset, resolved.ErrorLine, resolved.ErrorColumn, resolved.ErrorOffset, resolved.ErrorLine, resolved.ErrorColumn);
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.XmlNamespace, resolved.Message, span));
        result.Success = false;
        return result;
    }
}
