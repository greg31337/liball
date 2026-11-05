namespace whois;

/// <summary>
/// Contains the ordered generic model derived from a decoded WHOIS response.
/// </summary>
public class WhoisParsedResponse
{
    /// <summary>
    /// Contains the decoded response from which this model was derived.
    /// </summary>
    public WhoisTextResponse TextResponse = new WhoisTextResponse();

    /// <summary>
    /// Contains all derived lines in source order.
    /// </summary>
    public WhoisLine[] Lines = System.Array.Empty<WhoisLine>();

    /// <summary>
    /// Contains all parsed fields in source order.
    /// </summary>
    public WhoisField[] Fields = System.Array.Empty<WhoisField>();

    /// <summary>
    /// Contains all generic records in source order.
    /// </summary>
    public WhoisRecord[] Records = System.Array.Empty<WhoisRecord>();

    /// <summary>
    /// Indicates whether generic parsing completed successfully.
    /// </summary>
    public bool IsParsed = false;

    /// <summary>
    /// Contains the parsing error, or <c>null</c> when none occurred.
    /// </summary>
    public System.Exception? Error = null;
}
