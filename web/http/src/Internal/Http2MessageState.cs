using System.Collections.Generic;

namespace http;

internal class Http2ContentLengthResult
{
    internal bool IsValid = true;
    internal bool HasValue;
    internal long Value;
}

internal class Http2MappedStreamState
{
    internal int StreamId;
    internal HttpRequest? Request;
    internal HttpResponse? Response;
    internal bool ResponseStarted;
    internal bool RequestComplete;
    internal bool ResponseComplete;
    internal bool RequestFailed;
    internal bool ResponseFailed;
    internal bool RequestAllowsContent = true;
    internal bool ResponseAllowsContent = true;
    internal bool RequestHasContentLength;
    internal long RequestContentLength;
    internal bool ResponseHasContentLength;
    internal long ResponseContentLength;
    internal bool ResponseIgnoresContentLength;
    internal bool IsExtendedConnect;
    internal bool IsTunnel;
}

internal class Http2MessageState
{
    internal List<Http2MappedStreamState> streams =
        new List<Http2MappedStreamState>();

    internal Http2MessageResult RegisterRequestHead(
        Http2MessageResult result,
        HttpLimits limits)
    {
        if (result.Status != HttpOperationStatus.Done || result.Request == null)
        {
            return result;
        }

        Http2MappedStreamState state = FindOrCreateState(result.StreamId);
        if (state.Request != null)
        {
            return RejectMessage(
                result,
                "http2-request-head-already-received",
                "The stream has already received its request header section.");
        }

        Http2ContentLengthResult length = ParseContentLength(
            result.Request.Headers);
        if (!length.IsValid)
        {
            return RejectMessage(
                result,
                "invalid-http2-content-length",
                "The request Content-Length field is malformed or conflicting.");
        }

        bool allowsContent = result.Request.Method != HttpMethods.Connect &&
            result.Request.Method != HttpMethods.Trace;
        if (!allowsContent && length.HasValue && length.Value != 0)
        {
            return RejectMessage(
                result,
                "http2-request-content-not-allowed",
                "The request method does not permit message content.");
        }

        if (allowsContent && length.HasValue &&
            length.Value > limits.MaximumContentSize)
        {
            return RejectMessage(
                result,
                "http2-content-limit-exceeded",
                "The declared request content exceeds the configured limit.");
        }

        if (result.EndStream && length.HasValue && length.Value != 0)
        {
            return RejectMessage(
                result,
                "http2-content-length-mismatch",
                "The request ended before its declared content length was received.");
        }

        state.Request = result.Request;
        state.RequestComplete = result.EndStream;
        state.RequestAllowsContent = allowsContent;
        state.RequestHasContentLength = length.HasValue;
        state.RequestContentLength = length.Value;
        state.IsExtendedConnect = result.Request.ConnectProtocol != null;
        result.IsTunnel = state.IsExtendedConnect;
        return result;
    }

