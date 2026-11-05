namespace json;

internal class JsonDiagnosticFactory
{
    const int MaximumContextCharacters = 256;

    int maximumExcerptBytes;

    internal JsonDiagnosticFactory(int maximumExcerptBytes)
    {
        JsonOptionNormalizer.RejectNegativeLimit(maximumExcerptBytes, "MaximumDiagnosticExcerptBytes");
        this.maximumExcerptBytes = maximumExcerptBytes == 0
            ? JsonDefaults.MaximumDiagnosticExcerptBytes
            : maximumExcerptBytes;
    }

    internal int GetMaximumExcerptBytes()
    {
        return maximumExcerptBytes;
    }

    internal JsonDiagnostic CreateDiagnostic(
        JsonDiagnosticCode code,
        string message,
        JsonSourceRange? range,
        string? pointer,
        int depth,
        string? expectedTokens,
        string? actualToken,
        global::System.ReadOnlySpan<byte> sourceBytes,
        long sourceByteOffset)
    {
        int numericCode = (int)code;
        if (numericCode < (int)JsonDiagnosticCode.None || numericCode > (int)JsonDiagnosticCode.IoFailure)
        {
            throw new global::System.ArgumentOutOfRangeException("code");
        }

        if (message == null)
        {
            throw new global::System.ArgumentNullException("message");
        }

        if (depth < 0)
        {
            throw new global::System.ArgumentOutOfRangeException("depth");
        }

        if (sourceByteOffset < 0)
        {
            throw new global::System.ArgumentOutOfRangeException("sourceByteOffset");
        }

        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Range = CopyRange(range);
        diagnostic.Pointer = pointer;
        diagnostic.Depth = depth;
        diagnostic.ExpectedTokens = BoundContext(expectedTokens);
        diagnostic.ActualToken = BoundContext(actualToken);
        diagnostic.SourceExcerpt = CopyExcerpt(sourceBytes, sourceByteOffset, range);
        return diagnostic;
    }

    byte[]? CopyExcerpt(
        global::System.ReadOnlySpan<byte> sourceBytes,
        long sourceByteOffset,
        JsonSourceRange? range)
    {
        if (sourceBytes.Length == 0)
        {
            return null;
        }

        int excerptLength = sourceBytes.Length < maximumExcerptBytes
            ? sourceBytes.Length
            : maximumExcerptBytes;
        int excerptStart = 0;
        if (sourceBytes.Length > excerptLength)
        {
            int focus = GetExcerptFocus(sourceBytes.Length, sourceByteOffset, range);
            excerptStart = focus - excerptLength / 2;
            if (excerptStart < 0)
            {
                excerptStart = 0;
            }

            int latestStart = sourceBytes.Length - excerptLength;
            if (excerptStart > latestStart)
            {
                excerptStart = latestStart;
            }
        }

        byte[] excerpt = new byte[excerptLength];
        sourceBytes.Slice(excerptStart, excerptLength).CopyTo(excerpt);
        return excerpt;
    }

    static int GetExcerptFocus(int sourceLength, long sourceByteOffset, JsonSourceRange? range)
    {
        if (range == null || range.Start == null)
        {
            return 0;
        }

        long failureOffset = range.Start.ByteOffset;
        if (failureOffset <= sourceByteOffset)
        {
            return 0;
        }

        long relativeOffset = failureOffset - sourceByteOffset;
        if (relativeOffset >= sourceLength)
        {
            return sourceLength;
        }

        return (int)relativeOffset;
    }

    static JsonSourceRange? CopyRange(JsonSourceRange? range)
    {
        if (range == null)
        {
            return null;
        }

        JsonSourceRange copy = new JsonSourceRange();
        copy.Start = CopyPosition(range.Start);
        copy.End = CopyPosition(range.End);
        return copy;
    }

    static JsonSourcePosition? CopyPosition(JsonSourcePosition? position)
    {
        if (position == null)
        {
            return null;
        }

        JsonSourcePosition copy = new JsonSourcePosition();
        copy.ByteOffset = position.ByteOffset;
        copy.Line = position.Line;
        copy.ByteColumn = position.ByteColumn;
        copy.ScalarColumn = position.ScalarColumn;
        return copy;
    }

    static string? BoundContext(string? context)
    {
        if (context == null || context.Length <= MaximumContextCharacters)
        {
            return context;
        }

        int length = MaximumContextCharacters;
        if (global::System.Char.IsHighSurrogate(context[length - 1]) &&
            global::System.Char.IsLowSurrogate(context[length]))
        {
            length--;
        }

        return context.Substring(0, length);
    }
}
