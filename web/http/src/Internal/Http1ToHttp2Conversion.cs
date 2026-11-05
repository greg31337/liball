using System;
using System.Collections.Generic;
using System.Text;

namespace http;

internal class Http1ToHttp2FilterResult
{
    internal bool IsValid = true;
    internal HttpHeaders Headers = new HttpHeaders();
    internal HttpHeaders Trailers = new HttpHeaders();
    internal string ErrorCodeName = "";
    internal string ErrorMessage = "";
}

internal class Http1ToHttp2ControlResult
{
    internal bool IsValid = true;
    internal HttpRequestTargetForm TargetForm;
    internal string Scheme = "";
    internal string? Authority;
    internal string Target = "";
    internal string? Path;
    internal bool HasQuery;
    internal string? Query;
    internal string ErrorCodeName = "";
    internal string ErrorMessage = "";
}

internal static class Http1ToHttp2Conversion
{
    internal static Http2MessageResult Convert(
        HttpMessageKind messageKind,
        HttpRequest? request,
        HttpResponse? response,
        HttpLimits limits)
    {
        if (messageKind == HttpMessageKind.Request)
        {
            if (request == null || response != null)
            {
                return RejectConversion(
                    messageKind,
                    "invalid-http1-request-conversion-input",
                    "Request conversion requires only a common HTTP/1 request.");
            }

            return ConvertRequest(request, limits);
        }

        if (messageKind == HttpMessageKind.Response ||
            messageKind == HttpMessageKind.InformationalResponse)
        {
            if (response == null || request != null)
            {
                return RejectConversion(
                    messageKind,
                    "invalid-http1-response-conversion-input",
                    "Response conversion requires only a common HTTP/1 response.");
            }

            return ConvertResponse(messageKind, response, limits);
        }

        return RejectConversion(
            messageKind,
            "invalid-http1-conversion-kind",
            "Only HTTP/1 requests and responses can be converted to HTTP/2.");
    }

    internal static Http2MessageResult ConvertRequest(
        HttpRequest source,
        HttpLimits limits)
    {
        if (!HasRequestStructure(source) || !IsHttp1Version(source.ProtocolVersion))
        {
            return RejectConversion(
                HttpMessageKind.Request,
                "invalid-http1-request-conversion-input",
                "A complete HTTP/1.0 or HTTP/1.1 request model is required.");
        }

        if (source.Content.Length > limits.MaximumContentSize)
        {
            return RejectConversion(
                HttpMessageKind.Request,
                "http1-content-limit-exceeded",
                "The HTTP/1 request content exceeds the configured limit.");
        }

        Http1ToHttp2FilterResult filtered = FilterFields(
            source.Headers, source.Trailers, true,
            source.ProtocolVersion, limits);
        if (!filtered.IsValid)
        {
            return RejectConversion(
                HttpMessageKind.Request,
                filtered.ErrorCodeName,
                filtered.ErrorMessage);
        }

        Http1ToHttp2ControlResult control = ResolveRequestControl(source);
        if (!control.IsValid)
        {
            return RejectConversion(
                HttpMessageKind.Request,
                control.ErrorCodeName,
                control.ErrorMessage);
        }

        Http1ParserOptions options = new Http1ParserOptions();
        options.Limits = limits;
        Http1FramingResult framing = Http1Framing.DetermineRequest(
            source, options);
        Http2MessageResult framingError = ValidateRequestFraming(
            source, framing, limits);
        if (framingError.Status == HttpOperationStatus.Rejected)
        {
            return framingError;
        }

        HttpRequest converted = CopyConvertedRequest(
            source, filtered, control);
        Http2MessageResult trailers = Http2EncodingSemantics.EncodeTrailers(
            0, converted.Trailers, limits);
        if (trailers.Status != HttpOperationStatus.Done)
        {
            return RejectConversion(
                HttpMessageKind.Request,
                trailers.ErrorCodeName ?? "invalid-http1-request-trailers",
                trailers.ErrorMessage ??
                    "The HTTP/1 request trailers cannot be represented in HTTP/2.");
        }

        converted.Trailers = trailers.Headers;
        bool endStream = source.IsComplete && source.Content.Length == 0 &&
            converted.Trailers.Fields.Count == 0;
        Http2MessageResult result = Http2EncodingSemantics.EncodeRequest(
            0, converted, endStream, limits);
        if (result.Status != HttpOperationStatus.Done)
        {
            return result;
        }

        converted.Headers = CreateCommonHeaders(result.Headers);
        CompleteConversionResult(
            result, converted.Content, converted.Trailers,
            source.IsComplete);
        return result;
    }

