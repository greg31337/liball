using System;
using System.Collections.Generic;
using System.IO;

namespace email;
internal class EmailWriterWorker
{
    EmailMessage message;
    EmailWriteOptions options;
    EmailIssueList issues;
    List<EmailWriteReplacement> replacements;

    internal EmailWriterWorker(EmailMessage message, EmailWriteOptions options)
    {
        this.message = message;
        this.options = options;
        issues = new EmailIssueList();
        replacements = new List<EmailWriteReplacement>();
    }

    internal EmailWriteResult Write()
    {
        if (options.IsCanonical())
        {
            EmailCanonicalWriterWorker canonical = new EmailCanonicalWriterWorker(options);
            byte[]? canonicalBytes = canonical.WriteMessage(message);
            issues.AddRange(canonical.GetIssues());
            long canonicalSourceBytes = message.HasSourceInfo() ? ((message.GetSourceInfo() ?? throw new InvalidOperationException()).GetMessageRange() ?? throw new InvalidOperationException()).GetLength() : 0;
            if (canonicalBytes == null)
            {
                return CreateFailure(canonicalSourceBytes);
            }

            return new EmailWriteResult(options.GetMode(), canonicalBytes, issues, canonicalSourceBytes, 0, canonicalBytes.Length, 0, 1);
        }

        if (!message.HasSourceInfo())
        {
            return Fail("The selected write mode requires preserved message source information.", "Parse with source preservation or select canonical generation.");
        }

        EmailSourceInfo? sourceInfo = message.GetSourceInfo();
        EmailRange? messageRange = (sourceInfo ?? throw new InvalidOperationException()).GetMessageRange();
        if (options.IsExactPreservation())
        {
            if (!options.GetIncludeBlindRecipientHeaders() && EmailWriteHeaderPolicy.HasBlindRecipientHeaders(message.GetHeaders()))
            {
                return Fail("Exact preservation cannot omit blind-recipient headers.", "Use minimal regeneration or explicitly allow blind-recipient headers for exact archival output.");
            }

            if (message.IsExactDirty())
            {
                return Fail("Exact preservation cannot write a message whose byte representation is dirty.", "Use minimal regeneration or restore the original message state.");
            }

            return CopyExact(sourceInfo, messageRange);
        }

        if (!PrepareHeaderReplacement(sourceInfo) || !PrepareBodyReplacements(sourceInfo))
        {
            return CreateFailure((messageRange ?? throw new InvalidOperationException()).GetLength());
        }

        SortReplacements();
        if (!ValidateReplacements(messageRange) || !ValidateCleanGaps(messageRange))
        {
            return CreateFailure((messageRange ?? throw new InvalidOperationException()).GetLength());
        }

        return Assemble(sourceInfo, messageRange);
    }

    EmailWriteResult CopyExact(EmailSourceInfo sourceInfo, EmailRange? range)
    {
        if (!CanProduce((range ?? throw new InvalidOperationException()).GetLength()))
        {
            return CreateFailure(range.GetLength());
        }

        byte[] bytes = MaterializeRange(sourceInfo.GetContent(), range);
        return new EmailWriteResult(options.GetMode(), bytes, issues, range.GetLength(), bytes.Length, 0, bytes.Length == 0 ? 0 : 1, 0);
    }

