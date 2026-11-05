namespace sip;

internal static class SipMimeFormatter
{
    internal static SipWriteResult FormatEntityModel(SipMimeEntity? entity, SipWriteOptions? options)
    {
        if (entity == null) return CreateError("A MIME entity is required.");
        if (options == null) return CreateError("MIME write options are required.");
        if (options.MaxHeaderLineBytes < 0)
            return CreateError("The maximum MIME header line size cannot be negative.");
        return FormatEntityDepth(entity, options, 0);
    }

    static SipWriteResult FormatEntityDepth(SipMimeEntity entity, SipWriteOptions options, int depth)
    {
        if (options.PreserveOriginal && entity.OriginalBytes != null && entity.OriginalBytes.Length > 0)
        {
            SipValidationIssue[] originalIssues = SipMimeValidator.ValidateEntityModel(entity, true);
            if (originalIssues.Length > 0)
            {
                string? originalMessage = originalIssues[0].Message;
                return CreateError(originalMessage == null ? "The MIME entity is invalid." : originalMessage);
            }
            return CreateSuccess(SipMimeSyntax.CopyByteRange(
                entity.OriginalBytes, 0, entity.OriginalBytes.Length));
        }
        SipMediaType? preparedType = PrepareContentType(entity);
        bool multipart = SipMimeSyntax.IsMultipartType(preparedType);
        System.Collections.Generic.List<byte[]> partBytes = new System.Collections.Generic.List<byte[]>();
        if (multipart && entity.Parts != null)
        {
            int partIndex = 0;
            while (partIndex < entity.Parts.Length)
            {
                SipMimeEntity? part = entity.Parts[partIndex];
                if (part == null) return CreateError("A multipart entity contains a null body part.");
                SipWriteResult formattedPart = FormatEntityDepth(part, options, depth + 1);
                if (!formattedPart.Success) return formattedPart;
                partBytes.Add(formattedPart.Bytes);
                partIndex = partIndex + 1;
            }
        }
        string? boundary = SipMimeSyntax.GetBoundaryValue(preparedType);
        if (multipart && boundary == null)
        {
            boundary = CreateSafeBoundary(entity, partBytes);
            preparedType = AddBoundaryParameter(preparedType, boundary);
        }
        SipMimeEntity validationEntity = CopyEntityForValidation(entity, preparedType);
        SipValidationIssue[] issues = SipMimeValidator.ValidateEntityModel(validationEntity, true);
        if (issues.Length > 0)
        {
            string? issueMessage = issues[0].Message;
            return CreateError(issueMessage == null ? "The MIME entity is invalid." : issueMessage);
        }
        if (multipart && boundary != null && BoundaryCollides(entity, partBytes, boundary))
            return CreateError("The multipart boundary occurs at the start of an encapsulated line.");

        System.Collections.Generic.List<byte> output = new System.Collections.Generic.List<byte>();
        SipWriteResult headers = FormatHeaders(entity, preparedType, options);
        if (!headers.Success) return headers;
        AppendBytes(output, headers.Bytes);
        output.Add((byte)'\r');
        output.Add((byte)'\n');
        if (multipart)
        {
            string boundaryValue = boundary == null ? "" : boundary;
            AppendMultipartBody(output, entity, partBytes, boundaryValue);
        }
        else
        {
            byte[] body = entity.Body;
            if (SipMimeSyntax.IsSipFragmentType(preparedType) && body.Length == 0
                && entity.SipFragment != null)
            {
                SipWriteResult fragment = FormatSipFragment(entity.SipFragment, options);
                if (!fragment.Success) return fragment;
                body = fragment.Bytes;
            }
            AppendBytes(output, body);
        }
        return CreateSuccess(output.ToArray());
    }

