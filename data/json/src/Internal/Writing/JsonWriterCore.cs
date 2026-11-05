namespace json;

internal class JsonWriterCore
{
    JsonByteSink sink;
    JsonPrettyFormatter formatter;
    global::System.Collections.Generic.List<JsonWriteFrame> frames;
    int maximumNestingDepth;
    int maximumArrayItems;
    int maximumObjectMembers;
    int maximumOutputBytes;
    JsonInputMode outputMode;
    bool completed;
    bool hasWrittenRoot;
    bool sequenceTerminatorWritten;

    internal JsonWriterCore(JsonWriterOptions options, JsonWriterTarget target)
    {
        JsonWriterOptions normalized =
            JsonOptionNormalizer.NormalizeWriterOptions(options);
        JsonLimits limits = normalized.Limits
            ?? throw new global::System.InvalidOperationException(
                "Normalized writer limits are required.");
        maximumNestingDepth = limits.MaximumNestingDepth;
        maximumArrayItems = limits.MaximumArrayItems;
        maximumObjectMembers = limits.MaximumObjectMembers;
        maximumOutputBytes = limits.MaximumOutputBytes;
        outputMode = normalized.OutputMode;
        sink = new JsonByteSink(target, limits.MaximumOutputBytes);
        formatter = new JsonPrettyFormatter(normalized);
        frames = new global::System.Collections.Generic.List<JsonWriteFrame>();
        frames.Add(new JsonWriteFrame(JsonContainerKind.Root));
    }

    internal JsonOperationResult WriteObjectStart()
    {
        return WriteContainerStart(
            JsonContainerKind.Object,
            JsonSyntaxBytes.BeginObject,
            "Object");
    }

    internal JsonOperationResult WriteObjectEnd()
    {
        return WriteContainerEnd(
            JsonContainerKind.Object,
            JsonSyntaxBytes.EndObject,
            "EndObject");
    }

    internal JsonOperationResult WriteArrayStart()
    {
        return WriteContainerStart(
            JsonContainerKind.Array,
            JsonSyntaxBytes.BeginArray,
            "Array");
    }

    internal JsonOperationResult WriteArrayEnd()
    {
        return WriteContainerEnd(
            JsonContainerKind.Array,
            JsonSyntaxBytes.EndArray,
            "EndArray");
    }

    internal JsonOperationResult WriteEncodedPropertyName(
        global::System.ReadOnlySpan<byte> encodedName)
    {
        JsonOperationResult? availability = ValidateOperationAvailability(
            "PropertyName");
        if (availability != null)
        {
            return availability;
        }

        JsonWriteFrame frame = GetCurrentFrame();
        if (!frame.CanAcceptPropertyName())
        {
            return CreateStateFailure(
                "A property name is valid only when an object is ready for its next member.",
                "Property name",
                "PropertyName");
        }

        if (frame.GetMemberCount() >= maximumObjectMembers)
        {
            return CreateLimitFailure(
                JsonDiagnosticCode.ObjectMemberLimitExceeded,
                "The configured object member limit was exceeded.",
                "PropertyName");
        }

        if (encodedName.Length > maximumOutputBytes)
        {
            return CreateLimitFailure(
                JsonDiagnosticCode.OutputByteLimitExceeded,
                "The configured output byte limit was exceeded.",
                "PropertyName");
        }

        byte[] ownedName = new byte[encodedName.Length];
        encodedName.CopyTo(ownedName);
        JsonOperationResult writeResult = WriteFormattedOutput(() =>
            formatter.FormatPropertyName(
                frame,
                GetContainerDepth(),
                ownedName));
        if (writeResult.Success)
        {
            frame.AcceptPropertyName();
        }

        return writeResult;
    }

    internal JsonOperationResult WriteEncodedScalarValue(
        global::System.ReadOnlySpan<byte> encodedValue)
    {
        JsonOperationResult? availability = ValidateValueAvailability(
            "Value");
        if (availability != null)
        {
            return availability;
        }

        JsonWriteFrame frame = GetCurrentFrame();
        bool additionalRoot = IsAdditionalRootStart();
        if (encodedValue.Length > maximumOutputBytes)
        {
            return CreateLimitFailure(
                JsonDiagnosticCode.OutputByteLimitExceeded,
                "The configured output byte limit was exceeded.",
                "Value");
        }

        byte[] ownedValue = new byte[encodedValue.Length];
        encodedValue.CopyTo(ownedValue);
        JsonOperationResult writeResult = WriteFormattedOutput(() =>
            AddRootFraming(formatter.FormatScalarValue(
                frame,
                GetContainerDepth(),
                ownedValue)));
        if (writeResult.Success)
        {
            if (additionalRoot)
            {
                frames[0] = new JsonWriteFrame(JsonContainerKind.Root);
                frame = frames[0];
            }

            frame.AcceptCompletedValue();
            if (frame.GetContainerKind() == JsonContainerKind.Root)
            {
                hasWrittenRoot = true;
            }
        }

        return writeResult;
    }

