using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace http;

internal static class CacheResponseSemantics
{
    internal static HttpCacheDecision Build(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        ValidateContexts(requestContext, responseContext);
        HttpCacheDecision reuse = CacheReuseSemantics.Evaluate(
            requestContext,
            responseContext);
        HttpCacheDecision result = CreateDecision(
            reuse,
            requestContext,
            responseContext);
        if (reuse.Outcome != HttpCacheOutcome.ServeFresh &&
            reuse.Outcome != HttpCacheOutcome.ServeStale)
        {
            result.Response = null;
            return result;
        }

        HttpRequest request = GetRequest(requestContext);
        HttpResponse stored = GetResponse(responseContext);
        ValidateRequest(request);
        ValidateResponse(stored);
        HttpRepresentation representation =
            RepresentationSemantics.ExtractFromResponse(stored);
        HttpPreconditionContext preconditionContext = BuildPreconditionContext(
            request,
            responseContext,
            representation);
        HttpPreconditionDecision precondition =
            PreconditionSemantics.EvaluatePreconditions(
                request,
                preconditionContext);
        result.PreconditionDecision = precondition;
        if (precondition.Outcome == HttpPreconditionOutcome.Rejected ||
            precondition.Outcome == HttpPreconditionOutcome.PreconditionFailed)
        {
            result.Outcome = HttpCacheOutcome.Unusable;
            result.Response = null;
            result.Reason = "The cached response cannot satisfy the request preconditions.";
            return result;
        }

        HttpResponse response = CopyResponseWithoutHopFields(stored);
        SetAge(response, reuse.CurrentAge);
        if (precondition.Outcome == HttpPreconditionOutcome.NotModified)
        {
            ConvertToNotModified(response);
            result.Response = response;
            result.Reason = "The cached representation satisfies a conditional request with 304.";
            return result;
        }

        if (precondition.ApplyRange)
        {
            if (!responseContext.IsComplete || !stored.IsComplete)
            {
                result.Outcome = HttpCacheOutcome.Validate;
                result.RequiresValidation = true;
                result.Response = null;
                result.Reason =
                    "A range cannot be constructed from incomplete stored content.";
                return result;
            }

            HttpRangeDecision range = EvaluateRange(request, representation.Content.LongLength);
            result.RangeDecision = range;
            if (range.Outcome == HttpRangeOutcome.Satisfiable)
            {
                ApplySatisfiableRange(response, representation, range);
            }
            else if (range.Outcome == HttpRangeOutcome.Unsatisfiable)
            {
                ApplyUnsatisfiedRange(response, range);
            }
        }

        if (request.Method == HttpMethods.Head)
        {
            response.Content = Array.Empty<byte>();
            response.Trailers = new HttpHeaders();
        }

        result.Response = response;
        result.Reason = response.StatusCode == HttpStatusCodes.PartialContent ?
            "A partial response was constructed from complete cached content." :
            response.StatusCode == HttpStatusCodes.RangeNotSatisfiable ?
            "The cached representation does not overlap the requested range." :
            "The cached response was constructed for the presented request.";
        return result;
    }

    static HttpCacheDecision CreateDecision(
        HttpCacheDecision reuse,
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        HttpCacheDecision result = new HttpCacheDecision();
        result.Outcome = reuse.Outcome;
        result.RequestContext = requestContext;
        result.ResponseContext = responseContext;
        result.HasCurrentAge = reuse.HasCurrentAge;
        result.CurrentAge = reuse.CurrentAge;
        result.HasFreshnessLifetime = reuse.HasFreshnessLifetime;
        result.FreshnessLifetime = reuse.FreshnessLifetime;
        result.IsHeuristicFreshness = reuse.IsHeuristicFreshness;
        result.RequiresValidation = reuse.RequiresValidation;
        result.CanServeStale = reuse.CanServeStale;
        result.PrimaryKey = reuse.PrimaryKey;
        result.SecondaryKey = reuse.SecondaryKey;
        result.Reason = reuse.Reason;
        result.ValidationResult = responseContext.ValidationResult;
        result.CollapsedRequestCount = requestContext.CollapsedRequestCount;
        result.WasCollapsed = requestContext.CollapsedRequestCount > 1;
        result.WasRevalidated = responseContext.ValidationResult != null &&
            responseContext.ValidationResult.WasMerged;
        return result;
    }

    static HttpPreconditionContext BuildPreconditionContext(
        HttpRequest request,
        HttpCacheResponseContext responseContext,
        HttpRepresentation representation)
    {
        HttpPreconditionContext result = new HttpPreconditionContext();
        result.Method = request.Method;
        result.IsCacheRecipient = true;
        result.RepresentationExists = true;
        result.EntityTag = representation.EntityTag;
        result.HasLastModified = responseContext.HasLastModified ||
            representation.HasLastModified;
        result.LastModified = responseContext.HasLastModified ?
            responseContext.LastModified : representation.LastModified;
        result.HasDate = responseContext.HasDate;
        result.Date = responseContext.Date;
        if (!result.HasDate)
        {
            ReadDateFromResponse(GetResponse(responseContext), result);
        }

        return result;
    }

