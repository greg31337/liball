namespace json;

internal class JsonGraphFrame
{
    internal JsonValue Value;
    internal JsonPointer Pointer;
    internal int Depth;
    internal int NextChildIndex;

    internal JsonGraphFrame(JsonValue value, JsonPointer pointer, int depth)
    {
        Value = value;
        Pointer = pointer;
        Depth = depth;
    }
}

internal static class JsonGraphWalker
{
    internal static JsonVisitAction TraverseGraph(
        JsonValue root,
        IJsonValueVisitor visitor)
    {
        if (root == null)
        {
            throw new global::System.ArgumentNullException("root");
        }

        if (visitor == null)
        {
            throw new global::System.ArgumentNullException("visitor");
        }

        JsonPointer rootPointer = CreateRootPointer();
        JsonDiagnostic? rootFailure = ValidateValueNode(root, rootPointer, 0);
        if (rootFailure != null)
        {
            throw new global::System.InvalidOperationException(rootFailure.Message);
        }

        JsonVisit rootVisit = CreateTraversalVisit(
            root,
            null,
            null,
            -1,
            rootPointer,
            0);
        JsonVisitAction lastAction = visitor.VisitValue(rootVisit);
        ValidateVisitAction(lastAction);
        if (lastAction == JsonVisitAction.Stop ||
            lastAction == JsonVisitAction.SkipChildren ||
            !IsContainer(root))
        {
            return lastAction;
        }

        global::System.Collections.Generic.List<JsonGraphFrame> frames =
            new global::System.Collections.Generic.List<JsonGraphFrame>();
        global::System.Collections.Generic.HashSet<JsonValue> activeContainers =
            new global::System.Collections.Generic.HashSet<JsonValue>();
        frames.Add(new JsonGraphFrame(root, rootPointer, 0));
        activeContainers.Add(root);

        while (frames.Count > 0)
        {
            JsonGraphFrame frame = frames[frames.Count - 1];
            JsonValue child;
            string? memberName = null;
            int arrayIndex = -1;
            string pointerSegment;
            if (frame.Value.Kind == JsonValueKind.Array)
            {
                global::System.Collections.Generic.List<JsonValue> items =
                    frame.Value.ArrayItems
                    ?? throw new global::System.InvalidOperationException(
                        "An array value requires an item list.");
                if (frame.NextChildIndex >= items.Count)
                {
                    activeContainers.Remove(frame.Value);
                    frames.RemoveAt(frames.Count - 1);
                    continue;
                }

                arrayIndex = frame.NextChildIndex;
                child = items[arrayIndex]
                    ?? throw new global::System.InvalidOperationException(
                        "An array item cannot be null.");
                frame.NextChildIndex++;
                pointerSegment = arrayIndex.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                global::System.Collections.Generic.List<JsonMember> members =
                    frame.Value.ObjectMembers
                    ?? throw new global::System.InvalidOperationException(
                        "An object value requires a member list.");
                if (frame.NextChildIndex >= members.Count)
                {
                    activeContainers.Remove(frame.Value);
                    frames.RemoveAt(frames.Count - 1);
                    continue;
                }

                JsonMember member = members[frame.NextChildIndex]
                    ?? throw new global::System.InvalidOperationException(
                        "An object member cannot be null.");
                frame.NextChildIndex++;
                JsonDiagnostic? memberFailure = ValidateObjectMember(
                    member,
                    frame.Pointer,
                    frame.Depth + 1);
                if (memberFailure != null)
                {
                    throw new global::System.InvalidOperationException(
                        memberFailure.Message);
                }

                memberName = member.Name
                    ?? throw new global::System.InvalidOperationException(
                        "An object member name is required.");
                child = member.Value
                    ?? throw new global::System.InvalidOperationException(
                        "An object member value is required.");
                pointerSegment = memberName;
            }

            JsonPointer childPointer = CreateChildPointer(
                frame.Pointer,
                pointerSegment);
            int childDepth = frame.Depth + 1;
            JsonDiagnostic? childFailure = ValidateValueNode(
                child,
                childPointer,
                childDepth);
            if (childFailure != null)
            {
                throw new global::System.InvalidOperationException(
                    childFailure.Message);
            }

            if (IsContainer(child) && activeContainers.Contains(child))
            {
                throw new global::System.InvalidOperationException(
                    "The JSON model contains a reference cycle.");
            }

            JsonVisit visit = CreateTraversalVisit(
                child,
                frame.Value,
                memberName,
                arrayIndex,
                childPointer,
                childDepth);
            lastAction = visitor.VisitValue(visit);
            ValidateVisitAction(lastAction);
            if (lastAction == JsonVisitAction.Stop)
            {
                return lastAction;
            }

            if (lastAction == JsonVisitAction.Continue && IsContainer(child))
            {
                activeContainers.Add(child);
                frames.Add(new JsonGraphFrame(child, childPointer, childDepth));
            }
        }

        return lastAction;
    }