    internal JsonOperationResult FlushOutput()
    {
        return sink.FlushSink();
    }

    internal JsonOperationResult CompleteOutput()
    {
        if (completed)
        {
            return CreateSuccess(0);
        }

        if (frames.Count != 1 || frames[0].GetValueCount() != 1)
        {
            return CreateStateFailure(
                "The writer cannot complete until exactly one root value is closed.",
                "Complete root value",
                "Complete");
        }

        if (outputMode == JsonInputMode.JsonTextSequence &&
            !sequenceTerminatorWritten)
        {
            JsonOperationResult terminator = WriteOutput(
                new byte[] { JsonSyntaxBytes.LineFeed });
            if (!terminator.Success)
            {
                return terminator;
            }

            sequenceTerminatorWritten = true;
        }

        JsonOperationResult result = sink.FlushSink();
        if (result.Success)
        {
            completed = true;
        }

        return result;
    }

    internal long GetPendingByteCount()
    {
        return sink.GetBytesPending();
    }

    internal long GetCommittedByteCount()
    {
        return sink.GetBytesCommitted();
    }

    internal int GetContainerDepth()
    {
        return frames.Count - 1;
    }

    internal bool IsOutputCompleted()
    {
        return completed;
    }

    internal void ResetCore()
    {
        frames.Clear();
        frames.Add(new JsonWriteFrame(JsonContainerKind.Root));
        completed = false;
        hasWrittenRoot = false;
        sequenceTerminatorWritten = false;
        sink.ResetSink();
    }

    JsonOperationResult WriteContainerStart(
        JsonContainerKind kind,
        byte openingByte,
        string actualToken)
    {
        JsonOperationResult? availability = ValidateValueAvailability(
            actualToken);
        if (availability != null)
        {
            return availability;
        }

        int depth = GetContainerDepth();
        if (depth >= maximumNestingDepth)
        {
            return CreateLimitFailure(
                JsonDiagnosticCode.NestingDepthLimitExceeded,
                "The configured writer nesting depth limit was exceeded.",
                actualToken);
        }

        JsonWriteFrame parent = GetCurrentFrame();
        bool additionalRoot = IsAdditionalRootStart();
        JsonOperationResult result = WriteFormattedOutput(() =>
            AddRootFraming(formatter.FormatContainerStart(
                parent,
                GetContainerDepth(),
                openingByte)));
        if (result.Success)
        {
            if (additionalRoot)
            {
                frames[0] = new JsonWriteFrame(JsonContainerKind.Root);
            }

            if (parent.GetContainerKind() == JsonContainerKind.Root)
            {
                hasWrittenRoot = true;
            }

            frames.Add(new JsonWriteFrame(kind));
        }

        return result;
    }

    JsonOperationResult WriteContainerEnd(
        JsonContainerKind expectedKind,
        byte closingByte,
        string actualToken)
    {
        JsonOperationResult? availability = ValidateOperationAvailability(
            actualToken);
        if (availability != null)
        {
            return availability;
        }

        if (frames.Count == 1)
        {
            return CreateStateFailure(
                "No open container is available to close.",
                "Value",
                actualToken);
        }

        JsonWriteFrame frame = GetCurrentFrame();
        if (frame.GetContainerKind() != expectedKind)
        {
            return CreateStateFailure(
                "The requested container end does not match the open container.",
                frame.GetContainerKind() == JsonContainerKind.Array
                    ? "EndArray"
                    : "EndObject",
                actualToken);
        }

        if (!frame.CanCloseContainer())
        {
            return CreateStateFailure(
                "An object property value is required before the object can close.",
                "Value",
                actualToken);
        }

        JsonOperationResult result = WriteFormattedOutput(() =>
            formatter.FormatContainerEnd(
                frame,
                GetContainerDepth(),
                closingByte));
        if (result.Success)
        {
            frames.RemoveAt(frames.Count - 1);
            GetCurrentFrame().AcceptCompletedValue();
        }

        return result;
    }

    JsonOperationResult? ValidateValueAvailability(string actualToken)
    {
        JsonOperationResult? availability = ValidateOperationAvailability(
            actualToken);
        if (availability != null)
        {
            return availability;
        }

        JsonWriteFrame frame = GetCurrentFrame();
        if (!frame.CanAcceptValue())
        {
            if (frame.GetContainerKind() == JsonContainerKind.Root &&
                frame.GetValueCount() == 1 &&
                outputMode != JsonInputMode.SingleValue)
            {
                return null;
            }

            string expected = frame.GetContainerKind() == JsonContainerKind.Object
                ? "Property name"
                : "End of output";
            return CreateStateFailure(
                "A JSON value is not valid in the current writer state.",
                expected,
                actualToken);
        }

        if (frame.GetContainerKind() == JsonContainerKind.Array &&
            frame.GetValueCount() >= maximumArrayItems)
        {
            return CreateLimitFailure(
                JsonDiagnosticCode.ArrayItemLimitExceeded,
                "The configured array item limit was exceeded.",
                actualToken);
        }

        return null;
    }