    static SipWriteResult FormatHeaders(SipMimeEntity entity, SipMediaType? contentType,
        SipWriteOptions options)
    {
        System.Collections.Generic.List<byte> output = new System.Collections.Generic.List<byte>();
        bool wroteType = false;
        bool wroteDisposition = false;
        bool wroteEncoding = false;
        int index = 0;
        while (index < entity.Headers.Length)
        {
            SipHeader? header = entity.Headers[index];
            if (header == null) return CreateError("A MIME entity contains a null header field.");
            if (SipMimeSyntax.HeaderNameIs(header.Name, SipHeaderNames.ContentType,
                SipHeaderNames.CompactContentType))
            {
                if (!wroteType && contentType != null)
                {
                    SipWriteResult formattedType = FormatContentType(contentType, options);
                    if (!formattedType.Success) return formattedType;
                    AppendBytes(output, formattedType.Bytes);
                    wroteType = true;
                }
                else if (!wroteType)
                {
                    SipWriteResult formatted = SipHeaderFormatter.FormatHeaderModel(header, options);
                    if (!formatted.Success) return formatted;
                    AppendBytes(output, formatted.Bytes);
                    wroteType = true;
                }
            }
            else if (SipMimeSyntax.HeaderNameIs(header.Name, SipHeaderNames.ContentDisposition, null))
            {
                if (!wroteDisposition && entity.ContentDisposition != null)
                {
                    SipWriteResult formattedDisposition = FormatContentDisposition(
                        entity.ContentDisposition, options);
                    if (!formattedDisposition.Success) return formattedDisposition;
                    AppendBytes(output, formattedDisposition.Bytes);
                    wroteDisposition = true;
                }
                else if (!wroteDisposition)
                {
                    SipWriteResult formatted = SipHeaderFormatter.FormatHeaderModel(header, options);
                    if (!formatted.Success) return formatted;
                    AppendBytes(output, formatted.Bytes);
                    wroteDisposition = true;
                }
            }
            else if (SipMimeSyntax.HeaderNameIs(header.Name, "Content-Transfer-Encoding", null))
            {
                if (!wroteEncoding && entity.ContentTransferEncoding != null)
                {
                    SipWriteResult formattedEncoding = FormatTransferEncoding(
                        entity.ContentTransferEncoding, options);
                    if (!formattedEncoding.Success) return formattedEncoding;
                    AppendBytes(output, formattedEncoding.Bytes);
                    wroteEncoding = true;
                }
                else if (!wroteEncoding)
                {
                    SipWriteResult formatted = SipHeaderFormatter.FormatHeaderModel(header, options);
                    if (!formatted.Success) return formatted;
                    AppendBytes(output, formatted.Bytes);
                    wroteEncoding = true;
                }
            }
            else
            {
                SipWriteResult formatted = SipHeaderFormatter.FormatHeaderModel(header, options);
                if (!formatted.Success) return formatted;
                AppendBytes(output, formatted.Bytes);
            }
            index = index + 1;
        }
        if (!wroteType && contentType != null)
        {
            SipWriteResult formattedType = FormatContentType(contentType, options);
            if (!formattedType.Success) return formattedType;
            AppendBytes(output, formattedType.Bytes);
        }
        if (!wroteDisposition && entity.ContentDisposition != null)
        {
            SipWriteResult formattedDisposition = FormatContentDisposition(entity.ContentDisposition, options);
            if (!formattedDisposition.Success) return formattedDisposition;
            AppendBytes(output, formattedDisposition.Bytes);
        }
        if (!wroteEncoding && entity.ContentTransferEncoding != null)
        {
            SipWriteResult formattedEncoding = FormatTransferEncoding(entity.ContentTransferEncoding, options);
            if (!formattedEncoding.Success) return formattedEncoding;
            AppendBytes(output, formattedEncoding.Bytes);
        }
        return CreateSuccess(output.ToArray());
    }

    static SipWriteResult FormatContentType(SipMediaType mediaType, SipWriteOptions options)
    {
        SipCoreHeaderResult parsed = new SipCoreHeaderResult();
        parsed.Success = true;
        parsed.MediaType = mediaType;
        return SipCoreHeaderCodec.FormatCoreHeaderValue(SipHeaderNames.ContentType, parsed, options);
    }