    internal static Http2MessageResult ConvertResponse(
        HttpMessageKind messageKind,
        HttpResponse source,
        HttpLimits limits)
    {
        if (!HasResponseStructure(source) || !IsHttp1Version(source.ProtocolVersion))
        {
            return RejectConversion(
                messageKind,
                "invalid-http1-response-conversion-input",
                "A complete HTTP/1.0 or HTTP/1.1 response model is required.");
        }

        bool informational = source.StatusCode >= 100 &&
            source.StatusCode <= 199;
        if (source.StatusCode < 100 || source.StatusCode > 999 ||
            source.IsInformational != informational ||
            informational && messageKind != HttpMessageKind.InformationalResponse ||
            !informational && messageKind != HttpMessageKind.Response)
        {
            return RejectConversion(
                messageKind,
                "inconsistent-http1-response-conversion-kind",
                "The response status, informational flag, and message kind must agree.");
        }

        if (!IsValidReasonPhrase(source))
        {
            return RejectConversion(
                messageKind,
                "invalid-http1-reason-phrase",
                "The HTTP/1 reason phrase contains a prohibited control octet.");
        }

        if (source.Content.Length > limits.MaximumContentSize)
        {
            return RejectConversion(
                messageKind,
                "http1-content-limit-exceeded",
                "The HTTP/1 response content exceeds the configured limit.");
        }

        Http1ToHttp2FilterResult filtered = FilterFields(
            source.Headers, source.Trailers, false,
            source.ProtocolVersion, limits);
        if (!filtered.IsValid)
        {
            return RejectConversion(
                messageKind,
                filtered.ErrorCodeName,
                filtered.ErrorMessage);
        }

        Http1ParserOptions options = new Http1ParserOptions();
        options.Limits = limits;
        Http1FramingResult parsed = ParseResponseFraming(source, options);
        Http1FramingResult framing = Http1Framing.DetermineResponse(
            source, options);
        Http2MessageResult framingError = ValidateResponseFraming(
            messageKind, source, parsed, framing, limits);
        if (framingError.Status == HttpOperationStatus.Rejected)
        {
            return framingError;
        }

        HttpResponse converted = CopyConvertedResponse(source, filtered);
        Http2MessageResult trailers = Http2EncodingSemantics.EncodeTrailers(
            0, converted.Trailers, limits);
        if (trailers.Status != HttpOperationStatus.Done)
        {
            return RejectConversion(
                messageKind,
                trailers.ErrorCodeName ?? "invalid-http1-response-trailers",
                trailers.ErrorMessage ??
                    "The HTTP/1 response trailers cannot be represented in HTTP/2.");
        }

        converted.Trailers = trailers.Headers;
        bool endStream = !informational && source.IsComplete &&
            source.Content.Length == 0 && converted.Trailers.Fields.Count == 0;
        Http2MessageResult result = Http2EncodingSemantics.EncodeResponse(
            0, converted, endStream, limits);
        if (result.Status != HttpOperationStatus.Done)
        {
            return result;
        }

        converted.Headers = CreateCommonHeaders(result.Headers);
        CompleteConversionResult(
            result, converted.Content, converted.Trailers,
            source.IsComplete);
        return result;
    }

