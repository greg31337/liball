namespace whois;

/// <summary>
/// Encodes WHOIS query text with strict caller-selected encoding and RFC 3912 framing.
/// </summary>
public class WhoisQueryCodec : IWhoisQueryCodec
{
    /// <summary>
    /// Initializes the built-in WHOIS query codec.
    /// </summary>
    public WhoisQueryCodec()
    {
    }

    /// <summary>
    /// Encodes the exact request query and appends one ASCII CRLF terminator.
    /// </summary>
    /// <param name="request">The exact exchange request.</param>
    /// <returns>The encoded and framed query payload.</returns>
    /// <exception cref="System.ArgumentNullException">The request or its encoding configuration is <c>null</c>.</exception>
    /// <exception cref="WhoisQueryException">The query text or resource limits are invalid, or the encoded query exceeds its byte limit.</exception>
    /// <exception cref="System.Text.EncoderFallbackException">The query contains text that the selected encoding cannot represent.</exception>
    public byte[] EncodeQuery(WhoisRequest request)
    {
        InputGuard.RequireReference(request, "request");
        InputGuard.RequireReference(request.Encoding, "request.Encoding");
        InputGuard.RequireReference(request.Encoding.RequestEncoding, "request.Encoding.RequestEncoding");
        InputGuard.ValidateQueryText(request.QueryText, WhoisQueryKind.FreeForm);
        InputGuard.ValidateLimits(request.Limits, request.QueryText, WhoisQueryKind.FreeForm);

        System.Text.Encoding encoding = System.Text.Encoding.GetEncoding(request.Encoding.RequestEncoding.CodePage, System.Text.EncoderFallback.ExceptionFallback, System.Text.DecoderFallback.ExceptionFallback);
        int byteCount = encoding.GetByteCount(request.QueryText);
        if (byteCount > request.Limits.MaximumEncodedQueryBytes)
        {
            throw InputGuard.CreateQueryError("The encoded query exceeds its byte limit.", "Limits.MaximumEncodedQueryBytes", request.QueryText, WhoisQueryKind.FreeForm, request.Endpoint);
        }
        if (byteCount > 2147483645)
        {
            throw InputGuard.CreateQueryError("The encoded query is too large to frame.", "Limits.MaximumEncodedQueryBytes", request.QueryText, WhoisQueryKind.FreeForm, request.Endpoint);
        }

        byte[] payload = new byte[byteCount + 2];
        encoding.GetBytes(request.QueryText, 0, request.QueryText.Length, payload, 0);
        payload[byteCount] = 13;
        payload[byteCount + 1] = 10;
        return payload;
    }
}
