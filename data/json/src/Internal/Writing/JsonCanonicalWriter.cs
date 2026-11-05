namespace json;

internal class JsonCanonicalWriter
{
    JsonLimits limits;
    JsonByteSink sink;
    JsonStringEncoder stringEncoder;
    JsonCanonicalNumberFormatter numberFormatter;
    JsonPropertySorter propertySorter;

    internal JsonCanonicalWriter(
        JsonLimits? limits,
        JsonWriterTarget target)
    {
        this.limits = JsonOptionNormalizer.NormalizeLimits(limits);
        JsonWriterOptions options = new JsonWriterOptions();
        options.PropertyOrder = JsonPropertyOrder.Jcs;
        options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        options.Limits = this.limits;
        sink = new JsonByteSink(target, this.limits.MaximumOutputBytes);
        stringEncoder = new JsonStringEncoder(options);
        numberFormatter = new JsonCanonicalNumberFormatter(this.limits);
        propertySorter = new JsonPropertySorter(options);
    }

    internal JsonOperationResult WriteDocument(JsonDocument? document)
    {
        JsonModelValidator validator = new JsonModelValidator(limits);
        JsonOperationResult modelValidation =
            validator.ValidateJcsDocumentModel(document);
        if (!modelValidation.Success)
        {
            return modelValidation;
        }

        JsonValue root = document?.Root
            ?? throw new global::System.InvalidOperationException(
                "A validated document requires a root value.");
        JsonOperationResult constraints = ValidateJcsConstraints(root);
        if (!constraints.Success)
        {
            return constraints;
        }

        JsonExceptionCapture capture = new JsonExceptionCapture(
            () => ExecuteWrite(root));
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            return CreateFailureFromException(failure);
        }

