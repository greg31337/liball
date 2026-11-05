using System;
using System.Collections.Generic;
using System.Text;

namespace email;

internal class EmailCanonicalWriterWorker
{
    EmailWriteOptions options;
    EmailIssueList issues;
    int partCount;

    internal EmailCanonicalWriterWorker(EmailWriteOptions options)
    {
        this.options = options;
        issues = new EmailIssueList();
        partCount = 0;
    }

    internal EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    internal byte[]? WriteMessage(EmailMessage message)
    {
        return WriteMessageAtDepth(message, 0);
    }

    internal byte[]? WriteEntity(MimeEntity entity)
    {
        return WriteFullEntity(entity, 0);
    }

    internal byte[]? WriteDirectEntityHeaders(MimeEntity entity, string transferEncodingName)
    {
        EmailHeaders? headers = BuildHeaders(entity.GetHeaders(), entity, transferEncodingName, null, false);
        return headers == null ? null : WriteHeaders(headers);
    }

    internal byte[]? WriteDirectMessageHeaders(EmailMessage message, MimeEntity bodyEntity, string transferEncodingName)
    {
        EmailHeaders? headers = BuildHeaders(message.GetHeaders(), bodyEntity, transferEncodingName, null, true);
        return headers == null ? null : WriteHeaders(headers);
    }

    byte[]? WriteMessageAtDepth(EmailMessage? message, int depth)
    {
        if (!CheckDepth(depth))
        {
            return null;
        }

        MimeEntity? bodyEntity = (message ?? throw new InvalidOperationException()).HasBody() ? message.GetBody() : null;
        EmailCanonicalBody? body = bodyEntity == null ? new EmailCanonicalBody(new byte[0], null) : WriteEntityBody(bodyEntity, depth);
        if (body == null)
        {
            return null;
        }

        EmailHeaders? headers = BuildHeaders(message.GetHeaders(), bodyEntity, body.GetTransferEncodingName(), body.GetBoundary(), true);
        if (headers == null)
        {
            return null;
        }

        byte[]? headerBytes = WriteHeaders(headers);
        if (headerBytes == null)
        {
            return null;
        }

        byte[]? combined = JoinBounded(headerBytes, body.GetBytes());
        return combined == null ? null : EnsureTerminalCrlf(combined);
    }

    byte[]? WriteFullEntity(MimeEntity entity, int depth)
    {
        EmailCanonicalBody? body = WriteEntityBody(entity, depth);
        if (body == null)
        {
            return null;
        }

        EmailHeaders? headers = BuildHeaders(entity.GetHeaders(), entity, body.GetTransferEncodingName(), body.GetBoundary(), false);
        if (headers == null)
        {
            return null;
        }

        byte[]? headerBytes = WriteHeaders(headers);
        if (headerBytes == null)
        {
            return null;
        }

        byte[]? combined = JoinBounded(headerBytes, body.GetBytes());
        return combined == null ? null : EnsureTerminalCrlf(combined);
    }

    EmailCanonicalBody? WriteEntityBody(MimeEntity entity, int depth)
    {
        if (!CheckDepth(depth) || !AddPart())
        {
            return null;
        }

        if (entity.IsContainer())
        {
            return WriteMultipartBody(entity, depth);
        }

        if (entity.GetKind() == MimeEntityKind.Message)
        {
            byte[]? nested = WriteMessageAtDepth(entity.GetNestedMessage(), depth + 1);
            if (nested == null)
            {
                return null;
            }

            return EncodeDecodedContent(entity, nested);
        }

        return PrepareDirectContent(entity);
    }

