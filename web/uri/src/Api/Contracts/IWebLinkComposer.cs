namespace uri;

/// <summary>
/// Composes validated Web Link models into bounded Link field values.
/// </summary>
public interface IWebLinkComposer
{
    /// <summary>Composes an ordered Web Link set or throws on failure.</summary>
    /// <param name="linkSet">The Web Link set to compose.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The composed Link field value.</returns>
    /// <exception cref="WebLinkSyntaxException">The model or a configured limit is invalid.</exception>
    string ComposeLinkFieldValue(
        WebLinkSet linkSet,
        OperationLimits limits);

    /// <summary>Composes a Web Link set without throwing for expected failure.</summary>
    /// <param name="linkSet">The Web Link set to compose.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The composition outcome.</returns>
    UriTextResult TryComposeLinkFieldValue(
        WebLinkSet linkSet,
        OperationLimits limits);

    /// <summary>Validates a Web Link set and measures its composed output length.</summary>
    /// <param name="linkSet">The Web Link set to measure.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The exact number of UTF-16 code units in the composed field value.</returns>
    /// <exception cref="WebLinkSyntaxException">The model or a configured limit is invalid.</exception>
    int MeasureLinkFieldValue(
        WebLinkSet linkSet,
        OperationLimits limits);
}
