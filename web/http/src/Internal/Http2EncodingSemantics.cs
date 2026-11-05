using System;
using System.Globalization;
using System.Text;

namespace http;

internal class Http2EncodingFieldResult
{
    internal bool IsValid = true;
    internal HttpHeaders Headers = new HttpHeaders();
    internal string ErrorCodeName = "";
    internal string ErrorMessage = "";
}

internal static class Http2EncodingSemantics
{
    internal static Http2MessageResult EncodeRequest(
        int streamId,
        HttpRequest request,
        bool endStream,
        HttpLimits limits)
    {
        Http2MessageResult result = CreateEncodingResult(
            streamId, HttpMessageKind.Request, endStream);
        if (!HasRequestParts(request))
        {
            return RejectEncoding(
                result,
                "invalid-http2-request",
                "A complete common request model is required for HTTP/2 encoding.");
        }

        Http2EncodingFieldResult regular = NormalizeFields(
            request.Headers, true);
        if (!regular.IsValid)
        {
            return RejectEncoding(
                result, regular.ErrorCodeName, regular.ErrorMessage);
        }

        string method = request.Method;
        string scheme = request.Scheme ?? "";
        string authority = request.Authority ?? "";
        string target = request.Target;
        bool extendedConnect = request.ConnectProtocol != null;
        if (!Http2RequestSemantics.IsValidMethod(method))
        {
            return RejectEncoding(
                result,
                "invalid-http2-request-method",
                "The request method must be a non-empty HTTP token.");
        }

        if (method == HttpMethods.Connect && extendedConnect)
        {
            if (!ValidateExtendedConnectControl(request, scheme, authority, target))
            {
                return RejectEncoding(
                    result,
                    "invalid-http2-extended-connect-pseudo-headers",
                    "Extended CONNECT requires valid protocol, scheme, authority, and path values.");
            }

            AddTextField(result.Headers, ":method", method);
            AddTextField(result.Headers, ":protocol", request.ConnectProtocol ?? "");
            AddTextField(result.Headers, ":scheme", scheme);
            AddTextField(result.Headers, ":authority", authority);
            AddTextField(result.Headers, ":path", target);
            result.IsTunnel = true;
        }
        else if (method == HttpMethods.Connect)
        {
            if (request.ConnectProtocol != null ||
                !Http2RequestSemantics.IsValidAuthority(authority, "", true) ||
                !RequestTargetSemantics.AreAuthoritiesEquivalent(
                    target, authority, ""))
            {
                return RejectEncoding(
                    result,
                    "invalid-http2-connect-pseudo-headers",
                    "Standard CONNECT requires matching authority-form target control data.");
            }

            AddTextField(result.Headers, ":method", method);
            AddTextField(result.Headers, ":authority", authority);
        }
        else
        {
            if (extendedConnect ||
                !Http2RequestSemantics.IsValidScheme(scheme) ||
                !Http2RequestSemantics.IsValidPath(method, target) ||
                request.Authority != null &&
                    !Http2RequestSemantics.IsValidAuthority(
                        authority, scheme, false))
            {
                return RejectEncoding(
                    result,
                    "invalid-http2-request-control-data",
                    "The common request cannot be represented by HTTP/2 pseudo-header fields.");
            }

            AddTextField(result.Headers, ":method", method);
            AddTextField(result.Headers, ":scheme", scheme);
            if (request.Authority != null)
            {
                AddTextField(result.Headers, ":authority", authority);
            }

            AddTextField(result.Headers, ":path", target);
        }

        Http2FieldValidationResult authorityValidation =
            Http2FieldSemantics.ValidateRequestAuthority(
                regular.Headers, request.Authority != null,
                authority, scheme);
        if (!authorityValidation.IsValid)
        {
            return RejectEncoding(
                result,
                authorityValidation.ErrorCodeName,
                authorityValidation.ErrorMessage);
        }

        if (!ValidateRequestContent(request, regular.Headers, endStream, result))
        {
            return result;
        }

        AppendFields(result.Headers, regular.Headers);
        Http2FieldValidationResult validation =
            Http2FieldSemantics.ValidateFields(
                result.Headers, true, limits);
        if (!validation.IsValid)
        {
            return RejectEncoding(
                result, validation.ErrorCodeName, validation.ErrorMessage);
        }

        result.Status = HttpOperationStatus.Done;
        result.Request = request;
        result.IsComplete = endStream;
        return result;
    }