    internal static Http1ToHttp2FilterResult FilterFields(
        HttpHeaders headers,
        HttpHeaders trailers,
        bool request,
        HttpProtocolVersion version,
        HttpLimits limits)
    {
        Http1ToHttp2FilterResult result = new Http1ToHttp2FilterResult();
        if (!ValidateSourceFields(headers) || !ValidateSourceFields(trailers))
        {
            return RejectFilter(
                result,
                "invalid-http1-conversion-field",
                "The HTTP/1 message contains an incomplete or malformed field.");
        }

        List<string> hopNames = CreateFixedHopNames();
        List<HttpHeaderField> connectionFields =
            HeaderStore.FindMatchingFields(headers, HttpHeaderNames.Connection);
        if (connectionFields.Count != 0)
        {
            byte[] combined = HeaderStore.CombineMatchingValues(
                headers, HttpHeaderNames.Connection);
            FieldValueParseResult parsed = FieldValueParser.ParseConnection(
                combined, combined.Length, limits.MaximumListMemberCount);
            if (parsed.Status != HttpOperationStatus.Done)
            {
                return RejectFilter(
                    result,
                    "invalid-http1-connection-field",
                    "The HTTP/1 Connection field is malformed.");
            }

            int optionIndex = 0;
            while (optionIndex < parsed.Values.Count)
            {
                string option = Encoding.ASCII.GetString(
                    parsed.Values[optionIndex]);
                if (Ascii.EqualsStringsIgnoreCase(
                        option, HttpHeaderNames.ContentLength) ||
                    Ascii.EqualsStringsIgnoreCase(
                        option, HttpHeaderNames.TransferEncoding))
                {
                    return RejectFilter(
                        result,
                        "http1-connection-framing-option",
                        "Connection cannot nominate a message framing field.");
                }

                AddHopName(hopNames, option);
                optionIndex = optionIndex + 1;
            }
        }

        bool retainTe = false;
        bool teSensitive = false;
        bool teNeverIndex = false;
        if (request && version != HttpProtocolVersion.Http10)
        {
            Http1ToHttp2FilterResult teResult = ReadTeSignal(
                headers, limits);
            if (!teResult.IsValid)
            {
                return teResult;
            }

            retainTe = teResult.Headers.Fields.Count != 0;
            if (retainTe)
            {
                teSensitive = teResult.Headers.Fields[0].IsSensitive;
                teNeverIndex = teResult.Headers.Fields[0].NeverIndex;
            }
        }

        CopyFilteredFields(
            headers, result.Headers, hopNames, request,
            retainTe, teSensitive, teNeverIndex);
        CopyFilteredTrailers(trailers, result.Trailers, hopNames);
        return result;
    }

    internal static Http1ToHttp2FilterResult ReadTeSignal(
        HttpHeaders headers,
        HttpLimits limits)
    {
        Http1ToHttp2FilterResult result = new Http1ToHttp2FilterResult();
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(
            headers, HttpHeaderNames.TE);
        if (fields.Count == 0)
        {
            return result;
        }

        byte[] combined = HeaderStore.CombineMatchingValues(
            headers, HttpHeaderNames.TE);
        MediaTypeParseResult parsed = FieldValueParser.ParseTe(
            combined,
            combined.Length,
            limits.MaximumListMemberCount,
            limits.MaximumParameterCount);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            return RejectFilter(
                result,
                "invalid-http1-te-field",
                "The HTTP/1 TE field is malformed.");
        }

        bool trailers = false;
        bool sensitive = false;
        bool neverIndex = false;
        int index = 0;
        while (index < fields.Count)
        {
            sensitive = sensitive || fields[index].IsSensitive;
            neverIndex = neverIndex || fields[index].NeverIndex;
            index = index + 1;
        }

        index = 0;
        while (index < parsed.Values.Count)
        {
            if (Ascii.EqualsStringsIgnoreCase(
                parsed.Values[index].Value, HttpHeaderValues.Trailers))
            {
                trailers = true;
            }

            index = index + 1;
        }

        if (trailers)
        {
            HttpHeaderField field = CreateField(
                "te", Encoding.ASCII.GetBytes(HttpHeaderValues.Trailers));
            field.IsSensitive = sensitive;
            field.NeverIndex = neverIndex;
            result.Headers.Fields.Add(field);
        }

