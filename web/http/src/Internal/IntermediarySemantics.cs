using System;
using System.Collections.Generic;
using System.Text;

namespace http;

internal static class IntermediarySemantics
{
    internal static HttpHeaders DetermineHopFields(HttpHeaders headers)
    {
        ValidateHeaders(headers);
        List<string> names = CollectHopNames(headers);
        HttpHeaders result = new HttpHeaders();
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (ContainsName(names, field.Name))
            {
                result.Fields.Add(CopyField(field));
            }

            index = index + 1;
        }

        return result;
    }

    internal static HttpRequest NormalizeRequest(HttpRequest request)
    {
        ValidateRequest(request);
        HttpRequestTargetForm form = RequestTargetSemantics.Determine(request);
        HttpRequest result = CopyRequest(request);
        bool retainTeTrailers = request.ProtocolVersion != HttpProtocolVersion.Http10 &&
            ContainsTeTrailers(request.Headers);
        List<string> hopNames = CollectHopNames(request.Headers);
        RemoveNamedFields(result.Headers, hopNames);
        RemoveNamedFields(result.Trailers, hopNames);
        if (!HeaderStore.AreValidTrailers(result.Trailers))
        {
            throw CreateProtocolError(
                "invalid-trailers",
                "The forwarded request contains a prohibited or malformed trailer field.");
        }

        if (retainTeTrailers)
        {
            result.Headers.Fields.Add(CreateTextField(HttpHeaderNames.TE, HttpHeaderValues.Trailers));
            if (result.ProtocolVersion == HttpProtocolVersion.Http11)
            {
                result.Headers.Fields.Add(CreateTextField(
                    HttpHeaderNames.Connection,
                    HttpHeaderNames.TE));
            }
        }

        NormalizeVia(result);
        if (!ApplyMaxForwards(result, int.MaxValue))
        {
            throw CreateProtocolError(
                "max-forwards-exhausted",
                "A request with Max-Forwards zero cannot be forwarded.");
        }

        NormalizeTargetMetadata(result, form);
        result.TargetForm = form;
        result.MethodMetadata = MethodSemantics.Describe(result.Method);
        return result;
    }

    internal static bool ApplyMaxForwards(HttpRequest request, int maximumSupported)
    {
        if (request == null || request.Headers == null ||
            request.Headers.Fields == null || maximumSupported < 0)
        {
            throw CreateProtocolError(
                "invalid-max-forwards-context",
                "The Max-Forwards forwarding context is invalid.");
        }

        if (request.Method != HttpMethods.Trace && request.Method != HttpMethods.Options)
        {
            return true;
        }

        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(
            request.Headers,
            HttpHeaderNames.MaxForwards);
        if (fields.Count == 0)
        {
            return true;
        }

        if (fields.Count != 1)
        {
            throw CreateProtocolError(
                "multiple-max-forwards-fields",
                "A request cannot contain multiple Max-Forwards fields.");
        }

        byte[] trimmed = TrimOptionalWhitespace(fields[0].Value);
        CommonFieldParseResult parsed = FieldValueParser.ParseMaxForwards(
            trimmed,
            trimmed.Length);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            throw CreateProtocolError(
                "invalid-max-forwards",
                "The Max-Forwards field value is malformed.");
        }

        if (parsed.Number == 0)
        {
            return false;
        }

        long updated = parsed.Number - 1;
        if (updated > maximumSupported)
        {
            updated = maximumSupported;
        }

        FieldValueWriteResult written = FieldValueWriter.SerializeMaxForwards(
            updated,
            int.MaxValue);
        HttpHeaderField replacement = new HttpHeaderField();
        replacement.Name = fields[0].Name;
        replacement.Value = written.Bytes;
        HeaderStore.ReplaceFields(request.Headers, replacement);
        return true;
    }

    internal static bool AreSameOrigin(string left, string right)
    {
        UriValidationResult leftUri = UriCodec.ValidateUriReference(left, int.MaxValue);
        UriValidationResult rightUri = UriCodec.ValidateUriReference(right, int.MaxValue);
        if (!IsSafeOriginUri(leftUri) || !IsSafeOriginUri(rightUri) ||
            !Ascii.EqualsStringsIgnoreCase(leftUri.Scheme, rightUri.Scheme))
        {
            return false;
        }

        return RequestTargetSemantics.AreAuthoritiesEquivalent(
            leftUri.Authority,
            rightUri.Authority,
            leftUri.Scheme);
    }

    static List<string> CollectHopNames(HttpHeaders headers)
    {
        List<string> names = new List<string>();
        AddUniqueName(names, HttpHeaderNames.Connection);
        AddUniqueName(names, "Proxy-Connection");
        AddUniqueName(names, HttpHeaderNames.KeepAlive);
        AddUniqueName(names, HttpHeaderNames.TE);
        AddUniqueName(names, HttpHeaderNames.TransferEncoding);
        AddUniqueName(names, HttpHeaderNames.Upgrade);

        List<HttpHeaderField> connectionFields = HeaderStore.FindMatchingFields(
            headers,
            HttpHeaderNames.Connection);
        if (connectionFields.Count == 0)
        {
            return names;
        }

        byte[] combined = HeaderStore.CombineMatchingValues(headers, HttpHeaderNames.Connection);
        FieldValueParseResult parsed = FieldValueParser.ParseConnection(
            combined,
            combined.Length,
            int.MaxValue);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            throw CreateProtocolError(
                "invalid-connection-field",
                "The Connection field value is malformed.");
        }

        int index = 0;
        while (index < parsed.Values.Count)
        {
            AddUniqueName(names, Encoding.ASCII.GetString(parsed.Values[index]));
            index = index + 1;
        }

        return names;
    }

    static bool ContainsTeTrailers(HttpHeaders headers)
    {
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(headers, HttpHeaderNames.TE);
        if (fields.Count == 0)
        {
            return false;
        }

        byte[] combined = HeaderStore.CombineMatchingValues(headers, HttpHeaderNames.TE);
        MediaTypeParseResult parsed = FieldValueParser.ParseTe(
            combined,
            combined.Length,
            int.MaxValue,
            int.MaxValue);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            throw CreateProtocolError(
                "invalid-te-field",
                "The TE field value is malformed.");
        }

        int index = 0;
        while (index < parsed.Values.Count)
        {
            if (Ascii.EqualsStringsIgnoreCase(
                parsed.Values[index].Value,
                HttpHeaderValues.Trailers))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static void NormalizeVia(HttpRequest request)
    {
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(
            request.Headers,
            HttpHeaderNames.Via);
        if (fields.Count == 0)
        {
            request.ViaEntries.Clear();
            return;
        }

        byte[] combined = HeaderStore.CombineMatchingValues(request.Headers, HttpHeaderNames.Via);
        CommonFieldParseResult parsed = FieldValueParser.ParseViaEntries(
            combined,
            combined.Length,
            int.MaxValue,
            combined.Length);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            throw CreateProtocolError(
                "invalid-via-field",
                "The Via field value is malformed.");
        }

        FieldValueWriteResult written = FieldValueWriter.SerializeViaEntries(
            parsed.ViaEntries,
            int.MaxValue,
            int.MaxValue,
            combined.Length);
        if (written.Status != HttpOperationStatus.Done)
        {
            throw CreateProtocolError(
                "invalid-via-field",
                "The Via metadata cannot be forwarded.");
        }

        HeaderStore.RemoveMatchingFields(request.Headers, HttpHeaderNames.Via);
        HttpHeaderField replacement = new HttpHeaderField();
        replacement.Name = HttpHeaderNames.Via;
        replacement.Value = written.Bytes;
        request.Headers.Fields.Add(replacement);
        request.ViaEntries = parsed.ViaEntries;
    }

    static void NormalizeTargetMetadata(
        HttpRequest request,
        HttpRequestTargetForm form)
    {
        if (form == HttpRequestTargetForm.Absolute)
        {
            UriValidationResult parsed = UriCodec.ValidateUriReference(request.Target, int.MaxValue);
            request.Scheme = parsed.Scheme;
            request.Authority = parsed.HasAuthority ? parsed.Authority : null;
            request.Path = parsed.Path;
            request.HasQuery = parsed.HasQuery;
            request.Query = parsed.HasQuery ? parsed.Query : null;
            if (request.ProtocolVersion == HttpProtocolVersion.Http11 && parsed.HasAuthority)
            {
                ReplaceHost(request.Headers, parsed.Authority);
            }

            return;
        }

        if (form == HttpRequestTargetForm.Authority)
        {
            request.Authority = request.Target;
            request.Path = null;
            request.HasQuery = false;
            request.Query = null;
            return;
        }

        if (request.Authority == null)
        {
            request.Authority = ReadSingleHeaderText(request.Headers, HttpHeaderNames.Host);
        }
        else if (request.ProtocolVersion == HttpProtocolVersion.Http11)
        {
            ReplaceHost(request.Headers, request.Authority);
        }

        if (form == HttpRequestTargetForm.Asterisk)
        {
            request.Path = null;
            request.HasQuery = false;
            request.Query = null;
            return;
        }

        int query = FindCharacter(request.Target, '?');
        if (query < 0)
        {
            request.Path = request.Target;
            request.HasQuery = false;
            request.Query = null;
        }
        else
        {
            request.Path = request.Target.Substring(0, query);
            request.HasQuery = true;
            request.Query = request.Target.Substring(query + 1);
        }
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
        result.HasContinueExpectation = source.HasContinueExpectation;
        result.ShouldSendContinue = source.ShouldSendContinue;
        result.HasUnsupportedExpectations = source.HasUnsupportedExpectations;
        result.Content = CopyBytes(source.Content);
        result.Trailers = CopyHeaders(source.Trailers);
        result.Priority = source.Priority == null ? null : CopyPriority(source.Priority);
        result.IsComplete = source.IsComplete;

        int index = 0;
        while (index < source.WeightedPreferences.Count)
        {
            result.WeightedPreferences.Add(CopyWeightedValue(
                source.WeightedPreferences[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.LanguagePreferences.Count)
        {
            result.LanguagePreferences.Add(CopyLanguageRange(
                source.LanguagePreferences[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.AuthenticationValues.Count)
        {
            result.AuthenticationValues.Add(CopyAuthenticationValue(
                source.AuthenticationValues[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.ViaEntries.Count)
        {
            result.ViaEntries.Add(CopyViaEntry(source.ViaEntries[index]));
            index = index + 1;
        }

        return result;
    }

    static HttpWeightedValue CopyWeightedValue(HttpWeightedValue source)
    {
        if (source == null || source.Value == null || source.Parameters == null)
        {
            throw CreateProtocolError(
                "invalid-request",
                "The request contains incomplete preference metadata.");
        }

        HttpWeightedValue result = new HttpWeightedValue();
        result.Value = source.Value;
        result.Quality = source.Quality;
        result.SourceOrder = source.SourceOrder;
        result.Specificity = source.Specificity;
        int index = 0;
        while (index < source.Parameters.Count)
        {
            result.Parameters.Add(CopyParameter(source.Parameters[index]));
            index = index + 1;
        }

        return result;
    }

    static HttpLanguageRange CopyLanguageRange(HttpLanguageRange source)
    {
        if (source == null || source.Range == null)
        {
            throw CreateProtocolError(
                "invalid-request",
                "The request contains incomplete language metadata.");
        }

        HttpLanguageRange result = new HttpLanguageRange();
        result.Range = source.Range;
        result.IsWildcard = source.IsWildcard;
        result.Quality = source.Quality;
        result.SourceOrder = source.SourceOrder;
        result.Specificity = source.Specificity;
        return result;
    }

    static HttpAuthenticationValue CopyAuthenticationValue(HttpAuthenticationValue source)
    {
        if (source == null || source.Scheme == null || source.Parameters == null)
        {
            throw CreateProtocolError(
                "invalid-request",
                "The request contains incomplete authentication metadata.");
        }

        HttpAuthenticationValue result = new HttpAuthenticationValue();
        result.Scheme = source.Scheme;
        result.Token68 = source.Token68 == null ? null : CopyBytes(source.Token68);
        int index = 0;
        while (index < source.Parameters.Count)
        {
            result.Parameters.Add(CopyParameter(source.Parameters[index]));
            index = index + 1;
        }

        return result;
    }

    static HttpParameter CopyParameter(HttpParameter source)
    {
        if (source == null || source.Name == null || source.Value == null)
        {
            throw CreateProtocolError(
                "invalid-request",
                "The request contains incomplete parameter metadata.");
        }

        HttpParameter result = new HttpParameter();
        result.Name = source.Name;
        result.HasValue = source.HasValue;
        result.Value = CopyBytes(source.Value);
        result.IsQuoted = source.IsQuoted;
        return result;
    }

    static HttpPriority CopyPriority(HttpPriority source)
    {
        if (source.UnknownParameters == null)
        {
            throw CreateProtocolError(
                "invalid-request",
                "The request contains incomplete priority metadata.");
        }

        HttpPriority result = new HttpPriority();
        result.HasUrgency = source.HasUrgency;
        result.Urgency = source.Urgency;
        result.HasIncremental = source.HasIncremental;
        result.Incremental = source.Incremental;
        result.StructuredValue = source.StructuredValue == null ?
            null : CopyStructuredValue(source.StructuredValue);
        int index = 0;
        while (index < source.UnknownParameters.Count)
        {
            result.UnknownParameters.Add(CopyStructuredMember(
                source.UnknownParameters[index]));
            index = index + 1;
        }

        return result;
    }

    static StructuredFieldValue CopyStructuredValue(StructuredFieldValue source)
    {
        if (source.Members == null)
        {
            throw CreateProtocolError(
                "invalid-request",
                "The request contains incomplete structured field metadata.");
        }

        StructuredFieldValue result = new StructuredFieldValue();
        result.IsItem = source.IsItem;
        result.IsList = source.IsList;
        result.IsDictionary = source.IsDictionary;
        result.Item = source.Item == null ? null : CopyStructuredMember(source.Item);
        int index = 0;
        while (index < source.Members.Count)
        {
            result.Members.Add(CopyStructuredMember(source.Members[index]));
            index = index + 1;
        }

        return result;
    }

    static StructuredFieldMember CopyStructuredMember(StructuredFieldMember source)
    {
        if (source == null || source.ByteSequenceValue == null ||
            source.InnerList == null || source.Parameters == null)
        {
            throw CreateProtocolError(
                "invalid-request",
                "The request contains incomplete structured field metadata.");
        }

        StructuredFieldMember result = new StructuredFieldMember();
        result.ValueKind = source.ValueKind;
        result.TextValue = source.TextValue;
        result.ByteSequenceValue = CopyBytes(source.ByteSequenceValue);
        result.IntegerValue = source.IntegerValue;
        result.DecimalThousandths = source.DecimalThousandths;
        result.BooleanValue = source.BooleanValue;
        result.DateSeconds = source.DateSeconds;
        result.IsInnerList = source.IsInnerList;
        result.DictionaryKey = source.DictionaryKey;
        result.HasExplicitDictionaryValue = source.HasExplicitDictionaryValue;
        int index = 0;
        while (index < source.InnerList.Count)
        {
            result.InnerList.Add(CopyStructuredMember(source.InnerList[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.Parameters.Count)
        {
            result.Parameters.Add(CopyStructuredParameter(source.Parameters[index]));
            index = index + 1;
        }

        return result;
    }

    static StructuredFieldParameter CopyStructuredParameter(
        StructuredFieldParameter source)
    {
        if (source == null || source.Name == null)
        {
            throw CreateProtocolError(
                "invalid-request",
                "The request contains incomplete structured field metadata.");
        }

        StructuredFieldParameter result = new StructuredFieldParameter();
        result.Name = source.Name;
        result.BareItem = source.BareItem == null ?
            null : CopyStructuredMember(source.BareItem);
        return result;
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

    static HttpViaEntry CopyViaEntry(HttpViaEntry source)
    {
        if (source == null || source.ReceivedProtocol == null || source.ReceivedBy == null)
        {
            throw CreateProtocolError(
                "invalid-request",
                "The request contains incomplete Via metadata.");
        }

        HttpViaEntry result = new HttpViaEntry();
        result.ReceivedProtocol = source.ReceivedProtocol;
        result.ReceivedBy = source.ReceivedBy;
        result.HasPort = source.HasPort;
        result.Port = source.Port;
        result.Comment = source.Comment == null ? null : CopyBytes(source.Comment);
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

    static void RemoveNamedFields(HttpHeaders headers, List<string> names)
    {
        int index = 0;
        while (index < headers.Fields.Count)
        {
            if (ContainsName(names, headers.Fields[index].Name))
            {
                headers.Fields.RemoveAt(index);
            }
            else
            {
                index = index + 1;
            }
        }
    }

    static void AddUniqueName(List<string> names, string name)
    {
        if (!ContainsName(names, name))
        {
            names.Add(name);
        }
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

    static HttpHeaderField CreateTextField(string name, string value)
    {
        HttpHeaderField result = new HttpHeaderField();
        result.Name = name;
        result.Value = Encoding.ASCII.GetBytes(value);
        return result;
    }

    static void ReplaceHost(HttpHeaders headers, string authority)
    {
        HttpHeaderField host = CreateTextField(HttpHeaderNames.Host, authority);
        HeaderStore.ReplaceFields(headers, host);
    }

    static string? ReadSingleHeaderText(HttpHeaders headers, string name)
    {
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(headers, name);
        if (fields.Count == 0)
        {
            return null;
        }

        byte[] value = TrimOptionalWhitespace(fields[0].Value);
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0x7f)
            {
                throw CreateProtocolError(
                    "invalid-forwarding-field",
                    "A forwarding field is not ASCII.");
            }

            index = index + 1;
        }

        return Encoding.ASCII.GetString(value);
    }

    static bool IsSafeOriginUri(UriValidationResult value)
    {
        return value.Status == HttpOperationStatus.Done && value.IsAbsolute &&
            value.HasAuthority && !value.HasUserInfo && value.Host.Length != 0 &&
            !value.HasFragment &&
            (Ascii.EqualsStringsIgnoreCase(value.Scheme, "http") ||
            Ascii.EqualsStringsIgnoreCase(value.Scheme, "https"));
    }

    static int FindCharacter(string value, char character)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == character)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static void ValidateHeaders(HttpHeaders headers)
    {
        if (headers == null || headers.Fields == null)
        {
            throw CreateProtocolError(
                "invalid-headers",
                "The header collection is incomplete.");
        }

        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (field == null || !HeaderStore.IsValidName(field.Name) ||
                !HeaderStore.IsValidValue(field.Value))
            {
                throw CreateProtocolError(
                    "invalid-headers",
                    "The header collection contains a malformed field.");
            }

            index = index + 1;
        }
    }

    static void ValidateRequest(HttpRequest request)
    {
        if (request == null || request.Content == null || request.Trailers == null ||
            request.WeightedPreferences == null || request.LanguagePreferences == null ||
            request.AuthenticationValues == null || request.ViaEntries == null)
        {
            throw CreateProtocolError(
                "invalid-request",
                "The HTTP request contains incomplete message data.");
        }

        ValidateHeaders(request.Headers);
        ValidateHeaders(request.Trailers);
    }

    static HttpProtocolException CreateProtocolError(string code, string message)
    {
        return new HttpProtocolException(message, code);
    }
}
