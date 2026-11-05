namespace http;

internal class Http2ResponseSemantics
{
    internal Http2MessageResult Decode(
        int streamId,
        HttpHeaders headers,
        bool endStream,
        HttpLimits limits)
    {
        Http2MessageResult result = CreateResponseResult(
            streamId, headers, endStream);
        if (headers == null || headers.Fields == null)
        {
            return RejectResponse(
                result,
                "invalid-http2-response-fields",
                "An HTTP/2 response requires a decoded field section.");
        }

        Http2FieldValidationResult fieldValidation =
            Http2FieldSemantics.ValidateFields(headers, false, limits);
        if (!fieldValidation.IsValid)
        {
            return RejectResponse(
                result,
                fieldValidation.ErrorCodeName,
                fieldValidation.ErrorMessage);
        }

        HttpResponse response = new HttpResponse();
        response.ProtocolVersion = HttpProtocolVersion.Http2;
        bool regularSeen = false;
        bool statusSeen = false;
        int statusCode = 0;
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (field == null || field.Name == null || field.Value == null)
            {
                return RejectResponse(
                    result,
                    "invalid-http2-response-field",
                    "An HTTP/2 response contains an incomplete field.");
            }

            bool pseudo = field.Name.Length != 0 && field.Name[0] == ':';
            if (!pseudo)
            {
                regularSeen = true;
                response.Headers.Fields.Add(CopyResponseField(field));
                index = index + 1;
                continue;
            }

            if (regularSeen)
            {
                return RejectResponse(
                    result,
                    "http2-response-pseudo-header-order",
                    "Response pseudo-header fields must precede regular fields.");
            }

            if (field.Name != ":status")
            {
                return RejectResponse(
                    result,
                    "invalid-http2-response-pseudo-header",
                    "The response contains a request or undefined pseudo-header field.");
            }

            if (statusSeen)
            {
                return RejectResponse(
                    result,
                    "duplicate-http2-response-status",
                    "The response contains more than one :status pseudo-header field.");
            }

            statusSeen = true;
            statusCode = ParseStatusCode(field.Value);
            if (statusCode == 0)
            {
                return RejectResponse(
                    result,
                    "invalid-http2-response-status",
                    "The response :status value must be a valid three-digit HTTP status code.");
            }

            index = index + 1;
        }

        Http2CookieSemantics.CombineDecodedCookies(response.Headers);

        if (!statusSeen)
        {
            return RejectResponse(
                result,
                "missing-http2-response-status",
                "An HTTP/2 response requires exactly one :status pseudo-header field.");
        }

        if (statusCode == HttpStatusCodes.SwitchingProtocols)
        {
            return RejectResponse(
                result,
                "http2-switching-protocols-not-supported",
                "HTTP/2 does not support the 101 Switching Protocols status code.");
        }

        HttpStatusMetadata metadata = StatusSemantics.Describe(statusCode);
        bool informational =
            metadata.StatusClass == HttpStatusClass.Informational;
        if (informational && endStream)
        {
            return RejectResponse(
                result,
                "http2-informational-end-stream",
                "An informational HTTP/2 response cannot end the stream.");
        }

        response.StatusCode = statusCode;
        response.HasReasonPhrase = false;
        response.IsInformational = informational;
        response.IsComplete = informational || endStream;
        result.Status = HttpOperationStatus.Done;
        result.MessageKind = informational ?
            HttpMessageKind.InformationalResponse : HttpMessageKind.Response;
        result.Response = response;
        result.IsComplete = response.IsComplete;
        return result;
    }

    internal int ParseStatusCode(byte[] value)
    {
        if (value.Length != 3 ||
            value[0] < (byte)'1' || value[0] > (byte)'9' ||
            value[1] < (byte)'0' || value[1] > (byte)'9' ||
            value[2] < (byte)'0' || value[2] > (byte)'9')
        {
            return 0;
        }

        return (value[0] - (byte)'0') * 100 +
            (value[1] - (byte)'0') * 10 + value[2] - (byte)'0';
    }

    internal HttpHeaderField CopyResponseField(HttpHeaderField source)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = source.Name;
        field.Value = Http2RequestSemantics.CopyBytes(source.Value);
        field.IsSensitive = source.IsSensitive;
        field.NeverIndex = source.NeverIndex;
        return field;
    }

    internal Http2MessageResult CreateResponseResult(
        int streamId,
        HttpHeaders headers,
        bool endStream)
    {
        Http2MessageResult result = new Http2MessageResult();
        result.StreamId = streamId;
        result.MessageKind = HttpMessageKind.Response;
        result.EndStream = endStream;
        if (headers != null)
        {
            result.Headers = headers;
        }

        return result;
    }

    internal Http2MessageResult RejectResponse(
        Http2MessageResult result,
        string errorCodeName,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.IsMalformed = true;
        result.ErrorScope = Http2ErrorScope.Stream;
        result.HasErrorCode = true;
        result.ErrorCode = Http2ErrorCode.ProtocolError;
        result.ErrorCodeValue = (uint)Http2ErrorCode.ProtocolError;
        result.IsKnownErrorCode = true;
        result.ErrorCodeName = errorCodeName;
        result.ErrorMessage = errorMessage;
        return result;
    }
}
