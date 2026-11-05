using System.Collections.Generic;

namespace http;

internal enum Http1ParserStage
{
    StartLine = 0,
    Fields = 1,
    ContentLength = 2,
    ChunkSize = 3,
    ChunkData = 4,
    ChunkDataDelimiter = 5,
    ChunkTerminal = 6,
    CloseDelimited = 7,
    Complete = 8,
    Upgraded = 9,
    Tunnel = 10,
    Failed = 11
}

internal class Http1ParserState
{
    Http1ParserOptions options = new Http1ParserOptions();
    Http1LimitTracker limits = new Http1LimitTracker();
    Http1ParserStage stage;
    List<byte> line = new List<byte>();
    HttpRequest? request;
    HttpResponse? response;
    Http1FramingDecision? framingDecision;
    Http1ConnectionDecision? connectionDecision;
    List<byte> content = new List<byte>();
    long contentReceived;
    long chunkRemaining;
    int chunkDelimiterPosition;
    bool skippedLeadingLine;

    internal Http1ParseResult Parse(byte[] input)
    {
        Http1ParseResult result = ParseAvailable(input);
        if (input != null && result.Consumed < input.Length)
        {
            result.Unconsumed = CopyRange(
                input,
                result.Consumed,
                input.Length - result.Consumed);
        }

        if (input != null && result.Unconsumed.Length != 0 &&
            stage == Http1ParserStage.Complete && IsPostCloseDataProhibited())
        {
            RejectPostCloseData(result, result.Consumed);
        }

        return result;
    }

    Http1ParseResult ParseAvailable(byte[] input)
    {
        if (stage == Http1ParserStage.Failed)
        {
            return CreateTerminalResult();
        }

        if (stage == Http1ParserStage.Upgraded)
        {
            return CreateProtocolTerminalResult();
        }

        if (stage == Http1ParserStage.Tunnel)
        {
            return ConsumeTunnelInput(input);
        }

        if (stage == Http1ParserStage.Complete)
        {
            Http1ParseResult completed = CreateCompletedResult();
            if (input != null && input.Length != 0 && IsPostCloseDataProhibited())
            {
                RejectPostCloseData(completed, 0);
            }

            return completed;
        }

        Http1ParseResult result = CreateResult(HttpOperationStatus.NeedInput);
        if (input == null)
        {
            Reject(
                result,
                "invalid-http1-input",
                "The HTTP/1 input byte sequence is missing.",
                0,
                false);
            return result;
        }

        int index = 0;
        int lineStartPosition = -line.Count;
        while (index < input.Length && stage != Http1ParserStage.Complete &&
            stage != Http1ParserStage.Upgraded &&
            stage != Http1ParserStage.Tunnel &&
            stage != Http1ParserStage.Failed)
        {
            if (stage == Http1ParserStage.ContentLength)
            {
                int consumed = ConsumeFixedLengthContent(result, input, index);
                if (stage == Http1ParserStage.Failed)
                {
                    return result;
                }

                index = index + consumed;
                continue;
            }

            if (stage == Http1ParserStage.CloseDelimited)
            {
                int consumed = ConsumeCloseDelimitedContent(result, input, index);
                if (stage == Http1ParserStage.Failed)
                {
                    return result;
                }

                index = index + consumed;
                continue;
            }

            if (stage == Http1ParserStage.ChunkData)
            {
                int consumed = ConsumeChunkData(result, input, index);
                if (stage == Http1ParserStage.Failed)
                {
                    return result;
                }

                index = index + consumed;
                continue;
            }

            if (stage == Http1ParserStage.ChunkDataDelimiter)
            {
                if (!ConsumeChunkDataDelimiter(result, input[index], index))
                {
                    return result;
                }

                result.Consumed = index + 1;
                if (stage == Http1ParserStage.ChunkSize)
                {
                    lineStartPosition = index + 1;
                }

                index = index + 1;
                continue;
            }

            line.Add(input[index]);
            result.Consumed = index + 1;
            if (!limits.TrackBufferedInput(line.Count))
            {
                Reject(
                    result,
                    limits.ErrorCode ?? "http1-buffer-limit-exceeded",
                    limits.ErrorMessage ??
                        "The buffered HTTP/1 input exceeds the configured limit.",
                    index,
                    true);
                return result;
            }

            if (stage == Http1ParserStage.StartLine && input[index] != 0x0a &&
                input[index] != 0x0d && !limits.CheckStartLineSize(line.Count))
            {
                Reject(
                    result,
                    limits.ErrorCode ?? "http1-start-line-limit-exceeded",
                    limits.ErrorMessage ??
                        "The HTTP/1 start line exceeds the configured limit.",
                    index,
                    true);
                return result;
            }

            if (stage == Http1ParserStage.Fields && input[index] != 0x0a &&
                input[index] != 0x0d && !limits.CheckFieldLineSize(line.Count))
            {
                Reject(
                    result,
                    limits.ErrorCode ?? "http1-field-line-limit-exceeded",
                    limits.ErrorMessage ??
                        "An HTTP/1 field line exceeds the configured limit.",
                    index,
                    true);
                return result;
            }

            if (stage == Http1ParserStage.ChunkTerminal &&
                input[index] != 0x0a && input[index] != 0x0d &&
                !limits.CheckFieldLineSize(line.Count))
            {
                Reject(
                    result,
                    limits.ErrorCode ?? "http1-field-line-limit-exceeded",
                    limits.ErrorMessage ??
                        "An HTTP/1 trailer field line exceeds the configured limit.",
                    index,
                    true);
                return result;
            }

            if (input[index] == 0x0a &&
                !FinishLine(result, index, lineStartPosition))
            {
                return result;
            }

            if (input[index] == 0x0a)
            {
                lineStartPosition = index + 1;
            }

            index = index + 1;
        }

        if (stage == Http1ParserStage.Complete)
        {
            result.Status = HttpOperationStatus.Done;
            result.CanAcceptInput = false;
        }

        if (stage == Http1ParserStage.Upgraded ||
            stage == Http1ParserStage.Tunnel)
        {
            result.Status = HttpOperationStatus.Done;
            result.CanAcceptInput = false;
            result.IsTerminal = true;
        }

        result.NeedsEndOfInput = stage == Http1ParserStage.CloseDelimited;

        return result;
    }