    bool IsAdditionalRootStart()
    {
        JsonWriteFrame frame = GetCurrentFrame();
        return frame.GetContainerKind() == JsonContainerKind.Root &&
            frame.GetValueCount() == 1 &&
            outputMode != JsonInputMode.SingleValue;
    }

    byte[] AddRootFraming(byte[] valueBytes)
    {
        JsonWriteFrame frame = GetCurrentFrame();
        if (frame.GetContainerKind() != JsonContainerKind.Root)
        {
            return valueBytes;
        }

        int prefixLength = 0;
        if (outputMode == JsonInputMode.WhitespaceSeparated && hasWrittenRoot)
        {
            prefixLength = 1;
        }

        if (outputMode == JsonInputMode.JsonTextSequence)
        {
            prefixLength = hasWrittenRoot ? 2 : 1;
        }

        if (prefixLength == 0)
        {
            return valueBytes;
        }

        if (valueBytes.Length > maximumOutputBytes - prefixLength)
        {
            JsonDiagnostic diagnostic = new JsonDiagnostic();
            diagnostic.Code = JsonDiagnosticCode.OutputByteLimitExceeded;
            diagnostic.Message =
                "The configured output byte limit was exceeded by root framing.";
            diagnostic.ActualToken = "RootFraming";
            throw new JsonLimitException(diagnostic);
        }

        byte[] framed = new byte[prefixLength + valueBytes.Length];
        int offset = 0;
        if (outputMode == JsonInputMode.JsonTextSequence && hasWrittenRoot)
        {
            framed[offset] = JsonSyntaxBytes.LineFeed;
            offset++;
        }

        if (outputMode == JsonInputMode.JsonTextSequence)
        {
            framed[offset] = JsonSyntaxBytes.RecordSeparator;
            offset++;
        }
        else
        {
            framed[offset] = JsonSyntaxBytes.LineFeed;
            offset++;
        }

        global::System.Buffer.BlockCopy(
            valueBytes,
            0,
            framed,
            offset,
            valueBytes.Length);
        return framed;
    }

    JsonOperationResult? ValidateOperationAvailability(string actualToken)
    {
        if (completed)
        {
            return CreateStateFailure(
                "The writer has already completed.",
                "Reset",
                actualToken);
        }

        return null;
    }

    JsonWriteFrame GetCurrentFrame()
    {
        return frames[frames.Count - 1];
    }

    JsonOperationResult WriteFormattedOutput(
        global::System.Func<byte[]> createOutput)
    {
        byte[]? output = null;
        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => output = createOutput());
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            return CreateOutputFailure(failure);
        }

        return WriteOutput(output
            ?? throw new global::System.InvalidOperationException(
                "Successful formatting requires output bytes."));
    }

    JsonOperationResult WriteOutput(byte[] output)
    {
        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => sink.WriteBytes(output));
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure == null)
        {
            return CreateSuccess(output.Length);
        }

        return CreateOutputFailure(failure);
    }

    JsonOperationResult CreateOutputFailure(
        global::System.Exception failure)
    {
        JsonLimitException? limitFailure = failure as JsonLimitException;
        if (limitFailure != null)
        {
            return CreateFailure(limitFailure.Diagnostic);
        }

        JsonWriteException? writeFailure = failure as JsonWriteException;
        if (writeFailure != null)
        {
            return CreateFailure(writeFailure.Diagnostic);
        }

        throw failure;
    }

    JsonOperationResult CreateSuccess(long bytesWritten)
    {
        JsonOperationResult result = new JsonOperationResult();
        result.Success = true;
        result.BytesWritten = bytesWritten;
        return result;
    }

    JsonOperationResult CreateFailure(JsonDiagnostic diagnostic)
    {
        JsonOperationResult result = new JsonOperationResult();
        result.Success = false;
        result.Diagnostic = diagnostic;
        return result;
    }

    JsonOperationResult CreateStateFailure(
        string message,
        string expectedTokens,
        string actualToken)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.InvalidWriterState;
        diagnostic.Message = message;
        diagnostic.Depth = GetContainerDepth();
        diagnostic.ExpectedTokens = expectedTokens;
        diagnostic.ActualToken = actualToken;
        return CreateFailure(diagnostic);
    }

    JsonOperationResult CreateLimitFailure(
        JsonDiagnosticCode code,
        string message,
        string actualToken)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Depth = GetContainerDepth();
        diagnostic.ActualToken = actualToken;
        return CreateFailure(diagnostic);
    }
}
