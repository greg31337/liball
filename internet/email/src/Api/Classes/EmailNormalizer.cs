using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with normalizer.
/// </summary>
public class EmailNormalizer : IEmailNormalizerContract
{
    internal static EmailNormalizationResult NormalizeImplementationWithMessage(EmailMessage message)
    {
        return NormalizeImplementationWithMessageAndOptions(message, new EmailNormalizationOptions());
    }

    /// <summary>
    /// Normalizes the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The normalization result produced by the operation.</returns>
    public EmailNormalizationResult NormalizeWithMessage(EmailMessage message)
    {
        return EmailNormalizer.NormalizeImplementationWithMessage(message);
    }

    internal static EmailNormalizationResult NormalizeImplementationWithMessageAndOptions(EmailMessage message, EmailNormalizationOptions options)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        List<EmailNormalizationChange> changes = new List<EmailNormalizationChange>();
        EmailMessage normalized = NormalizeMessage(message, options.Clone(), changes, new EmailPathOperations().CreateRoot());
        if (changes.Count > 0)
        {
            normalized.MarkDirty();
        }
        else
        {
            normalized = message.Clone();
        }

        return new EmailNormalizationResult(normalized, null, changes);
    }

    /// <summary>
    /// Normalizes the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The normalization result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailNormalizationResult NormalizeWithMessageAndOptions(EmailMessage message, EmailNormalizationOptions options)
    {
        return EmailNormalizer.NormalizeImplementationWithMessageAndOptions(message, options);
    }

    internal static EmailNormalizationResult NormalizeImplementationWithEntity(MimeEntity entity)
    {
        return NormalizeImplementationWithEntityAndOptions(entity, new EmailNormalizationOptions());
    }

    /// <summary>
    /// Normalizes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The normalization result produced by the operation.</returns>
    public EmailNormalizationResult NormalizeWithEntity(MimeEntity entity)
    {
        return EmailNormalizer.NormalizeImplementationWithEntity(entity);
    }

    internal static EmailNormalizationResult NormalizeImplementationWithEntityAndOptions(MimeEntity entity, EmailNormalizationOptions options)
    {
        if (entity == null)
        {
            throw new ArgumentNullException("entity");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        List<EmailNormalizationChange> changes = new List<EmailNormalizationChange>();
        MimeEntity normalized = NormalizeEntity(entity, options.Clone(), changes, new EmailPathOperations().CreateRoot());
        if (changes.Count > 0)
        {
            normalized.MarkDirty();
        }
        else
        {
            normalized = entity.Clone();
        }

        return new EmailNormalizationResult(null, normalized, changes);
    }

    /// <summary>
    /// Normalizes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The normalization result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailNormalizationResult NormalizeWithEntityAndOptions(MimeEntity entity, EmailNormalizationOptions options)
    {
        return EmailNormalizer.NormalizeImplementationWithEntityAndOptions(entity, options);
    }

    static EmailMessage NormalizeMessage(EmailMessage? source, EmailNormalizationOptions options, List<EmailNormalizationChange> changes, EmailPath path)
    {
        int initialChanges = changes.Count;
        EmailHeaders headers = NormalizeHeaders((source ?? throw new InvalidOperationException()).GetHeaders(), options, changes, path);
        MimeEntity? body = source.HasBody() ? NormalizeEntity(source.GetBody(), options, changes, path) : null;
        if (body != null)
        {
            SynchronizeContentHeaders(headers, body.GetHeaders());
        }

        if (options.GetRepairMimeVersion())
        {
            EmailHeader? existing = headers.GetFirst("MIME-Version");
            string? previous = existing == null ? null : GetTrimmedValue(existing);
            if (existing == null || !string.Equals(previous, "1.0", StringComparison.Ordinal) || headers.GetCountNamed("MIME-Version") != 1)
            {
                ReplaceHeader(headers, "MIME-Version", "1.0", EmailHeaderValueKind.Unknown, options.GetNormalizeLineEndings());
                changes.Add(new EmailNormalizationChange(EmailNormalizationChangeKind.MimeVersion, path, "MIME-Version", previous, "1.0"));
            }
        }

        EmailMessage normalized = new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(headers, body, (source.GetEnvelope() ?? throw new InvalidOperationException()).Clone(), null, source.GetOwnIssues());
        if (changes.Count > initialChanges)
        {
            normalized.MarkDirty();
        }

        return normalized;
    }

    static MimeEntity NormalizeEntity(MimeEntity? source, EmailNormalizationOptions options, List<EmailNormalizationChange> changes, EmailPath path)
    {
        int initialChanges = changes.Count;
        EmailHeaders headers = NormalizeHeaders((source ?? throw new InvalidOperationException()).GetHeaders(), options, changes, path);
        List<MimeEntity> children = new List<MimeEntity>();
        int childIndex = 0;
        while (childIndex < source.GetChildCount())
        {
            children.Add(NormalizeEntity(source.GetChildAt(childIndex), options, changes, path.Append(childIndex)));
            childIndex++;
        }

        EmailMessage? nested = source.HasNestedMessage() ? NormalizeMessage(source.GetNestedMessage(), options, changes, path) : null;
        EmailMediaType? mediaType = source.GetMediaType();
        EmailDisposition? disposition = source.HasDisposition() ? source.GetDisposition() : null;
        string? transferEncoding = source.HasTransferEncoding() ? source.GetTransferEncodingName() : null;
        EmailContent? rawContent = source.HasRawContent() ? source.GetRawContent() : null;
        EmailContent? decodedContent = source.HasDecodedContent() ? source.GetDecodedContent() : null;
        bool mediaHeaderChanged = false;
        bool dispositionHeaderChanged = false;
        if (options.GetNormalizeCharsets())
        {
            EmailMediaType normalizedMediaType = NormalizeCharset(mediaType, changes, path);
            mediaHeaderChanged = !object.ReferenceEquals(mediaType, normalizedMediaType);
            mediaType = normalizedMediaType;
        }

        if (options.GetRepairContentTypes() && ((mediaType ?? throw new InvalidOperationException()).WasDefaulted() || !headers.Contains("Content-Type")))
        {
            string? previousType = headers.Contains("Content-Type") ? GetTrimmedValue(headers.GetFirst("Content-Type")) : null;
            mediaType = new EmailMediaType(mediaType.GetTopLevelType(), mediaType.GetSubtype(), mediaType.GetParameters(), mediaType.GetRawBytes(), mediaType.GetSourcePosition(), false);
            mediaHeaderChanged = true;
            changes.Add(new EmailNormalizationChange(EmailNormalizationChangeKind.ContentType, path, "Content-Type", previousType, GetMediaTypeText(mediaType)));
        }

        if (options.GetRepairBoundaries() && source.IsContainer())
        {
            EmailParameter? boundary = (mediaType ?? throw new InvalidOperationException()).GetParameters().GetFirst("boundary");
            string? previousBoundary = boundary == null ? null : Encoding.ASCII.GetString(boundary.GetValueBytes());
            if (!IsValidBoundary(previousBoundary))
            {
                string generated = GenerateBoundary(source, options, path);
                mediaType = ReplaceParameterWithMediaTypeAndNameAndValueAndUtf8(mediaType, "boundary", generated, false);
                mediaHeaderChanged = true;
                changes.Add(new EmailNormalizationChange(EmailNormalizationChangeKind.Boundary, path, "Content-Type", previousBoundary, generated));
            }
        }

        if (options.GetNormalizeFilenames())
        {
            string? filename = new EmailFilename().Decode(source);
            if (filename != null)
            {
                string sanitized = new EmailFilename().SanitizeWithFilename(filename).GetFilename();
                if (!string.Equals(filename, sanitized, StringComparison.Ordinal))
                {
                    if (disposition != null && disposition.GetParameters().GetDecoded("filename") != null)
                    {
                        disposition = ReplaceParameterWithDispositionAndNameAndValue(disposition, "filename", sanitized);
                        dispositionHeaderChanged = true;
                    }
                    else
                    {
                        mediaType = ReplaceParameterWithMediaTypeAndNameAndValueAndUtf8(mediaType, "name", sanitized, true);
                        mediaHeaderChanged = true;
                    }

                    changes.Add(new EmailNormalizationChange(EmailNormalizationChangeKind.Filename, path, disposition == null ? "Content-Type" : "Content-Disposition", filename, sanitized));
                }
            }
        }

        EmailNormalizationContent? normalizedContent = NormalizeContent(source, mediaType, options, changes, path);
        if (normalizedContent != null)
        {
            rawContent = normalizedContent.GetRawContent();
            decodedContent = normalizedContent.GetDecodedContent();
            transferEncoding = normalizedContent.GetTransferEncodingName();
        }

        if (mediaHeaderChanged)
        {
            ReplaceHeader(headers, "Content-Type", GetMediaTypeText(mediaType), EmailHeaderValueKind.MediaType, options.GetNormalizeLineEndings());
        }

        if (disposition != null && dispositionHeaderChanged)
        {
            ReplaceHeader(headers, "Content-Disposition", GetDispositionText(disposition), EmailHeaderValueKind.Disposition, options.GetNormalizeLineEndings());
        }

        if (options.GetRepairTransferEncodings() && normalizedContent != null)
        {
            ReplaceHeader(headers, "Content-Transfer-Encoding", transferEncoding, EmailHeaderValueKind.Unknown, options.GetNormalizeLineEndings());
        }

        MimeEntity normalized = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndPartialMessageMetadataAndExternalBodyMetadataAndBodyIngestMetadataAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(source.GetKind(), headers, mediaType, disposition, transferEncoding, source.HasContentId() ? source.GetContentId() : null, source.HasContentLocation() ? source.GetContentLocation() : null, source.HasContentLanguage() ? source.GetContentLanguage() : null, source.HasContentDescription() ? source.GetContentDescription() : null, source.HasPartialMessageMetadata() ? source.GetPartialMessageMetadata() : null, source.HasExternalBodyMetadata() ? source.GetExternalBodyMetadata() : null, source.HasBodyIngestMetadata() ? source.GetBodyIngestMetadata() : null, rawContent, decodedContent, children, nested, source.HasPreamble() ? source.GetPreamble() : null, source.HasEpilogue() ? source.GetEpilogue() : null, source.HasSourceRange() ? source.GetSourceRange() : null, source.GetOwnIssues());
        if (changes.Count > initialChanges)
        {
            normalized.MarkDirty();
        }

        return normalized;
    }

    static EmailNormalizationContent? NormalizeContent(MimeEntity source, EmailMediaType? mediaType, EmailNormalizationOptions options, List<EmailNormalizationChange> changes, EmailPath path)
    {
        if (source.IsContainer() || source.HasNestedMessage() || !source.HasContent())
        {
            return null;
        }

        bool repairEncoding = options.GetRepairTransferEncodings();
        bool identityRaw = source.HasRawContent() && (!source.HasTransferEncoding() || source.HasKnownTransferEncoding() && source.GetTransferEncodingKind() <= EmailTransferEncodingKind.Binary);
        bool normalizeNewlines = options.GetNormalizeLineEndings() && string.Equals((mediaType ?? throw new InvalidOperationException()).GetNormalizedTopLevelType(), "text", StringComparison.Ordinal) && (source.HasDecodedContent() || repairEncoding || identityRaw);
        if (!repairEncoding && !normalizeNewlines)
        {
            return null;
        }

        byte[] bytes;
        if (source.HasDecodedContent())
        {
            bytes = (source.GetDecodedContent() ?? throw new InvalidOperationException()).Materialize(options.GetMaximumContentBytes());
        }
        else
        {
            byte[] raw = (source.GetRawContent() ?? throw new InvalidOperationException()).Materialize(options.GetMaximumContentBytes());
            bytes = repairEncoding ? DecodeTransferContent(raw, source, options.GetMaximumContentBytes()) : raw;
        }

        bool contentChanged = false;
        if (normalizeNewlines)
        {
            byte[] normalizedBytes = NormalizeNewlineBytes(bytes);
            if (!Equal(bytes, normalizedBytes))
            {
                changes.Add(new EmailNormalizationChange(EmailNormalizationChangeKind.LineEnding, path, "body", null, "CRLF"));
                bytes = normalizedBytes;
                contentChanged = true;
            }
        }

        EmailContent content = new EmailContentOperations().CreateMemoryCopy(bytes, false);
        if (!repairEncoding)
        {
            return source.HasDecodedContent() ? new EmailNormalizationContent(null, content, source.HasTransferEncoding() ? source.GetTransferEncodingName() : null) : new EmailNormalizationContent(content, null, source.HasTransferEncoding() ? source.GetTransferEncodingName() : null);
        }

        string mediaName = (mediaType ?? throw new InvalidOperationException()).GetNormalizedTopLevelType() + "/" + mediaType.GetNormalizedSubtype();
        EmailEncodingSelectionResult selection = new EmailEncodingSelector().ChooseWithInputAndMediaTypeAndConstraint(bytes, mediaName, EmailTransferEncodingConstraint.SevenBit);
        string selected = GetTransferEncodingName(selection.GetEncoding());
        string? previous = source.HasTransferEncoding() ? source.GetTransferEncodingName() : null;
        bool encodedRaw = source.HasRawContent() && source.HasKnownTransferEncoding() && source.GetTransferEncodingKind() >= EmailTransferEncodingKind.QuotedPrintable;
        if (!string.Equals(previous, selected, StringComparison.Ordinal) || encodedRaw)
        {
            changes.Add(new EmailNormalizationChange(EmailNormalizationChangeKind.TransferEncoding, path, "Content-Transfer-Encoding", previous, selected));
            contentChanged = true;
        }

        return contentChanged ? new EmailNormalizationContent(null, content, selected) : null;
    }

    static byte[] DecodeTransferContent(byte[] raw, MimeEntity source, long maximumBytes)
    {
        if (!source.HasKnownTransferEncoding())
        {
            return raw;
        }

        EmailTransferEncodingKind kind = source.GetTransferEncodingKind();
        if (kind == EmailTransferEncodingKind.Base64)
        {
            return new EmailBase64().Decode(raw, EmailBase64DecodeMode.Tolerant, maximumBytes).GetDecodedBytes();
        }

        if (kind == EmailTransferEncodingKind.QuotedPrintable)
        {
            return new EmailQuotedPrintable().Decode(raw, EmailQuotedPrintableDecodeMode.Tolerant, maximumBytes).GetDecodedBytes();
        }

        return raw;
    }

    static EmailHeaders NormalizeHeaders(EmailHeaders? source, EmailNormalizationOptions options, List<EmailNormalizationChange> changes, EmailPath path)
    {
        EmailHeaders headers = (source ?? throw new InvalidOperationException()).Clone();
        int index = 0;
        while (index < headers.GetCount())
        {
            EmailHeader header = headers.GetAt(index);
            if (options.GetNormalizeLineEndings())
            {
                byte[] normalizedValue = NormalizeNewlineBytes(header.GetRawValue());
                if (header.GetLineEnding() != EmailLineEnding.CrLf || !Equal(header.GetRawValue(), normalizedValue))
                {
                    EmailHeader replacement = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(header.GetName(), normalizedValue, header.GetValueKind());
                    replacement.SetLineEnding(EmailLineEnding.CrLf);
                    headers.ReplaceAt(index, replacement);
                    header = replacement;
                    changes.Add(new EmailNormalizationChange(EmailNormalizationChangeKind.LineEnding, path, header.GetName(), null, "CRLF"));
                }
            }

            if (options.GetRefoldHeaders() && NeedsRefolding(header, options.GetPreferredHeaderLineLength()))
            {
                EmailHeader replacement = RefoldHeader(header, options.GetPreferredHeaderLineLength());
                headers.ReplaceAt(index, replacement);
                changes.Add(new EmailNormalizationChange(EmailNormalizationChangeKind.HeaderFolding, path, header.GetName(), null, options.GetPreferredHeaderLineLength().ToString()));
            }

            index++;
        }

        return headers;
    }

    static EmailHeader RefoldHeader(EmailHeader source, int preferredLength)
    {
        EmailHeader unfolded = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(source.GetName(), source.GetUnfoldedValue(), source.GetValueKind());
        EmailHeaders one = new EmailHeadersOperations().CreateWith();
        one.Add(unfolded);
        EmailHeaderWriteOptions writeOptions = new EmailHeaderWriteOptions();
        writeOptions.SetPreferredLineLength(preferredLength);
        writeOptions.SetCheckCardinality(false);
        writeOptions.SetCanonicalizeFieldNames(false);
        EmailHeaderWriteResult written = new EmailHeaderWriter().WriteCanonicalWithHeadersAndOptions(one, writeOptions);
        if (written.HasFatalError())
        {
            unfolded.SetLineEnding(EmailLineEnding.CrLf);
            return unfolded;
        }

        return new EmailHeaderBlockParser().ParseWithInput(written.GetBytes()).GetHeaders().GetAt(0);
    }

    static bool NeedsRefolding(EmailHeader header, int preferredLength)
    {
        return header.GetFoldCount() > 0 || header.GetNameBytes().Length + 2 + header.GetRawValue().Length > preferredLength;
    }

    static EmailMediaType NormalizeCharset(EmailMediaType? mediaType, List<EmailNormalizationChange> changes, EmailPath path)
    {
        EmailParameter? parameter = (mediaType ?? throw new InvalidOperationException()).GetParameters().GetFirst("charset");
        if (parameter == null)
        {
            return mediaType;
        }

        string current = Encoding.ASCII.GetString(parameter.GetValueBytes());
        EmailCharsetLookupResult lookup = new EmailCharsetRegistry().Lookup(current);
        if (!lookup.IsFound() || string.Equals(current, lookup.GetCanonicalName(), StringComparison.Ordinal))
        {
            return mediaType;
        }

        changes.Add(new EmailNormalizationChange(EmailNormalizationChangeKind.Charset, path, "Content-Type", current, lookup.GetCanonicalName()));
        return ReplaceParameterWithMediaTypeAndNameAndValueAndUtf8(mediaType, "charset", lookup.GetCanonicalName(), false);
    }

    static EmailMediaType ReplaceParameterWithMediaTypeAndNameAndValueAndUtf8(EmailMediaType? mediaType, string name, string? value, bool utf8)
    {
        EmailParameterList parameters = ReplaceParameterWithOriginalAndNameAndValueAndUtf8((mediaType ?? throw new InvalidOperationException()).GetParameters(), name, value, utf8);
        return new EmailMediaType(mediaType.GetTopLevelType(), mediaType.GetSubtype(), parameters, new byte[0], mediaType.GetSourcePosition(), false);
    }

    static EmailDisposition ReplaceParameterWithDispositionAndNameAndValue(EmailDisposition disposition, string name, string value)
    {
        EmailParameterList parameters = ReplaceParameterWithOriginalAndNameAndValueAndUtf8(disposition.GetParameters(), name, value, true);
        return new EmailDisposition(disposition.GetDispositionType(), parameters, new byte[0], disposition.GetSourcePosition());
    }

    static EmailParameterList ReplaceParameterWithOriginalAndNameAndValueAndUtf8(EmailParameterList original, string name, string? value, bool utf8)
    {
        List<EmailParameter> physical = new List<EmailParameter>();
        List<EmailRfc2231DecodedParameter> decoded = new List<EmailRfc2231DecodedParameter>();
        int index = 0;
        while (index < original.GetCount())
        {
            if (!string.Equals(original.GetAt(index).GetName(), name, StringComparison.OrdinalIgnoreCase))
            {
                physical.Add(original.GetAt(index));
            }

            index++;
        }

        index = 0;
        while (index < original.GetDecodedCount())
        {
            if (!string.Equals(original.GetDecodedAt(index).GetName(), name, StringComparison.OrdinalIgnoreCase))
            {
                decoded.Add(original.GetDecodedAt(index));
            }

            index++;
        }

        byte[] bytes = utf8 ? Encoding.UTF8.GetBytes((value ?? throw new InvalidOperationException())) : Encoding.ASCII.GetBytes((value ?? throw new InvalidOperationException()));
        EmailSourcePosition position = new EmailSourcePosition(0, 1, 1);
        physical.Add(new EmailParameter(name, bytes, bytes, true, new byte[0], position));
        decoded.Add(new EmailRfc2231DecodedParameter(name, bytes, utf8 ? "utf-8" : null, utf8, null, false, utf8, false, 1, new List<EmailRfc2231Parameter>()));
        return new EmailParameterList(physical, decoded);
    }

    static string GenerateBoundary(MimeEntity entity, EmailNormalizationOptions options, EmailPath path)
    {
        List<EmailContent> contents = new List<EmailContent>();
        CollectRepeatableContent(entity, contents);
        EmailWriteOptions writeOptions = new EmailWriteOptions();
        writeOptions.SetDeterministicOutput(true);
        writeOptions.SetDeterministicSeed(CreateBoundarySeed(options.GetDeterministicBoundarySeed(), path));
        return new EmailBoundaryGenerator().GenerateWithContentsAndOptions(contents, writeOptions).GetBoundary();
    }

    static void CollectRepeatableContent(MimeEntity entity, List<EmailContent> contents)
    {
        if (entity.HasContent() && (entity.GetContent() ?? throw new InvalidOperationException()).IsRepeatable())
        {
            contents.Add((entity.GetContent() ?? throw new InvalidOperationException()));
        }

        int index = 0;
        while (index < entity.GetChildCount())
        {
            CollectRepeatableContent(entity.GetChildAt(index), contents);
            index++;
        }
    }

    static byte[] CreateBoundarySeed(byte[] seed, EmailPath path)
    {
        byte[] combined = new byte[seed.Length + path.GetDepth() * 4 + 4];
        seed.CopyTo(combined, 0);
        int index = 0;
        while (index < path.GetDepth())
        {
            int value = path.GetAt(index);
            int offset = seed.Length + index * 4;
            combined[offset] = (byte)(value >> 24 & 255);
            combined[offset + 1] = (byte)(value >> 16 & 255);
            combined[offset + 2] = (byte)(value >> 8 & 255);
            combined[offset + 3] = (byte)(value & 255);
            index++;
        }

        return combined;
    }

    static bool IsValidBoundary(string? value)
    {
        if (value == null || value.Length < 1 || value.Length > 70 || value[value.Length - 1] == ' ')
        {
            return false;
        }

        const string allowed = "'()+_,-./:=? ";
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current > 127 || !char.IsLetterOrDigit(current) && allowed.IndexOf(current) < 0)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static void SynchronizeContentHeaders(EmailHeaders messageHeaders, EmailHeaders? entityHeaders)
    {
        string[] names = new string[]
        {
            "Content-Type",
            "Content-Transfer-Encoding",
            "Content-Disposition",
            "Content-ID",
            "Content-Location",
            "Content-Language",
            "Content-Description"
        };
        int index = 0;
        while (index < names.Length)
        {
            List<EmailHeader> replacements = (entityHeaders ?? throw new InvalidOperationException()).GetAllNamed(names[index]);
            if (messageHeaders.Contains(names[index]))
            {
                messageHeaders.ReplaceAllWithHeaders(names[index], replacements);
            }
            else
            {
                int replacementIndex = 0;
                while (replacementIndex < replacements.Count)
                {
                    messageHeaders.Add(replacements[replacementIndex]);
                    replacementIndex++;
                }
            }

            index++;
        }
    }

    static void ReplaceHeader(EmailHeaders headers, string name, string? value, EmailHeaderValueKind kind, bool crlf)
    {
        EmailHeader replacement = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, Encoding.UTF8.GetBytes((value ?? throw new InvalidOperationException())), kind);
        if (crlf)
        {
            replacement.SetLineEnding(EmailLineEnding.CrLf);
        }

        if (headers.Contains(name))
        {
            headers.ReplaceAll(name, replacement);
        }
        else
        {
            headers.Add(replacement);
        }
    }

    static string GetMediaTypeText(EmailMediaType? value)
    {
        return Encoding.UTF8.GetString(new EmailValueWriter().WriteMediaTypeWithValue(value).GetBytes());
    }

    static string GetDispositionText(EmailDisposition value)
    {
        return Encoding.UTF8.GetString(new EmailValueWriter().WriteDispositionWithValue(value).GetBytes());
    }

    static string GetTrimmedValue(EmailHeader? header)
    {
        return Encoding.ASCII.GetString((header ?? throw new InvalidOperationException()).GetUnfoldedValue()).Trim();
    }

    static string GetTransferEncodingName(EmailTransferEncodingKind kind)
    {
        if (kind == EmailTransferEncodingKind.SevenBit)
        {
            return "7bit";
        }

        if (kind == EmailTransferEncodingKind.EightBit)
        {
            return "8bit";
        }

        if (kind == EmailTransferEncodingKind.Binary)
        {
            return "binary";
        }

        return kind == EmailTransferEncodingKind.QuotedPrintable ? "quoted-printable" : "base64";
    }

    static byte[] NormalizeNewlineBytes(byte[] value)
    {
        List<byte> output = new List<byte>();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == 13)
            {
                output.Add(13);
                output.Add(10);
                if (index + 1 < value.Length && value[index + 1] == 10)
                {
                    index++;
                }
            }
            else if (value[index] == 10)
            {
                output.Add(13);
                output.Add(10);
            }
            else
            {
                output.Add(value[index]);
            }

            index++;
        }

        return output.ToArray();
    }

    static bool Equal(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }
}
