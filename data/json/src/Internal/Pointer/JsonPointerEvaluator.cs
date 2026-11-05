namespace json;

internal class JsonPointerStepResult
{
    internal bool Success;
    internal global::System.Collections.Generic.List<JsonValue> Values =
        new global::System.Collections.Generic.List<JsonValue>();
    internal JsonDiagnosticCode Code;
    internal string? Message;
    internal string? ExpectedTokens;
}

internal class JsonPointerIndexResult
{
    internal bool Success;
    internal int Value;
    internal JsonDiagnosticCode Code;
    internal string? Message;
}

internal static class JsonPointerEvaluator
{
    internal static JsonValue ResolveSingleValue(
        JsonValue root,
        JsonPointer pointer,
        JsonDuplicateKeyPolicy duplicateKeyPolicy)
    {
        JsonPointerResult result = TryResolveSingleValue(root, pointer, duplicateKeyPolicy);
        if (!result.Success)
        {
            JsonDiagnostic diagnostic = result.Diagnostic
                ?? throw new global::System.InvalidOperationException("A failed pointer evaluation requires a diagnostic.");
            throw new JsonPointerException(diagnostic);
        }

        return result.Value
            ?? throw new global::System.InvalidOperationException("A successful pointer evaluation requires a value.");
    }

    internal static JsonPointerResult TryResolveSingleValue(
        JsonValue root,
        JsonPointer pointer,
        JsonDuplicateKeyPolicy duplicateKeyPolicy)
    {
        JsonPointerResult? invalid = ValidateEvaluationInputs(
            root,
            pointer,
            duplicateKeyPolicy,
            true);
        if (invalid != null)
        {
            return invalid;
        }

        global::System.Collections.Generic.List<string> tokens = pointer.ReferenceTokens
            ?? throw new global::System.InvalidOperationException("A validated pointer requires reference tokens.");
        JsonValue current = root;
        int segmentIndex = 0;
        while (segmentIndex < tokens.Count)
        {
            string token = tokens[segmentIndex];
            JsonPointerStepResult step = ResolveStep(
                current,
                token,
                duplicateKeyPolicy,
                false);
            if (!step.Success)
            {
                return CreateFailureResult(pointer, segmentIndex, token, step);
            }

            current = step.Values[0];
            segmentIndex++;
        }

        JsonPointerResult result = new JsonPointerResult();
        result.Success = true;
        result.Value = current;
        result.Values = null;
        result.FailedSegmentIndex = -1;
        result.Diagnostic = null;
        return result;
    }

    internal static JsonPointerResult ResolveAllValues(JsonValue root, JsonPointer pointer)
    {
        JsonPointerResult? invalid = ValidateEvaluationInputs(
            root,
            pointer,
            JsonDuplicateKeyPolicy.Preserve,
            false);
        if (invalid != null)
        {
            return invalid;
        }

        global::System.Collections.Generic.List<string> tokens = pointer.ReferenceTokens
            ?? throw new global::System.InvalidOperationException("A validated pointer requires reference tokens.");
        global::System.Collections.Generic.List<JsonValue> current =
            new global::System.Collections.Generic.List<JsonValue>();
        current.Add(root);
        int segmentIndex = 0;
        while (segmentIndex < tokens.Count)
        {
            string token = tokens[segmentIndex];
            global::System.Collections.Generic.List<JsonValue> next =
                new global::System.Collections.Generic.List<JsonValue>();
            JsonPointerStepResult? firstFailure = null;
            foreach (JsonValue candidate in current)
            {
                JsonPointerStepResult step = ResolveStep(
                    candidate,
                    token,
                    JsonDuplicateKeyPolicy.Preserve,
                    true);
                if (!step.Success)
                {
                    if (firstFailure == null)
                    {
                        firstFailure = step;
                    }

                    continue;
                }

                foreach (JsonValue value in step.Values)
                {
                    next.Add(value);
                }
            }

            if (next.Count == 0)
            {
                JsonPointerStepResult failure = firstFailure
                    ?? throw new global::System.InvalidOperationException(
                        "An unsuccessful all-match step requires a failure.");
                return CreateFailureResult(pointer, segmentIndex, token, failure);
            }

            current = next;
            segmentIndex++;
        }

        JsonPointerResult result = new JsonPointerResult();
        result.Success = true;
        result.Value = current[0];
        result.Values = current;
        result.FailedSegmentIndex = -1;
        result.Diagnostic = null;
        return result;
    }

