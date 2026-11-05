using System;
using System.Collections.Generic;

namespace http;

internal class CacheValidatorMetadata
{
    internal HttpEntityTag? EntityTag;
    internal bool HasLastModified;
    internal DateTimeOffset LastModified;
}

internal class CacheRefreshedMetadata
{
    internal HttpCacheControl CacheControl = new HttpCacheControl();
    internal bool HasDate;
    internal DateTimeOffset Date;
    internal bool HasAge;
    internal long Age;
    internal bool HasExpires;
    internal DateTimeOffset Expires;
    internal bool HasLastModified;
    internal DateTimeOffset LastModified;
}

internal static class CacheValidationSemantics
{
    internal static HttpCacheValidationResult MergeNotModified(
        HttpCacheResponseContext responseContext,
        HttpResponse notModifiedResponse)
    {
        List<HttpCacheResponseContext> candidates = CollectCandidates(responseContext);
        ValidateNotModifiedResponse(notModifiedResponse);
        CacheValidatorMetadata received = ReadValidators(notModifiedResponse, false);
        List<HttpCacheResponseContext> selected = SelectCandidates(
            candidates,
            received);
        if (selected.Count == 0)
        {
            return CreateUnmerged(
                "The 304 validators did not safely identify a stored response.");
        }

        List<HttpResponse> mergedResponses = new List<HttpResponse>();
        List<CacheRefreshedMetadata> refreshedMetadata =
            new List<CacheRefreshedMetadata>();
        int index = 0;
        while (index < selected.Count)
        {
            HttpResponse stored = GetResponse(selected[index]);
            HttpResponse merged = BuildMergedResponse(stored, notModifiedResponse);
            mergedResponses.Add(merged);
            refreshedMetadata.Add(BuildRefreshedMetadata(selected[index], merged));
            index = index + 1;
        }

        HttpCacheValidationResult result = new HttpCacheValidationResult();
        index = 0;
        while (index < selected.Count)
        {
            HttpResponse target = GetResponse(selected[index]);
            ApplyResponse(target, mergedResponses[index]);
            ApplyMetadata(
                selected[index],
                refreshedMetadata[index],
                responseContext.CurrentTime);
            result.AffectedResponses.Add(target);
            if (result.Response == null)
            {
                result.Response = target;
            }

            index = index + 1;
        }

        result.WasMerged = true;
        result.Reason = selected.Count == 1 ?
            "The 304 metadata was merged into one stored response." :
            "The 304 metadata was merged into matching stored responses.";
        index = 0;
        while (index < selected.Count)
        {
            selected[index].ValidationResult = result;
            index = index + 1;
        }

        return result;
    }

    static List<HttpCacheResponseContext> CollectCandidates(
        HttpCacheResponseContext responseContext)
    {
        if (responseContext == null || responseContext.Response == null ||
            responseContext.MatchingResponseContexts == null)
        {
            throw CreateProtocolError(
                "invalid-cache-validation-context",
                "The cache validation context is incomplete.");
        }

        List<HttpCacheResponseContext> result =
            new List<HttpCacheResponseContext>();
        AddCandidate(result, responseContext);
        int index = 0;
        while (index < responseContext.MatchingResponseContexts.Count)
        {
            HttpCacheResponseContext candidate =
                responseContext.MatchingResponseContexts[index];
            if (candidate == null || candidate.Response == null)
            {
                throw CreateProtocolError(
                    "invalid-cache-validation-context",
                    "A matching stored-response context is incomplete.");
            }

            AddCandidate(result, candidate);
            index = index + 1;
        }

        index = 0;
        while (index < result.Count)
        {
            ValidateStoredContext(result[index]);
            ReadValidators(GetResponse(result[index]), true);
            index = index + 1;
        }

        return result;
    }

    static void AddCandidate(
        List<HttpCacheResponseContext> candidates,
        HttpCacheResponseContext candidate)
    {
        HttpResponse response = GetResponse(candidate);
        int index = 0;
        while (index < candidates.Count)
        {
            if (GetResponse(candidates[index]) == response)
            {
                return;
            }

            index = index + 1;
        }

        candidates.Add(candidate);
    }