    internal Http2MessageResult RegisterResponseHead(
        Http2MessageResult result,
        HttpLimits limits)
    {
        if (result.Status != HttpOperationStatus.Done || result.Response == null)
        {
            return result;
        }

        Http2ContentLengthResult length = ParseContentLength(
            result.Response.Headers);
        if (!length.IsValid)
        {
            return RejectMessage(
                result,
                "invalid-http2-content-length",
                "The response Content-Length field is malformed or conflicting.");
        }

        if (result.Response.IsInformational)
        {
            if (length.HasValue)
            {
                return RejectMessage(
                    result,
                    "http2-content-length-not-allowed",
                    "An informational response cannot contain Content-Length.");
            }

            Http2MappedStreamState informationalState =
                FindOrCreateState(result.StreamId);
            informationalState.ResponseStarted = true;
            return result;
        }

        Http2MappedStreamState state = FindOrCreateState(result.StreamId);
        if (state.Response != null)
        {
            return RejectMessage(
                result,
                "http2-final-response-already-received",
                "The stream has already received its final response.");
        }

        string requestMethod = "";
        if (state.Request != null)
        {
            requestMethod = state.Request.Method;
        }

        HttpStatusMetadata metadata = StatusSemantics.Describe(
            result.Response.StatusCode);
        bool headResponse = requestMethod == HttpMethods.Head;
        bool connectResponse = requestMethod == HttpMethods.Connect &&
            metadata.StatusClass == HttpStatusClass.Successful;
        bool lengthProhibited =
            result.Response.StatusCode == HttpStatusCodes.NoContent ||
            connectResponse;
        if (lengthProhibited && length.HasValue)
        {
            return RejectMessage(
                result,
                "http2-content-length-not-allowed",
                "The response status or request method prohibits Content-Length.");
        }

        bool allowsContent = metadata.AllowsContent &&
            !headResponse && !connectResponse;
        bool ignoresLength = headResponse ||
            result.Response.StatusCode == HttpStatusCodes.NotModified;
        if (allowsContent && length.HasValue &&
            length.Value > limits.MaximumContentSize)
        {
            return RejectMessage(
                result,
                "http2-content-limit-exceeded",
                "The declared response content exceeds the configured limit.");
        }

        if (result.EndStream && length.HasValue && !ignoresLength &&
            length.Value != 0)
        {
            return RejectMessage(
                result,
                "http2-content-length-mismatch",
                "The response ended before its declared content length was received.");
        }

        state.Response = result.Response;
        state.ResponseStarted = true;
        state.ResponseComplete = result.EndStream;
        state.ResponseAllowsContent = allowsContent;
        state.ResponseHasContentLength = length.HasValue;
        state.ResponseContentLength = length.Value;
        state.ResponseIgnoresContentLength = ignoresLength;
        state.IsTunnel = connectResponse;
        result.IsTunnel = state.IsTunnel;
        return result;
    }

    internal bool HasFinalResponse(int streamId)
    {
        Http2MappedStreamState? state = FindState(streamId);
        return state != null && state.Response != null;
    }

    internal bool HasResponseHead(int streamId)
    {
        Http2MappedStreamState? state = FindState(streamId);
        return state != null && state.ResponseStarted;
    }

    internal bool HasRequestHead(int streamId)
    {
        Http2MappedStreamState? state = FindState(streamId);
        return state != null && state.Request != null;
    }

    internal void RegisterPromisedRequest(
        int streamId,
        HttpRequest request)
    {
        Http2MappedStreamState state = FindOrCreateState(streamId);
        state.Request = request;
        state.RequestComplete = true;
        state.RequestAllowsContent = false;
        state.IsExtendedConnect = false;
    }

    internal Http2MessageResult DecodeContent(
        int streamId,
        HttpMessageKind messageKind,
        byte[] content,
        bool endStream,
        HttpLimits limits)
    {
        Http2MessageResult result = CreateMessageResult(
            streamId, messageKind, new HttpHeaders(), endStream);
        if (content == null)
        {
            return RejectMessage(
                result,
                "invalid-http2-content",
                "HTTP/2 DATA content cannot be null.");
        }

        if (messageKind != HttpMessageKind.Request &&
            messageKind != HttpMessageKind.Response)
        {
            return RejectMessage(
                result,
                "invalid-http2-content-message-kind",
                "HTTP/2 DATA must belong to a request or final response.");
        }

        Http2MappedStreamState? state = FindState(streamId);
        if (state == null)
        {
            return RejectMessage(
                result,
                "http2-message-head-not-received",
                "DATA cannot precede the initial message header section.");
        }

        if (messageKind == HttpMessageKind.Request)
        {
            if (state.IsTunnel)
            {
                return DecodeTunnelContent(
                    result, state, content, endStream, true);
            }

            return DecodeRequestContent(
                result, state, content, endStream, limits);
        }

        if (state.IsTunnel)
        {
            return DecodeTunnelContent(
                result, state, content, endStream, false);
        }

        return DecodeResponseContent(
            result, state, content, endStream, limits);
    }

