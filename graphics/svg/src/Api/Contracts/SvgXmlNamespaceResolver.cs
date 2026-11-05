namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgXmlNamespaceResolver"/>.</summary>
public interface ISvgXmlNamespaceResolver
{
    /// <summary>Processes namespace scopes for one validated source-preserving token stream.</summary>
    /// <param name="source">The exact decoded XML source.</param>
    /// <param name="tokens">The ordered validated XML tokens.</param>
    /// <returns>A success result or the earliest located namespace failure.</returns>
    SvgXmlNamespaceResult Resolve(string? source, System.Collections.Generic.List<SvgXmlToken>? tokens);
}
