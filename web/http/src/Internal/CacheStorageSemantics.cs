using System;
using System.Collections.Generic;
using System.Text;

namespace http;

internal static class CacheStorageSemantics
{
    internal static HttpCacheDecision Evaluate(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        ValidateContexts(requestContext, responseContext);
        HttpRequest request = GetRequest(requestContext);
        HttpResponse response = GetResponse(responseContext);
        HttpCacheControl requestControl = requestContext.CacheControl;
        HttpCacheControl responseControl = responseContext.CacheControl;

        if (requestControl.HasConflicts || responseControl.HasConflicts ||
            !HasValidDirectiveState(requestControl) ||
            !HasValidDirectiveState(responseControl))
        {
            return CreateDoNotStore(
                requestContext,
                responseContext,
                "Conflicting or invalid Cache-Control directives prevent storage.");
        }

        HttpMethodMetadata method = MethodSemantics.Describe(request.Method);
        if (!method.IsCacheable)
        {
            return CreateDoNotStore(
                requestContext,
                responseContext,
                "The request method does not define cache storage semantics.");
        }

        HttpStatusMetadata status = StatusSemantics.Describe(response.StatusCode);
        if (status.StatusClass == HttpStatusClass.Informational)
        {
            return CreateDoNotStore(
                requestContext,
                responseContext,
                "Informational responses are not final and cannot be stored.");
        }

        bool understoodStatus = UnderstandsCachingStatus(response.StatusCode);
        if ((response.StatusCode == HttpStatusCodes.PartialContent ||
            response.StatusCode == HttpStatusCodes.NotModified ||
            responseControl.MustUnderstand) && !understoodStatus)
        {
            return CreateDoNotStore(
                requestContext,
                responseContext,
                "The response status caching requirements are not understood.");
        }

        if (requestControl.NoStore)
        {
            return CreateDoNotStore(
                requestContext,
                responseContext,
                "The request no-store directive prohibits storing its response.");
        }

        bool ignoreResponseNoStore = responseControl.MustUnderstand && understoodStatus;
        if (responseControl.NoStore && !ignoreResponseNoStore)
        {
            return CreateDoNotStore(
                requestContext,
                responseContext,
                "The response no-store directive prohibits storage.");
        }

        if (requestContext.IsShared && responseControl.Private &&
            responseControl.PrivateFields.Count == 0)
        {
            return CreateDoNotStore(
                requestContext,
                responseContext,
                "An unqualified private directive prohibits shared storage.");
        }

        if (requestContext.IsShared && HasAuthorization(request) &&
            !AllowsAuthenticatedSharedStorage(responseControl))
        {
            return CreateDoNotStore(
                requestContext,
                responseContext,
                "The authenticated request lacks a response directive allowing shared storage.");
        }

        if (!CanStoreResponseCompleteness(request, response, responseContext))
        {
            return CreateDoNotStore(
                requestContext,
                responseContext,
                "The incomplete or partial response cannot be stored safely.");
        }

        bool hasExplicitFreshness = HasExplicitFreshness(
            requestContext.IsShared,
            responseContext,
            responseControl);
        if (request.Method == HttpMethods.Post &&
            (!hasExplicitFreshness || !HasMatchingPostContentLocation(request, response)))
        {
            return CreateDoNotStore(
                requestContext,
                responseContext,
                "A POST response requires explicit freshness and a matching Content-Location.");
        }

        bool privateCacheability = !requestContext.IsShared && responseControl.Private;
        bool explicitCacheability = responseControl.Public || privateCacheability ||
            hasExplicitFreshness;
        bool heuristicCacheability = status.IsCacheableByDefault;
        if (!explicitCacheability && !heuristicCacheability)
        {
            return CreateDoNotStore(
                requestContext,
                responseContext,
                "The response is neither explicitly nor heuristically cacheable.");
        }

        HttpCacheDecision result = CreateStore(
            requestContext,
            responseContext,
            explicitCacheability ?
                "The response satisfies explicit cache storage requirements." :
                "The response status is heuristically cacheable by default.");
        result.IsHeuristicFreshness = !hasExplicitFreshness &&
            (heuristicCacheability || responseControl.Public || privateCacheability);
        return result;
    }

