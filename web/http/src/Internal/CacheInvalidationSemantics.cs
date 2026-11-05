using System;
using System.Collections.Generic;
using System.Text;

namespace http;

internal static class CacheInvalidationSemantics
{
    internal static HttpCacheDecision Evaluate(
        HttpRequest request,
        HttpResponse response)
    {
        ValidateRequest(request);
        ValidateResponse(response);
        HttpMethodMetadata method = MethodSemantics.Describe(request.Method);
        HttpStatusMetadata status = StatusSemantics.Describe(response.StatusCode);
        HttpCacheDecision result = new HttpCacheDecision();
        result.Response = response;
        if (method.IsSafe || status.StatusClass != HttpStatusClass.Successful &&
            status.StatusClass != HttpStatusClass.Redirection)
        {
            result.Outcome = HttpCacheOutcome.Unusable;
            result.Reason = method.IsSafe ?
                "A safe request does not invalidate stored responses." :
                "An error response does not invalidate stored responses.";
            return result;
        }

        string effectiveUri = RequestTargetSemantics.BuildEffectiveUri(request);
        string target = CacheKeySemantics.NormalizeTargetUri(effectiveUri);
        result.InvalidationTargets.Add(target);
        AddLocationTarget(
            result.InvalidationTargets,
            response.Headers,
            HttpHeaderNames.Location,
            true,
            target);
        AddLocationTarget(
            result.InvalidationTargets,
            response.Headers,
            HttpHeaderNames.ContentLocation,
            false,
            target);
        result.Outcome = HttpCacheOutcome.Invalidate;
        result.Reason = result.InvalidationTargets.Count == 1 ?
            "The successful unsafe request invalidates its target URI." :
            "The successful unsafe request invalidates same-origin target URIs.";
        return result;
    }

    static void AddLocationTarget(
        List<string> targets,
        HttpHeaders headers,
        string name,
        bool allowFragment,
        string requestTarget)
    {
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(headers, name);
        if (fields.Count != 1)
        {
            return;
        }

        byte[] value = TrimOptionalWhitespace(fields[0].Value);
        FieldValueParseResult parsed = allowFragment ?
            FieldValueParser.ParseLocation(value, value.Length) :
            FieldValueParser.ParseContentLocation(value, value.Length);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            return;
        }

        string reference = Encoding.ASCII.GetString(parsed.Value);
        Uri baseUri = new Uri(requestTarget, UriKind.Absolute);
        Uri resolved = new Uri(baseUri, reference);
        string absolute = RemoveFragment(resolved.AbsoluteUri);
        UriValidationResult validated = UriCodec.ValidateUriReference(
            absolute,
            absolute.Length);
        if (validated.Status != HttpOperationStatus.Done ||
            !validated.IsAbsolute || validated.HasFragment ||
            !IntermediarySemantics.AreSameOrigin(requestTarget, absolute))
        {
            return;
        }

        AddUniqueTarget(targets, CacheKeySemantics.NormalizeTargetUri(absolute));
    }

    static string RemoveFragment(string value)
    {
        int fragment = value.IndexOf('#');
        return fragment < 0 ? value : value.Substring(0, fragment);
    }

    static void AddUniqueTarget(List<string> targets, string target)
    {
        int index = 0;
        while (index < targets.Count)
        {
            if (targets[index] == target)
            {
                return;
            }

            index = index + 1;
        }

        targets.Add(target);
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

    static void ValidateRequest(HttpRequest request)
    {
        if (request == null || request.Headers == null ||
            request.Headers.Fields == null || request.Target == null ||
            request.Method == null)
        {
            throw CreateProtocolError(
                "invalid-cache-invalidation-request",
                "The cache invalidation request is incomplete.");
        }

        ValidateHeaders(request.Headers);
    }

    static void ValidateResponse(HttpResponse response)
    {
        if (response == null || response.Headers == null ||
            response.Headers.Fields == null)
        {
            throw CreateProtocolError(
                "invalid-cache-invalidation-response",
                "The cache invalidation response is incomplete.");
        }

        ValidateHeaders(response.Headers);
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
                    "invalid-cache-invalidation-field",
                    "A cache invalidation field is malformed.");
            }

            index = index + 1;
        }
    }

    static HttpProtocolException CreateProtocolError(string code, string message)
    {
        return new HttpProtocolException(message, code);
    }
}
