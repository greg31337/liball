namespace http;

internal class Http1LimitTracker
{
    HttpLimits limits = new HttpLimits();
    long fieldSectionSize;
    int fieldCount;
    long contentSize;
    long trailerSectionSize;
    int trailerCount;
    long bufferedInputSize;
    long bufferedEventSize;

    internal string? ErrorCode;
    internal string? ErrorMessage;

    internal void Reset(HttpLimits value)
    {
        if (value == null || value.MaximumStartLineSize < 0 ||
            value.MaximumFieldLineSize < 0 ||
            value.MaximumFieldSectionSize < 0 ||
            value.MaximumFieldCount < 0 ||
            value.MaximumFieldValueSize < 0 ||
            value.MaximumTrailerSectionSize < 0 ||
            value.MaximumTrailerCount < 0 ||
            value.MaximumContentSize < 0 ||
            value.MaximumChunkSize < 0 ||
            value.MaximumBufferedSize < 0)
        {
            throw new HttpProtocolException(
                "The HTTP/1 resource limits are incomplete or invalid.",
                "invalid-http1-parser-options");
        }

        limits = value;
        fieldSectionSize = 0;
        fieldCount = 0;
        contentSize = 0;
        trailerSectionSize = 0;
        trailerCount = 0;
        bufferedInputSize = 0;
        bufferedEventSize = 0;
        ClearError();
    }

    internal bool CheckStartLineSize(int size)
    {
        ClearError();
        if (size < 0)
        {
            return RejectInvalidSize();
        }

        if (size > limits.MaximumStartLineSize)
        {
            return Reject(
                "http1-start-line-limit-exceeded",
                "The HTTP/1 start line exceeds the configured limit.");
        }

        return true;
    }

    internal bool CheckFieldLineSize(int size)
    {
        ClearError();
        if (size < 0)
        {
            return RejectInvalidSize();
        }

        if (size > limits.MaximumFieldLineSize)
        {
            return Reject(
                "http1-field-line-limit-exceeded",
                "An HTTP/1 field line exceeds the configured limit.");
        }

        return true;
    }

    internal bool TrackFieldLine(int lineSize, long valueSize, bool addsField)
    {
        ClearError();
        if (lineSize < 0 || valueSize < 0)
        {
            return RejectInvalidSize();
        }

        if (lineSize > limits.MaximumFieldLineSize)
        {
            return Reject(
                "http1-field-line-limit-exceeded",
                "An HTTP/1 field line exceeds the configured limit.");
        }

        if (valueSize > limits.MaximumFieldValueSize)
        {
            return Reject(
                "http1-field-value-limit-exceeded",
                "An HTTP/1 field value exceeds the configured limit.");
        }

        long nextSectionSize = fieldSectionSize + lineSize;
        if (nextSectionSize > limits.MaximumFieldSectionSize)
        {
            return Reject(
                "http1-field-section-limit-exceeded",
                "The HTTP/1 field section exceeds the configured limit.");
        }

        int nextFieldCount = fieldCount;
        if (addsField)
        {
            nextFieldCount = nextFieldCount + 1;
        }

        if (nextFieldCount > limits.MaximumFieldCount)
        {
            return Reject(
                "http1-field-count-limit-exceeded",
                "The HTTP/1 field count exceeds the configured limit.");
        }

        fieldSectionSize = nextSectionSize;
        fieldCount = nextFieldCount;
        return true;
    }

    internal bool AddContentSize(int size)
    {
        ClearError();
        if (size < 0)
        {
            return RejectInvalidSize();
        }

        long nextSize = contentSize + size;
        if (nextSize > limits.MaximumContentSize)
        {
            return Reject(
                "http1-content-limit-exceeded",
                "The retained HTTP/1 content exceeds the configured limit.");
        }

        contentSize = nextSize;
        return true;
    }

    internal bool CheckContentSize(long size)
    {
        ClearError();
        if (size < 0)
        {
            return RejectInvalidSize();
        }

        if (size > limits.MaximumContentSize)
        {
            return Reject(
                "http1-content-limit-exceeded",
                "The retained HTTP/1 content exceeds the configured limit.");
        }

        return true;
    }

    internal bool CheckChunkSize(long size)
    {
        ClearError();
        if (size < 0)
        {
            return RejectInvalidSize();
        }

        if (size > limits.MaximumChunkSize)
        {
            return Reject(
                "http1-chunk-limit-exceeded",
                "An HTTP/1 chunk exceeds the configured limit.");
        }

        return true;
    }

    internal bool TrackTrailerLine(int lineSize, long valueSize, bool addsField)
    {
        ClearError();
        if (lineSize < 0 || valueSize < 0)
        {
            return RejectInvalidSize();
        }

        if (lineSize > limits.MaximumFieldLineSize)
        {
            return Reject(
                "http1-field-line-limit-exceeded",
                "An HTTP/1 trailer field line exceeds the configured limit.");
        }

        if (valueSize > limits.MaximumFieldValueSize)
        {
            return Reject(
                "http1-field-value-limit-exceeded",
                "An HTTP/1 trailer field value exceeds the configured limit.");
        }

        long nextSectionSize = trailerSectionSize + lineSize;
        if (nextSectionSize > limits.MaximumTrailerSectionSize)
        {
            return Reject(
                "http1-trailer-section-limit-exceeded",
                "The HTTP/1 trailer section exceeds the configured limit.");
        }

        int nextTrailerCount = trailerCount;
        if (addsField)
        {
            nextTrailerCount = nextTrailerCount + 1;
        }

        if (nextTrailerCount > limits.MaximumTrailerCount)
        {
            return Reject(
                "http1-trailer-count-limit-exceeded",
                "The HTTP/1 trailer count exceeds the configured limit.");
        }

        trailerSectionSize = nextSectionSize;
        trailerCount = nextTrailerCount;
        return true;
    }

    internal bool TrackBufferedInput(int size)
    {
        ClearError();
        if (size < 0)
        {
            return RejectInvalidSize();
        }

        if ((long)size + bufferedEventSize > limits.MaximumBufferedSize)
        {
            return Reject(
                "http1-buffer-limit-exceeded",
                "The buffered HTTP/1 input and events exceed the configured limit.");
        }

        bufferedInputSize = size;
        return true;
    }

    internal bool AddBufferedEventSize(int size)
    {
        ClearError();
        if (size < 0)
        {
            return RejectInvalidSize();
        }

        long nextSize = bufferedEventSize + size;
        if (bufferedInputSize + nextSize > limits.MaximumBufferedSize)
        {
            return Reject(
                "http1-buffer-limit-exceeded",
                "The buffered HTTP/1 input and events exceed the configured limit.");
        }

        bufferedEventSize = nextSize;
        return true;
    }

    internal bool RemoveBufferedEventSize(int size)
    {
        ClearError();
        if (size < 0 || size > bufferedEventSize)
        {
            return RejectInvalidSize();
        }

        bufferedEventSize = bufferedEventSize - size;
        return true;
    }

    bool RejectInvalidSize()
    {
        return Reject(
            "invalid-http1-limit-size",
            "An HTTP/1 resource size cannot be negative or exceed retained data.");
    }

    bool Reject(string code, string message)
    {
        ErrorCode = code;
        ErrorMessage = message;
        return false;
    }

    void ClearError()
    {
        ErrorCode = null;
        ErrorMessage = null;
    }
}
