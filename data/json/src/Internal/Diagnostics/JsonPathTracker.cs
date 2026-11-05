namespace json;

internal class JsonPathFrame
{
    internal JsonContainerKind Kind;
    internal int ArrayIndex;
    internal string? PendingPropertyName;
    internal int ParentSegmentCount;

    internal JsonPathFrame(JsonContainerKind kind, int parentSegmentCount)
    {
        Kind = kind;
        ArrayIndex = 0;
        PendingPropertyName = null;
        ParentSegmentCount = parentSegmentCount;
    }
}

internal class JsonPathTracker
{
    global::System.Collections.Generic.List<string> segments;
    global::System.Collections.Generic.List<JsonPathFrame> frames;
    bool rootValueComplete;

    internal JsonPathTracker()
    {
        segments = new global::System.Collections.Generic.List<string>();
        frames = new global::System.Collections.Generic.List<JsonPathFrame>();
        rootValueComplete = false;
    }

    internal int GetDepth()
    {
        return frames.Count;
    }

    internal bool IsRootValueComplete()
    {
        return rootValueComplete;
    }

    internal JsonPointer GetCurrentPointer()
    {
        if (frames.Count == 0)
        {
            return CreateCurrentContainerPointer();
        }

        JsonPathFrame frame = frames[frames.Count - 1];
        if (frame.Kind == JsonContainerKind.Array)
        {
            return CreatePointerWithSegment(FormatArrayIndex(frame.ArrayIndex));
        }

        if (frame.PendingPropertyName != null)
        {
            return CreatePointerWithSegment(frame.PendingPropertyName);
        }

        return CreateCurrentContainerPointer();
    }

    internal JsonPointer AcceptPropertyName(string decodedName)
    {
        if (decodedName == null)
        {
            throw new global::System.ArgumentNullException("decodedName");
        }

        if (frames.Count == 0)
        {
            throw CreateInvalidState("A property name requires an open object.");
        }

        JsonPathFrame frame = frames[frames.Count - 1];
        if (frame.Kind != JsonContainerKind.Object)
        {
            throw CreateInvalidState("A property name is valid only inside an object.");
        }

        if (frame.PendingPropertyName != null)
        {
            throw CreateInvalidState("The current object property still requires a value.");
        }

        frame.PendingPropertyName = decodedName;
        return CreatePointerWithSegment(decodedName);
    }

    internal JsonPointer AcceptScalarValue()
    {
        JsonPointer pointer = CreateValuePointer();
        CompleteCurrentValue();
        return pointer;
    }

    internal JsonPointer OpenArray()
    {
        return OpenContainer(JsonContainerKind.Array);
    }

    internal JsonPointer OpenObject()
    {
        return OpenContainer(JsonContainerKind.Object);
    }

    internal JsonPointer CloseArray()
    {
        return CloseContainer(JsonContainerKind.Array);
    }

    internal JsonPointer CloseObject()
    {
        return CloseContainer(JsonContainerKind.Object);
    }

    internal void ResetPath()
    {
        segments.Clear();
        frames.Clear();
        rootValueComplete = false;
    }

    JsonPointer OpenContainer(JsonContainerKind kind)
    {
        JsonPointer pointer = CreateValuePointer();
        int parentSegmentCount = segments.Count;
        if (frames.Count != 0)
        {
            segments.Add(GetActiveValueSegment());
        }

        frames.Add(new JsonPathFrame(kind, parentSegmentCount));
        return pointer;
    }

    JsonPointer CloseContainer(JsonContainerKind kind)
    {
        if (frames.Count == 0)
        {
            throw CreateInvalidState("There is no open container to close.");
        }

        JsonPathFrame frame = frames[frames.Count - 1];
        if (frame.Kind != kind)
        {
            throw CreateInvalidState("The closing token does not match the current container.");
        }

        JsonPointer pointer = CreateCurrentContainerPointer();
        frames.RemoveAt(frames.Count - 1);
        while (segments.Count > frame.ParentSegmentCount)
        {
            segments.RemoveAt(segments.Count - 1);
        }

        CompleteCurrentValue();
        return pointer;
    }

    JsonPointer CreateValuePointer()
    {
        if (frames.Count == 0)
        {
            if (rootValueComplete)
            {
                throw CreateInvalidState("The root JSON value is already complete.");
            }

            return CreateCurrentContainerPointer();
        }

        return CreatePointerWithSegment(GetActiveValueSegment());
    }

    string GetActiveValueSegment()
    {
        JsonPathFrame frame = frames[frames.Count - 1];
        if (frame.Kind == JsonContainerKind.Array)
        {
            return FormatArrayIndex(frame.ArrayIndex);
        }

        string? propertyName = frame.PendingPropertyName;
        if (propertyName == null)
        {
            throw CreateInvalidState("An object property name is required before its value.");
        }

        return propertyName;
    }

    void CompleteCurrentValue()
    {
        if (frames.Count == 0)
        {
            if (rootValueComplete)
            {
                throw CreateInvalidState("The root JSON value is already complete.");
            }

            rootValueComplete = true;
            return;
        }

        JsonPathFrame frame = frames[frames.Count - 1];
        if (frame.Kind == JsonContainerKind.Array)
        {
            if (frame.ArrayIndex == int.MaxValue)
            {
                throw CreateInvalidState("The array index cannot be represented by a signed integer.");
            }

            frame.ArrayIndex++;
            return;
        }

        if (frame.PendingPropertyName == null)
        {
            throw CreateInvalidState("An object property name is required before its value.");
        }

        frame.PendingPropertyName = null;
    }

    JsonPointer CreateCurrentContainerPointer()
    {
        return BuildPointer(false, null);
    }

    JsonPointer CreatePointerWithSegment(string segment)
    {
        return BuildPointer(true, segment);
    }

    JsonPointer BuildPointer(bool includeExtraSegment, string? extraSegment)
    {
        global::System.Text.StringBuilder representation = new global::System.Text.StringBuilder();
        global::System.Collections.Generic.List<string> referenceTokens =
            new global::System.Collections.Generic.List<string>();

        int index = 0;
        while (index < segments.Count)
        {
            string segment = segments[index];
            representation.Append('/');
            AppendEscapedToken(representation, segment);
            referenceTokens.Add(segment);
            index++;
        }

        if (includeExtraSegment)
        {
            if (extraSegment == null)
            {
                throw CreateInvalidState("A pointer segment cannot be null.");
            }

            representation.Append('/');
            AppendEscapedToken(representation, extraSegment);
            referenceTokens.Add(extraSegment);
        }

        JsonPointer pointer = new JsonPointer();
        pointer.Encoding = JsonPointerEncoding.String;
        pointer.OriginalRepresentation = representation.ToString();
        pointer.ReferenceTokens = referenceTokens;
        return pointer;
    }

    static string FormatArrayIndex(int index)
    {
        return index.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
    }

    static void AppendEscapedToken(global::System.Text.StringBuilder builder, string token)
    {
        int index = 0;
        while (index < token.Length)
        {
            char value = token[index];
            if (value == '~')
            {
                builder.Append("~0");
            }
            else if (value == '/')
            {
                builder.Append("~1");
            }
            else
            {
                builder.Append(value);
            }

            index++;
        }
    }

    static global::System.InvalidOperationException CreateInvalidState(string message)
    {
        return new global::System.InvalidOperationException(message);
    }
}
