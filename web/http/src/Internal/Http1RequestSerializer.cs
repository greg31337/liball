using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace http;

internal class Http1RequestSerializationPlan
{
    internal HttpOperationStatus Status;
    internal string Target = "";
    internal string Version = "";
    internal string ChunkExtension = "";
    internal HttpHeaders Headers = new HttpHeaders();
    internal Http1FramingDecision FramingDecision = new Http1FramingDecision();
    internal Http1ConnectionDecision? ConnectionDecision;
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal static class Http1RequestSerializer
{
    internal static Http1SerializationResult Serialize(
        HttpRequest request,
        Http1SerializationOptions options)
    {
        Http1SerializationResult result = CreateResult(options);
        Http1RequestSerializationPlan plan = CreatePlan(request, options);
        if (plan.Status != HttpOperationStatus.Done)
        {
            return Reject(result, plan.ErrorCode, plan.ErrorMessage);
        }

        long required = CalculateRequiredSize(request, options, plan);
        if (required < 0 || required > int.MaxValue)
        {
            return Reject(
                result,
                "http1-serialization-size-overflow",
                "The serialized HTTP/1 request exceeds the supported output size.");
        }

        result.Bytes = new byte[(int)required];
        result.BytesWritten = WriteRequestToSpan(request, options, plan, result.Bytes);
        result.RequiredSize = result.BytesWritten;
        result.FramingDecision = plan.FramingDecision;
        result.ConnectionDecision = plan.ConnectionDecision;
        result.Status = HttpOperationStatus.Done;
        return result;
    }

    internal static Http1SerializationResult Write(
        HttpRequest request,
        Http1SerializationOptions options,
        Span<byte> output)
    {
        Http1SerializationResult result = CreateResult(options);
        Http1RequestSerializationPlan plan = CreatePlan(request, options);
        if (plan.Status != HttpOperationStatus.Done)
        {
            return Reject(result, plan.ErrorCode, plan.ErrorMessage);
        }

        long required = CalculateRequiredSize(request, options, plan);
        if (required < 0 || required > int.MaxValue)
        {
            return Reject(
                result,
                "http1-serialization-size-overflow",
                "The serialized HTTP/1 request exceeds the supported output size.");
        }

        result.RequiredSize = (int)required;
        result.FramingDecision = plan.FramingDecision;
        result.ConnectionDecision = plan.ConnectionDecision;
        if (output.Length < result.RequiredSize)
        {
            result.Status = HttpOperationStatus.NeedOutput;
            return result;
        }

        result.BytesWritten = WriteRequestToSpan(request, options, plan, output);
        result.Status = HttpOperationStatus.Done;
        return result;
    }

    internal static Http1FramingDecision AnalyzeFraming(HttpRequest request)
    {
        Http1SerializationOptions options = new Http1SerializationOptions();
        Http1RequestSerializationPlan plan = CreatePlan(request, options);
        if (plan.Status != HttpOperationStatus.Done)
        {
            Http1BodyFraming framing = plan.FramingDecision.BodyFraming;
            throw new Http1FramingException(
                plan.ErrorMessage ?? "The HTTP/1 request framing is invalid.",
                plan.ErrorCode ?? "invalid-http1-request-framing",
                framing);
        }

        return plan.FramingDecision;
    }

    internal static Http1ConnectionDecision AnalyzeConnection(HttpRequest request)
    {
        Http1SerializationOptions options = new Http1SerializationOptions();
        Http1RequestSerializationPlan plan = CreatePlan(request, options);
        if (plan.Status != HttpOperationStatus.Done || plan.ConnectionDecision == null)
        {
            throw new Http1FramingException(
                plan.ErrorMessage ?? "The HTTP/1 request connection state is invalid.",
                plan.ErrorCode ?? "invalid-http1-request-connection",
                plan.FramingDecision.BodyFraming);
        }

        return plan.ConnectionDecision;
    }

    static Http1RequestSerializationPlan CreatePlan(
        HttpRequest request,
        Http1SerializationOptions options)
    {
        Http1RequestSerializationPlan plan = new Http1RequestSerializationPlan();
        if (!HasValidStructure(request, options))
        {
            return RejectPlan(
                plan,
                "invalid-http1-serialization-input",
                "The HTTP/1 request or serialization options are incomplete.");
        }

        if (!HasValidLimits(options.Limits))
        {
            return RejectPlan(
                plan,
                "invalid-http1-serialization-limits",
                "The HTTP/1 serialization limits cannot be negative.");
        }

        if (!HeaderStore.IsValidName(request.Method))
        {
            return RejectPlan(
                plan,
                "invalid-http1-method",
                "The HTTP/1 request method must be a non-empty ASCII token.");
        }

        if (request.Content.Length > options.Limits.MaximumContentSize)
        {
            return RejectPlan(
                plan,
                "http1-content-limit-exceeded",
                "The HTTP/1 request content exceeds the configured limit.");
        }

        if ((request.Method == HttpMethods.Connect ||
            request.Method == HttpMethods.Trace) &&
            (request.Content.Length != 0 || request.Trailers.Fields.Count != 0))
        {
            return RejectPlan(
                plan,
                "http1-request-content-not-allowed",
                "The request method does not permit content or trailers.");
        }

        string version = ResolveVersion(request.ProtocolVersion);
        if (version.Length == 0)
        {
            return RejectPlan(
                plan,
                "invalid-http1-version",
                "Only HTTP/1.0 and HTTP/1.1 requests can be serialized.");
        }

        string target = options.RequestTarget ?? request.Target;
        string? host = ValidateFieldsAndHost(request, options, plan);
        if (plan.Status == HttpOperationStatus.Rejected)
        {
            return plan;
        }

        if (!ValidateSerializedFieldLimits(plan.Headers, options.Limits, plan))
        {
            return plan;
        }

        string? unsafeConnectionOption =
            Http1FramingNormalizer.FindUnsafeConnectionOption(plan.Headers);
        if (unsafeConnectionOption != null)
        {
            return RejectPlan(
                plan,
                "http1-connection-framing-option",
                "Connection cannot nominate a message framing field.");
        }

        if (options.UseAbsoluteRequestTarget)
        {
            target = BuildAbsoluteTarget(request, target, host, plan);
            if (plan.Status == HttpOperationStatus.Rejected)
            {
                return plan;
            }
        }

        if (!IsAscii(target))
        {
            return RejectPlan(
                plan,
                "invalid-http1-request-target",
                "The HTTP/1 request target must contain only ASCII octets.");
        }

        string startLine = request.Method + " " + target + " " + version;
        if (startLine.Length > options.Limits.MaximumStartLineSize)
        {
            return RejectPlan(
                plan,
                "http1-start-line-limit-exceeded",
                "The HTTP/1 request line exceeds the configured limit.");
        }

        Http1RequestLineResult parsedLine = Http1LineParser.ParseRequestLine(
            Encoding.ASCII.GetBytes(startLine));
        if (parsedLine.Status != HttpOperationStatus.Done || parsedLine.Request == null)
        {
            return RejectPlan(
                plan,
                parsedLine.ErrorCode ?? "invalid-http1-request-line",
                parsedLine.ErrorMessage ?? "The HTTP/1 request line is invalid.");
        }

        if (!ValidateAuthority(request, parsedLine.Request, host))
        {
            return RejectPlan(
                plan,
                "conflicting-http1-request-authority",
                "The request target, authority, and Host field do not agree.");
        }

        Http1ParserOptions parserOptions = new Http1ParserOptions();
        parserOptions.Limits = options.Limits;
        Http1FramingResult existing = Http1Framing.DetermineRequest(
            request,
            parserOptions);
        if (!existing.Decision.IsValid)
        {
            plan.FramingDecision = existing.Decision;
            return RejectPlan(
                plan,
                existing.ErrorCode ?? "invalid-http1-request-framing",
                existing.ErrorMessage ?? "The HTTP/1 request framing is invalid.");
        }

        plan.Target = target;
        plan.Version = version;
        plan.FramingDecision = SelectFraming(request, options, existing.Decision, plan);
        if (plan.Status == HttpOperationStatus.Rejected)
        {
            return plan;
        }

        if (options.HasBodyFraming)
        {
            Http1TrailerValidationResult declarations =
                Http1TrailerValidator.ValidateDeclarations(
                    plan.Headers,
                    options.Limits);
            if (declarations.Status != HttpOperationStatus.Done)
            {
                return RejectPlan(
                    plan,
                    declarations.ErrorCode ?? "invalid-http1-trailer-declaration",
                    declarations.ErrorMessage ??
                        "The HTTP/1 Trailer field declaration is invalid.");
            }

            Http1FramingNormalizer.NormalizeSelectedFraming(
                plan.Headers,
                plan.FramingDecision.BodyFraming,
                request.Content.Length);
        }

        AddGeneratedFramingFields(request, options, existing.Decision, plan);
        Http1ChunkSerializationResult chunks =
            Http1ChunkSerializer.PrepareChunkedMessage(
                plan.Headers,
                request.Trailers,
                plan.FramingDecision,
                options);
        if (chunks.Status != HttpOperationStatus.Done)
        {
            return RejectPlan(
                plan,
                chunks.ErrorCode ?? "invalid-http1-chunked-serialization",
                chunks.ErrorMessage ??
                    "The HTTP/1 request chunked content cannot be serialized.");
        }

        plan.ChunkExtension = chunks.ChunkExtension;
        if (options.CloseConnection)
        {
            AddAsciiField(plan.Headers, HttpHeaderNames.Connection, "close");
        }

        if (!ValidateSerializedFieldLimits(plan.Headers, options.Limits, plan))
        {
            return plan;
        }

        Http1ConnectionResult connection = Http1Connection.Determine(
            plan.Headers,
            request.ProtocolVersion,
            plan.FramingDecision,
            false,
            false,
            0,
            options.Limits);
        if (connection.Status != HttpOperationStatus.Done)
        {
            return RejectPlan(
                plan,
                connection.ErrorCode ?? "invalid-http1-connection",
                connection.ErrorMessage ?? "The HTTP/1 connection fields are invalid.");
        }

        plan.ConnectionDecision = connection.Decision;
        plan.Status = HttpOperationStatus.Done;
        return plan;
    }

    static bool HasValidStructure(
        HttpRequest request,
        Http1SerializationOptions options)
    {
        return request != null && options != null && options.Limits != null &&
            request.Method != null && request.Target != null &&
            request.Headers != null && request.Headers.Fields != null &&
            request.Content != null && request.Trailers != null &&
            request.Trailers.Fields != null;
    }

    static bool HasValidLimits(HttpLimits limits)
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

    static string ResolveVersion(HttpProtocolVersion version)
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

    static string? ValidateFieldsAndHost(
        HttpRequest request,
        Http1SerializationOptions options,
        Http1RequestSerializationPlan plan)
    {
        int hostCount = 0;
        string? host = null;
        int fieldIndex = 0;
        while (fieldIndex < request.Headers.Fields.Count)
        {
            HttpHeaderField field = request.Headers.Fields[fieldIndex];
            if (field == null || !HeaderStore.IsValidName(field.Name) ||
                !HeaderStore.IsValidValue(field.Value))
            {
                RejectPlan(
                    plan,
                    "invalid-http1-field",
                    "The HTTP/1 request contains an invalid field name or value.");
                return null;
            }

            plan.Headers.Fields.Add(field);
            if (Ascii.EqualsStringsIgnoreCase(field.Name, HttpHeaderNames.Host))
            {
                hostCount = hostCount + 1;
                host = DecodeHost(field.Value);
                if (host == null || !IsValidAuthority(host, false))
                {
                    RejectPlan(
                        plan,
                        "invalid-http1-host-field",
                        "The HTTP/1 Host field value is malformed.");
                    return null;
                }
            }

            fieldIndex = fieldIndex + 1;
        }

        if (hostCount > 1)
        {
            RejectPlan(
                plan,
                "multiple-http1-host-fields",
                "An HTTP/1 request cannot contain multiple Host fields.");
            return null;
        }

        if (request.ProtocolVersion == HttpProtocolVersion.Http11 && hostCount != 1)
        {
            RejectPlan(
                plan,
                "missing-http1-host-field",
                "An HTTP/1.1 request requires exactly one Host field.");
            return null;
        }

        return host;
    }

    static string BuildAbsoluteTarget(
        HttpRequest request,
        string target,
        string? host,
        Http1RequestSerializationPlan plan)
    {
        if (target == "*" || request.Method == HttpMethods.Connect ||
            target.Length == 0 || target[0] != '/')
        {
            return target;
        }

        if (request.Scheme == null ||
            UriCodec.ValidateScheme(request.Scheme, int.MaxValue).Status !=
                HttpOperationStatus.Done)
        {
            RejectPlan(
                plan,
                "invalid-http1-request-scheme",
                "A valid scheme is required for an absolute request target.");
            return target;
        }

        string authority = request.Authority ?? host ?? "";
        if (!IsValidAuthority(authority, false) || authority.Length == 0)
        {
            RejectPlan(
                plan,
                "invalid-http1-request-authority",
                "A valid authority is required for an absolute request target.");
            return target;
        }

        return request.Scheme + "://" + authority + target;
    }

    static bool ValidateAuthority(
        HttpRequest source,
        HttpRequest parsed,
        string? host)
    {
        string scheme = parsed.Scheme ?? source.Scheme ?? "";
        if (source.Authority != null && !IsValidAuthority(source.Authority, false))
        {
            return false;
        }

        if (parsed.Authority != null && host != null &&
            !RequestTargetSemantics.AreAuthoritiesEquivalent(
                parsed.Authority,
                host,
                scheme))
        {
            return false;
        }

        if (source.Authority != null && host != null &&
            !RequestTargetSemantics.AreAuthoritiesEquivalent(
                source.Authority,
                host,
                scheme))
        {
            return false;
        }

        if (source.Authority != null && parsed.Authority != null &&
            !RequestTargetSemantics.AreAuthoritiesEquivalent(
                source.Authority,
                parsed.Authority,
                scheme))
        {
            return false;
        }

        return true;
    }

    static Http1FramingDecision SelectFraming(
        HttpRequest request,
        Http1SerializationOptions options,
        Http1FramingDecision existing,
        Http1RequestSerializationPlan plan)
    {
        Http1BodyFraming selected = existing.BodyFraming;
        if (existing.BodyFraming == Http1BodyFraming.ContentLength &&
            options.VerifyContentLength &&
            existing.DeclaredLength != request.Content.Length)
        {
            RejectPlan(
                plan,
                "mismatched-http1-content-length",
                "Content-Length does not match the request content size.");
            return existing;
        }

        if (options.HasBodyFraming)
        {
            selected = options.BodyFraming;
            if (selected != Http1BodyFraming.None &&
                selected != Http1BodyFraming.ContentLength &&
                selected != Http1BodyFraming.Chunked)
            {
                RejectPlan(
                    plan,
                    "unsupported-http1-request-framing",
                    "The selected body framing cannot be used for an HTTP/1 request.");
                return existing;
            }

        }
        else if (selected == Http1BodyFraming.None &&
            request.Trailers.Fields.Count != 0)
        {
            selected = Http1BodyFraming.Chunked;
        }
        else if (selected == Http1BodyFraming.None && request.Content.Length != 0)
        {
            selected = Http1BodyFraming.ContentLength;
        }

        if (selected == Http1BodyFraming.None && request.Content.Length != 0)
        {
            RejectPlan(
                plan,
                "unframed-http1-request-content",
                "HTTP/1 request content requires Content-Length or chunked framing.");
            return existing;
        }

        if (selected == Http1BodyFraming.ContentLength &&
            existing.BodyFraming != Http1BodyFraming.ContentLength &&
            !options.AddContentLength)
        {
            RejectPlan(
                plan,
                "missing-http1-content-length",
                "Content-Length generation is disabled for length-framed content.");
            return existing;
        }

        if (selected == Http1BodyFraming.Chunked && options.ChunkSize <= 0)
        {
            RejectPlan(
                plan,
                "invalid-http1-chunk-size",
                "The configured HTTP/1 chunk size must be positive.");
            return existing;
        }

        if (selected == Http1BodyFraming.Chunked &&
            request.ProtocolVersion != HttpProtocolVersion.Http11)
        {
            RejectPlan(
                plan,
                "http1-chunked-requires-http11",
                "Chunked transfer coding cannot be used with HTTP/1.0.");
            return existing;
        }

        if (selected == Http1BodyFraming.Chunked &&
            options.ChunkSize > options.Limits.MaximumChunkSize)
        {
            RejectPlan(
                plan,
                "http1-chunk-limit-exceeded",
                "The configured HTTP/1 chunk size exceeds the configured limit.");
            return existing;
        }

        if (selected == existing.BodyFraming)
        {
            return existing;
        }

        return CreateFraming(selected, request.Content.Length);
    }

    static Http1FramingDecision CreateFraming(
        Http1BodyFraming framing,
        int contentLength)
    {
        Http1FramingDecision result = new Http1FramingDecision();
        result.BodyFraming = framing;
        result.IsValid = true;
        result.IsSelfDelimited = true;
        result.HasNoBody = framing == Http1BodyFraming.None;
        result.HasDeclaredLength = framing == Http1BodyFraming.ContentLength;
        result.DeclaredLength = contentLength;
        if (framing == Http1BodyFraming.Chunked)
        {
            result.TransferCodings.Add(HttpTransferCodings.Chunked);
        }

        if (framing == Http1BodyFraming.Chunked)
        {
            result.Reason = "The request uses chunked transfer coding.";
        }
        else if (framing == Http1BodyFraming.None)
        {
            result.Reason = "The request has no message body.";
        }
        else
        {
            result.Reason = "The request uses an exact content length.";
        }

        return result;
    }

    static void AddGeneratedFramingFields(
        HttpRequest request,
        Http1SerializationOptions options,
        Http1FramingDecision existing,
        Http1RequestSerializationPlan plan)
    {
        if (options.HasBodyFraming)
        {
            return;
        }

        if (plan.FramingDecision.BodyFraming == Http1BodyFraming.ContentLength &&
            existing.BodyFraming == Http1BodyFraming.None &&
            options.AddContentLength)
        {
            AddAsciiField(
                plan.Headers,
                HttpHeaderNames.ContentLength,
                request.Content.Length.ToString(CultureInfo.InvariantCulture));
        }

        if (plan.FramingDecision.BodyFraming == Http1BodyFraming.Chunked &&
            existing.BodyFraming == Http1BodyFraming.None)
        {
            AddAsciiField(
                plan.Headers,
                HttpHeaderNames.TransferEncoding,
                HttpTransferCodings.Chunked);
        }
    }

    static bool ValidateSerializedFieldLimits(
        HttpHeaders headers,
        HttpLimits limits,
        Http1RequestSerializationPlan plan)
    {
        if (headers.Fields.Count > limits.MaximumFieldCount)
        {
            RejectPlan(
                plan,
                "http1-field-count-limit-exceeded",
                "The HTTP/1 request field count exceeds the configured limit.");
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
                RejectPlan(
                    plan,
                    "http1-field-value-limit-exceeded",
                    "An HTTP/1 request field value exceeds the configured limit.");
                return false;
            }

            if (lineSize > limits.MaximumFieldLineSize)
            {
                RejectPlan(
                    plan,
                    "http1-field-line-limit-exceeded",
                    "An HTTP/1 request field line exceeds the configured limit.");
                return false;
            }

            sectionSize = sectionSize + lineSize;
            if (sectionSize > limits.MaximumFieldSectionSize)
            {
                RejectPlan(
                    plan,
                    "http1-field-section-limit-exceeded",
                    "The HTTP/1 request field section exceeds the configured limit.");
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static void AddAsciiField(HttpHeaders headers, string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        headers.Fields.Add(field);
    }

    static string? DecodeHost(byte[] value)
    {
        int start = 0;
        while (start < value.Length && (value[start] == 0x20 || value[start] == 0x09))
        {
            start = start + 1;
        }

        int end = value.Length;
        while (end > start && (value[end - 1] == 0x20 || value[end - 1] == 0x09))
        {
            end = end - 1;
        }

        int index = start;
        while (index < end)
        {
            if (value[index] < 0x21 || value[index] > 0x7e)
            {
                return null;
            }

            index = index + 1;
        }

        return Encoding.ASCII.GetString(value, start, end - start);
    }

    static bool IsValidAuthority(string value, bool requirePort)
    {
        UriValidationResult authority = UriCodec.ValidateAuthority(value, int.MaxValue);
        if (authority.Status != HttpOperationStatus.Done || authority.HasUserInfo ||
            authority.HasPort && authority.HasPortValue && !authority.IsPortInRange)
        {
            return false;
        }

        if (requirePort && (!authority.HasPort || !authority.HasPortValue ||
            authority.Host.Length == 0))
        {
            return false;
        }

        return true;
    }

    static bool IsAscii(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0x7f)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static string CanonicalizeFieldName(string value)
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

    static long CalculateRequiredSize(
        HttpRequest request,
        Http1SerializationOptions options,
        Http1RequestSerializationPlan plan)
    {
        long size = request.Method.Length + 1L + plan.Target.Length + 1 +
            plan.Version.Length + 2;
        int index = 0;
        while (index < plan.Headers.Fields.Count)
        {
            HttpHeaderField field = plan.Headers.Fields[index];
            size = size + field.Name.Length + 2L + field.Value.Length + 2;
            index = index + 1;
        }

        size = size + 2;
        if (plan.FramingDecision.BodyFraming == Http1BodyFraming.ContentLength)
        {
            return size + request.Content.Length;
        }

        if (plan.FramingDecision.BodyFraming != Http1BodyFraming.Chunked)
        {
            return size;
        }

        return size + Http1ChunkSerializer.CalculateEncodedSize(
            request.Content,
            options.ChunkSize,
            plan.ChunkExtension,
            request.Trailers);
    }

    static int WriteRequestToSpan(
        HttpRequest request,
        Http1SerializationOptions options,
        Http1RequestSerializationPlan plan,
        Span<byte> output)
    {
        int offset = 0;
        offset = Http1SpanWriter.WriteAsciiCharacters(output, offset, request.Method);
        offset = Http1SpanWriter.WriteSingleByte(output, offset, 0x20);
        offset = Http1SpanWriter.WriteAsciiCharacters(output, offset, plan.Target);
        offset = Http1SpanWriter.WriteSingleByte(output, offset, 0x20);
        offset = Http1SpanWriter.WriteAsciiCharacters(output, offset, plan.Version);
        offset = Http1SpanWriter.WriteLineEnding(output, offset);
        int fieldIndex = 0;
        while (fieldIndex < plan.Headers.Fields.Count)
        {
            HttpHeaderField field = plan.Headers.Fields[fieldIndex];
            string name = options.PreserveFieldNameCasing ?
                field.Name : CanonicalizeFieldName(field.Name);
            offset = Http1SpanWriter.WriteAsciiCharacters(output, offset, name);
            offset = Http1SpanWriter.WriteAsciiCharacters(output, offset, ": ");
            offset = Http1SpanWriter.WriteByteArray(output, offset, field.Value);
            offset = Http1SpanWriter.WriteLineEnding(output, offset);
            fieldIndex = fieldIndex + 1;
        }

        offset = Http1SpanWriter.WriteLineEnding(output, offset);
        if (plan.FramingDecision.BodyFraming == Http1BodyFraming.ContentLength)
        {
            offset = Http1SpanWriter.WriteByteArray(output, offset, request.Content);
        }
        else if (plan.FramingDecision.BodyFraming == Http1BodyFraming.Chunked)
        {
            offset = Http1SpanWriter.WriteChunkSequence(
                output,
                offset,
                request.Content,
                options.ChunkSize,
                plan.ChunkExtension,
                request.Trailers,
                options.PreserveFieldNameCasing);
        }

        return offset;
    }

    static Http1SerializationResult CreateResult(Http1SerializationOptions options)
    {
        Http1SerializationResult result = new Http1SerializationResult();
        result.Options = options;
        return result;
    }

    static Http1SerializationResult Reject(
        Http1SerializationResult result,
        string? code,
        string? message)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorCode = code ?? "invalid-http1-request-serialization";
        result.ErrorMessage = message ?? "The HTTP/1 request cannot be serialized.";
        return result;
    }

    static Http1RequestSerializationPlan RejectPlan(
        Http1RequestSerializationPlan plan,
        string code,
        string message)
    {
        plan.Status = HttpOperationStatus.Rejected;
        plan.ErrorCode = code;
        plan.ErrorMessage = message;
        return plan;
    }
}
