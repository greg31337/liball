namespace json;

/// <summary>Publishes incrementally parsed UTF-8 JSON tokens to a handler.</summary>
public class JsonPushParser : IJsonPushParser
{
    JsonEventDispatcher dispatcher;
    int streamBufferBytes;
    JsonStreamInput? streamInput;
    JsonDiagnostic? streamFailure;

    /// <summary>Initializes a push parser with the supplied policies and token handler.</summary>
    /// <param name="options">The input policies to apply.</param>
    /// <param name="handler">The handler that receives parsed tokens and document boundaries.</param>
    public JsonPushParser(JsonReaderOptions options, IJsonTokenHandler handler)
    {
        JsonReaderOptions normalized = JsonOptionNormalizer.NormalizeReaderOptions(options);
        JsonLimits limits = normalized.Limits
            ?? throw new global::System.InvalidOperationException("Normalized limits are required.");
        streamBufferBytes = normalized.StreamBufferBytes;
        if (streamBufferBytes > limits.MaximumInputBufferBytes)
        {
            streamBufferBytes = limits.MaximumInputBufferBytes;
        }

        dispatcher = new JsonEventDispatcher(normalized, handler);
    }

    /// <summary>Copies and processes the supplied UTF-8 input.</summary>
    /// <param name="utf8">The next input bytes.</param>
    /// <returns>The current incremental operation state.</returns>
    public JsonFeedResult FeedPushUtf8(global::System.ReadOnlySpan<byte> utf8)
    {
        RejectPushByteFeedWithStream();
        byte[] ownedInput = new byte[utf8.Length];
        utf8.CopyTo(ownedInput);
        return dispatcher.FeedDispatchBytes(ownedInput);
    }

    /// <summary>Attaches and processes a caller-owned UTF-8 input stream.</summary>
    /// <param name="stream">The caller-owned input stream.</param>
    /// <returns>The current incremental operation state.</returns>
    public JsonFeedResult FeedPushStream(global::System.IO.Stream stream)
    {
        if (streamInput != null)
        {
            throw new global::System.InvalidOperationException("An input stream is already attached.");
        }

        streamInput = new JsonStreamInput(stream, streamBufferBytes);
        return PumpPushStream();
    }

    /// <summary>Signals that no additional input bytes will be supplied.</summary>
    /// <returns>The current incremental operation state.</returns>
    public JsonFeedResult CompletePushInput()
    {
        if (streamFailure != null)
        {
            return CreatePushFailureResult(streamFailure);
        }

        if (streamInput != null && !streamInput.IsStreamCompleted())
        {
            throw new global::System.InvalidOperationException("Attached stream input completes at end of stream.");
        }

        return dispatcher.CompleteDispatchInput();
    }

    /// <summary>Resumes token publication after a handler-requested pause.</summary>
    /// <returns>The current incremental operation state.</returns>
    public JsonFeedResult ResumePushParser()
    {
        if (streamFailure != null)
        {
            return CreatePushFailureResult(streamFailure);
        }

        long acceptedBefore = dispatcher.GetDispatchAcceptedByteCount();
        long consumedBefore = dispatcher.GetDispatchConsumedByteCount();
        JsonFeedResult current = dispatcher.ResumeDispatching();
        if (current.ReadStatus == JsonReadStatus.NeedMoreData &&
            streamInput != null &&
            !streamInput.IsStreamCompleted())
        {
            current = PumpPushStream();
        }

        JsonFeedResult result = new JsonFeedResult();
        result.BytesAccepted = (int)(dispatcher.GetDispatchAcceptedByteCount() - acceptedBefore);
        result.BytesConsumed = (int)(dispatcher.GetDispatchConsumedByteCount() - consumedBefore);
        result.ReadStatus = current.ReadStatus;
        result.Diagnostic = current.Diagnostic;
        return result;
    }

    /// <summary>Gets the total number of input bytes accepted by the parser.</summary>
    /// <returns>The accepted byte count.</returns>
    public long GetPushBytesAccepted()
    {
        return dispatcher.GetDispatchAcceptedByteCount();
    }

    /// <summary>Gets the total number of accepted input bytes consumed by the parser.</summary>
    /// <returns>The consumed byte count.</returns>
    public long GetPushBytesConsumed()
    {
        return dispatcher.GetDispatchConsumedByteCount();
    }

    /// <summary>Resets the parser for independent reuse.</summary>
    public void ResetPushParser()
    {
        dispatcher.ResetDispatcher();
        streamInput = null;
        streamFailure = null;
    }

    JsonFeedResult PumpPushStream()
    {
        JsonStreamInput input = streamInput
            ?? throw new global::System.InvalidOperationException("An input stream is required.");
        long acceptedBefore = dispatcher.GetDispatchAcceptedByteCount();
        long consumedBefore = dispatcher.GetDispatchConsumedByteCount();
        JsonFeedResult current = new JsonFeedResult();
        current.ReadStatus = JsonReadStatus.NeedMoreData;
        while (current.ReadStatus == JsonReadStatus.NeedMoreData)
        {
            JsonStreamReadResult read = input.ReadStreamBlock();
            if (read.Diagnostic != null)
            {
                streamFailure = read.Diagnostic;
                current.ReadStatus = JsonReadStatus.Error;
                current.Diagnostic = read.Diagnostic;
                break;
            }

            if (read.EndOfStream)
            {
                current = dispatcher.CompleteDispatchInput();
                break;
            }

            byte[] bytes = read.Bytes
                ?? throw new global::System.InvalidOperationException("A stream read requires bytes.");
            current = dispatcher.FeedDispatchBytes(bytes);
        }

        JsonFeedResult result = new JsonFeedResult();
        result.BytesAccepted = (int)(dispatcher.GetDispatchAcceptedByteCount() - acceptedBefore);
        result.BytesConsumed = (int)(dispatcher.GetDispatchConsumedByteCount() - consumedBefore);
        result.ReadStatus = current.ReadStatus;
        result.Diagnostic = current.Diagnostic;
        return result;
    }

    void RejectPushByteFeedWithStream()
    {
        if (streamInput != null)
        {
            throw new global::System.InvalidOperationException("Byte input cannot be supplied while a stream is attached.");
        }

        if (streamFailure != null)
        {
            throw new global::System.InvalidOperationException("The failed parser must be reset before more input is supplied.");
        }
    }

    JsonFeedResult CreatePushFailureResult(JsonDiagnostic diagnostic)
    {
        JsonFeedResult result = new JsonFeedResult();
        result.ReadStatus = JsonReadStatus.Error;
        result.Diagnostic = diagnostic;
        return result;
    }
}