    internal static Http2MessageResult EncodeResponse(
        int streamId,
        HttpResponse response,
        bool endStream,
        HttpLimits limits)
    {
        Http2MessageResult result = CreateEncodingResult(
            streamId, HttpMessageKind.Response, endStream);
        if (!HasResponseParts(response))
        {
            return RejectEncoding(
                result,
                "invalid-http2-response",
                "A complete common response model is required for HTTP/2 encoding.");
        }

        HttpStatusMetadata status = StatusSemantics.Describe(
            response.StatusCode);
        bool informational =
            status.StatusClass == HttpStatusClass.Informational;
        if (response.StatusCode == HttpStatusCodes.SwitchingProtocols)
        {
            return RejectEncoding(
                result,
                "http2-switching-protocols-not-supported",
                "HTTP/2 does not support the 101 Switching Protocols status code.");
        }

        if (response.IsInformational != informational)
        {
            return RejectEncoding(
                result,
                "inconsistent-http2-informational-status",
                "The informational flag must agree with the response status code.");
        }

        if (informational && endStream)
        {
            return RejectEncoding(
                result,
                "http2-informational-end-stream",
                "An informational HTTP/2 response cannot end the stream.");
        }

        Http2EncodingFieldResult regular = NormalizeFields(
            response.Headers, false);
        if (!regular.IsValid)
        {
            return RejectEncoding(
                result, regular.ErrorCodeName, regular.ErrorMessage);
        }

        if (!ValidateResponseContent(
            response, status, regular.Headers, endStream, result))
        {
            return result;
        }

        AddTextField(
            result.Headers,
            ":status",
            response.StatusCode.ToString("D3", CultureInfo.InvariantCulture));
        AppendFields(result.Headers, regular.Headers);
        Http2FieldValidationResult validation =
            Http2FieldSemantics.ValidateFields(
                result.Headers, false, limits);
        if (!validation.IsValid)
        {
            return RejectEncoding(
                result, validation.ErrorCodeName, validation.ErrorMessage);
        }

        result.Status = HttpOperationStatus.Done;
        result.MessageKind = informational ?
            HttpMessageKind.InformationalResponse : HttpMessageKind.Response;
        result.Response = response;
        result.IsComplete = informational || endStream;
        return result;
    }

    internal static Http2MessageResult EncodeTrailers(
        int streamId,
        HttpHeaders trailers,
        HttpLimits limits)
    {
        Http2MessageResult result = CreateEncodingResult(
            streamId, HttpMessageKind.Trailers, true);
        if (trailers == null || trailers.Fields == null)
        {
            return RejectEncoding(
                result,
                "invalid-http2-trailers",
                "A complete trailer field collection is required.");
        }

        Http2EncodingFieldResult normalized = NormalizeFields(
            trailers, false);
        if (!normalized.IsValid)
        {
            return RejectEncoding(
                result, normalized.ErrorCodeName, normalized.ErrorMessage);
        }

        if (!HeaderStore.AreValidTrailers(normalized.Headers))
        {
            return RejectEncoding(
                result,
                "invalid-http2-trailer-field",
                "The trailer section contains a prohibited field.");
        }

        Http2FieldValidationResult validation =
            Http2FieldSemantics.ValidateFields(
                normalized.Headers, false, limits);
        if (!validation.IsValid)
        {
            return RejectEncoding(
                result, validation.ErrorCodeName, validation.ErrorMessage);
        }

        result.Status = HttpOperationStatus.Done;
        result.Headers = normalized.Headers;
        result.Trailers = normalized.Headers;
        result.IsComplete = true;
        return result;
    }