    EmailCanonicalBody? WriteMultipartBody(MimeEntity entity, int depth)
    {
        byte[]? preamble = entity.HasPreamble() ? Materialize(entity.GetPreamble()) : new byte[0];
        byte[]? epilogue = entity.HasEpilogue() ? Materialize(entity.GetEpilogue()) : new byte[0];
        if (preamble == null || epilogue == null)
        {
            return null;
        }

        preamble = NormalizeCrlf(preamble);
        epilogue = NormalizeCrlf(epilogue);
        if (preamble == null || epilogue == null)
        {
            return null;
        }

        List<byte[]> childBytes = new List<byte[]>();
        List<EmailContent> collisionContent = new List<EmailContent>();
        AddCollisionContent(collisionContent, preamble);
        int childIndex = 0;
        while (childIndex < entity.GetChildCount())
        {
            byte[]? child;
            if (entity.GetKind() == MimeEntityKind.Signed && childIndex == 0)
            {
                child = WriteSignedContent(entity.GetChildAt(childIndex), depth + 1);
            }
            else
            {
                child = WriteFullEntity(entity.GetChildAt(childIndex), depth + 1);
            }

            if (child == null)
            {
                return null;
            }

            childBytes.Add(child);
            AddCollisionContent(collisionContent, child);
            childIndex++;
        }

        AddCollisionContent(collisionContent, epilogue);
        EmailBoundaryGenerationResult generation = new EmailBoundaryGenerator().GenerateWithContentsAndOptions(collisionContent, options);
        string boundary = generation.GetBoundary();
        List<byte> output = new List<byte>();
        if (!AppendBytes(output, preamble) || !EnsureListTerminalCrlf(output))
        {
            return null;
        }

        childIndex = 0;
        while (childIndex < childBytes.Count)
        {
            if (!AppendAscii(output, "--" + boundary + "\r\n") || !AppendBytes(output, childBytes[childIndex]) || !EnsureListTerminalCrlf(output))
            {
                return null;
            }

            childIndex++;
        }

        if (!AppendAscii(output, "--" + boundary + "--\r\n") || !AppendBytes(output, epilogue))
        {
            return null;
        }

        byte[] body = output.ToArray();
        string transferEncoding = GetContainerEncodingName();
        if (!ValidateContainerBytes(body, transferEncoding))
        {
            return null;
        }

        return EmailCanonicalBody.CreateWithBytesAndTransferEncodingNameAndBoundary(body, transferEncoding, boundary);
    }

    byte[]? WriteSignedContent(MimeEntity entity, int depth)
    {
        if (!CheckDepth(depth) || !AddPart())
        {
            return null;
        }

        if (entity.HasCurrentOriginalEntityContent())
        {
            return Materialize(entity.GetOriginalEntityContent());
        }

        if (entity.IsContainer() || entity.GetKind() == MimeEntityKind.Message)
        {
            AddFatal("The signed content cannot be reconstructed without changing its canonical bytes.", "Retain a direct-content first part with its original raw headers and body bytes.");
            return null;
        }

        if (!entity.HasRawContent())
        {
            AddFatal("The signed content has no retained raw body bytes.", "Parse with raw body preservation before writing multipart/signed content.");
            return null;
        }

        EmailHeaderWriteOptions headerOptions = options.GetHeaderWriteOptions();
        headerOptions.SetIncludeSeparator(true);
        EmailHeaderWriteResult headerResult = new EmailHeaderWriter().Write(entity.GetHeaders(), EmailHeaderWriteMode.Raw, headerOptions);
        issues.AddRange(headerResult.GetIssues());
        if (headerResult.HasFatalError())
        {
            return null;
        }

        byte[]? body = Materialize(entity.GetRawContent());
        return body == null ? null : JoinBounded(headerResult.GetBytes(), body);
    }