    static List<HttpCacheResponseContext> SelectCandidates(
        List<HttpCacheResponseContext> candidates,
        CacheValidatorMetadata received)
    {
        List<HttpCacheResponseContext> result =
            new List<HttpCacheResponseContext>();
        if (received.EntityTag != null && !received.EntityTag.IsWeak)
        {
            int strongIndex = 0;
            while (strongIndex < candidates.Count)
            {
                CacheValidatorMetadata stored = ReadValidators(
                    GetResponse(candidates[strongIndex]),
                    true);
                if (stored.EntityTag != null && !stored.EntityTag.IsWeak &&
                    EntityTagComparer.CompareStrong(
                        stored.EntityTag,
                        received.EntityTag))
                {
                    result.Add(candidates[strongIndex]);
                }

                strongIndex = strongIndex + 1;
            }

            return result;
        }

        if (received.EntityTag != null || received.HasLastModified)
        {
            HttpCacheResponseContext? newest = null;
            int weakIndex = 0;
            while (weakIndex < candidates.Count)
            {
                CacheValidatorMetadata stored = ReadValidators(
                    GetResponse(candidates[weakIndex]),
                    true);
                if (ValidatorsCorrespond(received, stored) &&
                    (newest == null || IsNewer(candidates[weakIndex], newest)))
                {
                    newest = candidates[weakIndex];
                }

                weakIndex = weakIndex + 1;
            }

            if (newest != null)
            {
                result.Add(newest);
            }

            return result;
        }

        if (candidates.Count == 1)
        {
            CacheValidatorMetadata stored = ReadValidators(
                GetResponse(candidates[0]),
                true);
            if (stored.EntityTag == null && !stored.HasLastModified)
            {
                result.Add(candidates[0]);
            }
        }

        return result;
    }

    static bool ValidatorsCorrespond(
        CacheValidatorMetadata received,
        CacheValidatorMetadata stored)
    {
        if (received.EntityTag != null &&
            (stored.EntityTag == null || !EntityTagComparer.CompareWeak(
                received.EntityTag,
                stored.EntityTag)))
        {
            return false;
        }

        if (received.HasLastModified &&
            (!stored.HasLastModified || !HttpDateCodec.EqualsAtSecondPrecision(
                received.LastModified,
                stored.LastModified)))
        {
            return false;
        }

        return true;
    }

    static bool IsNewer(
        HttpCacheResponseContext candidate,
        HttpCacheResponseContext current)
    {
        DateTimeOffset candidateTime = candidate.HasDate ?
            candidate.Date : candidate.ResponseTime;
        DateTimeOffset currentTime = current.HasDate ?
            current.Date : current.ResponseTime;
        return HttpDateCodec.CompareAtSecondPrecision(
            candidateTime,
            currentTime) > 0;
    }

    static HttpResponse BuildMergedResponse(
        HttpResponse stored,
        HttpResponse notModified)
    {
        HttpResponse result = CopyResponse(stored);
        HttpHeaders hopFields = IntermediarySemantics.DetermineHopFields(
            notModified.Headers);
        List<string> replacementNames = new List<string>();
        int index = 0;
        while (index < notModified.Headers.Fields.Count)
        {
            HttpHeaderField field = notModified.Headers.Fields[index];
            if (!IsExcludedUpdate(field.Name, hopFields) &&
                !ContainsName(replacementNames, field.Name))
            {
                replacementNames.Add(field.Name);
                HeaderStore.RemoveMatchingFields(result.Headers, field.Name);
            }

            index = index + 1;
        }

        index = 0;
        while (index < notModified.Headers.Fields.Count)
        {
            HttpHeaderField field = notModified.Headers.Fields[index];
            if (!IsExcludedUpdate(field.Name, hopFields))
            {
                result.Headers.Fields.Add(CopyField(field));
            }

            index = index + 1;
        }

        return result;
    }

