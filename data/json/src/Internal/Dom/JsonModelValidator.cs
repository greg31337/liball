namespace json;

internal class JsonModelValidationFrame
{
    internal JsonValue Value;
    internal int ValueDepth;
    internal int NextIndex;

    internal JsonModelValidationFrame(JsonValue value, int valueDepth)
    {
        Value = value;
        ValueDepth = valueDepth;
    }
}

internal class JsonModelValidator
{
    JsonLimits limits;
    JsonValueFactory numberFactory = new JsonValueFactory();
    JsonDiagnostic? failure;
    int nodeCount;
    bool rejectUnicodeNoncharacters;

    internal JsonModelValidator(JsonLimits? limits)
    {
        this.limits = JsonOptionNormalizer.NormalizeLimits(limits);
    }

    internal JsonOperationResult ValidateValueGraph(JsonValue? value)
    {
        rejectUnicodeNoncharacters = false;
        return ValidateGraph(value);
    }

    internal JsonOperationResult ValidateDocumentModel(JsonDocument? document)
    {
        failure = null;
        nodeCount = 0;
        if (document == null)
        {
            SetFailure(JsonDiagnosticCode.InvalidModel, "A JSON document is required.", 0, "document");
            return CreateResult();
        }

        if (!ValidateDocumentMetadata(document))
        {
            return CreateResult();
        }

        rejectUnicodeNoncharacters =
            document.ConformanceProfile == JsonConformanceProfile.IJson ||
            document.ConformanceProfile == JsonConformanceProfile.Jcs;
        return ValidateGraph(document.Root);
    }

    internal JsonOperationResult ValidateJcsDocumentModel(
        JsonDocument? document)
    {
        failure = null;
        nodeCount = 0;
        if (document == null)
        {
            SetFailure(
                JsonDiagnosticCode.InvalidModel,
                "A JSON document is required.",
                0,
                "document");
            return CreateResult();
        }

        if (!ValidateDocumentMetadata(document))
        {
            return CreateResult();
        }

        rejectUnicodeNoncharacters = true;
        return ValidateGraph(document.Root);
    }

    JsonOperationResult ValidateGraph(JsonValue? root)
    {
        failure = null;
        nodeCount = 0;
        if (root == null)
        {
            SetFailure(JsonDiagnosticCode.InvalidModel, "A JSON root value is required.", 0, "root");
            return CreateResult();
        }

        if (!ValidateValueNode(root, 0))
        {
            return CreateResult();
        }

        global::System.Collections.Generic.List<JsonModelValidationFrame> frames =
            new global::System.Collections.Generic.List<JsonModelValidationFrame>();
        global::System.Collections.Generic.HashSet<JsonValue> activeContainers =
            new global::System.Collections.Generic.HashSet<JsonValue>();
        if (IsContainer(root))
        {
            frames.Add(new JsonModelValidationFrame(root, 0));
            activeContainers.Add(root);
        }

        while (frames.Count > 0 && failure == null)
        {
            JsonModelValidationFrame frame = frames[frames.Count - 1];
            if (frame.Value.Kind == JsonValueKind.Array)
            {
                ValidateNextArrayItem(frame, frames, activeContainers);
                continue;
            }

            if (frame.Value.Kind == JsonValueKind.Object)
            {
                ValidateNextObjectMember(frame, frames, activeContainers);
                continue;
            }

            SetFailure(
                JsonDiagnosticCode.InvalidModel,
                "A validation frame requires a container value.",
                frame.ValueDepth,
                frame.Value.Kind.ToString());
        }

        return CreateResult();
    }

    void ValidateNextArrayItem(
        JsonModelValidationFrame frame,
        global::System.Collections.Generic.List<JsonModelValidationFrame> frames,
        global::System.Collections.Generic.HashSet<JsonValue> activeContainers)
    {
        global::System.Collections.Generic.List<JsonValue> items = frame.Value.ArrayItems
            ?? throw new global::System.InvalidOperationException("A validated array requires an item list.");
        if (frame.NextIndex >= items.Count)
        {
            activeContainers.Remove(frame.Value);
            frames.RemoveAt(frames.Count - 1);
            return;
        }

        JsonValue? item = items[frame.NextIndex];
        frame.NextIndex++;
        int depth = frame.ValueDepth + 1;
        if (item == null)
        {
            SetFailure(JsonDiagnosticCode.InvalidModel, "An array item cannot be null.", depth, "null reference");
            return;
        }

        if (IsContainer(item) && activeContainers.Contains(item))
        {
            SetFailure(JsonDiagnosticCode.ModelCycle, "The JSON model contains a reference cycle.", depth, item.Kind.ToString());
            return;
        }

        if (!ValidateValueNode(item, depth))
        {
            return;
        }

        if (IsContainer(item))
        {
            activeContainers.Add(item);
            frames.Add(new JsonModelValidationFrame(item, depth));
        }
    }

