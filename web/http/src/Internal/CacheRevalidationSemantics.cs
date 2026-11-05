using System;
using System.Collections.Generic;

namespace http;

internal static class CacheRevalidationSemantics
{
    internal static HttpRequest Build(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        ValidateContexts(requestContext, responseContext);
        HttpRequest request = GetRequest(requestContext);
        HttpResponse response = GetResponse(responseContext);
        MethodSemantics.Describe(request.Method);
        if (request.Method != HttpMethods.Get && request.Method != HttpMethods.Head)
        {
            throw new HttpProtocolException(
                "Only GET and HEAD cache requests can be revalidated.",
                "invalid-cache-revalidation-method");
        }

        HttpRequest result = CopyRequest(request);
        HttpEntityTag? entityTag = ReadEntityTag(response);
        List<HttpHeaderField> modifiedFields = FindStoredFields(
            response,
            HttpHeaderNames.LastModified);
        bool hasLastModified = responseContext.HasLastModified ||
            modifiedFields.Count != 0;
        DateTimeOffset lastModified = ReadLastModified(
            responseContext,
            modifiedFields);
        bool hasRange = HeaderStore.FindMatchingFields(
            result.Headers,
            HttpHeaderNames.Range).Count != 0;
        bool hasIfRange = HeaderStore.FindMatchingFields(
            result.Headers,
            HttpHeaderNames.IfRange).Count != 0;

        if (hasRange && !hasIfRange && entityTag != null && !entityTag.IsWeak)
        {
            SetEntityTagField(result.Headers, HttpHeaderNames.IfRange, entityTag);
            return result;
        }

        if (hasRange && !hasIfRange && entityTag == null && hasLastModified)
        {
            SetDateField(result.Headers, HttpHeaderNames.IfRange, lastModified);
            return result;
        }

        if (entityTag != null)
        {
            MergeIfNoneMatch(result.Headers, entityTag);
        }

        if (!hasRange && hasLastModified &&
            HeaderStore.FindMatchingFields(
                result.Headers,
                HttpHeaderNames.IfModifiedSince).Count == 0)
        {
            SetDateField(
                result.Headers,
                HttpHeaderNames.IfModifiedSince,
                lastModified);
        }

        return result;
    }

    static void ValidateContexts(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        if (requestContext == null || responseContext == null ||
            requestContext.Request == null || responseContext.Response == null)
        {
            throw new HttpProtocolException(
                "The cache revalidation context is incomplete.",
                "invalid-cache-revalidation-context");
        }

        ValidateRequest(requestContext.Request);
        ValidateResponse(responseContext.Response);
    }

    static void ValidateRequest(HttpRequest request)
    {
        if (request.Headers == null || request.Headers.Fields == null ||
            request.Trailers == null || request.Trailers.Fields == null ||
            request.Content == null || request.WeightedPreferences == null ||
            request.LanguagePreferences == null ||
            request.AuthenticationValues == null || request.ViaEntries == null)
        {
            throw new HttpProtocolException(
                "The cache revalidation request is incomplete.",
                "invalid-cache-revalidation-context");
        }

        ValidateHeaders(request.Headers);
        ValidateHeaders(request.Trailers);
    }

    static void ValidateResponse(HttpResponse response)
    {
        if (response.Headers == null || response.Headers.Fields == null ||
            response.Trailers == null || response.Trailers.Fields == null)
        {
            throw new HttpProtocolException(
                "The stored response fields are incomplete.",
                "invalid-cache-revalidation-context");
        }

        ValidateHeaders(response.Headers);
        ValidateHeaders(response.Trailers);
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
                throw new HttpProtocolException(
                    "A cache revalidation field is malformed.",
                    "invalid-cache-revalidation-field");
            }

