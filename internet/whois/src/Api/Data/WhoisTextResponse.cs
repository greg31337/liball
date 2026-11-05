namespace whois;

/// <summary>
/// Contains a decoded view derived from an immutable raw WHOIS response.
/// </summary>
public class WhoisTextResponse
{
    /// <summary>
    /// Contains the raw response from which this text was derived.
    /// </summary>
    public WhoisRawResponse RawResponse = new WhoisRawResponse();

    /// <summary>
    /// Contains the decoded response text with original characters and newlines preserved.
    /// </summary>
    public string Text = string.Empty;

    /// <summary>
    /// Contains the selected encoding name.
    /// </summary>
    public string EncodingName = string.Empty;

    /// <summary>
    /// Indicates whether a byte-order mark selected the response encoding.
    /// </summary>
    public bool ByteOrderMarkDetected = false;

    /// <summary>
    /// Indicates whether decoding used a configured fallback encoding.
    /// </summary>
    public bool FallbackUsed = false;

    /// <summary>
    /// Indicates whether invalid byte sequences were replaced.
    /// </summary>
    public bool ReplacementUsed = false;

    /// <summary>
    /// Indicates whether response decoding completed successfully.
    /// </summary>
    public bool IsDecoded = false;

    /// <summary>
    /// Contains the decoding error, or <c>null</c> when none occurred.
    /// </summary>
    public System.Exception? Error = null;
}
