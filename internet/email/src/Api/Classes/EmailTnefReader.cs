using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Reads and interprets TNEF.
/// </summary>
public class EmailTnefReader : IEmailTnefReaderContract
{
    /// <summary>
    /// The TNEF signature value exposed by this API.
    /// </summary>
    internal const uint TnefSignature = 0x223E9F78;
    /// <summary>
    /// The TNEF version tag value exposed by this API.
    /// </summary>
    internal const uint TnefVersionTag = 0x00089006;
    /// <summary>
    /// The oem code page tag value exposed by this API.
    /// </summary>
    internal const uint OemCodePageTag = 0x00069007;
    /// <summary>
    /// The attachment rendering data tag value exposed by this API.
    /// </summary>
    internal const uint AttachmentRenderingDataTag = 0x00069002;
    /// <summary>
    /// The message properties tag value exposed by this API.
    /// </summary>
    internal const uint MessagePropertiesTag = 0x00069003;
    /// <summary>
    /// The recipient table tag value exposed by this API.
    /// </summary>
    internal const uint RecipientTableTag = 0x00069004;
    /// <summary>
    /// The attachment properties tag value exposed by this API.
    /// </summary>
    internal const uint AttachmentPropertiesTag = 0x00069005;
    /// <summary>
    /// The subject tag value exposed by this API.
    /// </summary>
    internal const uint SubjectTag = 0x00018004;
    /// <summary>
    /// The body tag value exposed by this API.
    /// </summary>
    internal const uint BodyTag = 0x0002800C;
    /// <summary>
    /// The message class tag value exposed by this API.
    /// </summary>
    internal const uint MessageClassTag = 0x00078008;
    /// <summary>
    /// The attachment data tag value exposed by this API.
    /// </summary>
    internal const uint AttachmentDataTag = 0x0006800F;
    /// <summary>
    /// The attachment title tag value exposed by this API.
    /// </summary>
    internal const uint AttachmentTitleTag = 0x00018010;
    /// <summary>
    /// The attachment transport filename tag value exposed by this API.
    /// </summary>
    internal const uint AttachmentTransportFilenameTag = 0x00069001;
    internal static EmailTnefParseResult ParseImplementationWithInputUsingReadOnlySpan(ReadOnlySpan<byte> input)
    {
        return ParseImplementationWithInputAndOptionsUsingReadOnlySpanAndEmailTnefReaderOptions(input, new EmailTnefReaderOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailTnefParseResult ParseWithInputUsingReadOnlySpan(ReadOnlySpan<byte> input)
    {
        return EmailTnefReader.ParseImplementationWithInputUsingReadOnlySpan(input);
    }

    internal static EmailTnefParseResult ParseImplementationWithInputAndOptionsUsingReadOnlySpanAndEmailTnefReaderOptions(ReadOnlySpan<byte> input, EmailTnefReaderOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        EmailTnefReaderOptions snapshot = options.Clone();
        byte[] value = input.ToArray();
        ValidateInputLength(value.LongLength, snapshot);
        return ParseBytes(value, snapshot);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailTnefParseResult ParseWithInputAndOptionsUsingReadOnlySpanAndEmailTnefReaderOptions(ReadOnlySpan<byte> input, EmailTnefReaderOptions options)
    {
        return EmailTnefReader.ParseImplementationWithInputAndOptionsUsingReadOnlySpanAndEmailTnefReaderOptions(input, options);
    }

    internal static EmailTnefParseResult ParseImplementationWithInputUsingStream(Stream input)
    {
        return ParseImplementationWithInputAndOptionsUsingStreamAndEmailTnefReaderOptions(input, new EmailTnefReaderOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailTnefParseResult ParseWithInputUsingStream(Stream input)
    {
        return EmailTnefReader.ParseImplementationWithInputUsingStream(input);
    }

    internal static EmailTnefParseResult ParseImplementationWithInputAndOptionsUsingStreamAndEmailTnefReaderOptions(Stream input, EmailTnefReaderOptions options)
    {
        if (input == null)
            throw new ArgumentNullException("input");
        if (!input.CanRead)
            throw new ArgumentException("The TNEF input stream must be readable.", "input");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailTnefReaderOptions snapshot = options.Clone();
        long maximum = snapshot.GetLimits().GetMaxEntryBytes();
        if (input.CanSeek)
        {
            long remaining = input.Length - input.Position;
            if (remaining < 0)
                throw new ArgumentException("The stream position exceeds its length.", "input");
            ValidateInputLength(remaining, snapshot);
        }

        byte[] buffer = new byte[snapshot.GetReadBufferBytes()];
        using (MemoryStream output = new MemoryStream())
        {
            while (true)
            {
                int count = input.Read(buffer, 0, buffer.Length);
                if (count == 0)
                    break;
                long observed = output.Length + count;
                if (observed > maximum)
                    throw new EmailLimitException("MaxTnefBytes", maximum, observed);
                output.Write(buffer, 0, count);
            }

            return ParseBytes(output.ToArray(), snapshot);
        }
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailTnefParseResult ParseWithInputAndOptionsUsingStreamAndEmailTnefReaderOptions(Stream input, EmailTnefReaderOptions options)
    {
        return EmailTnefReader.ParseImplementationWithInputAndOptionsUsingStreamAndEmailTnefReaderOptions(input, options);
    }

    internal static EmailTnefParseResult ParseImplementationWithEntity(MimeEntity entity)
    {
        return ParseImplementationWithEntityAndOptions(entity, new EmailTnefReaderOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailTnefParseResult ParseWithEntity(MimeEntity entity)
    {
        return EmailTnefReader.ParseImplementationWithEntity(entity);
    }

    internal static EmailTnefParseResult ParseImplementationWithEntityAndOptions(MimeEntity entity, EmailTnefReaderOptions options)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailIssueList issues = new EmailIssueList();
        if (entity.GetKind() != MimeEntityKind.Tnef)
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.TnefHeaderMalformed, "The MIME entity is not a supported TNEF media type.", 0, "No TNEF container was parsed."));
            return new EmailTnefParseResultOperations().CreateWithContainerAndIssuesAndComplete(null, issues, false);
        }

        if (!entity.HasContent())
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.TnefHeaderMalformed, "The TNEF MIME entity has no content.", 0, "No TNEF container was parsed."));
            return new EmailTnefParseResultOperations().CreateWithContainerAndIssuesAndComplete(null, issues, false);
        }

        EmailTnefReaderOptions snapshot = options.Clone();
        EmailContent? content = entity.HasDecodedContent() ? entity.GetDecodedContent() : entity.GetContent();
        byte[] bytes = (content ?? throw new InvalidOperationException()).Materialize(snapshot.GetLimits().GetMaxEntryBytes());
        return ParseBytes(bytes, snapshot);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailTnefParseResult ParseWithEntityAndOptions(MimeEntity entity, EmailTnefReaderOptions options)
    {
        return EmailTnefReader.ParseImplementationWithEntityAndOptions(entity, options);
    }

    static EmailTnefParseResult ParseBytes(byte[] bytes, EmailTnefReaderOptions options)
    {
        EmailIssueList issues = new EmailIssueList();
        if (bytes.Length < 6)
        {
            issues.Add(Issue(EmailIssueSeverity.Fatal, EmailIssueCode.TnefHeaderMalformed, "The TNEF stream is shorter than its six-byte container header.", bytes.Length, "The available bytes were left unparsed."));
            return new EmailTnefParseResultOperations().CreateWithContainerAndIssuesAndComplete(null, issues, false);
        }

        uint signature = ReadUInt32(bytes, 0);
        int legacyKey = ReadUInt16(bytes, 4);
        EmailTnefHeader header = new EmailTnefHeader(signature, legacyKey, new EmailRange(0, 6));
        List<EmailTnefAttribute> attributes = new List<EmailTnefAttribute>();
        if (signature != TnefSignature)
        {
            issues.Add(Issue(EmailIssueSeverity.Fatal, EmailIssueCode.TnefSignatureInvalid, "The stream does not begin with the TNEF signature 0x223E9F78.", 0, "The header and source bytes were retained, but attribute parsing was not attempted."));
            EmailTnefContainer invalid = new EmailTnefContainer(header, attributes, bytes, 0);
            return new EmailTnefParseResultOperations().CreateWithContainerAndIssuesAndComplete(invalid, issues, false);
        }

        if (legacyKey == 0)
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.TnefHeaderMalformed, "The TNEF legacy key is zero.", 4, "The key was retained and attribute parsing continued."));
        int offset = 6;
        int attachmentCount = 0;
        bool complete = true;
        EmailArchiveLimits limits = options.GetLimits();
        while (offset < bytes.Length)
        {
            if (attributes.Count >= limits.GetMaxTnefAttributeCount())
                throw new EmailLimitException("MaxTnefAttributeCount", limits.GetMaxTnefAttributeCount(), attributes.Count + 1);
            int attributeStart = offset;
            if (bytes.Length - offset < 9)
            {
                issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.TnefAttributeMalformed, "The final TNEF attribute header is truncated.", offset, "The complete prefix of the container was retained."));
                complete = false;
                break;
            }

