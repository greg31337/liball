namespace uri;

/// <summary>
/// Provides validated and bounded Web Link field composition.
/// </summary>
public class WebLinkComposer : IWebLinkComposer
{
    /// <summary>Initializes a stateless Web Link composer.</summary>
    public WebLinkComposer()
    {
    }

    /// <summary>Composes an ordered Web Link set or throws on failure.</summary>
    /// <param name="linkSet">The Web Link set to compose.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The composed Link field value.</returns>
    /// <exception cref="WebLinkSyntaxException">The model or a configured limit is invalid.</exception>
    public string ComposeLinkFieldValue(
        WebLinkSet linkSet,
        OperationLimits limits)
    {
        UriTextResult result = WebLinkComposerCore.ComposeLinkFieldValue(
            linkSet,
            limits);
        if (!result.Success)
        {
            throw CreateSyntaxException(result.Diagnostic);
        }

        return result.Text;
    }

    /// <summary>Composes a Web Link set without throwing for expected failure.</summary>
    /// <param name="linkSet">The Web Link set to compose.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The composition outcome.</returns>
    public UriTextResult TryComposeLinkFieldValue(
        WebLinkSet linkSet,
        OperationLimits limits)
    {
        return WebLinkComposerCore.ComposeLinkFieldValue(linkSet, limits);
    }

    /// <summary>Validates a Web Link set and measures its composed output length.</summary>
    /// <param name="linkSet">The Web Link set to measure.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The exact number of UTF-16 code units in the composed field value.</returns>
    /// <exception cref="WebLinkSyntaxException">The model or a configured limit is invalid.</exception>
    public int MeasureLinkFieldValue(
        WebLinkSet linkSet,
        OperationLimits limits)
    {
        UriMeasureResult result = WebLinkComposerCore.MeasureLinkFieldValue(
            linkSet,
            limits);
        if (!result.Success)
        {
            throw CreateSyntaxException(result.Diagnostic);
        }

        return result.Length;
    }

    internal WebLinkSyntaxException CreateSyntaxException(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.WebLink);
        return new WebLinkSyntaxException(failure);
    }
}