    static SipWriteResult FormatContentDisposition(SipContentDisposition disposition,
        SipWriteOptions options)
    {
        SipCoreHeaderResult parsed = new SipCoreHeaderResult();
        parsed.Success = true;
        parsed.ContentDisposition = disposition;
        return SipCoreHeaderCodec.FormatCoreHeaderValue(SipHeaderNames.ContentDisposition, parsed, options);
    }

    static SipWriteResult FormatTransferEncoding(string encoding, SipWriteOptions options)
    {
        SipHeader header = new SipHeader();
        header.Name = "Content-Transfer-Encoding";
        header.Value = encoding.ToLowerInvariant();
        return SipHeaderFormatter.FormatHeaderModel(header, options);
    }

    static SipMediaType? PrepareContentType(SipMimeEntity entity)
    {
        SipMediaType? source = entity.ContentType;
        if (source == null && entity.Parts != null && entity.Parts.Length > 0)
        {
            source = new SipMediaType();
            source.Type = "multipart";
            source.Subtype = "mixed";
        }
        if (source == null) return null;
        SipMediaType copy = new SipMediaType();
        copy.Type = source.Type;
        copy.Subtype = source.Subtype;
        copy.QualityThousandths = source.QualityThousandths;
        copy.OriginalText = source.OriginalText;
        if (source.Parameters == null)
        {
            copy.Parameters = new SipUriParameter[0];
            return copy;
        }
        copy.Parameters = new SipUriParameter[source.Parameters.Length];
        int index = 0;
        while (index < source.Parameters.Length)
        {
            copy.Parameters[index] = CopyParameter(source.Parameters[index]);
            index = index + 1;
        }
        return copy;
    }

    static SipMediaType AddBoundaryParameter(SipMediaType? source, string boundary)
    {
        SipMediaType result = source == null ? new SipMediaType() : source;
        if (source == null)
        {
            result.Type = "multipart";
            result.Subtype = "mixed";
        }
        int oldLength = result.Parameters == null ? 0 : result.Parameters.Length;
        SipUriParameter[] parameters = new SipUriParameter[oldLength + 1];
        SipUriParameter[] oldParameters = result.Parameters == null
            ? new SipUriParameter[0] : result.Parameters;
        if (oldLength > 0) System.Array.Copy(oldParameters, parameters, oldLength);
        SipUriParameter parameter = new SipUriParameter();
        parameter.Name = "boundary";
        parameter.Value = boundary;
        parameter.HasValue = true;
        parameters[oldLength] = parameter;
        result.Parameters = parameters;
        return result;
    }

    static SipUriParameter CopyParameter(SipUriParameter? source)
    {
        SipUriParameter result = new SipUriParameter();
        if (source == null) return result;
        result.Name = source.Name;
        result.Value = source.Value;
        result.HasValue = source.HasValue;
        result.OriginalText = source.OriginalText;
        return result;
    }

    static SipMimeEntity CopyEntityForValidation(SipMimeEntity source, SipMediaType? contentType)
    {
        SipMimeEntity result = new SipMimeEntity();
        result.Headers = source.Headers;
        result.ContentType = contentType;
        result.ContentDisposition = source.ContentDisposition;
        result.ContentTransferEncoding = source.ContentTransferEncoding;
        result.Body = source.Body;
        result.Parts = source.Parts;
        result.Preamble = source.Preamble;
        result.Epilogue = source.Epilogue;
        result.SipFragment = source.SipFragment;
        result.OriginalBytes = source.OriginalBytes;
        return result;
    }

    static string CreateSafeBoundary(SipMimeEntity entity,
        System.Collections.Generic.List<byte[]> partBytes)
    {
        SipProtocolTokenGenerator generator = new SipProtocolTokenGenerator(
            new SipCryptographicRandomSource());
        int attempt = 0;
        while (attempt < 32)
        {
            string boundary = generator.CreateMimeBoundary();
            if (!BoundaryCollides(entity, partBytes, boundary)) return boundary;
            attempt = attempt + 1;
        }
        return "sip-boundary-generation-failed";
    }