    static JsonPointerResult? ValidateEvaluationInputs(
        JsonValue? root,
        JsonPointer? pointer,
        JsonDuplicateKeyPolicy duplicateKeyPolicy,
        bool validateDuplicatePolicy)
    {
        if (root == null)
        {
            return CreateInputFailure(
                JsonDiagnosticCode.InvalidModel,
                "A JSON root value is required for pointer evaluation.",
                -1,
                null,
                "root");
        }

        if (pointer == null || pointer.ReferenceTokens == null)
        {
            return CreateInputFailure(
                JsonDiagnosticCode.InvalidPointerSyntax,
                "A decoded JSON Pointer with reference tokens is required.",
                -1,
                null,
                "pointer model");
        }

        if (pointer.Encoding != JsonPointerEncoding.String &&
            pointer.Encoding != JsonPointerEncoding.UriFragment)
        {
            return CreateInputFailure(
                JsonDiagnosticCode.InvalidPointerSyntax,
                "The JSON Pointer encoding is undefined.",
                -1,
                pointer,
                "pointer model");
        }

        if (validateDuplicatePolicy &&
            duplicateKeyPolicy != JsonDuplicateKeyPolicy.Preserve &&
            duplicateKeyPolicy != JsonDuplicateKeyPolicy.Reject &&
            duplicateKeyPolicy != JsonDuplicateKeyPolicy.KeepFirst &&
            duplicateKeyPolicy != JsonDuplicateKeyPolicy.KeepLast)
        {
            return CreateInputFailure(
                JsonDiagnosticCode.InvalidOptions,
                "The duplicate-name selection policy is undefined.",
                -1,
                pointer,
                duplicateKeyPolicy.ToString());
        }

        int tokenIndex = 0;
        while (tokenIndex < pointer.ReferenceTokens.Count)
        {
            string? token = pointer.ReferenceTokens[tokenIndex];
            if (token == null || !IsValidUnicodeText(token))
            {
                return CreateInputFailure(
                    JsonDiagnosticCode.InvalidPointerSyntax,
                    "A JSON Pointer reference token contains invalid Unicode text.",
                    tokenIndex,
                    pointer,
                    "reference token");
            }

            tokenIndex++;
        }

        return null;
    }

    static JsonPointerStepResult ResolveStep(
        JsonValue current,
        string token,
        JsonDuplicateKeyPolicy duplicateKeyPolicy,
        bool allMatches)
    {
        if (current.Kind == JsonValueKind.Object)
        {
            return ResolveObjectStep(current, token, duplicateKeyPolicy, allMatches);
        }

        if (current.Kind == JsonValueKind.Array)
        {
            return ResolveArrayStep(current, token);
        }

        return CreateStepFailure(
            JsonDiagnosticCode.PointerTypeMismatch,
            "A pointer reference token cannot be applied to the current value kind.",
            "array index or object member name");
    }

    static JsonPointerStepResult ResolveObjectStep(
        JsonValue current,
        string token,
        JsonDuplicateKeyPolicy duplicateKeyPolicy,
        bool allMatches)
    {
        if (current.ObjectMembers == null)
        {
            return CreateStepFailure(
                JsonDiagnosticCode.InvalidModel,
                "An object value requires a member list.",
                "valid object model");
        }

        JsonPointerStepResult result = new JsonPointerStepResult();
        foreach (JsonMember? member in current.ObjectMembers)
        {
            if (member == null ||
                member.Name == null ||
                member.Value == null ||
                member.SourceOrdinal < -1)
            {
                return CreateStepFailure(
                    JsonDiagnosticCode.InvalidModel,
                    "The object contains an invalid member.",
                    "valid object member");
            }

            if (global::System.String.Equals(
                member.Name,
                token,
                global::System.StringComparison.Ordinal))
            {
                result.Values.Add(member.Value);
            }
        }

        if (result.Values.Count == 0)
        {
            return CreateStepFailure(
                JsonDiagnosticCode.PointerTargetNotFound,
                "The pointer token does not identify an object member.",
                "existing object member name");
        }

        if (allMatches)
        {
            result.Success = true;
            return result;
        }

        int selectedIndex = 0;
        if (duplicateKeyPolicy == JsonDuplicateKeyPolicy.Reject &&
            result.Values.Count > 1)
        {
            return CreateStepFailure(
                JsonDiagnosticCode.DuplicatePropertyName,
                "The pointer token identifies duplicate object members.",
                "unique object member name");
        }

        if (duplicateKeyPolicy == JsonDuplicateKeyPolicy.KeepLast)
        {
            selectedIndex = result.Values.Count - 1;
        }

        JsonValue selected = result.Values[selectedIndex];
        result.Values.Clear();
        result.Values.Add(selected);
        result.Success = true;
        return result;
    }

