namespace json;

internal class JsonByteSink
{
    global::System.IO.Stream? stream;
    global::System.Buffers.IBufferWriter<byte>? bufferWriter;
    byte[] buffer;
    int pendingByteCount;
    int maximumOutputBytes;
    long committedByteCount;
    long currentFlushByteCount;
    bool failed;
    JsonDiagnostic? failureDiagnostic;

    internal JsonByteSink(JsonWriterTarget target, int maximumOutputBytes)
    {
        JsonWriterTarget normalizedTarget =
            JsonOptionNormalizer.NormalizeWriterTarget(target);
        if (maximumOutputBytes < 0)
        {
            throw new JsonLimitException(
                JsonOptionNormalizer.CreateLimitDiagnostic(
                    "MaximumOutputBytes cannot be negative."));
        }

        this.maximumOutputBytes = maximumOutputBytes == 0
            ? JsonDefaults.MaximumOutputBytes
            : maximumOutputBytes;
        stream = normalizedTarget.Stream;
        bufferWriter = normalizedTarget.BufferWriter;
        int initialCapacity = JsonDefaults.StreamBufferBytes;
        if (initialCapacity > this.maximumOutputBytes)
        {
            initialCapacity = this.maximumOutputBytes;
        }

        buffer = new byte[initialCapacity];
    }

    internal void WriteByte(byte value)
    {
        EnsureWritable();
        EnsureOutputCapacity(1);
        EnsureBufferCapacity(1);
        buffer[pendingByteCount] = value;
        pendingByteCount++;
    }

    internal void WriteBytes(global::System.ReadOnlySpan<byte> bytes)
    {
        EnsureWritable();
        EnsureOutputCapacity(bytes.Length);
        EnsureBufferCapacity(bytes.Length);
        bytes.CopyTo(new global::System.Span<byte>(
            buffer,
            pendingByteCount,
            bytes.Length));
        pendingByteCount += bytes.Length;
    }

    internal JsonOperationResult FlushSink()
    {
        if (failed)
        {
            return CreateFlushFailure(0);
        }

        currentFlushByteCount = 0;
        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => ExecuteFlush());
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            if (!IsDestinationFailure(failure))
            {
                throw failure;
            }

            failed = true;
            failureDiagnostic = CreateIoDiagnostic(failure.Message);
            return CreateFlushFailure(currentFlushByteCount);
        }

        JsonOperationResult result = new JsonOperationResult();
        result.Success = true;
        result.BytesWritten = currentFlushByteCount;
        return result;
    }

    internal long GetBytesPending()
    {
        return pendingByteCount;
    }

    internal long GetBytesCommitted()
    {
        return committedByteCount;
    }

    internal void ResetSink()
    {
        pendingByteCount = 0;
        committedByteCount = 0;
        currentFlushByteCount = 0;
        failed = false;
        failureDiagnostic = null;
    }

    void EnsureWritable()
    {
        if (failed)
        {
            JsonDiagnostic diagnostic = failureDiagnostic
                ?? throw new global::System.InvalidOperationException(
                    "A failed byte sink requires a diagnostic.");
            throw new JsonWriteException(diagnostic);
        }
    }

    void EnsureOutputCapacity(int appendLength)
    {
        long currentTotal = committedByteCount + pendingByteCount;
        if (appendLength > maximumOutputBytes - currentTotal)
        {
            throw new JsonLimitException(CreateOutputLimitDiagnostic());
        }
    }

    void EnsureBufferCapacity(int appendLength)
    {
        int required = pendingByteCount + appendLength;
        if (required <= buffer.Length)
        {
            return;
        }

        int newCapacity = buffer.Length;
        if (newCapacity == 0)
        {
            newCapacity = 1;
        }

        while (newCapacity < required)
        {
            if (newCapacity > maximumOutputBytes / 2)
            {
                newCapacity = maximumOutputBytes;
            }
            else
            {
                newCapacity *= 2;
            }
        }

        byte[] replacement = new byte[newCapacity];
        if (pendingByteCount > 0)
        {
            global::System.Buffer.BlockCopy(
                buffer,
                0,
                replacement,
                0,
                pendingByteCount);
        }

        buffer = replacement;
    }

    void ExecuteFlush()
    {
        if (stream != null)
        {
            if (pendingByteCount > 0)
            {
                int written = pendingByteCount;
                stream.Write(buffer, 0, written);
                pendingByteCount = 0;
                committedByteCount += written;
                currentFlushByteCount = written;
            }

            stream.Flush();
            return;
        }

        global::System.Buffers.IBufferWriter<byte> destination = bufferWriter
            ?? throw new global::System.InvalidOperationException(
                "A byte sink requires one output destination.");
        if (pendingByteCount == 0)
        {
            return;
        }

        global::System.Span<byte> destinationSpan =
            destination.GetSpan(pendingByteCount);
        if (destinationSpan.Length < pendingByteCount)
        {
            throw new global::System.InvalidOperationException(
                "The buffer writer returned less space than requested.");
        }

        new global::System.ReadOnlySpan<byte>(
            buffer,
            0,
            pendingByteCount).CopyTo(destinationSpan);
        int advanced = pendingByteCount;
        destination.Advance(advanced);
        pendingByteCount = 0;
        committedByteCount += advanced;
        currentFlushByteCount = advanced;
    }

    bool IsDestinationFailure(global::System.Exception failure)
    {
        return failure as global::System.IO.IOException != null ||
            failure as global::System.NotSupportedException != null ||
            failure as global::System.ObjectDisposedException != null ||
            failure as global::System.InvalidOperationException != null ||
            failure as global::System.ArgumentException != null;
    }

    JsonDiagnostic CreateOutputLimitDiagnostic()
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.OutputByteLimitExceeded;
        diagnostic.Message = "The configured output byte limit was exceeded.";
        diagnostic.ActualToken =
            (committedByteCount + pendingByteCount).ToString(
                global::System.Globalization.CultureInfo.InvariantCulture);
        return diagnostic;
    }

    JsonDiagnostic CreateIoDiagnostic(string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.IoFailure;
        diagnostic.Message =
            "Writing to the caller-owned output destination failed: " + message;
        return diagnostic;
    }

    JsonOperationResult CreateFlushFailure(long bytesWritten)
    {
        JsonOperationResult result = new JsonOperationResult();
        result.Success = false;
        result.BytesWritten = bytesWritten;
        result.Diagnostic = failureDiagnostic;
        return result;
    }
}
