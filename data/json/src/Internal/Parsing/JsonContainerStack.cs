namespace json;

internal class JsonContainerStack
{
    JsonContainerFrame[] frames;
    int count;
    int maximumDepth;

    internal JsonContainerStack(int maximumDepth)
    {
        JsonOptionNormalizer.RejectNegativeLimit(maximumDepth, "MaximumNestingDepth");
        this.maximumDepth = maximumDepth == 0 ? JsonDefaults.MaximumNestingDepth : maximumDepth;
        int initialCapacity = this.maximumDepth < 7 ? this.maximumDepth + 1 : 8;
        frames = new JsonContainerFrame[initialCapacity];
        frames[0] = new JsonContainerFrame(JsonContainerKind.Root);
        count = 1;
    }

    internal JsonContainerFrame GetCurrentFrame()
    {
        return frames[count - 1];
    }

    internal int GetFrameCount()
    {
        return count;
    }

    internal int GetContainerDepth()
    {
        return count - 1;
    }

    internal bool IsDocumentComplete()
    {
        return count == 1 && frames[0].IsComplete();
    }

    internal void AcceptScalarValue()
    {
        GetCurrentFrame().AcceptValue();
    }

    internal void AcceptPropertyName()
    {
        GetCurrentFrame().AcceptPropertyName();
    }

    internal void AcceptNameSeparator()
    {
        GetCurrentFrame().AcceptNameSeparator();
    }

    internal void AcceptValueSeparator()
    {
        GetCurrentFrame().AcceptValueSeparator();
    }

    internal void OpenArray()
    {
        OpenContainer(JsonContainerKind.Array);
    }

    internal void OpenObject()
    {
        OpenContainer(JsonContainerKind.Object);
    }

    internal void CloseArray()
    {
        CloseContainer(JsonContainerKind.Array, "]");
    }

    internal void CloseObject()
    {
        CloseContainer(JsonContainerKind.Object, "}");
    }

    internal void CompleteInput()
    {
        GetCurrentFrame().CompleteInput();
        if (count != 1)
        {
            throw CreateStackFailure(JsonDiagnosticCode.UnexpectedEndOfInput, "Input ended before all JSON containers were closed.");
        }
    }

    internal void ResetStack()
    {
        frames[0].ResetFrame();
        count = 1;
    }

    void OpenContainer(JsonContainerKind kind)
    {
        JsonContainerFrame parent = GetCurrentFrame();
        if (!parent.CanAcceptValue())
        {
            parent.AcceptValue();
        }

        if (count - 1 >= maximumDepth)
        {
            throw CreateDepthFailure();
        }

        EnsureCapacity();
        parent.AcceptValue();
        frames[count] = new JsonContainerFrame(kind);
        count++;
    }

    void CloseContainer(JsonContainerKind kind, string token)
    {
        if (count == 1 || GetCurrentFrame().GetContainerKind() != kind)
        {
            throw CreateStackFailure(JsonDiagnosticCode.UnexpectedToken, $"The token {token} does not close the current container.");
        }

        GetCurrentFrame().CloseContainer();
        count--;
    }

    void EnsureCapacity()
    {
        if (count < frames.Length)
        {
            return;
        }

        int maximumCapacity = maximumDepth == int.MaxValue ? int.MaxValue : maximumDepth + 1;
        int newCapacity = frames.Length > int.MaxValue / 2 ? int.MaxValue : frames.Length * 2;
        if (newCapacity > maximumCapacity)
        {
            newCapacity = maximumCapacity;
        }

        JsonContainerFrame[] expanded = new JsonContainerFrame[newCapacity];
        global::System.Array.Copy(frames, expanded, count);
        frames = expanded;
    }

    JsonLimitException CreateDepthFailure()
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.NestingDepthLimitExceeded;
        diagnostic.Message = "The configured JSON container nesting depth limit was exceeded.";
        diagnostic.Depth = count - 1;
        return new JsonLimitException(diagnostic);
    }

    JsonParseException CreateStackFailure(JsonDiagnosticCode code, string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Depth = count - 1;
        return new JsonParseException(diagnostic);
    }
}