    bool PrepareHeaderReplacement(EmailSourceInfo sourceInfo)
    {
        bool stripBlindRecipients = !options.GetIncludeBlindRecipientHeaders() && EmailWriteHeaderPolicy.HasBlindRecipientHeaders(message.GetHeaders());
        if (!sourceInfo.HasHeaderBlockRange())
        {
            if (stripBlindRecipients || (message.GetHeaders() ?? throw new InvalidOperationException()).GetCount() > 0 && message.IsExactDirty())
            {
                AddFatal("Minimal regeneration cannot locate the original header block.", "Use canonical generation for a message without complete header source ranges.");
                return false;
            }

            return true;
        }

        EmailRange? headerRange = sourceInfo.GetHeaderBlockRange();
        if (stripBlindRecipients)
        {
            EmailHeaderWriteOptions strippedOptions = options.GetHeaderWriteOptions();
            strippedOptions.SetIncludeSeparator(false);
            EmailHeaders strippedHeaders = EmailWriteHeaderPolicy.Apply(message.GetHeaders(), options);
            EmailHeaderWriteResult strippedResult = new EmailHeaderWriter().Write(strippedHeaders, EmailHeaderWriteMode.Preserving, strippedOptions);
            issues.AddRange(strippedResult.GetIssues());
            if (strippedResult.HasFatalError())
            {
                return false;
            }

            replacements.Add(new EmailWriteReplacement(headerRange, strippedResult.GetBytes()));
        }
        else if (!message.IsRangeClean(headerRange))
        {
            if (CanReplaceHeadersIndividually(headerRange))
            {
                int index = 0;
                while (index < (message.GetHeaders() ?? throw new InvalidOperationException()).GetCount())
                {
                    EmailHeader header = (message.GetHeaders() ?? throw new InvalidOperationException()).GetAt(index);
                    EmailRange? range = header.GetSourceRange();
                    if (!message.IsRangeClean(range))
                    {
                        byte[]? bytes = WriteHeader(header);
                        if (bytes == null)
                        {
                            return false;
                        }

                        replacements.Add(new EmailWriteReplacement(range, bytes));
                    }

                    index++;
                }
            }
            else
            {
                EmailHeaderWriteOptions headerOptions = options.GetHeaderWriteOptions();
                headerOptions.SetIncludeSeparator(false);
                EmailHeaderWriteResult headerResult = new EmailHeaderWriter().Write(message.GetHeaders(), EmailHeaderWriteMode.Preserving, headerOptions);
                issues.AddRange(headerResult.GetIssues());
                if (headerResult.HasFatalError())
                {
                    return false;
                }

                replacements.Add(new EmailWriteReplacement(headerRange, headerResult.GetBytes()));
            }
        }

        if (sourceInfo.HasSeparatorRange() && !message.IsRangeClean(sourceInfo.GetSeparatorRange()))
        {
            replacements.Add(new EmailWriteReplacement(sourceInfo.GetSeparatorRange(), new byte[] { 13, 10 }));
        }

        return true;
    }

    bool CanReplaceHeadersIndividually(EmailRange? headerRange)
    {
        long offset = (headerRange ?? throw new InvalidOperationException()).GetStartOffset();
        int index = 0;
        while (index < (message.GetHeaders() ?? throw new InvalidOperationException()).GetCount())
        {
            EmailHeader header = (message.GetHeaders() ?? throw new InvalidOperationException()).GetAt(index);
            if (!header.HasSourceRange())
            {
                return false;
            }

            EmailRange? range = header.GetSourceRange();
            if (!headerRange.ContainsRange(range) || (range ?? throw new InvalidOperationException()).GetStartOffset() != offset)
            {
                return false;
            }

            offset = range.GetEndOffsetExclusive();
            index++;
        }

        return offset == headerRange.GetEndOffsetExclusive();
    }

    byte[]? WriteHeader(EmailHeader header)
    {
        List<EmailHeader> values = new List<EmailHeader>();
        values.Add(header);
        EmailHeaderWriteOptions headerOptions = options.GetHeaderWriteOptions();
        headerOptions.SetIncludeSeparator(false);
        EmailHeaderWriteResult result = new EmailHeaderWriter().Write(new EmailHeadersOperations().CreateWithPreservedHeaders(values), EmailHeaderWriteMode.Preserving, headerOptions);
        issues.AddRange(result.GetIssues());
        return result.HasFatalError() ? null : result.GetBytes();
    }

