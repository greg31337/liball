using System.Collections.Generic;

namespace http;

/// <summary>
/// Parses, normalizes, constructs, and parses HTTP byte-range representations.
/// </summary>
public class HttpRangeService : IHttpRangeService
{
    /// <summary>Creates an HTTP range service.</summary>
    public HttpRangeService()
    {
    }

    /// <summary>Parses a complete Range field value.</summary>
    /// <param name="input">The field value octets.</param>
    /// <param name="limits">The parsing limits.</param>
    /// <returns>The parsed range request.</returns>
    /// <exception cref="HttpParseException">The field value is malformed.</exception>
    /// <exception cref="HttpLimitExceededException">A parsing limit is exceeded.</exception>
    public HttpRangeRequest ParseRangeRequest(byte[] input, HttpLimits limits)
    {
        if (limits == null || limits.MaximumFieldValueSize < 0 ||
            limits.MaximumRangeCount < 0)
        {
            throw new HttpProtocolException(
                "Range parsing limits are incomplete or invalid.",
                "invalid-range-limits");
        }

        RangeValueParseResult parsed = FieldValueParser.ParseRangeValue(
            input,
            limits.MaximumFieldValueSize,
            limits.MaximumRangeCount);
        if (parsed.IsLimitExceeded)
        {
            bool sizeExceeded = input.Length >
                limits.MaximumFieldValueSize;
            string limitName = sizeExceeded ?
                "maximum-field-value-size" : "maximum-range-count";
            long limit = sizeExceeded ?
                limits.MaximumFieldValueSize : limits.MaximumRangeCount;
            long actual = (long)limits.MaximumRangeCount + 1;
            if (sizeExceeded)
            {
                actual = input.Length;
            }
            throw new HttpLimitExceededException(
                "Range parsing exceeded a configured limit.",
                limitName,
                limit,
                actual);
        }

        if (parsed.Status != HttpOperationStatus.Done)
        {
            throw new HttpParseException(
                "The Range field value is malformed.",
                "invalid-range-value",
                parsed.ErrorPosition);
        }

        parsed.Range.Limits = limits;
        return parsed.Range;
    }

    /// <summary>Normalizes byte ranges against a representation length.</summary>
    /// <param name="request">The parsed range request.</param>
    /// <param name="representationLength">The selected representation length.</param>
    /// <returns>The normalized range decision and Content-Range metadata.</returns>
    public HttpRangeDecision NormalizeByteRanges(
        HttpRangeRequest request,
        long representationLength)
    {
        return RangeSemantics.NormalizeByteRanges(request, representationLength);
    }

    /// <summary>Builds Content-Range metadata for one normalized range.</summary>
    /// <param name="range">The normalized range.</param>
    /// <param name="representationLength">The complete representation length.</param>
    /// <returns>The Content-Range metadata.</returns>
    /// <exception cref="HttpProtocolException">The range or length is invalid.</exception>
    public HttpContentRange BuildContentRange(
        HttpByteRange range,
        long representationLength)
    {
        return RangeSemantics.BuildContentRange(range, representationLength);
    }

    /// <summary>Builds a multipart/byteranges payload.</summary>
    /// <param name="representation">The selected representation.</param>
    /// <param name="ranges">The normalized ranges.</param>
    /// <param name="boundary">A boundary, or an empty string to generate one.</param>
    /// <returns>The assembled multipart payload and its parts.</returns>
    /// <exception cref="HttpProtocolException">The representation, ranges, or boundary is invalid.</exception>
    /// <exception cref="HttpFieldException">The representation media type is invalid.</exception>
    /// <exception cref="HttpLimitExceededException">The encoded payload is too large.</exception>
    public HttpMultipartRange BuildMultipartRanges(
        HttpRepresentation representation,
        List<HttpByteRange> ranges,
        string boundary)
    {
        return MultipartRangeCodec.Build(representation, ranges, boundary);
    }

    /// <summary>Parses a multipart/byteranges payload.</summary>
    /// <param name="content">The multipart content bytes.</param>
    /// <param name="boundary">The multipart boundary.</param>
    /// <param name="limits">The parsing limits.</param>
    /// <returns>The parsed multipart payload and its parts.</returns>
    /// <exception cref="HttpParseException">The multipart payload is malformed.</exception>
    /// <exception cref="HttpProtocolException">The boundary or limits are invalid.</exception>
    /// <exception cref="HttpLimitExceededException">A parsing limit is exceeded.</exception>
    public HttpMultipartRange ParseMultipartRanges(
        byte[] content,
        string boundary,
        HttpLimits limits)
    {
        return MultipartRangeCodec.Parse(content, boundary, limits);
    }
}