        return sink.FlushSink();
    }

    JsonOperationResult ValidateJcsConstraints(JsonValue root)
    {
        global::System.Collections.Generic.List<JsonValue> pending =
            new global::System.Collections.Generic.List<JsonValue>();
        pending.Add(root);
        while (pending.Count > 0)
        {
            JsonValue value = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (value.Kind == JsonValueKind.Number)
            {
                JsonNumber number = value.Number
                    ?? throw new global::System.InvalidOperationException(
                        "A validated number requires a payload.");
                JsonDoubleResult conversion =
                    JsonNumberConversion.TryConvertToDouble(number);
                if (!conversion.Success)
                {
                    JsonDiagnostic diagnostic = conversion.Diagnostic
                        ?? throw new global::System.InvalidOperationException(
                            "A failed number conversion requires a diagnostic.");
                    return CreateFailure(diagnostic);
                }

                continue;
            }

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
                    JsonDiagnostic duplicate = new JsonDiagnostic();
                    duplicate.Code =
                        JsonDiagnosticCode.DuplicatePropertyName;
                    duplicate.Message =
                        "JCS canonicalization forbids duplicate property names.";
                    duplicate.ActualToken = name;
                    return CreateFailure(duplicate);
                }

                pending.Add(member.Value
                    ?? throw new global::System.InvalidOperationException(
                        "A validated member requires a value."));
                memberIndex++;
            }
        }

        return CreateSuccess();
    }

    void ExecuteWrite(JsonValue root)
    {
        global::System.Collections.Generic.List<JsonDomWriteFrame> frames =
            new global::System.Collections.Generic.List<JsonDomWriteFrame>();
        WriteValueNode(root, frames);
        while (frames.Count > 0)
        {
            JsonDomWriteFrame frame = frames[frames.Count - 1];
            if (frame.Value.Kind == JsonValueKind.Array)
            {
                global::System.Collections.Generic.List<JsonValue> items =
                    frame.Value.ArrayItems
                    ?? throw new global::System.InvalidOperationException(
                        "A validated array requires items.");
                if (frame.NextIndex >= items.Count)
                {
                    sink.WriteByte(JsonSyntaxBytes.EndArray);
                    frames.RemoveAt(frames.Count - 1);
                    continue;
                }

                if (frame.NextIndex > 0)
                {
                    sink.WriteByte(JsonSyntaxBytes.ValueSeparator);
                }

                JsonValue item = items[frame.NextIndex];
                frame.NextIndex++;
                WriteValueNode(item, frames);
                continue;
            }

            global::System.Collections.Generic.List<JsonMember> members =
                frame.Members
                ?? throw new global::System.InvalidOperationException(
                    "A canonical object frame requires sorted members.");
            if (frame.NextIndex >= members.Count)
            {
                sink.WriteByte(JsonSyntaxBytes.EndObject);
                frames.RemoveAt(frames.Count - 1);
                continue;
            }

            if (frame.NextIndex > 0)
            {
                sink.WriteByte(JsonSyntaxBytes.ValueSeparator);
            }

            JsonMember member = members[frame.NextIndex];
            frame.NextIndex++;
            string name = member.Name
                ?? throw new global::System.InvalidOperationException(
                    "A validated member requires a name.");
            sink.WriteBytes(stringEncoder.EncodeJcsString(name));
            sink.WriteByte(JsonSyntaxBytes.NameSeparator);
            WriteValueNode(
                member.Value
                    ?? throw new global::System.InvalidOperationException(
                        "A validated member requires a value."),
                frames);
        }
    }

    void WriteValueNode(
        JsonValue value,
        global::System.Collections.Generic.List<JsonDomWriteFrame> frames)
    {
        if (value.Kind == JsonValueKind.Null)
        {
            sink.WriteBytes(new byte[] { 0x6E, 0x75, 0x6C, 0x6C });
            return;
        }

        if (value.Kind == JsonValueKind.Boolean)
        {
            sink.WriteBytes(value.BooleanValue
                ? new byte[] { 0x74, 0x72, 0x75, 0x65 }
                : new byte[] { 0x66, 0x61, 0x6C, 0x73, 0x65 });
            return;
        }

        if (value.Kind == JsonValueKind.Number)
        {
            JsonNumber number = value.Number
                ?? throw new global::System.InvalidOperationException(
                    "A validated number requires a payload.");
            double converted = JsonNumberConversion.ConvertToDouble(number);
            sink.WriteBytes(numberFormatter.FormatDouble(converted));
            return;
        }

        if (value.Kind == JsonValueKind.String)
        {
            string text = value.StringValue
                ?? throw new global::System.InvalidOperationException(
                    "A validated string requires a payload.");
            sink.WriteBytes(stringEncoder.EncodeJcsString(text));
            return;
        }

        if (value.Kind == JsonValueKind.Array)
        {
            sink.WriteByte(JsonSyntaxBytes.BeginArray);
            frames.Add(new JsonDomWriteFrame(value, null));
            return;
        }

        if (value.Kind == JsonValueKind.Object)
        {
            global::System.Collections.Generic.List<JsonMember> members =
                value.ObjectMembers
                ?? throw new global::System.InvalidOperationException(
                    "A validated object requires members.");
            global::System.Collections.Generic.List<JsonMember> sorted =
                propertySorter.SortProperties(members);
            sink.WriteByte(JsonSyntaxBytes.BeginObject);
            frames.Add(new JsonDomWriteFrame(value, sorted));
            return;
        }

        throw new global::System.InvalidOperationException(
            "A validated value requires a JSON kind.");
    }

    JsonOperationResult CreateFailureFromException(
        global::System.Exception failure)
    {
        JsonLimitException? limitFailure = failure as JsonLimitException;
        if (limitFailure != null)
        {
            return CreateFailure(limitFailure.Diagnostic);
        }

        JsonWriteException? writeFailure = failure as JsonWriteException;
        if (writeFailure != null)
        {
            return CreateFailure(writeFailure.Diagnostic);
        }

        JsonParseException? parseFailure = failure as JsonParseException;
        if (parseFailure != null)
        {
            return CreateFailure(parseFailure.Diagnostic);
        }

        throw failure;
    }

    JsonOperationResult CreateSuccess()
    {
        JsonOperationResult result = new JsonOperationResult();
        result.Success = true;
        return result;
    }

    JsonOperationResult CreateFailure(JsonDiagnostic diagnostic)
    {
        JsonOperationResult result = new JsonOperationResult();
        result.Success = false;
        result.BytesWritten = sink.GetBytesCommitted();
        result.Diagnostic = diagnostic;
        return result;
    }
}