    void ValidateNextObjectMember(
        JsonModelValidationFrame frame,
        global::System.Collections.Generic.List<JsonModelValidationFrame> frames,
        global::System.Collections.Generic.HashSet<JsonValue> activeContainers)
    {
        global::System.Collections.Generic.List<JsonMember> members = frame.Value.ObjectMembers
            ?? throw new global::System.InvalidOperationException("A validated object requires a member list.");
        if (frame.NextIndex >= members.Count)
        {
            activeContainers.Remove(frame.Value);
            frames.RemoveAt(frames.Count - 1);
            return;
        }

        JsonMember? member = members[frame.NextIndex];
        frame.NextIndex++;
        int depth = frame.ValueDepth + 1;
        if (member == null)
        {
            SetFailure(JsonDiagnosticCode.InvalidModel, "An object member cannot be null.", depth, "null reference");
            return;
        }

        if (!RegisterNode(depth, "member"))
        {
            return;
        }

        if (member.Name == null)
        {
            SetFailure(JsonDiagnosticCode.InvalidModel, "An object member name cannot be null.", depth, "member");
            return;
        }

        if (!ValidateString(member.Name, depth, "member name"))
        {
            return;
        }

        if (member.SourceOrdinal < -1)
        {
            SetFailure(JsonDiagnosticCode.InvalidModel, "An object member source ordinal cannot be less than -1.", depth, "member");
            return;
        }

        JsonValue? memberValue = member.Value;
        if (memberValue == null)
        {
            SetFailure(JsonDiagnosticCode.InvalidModel, "An object member value cannot be null.", depth, "null reference");
            return;
        }

        if (IsContainer(memberValue) && activeContainers.Contains(memberValue))
        {
            SetFailure(JsonDiagnosticCode.ModelCycle, "The JSON model contains a reference cycle.", depth, memberValue.Kind.ToString());
            return;
        }

        if (!ValidateValueNode(memberValue, depth))
        {
            return;
        }

        if (IsContainer(memberValue))
        {
            activeContainers.Add(memberValue);
            frames.Add(new JsonModelValidationFrame(memberValue, depth));
        }
    }

    bool ValidateValueNode(JsonValue value, int depth)
    {
        if (!RegisterNode(depth, value.Kind.ToString()))
        {
            return false;
        }

        if (!ValidateValueShape(value, depth))
        {
            return false;
        }

        if (value.Kind == JsonValueKind.Number)
        {
            return ValidateNumber(value.Number, depth);
        }

        if (value.Kind == JsonValueKind.String)
        {
            string text = value.StringValue
                ?? throw new global::System.InvalidOperationException("A validated string requires a payload.");
            return ValidateString(text, depth, "string value");
        }

        if (value.Kind == JsonValueKind.Array)
        {
            if (depth >= limits.MaximumNestingDepth)
            {
                return SetFailure(JsonDiagnosticCode.NestingDepthLimitExceeded, "The configured model nesting depth limit was exceeded.", depth, "Array");
            }

            global::System.Collections.Generic.List<JsonValue> items = value.ArrayItems
                ?? throw new global::System.InvalidOperationException("A validated array requires an item list.");
            if (items.Count > limits.MaximumArrayItems)
            {
                return SetFailure(JsonDiagnosticCode.ArrayItemLimitExceeded, "The configured model array item limit was exceeded.", depth, "Array");
            }
        }

        if (value.Kind == JsonValueKind.Object)
        {
            if (depth >= limits.MaximumNestingDepth)
            {
                return SetFailure(JsonDiagnosticCode.NestingDepthLimitExceeded, "The configured model nesting depth limit was exceeded.", depth, "Object");
            }

            global::System.Collections.Generic.List<JsonMember> members = value.ObjectMembers
                ?? throw new global::System.InvalidOperationException("A validated object requires a member list.");
            if (members.Count > limits.MaximumObjectMembers)
            {
                return SetFailure(JsonDiagnosticCode.ObjectMemberLimitExceeded, "The configured model object member limit was exceeded.", depth, "Object");
            }
        }

        return true;
    }

