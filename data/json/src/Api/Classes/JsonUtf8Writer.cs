namespace json;

/// <summary>Writes stateful JSON values as UTF-8 to a caller-owned destination.</summary>
public class JsonUtf8Writer : IJsonTokenWriter
{
    static readonly byte[] NullBytes = { 0x6E, 0x75, 0x6C, 0x6C };
    static readonly byte[] TrueBytes = { 0x74, 0x72, 0x75, 0x65 };
    static readonly byte[] FalseBytes = { 0x66, 0x61, 0x6C, 0x73, 0x65 };

    JsonWriterCore core;
    JsonStringEncoder stringEncoder;
    JsonNumberFormatter numberFormatter;
    JsonPropertySorter propertySorter;
    JsonModelValidator modelValidator;
    JsonUtf8Validator rawValidator = new JsonUtf8Validator();
    JsonReaderOptions rawReaderOptions;
    JsonDuplicateKeyPolicy duplicateKeyPolicy;
    bool skipModelValidation;
    long compositeBytesWritten;

    /// <summary>Initializes a writer with output policies and one caller-owned destination.</summary>
    /// <param name="options">The output policies to apply.</param>
    /// <param name="target">The caller-owned output destination.</param>
    public JsonUtf8Writer(JsonWriterOptions options, JsonWriterTarget target)
    {
        JsonWriterOptions normalized =
            JsonOptionNormalizer.NormalizeWriterOptions(options);
        JsonWriterTarget normalizedTarget =
            JsonOptionNormalizer.NormalizeWriterTarget(target);
        JsonLimits limits = normalized.Limits
            ?? throw new global::System.InvalidOperationException(
                "Normalized writer limits are required.");
        core = new JsonWriterCore(normalized, normalizedTarget);
        stringEncoder = new JsonStringEncoder(normalized);
        numberFormatter = new JsonNumberFormatter(normalized);
        propertySorter = new JsonPropertySorter(normalized);
        modelValidator = new JsonModelValidator(limits);
        duplicateKeyPolicy = normalized.DuplicateKeyPolicy;
        skipModelValidation = normalized.SkipModelValidation;
        rawReaderOptions = CreateRawReaderOptions(normalized, limits);
    }

    /// <summary>Writes the beginning of an object.</summary>
    /// <returns>The write outcome.</returns>
    public JsonOperationResult WriteStartObject()
    {
        return core.WriteObjectStart();
    }

    /// <summary>Writes the end of an object.</summary>
    /// <returns>The write outcome.</returns>
    public JsonOperationResult WriteEndObject()
    {
        return core.WriteObjectEnd();
    }

    /// <summary>Writes the beginning of an array.</summary>
    /// <returns>The write outcome.</returns>
    public JsonOperationResult WriteStartArray()
    {
        return core.WriteArrayStart();
    }

    /// <summary>Writes the end of an array.</summary>
    /// <returns>The write outcome.</returns>
    public JsonOperationResult WriteEndArray()
    {
        return core.WriteArrayEnd();
    }

    /// <summary>Writes an object property name.</summary>
    /// <param name="name">The decoded property name.</param>
    /// <returns>The write outcome.</returns>
    public JsonOperationResult WritePropertyName(string name)
    {
        return WriteEncodedProperty(() => stringEncoder.EncodeString(name));
    }

    /// <summary>Writes the JSON null value.</summary>
    /// <returns>The write outcome.</returns>
    public JsonOperationResult WriteNull()
    {
        return core.WriteEncodedScalarValue(NullBytes);
    }

    /// <summary>Writes a JSON boolean value.</summary>
    /// <param name="value">The boolean value.</param>
    /// <returns>The write outcome.</returns>
    public JsonOperationResult WriteBoolean(bool value)
    {
        return core.WriteEncodedScalarValue(value ? TrueBytes : FalseBytes);
    }

    /// <summary>Writes a JSON string value.</summary>
    /// <param name="value">The decoded string value.</param>
    /// <returns>The write outcome.</returns>
    public JsonOperationResult WriteString(string value)
    {
        return WriteEncodedScalar(() => stringEncoder.EncodeString(value));
    }

    /// <summary>Writes a validated exact JSON number lexeme.</summary>
    /// <param name="lexeme">The exact number lexeme.</param>
    /// <returns>The write outcome.</returns>
    public JsonOperationResult WriteNumberLexeme(string lexeme)
    {
        return WriteEncodedScalar(() =>
            numberFormatter.FormatExactLexeme(lexeme));
    }