    internal static JsonOperationResult PublishGraphTokens(
        JsonValue root,
        IJsonTokenHandler handler)
    {
        if (root == null)
        {
            return CreatePublicationFailure(
                JsonDiagnosticCode.InvalidModel,
                "A JSON root value is required.",
                null,
                0,
                "root");
        }

        if (handler == null)
        {
            return CreatePublicationFailure(
                JsonDiagnosticCode.InvalidOptions,
                "A token handler is required.",
                null,
                0,
                "handler");
        }

        JsonPointer rootPointer = CreateRootPointer();
        JsonDiagnostic? rootFailure = ValidateValueNode(root, rootPointer, 0);
        if (rootFailure != null)
        {
            return CreatePublicationFailureFromDiagnostic(rootFailure);
        }

        JsonToken rootToken = CreateValueToken(root, rootPointer, 0);
        JsonOperationResult? rootTermination = GetPublicationTermination(
            handler.HandleToken(rootToken));
        if (rootTermination != null)
        {
            return rootTermination;
        }

        if (!IsContainer(root))
        {
            return CreatePublicationSuccess();
        }

        global::System.Collections.Generic.List<JsonGraphFrame> frames =
            new global::System.Collections.Generic.List<JsonGraphFrame>();
        global::System.Collections.Generic.HashSet<JsonValue> activeContainers =
            new global::System.Collections.Generic.HashSet<JsonValue>();
        frames.Add(new JsonGraphFrame(root, rootPointer, 0));
        activeContainers.Add(root);

        while (frames.Count > 0)
        {
            JsonGraphFrame frame = frames[frames.Count - 1];
            JsonValue? childCandidate;
            string pointerSegment;
            JsonSourceRange? propertyRange = null;
            string? propertyName = null;
            if (frame.Value.Kind == JsonValueKind.Array)
            {
                global::System.Collections.Generic.List<JsonValue> items =
                    frame.Value.ArrayItems
                    ?? throw new global::System.InvalidOperationException(
                        "An array value requires an item list.");
                if (frame.NextChildIndex >= items.Count)
                {
                    JsonToken endToken = CreateContainerEndToken(
                        frame.Value,
                        frame.Pointer,
                        frame.Depth);
                    activeContainers.Remove(frame.Value);
                    frames.RemoveAt(frames.Count - 1);
                    JsonOperationResult? endTermination = GetPublicationTermination(
                        handler.HandleToken(endToken));
                    if (endTermination != null)
                    {
                        return endTermination;
                    }

                    continue;
                }

                int arrayIndex = frame.NextChildIndex;
                childCandidate = items[arrayIndex];
                frame.NextChildIndex++;
                pointerSegment = arrayIndex.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                global::System.Collections.Generic.List<JsonMember> members =
                    frame.Value.ObjectMembers
                    ?? throw new global::System.InvalidOperationException(
                        "An object value requires a member list.");
                if (frame.NextChildIndex >= members.Count)
                {
                    JsonToken endToken = CreateContainerEndToken(
                        frame.Value,
                        frame.Pointer,
                        frame.Depth);
                    activeContainers.Remove(frame.Value);
                    frames.RemoveAt(frames.Count - 1);
                    JsonOperationResult? endTermination = GetPublicationTermination(
                        handler.HandleToken(endToken));
                    if (endTermination != null)
                    {
                        return endTermination;
                    }

                    continue;
                }

                JsonMember? member = members[frame.NextChildIndex];
                frame.NextChildIndex++;
                JsonDiagnostic? memberFailure = ValidateObjectMember(
                    member,
                    frame.Pointer,
                    frame.Depth + 1);
                if (memberFailure != null)
                {
                    return CreatePublicationFailureFromDiagnostic(memberFailure);
                }

                propertyName = member?.Name
                    ?? throw new global::System.InvalidOperationException(
                        "A validated object member requires a name.");
                childCandidate = member.Value;
                propertyRange = member.NameRange;
                pointerSegment = propertyName;
            }

            JsonPointer childPointer = CreateChildPointer(
                frame.Pointer,
                pointerSegment);
            int childDepth = frame.Depth + 1;
            if (childCandidate == null)
            {
                return CreatePublicationFailure(
                    JsonDiagnosticCode.InvalidModel,
                    "An array item cannot be null.",
                    childPointer,
                    childDepth,
                    "null reference");
            }

            JsonValue child = childCandidate;
            if (propertyName != null)
            {
                JsonToken propertyToken = CreatePropertyNameToken(
                    propertyName,
                    childPointer,
                    childDepth,
                    propertyRange);
                JsonOperationResult? propertyTermination = GetPublicationTermination(
                    handler.HandleToken(propertyToken));
                if (propertyTermination != null)
                {
                    return propertyTermination;
                }
            }

            JsonDiagnostic? childFailure = ValidateValueNode(
                child,
                childPointer,
                childDepth);
            if (childFailure != null)
            {
                return CreatePublicationFailureFromDiagnostic(childFailure);
            }

            if (IsContainer(child) && activeContainers.Contains(child))
            {
                return CreatePublicationFailure(
                    JsonDiagnosticCode.ModelCycle,
                    "The JSON model contains a reference cycle.",
                    childPointer,
                    childDepth,
                    child.Kind.ToString());
            }

            JsonToken childToken = CreateValueToken(
                child,
                childPointer,
                childDepth);
            JsonOperationResult? childTermination = GetPublicationTermination(
                handler.HandleToken(childToken));
            if (childTermination != null)
            {
                return childTermination;
            }

            if (IsContainer(child))
            {
                activeContainers.Add(child);
                frames.Add(new JsonGraphFrame(child, childPointer, childDepth));
            }
        }

        return CreatePublicationSuccess();
    }