    internal Http2MessageResult DecodeTrailers(
        int streamId,
        HttpHeaders headers,
        bool endStream,
        HttpLimits limits)
    {
        Http2MessageResult result = CreateMessageResult(
            streamId, HttpMessageKind.Trailers, headers, endStream);
        Http2MappedStreamState? state = FindState(streamId);
        if (state == null)
        {
            return RejectMessage(
                result,
                "http2-message-head-not-received",
                "Trailers cannot precede the initial message header section.");
        }

        if (state.IsTunnel)
        {
            return RejectMessage(
                result,
                "http2-tunnel-trailers-not-allowed",
                "An extended CONNECT tunnel cannot contain HTTP trailer fields.");
        }

        bool requestActive = state.Request != null &&
            !state.RequestComplete && !state.RequestFailed;
        bool responseActive = state.Response != null &&
            !state.ResponseComplete && !state.ResponseFailed;
        if (requestActive && responseActive || !requestActive && !responseActive)
        {
            return RejectMessage(
                result,
                requestActive && responseActive ?
                    "ambiguous-http2-message-state" :
                    "http2-message-already-complete",
                requestActive && responseActive ?
                    "The trailer section does not identify its message direction." :
                    "The mapped HTTP/2 message is already complete or malformed.");
        }

        if (!endStream)
        {
            return RejectMessage(
                result,
                "http2-trailers-require-end-stream",
                "An HTTP/2 trailer section must end the stream.");
        }

        if (headers == null || headers.Fields == null)
        {
            return RejectMessage(
                result,
                "invalid-http2-trailers",
                "HTTP/2 trailers require a decoded field section.");
        }

        Http2FieldValidationResult validation =
            Http2FieldSemantics.ValidateFields(
                headers, requestActive, limits);
        if (!validation.IsValid)
        {
            return RejectMessage(
                result, validation.ErrorCodeName, validation.ErrorMessage);
        }

        int index = 0;
        while (index < headers.Fields.Count)
        {
            if (headers.Fields[index].Name[0] == ':')
            {
                return RejectMessage(
                    result,
                    "http2-trailer-pseudo-header",
                    "HTTP/2 trailer sections cannot contain pseudo-header fields.");
            }

            index = index + 1;
        }

        if (!HeaderStore.AreValidTrailers(headers))
        {
            return RejectMessage(
                result,
                "invalid-http2-trailer-field",
                "The HTTP/2 trailer section contains a prohibited field.");
        }

        if (requestActive)
        {
            return CompleteRequestTrailers(result, state, headers);
        }

        return CompleteResponseTrailers(result, state, headers);
    }

    internal Http2MessageResult DecodeTunnelContent(
        Http2MessageResult result,
        Http2MappedStreamState state,
        byte[] content,
        bool endStream,
        bool requestDirection)
    {
        bool complete = requestDirection ?
            state.RequestComplete : state.ResponseComplete;
        bool failed = requestDirection ?
            state.RequestFailed : state.ResponseFailed;
        if (complete || failed)
        {
            return RejectMessage(
                result,
                "http2-message-already-complete",
                "The tunnel direction is already complete or malformed.");
        }

        if (requestDirection)
        {
            state.RequestComplete = endStream;
            if (state.Request != null)
            {
                state.Request.IsComplete = endStream;
                result.Request = state.Request;
            }
        }
        else
        {
            state.ResponseComplete = endStream;
            if (state.Response != null)
            {
                state.Response.IsComplete = endStream;
                result.Response = state.Response;
            }
        }

        result.Status = HttpOperationStatus.Done;
        result.Content = Http2RequestSemantics.CopyBytes(content);
        result.IsComplete = endStream;
        result.IsTunnel = true;
        return result;
    }