    /// <summary>Writes a signed 64-bit number.</summary>
    /// <param name="value">The number to write.</param>
    /// <returns>The write outcome.</returns>
    public JsonOperationResult WriteInt64(long value)
    {
        return WriteEncodedScalar(() => numberFormatter.FormatInt64(value));
    }

    /// <summary>Writes an unsigned 64-bit number.</summary>
    /// <param name="value">The number to write.</param>
    /// <returns>The write outcome.</returns>
    public JsonOperationResult WriteUInt64(ulong value)
    {
        return WriteEncodedScalar(() => numberFormatter.FormatUInt64(value));
    }

    /// <summary>Writes a decimal number.</summary>
    /// <param name="value">The number to write.</param>
    /// <returns>The write outcome.</returns>
    public JsonOperationResult WriteDecimal(decimal value)
    {
        return WriteEncodedScalar(() => numberFormatter.FormatDecimal(value));
    }

    /// <summary>Writes a finite double-precision number.</summary>
    /// <param name="value">The number to write.</param>
    /// <returns>The write outcome.</returns>
    public JsonOperationResult WriteDouble(double value)
    {
        return WriteEncodedScalar(() => numberFormatter.FormatDouble(value));
    }

    /// <summary>Writes a validated JSON model value.</summary>
    /// <param name="value">The value to write.</param>
    /// <returns>The write outcome.</returns>
    public JsonOperationResult WriteJsonValue(JsonValue value)
    {
        compositeBytesWritten = 0;
        JsonOperationResult? result = null;
        JsonExceptionCapture capture = new JsonExceptionCapture(() =>
            result = ExecuteModelWrite(value));
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        JsonOperationResult completed;
        if (failure == null)
        {
            completed = result
                ?? throw new global::System.InvalidOperationException(
                    "Model writing did not produce a result.");
        }
        else
        {
            completed = CreateFailureFromException(failure);
            completed.BytesWritten = compositeBytesWritten;
        }

        compositeBytesWritten = 0;
        return completed;
    }

    /// <summary>Validates and writes exactly one raw UTF-8 JSON value.</summary>
    /// <param name="utf8">The raw UTF-8 value, which is not retained.</param>
    /// <returns>The write outcome.</returns>
    public JsonOperationResult WriteRawValue(
        global::System.ReadOnlySpan<byte> utf8)
    {
        JsonOperationResult validation = rawValidator.ValidateUtf8(
            utf8,
            rawReaderOptions);
        if (!validation.Success)
        {
            return CreateRawFailure(validation);
        }

        JsonOperationResult result = core.WriteEncodedScalarValue(utf8);
        result.BytesConsumed = utf8.Length;
        return result;
    }

    /// <summary>Flushes pending output without closing the caller-owned destination.</summary>
    /// <returns>The flush outcome.</returns>
    public JsonOperationResult FlushWriter()
    {
        return core.FlushOutput();
    }

    /// <summary>Completes and flushes one well-formed output stream.</summary>
    /// <returns>The completion outcome.</returns>
    public JsonOperationResult CompleteWriter()
    {
        return core.CompleteOutput();
    }

    /// <summary>Gets the number of output bytes pending in the writer.</summary>
    /// <returns>The pending byte count.</returns>
    public long GetBytesPending()
    {
        return core.GetPendingByteCount();
    }

    /// <summary>Gets the number of output bytes committed to the destination.</summary>
    /// <returns>The committed byte count.</returns>
    public long GetBytesCommitted()
    {
        return core.GetCommittedByteCount();
    }

    /// <summary>Resets the writer for independent reuse.</summary>
    public void ResetWriter()
    {
        compositeBytesWritten = 0;
        core.ResetCore();
    }

    JsonOperationResult WriteEncodedProperty(
        global::System.Func<byte[]> encode)
    {
        byte[]? bytes = null;
        JsonExceptionCapture capture = new JsonExceptionCapture(() =>
            bytes = encode());
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            return CreateFailureFromException(failure);
        }