    static void ValidateContexts(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        if (requestContext == null || responseContext == null ||
            requestContext.Request == null || responseContext.Response == null ||
            requestContext.CacheControl == null || responseContext.CacheControl == null)
        {
            throw new HttpProtocolException(
                "The cache storage context is incomplete.",
                "invalid-cache-storage-context");
        }

        HttpRequest request = requestContext.Request;
        HttpResponse response = responseContext.Response;
        if (request.Method == null || request.Headers == null ||
            request.Headers.Fields == null || response.Headers == null ||
            response.Headers.Fields == null || response.Content == null)
        {
            throw new HttpProtocolException(
                "The cache storage message data is incomplete.",
                "invalid-cache-storage-context");
        }

        ValidateHeaderFields(request.Headers);
        ValidateHeaderFields(response.Headers);
    }

    static void ValidateHeaderFields(HttpHeaders headers)
    {
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (field == null || !HeaderStore.IsValidName(field.Name) ||
                !HeaderStore.IsValidValue(field.Value))
            {
                throw new HttpProtocolException(
                    "A cache storage message contains a malformed field.",
                    "invalid-cache-storage-field");
            }

            index = index + 1;
        }
    }

    static HttpRequest GetRequest(HttpCacheRequestContext context)
    {
        HttpRequest? request = context.Request;
        if (request == null)
        {
            throw new HttpProtocolException(
                "The cache request is missing.",
                "invalid-cache-storage-context");
        }

        return request;
    }

    static HttpResponse GetResponse(HttpCacheResponseContext context)
    {
        HttpResponse? response = context.Response;
        if (response == null)
        {
            throw new HttpProtocolException(
                "The cache response is missing.",
                "invalid-cache-storage-context");
        }

        return response;
    }

    static bool HasValidDirectiveState(HttpCacheControl control)
    {
        if (control.NoCacheFields == null || control.PrivateFields == null ||
            control.Extensions == null ||
            control.HasMaxAge && control.MaxAge < 0 ||
            control.HasSharedMaxAge && control.SharedMaxAge < 0 ||
            control.HasMaxStaleValue && !control.HasMaxStale ||
            control.HasMaxStaleValue && control.MaxStale < 0 ||
            control.HasMinFresh && control.MinFresh < 0 ||
            control.HasStaleIfError && control.StaleIfError < 0 ||
            control.HasStaleWhileRevalidate &&
            control.StaleWhileRevalidate < 0 ||
            !HasValidFieldNames(control.NoCacheFields) ||
            !HasValidFieldNames(control.PrivateFields))
        {
            return false;
        }

        int index = 0;
        while (index < control.Extensions.Count)
        {
            HttpParameter extension = control.Extensions[index];
            if (extension == null || !HeaderStore.IsValidName(extension.Name) ||
                !HeaderStore.IsValidValue(extension.Value))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool HasValidFieldNames(List<string> names)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (!HeaderStore.IsValidName(names[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool UnderstandsCachingStatus(int statusCode)
    {
        switch (statusCode)
        {
            case 200:
            case 201:
            case 202:
            case 203:
            case 204:
            case 205:
            case 206:
            case 300:
            case 301:
            case 302:
            case 303:
            case 304:
            case 305:
            case 306:
            case 307:
            case 308:
            case 400:
            case 401:
            case 402:
            case 403:
            case 404:
            case 405:
            case 406:
            case 407:
            case 408:
            case 409:
            case 410:
            case 411:
            case 412:
            case 413:
            case 414:
            case 415:
            case 416:
            case 417:
            case 418:
            case 421:
            case 422:
            case 426:
            case 500:
            case 501:
            case 502:
            case 503:
            case 504:
            case 505:
                return true;
            default:
                return false;
        }
    }

    static bool HasAuthorization(HttpRequest request)
    {
        return HeaderStore.FindMatchingFields(
            request.Headers,
            HttpHeaderNames.Authorization).Count != 0;
    }

    static bool AllowsAuthenticatedSharedStorage(HttpCacheControl responseControl)
    {
        return responseControl.MustRevalidate || responseControl.Public ||
            responseControl.HasSharedMaxAge;
    }

    static bool CanStoreResponseCompleteness(
        HttpRequest request,
        HttpResponse response,
        HttpCacheResponseContext responseContext)
    {
        if (response.StatusCode == HttpStatusCodes.PartialContent)
        {
            return response.IsComplete && request.Method == HttpMethods.Get &&
                UnderstandsPartialResponse(response);
        }

        if (response.IsComplete && responseContext.IsComplete)
        {
            return true;
        }

        return request.Method == HttpMethods.Get &&
            response.StatusCode == HttpStatusCodes.Ok;
    }

    static bool UnderstandsPartialResponse(HttpResponse response)
    {
        List<HttpHeaderField> contentRanges = HeaderStore.FindMatchingFields(
            response.Headers,
            HttpHeaderNames.ContentRange);
        if (contentRanges.Count == 1)
        {
            byte[] value = TrimOptionalWhitespace(contentRanges[0].Value);
            RangeValueParseResult parsed = FieldValueParser.ParseContentRange(
                value,
                value.Length);
            return parsed.Status == HttpOperationStatus.Done &&
                !parsed.ContentRange.IsUnsatisfied &&
                Ascii.EqualsStringsIgnoreCase(
                    parsed.ContentRange.Unit,
                    HttpRangeUnits.Bytes);
        }

        if (contentRanges.Count != 0)
        {
            return false;
        }

        return HasMultipartByteRangesContentType(response);
    }

    static bool HasMultipartByteRangesContentType(HttpResponse response)
    {
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(
            response.Headers,
            HttpHeaderNames.ContentType);
        if (fields.Count != 1)
        {
            return false;
        }

        byte[] value = TrimOptionalWhitespace(fields[0].Value);
        MediaTypeParseResult parsed = FieldValueParser.ParseMediaType(
            value,
            value.Length,
            int.MaxValue);
        if (parsed.Status != HttpOperationStatus.Done ||
            !Ascii.EqualsStringsIgnoreCase(parsed.Value.Type, "multipart") ||
            !Ascii.EqualsStringsIgnoreCase(parsed.Value.Subtype, "byteranges"))
        {
            return false;
        }

        int boundaryCount = 0;
        int index = 0;
        while (index < parsed.Value.Parameters.Count)
        {
            HttpParameter parameter = parsed.Value.Parameters[index];
            if (Ascii.EqualsStringsIgnoreCase(parameter.Name, "boundary"))
            {
                boundaryCount = boundaryCount + 1;
            }

            index = index + 1;
        }

        return boundaryCount == 1;
    }

    static bool HasExplicitFreshness(
        bool shared,
        HttpCacheResponseContext responseContext,
        HttpCacheControl responseControl)
    {
        return responseControl.HasMaxAge || responseContext.HasExpires ||
            shared && responseControl.HasSharedMaxAge;
    }

    static bool HasMatchingPostContentLocation(
        HttpRequest request,
        HttpResponse response)
    {
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(
            response.Headers,
            HttpHeaderNames.ContentLocation);
        if (fields.Count != 1)
        {
            return false;
        }

        byte[] value = TrimOptionalWhitespace(fields[0].Value);
        FieldValueParseResult parsed = FieldValueParser.ParseContentLocation(
            value,
            value.Length);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            return false;
        }

        string location = Encoding.ASCII.GetString(parsed.Value);
        UriValidationResult validated = UriCodec.ValidateUriReference(
            location,
            location.Length);
        if (validated.Status != HttpOperationStatus.Done || validated.HasFragment)
        {
            return false;
        }

        string effectiveUri = RequestTargetSemantics.BuildEffectiveUri(request);
        Uri target = new Uri(effectiveUri, UriKind.Absolute);
        Uri resolved = new Uri(target, location);
        return target.AbsoluteUri == resolved.AbsoluteUri;
    }

    static byte[] TrimOptionalWhitespace(byte[] value)
    {
        int start = 0;
        while (start < value.Length && (value[start] == 0x20 || value[start] == 0x09))
        {
            start = start + 1;
        }

        int end = value.Length;
        while (end > start && (value[end - 1] == 0x20 || value[end - 1] == 0x09))
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

    static HttpCacheDecision CreateStore(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext,
        string reason)
    {
        HttpCacheDecision result = new HttpCacheDecision();
        result.Outcome = HttpCacheOutcome.Store;
        result.RequestContext = requestContext;
        result.ResponseContext = responseContext;
        result.Response = responseContext.Response;
        result.Reason = reason;
        return result;
    }

    static HttpCacheDecision CreateDoNotStore(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext,
        string reason)
    {
        HttpCacheDecision result = new HttpCacheDecision();
        result.Outcome = HttpCacheOutcome.DoNotStore;
        result.RequestContext = requestContext;
        result.ResponseContext = responseContext;
        result.Reason = reason;
        return result;
    }
}
