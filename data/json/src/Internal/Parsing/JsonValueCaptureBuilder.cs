namespace json;

internal class JsonValueCaptureFrame
{
    internal JsonValue Container;
    internal JsonMember? ParentMember;
    internal JsonMember? PendingMember;
    internal JsonDuplicateDecision? PendingDecision;
    internal int NextSourceOrdinal;

    internal JsonValueCaptureFrame(JsonValue container, JsonMember? parentMember)
    {
        Container = container;
        ParentMember = parentMember;
    }
}

internal class JsonValueCaptureBuilder
{
    bool captureSourceRanges;
    int maximumDomNodes;
    int nodeCount;
    global::System.Collections.Generic.List<JsonValueCaptureFrame> frames;
    JsonValue? capturedValue;
    bool started;

    internal JsonValueCaptureBuilder(bool captureSourceRanges, int maximumDomNodes)
    {
        this.captureSourceRanges = captureSourceRanges;
        this.maximumDomNodes = maximumDomNodes;
        frames = new global::System.Collections.Generic.List<JsonValueCaptureFrame>();
    }

    internal bool AcceptCaptureToken(JsonToken token, JsonDuplicateDecision? duplicateDecision)
    {
        if (!started)
        {
            if (IsScalarToken(token.Kind))
            {
                capturedValue = CreateScalarValue(token);
                started = true;
                return true;
            }

            if (IsContainerStart(token.Kind))
            {
                JsonValue root = CreateContainerValue(token);
                capturedValue = root;
                frames.Add(new JsonValueCaptureFrame(root, null));
                started = true;
                return false;
            }

            throw new global::System.InvalidOperationException("The current token is not the start of a JSON value.");
        }

        if (frames.Count == 0)
        {
            throw new global::System.InvalidOperationException("The captured value is already complete.");
        }

        JsonValueCaptureFrame frame = frames[frames.Count - 1];
        if (token.Kind == JsonTokenKind.PropertyName)
        {
            AcceptPropertyName(frame, token, duplicateDecision);
            return false;
        }

        if (IsScalarToken(token.Kind))
        {
            JsonValue scalar = CreateScalarValue(token);
            JsonMember? member = AttachValue(frame, scalar);
            CompleteMemberRange(member, scalar.Range);
            return false;
        }

        if (IsContainerStart(token.Kind))
        {
            JsonValue container = CreateContainerValue(token);
            JsonMember? member = AttachValue(frame, container);
            frames.Add(new JsonValueCaptureFrame(container, member));
            return false;
        }

        if (IsContainerEnd(token.Kind))
        {
            CompleteContainer(frame, token);
            frames.RemoveAt(frames.Count - 1);
            return frames.Count == 0;
        }

        throw new global::System.InvalidOperationException("The token cannot participate in a captured value.");
    }

    internal JsonValue GetCapturedValue()
    {
        if (!started || capturedValue == null || frames.Count != 0)
        {
            throw new global::System.InvalidOperationException("The captured value is not complete.");
        }

        return capturedValue;
    }

    internal void ResetCaptureBuilder()
    {
        frames.Clear();
        capturedValue = null;
        nodeCount = 0;
        started = false;
    }

    void AcceptPropertyName(
        JsonValueCaptureFrame frame,
        JsonToken token,
        JsonDuplicateDecision? duplicateDecision)
    {
        if (frame.Container.Kind != JsonValueKind.Object || frame.PendingMember != null)
        {
            throw new global::System.InvalidOperationException("A property name requires an object awaiting a member.");
        }

        if (duplicateDecision == null)
        {
            throw new global::System.InvalidOperationException("A property name requires a duplicate decision.");
        }

        EnsureNodeCapacity(token);
        JsonMember member = new JsonMember();
        member.Name = token.StringValue;
        member.SourceOrdinal = frame.NextSourceOrdinal;
        if (captureSourceRanges)
        {
            member.NameRange = CloneRange(token.Range);
        }

        frame.NextSourceOrdinal++;
        frame.PendingMember = member;
        frame.PendingDecision = duplicateDecision;
    }

    JsonMember? AttachValue(JsonValueCaptureFrame frame, JsonValue value)
    {
        if (frame.Container.Kind == JsonValueKind.Array)
        {
            global::System.Collections.Generic.List<JsonValue> items = frame.Container.ArrayItems
                ?? throw new global::System.InvalidOperationException("An array requires an item list.");
            items.Add(value);
            return null;
        }

        if (frame.Container.Kind != JsonValueKind.Object ||
            frame.PendingMember == null ||
            frame.PendingDecision == null)
        {
            throw new global::System.InvalidOperationException("An object value requires a pending member.");
        }

        JsonMember member = frame.PendingMember;
        JsonDuplicateDecision decision = frame.PendingDecision;
        member.Value = value;
        global::System.Collections.Generic.List<JsonMember> members = frame.Container.ObjectMembers
            ?? throw new global::System.InvalidOperationException("An object requires a member list.");
        if (decision.Kind == JsonDuplicateDecisionKind.Append)
        {
            members.Add(member);
        }
        else if (decision.Kind == JsonDuplicateDecisionKind.Replace)
        {
            members[decision.MemberIndex] = member;
        }
        else if (decision.Kind != JsonDuplicateDecisionKind.Skip)
        {
            throw new global::System.InvalidOperationException("The duplicate decision is invalid.");
        }

        frame.PendingMember = null;
        frame.PendingDecision = null;
        return member;
    }

