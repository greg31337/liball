namespace json;

/// <summary>Provides incremental pull-based token reading for UTF-8 JSON input.</summary>
public class JsonPullReader : IJsonPullReader
{
    JsonPullReaderCore core;
    int streamBufferBytes;
    JsonStreamInput? streamInput;
    JsonDiagnostic? streamFailure;

    /// <summary>Initializes a pull reader with the supplied input policies.</summary>
    /// <param name="options">The input policies to apply.</param>
    public JsonPullReader(JsonReaderOptions options)
    {
        JsonReaderOptions normalized = JsonOptionNormalizer.NormalizeReaderOptions(options);
        JsonLimits limits = normalized.Limits
            ?? throw new global::System.InvalidOperationException("Normalized limits are required.");
        streamBufferBytes = normalized.StreamBufferBytes;
        if (streamBufferBytes > limits.MaximumInputBufferBytes)
        {
            streamBufferBytes = limits.MaximumInputBufferBytes;
        }

        core = new JsonPullReaderCore(normalized);
    }

    /// <summary>Copies and processes the supplied UTF-8 input.</summary>
    /// <param name="utf8">The next input bytes.</param>
    /// <returns>The current incremental operation state.</returns>
    public JsonFeedResult FeedUtf8(global::System.ReadOnlySpan<byte> utf8)
    {
        RejectManualFeedWithStream();
        byte[] ownedInput = new byte[utf8.Length];
        utf8.CopyTo(ownedInput);
        return core.FeedBytes(ownedInput);
    }

    /// <summary>Attaches and begins reading a caller-owned UTF-8 input stream.</summary>
    /// <param name="stream">The caller-owned input stream.</param>
    /// <returns>The current incremental operation state.</returns>
    public JsonFeedResult FeedStream(global::System.IO.Stream stream)
    {
        if (streamInput != null)
        {
            throw new global::System.InvalidOperationException("An input stream is already attached.");
        }

        streamInput = new JsonStreamInput(stream, streamBufferBytes);
        return PumpReaderStream();
    }

    /// <summary>Signals that no additional input bytes will be supplied.</summary>
    /// <returns>The current incremental operation state.</returns>
    public JsonFeedResult CompleteInput()
    {
        if (streamFailure != null)
        {
            return CreateReaderFailureFeedResult(streamFailure);
        }

        if (streamInput != null && !streamInput.IsStreamCompleted())
        {
            throw new global::System.InvalidOperationException("Attached stream input completes at end of stream.");
        }

        return core.CompleteReaderInput();
    }

    /// <summary>Peeks at the next available token without advancing the reader.</summary>
    /// <returns>The token or current read state.</returns>
    public JsonTokenResult PeekToken()
    {
        return ContinueStreamPeek();
    }

    /// <summary>Reads the next available token.</summary>
    /// <returns>The token or current read state.</returns>
    public JsonTokenResult ReadToken()
    {
        return ContinueStreamRead();
    }

    /// <summary>Skips the current scalar or complete subtree after fully validating it.</summary>
    /// <returns>The skip outcome and current read state.</returns>
    public JsonOperationResult SkipValue()
    {
        return ContinueStreamSkip();
    }

    /// <summary>Captures the current scalar or complete subtree.</summary>
    /// <returns>The captured value or current read state.</returns>
    public JsonValueResult CaptureValue()
    {
        return ContinueStreamCapture();
    }

    /// <summary>Gets the total number of input bytes accepted by the reader.</summary>
    /// <returns>The accepted byte count.</returns>
    public long GetBytesAccepted()
    {
        return core.GetAcceptedByteCount();
    }

    /// <summary>Gets the total number of accepted input bytes consumed by the reader.</summary>
    /// <returns>The consumed byte count.</returns>
    public long GetBytesConsumed()
    {
        return core.GetConsumedByteCount();
    }

    /// <summary>Resets the reader for independent reuse.</summary>
    public void ResetReader()
    {
        core.ResetPullReader();
        streamInput = null;
        streamFailure = null;
    }

    JsonTokenResult ContinueStreamPeek()
    {
        if (streamFailure != null)
        {
            return CreateReaderFailureTokenResult(streamFailure);
        }

        JsonTokenResult result = core.PeekNextToken();
        while (result.Status == JsonReadStatus.NeedMoreData &&
            streamInput != null &&
            !streamInput.IsStreamCompleted())
        {
            JsonFeedResult feed = PumpReaderStream();
            if (feed.ReadStatus == JsonReadStatus.Error && streamFailure != null)
            {
                return CreateReaderFailureTokenResult(
                    feed.Diagnostic
                    ?? throw new global::System.InvalidOperationException("A stream error requires a diagnostic."));
            }

            result = core.PeekNextToken();
        }

        return result;
    }