    static bool BoundaryCollides(SipMimeEntity entity,
        System.Collections.Generic.List<byte[]> partBytes, string boundary)
    {
        byte[] marker = System.Text.Encoding.ASCII.GetBytes("--" + boundary);
        if (ContainsLinePrefix(entity.Preamble, marker) || ContainsLinePrefix(entity.Epilogue, marker)) return true;
        int index = 0;
        while (index < partBytes.Count)
        {
            if (ContainsLinePrefix(partBytes[index], marker)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool ContainsLinePrefix(byte[]? source, byte[] marker)
    {
        if (source == null) return false;
        int position = 0;
        while (position + marker.Length <= source.Length)
        {
            bool lineStart = position == 0 || position >= 2
                && source[position - 2] == '\r' && source[position - 1] == '\n';
            if (lineStart)
            {
                int index = 0;
                while (index < marker.Length && source[position + index] == marker[index])
                    index = index + 1;
                if (index == marker.Length) return true;
            }
            position = position + 1;
        }
        return false;
    }

    static void AppendMultipartBody(System.Collections.Generic.List<byte> output, SipMimeEntity entity,
        System.Collections.Generic.List<byte[]> parts, string boundary)
    {
        AppendBytes(output, entity.Preamble);
        if (entity.Preamble.Length > 0) AppendCrlf(output);
        byte[] delimiter = System.Text.Encoding.ASCII.GetBytes("--" + boundary + "\r\n");
        int index = 0;
        while (index < parts.Count)
        {
            AppendBytes(output, delimiter);
            AppendBytes(output, parts[index]);
            AppendCrlf(output);
            index = index + 1;
        }
        AppendBytes(output, System.Text.Encoding.ASCII.GetBytes("--" + boundary + "--\r\n"));
        AppendBytes(output, entity.Epilogue);
    }

    static SipWriteResult FormatSipFragment(SipMessage fragment, SipWriteOptions options)
    {
        if (fragment.Kind == SipMessageKind.Request || fragment.Kind == SipMessageKind.Response)
            return SipMessageFormatter.FormatMessageModel(fragment, options);
        if (fragment.Headers == null || fragment.Body == null)
            return CreateError("The SIP fragment model is incomplete.");
        if (fragment.Headers.Length == 0 && fragment.Body.Length == 0)
            return CreateSuccess(new byte[0]);
        System.Collections.Generic.List<byte> output = new System.Collections.Generic.List<byte>();
        int index = 0;
        while (index < fragment.Headers.Length)
        {
            SipHeader? header = fragment.Headers[index];
            if (header == null) return CreateError("The SIP fragment contains a null header field.");
            SipWriteResult formatted = SipHeaderFormatter.FormatHeaderModel(header, options);
            if (!formatted.Success) return formatted;
            AppendBytes(output, formatted.Bytes);
            index = index + 1;
        }
        if (fragment.Body.Length > 0)
        {
            AppendCrlf(output);
            AppendBytes(output, fragment.Body);
        }
        return CreateSuccess(output.ToArray());
    }

    static void AppendBytes(System.Collections.Generic.List<byte> output, byte[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            output.Add(source[index]);
            index = index + 1;
        }
    }

    static void AppendCrlf(System.Collections.Generic.List<byte> output)
    {
        output.Add((byte)'\r');
        output.Add((byte)'\n');
    }

    static SipWriteResult CreateSuccess(byte[] bytes)
    {
        SipWriteResult result = new SipWriteResult();
        result.Success = true;
        result.Bytes = bytes;
        result.Written = bytes.Length;
        result.RequiredBytes = bytes.Length;
        return result;
    }

    static SipWriteResult CreateError(string message)
    {
        SipWriteResult result = new SipWriteResult();
        result.ErrorMessage = message;
        return result;
    }
}