    internal static bool ValidateExtendedConnectControl(
        HttpRequest request,
        string scheme,
        string authority,
        string target)
    {
        return request.ConnectProtocol != null &&
            Http2RequestSemantics.IsValidProtocol(request.ConnectProtocol) &&
            Http2RequestSemantics.IsValidScheme(scheme) &&
            Http2RequestSemantics.IsValidAuthority(
                authority, scheme, false) &&
            Http2RequestSemantics.IsValidPath(HttpMethods.Connect, target);
    }

    internal static bool ValidateRequestContent(
        HttpRequest request,
        HttpHeaders headers,
        bool endStream,
        Http2MessageResult result)
    {
        bool contentForbidden = request.Method == HttpMethods.Connect ||
            request.Method == HttpMethods.Trace;
        if (contentForbidden &&
            (request.Content.Length != 0 || request.Trailers.Fields.Count != 0))
        {
            RejectEncoding(
                result,
                "http2-request-content-not-allowed",
                "The request method does not permit content or trailers.");
            return false;
        }

        if (endStream &&
            (request.Content.Length != 0 || request.Trailers.Fields.Count != 0))
        {
            RejectEncoding(
                result,
                "http2-premature-end-stream",
                "The request head cannot end a stream that has content or trailers.");
            return false;
        }

        Http2ContentLengthResult length =
            Http2MessageState.ParseContentLength(headers);
        if (!length.IsValid)
        {
            RejectEncoding(
                result,
                "invalid-http2-content-length",
                "The request Content-Length field is malformed or conflicting.");
            return false;
        }

        if (length.HasValue && length.Value != request.Content.Length)
        {
            RejectEncoding(
                result,
                "http2-content-length-mismatch",
                "Content-Length does not match the retained request content.");
            return false;
        }

        if (!length.HasValue && request.Content.Length != 0)
        {
            AddContentLength(headers, request.Content.Length);
        }

        return true;
    }

    internal static bool ValidateResponseContent(
        HttpResponse response,
        HttpStatusMetadata status,
        HttpHeaders headers,
        bool endStream,
        Http2MessageResult result)
    {
        bool contentForbidden = !status.AllowsContent;
        if (contentForbidden &&
            (response.Content.Length != 0 || response.Trailers.Fields.Count != 0))
        {
            RejectEncoding(
                result,
                "http2-response-content-not-allowed",
                "The response status does not permit content or trailers.");
            return false;
        }

        if (endStream &&
            (response.Content.Length != 0 || response.Trailers.Fields.Count != 0))
        {
            RejectEncoding(
                result,
                "http2-premature-end-stream",
                "The response head cannot end a stream that has content or trailers.");
            return false;
        }

        Http2ContentLengthResult length =
            Http2MessageState.ParseContentLength(headers);
        if (!length.IsValid)
        {
            RejectEncoding(
                result,
                "invalid-http2-content-length",
                "The response Content-Length field is malformed or conflicting.");
            return false;
        }

        bool lengthProhibited = response.IsInformational ||
            response.StatusCode == HttpStatusCodes.NoContent;
        if (lengthProhibited && length.HasValue)
        {
            RejectEncoding(
                result,
                "http2-content-length-not-allowed",
                "The response status prohibits Content-Length.");
            return false;
        }

        bool metadataLength =
            response.StatusCode == HttpStatusCodes.NotModified;
        if (length.HasValue && !metadataLength &&
            length.Value != response.Content.Length)
        {
            RejectEncoding(
                result,
                "http2-content-length-mismatch",
                "Content-Length does not match the retained response content.");
            return false;
        }

        if (!length.HasValue && response.Content.Length != 0)
        {
            AddContentLength(headers, response.Content.Length);
        }

        return true;
    }