    internal Http2MessageResult DecodeRequestContent(
        Http2MessageResult result,
        Http2MappedStreamState state,
        byte[] content,
        bool endStream,
        HttpLimits limits)
    {
        if (state.Request == null)
        {
            return RejectMessage(
                result,
                "http2-message-head-not-received",
                "Request DATA cannot precede the request header section.");
        }

        if (state.RequestComplete || state.RequestFailed)
        {
            return RejectMessage(
                result,
                "http2-message-already-complete",
                "The request is already complete or malformed.");
        }

        long total = (long)state.Request.Content.Length + content.Length;
        if (!state.RequestAllowsContent && content.Length != 0)
        {
            state.RequestFailed = true;
            return RejectMessage(
                result,
                "http2-request-content-not-allowed",
                "The request method does not permit message content.");
        }

        if (total > limits.MaximumContentSize)
        {
            state.RequestFailed = true;
            return RejectMessage(
                result,
                "http2-content-limit-exceeded",
                "The request content exceeds the configured limit.");
        }

        if (state.RequestHasContentLength &&
            (total > state.RequestContentLength ||
                endStream && total != state.RequestContentLength))
        {
            state.RequestFailed = true;
            return RejectMessage(
                result,
                "http2-content-length-mismatch",
                "The request DATA length does not match Content-Length.");
        }

        state.Request.Content = AppendContent(
            state.Request.Content, content);
        state.RequestComplete = endStream;
        state.Request.IsComplete = endStream;
        result.Status = HttpOperationStatus.Done;
        result.Request = state.Request;
        result.Content = Http2RequestSemantics.CopyBytes(content);
        result.IsComplete = endStream;
        return result;
    }

    internal Http2MessageResult DecodeResponseContent(
        Http2MessageResult result,
        Http2MappedStreamState state,
        byte[] content,
        bool endStream,
        HttpLimits limits)
    {
        if (state.Response == null)
        {
            return RejectMessage(
                result,
                "http2-message-head-not-received",
                "Response DATA cannot precede the final response header section.");
        }

        if (state.ResponseComplete || state.ResponseFailed)
        {
            return RejectMessage(
                result,
                "http2-message-already-complete",
                "The response is already complete or malformed.");
        }

        long total = (long)state.Response.Content.Length + content.Length;
        if (!state.ResponseAllowsContent && content.Length != 0)
        {
            state.ResponseFailed = true;
            return RejectMessage(
                result,
                "http2-response-content-not-allowed",
                "The response status or request method does not permit content.");
        }

        if (total > limits.MaximumContentSize)
        {
            state.ResponseFailed = true;
            return RejectMessage(
                result,
                "http2-content-limit-exceeded",
                "The response content exceeds the configured limit.");
        }

        if (state.ResponseHasContentLength &&
            !state.ResponseIgnoresContentLength &&
            (total > state.ResponseContentLength ||
                endStream && total != state.ResponseContentLength))
        {
            state.ResponseFailed = true;
            return RejectMessage(
                result,
                "http2-content-length-mismatch",
                "The response DATA length does not match Content-Length.");
        }

        state.Response.Content = AppendContent(
            state.Response.Content, content);
        state.ResponseComplete = endStream;
        state.Response.IsComplete = endStream;
        result.Status = HttpOperationStatus.Done;
        result.Response = state.Response;
        result.Content = Http2RequestSemantics.CopyBytes(content);
        result.IsComplete = endStream;
        return result;
    }

    internal Http2MessageResult CompleteRequestTrailers(
        Http2MessageResult result,
        Http2MappedStreamState state,
        HttpHeaders headers)
    {
        if (state.Request == null || !state.RequestAllowsContent)
        {
            state.RequestFailed = true;
            return RejectMessage(
                result,
                "http2-request-trailers-not-allowed",
                "The request method does not permit trailers.");
        }

        if (state.RequestHasContentLength &&
            state.Request.Content.Length != state.RequestContentLength)
        {
            state.RequestFailed = true;
            return RejectMessage(
                result,
                "http2-content-length-mismatch",
                "The request ended before its declared content length was received.");
        }

        HttpHeaders trailers = CopyTrailerFields(headers);
        state.Request.Trailers = trailers;
        state.Request.IsComplete = true;
        state.RequestComplete = true;
        result.Status = HttpOperationStatus.Done;
        result.Request = state.Request;
        result.Trailers = trailers;
        result.IsComplete = true;
        return result;
    }