    EmailCanonicalBody? PrepareDirectContent(MimeEntity entity)
    {
        if (!entity.HasContent())
        {
            AddFatal("A canonical MIME content entity has no retained content.", "Supply raw or decoded content before writing.");
            return null;
        }

        EmailTransferEncodingKind explicitEncoding = EmailTransferEncodingKind.SevenBit;
        bool knownEncoding = !entity.HasTransferEncoding() || entity.HasKnownTransferEncoding();
        if (!knownEncoding)
        {
            if (!entity.HasRawContent())
            {
                AddFatal("Decoded content cannot be written with an unknown transfer encoding.", "Supply raw transfer-encoded content or select a known encoding.");
                return null;
            }

            byte[]? unknownBytes = Materialize(entity.GetRawContent());
            if (unknownBytes == null || !ValidateUnknownTransferBytes(unknownBytes))
            {
                return null;
            }

            return new EmailCanonicalBody(unknownBytes, entity.GetTransferEncodingName());
        }

        if (entity.HasTransferEncoding())
        {
            explicitEncoding = entity.GetTransferEncodingKind();
        }

        byte[]? decoded;
        if (entity.HasDecodedContent())
        {
            decoded = Materialize(entity.GetDecodedContent());
        }
        else
        {
            byte[]? raw = Materialize(entity.GetRawContent());
            if (raw == null)
            {
                return null;
            }

            if (explicitEncoding == EmailTransferEncodingKind.Base64)
            {
                EmailBase64DecodeResult result = new EmailBase64().Decode(raw, EmailBase64DecodeMode.Strict, options.GetMaximumOutputBytes());
                if (!result.IsSuccess())
                {
                    AddFatal("Canonical writing could not decode the existing Base64 body.", "Repair the transfer-encoded content before canonical writing.");
                    return null;
                }

                decoded = result.GetDecodedBytes();
            }
            else if (explicitEncoding == EmailTransferEncodingKind.QuotedPrintable)
            {
                EmailQuotedPrintableDecodeResult result = new EmailQuotedPrintable().Decode(raw, EmailQuotedPrintableDecodeMode.Strict, options.GetMaximumOutputBytes());
                if (!result.IsSuccess())
                {
                    AddFatal("Canonical writing could not decode the existing quoted-printable body.", "Repair the transfer-encoded content before canonical writing.");
                    return null;
                }

                decoded = result.GetDecodedBytes();
            }
            else
            {
                decoded = raw;
            }
        }

        return EncodeDecodedContent(entity, decoded);
    }

    EmailCanonicalBody? EncodeDecodedContent(MimeEntity entity, byte[]? decoded)
    {
        EmailTransferEncodingKind selected;
        if (entity.HasTransferEncoding() && entity.HasKnownTransferEncoding())
        {
            EmailTransferEncodingKind explicitEncoding = entity.GetTransferEncodingKind();
            if (explicitEncoding == EmailTransferEncodingKind.Base64 || explicitEncoding == EmailTransferEncodingKind.QuotedPrintable)
            {
                selected = explicitEncoding;
            }
            else
            {
                selected = SelectEncoding(entity, decoded);
            }
        }
        else
        {
            selected = SelectEncoding(entity, decoded);
        }

        byte[]? encoded;
        if (selected == EmailTransferEncodingKind.Base64)
        {
            encoded = new EmailBase64().EncodeWithInput(decoded);
        }
        else if (selected == EmailTransferEncodingKind.QuotedPrintable)
        {
            encoded = new EmailQuotedPrintable().EncodeWithInput(decoded);
        }
        else if (selected == EmailTransferEncodingKind.Binary)
        {
            encoded = Copy(decoded);
        }
        else
        {
            encoded = NormalizeCrlf(decoded);
        }

        if (encoded == null)
        {
            return null;
        }

        if (encoded.Length > options.GetMaximumOutputBytes())
        {
            AddLimitFatal(encoded.Length);
            return null;
        }

        return new EmailCanonicalBody(encoded, GetEncodingName(selected));
    }

    EmailTransferEncodingKind SelectEncoding(MimeEntity entity, byte[]? decoded)
    {
        string mediaType = (entity.GetMediaType() ?? throw new InvalidOperationException()).GetNormalizedTopLevelType() + "/" + (entity.GetMediaType() ?? throw new InvalidOperationException()).GetNormalizedSubtype();
        EmailEncodingSelectionOptions selectionOptions = new EmailEncodingSelectionOptions();
        selectionOptions.SetMediaType(mediaType);
        selectionOptions.SetConstraint(options.GetTransportConstraint());
        selectionOptions.SetMaximumLineBytes(options.GetHeaderWriteOptions().GetMaximumLineLength());
        return new EmailEncodingSelector().ChooseWithInputAndOptions(decoded, selectionOptions).GetEncoding();
    }