    JsonTokenResult ContinueStreamRead()
    {
        if (streamFailure != null)
        {
            return CreateReaderFailureTokenResult(streamFailure);
        }

        JsonTokenResult result = core.ReadNextToken();
        while (result.Status == JsonReadStatus.NeedMoreData &&
            streamInput != null &&
            !streamInput.IsStreamCompleted())
        {
            JsonFeedResult feed = PumpReaderStream();
            if (feed.ReadStatus == JsonReadStatus.Error && streamFailure != null)
            {
                return CreateReaderFailureTokenResult(
                    feed.Diagnostic
                    ?? throw new global::System.InvalidOperationException("A stream error requires a diagnostic."));
            }

            result = core.ReadNextToken();
        }

        return result;
    }

    JsonOperationResult ContinueStreamSkip()
    {
        if (streamFailure != null)
        {
            return CreateReaderFailureOperationResult(streamFailure);
        }

        JsonOperationResult result = core.SkipCurrentValue();
        while (!result.Success &&
            result.Diagnostic == null &&
            streamInput != null &&
            !streamInput.IsStreamCompleted())
        {
            JsonFeedResult feed = PumpReaderStream();
            if (feed.ReadStatus == JsonReadStatus.Error && streamFailure != null)
            {
                return CreateReaderFailureOperationResult(
                    feed.Diagnostic
                    ?? throw new global::System.InvalidOperationException("A stream error requires a diagnostic."));
            }

            result = core.SkipCurrentValue();
        }

        return result;
    }

    JsonValueResult ContinueStreamCapture()
    {
        if (streamFailure != null)
        {
            return CreateReaderFailureValueResult(streamFailure);
        }

        JsonValueResult result = core.CaptureCurrentValue();
        while (!result.Success &&
            result.Diagnostic == null &&
            streamInput != null &&
            !streamInput.IsStreamCompleted())
        {
            JsonFeedResult feed = PumpReaderStream();
            if (feed.ReadStatus == JsonReadStatus.Error && streamFailure != null)
            {
                return CreateReaderFailureValueResult(
                    feed.Diagnostic
                    ?? throw new global::System.InvalidOperationException("A stream error requires a diagnostic."));
            }

            result = core.CaptureCurrentValue();
        }

        return result;
    }

    JsonFeedResult PumpReaderStream()
    {
        JsonStreamInput input = streamInput
            ?? throw new global::System.InvalidOperationException("An input stream is required.");
        long acceptedBefore = core.GetAcceptedByteCount();
        long consumedBefore = core.GetConsumedByteCount();
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
                current = core.CompleteReaderInput();
                break;
            }

            byte[] bytes = read.Bytes
                ?? throw new global::System.InvalidOperationException("A stream read requires bytes.");
            current = core.FeedBytes(bytes);
        }

        JsonFeedResult result = new JsonFeedResult();
        result.BytesAccepted = (int)(core.GetAcceptedByteCount() - acceptedBefore);
        result.BytesConsumed = (int)(core.GetConsumedByteCount() - consumedBefore);
        result.ReadStatus = current.ReadStatus;
        result.Diagnostic = current.Diagnostic;
        return result;
    }

    void RejectManualFeedWithStream()
    {
        if (streamInput != null)
        {
            throw new global::System.InvalidOperationException("Byte input cannot be supplied while a stream is attached.");
        }

        if (streamFailure != null)
        {
            throw new global::System.InvalidOperationException("The failed reader must be reset before more input is supplied.");
        }
    }

    JsonFeedResult CreateReaderFailureFeedResult(JsonDiagnostic diagnostic)
    {
        JsonFeedResult result = new JsonFeedResult();
        result.ReadStatus = JsonReadStatus.Error;
        result.Diagnostic = diagnostic;
        return result;
    }

    JsonTokenResult CreateReaderFailureTokenResult(JsonDiagnostic diagnostic)
    {
        JsonTokenResult result = new JsonTokenResult();
        result.Status = JsonReadStatus.Error;
        result.Diagnostic = diagnostic;
        return result;
    }

    JsonOperationResult CreateReaderFailureOperationResult(JsonDiagnostic diagnostic)
    {
        JsonOperationResult result = new JsonOperationResult();
        result.Success = false;
        result.BytesConsumed = core.GetConsumedByteCount();
        result.Diagnostic = diagnostic;
        return result;
    }

    JsonValueResult CreateReaderFailureValueResult(JsonDiagnostic diagnostic)
    {
        JsonValueResult result = new JsonValueResult();
        result.Success = false;
        result.Status = JsonReadStatus.Error;
        result.Diagnostic = diagnostic;
        return result;
    }
}
