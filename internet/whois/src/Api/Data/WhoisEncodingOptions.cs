namespace whois;

/// <summary>
/// Contains request encoding and response decoding behavior for a WHOIS exchange.
/// </summary>
public class WhoisEncodingOptions
{
    /// <summary>
    /// Contains the strict encoding used for request text before CRLF framing.
    /// </summary>
    public System.Text.Encoding RequestEncoding = System.Text.Encoding.GetEncoding(20127, System.Text.EncoderFallback.ExceptionFallback, System.Text.DecoderFallback.ExceptionFallback);

    /// <summary>
    /// Contains an explicit response encoding, or <c>null</c> to detect the encoding.
    /// </summary>
    public System.Text.Encoding? ResponseEncoding = null;

    /// <summary>
    /// Controls whether a Unicode byte-order mark is detected before UTF-8 decoding.
    /// </summary>
    public bool DetectByteOrderMark = true;

    /// <summary>
    /// Controls how invalid response bytes are decoded.
    /// </summary>
    public WhoisDecodingMode DecodingMode = WhoisDecodingMode.Strict;
}