    bool ValidateValueShape(JsonValue value, int depth)
    {
        if (value.Kind == JsonValueKind.Uninitialized)
        {
            return SetFailure(JsonDiagnosticCode.InvalidModel, "An uninitialized value is not a JSON value.", depth, "Uninitialized");
        }

        if (value.Kind != JsonValueKind.Null &&
            value.Kind != JsonValueKind.Boolean &&
            value.Kind != JsonValueKind.Number &&
            value.Kind != JsonValueKind.String &&
            value.Kind != JsonValueKind.Array &&
            value.Kind != JsonValueKind.Object)
        {
            return SetFailure(JsonDiagnosticCode.InvalidModel, "The JSON value kind is undefined.", depth, value.Kind.ToString());
        }

        if (value.Kind == JsonValueKind.Number && value.Number == null)
        {
            return SetFailure(JsonDiagnosticCode.InvalidModel, "A number value requires a number payload.", depth, "Number");
        }

        if (value.Kind == JsonValueKind.String && value.StringValue == null)
        {
            return SetFailure(JsonDiagnosticCode.InvalidModel, "A string value requires a string payload.", depth, "String");
        }

        if (value.Kind == JsonValueKind.Array && value.ArrayItems == null)
        {
            return SetFailure(JsonDiagnosticCode.InvalidModel, "An array value requires an item list.", depth, "Array");
        }

        if (value.Kind == JsonValueKind.Object && value.ObjectMembers == null)
        {
            return SetFailure(JsonDiagnosticCode.InvalidModel, "An object value requires a member list.", depth, "Object");
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
            return SetFailure(JsonDiagnosticCode.InvalidModel, "A JSON value contains a payload that does not match its kind.", depth, value.Kind.ToString());
        }

        return true;
    }

    bool ValidateNumber(JsonNumber? number, int depth)
    {
        if (number == null || number.Lexeme == null)
        {
            return SetFailure(JsonDiagnosticCode.InvalidModel, "A number payload requires an exact lexeme.", depth, "Number");
        }

        if (number.Lexeme.Length > limits.MaximumNumberCharacters)
        {
            return SetFailure(JsonDiagnosticCode.NumberCharacterLimitExceeded, "The configured model number character limit was exceeded.", depth, "Number");
        }

        JsonValueResult parsed = numberFactory.TryCreateNumber(number.Lexeme);
        JsonNumber? expected = parsed.Value?.Number;
        if (!parsed.Success || expected == null)
        {
            return SetFailure(JsonDiagnosticCode.InvalidModel, "A number payload has an invalid JSON number lexeme.", depth, "Number");
        }

        if (number.Kind != expected.Kind ||
            number.IsNegative != expected.IsNegative ||
            number.HasFraction != expected.HasFraction ||
            number.HasExponent != expected.HasExponent ||
            number.IsNegativeZero != expected.IsNegativeZero)
        {
            return SetFailure(JsonDiagnosticCode.InvalidModel, "A number payload classification does not match its lexeme.", depth, "Number");
        }

        return true;
    }

    bool ValidateString(string value, int depth, string actual)
    {
        int index = 0;
        while (index < value.Length)
        {
            int scalar = value[index];
            if (scalar >= 0xD800 && scalar <= 0xDBFF)
            {
                if (index + 1 >= value.Length)
                {
                    return SetFailure(JsonDiagnosticCode.InvalidModel, "A model string contains an unpaired high surrogate.", depth, actual);
                }

                int lowSurrogate = value[index + 1];
                if (lowSurrogate < 0xDC00 || lowSurrogate > 0xDFFF)
                {
                    return SetFailure(JsonDiagnosticCode.InvalidModel, "A model string contains an unpaired high surrogate.", depth, actual);
                }

                scalar = 0x10000 + ((scalar - 0xD800) << 10) + (lowSurrogate - 0xDC00);
                index++;
            }
            else if (scalar >= 0xDC00 && scalar <= 0xDFFF)
            {
                return SetFailure(JsonDiagnosticCode.InvalidModel, "A model string contains an unpaired low surrogate.", depth, actual);
            }

            if (rejectUnicodeNoncharacters && IsUnicodeNoncharacter(scalar))
            {
                return SetFailure(JsonDiagnosticCode.InvalidModel, "The document profile forbids Unicode noncharacters.", depth, actual);
            }

            index++;
        }

        if (value.Length > limits.MaximumDecodedStringCodeUnits)
        {
            return SetFailure(JsonDiagnosticCode.DecodedStringCodeUnitLimitExceeded, "The configured model string code-unit limit was exceeded.", depth, actual);
        }

        int byteCount = global::System.Text.Encoding.UTF8.GetByteCount(value);
        if (byteCount > limits.MaximumDecodedStringBytes)
        {
            return SetFailure(JsonDiagnosticCode.DecodedStringByteLimitExceeded, "The configured model string byte limit was exceeded.", depth, actual);
        }

        return true;
    }