        return core.WriteEncodedPropertyName(bytes
            ?? throw new global::System.InvalidOperationException(
                "Property-name encoding did not produce bytes."));
    }

    JsonOperationResult WriteEncodedScalar(
        global::System.Func<byte[]> encode)
    {
        byte[]? bytes = null;
        JsonExceptionCapture capture = new JsonExceptionCapture(() =>
            bytes = encode());
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            return CreateFailureFromException(failure);
        }

        return core.WriteEncodedScalarValue(bytes
            ?? throw new global::System.InvalidOperationException(
                "Scalar encoding did not produce bytes."));
    }

    JsonOperationResult ExecuteModelWrite(JsonValue value)
    {
        if (value == null)
        {
            return CreateFailure(CreateDiagnostic(
                JsonDiagnosticCode.InvalidModel,
                "A JSON root value is required.",
                "root"));
        }

        if (!skipModelValidation)
        {
            JsonOperationResult validation =
                modelValidator.ValidateValueGraph(value);
            if (!validation.Success)
            {
                return validation;
            }

            ValidateRejectedDuplicates(value);
        }

        global::System.Collections.Generic.List<JsonDomWriteFrame> frames =
            new global::System.Collections.Generic.List<JsonDomWriteFrame>();
        global::System.Collections.Generic.HashSet<JsonValue> active =
            new global::System.Collections.Generic.HashSet<JsonValue>();
        JsonOperationResult result = WriteModelNode(value, frames, active);
        if (!AccumulateModelResult(result))
        {
            return result;
        }

        while (frames.Count > 0)
        {
            JsonDomWriteFrame frame = frames[frames.Count - 1];
            if (frame.Value.Kind == JsonValueKind.Array)
            {
                global::System.Collections.Generic.List<JsonValue> items =
                    frame.Value.ArrayItems
                    ?? throw CreateModelFailure(
                        "An array value requires an item list.",
                        "Array");
                if (frame.NextIndex >= items.Count)
                {
                    result = WriteEndArray();
                    if (!AccumulateModelResult(result))
                    {
                        return result;
                    }

                    frames.RemoveAt(frames.Count - 1);
                    active.Remove(frame.Value);
                    continue;
                }

                JsonValue? item = items[frame.NextIndex];
                frame.NextIndex++;
                if (item == null)
                {
                    throw CreateModelFailure(
                        "An array item cannot be null.",
                        "null reference");
                }

                result = WriteModelNode(item, frames, active);
                if (!AccumulateModelResult(result))
                {
                    return result;
                }

                continue;
            }

            global::System.Collections.Generic.List<JsonMember> members =
                frame.Members
                ?? throw CreateModelFailure(
                    "An object value requires a member list.",
                    "Object");
            if (frame.NextIndex >= members.Count)
            {
                result = WriteEndObject();
                if (!AccumulateModelResult(result))
                {
                    return result;
                }

                frames.RemoveAt(frames.Count - 1);
                active.Remove(frame.Value);
                continue;
            }

            JsonMember? member = members[frame.NextIndex];
            frame.NextIndex++;
            if (member == null || member.Name == null ||
                member.Value == null)
            {
                throw CreateModelFailure(
                    "An object member requires a name and value.",
                    "ObjectMember");
            }

            result = WritePropertyName(member.Name);
            if (!AccumulateModelResult(result))
            {
                return result;
            }

            result = WriteModelNode(member.Value, frames, active);
            if (!AccumulateModelResult(result))
            {
                return result;
            }
        }

        return CreateSuccess(compositeBytesWritten);
    }

    JsonOperationResult WriteModelNode(
        JsonValue value,
        global::System.Collections.Generic.List<JsonDomWriteFrame> frames,
        global::System.Collections.Generic.HashSet<JsonValue> active)
    {
        if (value.Kind == JsonValueKind.Null)
        {
            return WriteNull();
        }

        if (value.Kind == JsonValueKind.Boolean)
        {
            return WriteBoolean(value.BooleanValue);
        }

        if (value.Kind == JsonValueKind.Number)
        {
            string lexeme = value.Number?.Lexeme
                ?? throw CreateModelFailure(
                    "A number value requires an exact lexeme.",
                    "Number");
            return WriteNumberLexeme(lexeme);
        }

        if (value.Kind == JsonValueKind.String)
        {
            string text = value.StringValue
                ?? throw CreateModelFailure(
                    "A string value requires a decoded payload.",
                    "String");
            return WriteString(text);
        }

        if (value.Kind == JsonValueKind.Array)
        {
            if (value.ArrayItems == null)
            {
                throw CreateModelFailure(
                    "An array value requires an item list.",
                    "Array");
            }

            if (active.Contains(value))
            {
                throw CreateModelCycleFailure();
            }

            JsonOperationResult result = WriteStartArray();
            if (result.Success)
            {
                active.Add(value);
                frames.Add(new JsonDomWriteFrame(value, null));
            }

            return result;
        }

        if (value.Kind == JsonValueKind.Object)
        {
            if (active.Contains(value))
            {
                throw CreateModelCycleFailure();
            }

            global::System.Collections.Generic.List<JsonMember> members =
                GetSerializableMembers(value);
            JsonOperationResult result = WriteStartObject();
            if (result.Success)
            {
                active.Add(value);
                frames.Add(new JsonDomWriteFrame(value, members));
            }

            return result;
        }

        throw CreateModelFailure(
            "A JSON value kind is uninitialized or undefined.",
            value.Kind.ToString());
    }

    global::System.Collections.Generic.List<JsonMember> GetSerializableMembers(
        JsonValue value)
    {
        global::System.Collections.Generic.List<JsonMember> source =
            value.ObjectMembers
            ?? throw CreateModelFailure(
                "An object value requires a member list.",
                "Object");
        if (duplicateKeyPolicy == JsonDuplicateKeyPolicy.Preserve)
        {
            return propertySorter.SortProperties(source);
        }

        if (duplicateKeyPolicy == JsonDuplicateKeyPolicy.Reject)
        {
            RejectDuplicateMembers(source);
            return propertySorter.SortProperties(source);
        }

        global::System.Collections.Generic.List<JsonMember> selected =
            new global::System.Collections.Generic.List<JsonMember>();
        if (duplicateKeyPolicy == JsonDuplicateKeyPolicy.KeepFirst)
        {
            global::System.Collections.Generic.HashSet<string> names =
                new global::System.Collections.Generic.HashSet<string>(
                    global::System.StringComparer.Ordinal);
            int index = 0;
            while (index < source.Count)
            {
                JsonMember member = GetNamedMember(source, index);
                string name = member.Name
                    ?? throw new global::System.InvalidOperationException(
                        "A named member requires a name.");
                if (names.Add(name))
                {
                    selected.Add(member);
                }

                index++;
            }
        }
        else
        {
            global::System.Collections.Generic.Dictionary<string, int> last =
                new global::System.Collections.Generic.Dictionary<string, int>(
                    global::System.StringComparer.Ordinal);
            int index = 0;
            while (index < source.Count)
            {
                JsonMember member = GetNamedMember(source, index);
                string name = member.Name
                    ?? throw new global::System.InvalidOperationException(
                        "A named member requires a name.");
                last[name] = index;
                index++;
            }

            index = 0;
            while (index < source.Count)
            {
                JsonMember member = GetNamedMember(source, index);
                string name = member.Name
                    ?? throw new global::System.InvalidOperationException(
                        "A named member requires a name.");
                if (last[name] == index)
                {
                    selected.Add(member);
                }

                index++;
            }
        }

        return propertySorter.SortProperties(selected);
    }

    void RejectDuplicateMembers(
        global::System.Collections.Generic.List<JsonMember> members)
    {
        global::System.Collections.Generic.HashSet<string> names =
            new global::System.Collections.Generic.HashSet<string>(
                global::System.StringComparer.Ordinal);
        int index = 0;
        while (index < members.Count)
        {
            JsonMember member = GetNamedMember(members, index);
            string name = member.Name
                ?? throw new global::System.InvalidOperationException(
                    "A named member requires a name.");
            if (!names.Add(name))
            {
                JsonDiagnostic diagnostic = new JsonDiagnostic();
                diagnostic.Code = JsonDiagnosticCode.DuplicatePropertyName;
                diagnostic.Message =
                    "The object contains a duplicate decoded property name.";
                diagnostic.ActualToken = name;
                throw new JsonWriteException(diagnostic);
            }

            index++;
        }
    }

    JsonMember GetNamedMember(
        global::System.Collections.Generic.List<JsonMember> members,
        int index)
    {
        JsonMember? member = members[index];
        if (member == null || member.Name == null)
        {
            throw CreateModelFailure(
                "An object member requires a decoded name.",
                "ObjectMember");
        }

        return member;
    }

    void ValidateRejectedDuplicates(JsonValue root)
    {
        if (duplicateKeyPolicy != JsonDuplicateKeyPolicy.Reject)
        {
            return;
        }

        global::System.Collections.Generic.List<JsonValue> pending =
            new global::System.Collections.Generic.List<JsonValue>();
        pending.Add(root);
        while (pending.Count > 0)
        {
            JsonValue value = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (value.Kind == JsonValueKind.Array)
            {
                global::System.Collections.Generic.List<JsonValue> items =
                    value.ArrayItems
                    ?? throw new global::System.InvalidOperationException(
                        "A validated array requires items.");
                int itemIndex = 0;
                while (itemIndex < items.Count)
                {
                    pending.Add(items[itemIndex]);
                    itemIndex++;
                }

                continue;
            }

            if (value.Kind != JsonValueKind.Object)
            {
                continue;
            }

            global::System.Collections.Generic.List<JsonMember> members =
                value.ObjectMembers
                ?? throw new global::System.InvalidOperationException(
                    "A validated object requires members.");
            global::System.Collections.Generic.HashSet<string> names =
                new global::System.Collections.Generic.HashSet<string>(
                    global::System.StringComparer.Ordinal);
            int memberIndex = 0;
            while (memberIndex < members.Count)
            {
                JsonMember member = members[memberIndex];
                string name = member.Name
                    ?? throw new global::System.InvalidOperationException(
                        "A validated member requires a name.");
                if (!names.Add(name))
                {
                    JsonDiagnostic diagnostic = new JsonDiagnostic();
                    diagnostic.Code = JsonDiagnosticCode.DuplicatePropertyName;
                    diagnostic.Message =
                        "The object contains a duplicate decoded property name.";
                    diagnostic.ActualToken = name;
                    throw new JsonWriteException(diagnostic);
                }

                pending.Add(member.Value
                    ?? throw new global::System.InvalidOperationException(
                        "A validated member requires a value."));
                memberIndex++;
            }
        }
    }

    bool AccumulateModelResult(JsonOperationResult result)
    {
        compositeBytesWritten += result.BytesWritten;
        if (!result.Success)
        {
            result.BytesWritten = compositeBytesWritten;
            return false;
        }

        return true;
    }

    JsonOperationResult CreateRawFailure(JsonOperationResult validation)
    {
        JsonDiagnostic source = validation.Diagnostic
            ?? CreateDiagnostic(
                JsonDiagnosticCode.InvalidRawValue,
                "The raw input is not exactly one complete JSON value.",
                "RawValue");
        if (IsLimitCode(source.Code))
        {
            return validation;
        }

        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.InvalidRawValue;
        diagnostic.Message =
            "The raw input is not exactly one complete JSON value: " +
            source.Message;
        diagnostic.Range = source.Range;
        diagnostic.Pointer = source.Pointer;
        diagnostic.Depth = source.Depth;
        diagnostic.ExpectedTokens = source.ExpectedTokens;
        diagnostic.ActualToken = source.ActualToken;
        diagnostic.SourceExcerpt = source.SourceExcerpt;
        JsonOperationResult result = new JsonOperationResult();
        result.Success = false;
        result.BytesConsumed = validation.BytesConsumed;
        result.Diagnostic = diagnostic;
        return result;
    }

    bool IsLimitCode(JsonDiagnosticCode code)
    {
        return code >= JsonDiagnosticCode.DocumentByteLimitExceeded &&
            code <= JsonDiagnosticCode.OutputByteLimitExceeded;
    }

    JsonOperationResult CreateFailureFromException(
        global::System.Exception failure)
    {
        JsonWriteException? writeFailure = failure as JsonWriteException;
        if (writeFailure != null)
        {
            return CreateFailure(writeFailure.Diagnostic);
        }

        JsonLimitException? limitFailure = failure as JsonLimitException;
        if (limitFailure != null)
        {
            return CreateFailure(limitFailure.Diagnostic);
        }

        throw failure;
    }

    JsonOperationResult CreateSuccess(long bytesWritten)
    {
        JsonOperationResult result = new JsonOperationResult();
        result.Success = true;
        result.BytesWritten = bytesWritten;
        return result;
    }

    JsonOperationResult CreateFailure(JsonDiagnostic diagnostic)
    {
        JsonOperationResult result = new JsonOperationResult();
        result.Success = false;
        result.Diagnostic = diagnostic;
        return result;
    }

    JsonWriteException CreateModelFailure(string message, string actualToken)
    {
        return new JsonWriteException(CreateDiagnostic(
            JsonDiagnosticCode.InvalidModel,
            message,
            actualToken));
    }

    JsonWriteException CreateModelCycleFailure()
    {
        return new JsonWriteException(CreateDiagnostic(
            JsonDiagnosticCode.ModelCycle,
            "The JSON model contains a reference cycle.",
            "Container"));
    }

    JsonDiagnostic CreateDiagnostic(
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

    JsonReaderOptions CreateRawReaderOptions(
        JsonWriterOptions options,
        JsonLimits limits)
    {
        JsonReaderOptions reader = new JsonReaderOptions();
        reader.ConformanceProfile = JsonConformanceProfile.SyntaxCompatible;
        reader.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Preserve;
        reader.InputMode = JsonInputMode.SingleValue;
        reader.UnicodePolicy = options.UnicodePolicy;
        reader.BomPolicy = JsonBomPolicy.Reject;
        reader.Limits = limits;
        return JsonOptionNormalizer.NormalizeReaderOptions(reader);
    }
}
