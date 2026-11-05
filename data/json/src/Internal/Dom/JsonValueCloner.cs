namespace json;

internal class JsonValueCloneFrame
{
    internal JsonValue Source;
    internal JsonValue Target;
    internal int NextIndex;

    internal JsonValueCloneFrame(JsonValue source, JsonValue target)
    {
        Source = source;
        Target = target;
    }
}

internal static class JsonValueCloner
{
    internal static JsonValue CloneValueGraph(JsonValue source)
    {
        if (source == null)
        {
            throw new global::System.ArgumentNullException("source");
        }

        global::System.Collections.Generic.Dictionary<JsonValue, JsonValue> clones =
            new global::System.Collections.Generic.Dictionary<JsonValue, JsonValue>();
        JsonValue root = CreateValueShell(source);
        clones.Add(source, root);
        global::System.Collections.Generic.List<JsonValueCloneFrame> frames =
            new global::System.Collections.Generic.List<JsonValueCloneFrame>();
        if (IsContainer(source))
        {
            frames.Add(new JsonValueCloneFrame(source, root));
        }

        while (frames.Count > 0)
        {
            JsonValueCloneFrame frame = frames[frames.Count - 1];
            if (frame.Source.Kind == JsonValueKind.Array)
            {
                CloneNextArrayItem(frame, frames, clones);
                continue;
            }

            if (frame.Source.Kind == JsonValueKind.Object)
            {
                CloneNextObjectMember(frame, frames, clones);
                continue;
            }

            throw new global::System.InvalidOperationException("A clone frame requires a container value.");
        }

        return root;
    }

    static void CloneNextArrayItem(
        JsonValueCloneFrame frame,
        global::System.Collections.Generic.List<JsonValueCloneFrame> frames,
        global::System.Collections.Generic.Dictionary<JsonValue, JsonValue> clones)
    {
        global::System.Collections.Generic.List<JsonValue> sourceItems = frame.Source.ArrayItems
            ?? throw new global::System.InvalidOperationException("An array value requires an item list.");
        global::System.Collections.Generic.List<JsonValue> targetItems = frame.Target.ArrayItems
            ?? throw new global::System.InvalidOperationException("A cloned array requires an item list.");
        if (frame.NextIndex >= sourceItems.Count)
        {
            frames.RemoveAt(frames.Count - 1);
            return;
        }

        JsonValue sourceItem = sourceItems[frame.NextIndex]
            ?? throw new global::System.InvalidOperationException("An array item cannot be null.");
        frame.NextIndex++;
        bool created = !clones.ContainsKey(sourceItem);
        JsonValue targetItem;
        if (created)
        {
            targetItem = CreateValueShell(sourceItem);
            clones.Add(sourceItem, targetItem);
        }
        else
        {
            targetItem = clones[sourceItem];
        }

        targetItems.Add(targetItem);
        if (created && IsContainer(sourceItem))
        {
            frames.Add(new JsonValueCloneFrame(sourceItem, targetItem));
        }
    }

    static void CloneNextObjectMember(
        JsonValueCloneFrame frame,
        global::System.Collections.Generic.List<JsonValueCloneFrame> frames,
        global::System.Collections.Generic.Dictionary<JsonValue, JsonValue> clones)
    {
        global::System.Collections.Generic.List<JsonMember> sourceMembers = frame.Source.ObjectMembers
            ?? throw new global::System.InvalidOperationException("An object value requires a member list.");
        global::System.Collections.Generic.List<JsonMember> targetMembers = frame.Target.ObjectMembers
            ?? throw new global::System.InvalidOperationException("A cloned object requires a member list.");
        if (frame.NextIndex >= sourceMembers.Count)
        {
            frames.RemoveAt(frames.Count - 1);
            return;
        }

        JsonMember sourceMember = sourceMembers[frame.NextIndex]
            ?? throw new global::System.InvalidOperationException("An object member cannot be null.");
        JsonValue sourceValue = sourceMember.Value
            ?? throw new global::System.InvalidOperationException("An object member value cannot be null.");
        frame.NextIndex++;

        JsonMember targetMember = new JsonMember();
        targetMember.Name = sourceMember.Name;
        targetMember.NameRange = CloneRange(sourceMember.NameRange);
        targetMember.MemberRange = CloneRange(sourceMember.MemberRange);
        targetMember.SourceOrdinal = sourceMember.SourceOrdinal;
        bool created = !clones.ContainsKey(sourceValue);
        JsonValue targetValue;
        if (created)
        {
            targetValue = CreateValueShell(sourceValue);
            clones.Add(sourceValue, targetValue);
        }
        else
        {
            targetValue = clones[sourceValue];
        }

        targetMember.Value = targetValue;
        targetMembers.Add(targetMember);
        if (created && IsContainer(sourceValue))
        {
            frames.Add(new JsonValueCloneFrame(sourceValue, targetValue));
        }
    }

    static JsonValue CreateValueShell(JsonValue source)
    {
        JsonValue target = new JsonValue();
        target.Kind = source.Kind;
        target.Range = CloneRange(source.Range);
        switch (source.Kind)
        {
            case JsonValueKind.Uninitialized:
                break;
            case JsonValueKind.Null:
                break;
            case JsonValueKind.Boolean:
                target.BooleanValue = source.BooleanValue;
                break;
            case JsonValueKind.Number:
                target.Number = CloneNumber(source.Number);
                break;
            case JsonValueKind.String:
                target.StringValue = source.StringValue;
                break;
            case JsonValueKind.Array:
                if (source.ArrayItems == null)
                {
                    throw new global::System.InvalidOperationException("An array value requires an item list.");
                }

                target.ArrayItems = new global::System.Collections.Generic.List<JsonValue>();
                break;
            case JsonValueKind.Object:
                if (source.ObjectMembers == null)
                {
                    throw new global::System.InvalidOperationException("An object value requires a member list.");
                }

                target.ObjectMembers = new global::System.Collections.Generic.List<JsonMember>();
                break;
            default:
                throw new global::System.InvalidOperationException("The JSON value kind is undefined.");
        }

        return target;
    }

    static JsonNumber CloneNumber(JsonNumber? source)
    {
        if (source == null)
        {
            throw new global::System.InvalidOperationException("A number value requires a number payload.");
        }

        JsonNumber target = new JsonNumber();
        target.Lexeme = source.Lexeme;
        target.Kind = source.Kind;
        target.IsNegative = source.IsNegative;
        target.HasFraction = source.HasFraction;
        target.HasExponent = source.HasExponent;
        target.IsNegativeZero = source.IsNegativeZero;
        return target;
    }

    static JsonSourceRange? CloneRange(JsonSourceRange? source)
    {
        if (source == null)
        {
            return null;
        }

        JsonSourceRange target = new JsonSourceRange();
        target.Start = ClonePosition(source.Start);
        target.End = ClonePosition(source.End);
        return target;
    }

    static JsonSourcePosition? ClonePosition(JsonSourcePosition? source)
    {
        if (source == null)
        {
            return null;
        }

        JsonSourcePosition target = new JsonSourcePosition();
        target.ByteOffset = source.ByteOffset;
        target.Line = source.Line;
        target.ByteColumn = source.ByteColumn;
        target.ScalarColumn = source.ScalarColumn;
        return target;
    }

    static bool IsContainer(JsonValue value)
    {
        return value.Kind == JsonValueKind.Array || value.Kind == JsonValueKind.Object;
    }
}