    bool PrepareBodyReplacements(EmailSourceInfo sourceInfo)
    {
        if (!sourceInfo.HasBodyRange() || !message.HasBody())
        {
            return true;
        }

        EmailRange? bodyRange = sourceInfo.GetBodyRange();
        if (message.IsRangeClean(bodyRange))
        {
            return true;
        }

        MimeEntity? root = message.GetBody();
        List<EmailRange> dirtyRanges = (root ?? throw new InvalidOperationException()).GetDirtySourceRanges();
        if (dirtyRanges.Count == 0)
        {
            byte[]? body = WriteRootBody(root);
            if (body == null)
            {
                return false;
            }

            replacements.Add(new EmailWriteReplacement(bodyRange, body));
            return true;
        }

        int index = 0;
        while (index < dirtyRanges.Count)
        {
            EmailRange range = dirtyRanges[index];
            MimeEntity? entity = FindEntity(root, range);
            if (entity == null)
            {
                AddFatal("A dirty MIME source range does not identify an entity in the current tree.", "Use canonical generation after structural MIME changes.");
                return false;
            }

            byte[]? bytes = object.ReferenceEquals(entity, root) ? WriteRootBody(entity) : WriteEntity(entity);
            if (bytes == null)
            {
                return false;
            }

            replacements.Add(new EmailWriteReplacement(range, bytes));
            index++;
        }

        return true;
    }

    byte[]? WriteRootBody(MimeEntity entity)
    {
        if (entity.IsContainer() || entity.GetKind() == MimeEntityKind.Message)
        {
            AddFatal("Minimal regeneration cannot rebuild a locally changed MIME container or nested message yet.", "Use an unchanged source range or canonical MIME generation.");
            return null;
        }

        return WriteDirectContent(entity);
    }

    byte[]? WriteEntity(MimeEntity entity)
    {
        if (entity.IsContainer() || entity.GetKind() == MimeEntityKind.Message)
        {
            AddFatal("Minimal regeneration cannot rebuild a locally changed MIME container or nested message yet.", "Use an unchanged source range or canonical MIME generation.");
            return null;
        }

        EmailHeaderWriteOptions headerOptions = options.GetHeaderWriteOptions();
        headerOptions.SetIncludeSeparator(true);
        EmailHeaderWriteResult headerResult = new EmailHeaderWriter().Write(entity.GetHeaders(), EmailHeaderWriteMode.Preserving, headerOptions);
        issues.AddRange(headerResult.GetIssues());
        if (headerResult.HasFatalError())
        {
            return null;
        }

        byte[]? content = WriteDirectContent(entity);
        if (content == null)
        {
            return null;
        }

        long combinedLength = (long)headerResult.GetByteCount() + content.Length;
        if (combinedLength > options.GetMaximumOutputBytes() || combinedLength > int.MaxValue)
        {
            AddLimitFatal(combinedLength);
            return null;
        }

        return Join(headerResult.GetBytes(), content);
    }

    byte[]? WriteDirectContent(MimeEntity entity)
    {
        if (!entity.HasContent())
        {
            AddFatal("The dirty MIME entity has no retained content to write.", "Supply raw or decoded content before minimal regeneration.");
            return null;
        }

        if (!entity.HasRawContent())
        {
            if (entity.HasTransferEncoding() && !entity.HasKnownTransferEncoding())
            {
                AddFatal("Decoded replacement content cannot be written with an unknown transfer encoding.", "Supply replacement raw content or select a known transfer encoding.");
                return null;
            }

            EmailTransferEncodingKind encoding = entity.GetEffectiveTransferEncodingKind();
            EmailContent? decoded = entity.GetDecodedContent();
            if (!(decoded ?? throw new InvalidOperationException()).HasKnownLength() || decoded.GetLength() > options.GetMaximumOutputBytes() || decoded.GetLength() > int.MaxValue)
            {
                AddLimitFatal(decoded.HasKnownLength() ? decoded.GetLength() : options.GetMaximumOutputBytes() + 1);
                return null;
            }

            byte[] decodedBytes = decoded.Materialize(decoded.GetLength());
            if (encoding == EmailTransferEncodingKind.QuotedPrintable)
            {
                return new EmailQuotedPrintable().EncodeWithInput(decodedBytes);
            }

            if (encoding == EmailTransferEncodingKind.Base64)
            {
                return new EmailBase64().EncodeWithInput(decodedBytes);
            }

            return NormalizeCrlf(decodedBytes);
        }

        EmailContent? content = entity.HasRawContent() ? entity.GetRawContent() : entity.GetDecodedContent();
        if (!(content ?? throw new InvalidOperationException()).HasKnownLength() || content.GetLength() > options.GetMaximumOutputBytes() || content.GetLength() > int.MaxValue)
        {
            AddLimitFatal(content.HasKnownLength() ? content.GetLength() : options.GetMaximumOutputBytes() + 1);
            return null;
        }

        return content.Materialize(content.GetLength());
    }