    internal Http1ParseResult CompleteInput()
    {
        if (stage == Http1ParserStage.Failed)
        {
            return CreateTerminalResult();
        }

        if (stage == Http1ParserStage.Upgraded ||
            stage == Http1ParserStage.Tunnel)
        {
            return CreateProtocolTerminalResult();
        }

        if (stage == Http1ParserStage.Complete)
        {
            return CreateCompletedResult();
        }

        Http1ParseResult result = CreateResult(HttpOperationStatus.Rejected);
        if (stage == Http1ParserStage.CloseDelimited)
        {
            CompleteMessage(result, GetCurrentMessageKind());
            result.Status = HttpOperationStatus.Done;
            result.CanAcceptInput = false;
            result.NeedsEndOfInput = false;
            return result;
        }

        if (stage == Http1ParserStage.ContentLength)
        {
            Reject(
                result,
                "incomplete-http1-content",
                "The HTTP/1 message ended before its declared content length was received.",
                0,
                false);
            return result;
        }

        if (stage == Http1ParserStage.ChunkSize ||
            stage == Http1ParserStage.ChunkData ||
            stage == Http1ParserStage.ChunkDataDelimiter ||
            stage == Http1ParserStage.ChunkTerminal)
        {
            Reject(
                result,
                "incomplete-http1-chunked-content",
                "The HTTP/1 chunked message ended before its terminal boundary.",
                0,
                false);
            return result;
        }

        Reject(
            result,
            "incomplete-http1-message",
            "The HTTP/1 message ended before it was complete.",
            0,
            false);
        return result;
    }

    internal void Reset(Http1ParserOptions value)
    {
        ValidateOptions(value);
        limits.Reset(value.Limits);
        options = value;
        stage = Http1ParserStage.StartLine;
        line.Clear();
        request = null;
        response = null;
        framingDecision = null;
        connectionDecision = null;
        content.Clear();
        contentReceived = 0;
        chunkRemaining = 0;
        chunkDelimiterPosition = 0;
        skippedLeadingLine = false;
    }