    static JsonVisit CreateTraversalVisit(
        JsonValue value,
        JsonValue? parent,
        string? memberName,
        int arrayIndex,
        JsonPointer pointer,
        int depth)
    {
        JsonVisit visit = new JsonVisit();
        visit.Value = value;
        visit.Parent = parent;
        visit.MemberName = memberName;
        visit.ArrayIndex = arrayIndex;
        visit.Pointer = CopyPointer(pointer);
        visit.Depth = depth;
        visit.Range = value.Range;
        return visit;
    }

    static void ValidateVisitAction(JsonVisitAction action)
    {
        if (action != JsonVisitAction.Continue &&
            action != JsonVisitAction.SkipChildren &&
            action != JsonVisitAction.Stop)
        {
            throw new global::System.InvalidOperationException(
                "The value visitor returned an undefined action.");
        }
    }

    static JsonDiagnostic? ValidateValueNode(
        JsonValue value,
        JsonPointer pointer,
        int depth)
    {
        if (value.Kind == JsonValueKind.Uninitialized ||
            (value.Kind != JsonValueKind.Null &&
                value.Kind != JsonValueKind.Boolean &&
                value.Kind != JsonValueKind.Number &&
                value.Kind != JsonValueKind.String &&
                value.Kind != JsonValueKind.Array &&
                value.Kind != JsonValueKind.Object))
        {
            return CreateGraphDiagnostic(
                JsonDiagnosticCode.InvalidModel,
                "The JSON value kind is uninitialized or undefined.",
                pointer,
                depth,
                value.Kind.ToString());
        }

        if (value.Kind == JsonValueKind.Number &&
            (value.Number == null || value.Number.Lexeme == null))
        {
            return CreateGraphDiagnostic(
                JsonDiagnosticCode.InvalidModel,
                "A number value requires an exact number payload.",
                pointer,
                depth,
                "Number");
        }

        if (value.Kind == JsonValueKind.String &&
            (value.StringValue == null || !IsValidUnicodeText(value.StringValue)))
        {
            return CreateGraphDiagnostic(
                JsonDiagnosticCode.InvalidModel,
                "A string value requires valid Unicode text.",
                pointer,
                depth,
                "String");
        }

        if (value.Kind == JsonValueKind.Array && value.ArrayItems == null)
        {
            return CreateGraphDiagnostic(
                JsonDiagnosticCode.InvalidModel,
                "An array value requires an item list.",
                pointer,
                depth,
                "Array");
        }

        if (value.Kind == JsonValueKind.Object && value.ObjectMembers == null)
        {
            return CreateGraphDiagnostic(
                JsonDiagnosticCode.InvalidModel,
                "An object value requires a member list.",
                pointer,
                depth,
                "Object");
        }

        bool numberAllowed = value.Kind == JsonValueKind.Number;
        bool stringAllowed = value.Kind == JsonValueKind.String;
        bool arrayAllowed = value.Kind == JsonValueKind.Array;
        bool objectAllowed = value.Kind == JsonValueKind.Object;
        bool booleanAllowed = value.Kind == JsonValueKind.Boolean;
        if ((!numberAllowed && value.Number != null) ||
            (!stringAllowed && value.StringValue != null) ||
            (!arrayAllowed && value.ArrayItems != null) ||
            (!objectAllowed && value.ObjectMembers != null) ||
            (!booleanAllowed && value.BooleanValue))
        {
            return CreateGraphDiagnostic(
                JsonDiagnosticCode.InvalidModel,
                "A JSON value contains a payload that does not match its kind.",
                pointer,
                depth,
                value.Kind.ToString());
        }

        return null;
    }

