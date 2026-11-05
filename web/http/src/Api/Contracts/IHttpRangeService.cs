using System.Collections.Generic;

namespace http;

/// <summary>
/// Parses and evaluates HTTP range requests.
/// </summary>
public interface IHttpRangeService
{
    /// <summary>Parses a Range field value.</summary>
    /// <param name="input">The field value octets.</param>
    /// <param name="limits">The parsing limits.</param>
    /// <returns>The parsed range request.</returns>
    HttpRangeRequest ParseRangeRequest(byte[] input, HttpLimits limits);

    /// <summary>
    /// Normalizes the ranges requested by an HTTP request.
    /// </summary>
    /// <param name="request">The parsed range request.</param>
    /// <param name="representationLength">The selected representation length.</param>
    /// <returns>The normalized range decision.</returns>
    HttpRangeDecision NormalizeByteRanges(HttpRangeRequest request, long representationLength);

    /// <summary>Builds Content-Range metadata for one normalized range.</summary>
    /// <param name="range">The normalized range.</param>
    /// <param name="representationLength">The complete representation length.</param>
    /// <returns>The Content-Range metadata.</returns>
    HttpContentRange BuildContentRange(HttpByteRange range, long representationLength);

    /// <summary>Builds a multipart/byteranges payload.</summary>
    /// <param name="representation">The selected representation.</param>
    /// <param name="ranges">The normalized ranges.</param>
    /// <param name="boundary">The validated multipart boundary.</param>
    /// <returns>The assembled multipart payload.</returns>
    HttpMultipartRange BuildMultipartRanges(
        HttpRepresentation representation,
        List<HttpByteRange> ranges,
        string boundary);

    /// <summary>Parses a multipart/byteranges payload.</summary>
    /// <param name="content">The multipart content bytes.</param>
    /// <param name="boundary">The multipart boundary.</param>
    /// <param name="limits">The parsing limits.</param>
    /// <returns>The parsed multipart payload.</returns>
    HttpMultipartRange ParseMultipartRanges(byte[] content, string boundary, HttpLimits limits);
}