    internal Http2MessageResult CompleteResponseTrailers(
        Http2MessageResult result,
        Http2MappedStreamState state,
        HttpHeaders headers)
    {
        if (state.Response == null || !state.ResponseAllowsContent)
        {
            state.ResponseFailed = true;
            return RejectMessage(
                result,
                "http2-response-trailers-not-allowed",
                "The response status or request method does not permit trailers.");
        }

        if (state.ResponseHasContentLength &&
            !state.ResponseIgnoresContentLength &&
            state.Response.Content.Length != state.ResponseContentLength)
        {
            state.ResponseFailed = true;
            return RejectMessage(
                result,
                "http2-content-length-mismatch",
                "The response ended before its declared content length was received.");
        }

        HttpHeaders trailers = CopyTrailerFields(headers);
        state.Response.Trailers = trailers;
        state.Response.IsComplete = true;
        state.ResponseComplete = true;
        result.Status = HttpOperationStatus.Done;
        result.Response = state.Response;
        result.Trailers = trailers;
        result.IsComplete = true;
        return result;
    }

    internal static Http2ContentLengthResult ParseContentLength(HttpHeaders headers)
    {
        Http2ContentLengthResult result = new Http2ContentLengthResult();
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (field.Name == "content-length" &&
                !ParseContentLengthValue(result, field.Value))
            {
                result.IsValid = false;
                return result;
            }

            index = index + 1;
        }

        return result;
    }

    internal static bool ParseContentLengthValue(
        Http2ContentLengthResult result,
        byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length &&
                (value[index] == 0x20 || value[index] == 0x09))
            {
                index = index + 1;
            }

            int digitCount = 0;
            long parsed = 0;
            while (index < value.Length &&
                value[index] >= (byte)'0' && value[index] <= (byte)'9')
            {
                int digit = value[index] - (byte)'0';
                if (parsed > (long.MaxValue - digit) / 10)
                {
                    return false;
                }

                parsed = parsed * 10 + digit;
                digitCount = digitCount + 1;
                index = index + 1;
            }

            if (digitCount == 0 ||
                result.HasValue && result.Value != parsed)
            {
                return false;
            }

            result.HasValue = true;
            result.Value = parsed;
            while (index < value.Length &&
                (value[index] == 0x20 || value[index] == 0x09))
            {
                index = index + 1;
            }

            if (index == value.Length)
            {
                return true;
            }

            if (value[index] != (byte)',')
            {
                return false;
            }

            index = index + 1;
        }

        return false;
    }

    internal Http2MappedStreamState FindOrCreateState(int streamId)
    {
        Http2MappedStreamState? existing = FindState(streamId);
        if (existing != null)
        {
            return existing;
        }

        Http2MappedStreamState state = new Http2MappedStreamState();
        state.StreamId = streamId;
        streams.Add(state);
        return state;
    }

    internal Http2MappedStreamState? FindState(int streamId)
    {
        int index = 0;
        while (index < streams.Count)
        {
            if (streams[index].StreamId == streamId)
            {
                return streams[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static byte[] AppendContent(byte[] existing, byte[] addition)
    {
        byte[] result = new byte[existing.Length + addition.Length];
        int index = 0;
        while (index < existing.Length)
        {
            result[index] = existing[index];
            index = index + 1;
        }

        int additionIndex = 0;
        while (additionIndex < addition.Length)
        {
            result[index] = addition[additionIndex];
            index = index + 1;
            additionIndex = additionIndex + 1;
        }

        return result;
    }

    internal static HttpHeaders CopyTrailerFields(HttpHeaders source)
    {
        HttpHeaders result = new HttpHeaders();
        int index = 0;
        while (index < source.Fields.Count)
        {
            result.Fields.Add(
                Http2CookieSemantics.CopyCookieField(source.Fields[index]));
            index = index + 1;
        }

        return result;
    }

    internal static Http2MessageResult CreateMessageResult(
        int streamId,
        HttpMessageKind messageKind,
        HttpHeaders headers,
        bool endStream)
    {
        Http2MessageResult result = new Http2MessageResult();
        result.StreamId = streamId;
        result.MessageKind = messageKind;
        result.EndStream = endStream;
        if (headers != null)
        {
            result.Headers = headers;
        }

        return result;
    }

    internal static Http2MessageResult RejectMessage(
        Http2MessageResult result,
        string errorCodeName,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Request = null;
        result.Response = null;
        result.IsComplete = false;
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
