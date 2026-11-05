namespace json;

internal static class JsonDomOperations
{
    internal static JsonValue GetArrayItem(JsonValue array, int index)
    {
        global::System.Collections.Generic.List<JsonValue> items =
            GetReadableArrayItems(array);
        if (index < 0 || index >= items.Count)
        {
            throw CreateReadFailure(
                JsonDiagnosticCode.PointerTargetNotFound,
                "The array index does not identify an item.",
                index.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        JsonValue? item = items[index];
        if (item == null)
        {
            throw CreateReadFailure(
                JsonDiagnosticCode.InvalidModel,
                "An array item cannot be null.",
                "null reference");
        }

        return item;
    }

    internal static JsonValue GetObjectValue(
        JsonValue objectValue,
        string name,
        JsonDuplicateKeyPolicy duplicateKeyPolicy)
    {
        ValidateLookupName(name);
        global::System.Collections.Generic.List<JsonMember> matches =
            CollectObjectMatches(objectValue, name);
        if (matches.Count == 0)
        {
            throw CreateReadFailure(
                JsonDiagnosticCode.PointerTargetNotFound,
                "The object member name does not identify a value.",
                name);
        }

        int selectedIndex = 0;
        switch (duplicateKeyPolicy)
        {
            case JsonDuplicateKeyPolicy.Preserve:
                break;
            case JsonDuplicateKeyPolicy.Reject:
                if (matches.Count > 1)
                {
                    throw CreateReadFailure(
                        JsonDiagnosticCode.DuplicatePropertyName,
                        "The object contains more than one member with the requested name.",
                        name);
                }

                break;
            case JsonDuplicateKeyPolicy.KeepFirst:
                break;
            case JsonDuplicateKeyPolicy.KeepLast:
                selectedIndex = matches.Count - 1;
                break;
            default:
                throw CreateReadFailure(
                    JsonDiagnosticCode.InvalidOptions,
                    "The duplicate-name selection policy is undefined.",
                    duplicateKeyPolicy.ToString());
        }

        JsonValue? value = matches[selectedIndex].Value;
        if (value == null)
        {
            throw CreateReadFailure(
                JsonDiagnosticCode.InvalidModel,
                "An object member value cannot be null.",
                "null reference");
        }

        return value;
    }

    internal static global::System.Collections.Generic.List<JsonMember> GetObjectMatches(
        JsonValue objectValue,
        string name)
    {
        ValidateLookupName(name);
        return CollectObjectMatches(objectValue, name);
    }

    internal static JsonOperationResult AppendArrayItem(JsonValue array, JsonValue value)
    {
        JsonOperationResult? failure = ValidateArrayMutation(array, value);
        if (failure != null)
        {
            return failure;
        }

        global::System.Collections.Generic.List<JsonValue> items = array.ArrayItems
            ?? throw new global::System.InvalidOperationException("A validated array requires an item list.");
        items.Add(value);
        return CreateMutationSuccess();
    }

    internal static JsonOperationResult InsertArrayItem(
        JsonValue array,
        int index,
        JsonValue value)
    {
        JsonOperationResult? failure = ValidateArrayMutation(array, value);
        if (failure != null)
        {
            return failure;
        }

        global::System.Collections.Generic.List<JsonValue> items = array.ArrayItems
            ?? throw new global::System.InvalidOperationException("A validated array requires an item list.");
        if (index < 0 || index > items.Count)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.PointerTargetNotFound,
                "The array insertion index is outside the valid range.",
                index.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        items.Insert(index, value);
        return CreateMutationSuccess();
    }

    internal static JsonOperationResult ReplaceArrayItem(
        JsonValue array,
        int index,
        JsonValue value)
    {
        JsonOperationResult? failure = ValidateArrayMutation(array, value);
        if (failure != null)
        {
            return failure;
        }

        global::System.Collections.Generic.List<JsonValue> items = array.ArrayItems
            ?? throw new global::System.InvalidOperationException("A validated array requires an item list.");
        if (index < 0 || index >= items.Count)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.PointerTargetNotFound,
                "The array index does not identify an item to replace.",
                index.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        items[index] = value;
        return CreateMutationSuccess();
    }

    internal static JsonOperationResult RemoveArrayItem(JsonValue array, int index)
    {
        JsonOperationResult? failure = ValidateArrayContainerForMutation(array);
        if (failure != null)
        {
            return failure;
        }

        global::System.Collections.Generic.List<JsonValue> items = array.ArrayItems
            ?? throw new global::System.InvalidOperationException("A validated array requires an item list.");
        if (index < 0 || index >= items.Count)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.PointerTargetNotFound,
                "The array index does not identify an item to remove.",
                index.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        items.RemoveAt(index);
        return CreateMutationSuccess();
    }

    internal static JsonOperationResult ClearArrayItems(JsonValue array)
    {
        JsonOperationResult? failure = ValidateArrayContainerForMutation(array);
        if (failure != null)
        {
            return failure;
        }

        global::System.Collections.Generic.List<JsonValue> items = array.ArrayItems
            ?? throw new global::System.InvalidOperationException("A validated array requires an item list.");
        items.Clear();
        return CreateMutationSuccess();
    }

    internal static JsonOperationResult AppendObjectMember(
        JsonValue objectValue,
        JsonMember member)
    {
        JsonOperationResult? failure = ValidateObjectMutation(objectValue, member);
        if (failure != null)
        {
            return failure;
        }

        global::System.Collections.Generic.List<JsonMember> members = objectValue.ObjectMembers
            ?? throw new global::System.InvalidOperationException("A validated object requires a member list.");
        members.Add(member);
        return CreateMutationSuccess();
    }

    internal static JsonOperationResult InsertObjectMember(
        JsonValue objectValue,
        int index,
        JsonMember member)
    {
        JsonOperationResult? failure = ValidateObjectMutation(objectValue, member);
        if (failure != null)
        {
            return failure;
        }

        global::System.Collections.Generic.List<JsonMember> members = objectValue.ObjectMembers
            ?? throw new global::System.InvalidOperationException("A validated object requires a member list.");
        if (index < 0 || index > members.Count)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.PointerTargetNotFound,
                "The object member insertion index is outside the valid range.",
                index.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        members.Insert(index, member);
        return CreateMutationSuccess();
    }

    internal static JsonOperationResult ReplaceObjectMember(
        JsonValue objectValue,
        int index,
        JsonMember member)
    {
        JsonOperationResult? failure = ValidateObjectMutation(objectValue, member);
        if (failure != null)
        {
            return failure;
        }

        global::System.Collections.Generic.List<JsonMember> members = objectValue.ObjectMembers
            ?? throw new global::System.InvalidOperationException("A validated object requires a member list.");
        if (index < 0 || index >= members.Count)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.PointerTargetNotFound,
                "The object member index does not identify a member to replace.",
                index.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        members[index] = member;
        return CreateMutationSuccess();
    }

    internal static JsonOperationResult RemoveObjectMember(JsonValue objectValue, int index)
    {
        JsonOperationResult? failure = ValidateObjectContainerForMutation(objectValue);
        if (failure != null)
        {
            return failure;
        }

        global::System.Collections.Generic.List<JsonMember> members = objectValue.ObjectMembers
            ?? throw new global::System.InvalidOperationException("A validated object requires a member list.");
        if (index < 0 || index >= members.Count)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.PointerTargetNotFound,
                "The object member index does not identify a member to remove.",
                index.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        members.RemoveAt(index);
        return CreateMutationSuccess();
    }

    internal static JsonOperationResult ClearObjectMembers(JsonValue objectValue)
    {
        JsonOperationResult? failure = ValidateObjectContainerForMutation(objectValue);
        if (failure != null)
        {
            return failure;
        }

        global::System.Collections.Generic.List<JsonMember> members = objectValue.ObjectMembers
            ?? throw new global::System.InvalidOperationException("A validated object requires a member list.");
        members.Clear();
        return CreateMutationSuccess();
    }

    static global::System.Collections.Generic.List<JsonValue> GetReadableArrayItems(
        JsonValue? array)
    {
        if (array == null)
        {
            throw CreateReadFailure(
                JsonDiagnosticCode.PointerTypeMismatch,
                "An array value is required.",
                "null reference");
        }

        if (array.Kind != JsonValueKind.Array)
        {
            throw CreateReadFailure(
                JsonDiagnosticCode.PointerTypeMismatch,
                "The requested value is not an array.",
                array.Kind.ToString());
        }

        if (array.ArrayItems == null)
        {
            throw CreateReadFailure(
                JsonDiagnosticCode.InvalidModel,
                "An array value requires an item list.",
                array.Kind.ToString());
        }

        return array.ArrayItems;
    }

    static global::System.Collections.Generic.List<JsonMember> CollectObjectMatches(
        JsonValue? objectValue,
        string name)
    {
        global::System.Collections.Generic.List<JsonMember> members =
            GetReadableObjectMembers(objectValue);
        global::System.Collections.Generic.List<JsonMember> matches =
            new global::System.Collections.Generic.List<JsonMember>();
        foreach (JsonMember? member in members)
        {
            ValidateStoredMember(member);
            if (global::System.String.Equals(
                member.Name,
                name,
                global::System.StringComparison.Ordinal))
            {
                matches.Add(member);
            }
        }

        return matches;
    }

    static global::System.Collections.Generic.List<JsonMember> GetReadableObjectMembers(
        JsonValue? objectValue)
    {
        if (objectValue == null)
        {
            throw CreateReadFailure(
                JsonDiagnosticCode.PointerTypeMismatch,
                "An object value is required.",
                "null reference");
        }

        if (objectValue.Kind != JsonValueKind.Object)
        {
            throw CreateReadFailure(
                JsonDiagnosticCode.PointerTypeMismatch,
                "The requested value is not an object.",
                objectValue.Kind.ToString());
        }

        if (objectValue.ObjectMembers == null)
        {
            throw CreateReadFailure(
                JsonDiagnosticCode.InvalidModel,
                "An object value requires a member list.",
                objectValue.Kind.ToString());
        }

        return objectValue.ObjectMembers;
    }

    static void ValidateLookupName(string? name)
    {
        if (name == null)
        {
            throw CreateReadFailure(
                JsonDiagnosticCode.InvalidOptions,
                "An object member name is required.",
                "null reference");
        }
    }

    static void ValidateStoredMember(JsonMember? member)
    {
        if (member == null)
        {
            throw CreateReadFailure(
                JsonDiagnosticCode.InvalidModel,
                "An object member cannot be null.",
                "null reference");
        }

        if (member.Name == null)
        {
            throw CreateReadFailure(
                JsonDiagnosticCode.InvalidModel,
                "An object member name cannot be null.",
                "member");
        }

        if (member.Value == null)
        {
            throw CreateReadFailure(
                JsonDiagnosticCode.InvalidModel,
                "An object member value cannot be null.",
                "null reference");
        }

        if (member.SourceOrdinal < -1)
        {
            throw CreateReadFailure(
                JsonDiagnosticCode.InvalidModel,
                "An object member source ordinal cannot be less than -1.",
                "member");
        }
    }

    static JsonOperationResult? ValidateArrayMutation(JsonValue? array, JsonValue? value)
    {
        JsonOperationResult? failure = ValidateArrayContainerForMutation(array);
        if (failure != null)
        {
            return failure;
        }

        if (value == null)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.InvalidModel,
                "An array item cannot be null.",
                "null reference");
        }

        return null;
    }