    JsonValue CreateScalarValue(JsonToken token)
    {
        EnsureNodeCapacity(token);
        JsonValue value = new JsonValue();
        if (token.Kind == JsonTokenKind.String)
        {
            value.Kind = JsonValueKind.String;
            value.StringValue = token.StringValue;
        }
        else if (token.Kind == JsonTokenKind.Number)
        {
            value.Kind = JsonValueKind.Number;
            value.Number = CloneNumber(token.Number);
        }
        else if (token.Kind == JsonTokenKind.Boolean)
        {
            value.Kind = JsonValueKind.Boolean;
            value.BooleanValue = token.BooleanValue;
        }
        else if (token.Kind == JsonTokenKind.Null)
        {
            value.Kind = JsonValueKind.Null;
        }
        else
        {
            throw new global::System.InvalidOperationException("The token is not a scalar value.");
        }

        if (captureSourceRanges)
        {
            value.Range = CloneRange(token.Range);
        }

        return value;
    }

    JsonValue CreateContainerValue(JsonToken token)
    {
        EnsureNodeCapacity(token);
        JsonValue value = new JsonValue();
        if (token.Kind == JsonTokenKind.StartArray)
        {
            value.Kind = JsonValueKind.Array;
            value.ArrayItems = new global::System.Collections.Generic.List<JsonValue>();
        }
        else if (token.Kind == JsonTokenKind.StartObject)
        {
            value.Kind = JsonValueKind.Object;
            value.ObjectMembers = new global::System.Collections.Generic.List<JsonMember>();
        }
        else
        {
            throw new global::System.InvalidOperationException("The token is not a container start.");
        }

        if (captureSourceRanges)
        {
            value.Range = CloneRange(token.Range);
        }

        return value;
    }

    void CompleteContainer(JsonValueCaptureFrame frame, JsonToken token)
    {
        if (frame.Container.Kind == JsonValueKind.Array && token.Kind != JsonTokenKind.EndArray)
        {
            throw new global::System.InvalidOperationException("An array capture requires an array end token.");
        }

        if (frame.Container.Kind == JsonValueKind.Object && token.Kind != JsonTokenKind.EndObject)
        {
            throw new global::System.InvalidOperationException("An object capture requires an object end token.");
        }

        if (frame.PendingMember != null)
        {
            throw new global::System.InvalidOperationException("An object member is missing its value.");
        }

        if (captureSourceRanges)
        {
            JsonSourceRange range = frame.Container.Range
                ?? throw new global::System.InvalidOperationException("A captured container requires a range.");
            range.End = ClonePosition(token.Range?.End);
            CompleteMemberRange(frame.ParentMember, range);
        }
    }

    void CompleteMemberRange(JsonMember? member, JsonSourceRange? valueRange)
    {
        if (!captureSourceRanges || member == null)
        {
            return;
        }

        JsonSourceRange range = new JsonSourceRange();
        range.Start = ClonePosition(member.NameRange?.Start);
        range.End = ClonePosition(valueRange?.End);
        member.MemberRange = range;
    }

    void EnsureNodeCapacity(JsonToken token)
    {
        if (nodeCount >= maximumDomNodes)
        {
            JsonDiagnostic diagnostic = new JsonDiagnostic();
            diagnostic.Code = JsonDiagnosticCode.DomNodeLimitExceeded;
            diagnostic.Message = "The configured DOM node limit was exceeded while materializing a JSON value.";
            diagnostic.Depth = token.Depth;
            diagnostic.Pointer = token.Pointer?.OriginalRepresentation;
            diagnostic.ActualToken = token.Kind.ToString();
            diagnostic.Range = CloneRange(token.Range);
            throw new JsonLimitException(diagnostic);
        }

        nodeCount++;
    }

    JsonNumber CloneNumber(JsonNumber? source)
    {
        if (source == null)
        {
            throw new global::System.InvalidOperationException("A number token requires a number payload.");
        }

        JsonNumber number = new JsonNumber();
        number.Lexeme = source.Lexeme;
        number.Kind = source.Kind;
        number.IsNegative = source.IsNegative;
        number.HasFraction = source.HasFraction;
        number.HasExponent = source.HasExponent;
        number.IsNegativeZero = source.IsNegativeZero;
        return number;
    }

    JsonSourceRange? CloneRange(JsonSourceRange? source)
    {
        if (source == null)
        {
            return null;
        }

        JsonSourceRange range = new JsonSourceRange();
        range.Start = ClonePosition(source.Start);
        range.End = ClonePosition(source.End);
        return range;
    }

    JsonSourcePosition? ClonePosition(JsonSourcePosition? source)
    {
        if (source == null)
        {
            return null;
        }

        JsonSourcePosition position = new JsonSourcePosition();
        position.ByteOffset = source.ByteOffset;
        position.Line = source.Line;
        position.ByteColumn = source.ByteColumn;
        position.ScalarColumn = source.ScalarColumn;
        return position;
    }

    bool IsScalarToken(JsonTokenKind kind)
    {
        return kind == JsonTokenKind.String ||
            kind == JsonTokenKind.Number ||
            kind == JsonTokenKind.Boolean ||
            kind == JsonTokenKind.Null;
    }

    bool IsContainerStart(JsonTokenKind kind)
    {
        return kind == JsonTokenKind.StartArray || kind == JsonTokenKind.StartObject;
    }

    bool IsContainerEnd(JsonTokenKind kind)
    {
        return kind == JsonTokenKind.EndArray || kind == JsonTokenKind.EndObject;
    }
}
