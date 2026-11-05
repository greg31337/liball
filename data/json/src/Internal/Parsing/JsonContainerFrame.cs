namespace json;

internal class JsonContainerFrame
{
    JsonContainerKind kind;
    JsonContainerState state;
    int valueCount;
    int memberCount;

    internal JsonContainerFrame(JsonContainerKind kind)
    {
        if (kind != JsonContainerKind.Root &&
            kind != JsonContainerKind.Array &&
            kind != JsonContainerKind.Object)
        {
            throw new global::System.ArgumentOutOfRangeException("kind");
        }

        this.kind = kind;
        state = GetInitialState(kind);
        valueCount = 0;
        memberCount = 0;
    }

    internal JsonContainerKind GetContainerKind()
    {
        return kind;
    }

    internal JsonContainerState GetContainerState()
    {
        return state;
    }

    internal int GetValueCount()
    {
        return valueCount;
    }

    internal int GetMemberCount()
    {
        return memberCount;
    }

    internal bool CanAcceptValue()
    {
        return state == JsonContainerState.RootValue ||
            state == JsonContainerState.ArrayFirstValueOrEnd ||
            state == JsonContainerState.ArrayValue ||
            state == JsonContainerState.ObjectValue;
    }

    internal bool CanAcceptPropertyName()
    {
        return state == JsonContainerState.ObjectFirstNameOrEnd ||
            state == JsonContainerState.ObjectName;
    }

    internal bool CanAcceptNameSeparator()
    {
        return state == JsonContainerState.ObjectColon;
    }

    internal bool CanAcceptValueSeparator()
    {
        return state == JsonContainerState.ArrayCommaOrEnd ||
            state == JsonContainerState.ObjectCommaOrEnd;
    }

    internal bool CanCloseContainer()
    {
        return state == JsonContainerState.ArrayFirstValueOrEnd ||
            state == JsonContainerState.ArrayCommaOrEnd ||
            state == JsonContainerState.ObjectFirstNameOrEnd ||
            state == JsonContainerState.ObjectCommaOrEnd;
    }

    internal bool IsComplete()
    {
        return state == JsonContainerState.RootComplete || state == JsonContainerState.Closed;
    }

    internal void AcceptValue()
    {
        if (state == JsonContainerState.RootValue)
        {
            state = JsonContainerState.RootComplete;
            valueCount++;
            return;
        }

        if (state == JsonContainerState.ArrayFirstValueOrEnd || state == JsonContainerState.ArrayValue)
        {
            state = JsonContainerState.ArrayCommaOrEnd;
            valueCount++;
            return;
        }

        if (state == JsonContainerState.ObjectValue)
        {
            state = JsonContainerState.ObjectCommaOrEnd;
            memberCount++;
            return;
        }

        if (state == JsonContainerState.RootComplete)
        {
            throw CreateFrameFailure(JsonDiagnosticCode.TrailingContent, "Content follows the complete root JSON value.");
        }

        if (state == JsonContainerState.ObjectFirstNameOrEnd || state == JsonContainerState.ObjectName)
        {
            throw CreateFrameFailure(JsonDiagnosticCode.MissingPropertyName, "An object member name is required before its value.");
        }

        if (state == JsonContainerState.ObjectColon)
        {
            throw CreateFrameFailure(JsonDiagnosticCode.MissingColon, "A colon is required after the object member name.");
        }

        if (state == JsonContainerState.ArrayCommaOrEnd || state == JsonContainerState.ObjectCommaOrEnd)
        {
            throw CreateFrameFailure(JsonDiagnosticCode.MissingCommaOrEnd, "A comma or container end is required before another value.");
        }

        throw CreateFrameFailure(JsonDiagnosticCode.UnexpectedToken, "A JSON value is not valid in the current container state.");
    }