    static JsonOperationResult? ValidateArrayContainerForMutation(JsonValue? array)
    {
        if (array == null)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.PointerTypeMismatch,
                "An array value is required.",
                "null reference");
        }

        if (array.Kind != JsonValueKind.Array)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.PointerTypeMismatch,
                "The requested value is not an array.",
                array.Kind.ToString());
        }

        if (array.ArrayItems == null)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.InvalidModel,
                "An array value requires an item list.",
                array.Kind.ToString());
        }

        return null;
    }

    static JsonOperationResult? ValidateObjectMutation(
        JsonValue? objectValue,
        JsonMember? member)
    {
        JsonOperationResult? failure = ValidateObjectContainerForMutation(objectValue);
        if (failure != null)
        {
            return failure;
        }

        if (member == null)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.InvalidModel,
                "An object member cannot be null.",
                "null reference");
        }

        if (member.Name == null)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.InvalidModel,
                "An object member name cannot be null.",
                "member");
        }

        if (member.Value == null)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.InvalidModel,
                "An object member value cannot be null.",
                "null reference");
        }

        if (member.SourceOrdinal < -1)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.InvalidModel,
                "An object member source ordinal cannot be less than -1.",
                "member");
        }

        return null;
    }

    static JsonOperationResult? ValidateObjectContainerForMutation(JsonValue? objectValue)
    {
        if (objectValue == null)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.PointerTypeMismatch,
                "An object value is required.",
                "null reference");
        }

        if (objectValue.Kind != JsonValueKind.Object)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.PointerTypeMismatch,
                "The requested value is not an object.",
                objectValue.Kind.ToString());
        }

        if (objectValue.ObjectMembers == null)
        {
            return CreateMutationFailure(
                JsonDiagnosticCode.InvalidModel,
                "An object value requires a member list.",
                objectValue.Kind.ToString());
        }

        return null;
    }

    static JsonOperationResult CreateMutationSuccess()
    {
        JsonOperationResult result = new JsonOperationResult();
        result.Success = true;
        return result;
    }

    static JsonOperationResult CreateMutationFailure(
        JsonDiagnosticCode code,
        string message,
        string actualToken)
    {
        JsonOperationResult result = new JsonOperationResult();
        result.Success = false;
        result.Diagnostic = CreateDomDiagnostic(code, message, actualToken);
        return result;
    }

    static JsonPointerException CreateReadFailure(
        JsonDiagnosticCode code,
        string message,
        string actualToken)
    {
        return new JsonPointerException(CreateDomDiagnostic(code, message, actualToken));
    }

    static JsonDiagnostic CreateDomDiagnostic(
        JsonDiagnosticCode code,
        string message,
        string actualToken)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.ActualToken = actualToken;
        return diagnostic;
    }
}