            int rawLevel = bytes[offset];
            uint tag = ReadUInt32(bytes, offset + 1);
            long declaredLength = ReadUInt32(bytes, offset + 5);
            int dataStart = offset + 9;
            if (declaredLength > limits.GetMaxEntryBytes())
                throw new EmailLimitException("MaxTnefAttributeBytes", limits.GetMaxEntryBytes(), declaredLength);
            if (rawLevel != 1 && rawLevel != 2)
                issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.TnefAttributeMalformed, "A TNEF attribute uses an unknown level " + rawLevel + ".", offset, "The raw level, tag, and available data were retained."));
            long dataEnd = dataStart + declaredLength;
            int attachmentIndex = ResolveAttachmentIndex(rawLevel, tag, attachmentCount);
            if (rawLevel == 2 && tag == AttachmentRenderingDataTag)
                attachmentCount++;
            if (dataEnd > bytes.Length)
            {
                int retainedLength = bytes.Length - dataStart;
                byte[] retained = Copy(bytes, dataStart, retainedLength);
                int calculated = CalculateChecksum(retained);
                EmailTnefAttribute truncated = CreateAttribute(attributes.Count, rawLevel, tag, declaredLength, retained, -1, calculated, false, attachmentIndex, attributeStart, bytes.Length - attributeStart, dataStart, retainedLength, -1);
                attributes.Add(truncated);
                issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.TnefAttributeMalformed, "A TNEF attribute declares " + declaredLength + " data bytes, but only " + retainedLength + " remain.", attributeStart, "The partial attribute data was retained without treating trailing bytes as a checksum."));
                complete = false;
                offset = bytes.Length;
                break;
            }

            int length = (int)declaredLength;
            byte[] data = Copy(bytes, dataStart, length);
            int calculatedChecksum = CalculateChecksum(data);
            int checksumOffset = dataStart + length;
            if (bytes.Length - checksumOffset < 2)
            {
                EmailTnefAttribute missingChecksum = CreateAttribute(attributes.Count, rawLevel, tag, declaredLength, data, -1, calculatedChecksum, false, attachmentIndex, attributeStart, bytes.Length - attributeStart, dataStart, length, -1);
                attributes.Add(missingChecksum);
                issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.TnefAttributeMalformed, "A TNEF attribute is missing its two-byte checksum.", checksumOffset, "The complete attribute data was retained."));
                complete = false;
                offset = bytes.Length;
                break;
            }

            int checksum = ReadUInt16(bytes, checksumOffset);
            EmailTnefAttribute attribute = CreateAttribute(attributes.Count, rawLevel, tag, declaredLength, data, checksum, calculatedChecksum, true, attachmentIndex, attributeStart, checksumOffset + 2 - attributeStart, dataStart, length, checksumOffset);
            attributes.Add(attribute);
            if (checksum != calculatedChecksum)
            {
                EmailIssueSeverity severity = tag == MessageClassTag || !options.GetRequireValidChecksums() ? EmailIssueSeverity.Warning : EmailIssueSeverity.Error;
                issues.Add(Issue(severity, EmailIssueCode.TnefChecksumMismatch, "A TNEF attribute checksum is " + checksum + ", but the calculated 16-bit byte sum is " + calculatedChecksum + ".", checksumOffset, "The attribute and its stored checksum were retained."));
            }

            offset = checksumOffset + 2;
        }

        ValidateStructure(attributes, issues);
        EmailTnefContainer container = new EmailTnefContainer(header, attributes, bytes, attachmentCount);
        EmailTnefMessage message = EmailTnefSemanticParser.Parse(container, options, issues);
        return new EmailTnefParseResultOperations().CreateWithContainerAndMessageAndIssuesAndComplete(container, message, issues, complete);
    }

    static EmailTnefAttribute CreateAttribute(int index, int rawLevel, uint tag, long declaredLength, byte[] data, int checksum, int calculatedChecksum, bool complete, int attachmentIndex, int sourceStart, int sourceLength, int dataStart, int dataLength, int checksumStart)
    {
        EmailRange? checksumRange = checksumStart < 0 ? null : new EmailRange(checksumStart, 2);
        return new EmailTnefAttribute(index, rawLevel, tag, declaredLength, data, checksum, calculatedChecksum, complete, attachmentIndex, new EmailRange(sourceStart, sourceLength), new EmailRange(sourceStart, 9), new EmailRange(dataStart, dataLength), checksumRange);
    }

    static int ResolveAttachmentIndex(int rawLevel, uint tag, int attachmentCount)
    {
        if (rawLevel != 2)
            return -1;
        if (tag == AttachmentRenderingDataTag)
            return attachmentCount;
        return attachmentCount - 1;
    }

    static void ValidateStructure(List<EmailTnefAttribute> attributes, EmailIssueList issues)
    {
        if (attributes.Count == 0)
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.TnefRequiredAttributeMissing, "The TNEF stream contains no version or OEM code-page attributes.", 6, "The empty attribute sequence was retained."));
            return;
        }

        if (attributes[0].GetLevel() != EmailTnefAttributeLevel.Message || attributes[0].GetKind() != EmailTnefAttributeKind.TnefVersion)
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.TnefRequiredAttributeMissing, "The first TNEF attribute is not the required message-level version attribute.", attributes[0].GetSourceRange().GetStartOffset(), "All attributes were retained in source order."));
        }
        else if (attributes[0].GetDeclaredLength() != 4)
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.TnefStructureMalformed, "The TNEF version attribute is not four bytes long.", attributes[0].GetDataRange().GetStartOffset(), "The version attribute data was retained without reinterpretation."));
        }

        if (attributes.Count < 2 || attributes[1].GetLevel() != EmailTnefAttributeLevel.Message || attributes[1].GetKind() != EmailTnefAttributeKind.OemCodePage)
        {
            long position = attributes.Count < 2 ? attributes[0].GetSourceRange().GetEndOffsetExclusive() : attributes[1].GetSourceRange().GetStartOffset();
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.TnefRequiredAttributeMissing, "The second TNEF attribute is not the required message-level OEM code-page attribute.", position, "All attributes were retained in source order."));
        }
        else if (attributes[1].GetDeclaredLength() < 4)
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.TnefStructureMalformed, "The OEM code-page attribute contains fewer than four bytes.", attributes[1].GetDataRange().GetStartOffset(), "The code-page attribute data was retained without reinterpretation."));
        }

        bool attachmentPhase = false;
        int currentAttachment = -1;
        int index = 0;
        while (index < attributes.Count)
        {
            EmailTnefAttribute attribute = attributes[index];
            if (attribute.GetLevel() == EmailTnefAttributeLevel.Attachment)
            {
                if (!attachmentPhase)
                {
                    attachmentPhase = true;
                    if (attribute.GetKind() != EmailTnefAttributeKind.AttachmentRenderingData)
                        issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.TnefStructureMalformed, "Attachment-level attributes do not begin with attachment rendering data.", attribute.GetSourceRange().GetStartOffset(), "The ungrouped attachment attribute was retained."));
                }

                if (attribute.GetKind() == EmailTnefAttributeKind.AttachmentRenderingData)
                    currentAttachment++;
                else if (currentAttachment < 0)
                    issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.TnefStructureMalformed, "An attachment attribute appears before any attachment rendering-data attribute.", attribute.GetSourceRange().GetStartOffset(), "The ungrouped attachment attribute was retained."));
            }
            else if (attribute.GetLevel() == EmailTnefAttributeLevel.Message && attachmentPhase)
            {
                issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.TnefStructureMalformed, "A message-level attribute appears after attachment-level attributes.", attribute.GetSourceRange().GetStartOffset(), "The out-of-order attribute was retained."));
            }

            index++;
        }
    }

    static int CalculateChecksum(byte[] data)
    {
        int checksum = 0;
        int index = 0;
        while (index < data.Length)
        {
            checksum = (checksum + data[index]) & 65535;
            index++;
        }

        return checksum;
    }

    static uint ReadUInt32(byte[] value, int offset)
    {
        return (uint)value[offset] | (uint)value[offset + 1] << 8 | (uint)value[offset + 2] << 16 | (uint)value[offset + 3] << 24;
    }

    static int ReadUInt16(byte[] value, int offset)
    {
        return value[offset] | value[offset + 1] << 8;
    }

    static byte[] Copy(byte[] value, int offset, int count)
    {
        byte[] result = new byte[count];
        if (count > 0)
            Array.Copy(value, offset, result, 0, count);
        return result;
    }

    static void ValidateInputLength(long length, EmailTnefReaderOptions options)
    {
        long maximum = options.GetLimits().GetMaxEntryBytes();
        if (length > maximum)
            throw new EmailLimitException("MaxTnefBytes", maximum, length);
    }

    static EmailIssue Issue(EmailIssueSeverity severity, EmailIssueCode code, string description, long offset, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, new EmailSourcePosition(offset, 1, offset + 1), null, null, recovery);
    }
}
