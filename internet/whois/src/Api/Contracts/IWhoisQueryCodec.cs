namespace whois;

/// <summary>
/// Defines strict encoding and RFC 3912 framing of query text.
/// </summary>
public interface IWhoisQueryCodec
{
    /// <summary>
    /// Encodes the exact request query and appends one ASCII CRLF terminator.
    /// </summary>
    /// <param name="request">The exact exchange request.</param>
    /// <returns>The encoded and framed query payload.</returns>
    byte[] EncodeQuery(WhoisRequest request);
}