    bool ValidateDocumentMetadata(JsonDocument document)
    {
        if (document.SourceByteCount < 0)
        {
            return SetFailure(JsonDiagnosticCode.InvalidModel, "A document source byte count cannot be negative.", 0, "document");
        }

        if (document.SourceByteCount > limits.MaximumDocumentBytes)
        {
            return SetFailure(JsonDiagnosticCode.DocumentByteLimitExceeded, "The configured document byte limit was exceeded.", 0, "document");
        }

        if (document.ConformanceProfile != JsonConformanceProfile.SyntaxCompatible &&
            document.ConformanceProfile != JsonConformanceProfile.Portable &&
            document.ConformanceProfile != JsonConformanceProfile.IJson &&
            document.ConformanceProfile != JsonConformanceProfile.Jcs)
        {
            return SetFailure(JsonDiagnosticCode.InvalidModel, "The document conformance profile is undefined.", 0, "document");
        }

        if (document.DuplicateKeyPolicy != JsonDuplicateKeyPolicy.Preserve &&
            document.DuplicateKeyPolicy != JsonDuplicateKeyPolicy.Reject &&
            document.DuplicateKeyPolicy != JsonDuplicateKeyPolicy.KeepFirst &&
            document.DuplicateKeyPolicy != JsonDuplicateKeyPolicy.KeepLast)
        {
            return SetFailure(JsonDiagnosticCode.InvalidModel, "The document duplicate policy is undefined.", 0, "document");
        }

        if (document.UnicodePolicy != JsonUnicodePolicy.SyntaxCompatible &&
            document.UnicodePolicy != JsonUnicodePolicy.UnicodeScalar)
        {
            return SetFailure(JsonDiagnosticCode.InvalidModel, "The document Unicode policy is undefined.", 0, "document");
        }

        if (document.InputMode != JsonInputMode.SingleValue &&
            document.InputMode != JsonInputMode.WhitespaceSeparated &&
            document.InputMode != JsonInputMode.JsonTextSequence)
        {
            return SetFailure(JsonDiagnosticCode.InvalidModel, "The document input mode is undefined.", 0, "document");
        }

        if (document.BomPolicy != JsonBomPolicy.Reject &&
            document.BomPolicy != JsonBomPolicy.AllowInitial)
        {
            return SetFailure(JsonDiagnosticCode.InvalidModel, "The document BOM policy is undefined.", 0, "document");
        }

        if (document.ConformanceProfile == JsonConformanceProfile.Portable ||
            document.ConformanceProfile == JsonConformanceProfile.IJson ||
            document.ConformanceProfile == JsonConformanceProfile.Jcs)
        {
            if (document.DuplicateKeyPolicy != JsonDuplicateKeyPolicy.Reject ||
                document.UnicodePolicy != JsonUnicodePolicy.UnicodeScalar ||
                document.BomPolicy != JsonBomPolicy.Reject)
            {
                return SetFailure(JsonDiagnosticCode.InvalidModel, "The document metadata is incompatible with its conformance profile.", 0, "document");
            }
        }

        if (document.ConformanceProfile == JsonConformanceProfile.Jcs &&
            document.InputMode != JsonInputMode.SingleValue)
        {
            return SetFailure(JsonDiagnosticCode.InvalidModel, "A JCS document requires single-value input metadata.", 0, "document");
        }

        return true;
    }

    bool RegisterNode(int depth, string actual)
    {
        if (nodeCount >= limits.MaximumDomNodes)
        {
            return SetFailure(JsonDiagnosticCode.DomNodeLimitExceeded, "The configured model node limit was exceeded.", depth, actual);
        }

        nodeCount++;
        return true;
    }

    bool SetFailure(
        JsonDiagnosticCode code,
        string message,
        int depth,
        string actual)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Depth = depth;
        diagnostic.ActualToken = actual;
        failure = diagnostic;
        return false;
    }

    JsonOperationResult CreateResult()
    {
        JsonOperationResult result = new JsonOperationResult();
        result.Success = failure == null;
        result.Diagnostic = failure;
        return result;
    }

    bool IsContainer(JsonValue value)
    {
        return value.Kind == JsonValueKind.Array || value.Kind == JsonValueKind.Object;
    }

    bool IsUnicodeNoncharacter(int scalar)
    {
        return (scalar >= 0xFDD0 && scalar <= 0xFDEF) ||
            (scalar & 0xFFFF) == 0xFFFE ||
            (scalar & 0xFFFF) == 0xFFFF;
    }
}