    bool ValidateUnknownTransferBytes(byte[] value)
    {
        EmailTransferEncodingKind identity = EmailTransferEncodingKind.SevenBit;
        if (options.GetTransportConstraint() == EmailTransferEncodingConstraint.EightBit)
        {
            identity = EmailTransferEncodingKind.EightBit;
        }
        else if (options.GetTransportConstraint() == EmailTransferEncodingConstraint.Binary)
        {
            identity = EmailTransferEncodingKind.Binary;
        }

        EmailTransferValidationResult validation = new EmailTransferEncoding().ValidateWithInputAndKindAndMaximumBytesAndMaximumLineBytes(value, identity, options.GetMaximumOutputBytes(), options.GetHeaderWriteOptions().GetMaximumLineLength());
        if (!validation.IsSuccess())
        {
            AddFatal("Opaque content with an unknown transfer encoding violates the selected transport constraint.", "Supply safely encoded raw content or select a compatible transport constraint.");
            return false;
        }

        return true;
    }

    EmailHeaders? BuildHeaders(EmailHeaders? original, MimeEntity? entity, string? transferEncodingName, string? boundary, bool messageHeaders)
    {
        EmailHeaders headers = EmailWriteHeaderPolicy.Apply(original, options);
        if (entity == null)
        {
            return headers;
        }

        EmailValueFormatOptions valueOptions = new EmailValueFormatOptions();
        valueOptions.SetMaximumLineLength(options.GetHeaderWriteOptions().GetPreferredLineLength());
        int maximumValueBytes = options.GetHeaderWriteOptions().GetMaximumOutputBytes();
        valueOptions.SetMaximumOutputBytes(maximumValueBytes);
        EmailMediaType? outputMediaType = boundary == null ? entity.GetMediaType() : CreateBoundaryMediaType(entity.GetMediaType(), boundary);
        EmailValueFormatResult mediaType = new EmailValueWriter().WriteMediaTypeWithValueAndOptions(outputMediaType, valueOptions);
        issues.AddRange(mediaType.GetIssues());
        if (mediaType.HasFatalError())
        {
            return null;
        }

        ReplaceOrAdd(headers, "Content-Type", new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Content-Type", mediaType.GetBytes(), EmailHeaderValueKind.MediaType));
        if (transferEncodingName != null)
        {
            ReplaceOrAdd(headers, "Content-Transfer-Encoding", new EmailHeaderOperations().CreateWithNameAndRawValue("Content-Transfer-Encoding", Encoding.ASCII.GetBytes(transferEncodingName)));
        }

        if (messageHeaders)
        {
            ReplaceOrAdd(headers, "MIME-Version", new EmailHeaderOperations().CreateWithNameAndRawValue("MIME-Version", Encoding.ASCII.GetBytes("1.0")));
        }

        return headers;
    }

    EmailMediaType CreateBoundaryMediaType(EmailMediaType? original, string boundary)
    {
        List<EmailParameter> parameters = new List<EmailParameter>();
        EmailParameterList originalParameters = (original ?? throw new InvalidOperationException()).GetParameters();
        int index = 0;
        while (index < originalParameters.GetCount())
        {
            EmailParameter parameter = originalParameters.GetAt(index);
            if (!string.Equals(parameter.GetName(), "boundary", StringComparison.OrdinalIgnoreCase))
            {
                parameters.Add(parameter);
            }

            index++;
        }

        byte[] boundaryBytes = Encoding.ASCII.GetBytes(boundary);
        parameters.Add(new EmailParameter("boundary", boundaryBytes, boundaryBytes, false, new byte[0], original.GetSourcePosition()));
        List<EmailRfc2231DecodedParameter> decoded = new List<EmailRfc2231DecodedParameter>();
        index = 0;
        while (index < originalParameters.GetDecodedCount())
        {
            EmailRfc2231DecodedParameter parameter = originalParameters.GetDecodedAt(index);
            if (!string.Equals(parameter.GetName(), "boundary", StringComparison.OrdinalIgnoreCase))
            {
                decoded.Add(parameter);
            }

            index++;
        }

        EmailParameterList outputParameters = new EmailParameterList(parameters, decoded);
        return new EmailMediaType(original.GetTopLevelType(), original.GetSubtype(), outputParameters, original.GetRawBytes(), original.GetSourcePosition(), original.WasDefaulted());
    }

    byte[]? WriteHeaders(EmailHeaders headers)
    {
        EmailHeaderWriteOptions headerOptions = options.GetHeaderWriteOptions();
        headerOptions.SetIncludeSeparator(true);
        EmailHeaderWriteResult result = new EmailHeaderWriter().Write(headers, EmailHeaderWriteMode.Canonical, headerOptions);
        issues.AddRange(result.GetIssues());
        return result.HasFatalError() ? null : result.GetBytes();
    }

    byte[]? Materialize(EmailContent? content)
    {
        if (!(content ?? throw new InvalidOperationException()).HasKnownLength() || content.GetLength() > options.GetMaximumOutputBytes() || content.GetLength() > int.MaxValue)
        {
            AddLimitFatal(content.HasKnownLength() ? content.GetLength() : options.GetMaximumOutputBytes() + 1);
            return null;
        }

        return content.Materialize(content.GetLength());
    }

    byte[]? JoinBounded(byte[] first, byte[] second)
    {
        long length = (long)first.Length + second.Length;
        if (length > options.GetMaximumOutputBytes() || length > int.MaxValue)
        {
            AddLimitFatal(length);
            return null;
        }

        byte[] result = new byte[(int)length];
        Array.Copy(first, result, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }

    bool AppendBytes(List<byte> output, byte[] value)
    {
        long length = (long)output.Count + value.Length;
        if (length > options.GetMaximumOutputBytes() || length > int.MaxValue)
        {
            AddLimitFatal(length);
            return false;
        }

        output.AddRange(value);
        return true;
    }

    bool AppendAscii(List<byte> output, string value)
    {
        return AppendBytes(output, Encoding.ASCII.GetBytes(value));
    }

    bool EnsureListTerminalCrlf(List<byte> output)
    {
        if (output.Count == 0 || output.Count >= 2 && output[output.Count - 2] == 13 && output[output.Count - 1] == 10)
        {
            return true;
        }

        if (output[output.Count - 1] == 13)
        {
            return AppendBytes(output, new byte[] { 10 });
        }

        return AppendBytes(output, new byte[] { 13, 10 });
    }

    void AddCollisionContent(List<EmailContent> contents, byte[] value)
    {
        if (value.Length > 0)
        {
            contents.Add(new EmailContentOperations().CreateMemory(value, EmailContentOwnership.Borrowed, false));
        }
    }

    string GetContainerEncodingName()
    {
        if (options.GetTransportConstraint() == EmailTransferEncodingConstraint.EightBit)
        {
            return "8bit";
        }

        return options.GetTransportConstraint() == EmailTransferEncodingConstraint.Binary ? "binary" : "7bit";
    }

    bool ValidateContainerBytes(byte[] value, string transferEncoding)
    {
        EmailTransferEncodingKind kind = EmailTransferEncodingKind.SevenBit;
        if (transferEncoding == "8bit")
        {
            kind = EmailTransferEncodingKind.EightBit;
        }
        else if (transferEncoding == "binary")
        {
            kind = EmailTransferEncodingKind.Binary;
        }

        EmailTransferValidationResult validation = new EmailTransferEncoding().ValidateWithInputAndKindAndMaximumBytesAndMaximumLineBytes(value, kind, options.GetMaximumOutputBytes(), options.GetHeaderWriteOptions().GetMaximumLineLength());
        if (validation.IsSuccess())
        {
            return true;
        }

        AddFatal("Canonical multipart content violates the selected transport constraint.", "Use safe preamble, epilogue, and signed-content bytes or select a compatible transport constraint.");
        return false;
    }

    byte[]? EnsureTerminalCrlf(byte[] value)
    {
        if (value.Length >= 2 && value[value.Length - 2] == 13 && value[value.Length - 1] == 10)
        {
            return value;
        }

        if (value.Length > 0 && value[value.Length - 1] == 13)
        {
            if ((long)value.Length + 1 > options.GetMaximumOutputBytes() || (long)value.Length + 1 > int.MaxValue)
            {
                AddLimitFatal((long)value.Length + 1);
                return null;
            }

            byte[] completed = new byte[value.Length + 1];
            Array.Copy(value, completed, value.Length);
            completed[completed.Length - 1] = 10;
            return completed;
        }

        if ((long)value.Length + 2 > options.GetMaximumOutputBytes() || (long)value.Length + 2 > int.MaxValue)
        {
            AddLimitFatal((long)value.Length + 2);
            return null;
        }

        byte[] terminated = new byte[value.Length + 2];
        Array.Copy(value, terminated, value.Length);
        terminated[terminated.Length - 2] = 13;
        terminated[terminated.Length - 1] = 10;
        return terminated;
    }

    bool CheckDepth(int depth)
    {
        if (depth > options.GetMaximumNestingDepth())
        {
            AddFatal("Canonical writing exceeded the configured MIME nesting depth of " + options.GetMaximumNestingDepth() + ".", "Reduce nesting or increase the configured limit.");
            return false;
        }

        return true;
    }

    bool AddPart()
    {
        partCount++;
        if (partCount > options.GetMaximumPartCount())
        {
            AddFatal("Canonical writing exceeded the configured MIME part count of " + options.GetMaximumPartCount() + ".", "Reduce the entity count or increase the configured limit.");
            return false;
        }

        return true;
    }

    void AddLimitFatal(long observed)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.WritePolicyViolation, "Canonical writing would exceed the configured maximum output size of " + options.GetMaximumOutputBytes() + " bytes after observing " + observed + " bytes.", null, null, null, "No partial output was returned."));
    }

    void AddFatal(string description, string recovery)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.WritePolicyViolation, description, null, null, null, recovery));
    }

    static void ReplaceOrAdd(EmailHeaders headers, string name, EmailHeader replacement)
    {
        int replaced = headers.ReplaceAll(name, replacement);
        if (replaced == 0)
        {
            headers.Add(replacement);
        }
    }

    static string GetEncodingName(EmailTransferEncodingKind encoding)
    {
        if (encoding == EmailTransferEncodingKind.SevenBit)
        {
            return "7bit";
        }

        if (encoding == EmailTransferEncodingKind.EightBit)
        {
            return "8bit";
        }

        if (encoding == EmailTransferEncodingKind.Binary)
        {
            return "binary";
        }

        return encoding == EmailTransferEncodingKind.QuotedPrintable ? "quoted-printable" : "base64";
    }

    byte[]? NormalizeCrlf(byte[]? value)
    {
        List<byte> output = new List<byte>();
        int index = 0;
        while (index < (value ?? throw new InvalidOperationException()).Length)
        {
            byte current = value[index];
            if (current == 13)
            {
                if ((long)output.Count + 2 > options.GetMaximumOutputBytes())
                {
                    AddLimitFatal((long)output.Count + 2);
                    return null;
                }

                output.Add(13);
                output.Add(10);
                if (index + 1 < value.Length && value[index + 1] == 10)
                {
                    index++;
                }
            }
            else if (current == 10)
            {
                if ((long)output.Count + 2 > options.GetMaximumOutputBytes())
                {
                    AddLimitFatal((long)output.Count + 2);
                    return null;
                }

                output.Add(13);
                output.Add(10);
            }
            else
            {
                if ((long)output.Count + 1 > options.GetMaximumOutputBytes())
                {
                    AddLimitFatal((long)output.Count + 1);
                    return null;
                }

                output.Add(current);
            }

            index++;
        }

        return output.ToArray();
    }

    static byte[] Copy(byte[]? value)
    {
        byte[] copy = new byte[(value ?? throw new InvalidOperationException()).Length];
        Array.Copy(value, copy, value.Length);
        return copy;
    }
}
internal class EmailCanonicalBody
{
    byte[] bytes;
    string? transferEncodingName;
    string? boundary;

    internal EmailCanonicalBody(byte[] bytes, string? transferEncodingName)
    {
        this.bytes = bytes;
        this.transferEncodingName = transferEncodingName;
        boundary = null;
    }

    internal static EmailCanonicalBody CreateWithBytesAndTransferEncodingName(byte[] bytes, string? transferEncodingName)
    {
        return new EmailCanonicalBody(bytes, transferEncodingName);
    }

    internal static EmailCanonicalBody CreateWithBytesAndTransferEncodingNameAndBoundary(byte[] bytes, string transferEncodingName, string boundary)
    {
        EmailCanonicalBody value = new EmailCanonicalBody(bytes, transferEncodingName);
        value.boundary = boundary;
        return value;
    }

    internal byte[] GetBytes()
    {
        return bytes;
    }

    internal string? GetTransferEncodingName()
    {
        return transferEncodingName;
    }

    internal string? GetBoundary()
    {
        return boundary;
    }
}