    internal void AcceptPropertyName()
    {
        if (state == JsonContainerState.ObjectFirstNameOrEnd || state == JsonContainerState.ObjectName)
        {
            state = JsonContainerState.ObjectColon;
            return;
        }

        if (state == JsonContainerState.ObjectCommaOrEnd)
        {
            throw CreateFrameFailure(JsonDiagnosticCode.MissingCommaOrEnd, "A comma or object end is required before another member name.");
        }

        if (state == JsonContainerState.ObjectColon)
        {
            throw CreateFrameFailure(JsonDiagnosticCode.MissingColon, "A colon is required after the object member name.");
        }

        throw CreateFrameFailure(JsonDiagnosticCode.UnexpectedToken, "An object member name is not valid in the current container state.");
    }

    internal void AcceptNameSeparator()
    {
        if (state == JsonContainerState.ObjectColon)
        {
            state = JsonContainerState.ObjectValue;
            return;
        }

        if (state == JsonContainerState.ObjectFirstNameOrEnd || state == JsonContainerState.ObjectName)
        {
            throw CreateFrameFailure(JsonDiagnosticCode.MissingPropertyName, "An object member name is required before a colon.");
        }

        if (state == JsonContainerState.ObjectCommaOrEnd)
        {
            throw CreateFrameFailure(JsonDiagnosticCode.MissingCommaOrEnd, "A comma or object end is required before another member.");
        }

        throw CreateFrameFailure(JsonDiagnosticCode.UnexpectedToken, "A colon is not valid in the current container state.");
    }

    internal void AcceptValueSeparator()
    {
        if (state == JsonContainerState.ArrayCommaOrEnd)
        {
            state = JsonContainerState.ArrayValue;
            return;
        }

        if (state == JsonContainerState.ObjectCommaOrEnd)
        {
            state = JsonContainerState.ObjectName;
            return;
        }

        if (state == JsonContainerState.ObjectColon)
        {
            throw CreateFrameFailure(JsonDiagnosticCode.MissingColon, "A colon is required after the object member name.");
        }

        if (state == JsonContainerState.ObjectName)
        {
            throw CreateFrameFailure(JsonDiagnosticCode.MissingPropertyName, "An object member name is required after a comma.");
        }

        throw CreateFrameFailure(JsonDiagnosticCode.UnexpectedToken, "A comma is not valid in the current container state.");
    }

    internal void CloseContainer()
    {
        if (state == JsonContainerState.ArrayFirstValueOrEnd ||
            state == JsonContainerState.ArrayCommaOrEnd ||
            state == JsonContainerState.ObjectFirstNameOrEnd ||
            state == JsonContainerState.ObjectCommaOrEnd)
        {
            state = JsonContainerState.Closed;
            return;
        }

        if (state == JsonContainerState.ArrayValue || state == JsonContainerState.ObjectName)
        {
            throw CreateFrameFailure(JsonDiagnosticCode.TrailingComma, "A container cannot end immediately after a comma.");
        }

        if (state == JsonContainerState.ObjectColon)
        {
            throw CreateFrameFailure(JsonDiagnosticCode.MissingColon, "A colon and value are required after the object member name.");
        }

        throw CreateFrameFailure(JsonDiagnosticCode.UnexpectedToken, "The container cannot end in the current state.");
    }

    internal void CompleteInput()
    {
        if (state == JsonContainerState.RootComplete || state == JsonContainerState.Closed)
        {
            return;
        }

        throw CreateFrameFailure(JsonDiagnosticCode.UnexpectedEndOfInput, "Input ended before the JSON value was complete.");
    }

    internal void ResetFrame()
    {
        state = GetInitialState(kind);
        valueCount = 0;
        memberCount = 0;
    }

    static JsonContainerState GetInitialState(JsonContainerKind kind)
    {
        if (kind == JsonContainerKind.Root)
        {
            return JsonContainerState.RootValue;
        }

        if (kind == JsonContainerKind.Array)
        {
            return JsonContainerState.ArrayFirstValueOrEnd;
        }

        return JsonContainerState.ObjectFirstNameOrEnd;
    }

    static JsonParseException CreateFrameFailure(JsonDiagnosticCode code, string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        return new JsonParseException(diagnostic);
    }
}