    internal static Http2EncodingFieldResult NormalizeFields(
        HttpHeaders source,
        bool splitCookies)
    {
        Http2EncodingFieldResult result = new Http2EncodingFieldResult();
        if (source == null || source.Fields == null)
        {
            result.IsValid = false;
            result.ErrorCodeName = "invalid-http2-fields";
            result.ErrorMessage = "A complete field collection is required.";
            return result;
        }

        int sourceIndex = 0;
        while (sourceIndex < source.Fields.Count)
        {
            HttpHeaderField sourceField = source.Fields[sourceIndex];
            if (sourceField == null || sourceField.Name == null ||
                sourceField.Value == null)
            {
                result.IsValid = false;
                result.ErrorCodeName = "invalid-http2-fields";
                result.ErrorMessage =
                    "The common field collection contains an incomplete field.";
                return result;
            }

            sourceIndex = sourceIndex + 1;
        }

        HttpHeaders prepared = Http2CookieSemantics.CreateEncodingCookieFields(
            source, splitCookies);
        int index = 0;
        while (index < prepared.Fields.Count)
        {
            HttpHeaderField field = prepared.Fields[index];
            if (field == null || field.Name == null || field.Value == null ||
                field.Name.Length == 0 || field.Name[0] == ':' ||
                !HeaderStore.IsValidName(field.Name))
            {
                result.IsValid = false;
                result.ErrorCodeName = "invalid-http2-field-name";
                result.ErrorMessage =
                    "Common message fields must have ordinary HTTP field names.";
                return result;
            }

            HttpHeaderField normalized = new HttpHeaderField();
            normalized.Name = LowercaseName(field.Name);
            normalized.Value = Http2RequestSemantics.CopyBytes(field.Value);
            normalized.IsSensitive = field.IsSensitive;
            normalized.NeverIndex = field.NeverIndex;
            result.Headers.Fields.Add(normalized);
            index = index + 1;
        }

        return result;
    }

    internal static string LowercaseName(string name)
    {
        char[] characters = new char[name.Length];
        int index = 0;
        while (index < name.Length)
        {
            char character = name[index];
            if (character >= 'A' && character <= 'Z')
            {
                character = (char)(character + 0x20);
            }

            characters[index] = character;
            index = index + 1;
        }

        return new string(characters);
    }

    internal static void AddTextField(
        HttpHeaders headers,
        string name,
        string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        headers.Fields.Add(field);
    }

    internal static void AddContentLength(HttpHeaders headers, int length)
    {
        AddTextField(
            headers,
            "content-length",
            length.ToString(CultureInfo.InvariantCulture));
    }

    internal static void AppendFields(HttpHeaders target, HttpHeaders source)
    {
        int index = 0;
        while (index < source.Fields.Count)
        {
            target.Fields.Add(source.Fields[index]);
            index = index + 1;
        }
    }

    internal static bool HasRequestParts(HttpRequest request)
    {
        return request != null && request.Method != null &&
            request.Target != null && request.Headers != null &&
            request.Headers.Fields != null && request.Content != null &&
            request.Trailers != null && request.Trailers.Fields != null;
    }

    internal static bool HasResponseParts(HttpResponse response)
    {
        return response != null && response.Headers != null &&
            response.Headers.Fields != null && response.Content != null &&
            response.Trailers != null && response.Trailers.Fields != null &&
            response.StatusCode >= 100 && response.StatusCode <= 999;
    }

    internal static Http2MessageResult CreateEncodingResult(
        int streamId,
        HttpMessageKind messageKind,
        bool endStream)
    {
        Http2MessageResult result = new Http2MessageResult();
        result.StreamId = streamId;
        result.MessageKind = messageKind;
        result.EndStream = endStream;
        return result;
    }

    internal static Http2MessageResult RejectEncoding(
        Http2MessageResult result,
        string errorCodeName,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Request = null;
        result.Response = null;
        result.IsComplete = false;
        result.ErrorCodeName = errorCodeName;
        result.ErrorMessage = errorMessage;
        return result;
    }
}
