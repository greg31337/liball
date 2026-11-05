using System;
using System.Globalization;
using System.Text;

namespace http;

internal class Http1ResponseSerializationPlan
{
    internal HttpOperationStatus Status;
    internal string Version = "";
    internal byte[] ReasonPhrase = Array.Empty<byte>();
    internal string ChunkExtension = "";
    internal HttpHeaders Headers = new HttpHeaders();
    internal Http1FramingDecision FramingDecision = new Http1FramingDecision();
    internal Http1ConnectionDecision? ConnectionDecision;
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal static class Http1ResponseSerializer
{
    internal static Http1SerializationResult SerializeResponse(
        HttpResponse response,
        Http1SerializationOptions options)
    {
        Http1SerializationResult result = CreateResponseResult(options);
        Http1ResponseSerializationPlan plan = CreateResponsePlan(response, options);
        if (plan.Status != HttpOperationStatus.Done)
        {
            return RejectResponseResult(result, plan.ErrorCode, plan.ErrorMessage);
        }

        long required = CalculateResponseSize(response, options, plan);
        if (required < 0 || required > int.MaxValue)
        {
            return RejectResponseResult(
                result,
                "http1-serialization-size-overflow",
                "The serialized HTTP/1 response exceeds the supported output size.");
        }

        result.Bytes = new byte[(int)required];
        result.BytesWritten = WriteResponseToSpan(response, options, plan, result.Bytes);
        result.RequiredSize = result.BytesWritten;
        result.FramingDecision = plan.FramingDecision;
        result.ConnectionDecision = plan.ConnectionDecision;
        result.Status = HttpOperationStatus.Done;
        return result;
    }

    internal static Http1SerializationResult WriteResponse(
        HttpResponse response,
        Http1SerializationOptions options,
        Span<byte> output)
    {
        Http1SerializationResult result = CreateResponseResult(options);
        Http1ResponseSerializationPlan plan = CreateResponsePlan(response, options);
        if (plan.Status != HttpOperationStatus.Done)
        {
            return RejectResponseResult(result, plan.ErrorCode, plan.ErrorMessage);
        }

        long required = CalculateResponseSize(response, options, plan);
        if (required < 0 || required > int.MaxValue)
        {
            return RejectResponseResult(
                result,
                "http1-serialization-size-overflow",
                "The serialized HTTP/1 response exceeds the supported output size.");
        }

        result.RequiredSize = (int)required;
        result.FramingDecision = plan.FramingDecision;
        result.ConnectionDecision = plan.ConnectionDecision;
        if (output.Length < result.RequiredSize)
        {
            result.Status = HttpOperationStatus.NeedOutput;
            return result;
        }

        result.BytesWritten = WriteResponseToSpan(response, options, plan, output);
        result.Status = HttpOperationStatus.Done;
        return result;
    }

    static Http1ResponseSerializationPlan CreateResponsePlan(
        HttpResponse response,
        Http1SerializationOptions options)
    {
        Http1ResponseSerializationPlan plan = new Http1ResponseSerializationPlan();
        if (!HasValidResponseStructure(response, options))
        {
            return RejectResponsePlan(
                plan,
                "invalid-http1-serialization-input",
                "The HTTP/1 response or serialization options are incomplete.");
        }

        if (!HasValidResponseLimits(options.Limits))
        {
            return RejectResponsePlan(
                plan,
                "invalid-http1-serialization-limits",
                "The HTTP/1 serialization limits cannot be negative.");
        }

        if (response.StatusCode < 100 || response.StatusCode > 999)
        {
            return RejectResponsePlan(
                plan,
                "invalid-http1-status-code",
                "An HTTP/1 response status code must contain three digits.");
        }

        bool informational = response.StatusCode < 200;
        if (response.IsInformational != informational)
        {
            return RejectResponsePlan(
                plan,
                "inconsistent-http1-informational-status",
                "The informational response flag must agree with the status code.");
        }

        string version = ResolveResponseVersion(response.ProtocolVersion);
        if (version.Length == 0)
        {
            return RejectResponsePlan(
                plan,
                "invalid-http1-version",
                "Only HTTP/1.0 and HTTP/1.1 responses can be serialized.");
        }

        byte[] reason = ResolveReasonPhrase(response);
        if (!IsValidReasonPhrase(reason))
        {
            return RejectResponsePlan(
                plan,
                "invalid-http1-reason-phrase",
                "The HTTP/1 reason phrase contains a prohibited control octet.");
        }

        long startLineSize = version.Length + 1L + 3 + 1 + reason.Length;
        if (startLineSize > options.Limits.MaximumStartLineSize)
        {
            return RejectResponsePlan(
                plan,
                "http1-start-line-limit-exceeded",
                "The HTTP/1 status line exceeds the configured limit.");
        }

        if (options.ResponseRequestMethod != null &&
            !HeaderStore.IsValidName(options.ResponseRequestMethod))
        {
            return RejectResponsePlan(
                plan,
                "invalid-http1-response-request-method",
                "The associated request method must be an ASCII token.");
        }

        if (response.Content.Length > options.Limits.MaximumContentSize)
        {
            return RejectResponsePlan(
                plan,
                "http1-content-limit-exceeded",
                "The HTTP/1 response content exceeds the configured limit.");
        }

        if (!CopyAndValidateResponseFields(response, plan))
        {
            return plan;
        }

        if (!ValidateResponseFieldLimits(plan.Headers, options.Limits, plan))
        {
            return plan;
        }

        string? unsafeConnectionOption =
            Http1FramingNormalizer.FindUnsafeConnectionOption(plan.Headers);
        if (unsafeConnectionOption != null)
        {
            return RejectResponsePlan(
                plan,
                "http1-connection-framing-option",
                "Connection cannot nominate a message framing field.");
        }

        bool isHead = options.ResponseRequestMethod == HttpMethods.Head;
        bool isConnect = options.ResponseRequestMethod == HttpMethods.Connect &&
            response.StatusCode >= 200 && response.StatusCode <= 299;
        bool isStrictNoBody = informational ||
            response.StatusCode == HttpStatusCodes.NoContent;
        bool isMetadataOnly = isHead ||
            response.StatusCode == HttpStatusCodes.NotModified;
        bool statusDisallowsContent = isStrictNoBody || isMetadataOnly ||
            response.StatusCode == HttpStatusCodes.ResetContent;

        if ((statusDisallowsContent || isConnect) && response.Content.Length != 0)
        {
            return RejectResponsePlan(
                plan,
                "http1-response-content-not-allowed",
                "The response status or request method does not permit content bytes.");
        }

        bool hasContentLength = HasResponseField(
            response.Headers,
            HttpHeaderNames.ContentLength);
        bool hasTransferEncoding = HasResponseField(
            response.Headers,
            HttpHeaderNames.TransferEncoding);
        bool hasConnectionClose = HasResponseConnectionClose(response.Headers);
        if ((isStrictNoBody || isConnect) &&
            (hasContentLength || hasTransferEncoding))
        {
            return RejectResponsePlan(
                plan,
                "http1-response-framing-field-not-allowed",
                "This response cannot contain Content-Length or Transfer-Encoding.");
        }

        if (isConnect && hasConnectionClose)
        {
            return RejectResponsePlan(
                plan,
                "http1-connect-close-conflict",
                "A successful CONNECT response cannot both establish and close a tunnel.");
        }

        Http1ParserOptions parserOptions = new Http1ParserOptions();
        parserOptions.Limits = options.Limits;
        parserOptions.ResponseRequestMethod = options.ResponseRequestMethod;
        Http1FramingResult existing = Http1Framing.DetermineResponse(
            response,
            parserOptions);
        if (!existing.Decision.IsValid)
        {
            plan.FramingDecision = existing.Decision;
            return RejectResponsePlan(
                plan,
                existing.ErrorCode ?? "invalid-http1-response-framing",
                existing.ErrorMessage ?? "The HTTP/1 response framing is invalid.");
        }

        if (isMetadataOnly && (hasContentLength || hasTransferEncoding))
        {
            Http1FramingResult metadata = ValidateMetadataFraming(response, options);
            if (!metadata.Decision.IsValid)
            {
                plan.FramingDecision = metadata.Decision;
                return RejectResponsePlan(
                    plan,
                    metadata.ErrorCode ?? "invalid-http1-response-framing",
                    metadata.ErrorMessage ?? "The response framing metadata is invalid.");
            }
        }

        plan.Version = version;
        plan.ReasonPhrase = reason;
        plan.FramingDecision = SelectResponseFraming(
            response,
            options,
            existing.Decision,
            hasContentLength,
            hasTransferEncoding,
            isMetadataOnly,
            isConnect,
            plan);
        if (plan.Status == HttpOperationStatus.Rejected)
        {
            return plan;
        }

        if (options.HasBodyFraming && !plan.FramingDecision.HasNoBody &&
            !plan.FramingDecision.IsTunnel)
        {
            Http1TrailerValidationResult declarations =
                Http1TrailerValidator.ValidateDeclarations(
                    plan.Headers,
                    options.Limits);
            if (declarations.Status != HttpOperationStatus.Done)
            {
                return RejectResponsePlan(
                    plan,
                    declarations.ErrorCode ?? "invalid-http1-trailer-declaration",
                    declarations.ErrorMessage ??
                        "The HTTP/1 Trailer field declaration is invalid.");
            }

            Http1FramingNormalizer.NormalizeSelectedFraming(
                plan.Headers,
                plan.FramingDecision.BodyFraming,
                response.Content.Length);
        }

        AddGeneratedResponseFields(
            response,
            options,
            hasContentLength,
            hasTransferEncoding,
            plan);
        Http1ChunkSerializationResult chunks =
            Http1ChunkSerializer.PrepareChunkedMessage(
                plan.Headers,
                response.Trailers,
                plan.FramingDecision,
                options);
        if (chunks.Status != HttpOperationStatus.Done)
        {
            return RejectResponsePlan(
                plan,
                chunks.ErrorCode ?? "invalid-http1-chunked-serialization",
                chunks.ErrorMessage ??
                    "The HTTP/1 response chunked content cannot be serialized.");
        }

        plan.ChunkExtension = chunks.ChunkExtension;
        if (options.CloseConnection)
        {
            if (plan.FramingDecision.IsTunnel)
            {
                return RejectResponsePlan(
                    plan,
                    "http1-connect-close-conflict",
                    "A successful CONNECT response cannot both establish and close a tunnel.");
            }

            if (!hasConnectionClose)
            {
                AddResponseAsciiField(plan.Headers, HttpHeaderNames.Connection, "close");
            }
        }
        else if (plan.FramingDecision.BodyFraming == Http1BodyFraming.CloseDelimited &&
            !hasConnectionClose)
        {
            AddResponseAsciiField(plan.Headers, HttpHeaderNames.Connection, "close");
        }

        if (!ValidateResponseFieldLimits(plan.Headers, options.Limits, plan))
        {
            return plan;
        }

        Http1ConnectionResult connection = Http1Connection.Determine(
            plan.Headers,
            response.ProtocolVersion,
            plan.FramingDecision,
            false,
            true,
            response.StatusCode,
            options.Limits);
        if (connection.Status != HttpOperationStatus.Done)
        {
            return RejectResponsePlan(
                plan,
                connection.ErrorCode ?? "invalid-http1-connection",
                connection.ErrorMessage ?? "The HTTP/1 connection fields are invalid.");
        }

        if (response.StatusCode == HttpStatusCodes.SwitchingProtocols &&
            !connection.Decision.IsUpgrade)
        {
            return RejectResponsePlan(
                plan,
                "invalid-http1-switching-protocols-response",
                "A 101 response requires valid Connection and Upgrade fields.");
        }

        plan.ConnectionDecision = connection.Decision;
        plan.Status = HttpOperationStatus.Done;
        return plan;
    }

    static bool HasValidResponseStructure(
        HttpResponse response,
        Http1SerializationOptions options)
    {
        return response != null && options != null && options.Limits != null &&
            response.ReasonPhrase != null && response.Headers != null &&
            response.Headers.Fields != null && response.Content != null &&
            response.Trailers != null && response.Trailers.Fields != null;
    }

    static bool HasValidResponseLimits(HttpLimits limits)
    {
        return limits.MaximumStartLineSize >= 0 &&
            limits.MaximumFieldLineSize >= 0 &&
            limits.MaximumFieldSectionSize >= 0 &&
            limits.MaximumFieldCount >= 0 &&
            limits.MaximumFieldValueSize >= 0 &&
            limits.MaximumTrailerSectionSize >= 0 &&
            limits.MaximumTrailerCount >= 0 &&
            limits.MaximumContentSize >= 0 &&
            limits.MaximumChunkSize >= 0 &&
            limits.MaximumListMemberCount >= 0 &&
            limits.MaximumParameterCount >= 0;
    }

    static string ResolveResponseVersion(HttpProtocolVersion version)
    {
        if (version == HttpProtocolVersion.Http10)
        {
            return HttpProtocolNames.Http10;
        }

        if (version == HttpProtocolVersion.Http11)
        {
            return HttpProtocolNames.Http11;
        }

        return "";
    }

    static byte[] ResolveReasonPhrase(HttpResponse response)
    {
        if (response.HasReasonPhrase)
        {
            return response.ReasonPhrase;
        }

        HttpStatusMetadata metadata = StatusSemantics.Describe(response.StatusCode);
        if (metadata.ReasonPhrase == null)
        {
            return Array.Empty<byte>();
        }

        return Encoding.ASCII.GetBytes(metadata.ReasonPhrase);
    }

    static bool IsValidReasonPhrase(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            byte octet = value[index];
            if (octet != 0x09 && (octet < 0x20 || octet == 0x7f))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool CopyAndValidateResponseFields(
        HttpResponse response,
        Http1ResponseSerializationPlan plan)
    {
        int fieldIndex = 0;
        while (fieldIndex < response.Headers.Fields.Count)
        {
            HttpHeaderField field = response.Headers.Fields[fieldIndex];
            if (field == null || !HeaderStore.IsValidName(field.Name) ||
                !HeaderStore.IsValidValue(field.Value))
            {
                RejectResponsePlan(
                    plan,
                    "invalid-http1-field",
                    "The HTTP/1 response contains an invalid field name or value.");
                return false;
            }

            plan.Headers.Fields.Add(field);
            fieldIndex = fieldIndex + 1;
        }

        return true;
    }

    static Http1FramingResult ValidateMetadataFraming(
        HttpResponse response,
        Http1SerializationOptions options)
    {
        HttpResponse ordinary = new HttpResponse();
        ordinary.ProtocolVersion = response.ProtocolVersion;
        ordinary.StatusCode = HttpStatusCodes.Ok;
        ordinary.Headers = response.Headers;
        Http1ParserOptions parserOptions = new Http1ParserOptions();
        parserOptions.Limits = options.Limits;
        return Http1Framing.DetermineResponse(ordinary, parserOptions);
    }

    static Http1FramingDecision SelectResponseFraming(
        HttpResponse response,
        Http1SerializationOptions options,
        Http1FramingDecision existing,
        bool hasContentLength,
        bool hasTransferEncoding,
        bool isMetadataOnly,
        bool isConnect,
        Http1ResponseSerializationPlan plan)
    {
        if (isConnect)
        {
            if (options.HasBodyFraming &&
                options.BodyFraming != Http1BodyFraming.Tunnel)
            {
                RejectResponsePlan(
                    plan,
                    "conflicting-http1-response-framing",
                    "A successful CONNECT response changes the connection to a tunnel.");
            }

            return existing;
        }

        if (existing.HasNoBody)
        {
            if (options.HasBodyFraming && options.BodyFraming != Http1BodyFraming.None)
            {
                RejectResponsePlan(
                    plan,
                    "conflicting-http1-response-framing",
                    "The selected framing conflicts with response no-body semantics.");
            }

            return existing;
        }

        if (isMetadataOnly)
        {
            return existing;
        }

        if (existing.BodyFraming == Http1BodyFraming.ContentLength &&
            options.VerifyContentLength &&
            existing.DeclaredLength != response.Content.Length)
        {
            RejectResponsePlan(
                plan,
                "mismatched-http1-content-length",
                "Content-Length does not match the response content size.");
            return existing;
        }

        Http1BodyFraming selected = existing.BodyFraming;
        if (hasTransferEncoding && !HasOnlyChunkedCoding(existing))
        {
            RejectResponsePlan(
                plan,
                "unsupported-http1-response-transfer-coding",
                "The response uses a transfer coding this serializer cannot apply.");
            return existing;
        }

        if (options.HasBodyFraming)
        {
            selected = options.BodyFraming;
            if (selected != Http1BodyFraming.ContentLength &&
                selected != Http1BodyFraming.Chunked &&
                selected != Http1BodyFraming.CloseDelimited)
            {
                RejectResponsePlan(
                    plan,
                    "unsupported-http1-response-framing",
                    "The selected body framing cannot be used for this HTTP/1 response.");
                return existing;
            }

        }
        else if (existing.BodyFraming == Http1BodyFraming.CloseDelimited &&
            !hasTransferEncoding && response.Trailers.Fields.Count != 0)
        {
            selected = Http1BodyFraming.Chunked;
        }
        else if (existing.BodyFraming == Http1BodyFraming.CloseDelimited &&
            !hasTransferEncoding && options.AddContentLength)
        {
            selected = Http1BodyFraming.ContentLength;
        }

        if (selected == Http1BodyFraming.ContentLength && !hasContentLength &&
            !options.AddContentLength)
        {
            RejectResponsePlan(
                plan,
                "missing-http1-content-length",
                "Content-Length generation is disabled for length-framed content.");
            return existing;
        }

        if (selected == Http1BodyFraming.Chunked)
        {
            if (options.ChunkSize <= 0)
            {
                RejectResponsePlan(
                    plan,
                    "invalid-http1-chunk-size",
                    "The configured HTTP/1 chunk size must be positive.");
                return existing;
            }

            if (response.ProtocolVersion != HttpProtocolVersion.Http11)
            {
                RejectResponsePlan(
                    plan,
                    "http1-chunked-requires-http11",
                    "Chunked transfer coding cannot be used with HTTP/1.0.");
                return existing;
            }

            if (options.ChunkSize > options.Limits.MaximumChunkSize)
            {
                RejectResponsePlan(
                    plan,
                    "http1-chunk-limit-exceeded",
                    "The configured HTTP/1 chunk size exceeds the configured limit.");
                return existing;
            }
        }

        if (selected == existing.BodyFraming)
        {
            return existing;
        }

        return CreateResponseFraming(selected, response.Content.Length);
    }

    static bool HasOnlyChunkedCoding(Http1FramingDecision framing)
    {
        return framing.TransferCodings.Count == 1 &&
            Ascii.EqualsStringsIgnoreCase(
                framing.TransferCodings[0],
                HttpTransferCodings.Chunked);
    }

    static Http1FramingDecision CreateResponseFraming(
        Http1BodyFraming framing,
        int contentLength)
    {
        Http1FramingDecision result = new Http1FramingDecision();
        result.BodyFraming = framing;
        result.IsValid = true;
        result.IsSelfDelimited = framing != Http1BodyFraming.CloseDelimited;
        result.HasDeclaredLength = framing == Http1BodyFraming.ContentLength;
        result.DeclaredLength = contentLength;
        if (framing == Http1BodyFraming.Chunked)
        {
            result.TransferCodings.Add(HttpTransferCodings.Chunked);
            result.Reason = "The response uses chunked transfer coding.";
        }
        else if (framing == Http1BodyFraming.CloseDelimited)
        {
            result.Reason = "The response content is delimited by connection closure.";
        }
        else
        {
            result.Reason = "The response uses an exact content length.";
        }

        return result;
    }

    static void AddGeneratedResponseFields(
        HttpResponse response,
        Http1SerializationOptions options,
        bool hasContentLength,
        bool hasTransferEncoding,
        Http1ResponseSerializationPlan plan)
    {
        if (options.HasBodyFraming)
        {
            return;
        }

        if (plan.FramingDecision.BodyFraming == Http1BodyFraming.ContentLength &&
            !hasContentLength && options.AddContentLength)
        {
            AddResponseAsciiField(
                plan.Headers,
                HttpHeaderNames.ContentLength,
                response.Content.Length.ToString(CultureInfo.InvariantCulture));
        }

        if (plan.FramingDecision.BodyFraming == Http1BodyFraming.Chunked &&
            !hasTransferEncoding)
        {
            AddResponseAsciiField(
                plan.Headers,
                HttpHeaderNames.TransferEncoding,
                HttpTransferCodings.Chunked);
        }
    }

    static bool HasResponseField(HttpHeaders headers, string name)
    {
        int index = 0;
        while (index < headers.Fields.Count)
        {
            if (Ascii.EqualsStringsIgnoreCase(headers.Fields[index].Name, name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool HasResponseConnectionClose(HttpHeaders headers)
    {
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (Ascii.EqualsStringsIgnoreCase(field.Name, HttpHeaderNames.Connection))
            {
                FieldValueParseResult parsed = FieldValueParser.ParseConnection(
                    field.Value,
                    int.MaxValue,
                    int.MaxValue);
                int valueIndex = 0;
                while (valueIndex < parsed.Values.Count)
                {
                    string value = Encoding.ASCII.GetString(parsed.Values[valueIndex]);
                    if (Ascii.EqualsStringsIgnoreCase(value, "close"))
                    {
                        return true;
                    }

                    valueIndex = valueIndex + 1;
                }
            }

            index = index + 1;
        }

        return false;
    }

    static bool ValidateResponseFieldLimits(
        HttpHeaders headers,
        HttpLimits limits,
        Http1ResponseSerializationPlan plan)
    {
        if (headers.Fields.Count > limits.MaximumFieldCount)
        {
            RejectResponsePlan(
                plan,
                "http1-field-count-limit-exceeded",
                "The HTTP/1 response field count exceeds the configured limit.");
            return false;
        }

        long sectionSize = 0;
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            long lineSize = (long)field.Name.Length + 2 + field.Value.Length;
            if (field.Value.Length > limits.MaximumFieldValueSize)
            {
                RejectResponsePlan(
                    plan,
                    "http1-field-value-limit-exceeded",
                    "An HTTP/1 response field value exceeds the configured limit.");
                return false;
            }

            if (lineSize > limits.MaximumFieldLineSize)
            {
                RejectResponsePlan(
                    plan,
                    "http1-field-line-limit-exceeded",
                    "An HTTP/1 response field line exceeds the configured limit.");
                return false;
            }

            sectionSize = sectionSize + lineSize;
            if (sectionSize > limits.MaximumFieldSectionSize)
            {
                RejectResponsePlan(
                    plan,
                    "http1-field-section-limit-exceeded",
                    "The HTTP/1 response field section exceeds the configured limit.");
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static void AddResponseAsciiField(HttpHeaders headers, string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        headers.Fields.Add(field);
    }

    static string CanonicalizeResponseFieldName(string value)
    {
        StringBuilder result = new StringBuilder();
        bool upper = true;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character >= 'A' && character <= 'Z' && !upper)
            {
                character = (char)(character + 0x20);
            }
            else if (character >= 'a' && character <= 'z' && upper)
            {
                character = (char)(character - 0x20);
            }

            result.Append(character);
            upper = character == '-';
            index = index + 1;
        }

        return result.ToString();
    }

    static long CalculateResponseSize(
        HttpResponse response,
        Http1SerializationOptions options,
        Http1ResponseSerializationPlan plan)
    {
        long size = plan.Version.Length + 1L + 3 + 1 +
            plan.ReasonPhrase.Length + 2;
        int index = 0;
        while (index < plan.Headers.Fields.Count)
        {
            HttpHeaderField field = plan.Headers.Fields[index];
            size = size + field.Name.Length + 2L + field.Value.Length + 2;
            index = index + 1;
        }

        size = size + 2;
        if (plan.FramingDecision.BodyFraming == Http1BodyFraming.ContentLength ||
            plan.FramingDecision.BodyFraming == Http1BodyFraming.CloseDelimited)
        {
            return size + response.Content.Length;
        }

        if (plan.FramingDecision.BodyFraming != Http1BodyFraming.Chunked)
        {
            return size;
        }

        return size + Http1ChunkSerializer.CalculateEncodedSize(
            response.Content,
            options.ChunkSize,
            plan.ChunkExtension,
            response.Trailers);
    }

    static int WriteResponseToSpan(
        HttpResponse response,
        Http1SerializationOptions options,
        Http1ResponseSerializationPlan plan,
        Span<byte> output)
    {
        int offset = 0;
        offset = Http1SpanWriter.WriteAsciiCharacters(output, offset, plan.Version);
        offset = Http1SpanWriter.WriteSingleByte(output, offset, 0x20);
        offset = Http1SpanWriter.WriteAsciiCharacters(
            output,
            offset,
            response.StatusCode.ToString("D3", CultureInfo.InvariantCulture));
        offset = Http1SpanWriter.WriteSingleByte(output, offset, 0x20);
        offset = Http1SpanWriter.WriteByteArray(output, offset, plan.ReasonPhrase);
        offset = Http1SpanWriter.WriteLineEnding(output, offset);
        int fieldIndex = 0;
        while (fieldIndex < plan.Headers.Fields.Count)
        {
            HttpHeaderField field = plan.Headers.Fields[fieldIndex];
            string name = options.PreserveFieldNameCasing ?
                field.Name : CanonicalizeResponseFieldName(field.Name);
            offset = Http1SpanWriter.WriteAsciiCharacters(output, offset, name);
            offset = Http1SpanWriter.WriteAsciiCharacters(output, offset, ": ");
            offset = Http1SpanWriter.WriteByteArray(output, offset, field.Value);
            offset = Http1SpanWriter.WriteLineEnding(output, offset);
            fieldIndex = fieldIndex + 1;
        }

        offset = Http1SpanWriter.WriteLineEnding(output, offset);
        if (plan.FramingDecision.BodyFraming == Http1BodyFraming.ContentLength ||
            plan.FramingDecision.BodyFraming == Http1BodyFraming.CloseDelimited)
        {
            offset = Http1SpanWriter.WriteByteArray(output, offset, response.Content);
        }
        else if (plan.FramingDecision.BodyFraming == Http1BodyFraming.Chunked)
        {
            offset = Http1SpanWriter.WriteChunkSequence(
                output,
                offset,
                response.Content,
                options.ChunkSize,
                plan.ChunkExtension,
                response.Trailers,
                options.PreserveFieldNameCasing);
        }

        return offset;
    }

    static Http1SerializationResult CreateResponseResult(
        Http1SerializationOptions options)
    {
        Http1SerializationResult result = new Http1SerializationResult();
        result.Options = options;
        return result;
    }

    static Http1SerializationResult RejectResponseResult(
        Http1SerializationResult result,
        string? code,
        string? message)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorCode = code ?? "invalid-http1-response-serialization";
        result.ErrorMessage = message ?? "The HTTP/1 response cannot be serialized.";
        return result;
    }

    static Http1ResponseSerializationPlan RejectResponsePlan(
        Http1ResponseSerializationPlan plan,
        string code,
        string message)
    {
        plan.Status = HttpOperationStatus.Rejected;
        plan.ErrorCode = code;
        plan.ErrorMessage = message;
        return plan;
    }
}