    static byte[] NormalizeCrlf(byte[] value)
    {
        List<byte> output = new List<byte>();
        int index = 0;
        while (index < value.Length)
        {
            byte current = value[index];
            if (current == 13)
            {
                output.Add(13);
                output.Add(10);
                if (index + 1 < value.Length && value[index + 1] == 10)
                {
                    index++;
                }
            }
            else if (current == 10)
            {
                output.Add(13);
                output.Add(10);
            }
            else
            {
                output.Add(current);
            }

            index++;
        }

        return output.ToArray();
    }

    MimeEntity? FindEntity(MimeEntity current, EmailRange range)
    {
        if (current.HasSourceRange() && (current.GetSourceRange() ?? throw new InvalidOperationException()).EqualsRange(range))
        {
            return current;
        }

        int index = 0;
        while (index < current.GetChildCount())
        {
            MimeEntity? found = FindEntity(current.GetChildAt(index), range);
            if (found != null)
            {
                return found;
            }

            index++;
        }

        return null;
    }

    bool ValidateReplacements(EmailRange? messageRange)
    {
        long end = (messageRange ?? throw new InvalidOperationException()).GetStartOffset();
        int index = 0;
        while (index < replacements.Count)
        {
            EmailRange? range = replacements[index].GetRange();
            if (!messageRange.ContainsRange(range) || (range ?? throw new InvalidOperationException()).GetStartOffset() < end)
            {
                AddFatal("Dirty source ranges overlap or fall outside the message source range.", "Use canonical generation after overlapping or structural edits.");
                return false;
            }

            end = range.GetEndOffsetExclusive();
            index++;
        }

        return true;
    }

    bool ValidateCleanGaps(EmailRange? messageRange)
    {
        long current = (messageRange ?? throw new InvalidOperationException()).GetStartOffset();
        int index = 0;
        while (index < replacements.Count)
        {
            EmailRange? replacementRange = replacements[index].GetRange();
            if (!IsCleanGap(current, (replacementRange ?? throw new InvalidOperationException()).GetStartOffset()))
            {
                return false;
            }

            current = replacementRange.GetEndOffsetExclusive();
            index++;
        }

        return IsCleanGap(current, messageRange.GetEndOffsetExclusive());
    }

    bool IsCleanGap(long start, long end)
    {
        if (end <= start)
        {
            return true;
        }

        if (!message.IsRangeClean(new EmailRange(start, end - start)))
        {
            AddFatal("Minimal regeneration cannot map every dirty byte range to regenerated model data.", "Use canonical generation after explicit or unsupported source-range edits.");
            return false;
        }

        return true;
    }

