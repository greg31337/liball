namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgXmlLoader"/>.</summary>
public interface ISvgXmlLoader
{
    /// <summary>Builds a document from a validated and namespace-resolved token stream.</summary>
    /// <param name="source">The exact decoded source.</param>
    /// <param name="tokens">The ordered validated and namespace-resolved tokens.</param>
    /// <param name="options">The validated load options and tree limits.</param>
    /// <param name="sourceUri">The source URI text, or an empty string.</param>
    /// <returns>A complete document or a located construction failure.</returns>
    SvgXmlBuildResult Build(string? source, System.Collections.Generic.List<SvgXmlToken>? tokens, SvgLoadOptions? options, string? sourceUri);
}