        return result;
    }

    internal static Http1ToHttp2ControlResult ResolveRequestControl(
        HttpRequest source)
    {
        Http1ToHttp2ControlResult result = new Http1ToHttp2ControlResult();
        if (!Http2RequestSemantics.IsValidMethod(source.Method) ||
            !IsAscii(source.Target))
        {
            return RejectControl(
                result,
                "invalid-http1-request-line",
                "The HTTP/1 request method and target must use valid ASCII syntax.");
        }

        string version = source.ProtocolVersion == HttpProtocolVersion.Http10 ?
            HttpProtocolNames.Http10 : HttpProtocolNames.Http11;
        string line = source.Method + " " + source.Target + " " + version;
        Http1RequestLineResult parsedLine = Http1LineParser.ParseRequestLine(
            Encoding.ASCII.GetBytes(line));
        if (parsedLine.Status != HttpOperationStatus.Done ||
            parsedLine.Request == null)
        {
            return RejectControl(
                result,
                parsedLine.ErrorCode ?? "invalid-http1-request-line",
                parsedLine.ErrorMessage ?? "The HTTP/1 request line is malformed.");
        }

        HttpRequest parsed = parsedLine.Request;
        Http1ToHttp2ControlResult hostResult = ReadRequestHost(source);
        if (!hostResult.IsValid)
        {
            return hostResult;
        }

        string? host = hostResult.Authority;
        result.TargetForm = parsed.TargetForm;
        result.Path = parsed.Path;
        result.HasQuery = parsed.HasQuery;
        result.Query = parsed.Query;
        if (parsed.TargetForm == HttpRequestTargetForm.Authority)
        {
            if (source.ConnectProtocol != null ||
                source.Authority != null &&
                !RequestTargetSemantics.AreAuthoritiesEquivalent(
                    source.Authority, parsed.Authority ?? "", "") ||
                host != null && !RequestTargetSemantics.AreAuthoritiesEquivalent(
                    host, parsed.Authority ?? "", ""))
            {
                return RejectControl(
                    result,
                    "conflicting-http1-request-authority",
                    "The CONNECT target, authority, and Host field must agree.");
            }

            result.Authority = parsed.Authority;
            result.Target = source.Target;
            return result;
        }

        if (source.ConnectProtocol != null)
        {
            return RejectControl(
                result,
                "http1-extended-connect-not-supported",
                "HTTP/1 cannot directly carry an extended CONNECT protocol pseudo-field.");
        }

        if (parsed.TargetForm == HttpRequestTargetForm.Absolute)
        {
            result.Scheme = parsed.Scheme ?? "";
            result.Authority = parsed.Authority;
            if (source.Authority != null &&
                (result.Authority == null ||
                    !RequestTargetSemantics.AreAuthoritiesEquivalent(
                        source.Authority, result.Authority, result.Scheme)) ||
                host != null &&
                (result.Authority == null ||
                    !RequestTargetSemantics.AreAuthoritiesEquivalent(
                        host, result.Authority, result.Scheme)))
            {
                return RejectControl(
                    result,
                    "conflicting-http1-request-authority",
                    "The absolute target, authority, and Host field must agree.");
            }

            result.Target = BuildRequestPath(parsed);
            if (result.Path == null || result.Path.Length == 0)
            {
                result.Path = "/";
            }

            result.TargetForm = HttpRequestTargetForm.Origin;
            return result;
        }

        result.Scheme = source.Scheme ?? "";
        if (source.Authority != null && host != null &&
            !RequestTargetSemantics.AreAuthoritiesEquivalent(
                source.Authority, host, result.Scheme))
        {
            return RejectControl(
                result,
                "conflicting-http1-request-authority",
                "The request authority and Host field must agree.");
        }

        result.Authority = source.Authority ?? host;
        result.Target = source.Target;
        return result;
    }

    internal static Http1ToHttp2ControlResult ReadRequestHost(
        HttpRequest source)
    {
        Http1ToHttp2ControlResult result = new Http1ToHttp2ControlResult();
        int count = 0;
        int index = 0;
        while (index < source.Headers.Fields.Count)
        {
            HttpHeaderField field = source.Headers.Fields[index];
            if (Ascii.EqualsStringsIgnoreCase(field.Name, HttpHeaderNames.Host))
            {
                count = count + 1;
                if (!Ascii.IsValid(field.Value))
                {
                    return RejectControl(
                        result,
                        "invalid-http1-host-field",
                        "The HTTP/1 Host field must contain ASCII authority data.");
                }

                result.Authority = Encoding.ASCII.GetString(field.Value);
            }

            index = index + 1;
        }

        if (count > 1 ||
            source.ProtocolVersion == HttpProtocolVersion.Http11 && count != 1)
        {
            return RejectControl(
                result,
                count > 1 ? "multiple-http1-host-fields" :
                    "missing-http1-host-field",
                "An HTTP/1.1 request requires exactly one Host field.");
        }

        if (result.Authority != null && result.Authority.Length != 0 &&
            !Http2RequestSemantics.IsValidAuthority(
                result.Authority, source.Scheme ?? "", false))
        {
            return RejectControl(
                result,
                "invalid-http1-host-field",
                "The HTTP/1 Host field contains malformed authority data.");
        }

        return result;
    }

    internal static Http2MessageResult ValidateRequestFraming(
        HttpRequest source,
        Http1FramingResult framing,
        HttpLimits limits)
    {
        if (!framing.Decision.IsValid)
        {
            return RejectConversion(
                HttpMessageKind.Request,
                framing.ErrorCode ?? "invalid-http1-request-framing",
                framing.ErrorMessage ?? "The HTTP/1 request framing is invalid.");
        }

        if (!HasSupportedTransferCoding(framing.Decision))
        {
            return RejectConversion(
                HttpMessageKind.Request,
                "unsupported-http1-transfer-coding-conversion",
                "Only a final chunked transfer coding can be removed during conversion.");
        }

        if (framing.Decision.BodyFraming == Http1BodyFraming.Chunked)
        {
            Http1TrailerValidationResult declarations =
                Http1TrailerValidator.ValidateDeclarations(
                    source.Headers, limits);
            if (declarations.Status != HttpOperationStatus.Done)
            {
                return RejectConversion(
                    HttpMessageKind.Request,
                    declarations.ErrorCode ??
                        "invalid-http1-trailer-declaration",
                    declarations.ErrorMessage ??
                        "The HTTP/1 trailer declaration is invalid.");
            }
        }

        if (framing.Decision.BodyFraming == Http1BodyFraming.None &&
            source.Content.Length != 0 ||
            framing.Decision.BodyFraming == Http1BodyFraming.ContentLength &&
            framing.Decision.DeclaredLength != source.Content.Length)
        {
            return RejectConversion(
                HttpMessageKind.Request,
                "http1-content-length-mismatch",
                "The retained request content does not match its HTTP/1 framing.");
        }

        if (source.Trailers.Fields.Count != 0 &&
            framing.Decision.BodyFraming != Http1BodyFraming.Chunked)
        {
            return RejectConversion(
                HttpMessageKind.Request,
                "http1-trailers-require-chunked-framing",
                "HTTP/1 request trailers require chunked framing.");
        }

        return new Http2MessageResult();
    }

    internal static Http1FramingResult ParseResponseFraming(
        HttpResponse source,
        Http1ParserOptions options)
    {
        HttpResponse probe = new HttpResponse();
        probe.ProtocolVersion = source.ProtocolVersion;
        probe.StatusCode = HttpStatusCodes.Ok;
        probe.Headers = source.Headers;
        return Http1Framing.DetermineResponse(probe, options);
    }

    internal static Http2MessageResult ValidateResponseFraming(
        HttpMessageKind messageKind,
        HttpResponse source,
        Http1FramingResult parsed,
        Http1FramingResult framing,
        HttpLimits limits)
    {
        if (!parsed.Decision.IsValid || !framing.Decision.IsValid)
        {
            Http1FramingResult failure = parsed.Decision.IsValid ? framing : parsed;
            return RejectConversion(
                messageKind,
                failure.ErrorCode ?? "invalid-http1-response-framing",
                failure.ErrorMessage ?? "The HTTP/1 response framing is invalid.");
        }

        bool strictNoBody = source.IsInformational ||
            source.StatusCode == HttpStatusCodes.NoContent;
        if (strictNoBody &&
            (HasField(source.Headers, HttpHeaderNames.ContentLength) ||
                HasField(source.Headers, HttpHeaderNames.TransferEncoding)))
        {
            return RejectConversion(
                messageKind,
                "http1-response-framing-field-not-allowed",
                "This HTTP/1 response cannot contain a framing field.");
        }

        if (!HasSupportedTransferCoding(parsed.Decision))
        {
            return RejectConversion(
                messageKind,
                "unsupported-http1-transfer-coding-conversion",
                "Only a final chunked transfer coding can be removed during conversion.");
        }

        if (parsed.Decision.BodyFraming == Http1BodyFraming.Chunked)
        {
            Http1TrailerValidationResult declarations =
                Http1TrailerValidator.ValidateDeclarations(
                    source.Headers, limits);
            if (declarations.Status != HttpOperationStatus.Done)
            {
                return RejectConversion(
                    messageKind,
                    declarations.ErrorCode ??
                        "invalid-http1-trailer-declaration",
                    declarations.ErrorMessage ??
                        "The HTTP/1 trailer declaration is invalid.");
            }
        }

        bool metadataLength =
            source.StatusCode == HttpStatusCodes.NotModified;
        if (!metadataLength &&
            parsed.Decision.BodyFraming == Http1BodyFraming.ContentLength &&
            parsed.Decision.DeclaredLength != source.Content.Length)
        {
            return RejectConversion(
                messageKind,
                "http1-content-length-mismatch",
                "The retained response content does not match its HTTP/1 framing.");
        }

        if (framing.Decision.HasNoBody && source.Content.Length != 0)
        {
            return RejectConversion(
                messageKind,
                "http1-response-content-not-allowed",
                "The response status does not permit retained content.");
        }

        if (source.Trailers.Fields.Count != 0 &&
            parsed.Decision.BodyFraming != Http1BodyFraming.Chunked)
        {
            return RejectConversion(
                messageKind,
                "http1-trailers-require-chunked-framing",
                "HTTP/1 response trailers require chunked framing.");
        }

        return new Http2MessageResult();
    }

    internal static HttpRequest CopyConvertedRequest(
        HttpRequest source,
        Http1ToHttp2FilterResult filtered,
        Http1ToHttp2ControlResult control)
    {
        HttpRequest result = new HttpRequest();
        result.ProtocolVersion = HttpProtocolVersion.Http2;
        result.Method = source.Method;
        result.MethodMetadata = MethodSemantics.Describe(source.Method);
        result.Target = control.Target;
        result.TargetForm = control.TargetForm;
        result.Scheme = control.TargetForm == HttpRequestTargetForm.Authority ?
            null : control.Scheme;
        result.Authority = control.Authority;
        result.Path = control.Path;
        result.HasQuery = control.HasQuery;
        result.Query = control.Query;
        result.Headers = filtered.Headers;
        result.Content = CopyBytes(source.Content);
        result.Trailers = filtered.Trailers;
        result.Priority = source.Priority;
        result.HasContinueExpectation = source.HasContinueExpectation;
        result.ShouldSendContinue = source.ShouldSendContinue;
        result.HasUnsupportedExpectations = source.HasUnsupportedExpectations;
        result.IsComplete = source.IsComplete;
        CopyRequestLists(source, result);
        return result;
    }

    internal static HttpResponse CopyConvertedResponse(
        HttpResponse source,
        Http1ToHttp2FilterResult filtered)
    {
        HttpResponse result = new HttpResponse();
        result.ProtocolVersion = HttpProtocolVersion.Http2;
        result.StatusCode = source.StatusCode;
        result.HasReasonPhrase = false;
        result.ReasonPhrase = Array.Empty<byte>();
        result.Headers = filtered.Headers;
        result.Content = CopyBytes(source.Content);
        result.Trailers = filtered.Trailers;
        result.IsInformational = source.IsInformational;
        result.IsComplete = source.IsComplete;
        return result;
    }

    internal static void CopyRequestLists(
        HttpRequest source,
        HttpRequest target)
    {
        int index = 0;
        while (index < source.WeightedPreferences.Count)
        {
            target.WeightedPreferences.Add(source.WeightedPreferences[index]);
            index = index + 1;
        }

        index = 0;
        while (index < source.LanguagePreferences.Count)
        {
            target.LanguagePreferences.Add(source.LanguagePreferences[index]);
            index = index + 1;
        }

        index = 0;
        while (index < source.AuthenticationValues.Count)
        {
            target.AuthenticationValues.Add(source.AuthenticationValues[index]);
            index = index + 1;
        }

        index = 0;
        while (index < source.ViaEntries.Count)
        {
            target.ViaEntries.Add(source.ViaEntries[index]);
            index = index + 1;
        }
    }

    internal static void CopyFilteredFields(
        HttpHeaders source,
        HttpHeaders target,
        List<string> hopNames,
        bool request,
        bool retainTe,
        bool teSensitive,
        bool teNeverIndex)
    {
        bool teAdded = false;
        int index = 0;
        while (index < source.Fields.Count)
        {
            HttpHeaderField field = source.Fields[index];
            bool te = Ascii.EqualsStringsIgnoreCase(
                field.Name, HttpHeaderNames.TE);
            bool host = request && Ascii.EqualsStringsIgnoreCase(
                field.Name, HttpHeaderNames.Host);
            if (te && retainTe && !teAdded)
            {
                HttpHeaderField retained = CreateField(
                    "te", Encoding.ASCII.GetBytes(HttpHeaderValues.Trailers));
                retained.IsSensitive = teSensitive;
                retained.NeverIndex = teNeverIndex;
                target.Fields.Add(retained);
                teAdded = true;
            }
            else if (!te && !host && !ContainsHopName(hopNames, field.Name))
            {
                target.Fields.Add(CopyField(field));
            }

            index = index + 1;
        }
    }

    internal static void CopyFilteredTrailers(
        HttpHeaders source,
        HttpHeaders target,
        List<string> hopNames)
    {
        int index = 0;
        while (index < source.Fields.Count)
        {
            HttpHeaderField field = source.Fields[index];
            if (!ContainsHopName(hopNames, field.Name) &&
                !Ascii.EqualsStringsIgnoreCase(field.Name, HttpHeaderNames.TE))
            {
                target.Fields.Add(CopyField(field));
            }

            index = index + 1;
        }
    }

    internal static bool ValidateSourceFields(HttpHeaders headers)
    {
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (field == null || field.Name == null || field.Value == null ||
                !HeaderStore.IsValidName(field.Name) ||
                !HeaderStore.IsValidValue(field.Value))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool HasSupportedTransferCoding(
        Http1FramingDecision framing)
    {
        if (framing.TransferCodings.Count == 0)
        {
            return true;
        }

        return framing.TransferCodings.Count == 1 &&
            Ascii.EqualsStringsIgnoreCase(
                framing.TransferCodings[0], HttpTransferCodings.Chunked);
    }

    internal static List<string> CreateFixedHopNames()
    {
        List<string> names = new List<string>();
        names.Add(HttpHeaderNames.Connection);
        names.Add("Proxy-Connection");
        names.Add(HttpHeaderNames.KeepAlive);
        names.Add(HttpHeaderNames.TransferEncoding);
        names.Add(HttpHeaderNames.Upgrade);
        return names;
    }

    internal static void AddHopName(List<string> names, string name)
    {
        if (!ContainsHopName(names, name))
        {
            names.Add(name);
        }
    }

    internal static bool ContainsHopName(List<string> names, string name)
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

    internal static bool HasField(HttpHeaders headers, string name)
    {
        int index = 0;
        while (index < headers.Fields.Count)
        {
            if (Ascii.EqualsStringsIgnoreCase(headers.Fields[index].Name, name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool HasRequestStructure(HttpRequest request)
    {
        return request != null && request.Method != null &&
            request.Target != null && request.Headers != null &&
            request.Headers.Fields != null && request.Content != null &&
            request.Trailers != null && request.Trailers.Fields != null &&
            request.WeightedPreferences != null &&
            request.LanguagePreferences != null &&
            request.AuthenticationValues != null && request.ViaEntries != null;
    }

    internal static bool HasResponseStructure(HttpResponse response)
    {
        return response != null && response.ReasonPhrase != null &&
            response.Headers != null && response.Headers.Fields != null &&
            response.Content != null && response.Trailers != null &&
            response.Trailers.Fields != null;
    }

    internal static bool IsHttp1Version(HttpProtocolVersion version)
    {
        return version == HttpProtocolVersion.Http10 ||
            version == HttpProtocolVersion.Http11;
    }

    internal static bool IsValidReasonPhrase(HttpResponse response)
    {
        if (!response.HasReasonPhrase)
        {
            return true;
        }

        return HeaderStore.IsValidValue(response.ReasonPhrase);
    }

    internal static bool IsAscii(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0x7f)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string BuildRequestPath(HttpRequest parsed)
    {
        string path = parsed.Path ?? "";
        if (path.Length == 0)
        {
            path = "/";
        }

        if (parsed.HasQuery)
        {
            path = path + "?" + (parsed.Query ?? "");
        }

        return path;
    }

    internal static HttpHeaderField CopyField(HttpHeaderField source)
    {
        HttpHeaderField result = CreateField(
            source.Name, CopyBytes(source.Value));
        result.IsSensitive = source.IsSensitive;
        result.NeverIndex = source.NeverIndex;
        return result;
    }

    internal static HttpHeaderField CreateField(string name, byte[] value)
    {
        HttpHeaderField result = new HttpHeaderField();
        result.Name = name;
        result.Value = value;
        return result;
    }

    internal static byte[] CopyBytes(byte[] source)
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

    internal static HttpHeaders CreateCommonHeaders(HttpHeaders encoded)
    {
        HttpHeaders result = new HttpHeaders();
        int index = 0;
        while (index < encoded.Fields.Count)
        {
            HttpHeaderField field = encoded.Fields[index];
            if (field.Name.Length == 0 || field.Name[0] != ':')
            {
                result.Fields.Add(CopyField(field));
            }

            index = index + 1;
        }

        Http2CookieSemantics.CombineDecodedCookies(result);
        return result;
    }

    internal static void CompleteConversionResult(
        Http2MessageResult result,
        byte[] content,
        HttpHeaders trailers,
        bool complete)
    {
        result.Content = content;
        if (trailers.Fields.Count != 0)
        {
            result.Trailers = trailers;
        }

        result.IsComplete = complete;
    }

    internal static Http1ToHttp2FilterResult RejectFilter(
        Http1ToHttp2FilterResult result,
        string errorCodeName,
        string errorMessage)
    {
        result.IsValid = false;
        result.ErrorCodeName = errorCodeName;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static Http1ToHttp2ControlResult RejectControl(
        Http1ToHttp2ControlResult result,
        string errorCodeName,
        string errorMessage)
    {
        result.IsValid = false;
        result.ErrorCodeName = errorCodeName;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static Http2MessageResult RejectConversion(
        HttpMessageKind messageKind,
        string errorCodeName,
        string errorMessage)
    {
        Http2MessageResult result = new Http2MessageResult();
        result.Status = HttpOperationStatus.Rejected;
        result.MessageKind = messageKind;
        result.ErrorCodeName = errorCodeName;
        result.ErrorMessage = errorMessage;
        return result;
    }
}
