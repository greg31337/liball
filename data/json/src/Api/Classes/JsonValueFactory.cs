namespace json;

/// <summary>Creates and clones canonical JSON model values.</summary>
public class JsonValueFactory : IJsonValueFactory
{
    /// <summary>Initializes a reusable JSON value factory.</summary>
    public JsonValueFactory()
    {
    }

    /// <summary>Creates the JSON null value.</summary>
    /// <returns>The created value.</returns>
    public JsonValue CreateNull()
    {
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.Null;
        return value;
    }

    /// <summary>Creates a JSON boolean value.</summary>
    /// <param name="value">The boolean payload.</param>
    /// <returns>The created value.</returns>
    public JsonValue CreateBoolean(bool value)
    {
        JsonValue result = new JsonValue();
        result.Kind = JsonValueKind.Boolean;
        result.BooleanValue = value;
        return result;
    }

    /// <summary>Creates a JSON number after validating its exact lexeme.</summary>
    /// <param name="lexeme">The exact decimal lexeme.</param>
    /// <returns>The created value.</returns>
    public JsonValue CreateNumber(string lexeme)
    {
        return CreateValidatedNumber(lexeme);
    }

    /// <summary>Attempts to create a JSON number without throwing for an invalid lexeme.</summary>
    /// <param name="lexeme">The exact decimal lexeme.</param>
    /// <returns>The creation outcome.</returns>
    public JsonValueResult TryCreateNumber(string lexeme)
    {
        JsonValue? value = null;
        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => value = CreateValidatedNumber(lexeme));
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        JsonValueResult result = new JsonValueResult();
        if (failure == null)
        {
            result.Success = true;
            result.Status = JsonReadStatus.DocumentComplete;
            result.Value = value;
            return result;
        }

        JsonParseException? parseFailure = failure as JsonParseException;
        if (parseFailure == null)
        {
            throw failure;
        }

        result.Success = false;
        result.Status = JsonReadStatus.Error;
        result.Diagnostic = parseFailure.Diagnostic;
        return result;
    }

    /// <summary>Creates a JSON string value.</summary>
    /// <param name="value">The decoded string payload.</param>
    /// <returns>The created value.</returns>
    public JsonValue CreateString(string value)
    {
        JsonValue result = new JsonValue();
        result.Kind = JsonValueKind.String;
        result.StringValue = value;
        return result;
    }

    /// <summary>Creates a JSON array from ordered items.</summary>
    /// <param name="items">The ordered array items.</param>
    /// <returns>The created value.</returns>
    public JsonValue CreateArray(global::System.Collections.Generic.List<JsonValue> items)
    {
        JsonValue result = new JsonValue();
        result.Kind = JsonValueKind.Array;
        result.ArrayItems = new global::System.Collections.Generic.List<JsonValue>(items);
        return result;
    }

    /// <summary>Creates a JSON object from ordered, duplicate-preserving members.</summary>
    /// <param name="members">The ordered object members.</param>
    /// <returns>The created value.</returns>
    public JsonValue CreateObject(global::System.Collections.Generic.List<JsonMember> members)
    {
        JsonValue result = new JsonValue();
        result.Kind = JsonValueKind.Object;
        result.ObjectMembers = new global::System.Collections.Generic.List<JsonMember>(members);
        return result;
    }

    /// <summary>Creates an iterative deep clone of a JSON value.</summary>
    /// <param name="value">The value to clone.</param>
    /// <returns>The independent clone.</returns>
    public JsonValue CloneValue(JsonValue value)
    {
        return JsonValueCloner.CloneValueGraph(value);
    }

    JsonValue CreateValidatedNumber(string lexeme)
    {
        if (lexeme == null)
        {
            throw CreateNumberFailure("A JSON number lexeme is required.", null);
        }

        JsonNumberScanner scanner = new JsonNumberScanner();
        foreach (char character in lexeme)
        {
            if (character > 0x7F)
            {
                throw CreateNumberFailure(
                    "A JSON number lexeme can contain only ASCII grammar characters.",
                    global::System.Text.Encoding.UTF8.GetBytes(lexeme));
            }

            scanner.PushByte((byte)character);
        }

        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.Number;
        value.Number = scanner.CreateNumber();
        return value;
    }

    JsonParseException CreateNumberFailure(string message, byte[]? excerpt)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.InvalidNumber;
        diagnostic.Message = message;
        diagnostic.SourceExcerpt = excerpt;
        return new JsonParseException(diagnostic);
    }
}