    static JsonDiagnostic? ValidateObjectMember(
        JsonMember? member,
        JsonPointer parentPointer,
        int depth)
    {
        if (member == null)
        {
            return CreateGraphDiagnostic(
                JsonDiagnosticCode.InvalidModel,
                "An object member cannot be null.",
                parentPointer,
                depth,
                "null reference");
        }

        if (member.Name == null || !IsValidUnicodeText(member.Name))
        {
            return CreateGraphDiagnostic(
                JsonDiagnosticCode.InvalidModel,
                "An object member requires a valid Unicode name.",
                parentPointer,
                depth,
                "member name");
        }

        if (member.Value == null)
        {
            return CreateGraphDiagnostic(
                JsonDiagnosticCode.InvalidModel,
                "An object member requires a value.",
                CreateChildPointer(parentPointer, member.Name),
                depth,
                "null reference");
        }

        if (member.SourceOrdinal < -1)
        {
            return CreateGraphDiagnostic(
                JsonDiagnosticCode.InvalidModel,
                "An object member source ordinal cannot be less than -1.",
                CreateChildPointer(parentPointer, member.Name),
                depth,
                member.SourceOrdinal.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }

        return null;
    }

    static JsonPointer CreateRootPointer()
    {
        JsonPointer pointer = new JsonPointer();
        pointer.Encoding = JsonPointerEncoding.String;
        pointer.OriginalRepresentation = "";
        pointer.ReferenceTokens =
            new global::System.Collections.Generic.List<string>();
        return pointer;
    }

    static JsonPointer CreateChildPointer(JsonPointer parent, string segment)
    {
        global::System.Collections.Generic.List<string> parentTokens =
            parent.ReferenceTokens
            ?? throw new global::System.InvalidOperationException(
                "A traversal pointer requires reference tokens.");
        global::System.Collections.Generic.List<string> childTokens =
            new global::System.Collections.Generic.List<string>(parentTokens);
        childTokens.Add(segment);
        JsonPointer pointer = new JsonPointer();
        pointer.Encoding = JsonPointerEncoding.String;
        pointer.OriginalRepresentation = BuildPointerRepresentation(childTokens);
        pointer.ReferenceTokens = childTokens;
        return pointer;
    }

    static JsonPointer CopyPointer(JsonPointer source)
    {
        global::System.Collections.Generic.List<string> tokens =
            source.ReferenceTokens
            ?? throw new global::System.InvalidOperationException(
                "A traversal pointer requires reference tokens.");
        JsonPointer copy = new JsonPointer();
        copy.Encoding = source.Encoding;
        copy.OriginalRepresentation = source.OriginalRepresentation;
        copy.ReferenceTokens =
            new global::System.Collections.Generic.List<string>(tokens);
        return copy;
    }

    static string BuildPointerRepresentation(
        global::System.Collections.Generic.List<string> tokens)
    {
        global::System.Text.StringBuilder builder =
            new global::System.Text.StringBuilder();
        foreach (string token in tokens)
        {
            builder.Append('/');
            AppendPointerToken(builder, token);
        }

        return builder.ToString();
    }

    static void AppendPointerToken(
        global::System.Text.StringBuilder builder,
        string token)
    {
        int index = 0;
        while (index < token.Length)
        {
            char character = token[index];
            if (character == '~')
            {
                builder.Append("~0");
            }
            else if (character == '/')
            {
                builder.Append("~1");
            }
            else
            {
                builder.Append(character);
            }

            index++;
        }
    }

    static bool IsContainer(JsonValue value)
    {
        return value.Kind == JsonValueKind.Array ||
            value.Kind == JsonValueKind.Object;
    }

    static JsonToken CreateValueToken(
        JsonValue value,
        JsonPointer pointer,
        int depth)
    {
        JsonToken token = new JsonToken();
        token.Depth = depth;
        token.Pointer = CopyPointer(pointer);
        token.Range = value.Range;
        if (value.Kind == JsonValueKind.Null)
        {
            token.Kind = JsonTokenKind.Null;
        }
        else if (value.Kind == JsonValueKind.Boolean)
        {
            token.Kind = JsonTokenKind.Boolean;
            token.BooleanValue = value.BooleanValue;
        }
        else if (value.Kind == JsonValueKind.Number)
        {
            token.Kind = JsonTokenKind.Number;
            token.Number = value.Number;
        }
        else if (value.Kind == JsonValueKind.String)
        {
            token.Kind = JsonTokenKind.String;
            token.StringValue = value.StringValue;
        }
        else if (value.Kind == JsonValueKind.Array)
        {
            token.Kind = JsonTokenKind.StartArray;
        }
        else if (value.Kind == JsonValueKind.Object)
        {
            token.Kind = JsonTokenKind.StartObject;
        }
        else
        {
            throw new global::System.InvalidOperationException(
                "A validated JSON value kind is required.");
        }

        return token;
    }

    static JsonToken CreateContainerEndToken(
        JsonValue value,
        JsonPointer pointer,
        int depth)
    {
        JsonToken token = new JsonToken();
        if (value.Kind == JsonValueKind.Array)
        {
            token.Kind = JsonTokenKind.EndArray;
        }
        else if (value.Kind == JsonValueKind.Object)
        {
            token.Kind = JsonTokenKind.EndObject;
        }
        else
        {
            throw new global::System.InvalidOperationException(
                "A container end token requires an array or object.");
        }

        token.Depth = depth;
        token.Pointer = CopyPointer(pointer);
        token.Range = value.Range;
        return token;
    }

    static JsonToken CreatePropertyNameToken(
        string name,
        JsonPointer pointer,
        int depth,
        JsonSourceRange? range)
    {
        JsonToken token = new JsonToken();
        token.Kind = JsonTokenKind.PropertyName;
        token.StringValue = name;
        token.Depth = depth;
        token.Pointer = CopyPointer(pointer);
        token.Range = range;
        return token;
    }

    static JsonOperationResult? GetPublicationTermination(JsonHandlerAction action)
    {
        if (action == JsonHandlerAction.Continue)
        {
            return null;
        }

        if (action == JsonHandlerAction.Stop)
        {
            return CreatePublicationSuccess();
        }

        if (action == JsonHandlerAction.Pause)
        {
            return CreatePublicationFailure(
                JsonDiagnosticCode.InvalidOptions,
                "One-shot token publication cannot pause because it has no resume operation.",
                null,
                0,
                action.ToString());
        }

        return CreatePublicationFailure(
            JsonDiagnosticCode.InvalidOptions,
            "The token handler returned an undefined action.",
            null,
            0,
            action.ToString());
    }

    static JsonOperationResult CreatePublicationSuccess()
    {
        JsonOperationResult result = new JsonOperationResult();
        result.Success = true;
        return result;
    }

    static JsonOperationResult CreatePublicationFailure(
        JsonDiagnosticCode code,
        string message,
        JsonPointer? pointer,
        int depth,
        string actualToken)
    {
        JsonDiagnostic diagnostic = CreateGraphDiagnostic(
            code,
            message,
            pointer,
            depth,
            actualToken);
        return CreatePublicationFailureFromDiagnostic(diagnostic);
    }

    static JsonOperationResult CreatePublicationFailureFromDiagnostic(
        JsonDiagnostic diagnostic)
    {
        JsonOperationResult result = new JsonOperationResult();
        result.Success = false;
        result.Diagnostic = diagnostic;
        return result;
    }

    static JsonDiagnostic CreateGraphDiagnostic(
        JsonDiagnosticCode code,
        string message,
        JsonPointer? pointer,
        int depth,
        string actualToken)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Pointer = pointer?.OriginalRepresentation;
        diagnostic.Depth = depth;
        diagnostic.ActualToken = actualToken;
        return diagnostic;
    }

    static bool IsValidUnicodeText(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            int character = text[index];
            if (character >= 0xD800 && character <= 0xDBFF)
            {
                if (index + 1 >= text.Length)
                {
                    return false;
                }

                int low = text[index + 1];
                if (low < 0xDC00 || low > 0xDFFF)
                {
                    return false;
                }

                index += 2;
                continue;
            }

            if (character >= 0xDC00 && character <= 0xDFFF)
            {
                return false;
            }

            index++;
        }

        return true;
    }
}