            index = index + 1;
        }
    }

    static HttpRequest GetRequest(HttpCacheRequestContext requestContext)
    {
        HttpRequest? request = requestContext.Request;
        if (request == null)
        {
            throw new HttpProtocolException(
                "The cache revalidation request is missing.",
                "invalid-cache-revalidation-context");
        }

        return request;
    }

    static HttpResponse GetResponse(HttpCacheResponseContext responseContext)
    {
        HttpResponse? response = responseContext.Response;
        if (response == null)
        {
            throw new HttpProtocolException(
                "The cache revalidation response is missing.",
                "invalid-cache-revalidation-context");
        }

        return response;
    }

    static HttpEntityTag? ReadEntityTag(HttpResponse response)
    {
        List<HttpHeaderField> fields = FindStoredFields(
            response,
            HttpHeaderNames.ETag);
        if (fields.Count == 0)
        {
            return null;
        }

        if (fields.Count != 1)
        {
            throw new HttpProtocolException(
                "The stored response contains multiple entity tags.",
                "invalid-cache-revalidation-validator");
        }

        byte[] value = TrimOptionalWhitespace(fields[0].Value);
        EntityTagParseResult parsed = FieldValueParser.ParseEntityTag(
            value,
            value.Length);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            throw new HttpProtocolException(
                "The stored response entity tag is malformed.",
                "invalid-cache-revalidation-validator");
        }

        return parsed.Value;
    }

    static DateTimeOffset ReadLastModified(
        HttpCacheResponseContext responseContext,
        List<HttpHeaderField> fields)
    {
        if (fields.Count > 1)
        {
            throw new HttpProtocolException(
                "The stored response contains multiple Last-Modified fields.",
                "invalid-cache-revalidation-validator");
        }

        if (fields.Count == 0)
        {
            return responseContext.LastModified;
        }

        byte[] value = TrimOptionalWhitespace(fields[0].Value);
        HttpDateParseResult parsed = HttpDateCodec.ParseHttpDate(
            value,
            value.Length);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            throw new HttpProtocolException(
                "The stored response Last-Modified value is malformed.",
                "invalid-cache-revalidation-validator");
        }

        return parsed.Value;
    }

    static void MergeIfNoneMatch(HttpHeaders headers, HttpEntityTag entityTag)
    {
        PreconditionFieldValue existing = PreconditionParser.ParseEntityTagCondition(
            headers,
            HttpHeaderNames.IfNoneMatch);
        if (!existing.IsValid || existing.IsPresent &&
            !existing.IsWildcard && existing.EntityTags.Count == 0)
        {
            throw new HttpProtocolException(
                "The presented If-None-Match field is malformed.",
                "invalid-cache-revalidation-condition");
        }

        if (existing.IsWildcard)
        {
            return;
        }

        bool alreadyPresent = false;
        int index = 0;
        while (index < existing.EntityTags.Count)
        {
            HttpEntityTag existingTag = existing.EntityTags[index];
            if (existingTag.IsWeak == entityTag.IsWeak &&
                EntityTagComparer.CompareWeak(existingTag, entityTag))
            {
                alreadyPresent = true;
            }

            index = index + 1;
        }

        if (!alreadyPresent)
        {
            existing.EntityTags.Add(entityTag);
        }

        FieldValueWriteResult written = FieldValueWriter.SerializeEntityTags(
            existing.EntityTags,
            int.MaxValue,
            int.MaxValue);
        RequireWritten(written, HttpHeaderNames.IfNoneMatch);
        SetField(headers, HttpHeaderNames.IfNoneMatch, written.Bytes);
    }

    static void SetEntityTagField(
        HttpHeaders headers,
        string name,
        HttpEntityTag entityTag)
    {
        FieldValueWriteResult written = FieldValueWriter.SerializeEntityTag(
            entityTag,
            int.MaxValue);
        RequireWritten(written, name);
        SetField(headers, name, written.Bytes);
    }

    static void SetDateField(
        HttpHeaders headers,
        string name,
        DateTimeOffset value)
    {
        FieldValueWriteResult written = HttpDateCodec.SerializeHttpDate(
            value,
            int.MaxValue);
        RequireWritten(written, name);
        SetField(headers, name, written.Bytes);
    }

    static void RequireWritten(FieldValueWriteResult result, string name)
    {
        if (result.Status != HttpOperationStatus.Done)
        {
            throw new HttpProtocolException(
                $"The {name} validator cannot be serialized.",
                "invalid-cache-revalidation-validator");
        }
    }

    static void SetField(HttpHeaders headers, string name, byte[] value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = value;
        HeaderStore.ReplaceFields(headers, field);
    }

    static List<HttpHeaderField> FindStoredFields(
        HttpResponse response,
        string name)
    {
        List<HttpHeaderField> result = HeaderStore.FindMatchingFields(
            response.Headers,
            name);
        List<HttpHeaderField> trailerFields = HeaderStore.FindMatchingFields(
            response.Trailers,
            name);
        int index = 0;
        while (index < trailerFields.Count)
        {
            result.Add(trailerFields[index]);
            index = index + 1;
        }

        return result;
    }

    static HttpRequest CopyRequest(HttpRequest source)
    {
        HttpRequest result = new HttpRequest();
        result.ProtocolVersion = source.ProtocolVersion;
        result.Method = source.Method;
        result.MethodMetadata = source.MethodMetadata;
        result.Target = source.Target;
        result.TargetForm = source.TargetForm;
        result.Scheme = source.Scheme;
        result.Authority = source.Authority;
        result.Path = source.Path;
        result.HasQuery = source.HasQuery;
        result.Query = source.Query;
        result.ConnectProtocol = source.ConnectProtocol;
        result.Headers = CopyHeaders(source.Headers);
        result.Content = CopyBytes(source.Content);
        result.Trailers = CopyHeaders(source.Trailers);
        result.Priority = source.Priority;
        result.IsComplete = source.IsComplete;

        int index = 0;
        while (index < source.WeightedPreferences.Count)
        {
            result.WeightedPreferences.Add(source.WeightedPreferences[index]);
            index = index + 1;
        }

        index = 0;
        while (index < source.LanguagePreferences.Count)
        {
            result.LanguagePreferences.Add(source.LanguagePreferences[index]);
            index = index + 1;
        }

        index = 0;
        while (index < source.AuthenticationValues.Count)
        {
            result.AuthenticationValues.Add(source.AuthenticationValues[index]);
            index = index + 1;
        }

        index = 0;
        while (index < source.ViaEntries.Count)
        {
            result.ViaEntries.Add(source.ViaEntries[index]);
            index = index + 1;
        }

        return result;
    }

    static HttpHeaders CopyHeaders(HttpHeaders source)
    {
        HttpHeaders result = new HttpHeaders();
        int index = 0;
        while (index < source.Fields.Count)
        {
            HttpHeaderField sourceField = source.Fields[index];
            HttpHeaderField field = new HttpHeaderField();
            field.Name = sourceField.Name;
            field.Value = CopyBytes(sourceField.Value);
            field.IsSensitive = sourceField.IsSensitive;
            field.NeverIndex = sourceField.NeverIndex;
            result.Fields.Add(field);
            index = index + 1;
        }

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
}