    static void ReadDateFromResponse(
        HttpResponse response,
        HttpPreconditionContext context)
    {
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(
            response.Headers,
            HttpHeaderNames.Date);
        if (fields.Count == 0)
        {
            return;
        }

        if (fields.Count != 1)
        {
            throw CreateProtocolError(
                "invalid-cached-date",
                "The stored response contains multiple Date fields.");
        }

        byte[] value = TrimOptionalWhitespace(fields[0].Value);
        HttpDateParseResult parsed = HttpDateCodec.ParseHttpDate(value, value.Length);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            throw CreateProtocolError(
                "invalid-cached-date",
                "The stored response Date field is malformed.");
        }

        context.HasDate = true;
        context.Date = parsed.Value;
    }

    static HttpRangeDecision EvaluateRange(
        HttpRequest request,
        long representationLength)
    {
        HttpLimits limits = new HttpLimits();
        byte[] combined = HeaderStore.CombineMatchingValues(
            request.Headers,
            HttpHeaderNames.Range);
        RangeValueParseResult parsed = FieldValueParser.ParseRangeValue(
            combined,
            limits.MaximumFieldValueSize,
            limits.MaximumRangeCount);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            HttpRangeDecision rejected = new HttpRangeDecision();
            rejected.Outcome = parsed.IsLimitExceeded ?
                HttpRangeOutcome.Ignored : HttpRangeOutcome.Malformed;
            rejected.Reason = parsed.IsLimitExceeded ?
                "The Range field exceeds the cache response limits." :
                "The Range field is malformed and was ignored.";
            return rejected;
        }

        parsed.Range.Limits = limits;
        return RangeSemantics.NormalizeByteRanges(
            parsed.Range,
            representationLength);
    }

    static void ApplySatisfiableRange(
        HttpResponse response,
        HttpRepresentation representation,
        HttpRangeDecision range)
    {
        response.StatusCode = HttpStatusCodes.PartialContent;
        SetReasonPhrase(response, HttpReasonPhrases.PartialContent);
        response.Trailers = new HttpHeaders();
        if (range.Ranges.Count == 1)
        {
            HttpByteRange selected = range.Ranges[0];
            int length = (int)(selected.End - selected.Start + 1);
            byte[] content = new byte[length];
            Array.Copy(
                representation.Content,
                (int)selected.Start,
                content,
                0,
                length);
            response.Content = content;
            SetContentRange(response, range.ContentRanges[0]);
            SetContentLength(response, content.LongLength);
            return;
        }

        HttpMultipartRange multipart = MultipartRangeCodec.Build(
            representation,
            range.Ranges,
            "");
        range.Multipart = multipart;
        response.Content = multipart.EncodedBody;
        HeaderStore.RemoveMatchingFields(response.Headers, HttpHeaderNames.ContentRange);
        SetTextField(
            response.Headers,
            HttpHeaderNames.ContentType,
            multipart.ContentType);
        SetContentLength(response, multipart.EncodedLength);
    }

    static void ApplyUnsatisfiedRange(
        HttpResponse response,
        HttpRangeDecision range)
    {
        response.StatusCode = HttpStatusCodes.RangeNotSatisfiable;
        SetReasonPhrase(response, HttpReasonPhrases.RangeNotSatisfiable);
        response.Content = Array.Empty<byte>();
        response.Trailers = new HttpHeaders();
        SetContentRange(response, range.ContentRanges[0]);
        SetContentLength(response, 0);
    }

    static void SetContentRange(
        HttpResponse response,
        HttpContentRange contentRange)
    {
        FieldValueWriteResult written = FieldValueWriter.SerializeContentRange(
            contentRange,
            int.MaxValue);
        if (written.Status != HttpOperationStatus.Done)
        {
            throw CreateProtocolError(
                "invalid-cached-content-range",
                "The cached Content-Range metadata cannot be serialized.");
        }

        SetBytesField(
            response.Headers,
            HttpHeaderNames.ContentRange,
            written.Bytes);
    }

    static void SetContentLength(HttpResponse response, long length)
    {
        SetTextField(
            response.Headers,
            HttpHeaderNames.ContentLength,
            length.ToString(CultureInfo.InvariantCulture));
    }

    static void SetAge(HttpResponse response, long age)
    {
        if (age < 0)
        {
            throw CreateProtocolError(
                "invalid-cached-age",
                "The calculated cached response age cannot be negative.");
        }

        SetTextField(
            response.Headers,
            HttpHeaderNames.Age,
            age.ToString(CultureInfo.InvariantCulture));
    }

    static void ConvertToNotModified(HttpResponse response)
    {
        response.StatusCode = HttpStatusCodes.NotModified;
        SetReasonPhrase(response, HttpReasonPhrases.NotModified);
        response.Content = Array.Empty<byte>();
        response.Trailers = new HttpHeaders();
        HeaderStore.RemoveMatchingFields(
            response.Headers,
            HttpHeaderNames.ContentRange);
        HeaderStore.RemoveMatchingFields(
            response.Headers,
            HttpHeaderNames.TransferEncoding);
    }

    static void SetReasonPhrase(HttpResponse response, string value)
    {
        response.HasReasonPhrase = true;
        response.ReasonPhrase = Encoding.ASCII.GetBytes(value);
    }

    static void SetTextField(HttpHeaders headers, string name, string value)
    {
        SetBytesField(headers, name, Encoding.ASCII.GetBytes(value));
    }

    static void SetBytesField(HttpHeaders headers, string name, byte[] value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = value;
        HeaderStore.ReplaceFields(headers, field);
    }

    static HttpResponse CopyResponseWithoutHopFields(HttpResponse source)
    {
        HttpHeaders hopFields = IntermediarySemantics.DetermineHopFields(
            source.Headers);
        HttpResponse result = new HttpResponse();
        result.ProtocolVersion = source.ProtocolVersion;
        result.StatusCode = source.StatusCode;
        result.HasReasonPhrase = source.HasReasonPhrase;
        result.ReasonPhrase = CopyBytes(source.ReasonPhrase);
        result.Headers = CopyHeadersWithoutNames(source.Headers, hopFields);
        result.Content = CopyBytes(source.Content);
        result.Trailers = CopyHeadersWithoutNames(source.Trailers, hopFields);
        result.IsInformational = source.IsInformational;
        result.IsComplete = source.IsComplete;
        return result;
    }

    static HttpHeaders CopyHeadersWithoutNames(
        HttpHeaders source,
        HttpHeaders excluded)
    {
        HttpHeaders result = new HttpHeaders();
        int index = 0;
        while (index < source.Fields.Count)
        {
            HttpHeaderField field = source.Fields[index];
            if (HeaderStore.FindMatchingFields(excluded, field.Name).Count == 0)
            {
                result.Fields.Add(CopyField(field));
            }

            index = index + 1;
        }

        return result;
    }

    static HttpHeaderField CopyField(HttpHeaderField source)
    {
        HttpHeaderField result = new HttpHeaderField();
        result.Name = source.Name;
        result.Value = CopyBytes(source.Value);
        result.IsSensitive = source.IsSensitive;
        result.NeverIndex = source.NeverIndex;
        return result;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        return result;
    }

    static byte[] TrimOptionalWhitespace(byte[] value)
    {
        int start = 0;
        while (start < value.Length &&
            (value[start] == 0x20 || value[start] == 0x09))
        {
            start = start + 1;
        }

        int end = value.Length;
        while (end > start &&
            (value[end - 1] == 0x20 || value[end - 1] == 0x09))
        {
            end = end - 1;
        }

        byte[] result = new byte[end - start];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = value[start + index];
            index = index + 1;
        }

        return result;
    }

    static void ValidateContexts(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        if (requestContext == null || responseContext == null ||
            requestContext.Request == null || requestContext.CacheControl == null ||
            requestContext.CollapsedRequestCount < 1)
        {
            throw CreateProtocolError(
                "invalid-cached-response-context",
                "The cached response context is incomplete or invalid.");
        }
    }

    static void ValidateRequest(HttpRequest request)
    {
        if (request.Method == null || request.Headers == null ||
            request.Headers.Fields == null || request.Content == null ||
            request.Trailers == null || request.Trailers.Fields == null)
        {
            throw CreateProtocolError(
                "invalid-cached-response-request",
                "The cached response request is incomplete.");
        }

        ValidateHeaders(request.Headers);
        ValidateHeaders(request.Trailers);
    }

    static void ValidateResponse(HttpResponse response)
    {
        if (response.Headers == null || response.Headers.Fields == null ||
            response.Content == null || response.Trailers == null ||
            response.Trailers.Fields == null || response.ReasonPhrase == null)
        {
            throw CreateProtocolError(
                "invalid-cached-response",
                "The stored response is incomplete.");
        }

        ValidateHeaders(response.Headers);
        ValidateHeaders(response.Trailers);
        if (!HeaderStore.AreValidTrailers(response.Trailers))
        {
            throw CreateProtocolError(
                "invalid-cached-response-trailers",
                "The stored response contains prohibited trailer fields.");
        }
    }

    static void ValidateHeaders(HttpHeaders headers)
    {
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (field == null || !HeaderStore.IsValidName(field.Name) ||
                !HeaderStore.IsValidValue(field.Value))
            {
                throw CreateProtocolError(
                    "invalid-cached-response-field",
                    "A cached response field is malformed.");
            }

            index = index + 1;
        }
    }

    static HttpRequest GetRequest(HttpCacheRequestContext context)
    {
        HttpRequest? request = context.Request;
        if (request == null)
        {
            throw CreateProtocolError(
                "invalid-cached-response-context",
                "The cached response request is missing.");
        }

        return request;
    }

    static HttpResponse GetResponse(HttpCacheResponseContext context)
    {
        HttpResponse? response = context.Response;
        if (response == null)
        {
            throw CreateProtocolError(
                "invalid-cached-response-context",
                "The stored response is missing.");
        }

        return response;
    }

    static HttpProtocolException CreateProtocolError(string code, string message)
    {
        return new HttpProtocolException(message, code);
    }
}