    EmailWriteResult Assemble(EmailSourceInfo sourceInfo, EmailRange? messageRange)
    {
        long outputLength = (messageRange ?? throw new InvalidOperationException()).GetLength();
        int index = 0;
        while (index < replacements.Count)
        {
            EmailWriteReplacement replacement = replacements[index];
            outputLength -= (replacement.GetRange() ?? throw new InvalidOperationException()).GetLength();
            outputLength += replacement.GetBytes().Length;
            index++;
        }

        if (!CanProduce(outputLength))
        {
            return CreateFailure(messageRange.GetLength());
        }

        byte[] source = MaterializeRange(sourceInfo.GetContent(), messageRange);
        byte[] output = new byte[(int)outputLength];
        long absoluteOffset = messageRange.GetStartOffset();
        int outputOffset = 0;
        long copiedBytes = 0;
        long regeneratedBytes = 0;
        int copiedRanges = 0;
        index = 0;
        while (index < replacements.Count)
        {
            EmailWriteReplacement replacement = replacements[index];
            int copyLength = (int)((replacement.GetRange() ?? throw new InvalidOperationException()).GetStartOffset() - absoluteOffset);
            if (copyLength > 0)
            {
                int sourceOffset = (int)(absoluteOffset - messageRange.GetStartOffset());
                Array.Copy(source, sourceOffset, output, outputOffset, copyLength);
                outputOffset += copyLength;
                copiedBytes += copyLength;
                copiedRanges++;
            }

            byte[] replacementBytes = replacement.GetBytes();
            if (replacementBytes.Length > 0)
            {
                Array.Copy(replacementBytes, 0, output, outputOffset, replacementBytes.Length);
                outputOffset += replacementBytes.Length;
                regeneratedBytes += replacementBytes.Length;
            }

            absoluteOffset = (replacement.GetRange() ?? throw new InvalidOperationException()).GetEndOffsetExclusive();
            index++;
        }

        int finalLength = (int)(messageRange.GetEndOffsetExclusive() - absoluteOffset);
        if (finalLength > 0)
        {
            int sourceOffset = (int)(absoluteOffset - messageRange.GetStartOffset());
            Array.Copy(source, sourceOffset, output, outputOffset, finalLength);
            copiedBytes += finalLength;
            copiedRanges++;
        }

        return new EmailWriteResult(options.GetMode(), output, issues, messageRange.GetLength(), copiedBytes, regeneratedBytes, copiedRanges, replacements.Count);
    }

    bool CanProduce(long byteCount)
    {
        if (byteCount < 0 || byteCount > options.GetMaximumOutputBytes() || byteCount > int.MaxValue)
        {
            AddLimitFatal(byteCount);
            return false;
        }

        return true;
    }

    void AddLimitFatal(long observed)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.WritePolicyViolation, "Writing would exceed the configured maximum output size of " + options.GetMaximumOutputBytes() + " bytes after observing " + observed + " bytes.", null, null, null, "No partial output was returned."));
    }

    void AddFatal(string description, string recovery)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.WritePolicyViolation, description, null, null, null, recovery));
    }

    EmailWriteResult Fail(string description, string recovery)
    {
        AddFatal(description, recovery);
        long sourceBytes = message.HasSourceInfo() ? ((message.GetSourceInfo() ?? throw new InvalidOperationException()).GetMessageRange() ?? throw new InvalidOperationException()).GetLength() : 0;
        return CreateFailure(sourceBytes);
    }

    EmailWriteResult CreateFailure(long sourceBytes)
    {
        return new EmailWriteResult(options.GetMode(), new byte[0], issues, sourceBytes, 0, 0, 0, 0);
    }

    void SortReplacements()
    {
        int index = 1;
        while (index < replacements.Count)
        {
            EmailWriteReplacement value = replacements[index];
            int insertion = index;
            while (insertion > 0 && (replacements[insertion - 1].GetRange() ?? throw new InvalidOperationException()).GetStartOffset() > (value.GetRange() ?? throw new InvalidOperationException()).GetStartOffset())
            {
                replacements[insertion] = replacements[insertion - 1];
                insertion--;
            }

            replacements[insertion] = value;
            index++;
        }
    }

    byte[] MaterializeRange(EmailContent? content, EmailRange range)
    {
        EmailContent slice = new EmailContentOperations().CreateSlice(content, range.GetStartOffset(), range.GetLength());
        return slice.Materialize(range.GetLength());
    }

    static byte[] Join(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, result, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }
}
internal class EmailWriteReplacement
{
    EmailRange? range;
    byte[] bytes;

    internal EmailWriteReplacement(EmailRange? range, byte[] bytes)
    {
        this.range = range;
        this.bytes = bytes;
    }

    internal EmailRange? GetRange()
    {
        return range;
    }

    internal byte[] GetBytes()
    {
        return bytes;
    }
}
