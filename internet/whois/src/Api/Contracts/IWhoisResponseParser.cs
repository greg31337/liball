namespace whois;

/// <summary>
/// Defines generic ordered WHOIS response parsing and field lookup.
/// </summary>
public interface IWhoisResponseParser
{
    /// <summary>
    /// Parses decoded text into ordered lines, fields, and generic records.
    /// </summary>
    /// <param name="response">The decoded response view.</param>
    /// <param name="limits">The parser resource limits.</param>
    /// <returns>The status-bearing generic parsed response.</returns>
    WhoisParsedResponse ParseResponse(WhoisTextResponse response, WhoisLimits limits);

    /// <summary>
    /// Finds every field with a case-insensitively matching canonical name.
    /// </summary>
    /// <param name="response">The parsed response to search.</param>
    /// <param name="name">The field name to match.</param>
    /// <returns>All matching fields in source order.</returns>
    WhoisField[] FindFields(WhoisParsedResponse response, string name);
}
