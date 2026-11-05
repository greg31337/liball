using System;
using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents an assembled multipart/byteranges payload and its parts.
/// </summary>
public class HttpMultipartRange
{
    /// <summary>
    /// The multipart boundary without delimiter prefixes.
    /// </summary>
    public string Boundary = "";

    /// <summary>
    /// The range representations in encoded order.
    /// </summary>
    public List<HttpRepresentation> Parts = new List<HttpRepresentation>();

    /// <summary>
    /// The complete Content-Type field value.
    /// </summary>
    public string ContentType = "";

    /// <summary>
    /// The exact encoded payload length.
    /// </summary>
    public long EncodedLength;

    /// <summary>
    /// The owned encoded multipart payload.
    /// </summary>
    public byte[] EncodedBody = Array.Empty<byte>();
}