    bool FinishLine(
        Http1ParseResult result,
        int inputPosition,
        int lineStartPosition)
    {
        bool hasCarriageReturn = line.Count >= 2 && line[line.Count - 2] == 0x0d;
        if (!hasCarriageReturn && !options.AllowBareLineFeed)
        {
            Reject(
                result,
                "invalid-http1-line-ending",
                "An HTTP/1 line is not terminated by CRLF.",
                inputPosition,
                true);
            return false;
        }

        line.RemoveAt(line.Count - 1);
        if (hasCarriageReturn)
        {
            line.RemoveAt(line.Count - 1);
        }

        byte[] value = line.ToArray();
        line.Clear();
        limits.TrackBufferedInput(0);
        if (stage == Http1ParserStage.ChunkSize)
        {
            return FinishChunkSizeLine(
                result,
                value,
                inputPosition,
                lineStartPosition);
        }

        if (stage == Http1ParserStage.ChunkTerminal)
        {
            return FinishTrailerLine(
                result,
                value,
                inputPosition,
                lineStartPosition);
        }

        if (stage == Http1ParserStage.StartLine)
        {
            if (value.Length == 0 && options.MessageKind == HttpMessageKind.Request &&
                options.AllowLeadingEmptyRequestLine && !skippedLeadingLine)
            {
                skippedLeadingLine = true;
                return true;
            }

            if (!limits.CheckStartLineSize(value.Length))
            {
                Reject(
                    result,
                    limits.ErrorCode ?? "http1-start-line-limit-exceeded",
                    limits.ErrorMessage ??
                        "The HTTP/1 start line exceeds the configured limit.",
                    inputPosition,
                    true);
                return false;
            }

            if (options.MessageKind == HttpMessageKind.Request)
            {
                Http1RequestLineResult parsed = Http1LineParser.ParseRequestLine(value);
                if (parsed.Status != HttpOperationStatus.Done || parsed.Request == null)
                {
                    int errorPosition = lineStartPosition >= 0 &&
                        parsed.HasErrorPosition ?
                        lineStartPosition + parsed.ErrorPosition : inputPosition;
                    Reject(
                        result,
                        parsed.ErrorCode ?? "invalid-http1-request-line",
                        parsed.ErrorMessage ?? "The HTTP/1 request line is malformed.",
                        errorPosition,
                        true);
                    return false;
                }

                request = parsed.Request;
            }
            else
            {
                Http1StatusLineResult parsed = Http1LineParser.ParseStatusLine(value);
                if (parsed.Status != HttpOperationStatus.Done || parsed.Response == null)
                {
                    int errorPosition = lineStartPosition >= 0 &&
                        parsed.HasErrorPosition ?
                        lineStartPosition + parsed.ErrorPosition : inputPosition;
                    Reject(
                        result,
                        parsed.ErrorCode ?? "invalid-http1-status-line",
                        parsed.ErrorMessage ?? "The HTTP/1 status line is malformed.",
                        errorPosition,
                        true);
                    return false;
                }

                response = parsed.Response;
            }

            stage = Http1ParserStage.Fields;
            return true;
        }

        if (value.Length == 0)
        {
            TrimTrailingFieldWhitespace();
            return CompleteHead(result, inputPosition);
        }

        Http1FieldLineResult field = Http1LineParser.ParseFieldLine(
            value,
            options.AllowWhitespaceBeforeColon,
            options.AllowObsoleteLineFolding);
        if (field.Status != HttpOperationStatus.Done)
        {
            int errorPosition = lineStartPosition >= 0 &&
                field.HasErrorPosition ?
                lineStartPosition + field.ErrorPosition : inputPosition;
            Reject(
                result,
                field.ErrorCode ?? "invalid-http1-field-line",
                field.ErrorMessage ?? "An HTTP/1 field line is malformed.",
                errorPosition,
                true);
            return false;
        }

        HttpHeaders? headers = GetCurrentHeaders();
        if (headers == null || field.IsContinuation && headers.Fields.Count == 0)
        {
            Reject(
                result,
                field.IsContinuation ?
                    "http1-folding-without-field" : "invalid-http1-field-state",
                field.IsContinuation ?
                    "A folded line requires a preceding HTTP field." :
                    "No message is available to receive the HTTP field.",
                inputPosition,
                true);
            return false;
        }

        if (field.IsContinuation)
        {
            HttpHeaderField previous = headers.Fields[headers.Fields.Count - 1];
            int separator = previous.Value.Length == 0 ? 0 : 1;
            long valueSize = (long)previous.Value.Length + separator +
                field.Continuation.Length;
            if (!limits.TrackFieldLine(value.Length, valueSize, false))
            {
                RejectLimit(result, inputPosition);
                return false;
            }

            AppendContinuation(
                previous,
                field.Continuation);
        }
        else if (field.Field != null)
        {
            if (!limits.TrackFieldLine(value.Length, field.Field.Value.Length, true))
            {
                RejectLimit(result, inputPosition);
                return false;
            }

            headers.Fields.Add(field.Field);
        }
        else
        {
            Reject(
                result,
                "invalid-http1-field-state",
                "The parsed HTTP/1 field result is incomplete.",
                inputPosition,
                true);
            return false;
        }

        return true;
    }