    static JsonPointerStepResult ResolveArrayStep(JsonValue current, string token)
    {
        if (current.ArrayItems == null)
        {
            return CreateStepFailure(
                JsonDiagnosticCode.InvalidModel,
                "An array value requires an item list.",
                "valid array model");
        }

        if (token == "-")
        {
            return CreateStepFailure(
                JsonDiagnosticCode.PointerTargetNotFound,
                "The '-' array token refers after the last existing item.",
                "existing array index");
        }

        JsonPointerIndexResult parsed = ParseArrayIndex(token);
        if (!parsed.Success)
        {
            return CreateStepFailure(
                parsed.Code,
                parsed.Message ?? "The pointer array index is invalid.",
                "array index");
        }

        if (parsed.Value >= current.ArrayItems.Count)
        {
            return CreateStepFailure(
                JsonDiagnosticCode.PointerTargetNotFound,
                "The pointer array index is outside the array bounds.",
                "existing array index");
        }

        JsonValue? value = current.ArrayItems[parsed.Value];
        if (value == null)
        {
            return CreateStepFailure(
                JsonDiagnosticCode.InvalidModel,
                "An array item cannot be null.",
                "valid array item");
        }

        JsonPointerStepResult result = new JsonPointerStepResult();
        result.Success = true;
        result.Values.Add(value);
        return result;
    }

    static JsonPointerIndexResult ParseArrayIndex(string token)
    {
        JsonPointerIndexResult result = new JsonPointerIndexResult();
        if (token.Length == 0 || (token.Length > 1 && token[0] == '0'))
        {
            result.Code = JsonDiagnosticCode.InvalidPointerIndex;
            result.Message = "A pointer array index must be '0' or begin with a nonzero digit.";
            return result;
        }

        int value = 0;
        int index = 0;
        while (index < token.Length)
        {
            char character = token[index];
            if (character < '0' || character > '9')
            {
                result.Code = JsonDiagnosticCode.InvalidPointerIndex;
                result.Message = "A pointer array index can contain only decimal digits.";
                return result;
            }

            int digit = character - '0';
            if (value > (int.MaxValue - digit) / 10)
            {
                result.Code = JsonDiagnosticCode.PointerIndexOutOfRange;
                result.Message = "The pointer array index exceeds the supported integer range.";
                return result;
            }

            value = (value * 10) + digit;
            index++;
        }

        result.Success = true;
        result.Value = value;
        return result;
    }

    static JsonPointerStepResult CreateStepFailure(
        JsonDiagnosticCode code,
        string message,
        string expectedTokens)
    {
        JsonPointerStepResult result = new JsonPointerStepResult();
        result.Success = false;
        result.Code = code;
        result.Message = message;
        result.ExpectedTokens = expectedTokens;
        return result;
    }

    static JsonPointerResult CreateFailureResult(
        JsonPointer pointer,
        int segmentIndex,
        string token,
        JsonPointerStepResult step)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = step.Code;
        diagnostic.Message = step.Message;
        diagnostic.Pointer = BuildResolvedPrefix(pointer, segmentIndex);
        diagnostic.Depth = segmentIndex;
        diagnostic.ExpectedTokens = step.ExpectedTokens;
        diagnostic.ActualToken = token;
        JsonPointerResult result = new JsonPointerResult();
        result.Success = false;
        result.Value = null;
        result.Values = null;
        result.FailedSegmentIndex = segmentIndex;
        result.Diagnostic = diagnostic;
        return result;
    }

    static JsonPointerResult CreateInputFailure(
        JsonDiagnosticCode code,
        string message,
        int failedSegmentIndex,
        JsonPointer? pointer,
        string actualToken)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Pointer = BuildResolvedPrefix(pointer, failedSegmentIndex);
        diagnostic.Depth = failedSegmentIndex < 0 ? 0 : failedSegmentIndex;
        diagnostic.ActualToken = actualToken;
        JsonPointerResult result = new JsonPointerResult();
        result.Success = false;
        result.Value = null;
        result.Values = null;
        result.FailedSegmentIndex = failedSegmentIndex;
        result.Diagnostic = diagnostic;
        return result;
    }

    static string BuildResolvedPrefix(JsonPointer? pointer, int segmentIndex)
    {
        if (pointer == null || pointer.ReferenceTokens == null || segmentIndex <= 0)
        {
            return "";
        }

        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        int index = 0;
        while (index < segmentIndex && index < pointer.ReferenceTokens.Count)
        {
            string? token = pointer.ReferenceTokens[index];
            if (token == null)
            {
                return builder.ToString();
            }

            builder.Append('/');
            AppendEscapedToken(builder, token);
            index++;
        }

        return builder.ToString();
    }

    static void AppendEscapedToken(global::System.Text.StringBuilder builder, string token)
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
