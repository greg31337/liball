namespace json;

internal class JsonStreamReadResult
{
    internal byte[]? Bytes;
    internal bool EndOfStream;
    internal JsonDiagnostic? Diagnostic;
}

internal class JsonStreamInput
{
    const int MaximumConsecutiveEmptyReads = 16;

    global::System.IO.Stream stream;
    byte[] buffer;
    int readCount;
    bool completed;
    bool failed;
    JsonDiagnostic? failureDiagnostic;

    internal JsonStreamInput(global::System.IO.Stream stream, int bufferBytes)
    {
        if (stream == null)
        {
            throw new global::System.ArgumentNullException("stream");
        }

        if (bufferBytes <= 0)
        {
            throw new global::System.ArgumentOutOfRangeException("bufferBytes");
        }

        this.stream = stream;
        buffer = new byte[bufferBytes];
    }

    internal JsonStreamReadResult ReadStreamBlock()
    {
        JsonStreamReadResult result = new JsonStreamReadResult();
        if (failed)
        {
            result.Diagnostic = failureDiagnostic;
            return result;
        }

        if (completed)
        {
            result.EndOfStream = true;
            return result;
        }

        readCount = 0;
        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => ExecuteStreamRead());
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            if (!IsInputFailure(failure))
            {
                throw failure;
            }

            failed = true;
            failureDiagnostic = CreateIoDiagnostic(failure.Message);
            result.Diagnostic = failureDiagnostic;
            return result;
        }

        if (readCount == 0)
        {
            completed = true;
            result.EndOfStream = true;
            return result;
        }

        byte[] bytes = new byte[readCount];
        global::System.Buffer.BlockCopy(buffer, 0, bytes, 0, readCount);
        result.Bytes = bytes;
        return result;
    }

    internal bool IsStreamCompleted()
    {
        return completed;
    }

    void ExecuteStreamRead()
    {
        int emptyReadCount = 0;
        while (true)
        {
            readCount = stream.Read(buffer, 0, buffer.Length);
            if (readCount < 0 || readCount > buffer.Length)
            {
                throw new global::System.IO.IOException("The input stream returned an invalid byte count.");
            }

            if (readCount != 0 || !HasKnownRemainingBytes())
            {
                return;
            }

            emptyReadCount++;
            if (emptyReadCount >= MaximumConsecutiveEmptyReads)
            {
                throw new global::System.IO.IOException("The input stream repeatedly returned no bytes before its reported end.");
            }
        }
    }

    bool HasKnownRemainingBytes()
    {
        return stream.CanSeek && stream.Position < stream.Length;
    }

    bool IsInputFailure(global::System.Exception failure)
    {
        return failure as global::System.IO.IOException != null ||
            failure as global::System.NotSupportedException != null ||
            failure as global::System.ObjectDisposedException != null;
    }

    JsonDiagnostic CreateIoDiagnostic(string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.IoFailure;
        diagnostic.Message = "Reading the caller-owned input stream failed: " + message;
        return diagnostic;
    }
}