    HttpHeaders? GetCurrentHeaders()
    {
        if (request != null)
        {
            return request.Headers;
        }

        if (response != null)
        {
            return response.Headers;
        }

        return null;
    }

    void AppendContinuation(HttpHeaderField field, byte[] continuation)
    {
        byte[] previous = field.Value;
        int separator = previous.Length == 0 ? 0 : 1;
        byte[] combined = new byte[previous.Length + separator + continuation.Length];
        int index = 0;
        while (index < previous.Length)
        {
            combined[index] = previous[index];
            index = index + 1;
        }

        if (separator != 0)
        {
            combined[index] = 0x20;
            index = index + 1;
        }

        int continuationIndex = 0;
        while (continuationIndex < continuation.Length)
        {
            combined[index] = continuation[continuationIndex];
            index = index + 1;
            continuationIndex = continuationIndex + 1;
        }

        field.Value = combined;
    }

    void TrimTrailingFieldWhitespace()
    {
        HttpHeaders? headers = GetCurrentHeaders();
        if (headers == null)
        {
            return;
        }

        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            int end = field.Value.Length;
            while (end > 0 &&
                (field.Value[end - 1] == 0x20 || field.Value[end - 1] == 0x09))
            {
                end = end - 1;
            }

            if (end != field.Value.Length)
            {
                field.Value = CopyBytes(field.Value, end);
            }

            index = index + 1;
        }
    }

    static byte[] CopyBytes(byte[] value, int count)
    {
        byte[] result = new byte[count];
        int index = 0;
        while (index < count)
        {
            result[index] = value[index];
            index = index + 1;
        }

        return result;
    }

    bool CompleteHead(Http1ParseResult result, int inputPosition)
    {
        HttpMessageKind messageKind = options.MessageKind;
        if (response != null && response.IsInformational)
        {
            messageKind = HttpMessageKind.InformationalResponse;
        }

        Http1FramingResult framing;
        if (request != null)
        {
            framing = Http1Framing.DetermineRequest(request, options);
        }
        else if (response != null)
        {
            framing = Http1Framing.DetermineResponse(response, options);
        }
        else
        {
            Reject(
                result,
                "invalid-http1-framing-state",
                "No parsed HTTP/1 message is available for framing.",
                inputPosition,
                true);
            return false;
        }

        if (!framing.Decision.IsValid)
        {
            Reject(
                result,
                framing.ErrorCode ?? "invalid-http1-framing",
                framing.ErrorMessage ?? "The HTTP/1 message framing is invalid.",
                inputPosition,
                true);
            return false;
        }

        framingDecision = framing.Decision;
        if (framing.Decision.BodyFraming == Http1BodyFraming.ContentLength &&
            !limits.CheckContentSize(framing.Decision.DeclaredLength))
        {
            RejectLimit(result, inputPosition);
            return false;
        }

        if (framing.Decision.BodyFraming == Http1BodyFraming.Chunked)
        {
            HttpHeaders? headers = GetCurrentHeaders();
            if (headers == null)
            {
                Reject(
                    result,
                    "invalid-http1-trailer-declaration-state",
                    "No message headers are available for trailer declaration validation.",
                    inputPosition,
                    true);
                return false;
            }

            Http1TrailerValidationResult trailerValidation =
                Http1TrailerValidator.ValidateDeclarations(headers, options.Limits);
            if (trailerValidation.Status != HttpOperationStatus.Done)
            {
                Reject(
                    result,
                    trailerValidation.ErrorCode ??
                        "invalid-http1-trailer-declaration",
                    trailerValidation.ErrorMessage ??
                        "The Trailer field declaration is invalid.",
                    inputPosition,
                    true);
                return false;
            }
        }

        if (request != null)
        {
            Http1ExpectationResult expectation = Http1Expectation.Analyze(
                request,
                framing.Decision,
                options.Limits);
            if (expectation.Status != HttpOperationStatus.Done)
            {
                Reject(
                    result,
                    expectation.ErrorCode ?? "invalid-http1-expectation",
                    expectation.ErrorMessage ??
                        "The HTTP/1 request expectations are invalid.",
                    inputPosition,
                    true);
                return false;
            }
        }

        HttpHeaders? connectionHeaders = GetCurrentHeaders();
        if (connectionHeaders == null)
        {
            Reject(
                result,
                "invalid-http1-connection-state",
                "No message headers are available for connection analysis.",
                inputPosition,
                true);
            return false;
        }

        HttpProtocolVersion protocolVersion = HttpProtocolVersion.Http11;
        if (request != null)
        {
            protocolVersion = request.ProtocolVersion;
        }
        else if (response != null)
        {
            protocolVersion = response.ProtocolVersion;
        }
        int statusCode = 0;
        if (response != null)
        {
            statusCode = response.StatusCode;
        }
        Http1ConnectionResult connection = Http1Connection.Determine(
            connectionHeaders,
            protocolVersion,
            framing.Decision,
            options.IsProxyRecipient,
            response != null,
            statusCode,
            options.Limits);
        if (connection.Status != HttpOperationStatus.Done)
        {
            Reject(
                result,
                connection.ErrorCode ?? "invalid-http1-connection",
                connection.ErrorMessage ??
                    "The HTTP/1 connection options are invalid.",
                inputPosition,
                true);
            return false;
        }

        connectionDecision = connection.Decision;
        if (request != null)
        {
            request.IsUpgradeRequested = connection.IsUpgradeRequested;
            request.UpgradeProtocols = connection.UpgradeProtocols;
        }

        Http1ParseEvent head = CreateEvent(Http1ParseEventKind.MessageHead, messageKind);
        head.FramingDecision = framing.Decision;
        result.Events.Add(head);

        if (framing.Decision.BodyFraming == Http1BodyFraming.ContentLength &&
            framing.Decision.DeclaredLength > 0)
        {
            stage = Http1ParserStage.ContentLength;
            return true;
        }

        if (framing.Decision.BodyFraming == Http1BodyFraming.Chunked)
        {
            stage = Http1ParserStage.ChunkSize;
            return true;
        }

        if (framing.Decision.BodyFraming == Http1BodyFraming.CloseDelimited)
        {
            stage = Http1ParserStage.CloseDelimited;
            return true;
        }

        CompleteMessage(result, messageKind);
        return true;
    }

    int ConsumeCloseDelimitedContent(
        Http1ParseResult result,
        byte[] input,
        int start)
    {
        int count = input.Length - start;
        if (!AppendContentSegment(result, input, start, count))
        {
            return 0;
        }

        return count;
    }

    int ConsumeFixedLengthContent(
        Http1ParseResult result,
        byte[] input,
        int start)
    {
        if (framingDecision == null)
        {
            Reject(
                result,
                "invalid-http1-content-state",
                "No framing decision is available for HTTP/1 content.",
                start,
                true);
            return 0;
        }

        long remaining = framingDecision.DeclaredLength - contentReceived;
        int available = input.Length - start;
        int count = available;
        if (remaining < count)
        {
            count = (int)remaining;
        }

        if (!limits.AddContentSize(count))
        {
            RejectLimit(result, start);
            return 0;
        }

        byte[] segment = CopyRange(input, start, count);
        int index = 0;
        while (index < segment.Length)
        {
            content.Add(segment[index]);
            index = index + 1;
        }

        contentReceived = contentReceived + count;
        UpdateMessageContent();
        HttpMessageKind messageKind = GetCurrentMessageKind();
        Http1ParseEvent contentEvent = CreateEvent(
            Http1ParseEventKind.Content,
            messageKind);
        contentEvent.Content = segment;
        contentEvent.FramingDecision = framingDecision;
        result.Events.Add(contentEvent);
        result.Consumed = start + count;

        if (contentReceived == framingDecision.DeclaredLength)
        {
            CompleteMessage(result, messageKind);
        }

        return count;
    }

    bool FinishChunkSizeLine(
        Http1ParseResult result,
        byte[] value,
        int inputPosition,
        int lineStartPosition)
    {
        Http1ChunkLineResult parsed = Http1ChunkCodec.ParseChunkLine(value);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            int errorPosition = lineStartPosition >= 0 ?
                lineStartPosition + parsed.ErrorPosition : inputPosition;
            Reject(
                result,
                parsed.ErrorCode ?? "invalid-http1-chunk-line",
                parsed.ErrorMessage ?? "The HTTP/1 chunk line is malformed.",
                errorPosition,
                true);
            return false;
        }

        if (!limits.CheckChunkSize(parsed.ChunkSize))
        {
            RejectLimit(result, inputPosition);
            return false;
        }

        if (!limits.CheckContentSize(contentReceived + parsed.ChunkSize))
        {
            RejectLimit(result, inputPosition);
            return false;
        }

        if (parsed.ChunkSize == 0)
        {
            stage = Http1ParserStage.ChunkTerminal;
            return true;
        }

        chunkRemaining = parsed.ChunkSize;
        stage = Http1ParserStage.ChunkData;
        return true;
    }

    bool FinishTrailerLine(
        Http1ParseResult result,
        byte[] value,
        int inputPosition,
        int lineStartPosition)
    {
        HttpHeaders? trailers = GetCurrentTrailers();
        if (trailers == null)
        {
            Reject(
                result,
                "invalid-http1-trailer-state",
                "No message is available to receive HTTP/1 trailer fields.",
                inputPosition,
                true);
            return false;
        }

        if (value.Length == 0)
        {
            if (trailers.Fields.Count != 0)
            {
                Http1ParseEvent trailerEvent = CreateEvent(
                    Http1ParseEventKind.Trailers,
                    HttpMessageKind.Trailers);
                trailerEvent.Trailers = trailers;
                trailerEvent.FramingDecision = framingDecision;
                result.Events.Add(trailerEvent);
            }

            CompleteMessage(result, GetCurrentMessageKind());
            return true;
        }

        Http1FieldLineResult field = Http1LineParser.ParseFieldLine(
            value,
            options.AllowWhitespaceBeforeColon,
            options.AllowObsoleteLineFolding);
        if (field.Status != HttpOperationStatus.Done)
        {
            int errorPosition = lineStartPosition >= 0 &&
                field.HasErrorPosition ?
                lineStartPosition + field.ErrorPosition : inputPosition;
            Reject(
                result,
                field.ErrorCode ?? "invalid-http1-trailer-field",
                field.ErrorMessage ?? "An HTTP/1 trailer field line is malformed.",
                errorPosition,
                true);
            return false;
        }

        if (field.IsContinuation)
        {
            if (trailers.Fields.Count == 0)
            {
                Reject(
                    result,
                    "http1-folding-without-trailer-field",
                    "A folded trailer line requires a preceding trailer field.",
                    inputPosition,
                    true);
                return false;
            }

            HttpHeaderField previous = trailers.Fields[trailers.Fields.Count - 1];
            int separator = previous.Value.Length == 0 ? 0 : 1;
            long valueSize = (long)previous.Value.Length + separator +
                field.Continuation.Length;
            if (!limits.TrackTrailerLine(value.Length, valueSize, false))
            {
                RejectLimit(result, inputPosition);
                return false;
            }

            AppendContinuation(previous, field.Continuation);
            return true;
        }

        if (field.Field == null)
        {
            Reject(
                result,
                "invalid-http1-trailer-state",
                "The parsed HTTP/1 trailer field result is incomplete.",
                inputPosition,
                true);
            return false;
        }

        if (HeaderStore.IsForbiddenTrailerName(field.Field.Name))
        {
            Reject(
                result,
                "forbidden-http1-trailer-field",
                "The field is forbidden in an HTTP trailer section.",
                inputPosition,
                true);
            return false;
        }

        if (!limits.TrackTrailerLine(value.Length, field.Field.Value.Length, true))
        {
            RejectLimit(result, inputPosition);
            return false;
        }

        trailers.Fields.Add(field.Field);
        return true;
    }

    HttpHeaders? GetCurrentTrailers()
    {
        if (request != null)
        {
            return request.Trailers;
        }

        if (response != null)
        {
            return response.Trailers;
        }

        return null;
    }

    int ConsumeChunkData(
        Http1ParseResult result,
        byte[] input,
        int start)
    {
        int available = input.Length - start;
        int count = available;
        if (chunkRemaining < count)
        {
            count = (int)chunkRemaining;
        }

        if (!AppendContentSegment(result, input, start, count))
        {
            return 0;
        }

        chunkRemaining = chunkRemaining - count;
        if (chunkRemaining == 0)
        {
            stage = Http1ParserStage.ChunkDataDelimiter;
            chunkDelimiterPosition = 0;
        }

        return count;
    }

    bool ConsumeChunkDataDelimiter(
        Http1ParseResult result,
        byte value,
        int inputPosition)
    {
        if (chunkDelimiterPosition == 0)
        {
            if (value == 0x0d)
            {
                chunkDelimiterPosition = 1;
                return true;
            }

            if (value == 0x0a && options.AllowBareLineFeed)
            {
                stage = Http1ParserStage.ChunkSize;
                return true;
            }
        }
        else if (value == 0x0a)
        {
            chunkDelimiterPosition = 0;
            stage = Http1ParserStage.ChunkSize;
            return true;
        }

        Reject(
            result,
            "invalid-http1-chunk-data-ending",
            "HTTP/1 chunk data is not terminated by CRLF.",
            inputPosition,
            true);
        return false;
    }

    bool AppendContentSegment(
        Http1ParseResult result,
        byte[] input,
        int start,
        int count)
    {
        if (!limits.AddContentSize(count))
        {
            RejectLimit(result, start);
            return false;
        }

        byte[] segment = CopyRange(input, start, count);
        int index = 0;
        while (index < segment.Length)
        {
            content.Add(segment[index]);
            index = index + 1;
        }

        contentReceived = contentReceived + count;
        UpdateMessageContent();
        Http1ParseEvent contentEvent = CreateEvent(
            Http1ParseEventKind.Content,
            GetCurrentMessageKind());
        contentEvent.Content = segment;
        contentEvent.FramingDecision = framingDecision;
        result.Events.Add(contentEvent);
        result.Consumed = start + count;
        return true;
    }

    void CompleteMessage(Http1ParseResult result, HttpMessageKind messageKind)
    {
        UpdateMessageContent();
        if (request != null)
        {
            request.IsComplete = true;
        }

        if (response != null)
        {
            response.IsComplete = true;
        }

        Http1ParseEvent complete = CreateEvent(Http1ParseEventKind.Complete, messageKind);
        complete.FramingDecision = framingDecision;
        result.Events.Add(complete);
        stage = Http1ParserStage.Complete;

        if (connectionDecision != null && connectionDecision.IsUpgrade)
        {
            Http1ParseEvent switched = CreateEvent(
                Http1ParseEventKind.ProtocolSwitch,
                messageKind);
            switched.FramingDecision = framingDecision;
            switched.ProtocolName = connectionDecision.ProtocolName;
            result.Events.Add(switched);
            stage = Http1ParserStage.Upgraded;
        }

        if (connectionDecision != null && connectionDecision.IsTunnel)
        {
            stage = Http1ParserStage.Tunnel;
        }
    }

    Http1ParseResult ConsumeTunnelInput(byte[] input)
    {
        Http1ParseResult result = CreateResult(HttpOperationStatus.Terminal);
        result.CanAcceptInput = false;
        result.IsTerminal = true;
        if (input == null)
        {
            Reject(
                result,
                "invalid-http1-input",
                "The HTTP/1 input byte sequence is missing.",
                0,
                false);
            return result;
        }

        if (input.Length != 0)
        {
            Http1ParseEvent tunnel = CreateEvent(
                Http1ParseEventKind.TunnelBytes,
                GetCurrentMessageKind());
            tunnel.Content = CopyRange(input, 0, input.Length);
            tunnel.FramingDecision = framingDecision;
            result.Events.Add(tunnel);
            result.Consumed = input.Length;
        }

        return result;
    }

    void UpdateMessageContent()
    {
        byte[] value = content.ToArray();
        if (request != null)
        {
            request.Content = value;
        }

        if (response != null)
        {
            response.Content = value;
        }
    }

    HttpMessageKind GetCurrentMessageKind()
    {
        if (response != null && response.IsInformational)
        {
            return HttpMessageKind.InformationalResponse;
        }

        return options.MessageKind;
    }

    static byte[] CopyRange(byte[] value, int start, int count)
    {
        byte[] result = new byte[count];
        int index = 0;
        while (index < count)
        {
            result[index] = value[start + index];
            index = index + 1;
        }

        return result;
    }

    Http1ParseEvent CreateEvent(Http1ParseEventKind kind, HttpMessageKind messageKind)
    {
        Http1ParseEvent result = new Http1ParseEvent();
        result.EventKind = kind;
        result.MessageKind = messageKind;
        result.Request = request;
        result.Response = response;
        result.ConnectionDecision = connectionDecision;
        return result;
    }

    Http1ParseResult CreateResult(HttpOperationStatus status)
    {
        Http1ParseResult result = new Http1ParseResult();
        result.Status = status;
        result.Options = options;
        result.CanAcceptInput = stage == Http1ParserStage.StartLine ||
            stage == Http1ParserStage.Fields ||
            stage == Http1ParserStage.ContentLength ||
            stage == Http1ParserStage.ChunkSize ||
            stage == Http1ParserStage.ChunkData ||
            stage == Http1ParserStage.ChunkDataDelimiter ||
            stage == Http1ParserStage.ChunkTerminal ||
            stage == Http1ParserStage.CloseDelimited;
        result.NeedsEndOfInput = stage == Http1ParserStage.CloseDelimited;
        result.IsTerminal = stage == Http1ParserStage.Failed ||
            stage == Http1ParserStage.Upgraded ||
            stage == Http1ParserStage.Tunnel;
        return result;
    }

    Http1ParseResult CreateTerminalResult()
    {
        Http1ParseResult result = CreateResult(HttpOperationStatus.Terminal);
        result.CanAcceptInput = false;
        result.IsTerminal = true;
        result.ErrorCode = "http1-parser-terminal";
        result.ErrorMessage = "The HTTP/1 parser is in a terminal failure state.";
        return result;
    }

    Http1ParseResult CreateCompletedResult()
    {
        Http1ParseResult result = CreateResult(HttpOperationStatus.Done);
        result.CanAcceptInput = false;
        return result;
    }

    Http1ParseResult CreateProtocolTerminalResult()
    {
        Http1ParseResult result = CreateResult(HttpOperationStatus.Terminal);
        result.CanAcceptInput = false;
        result.IsTerminal = true;
        return result;
    }

    void Reject(
        Http1ParseResult result,
        string code,
        string message,
        int position,
        bool hasPosition)
    {
        stage = Http1ParserStage.Failed;
        if (connectionDecision != null)
        {
            Http1Connection.MarkUnusable(
                connectionDecision,
                "The connection cannot be reused after an HTTP/1 parsing failure.");
        }

        result.Status = HttpOperationStatus.Rejected;
        result.NeedsEndOfInput = false;
        result.CanAcceptInput = false;
        result.IsTerminal = true;
        result.HasErrorPosition = hasPosition;
        result.ErrorPosition = position;
        result.ErrorCode = code;
        result.ErrorMessage = message;
    }

    void RejectLimit(Http1ParseResult result, int position)
    {
        Reject(
            result,
            limits.ErrorCode ?? "http1-limit-exceeded",
            limits.ErrorMessage ?? "An HTTP/1 resource limit was exceeded.",
            position,
            true);
    }

    bool IsPostCloseDataProhibited()
    {
        return connectionDecision != null && connectionDecision.MustClose &&
            !options.AllowPostCloseData;
    }

    void RejectPostCloseData(Http1ParseResult result, int position)
    {
        Reject(
            result,
            "http1-post-close-data",
            "HTTP/1 data followed a message after connection closure was selected.",
            position,
            true);
    }

    static void ValidateOptions(Http1ParserOptions value)
    {
        if (value == null || value.Limits == null ||
            value.MessageKind != HttpMessageKind.Request &&
            value.MessageKind != HttpMessageKind.Response)
        {
            throw new HttpProtocolException(
                "The HTTP/1 parser options are incomplete or invalid.",
                "invalid-http1-parser-options");
        }
    }
}