    static bool IsExcludedUpdate(string name, HttpHeaders hopFields)
    {
        return ContainsFieldName(hopFields, name) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderNames.ContentLength) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderNames.ContentRange) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderNames.ProxyAuthenticate) ||
            Ascii.EqualsStringsIgnoreCase(
                name,
                HttpHeaderNames.ProxyAuthenticationInfo) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderNames.ProxyAuthorization);
    }

    static bool ContainsFieldName(HttpHeaders headers, string name)
    {
        return HeaderStore.FindMatchingFields(headers, name).Count != 0;
    }

    static bool ContainsName(List<string> names, string name)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (Ascii.EqualsStringsIgnoreCase(names[index], name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static CacheValidatorMetadata ReadValidators(
        HttpResponse response,
        bool includeTrailers)
    {
        CacheValidatorMetadata result = new CacheValidatorMetadata();
        List<HttpHeaderField> entityTagFields = FindFields(
            response,
            HttpHeaderNames.ETag,
            includeTrailers);
        if (entityTagFields.Count > 1)
        {
            throw CreateProtocolError(
                "conflicting-cache-validator",
                "A response contains multiple entity tags.");
        }

        if (entityTagFields.Count == 1)
        {
            byte[] value = TrimOptionalWhitespace(entityTagFields[0].Value);
            EntityTagParseResult parsed = FieldValueParser.ParseEntityTag(
                value,
                value.Length);
            if (parsed.Status != HttpOperationStatus.Done)
            {
                throw CreateProtocolError(
                    "invalid-cache-validator",
                    "A response contains a malformed entity tag.");
            }

            result.EntityTag = parsed.Value;
        }

        List<HttpHeaderField> modifiedFields = FindFields(
            response,
            HttpHeaderNames.LastModified,
            includeTrailers);
        if (modifiedFields.Count > 1)
        {
            throw CreateProtocolError(
                "conflicting-cache-validator",
                "A response contains multiple Last-Modified values.");
        }

        if (modifiedFields.Count == 1)
        {
            byte[] value = TrimOptionalWhitespace(modifiedFields[0].Value);
            HttpDateParseResult parsed = HttpDateCodec.ParseHttpDate(
                value,
                value.Length);
            if (parsed.Status != HttpOperationStatus.Done)
            {
                throw CreateProtocolError(
                    "invalid-cache-validator",
                    "A response contains a malformed Last-Modified value.");
            }

            result.HasLastModified = true;
            result.LastModified = parsed.Value;
        }

        return result;
    }

    static List<HttpHeaderField> FindFields(
        HttpResponse response,
        string name,
        bool includeTrailers)
    {
        List<HttpHeaderField> result = HeaderStore.FindMatchingFields(
            response.Headers,
            name);
        if (includeTrailers)
        {
            List<HttpHeaderField> trailers = HeaderStore.FindMatchingFields(
                response.Trailers,
                name);
            int index = 0;
            while (index < trailers.Count)
            {
                result.Add(trailers[index]);
                index = index + 1;
            }
        }

        return result;
    }

    static CacheRefreshedMetadata BuildRefreshedMetadata(
        HttpCacheResponseContext context,
        HttpResponse response)
    {
        CacheRefreshedMetadata result = new CacheRefreshedMetadata();
        result.CacheControl = ReadCacheControl(response.Headers, context.CacheControl);
        ReadDateMetadata(response.Headers, HttpHeaderNames.Date, 0, result, context);
        ReadAgeMetadata(response.Headers, result, context);
        ReadDateMetadata(response.Headers, HttpHeaderNames.Expires, 1, result, context);
        ReadLastModifiedMetadata(response, result, context);
        return result;
    }

    static HttpCacheControl ReadCacheControl(
        HttpHeaders headers,
        HttpCacheControl fallback)
    {
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(
            headers,
            HttpHeaderNames.CacheControl);
        if (fields.Count == 0)
        {
            return fallback;
        }

        byte[] combined = HeaderStore.CombineMatchingValues(
            headers,
            HttpHeaderNames.CacheControl);
        CacheControlParseResult parsed = FieldValueParser.ParseCacheControlValue(
            combined,
            combined.Length,
            int.MaxValue,
            int.MaxValue);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            throw CreateProtocolError(
                "invalid-cache-validation-metadata",
                "The merged Cache-Control field is malformed.");
        }

        return parsed.Value;
    }

    static void ReadDateMetadata(
        HttpHeaders headers,
        string name,
        int kind,
        CacheRefreshedMetadata result,
        HttpCacheResponseContext fallback)
    {
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(headers, name);
        if (fields.Count == 0)
        {
            if (kind == 0)
            {
                result.HasDate = fallback.HasDate;
                result.Date = fallback.Date;
            }
            else
            {
                result.HasExpires = fallback.HasExpires;
                result.Expires = fallback.Expires;
            }

            return;
        }

        if (fields.Count != 1)
        {
            throw CreateProtocolError(
                "invalid-cache-validation-metadata",
                $"The merged {name} field is duplicated.");
        }

        byte[] value = TrimOptionalWhitespace(fields[0].Value);
        HttpDateParseResult parsed = HttpDateCodec.ParseHttpDate(
            value,
            value.Length);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            throw CreateProtocolError(
                "invalid-cache-validation-metadata",
                $"The merged {name} field is malformed.");
        }

        if (kind == 0)
        {
            result.HasDate = true;
            result.Date = parsed.Value;
        }
        else
        {
            result.HasExpires = true;
            result.Expires = parsed.Value;
        }
    }

    static void ReadAgeMetadata(
        HttpHeaders headers,
        CacheRefreshedMetadata result,
        HttpCacheResponseContext fallback)
    {
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(
            headers,
            HttpHeaderNames.Age);
        if (fields.Count == 0)
        {
            result.HasAge = fallback.HasAge;
            result.Age = fallback.Age;
            return;
        }

        if (fields.Count != 1)
        {
            throw CreateProtocolError(
                "invalid-cache-validation-metadata",
                "The merged Age field is duplicated.");
        }

        byte[] value = TrimOptionalWhitespace(fields[0].Value);
        IntegerParseResult parsed = IntegerParser.ParseDecimal(value);
        if (parsed.Overflow)
        {
            result.HasAge = true;
            result.Age = 2147483648;
            return;
        }

        if (!parsed.IsValid || parsed.Consumed != value.Length)
        {
            throw CreateProtocolError(
                "invalid-cache-validation-metadata",
                "The merged Age field is malformed.");
        }

        result.HasAge = true;
        result.Age = parsed.Value;
    }

    static void ReadLastModifiedMetadata(
        HttpResponse response,
        CacheRefreshedMetadata result,
        HttpCacheResponseContext fallback)
    {
        CacheValidatorMetadata validators = ReadValidators(response, true);
        if (validators.HasLastModified)
        {
            result.HasLastModified = true;
            result.LastModified = validators.LastModified;
        }
        else
        {
            result.HasLastModified = fallback.HasLastModified;
            result.LastModified = fallback.LastModified;
        }
    }

    static void ApplyMetadata(
        HttpCacheResponseContext context,
        CacheRefreshedMetadata metadata,
        DateTimeOffset validationTime)
    {
        context.CacheControl = metadata.CacheControl;
        context.HasDate = metadata.HasDate;
        context.Date = metadata.Date;
        context.HasAge = metadata.HasAge;
        context.Age = metadata.Age;
        context.HasExpires = metadata.HasExpires;
        context.Expires = metadata.Expires;
        context.HasLastModified = metadata.HasLastModified;
        context.LastModified = metadata.LastModified;
        context.RequestTime = validationTime;
        context.ResponseTime = validationTime;
        context.StoredTime = validationTime;
        context.CurrentTime = validationTime;
    }

    static void ValidateStoredContext(HttpCacheResponseContext context)
    {
        if (context.CacheControl == null ||
            context.MatchingResponseContexts == null)
        {
            throw CreateProtocolError(
                "invalid-cache-validation-context",
                "A stored-response context is incomplete.");
        }

        ValidateResponse(GetResponse(context));
    }

    static void ValidateNotModifiedResponse(HttpResponse response)
    {
        ValidateResponse(response);
        if (response.StatusCode != HttpStatusCodes.NotModified ||
            response.Content.Length != 0 || response.IsInformational)
        {
            throw CreateProtocolError(
                "invalid-not-modified-response",
                "Cache validation requires a content-free 304 response.");
        }
    }

    static void ValidateResponse(HttpResponse response)
    {
        if (response == null || response.Headers == null ||
            response.Headers.Fields == null || response.ReasonPhrase == null ||
            response.Content == null ||
            response.Trailers == null || response.Trailers.Fields == null)
        {
            throw CreateProtocolError(
                "invalid-cache-validation-response",
                "A cache validation response is incomplete.");
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
                throw CreateProtocolError(
                    "invalid-cache-validation-field",
                    "A cache validation field is malformed.");
            }

            index = index + 1;
        }
    }

    static HttpResponse GetResponse(HttpCacheResponseContext context)
    {
        HttpResponse? response = context.Response;
        if (response == null)
        {
            throw CreateProtocolError(
                "invalid-cache-validation-context",
                "A stored response is missing.");
        }

        return response;
    }

    static HttpResponse CopyResponse(HttpResponse source)
    {
        HttpResponse result = new HttpResponse();
        result.ProtocolVersion = source.ProtocolVersion;
        result.StatusCode = source.StatusCode;
        result.HasReasonPhrase = source.HasReasonPhrase;
        result.ReasonPhrase = CopyBytes(source.ReasonPhrase);
        result.Headers = CopyHeaders(source.Headers);
        result.Content = CopyBytes(source.Content);
        result.Trailers = CopyHeaders(source.Trailers);
        result.IsInformational = source.IsInformational;
        result.IsComplete = source.IsComplete;
        return result;
    }

    static void ApplyResponse(HttpResponse target, HttpResponse source)
    {
        target.ProtocolVersion = source.ProtocolVersion;
        target.StatusCode = source.StatusCode;
        target.HasReasonPhrase = source.HasReasonPhrase;
        target.ReasonPhrase = source.ReasonPhrase;
        target.Headers = source.Headers;
        target.Content = source.Content;
        target.Trailers = source.Trailers;
        target.IsInformational = source.IsInformational;
        target.IsComplete = source.IsComplete;
    }

    static HttpHeaders CopyHeaders(HttpHeaders source)
    {
        HttpHeaders result = new HttpHeaders();
        int index = 0;
        while (index < source.Fields.Count)
        {
            result.Fields.Add(CopyField(source.Fields[index]));
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

    static HttpCacheValidationResult CreateUnmerged(string reason)
    {
        HttpCacheValidationResult result = new HttpCacheValidationResult();
        result.Reason = reason;
        return result;
    }

    static HttpProtocolException CreateProtocolError(string code, string message)
    {
        return new HttpProtocolException(message, code);
    }
}
