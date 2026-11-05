using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace email;
internal class EmailRedactorWorker
{
    EmailRedactionOptions options;
    List<EmailRedactionChange> changes;
    Dictionary<string, EmailAddress> addressMap;
    Dictionary<string, EmailMessageId> identifierMap;
    HashSet<string> bodyPaths;
    HashSet<string> attachmentPaths;
    int addressCounter;
    int identifierCounter;
    int groupCounter;
    int attachmentCounter;

    internal EmailRedactorWorker(EmailRedactionOptions options)
    {
        this.options = options;
        changes = new List<EmailRedactionChange>();
        addressMap = new Dictionary<string, EmailAddress>(StringComparer.OrdinalIgnoreCase);
        identifierMap = new Dictionary<string, EmailMessageId>(StringComparer.OrdinalIgnoreCase);
        bodyPaths = new HashSet<string>(StringComparer.Ordinal);
        attachmentPaths = new HashSet<string>(StringComparer.Ordinal);
    }

    internal List<EmailRedactionChange> GetChanges()
    {
        return new List<EmailRedactionChange>(changes);
    }

    internal EmailMessage RedactMessage(EmailMessage source)
    {
        if (source.HasBody())
        {
            PreparePaths(source.GetBody());
        }

        EmailMessage redacted = RedactMessageCore(source, new EmailPathOperations().CreateRoot(), false);
        return changes.Count == 0 ? source.Clone() : redacted;
    }

    internal MimeEntity RedactEntity(MimeEntity source)
    {
        PreparePaths(source);
        MimeEntity redacted = RedactEntityCore(source, new EmailPathOperations().CreateRoot(), 0);
        return changes.Count == 0 ? source.Clone() : redacted;
    }

    EmailMessage RedactMessageCore(EmailMessage? source, EmailPath path, bool nested)
    {
        int initialChanges = changes.Count;
        EmailHeaders headers = RedactHeaders((source ?? throw new InvalidOperationException()).GetHeaders(), path);
        EmailPath bodyPath = nested ? path.Append(0) : path;
        MimeEntity? body = source.HasBody() ? RedactEntityCore(source.GetBody(), bodyPath, bodyPath.GetDepth()) : null;
        if (body != null)
        {
            SynchronizeContentHeaders(headers, body.GetHeaders());
        }

        EmailEnvelope envelope = options.GetRedactAddresses() ? RedactEnvelope(source.GetEnvelope(), path) : (source.GetEnvelope() ?? throw new InvalidOperationException()).Clone();
        EmailMessage redacted = new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(headers, body, envelope, null, source.GetOwnIssues());
        if (changes.Count > initialChanges)
        {
            redacted.MarkDirty();
        }

        return redacted;
    }

    MimeEntity RedactEntityCore(MimeEntity? source, EmailPath path, int depth)
    {
        if (depth > options.GetMaximumDepth())
        {
            throw new EmailLimitException("MaxTraversalDepth", options.GetMaximumDepth(), depth);
        }

        int initialChanges = changes.Count;
        EmailHeaders headers = RedactHeaders((source ?? throw new InvalidOperationException()).GetHeaders(), path);
        EmailMediaType? mediaType = source.GetMediaType();
        EmailDisposition? disposition = source.HasDisposition() ? source.GetDisposition() : null;
        EmailMessageId? contentId = source.HasContentId() ? source.GetContentId() : null;
        string? transferEncoding = source.HasTransferEncoding() ? source.GetTransferEncodingName() : null;
        EmailContent? rawContent = source.HasRawContent() ? source.GetRawContent() : null;
        EmailContent? decodedContent = source.HasDecodedContent() ? source.GetDecodedContent() : null;
        EmailBodyIngestMetadata? ingestMetadata = source.HasBodyIngestMetadata() ? source.GetBodyIngestMetadata() : null;
        EmailContent? preamble = source.HasPreamble() ? source.GetPreamble() : null;
        EmailContent? epilogue = source.HasEpilogue() ? source.GetEpilogue() : null;
        List<MimeEntity> children = new List<MimeEntity>();
        EmailMessage? nestedMessage = null;

        if (options.GetRedactMessageIds() && headers.Contains("Content-ID"))
        {
            contentId = ParseSingleIdentifier(headers.GetFirst("Content-ID"));
        }
        else if (options.GetRedactMessageIds() && contentId != null)
        {
            contentId = MapIdentifier(contentId);
            ReplaceHeader(headers, "Content-ID", FormatIdentifier(contentId), EmailHeaderValueKind.ContentId);
            changes.Add(new EmailRedactionChange(EmailRedactionKind.MessageId, path, "Content-ID", 1));
        }

        string pathText = path.GetDisplayText();
        bool attachment = attachmentPaths.Contains(pathText);
        bool body = bodyPaths.Contains(pathText);
        bool contentRemoved = options.GetRedactAttachmentContent() && attachment;
        if (contentRemoved && HasEntityPayload(source))
        {
            rawContent = null;
            ingestMetadata = null;
            preamble = null;
            epilogue = null;
            if (source.GetKind() == MimeEntityKind.Message)
            {
                decodedContent = null;
                EmailMessage placeholder = new EmailMessageOperations().CreateWith();
                placeholder.SetBody(new EmailEntityFactory().CreateTextWithValue(options.GetBodyReplacement()));
                nestedMessage = placeholder;
            }
            else if (source.GetKind() == MimeEntityKind.Multipart)
            {
                decodedContent = null;
            }
            else if (source.IsContainer())
            {
                decodedContent = null;
                int retainedIndex = 0;
                while (retainedIndex < source.GetChildCount())
                {
                    children.Add(RedactEntityCore(source.GetChildAt(retainedIndex), path.Append(retainedIndex), depth + 1));
                    retainedIndex++;
                }
            }
            else
            {
                decodedContent = new EmailContentOperations().CreateMemoryCopy(options.GetAttachmentContentReplacement(), false);
                if (!source.HasTransferEncoding() || !source.HasKnownTransferEncoding())
                {
                    transferEncoding = "base64";
                    ReplaceHeader(headers, "Content-Transfer-Encoding", transferEncoding, EmailHeaderValueKind.Unknown);
                }
            }

            changes.Add(new EmailRedactionChange(EmailRedactionKind.AttachmentContent, path, "content", 1));
        }
        else
        {
            int childIndex = 0;
            while (childIndex < source.GetChildCount())
            {
                children.Add(RedactEntityCore(source.GetChildAt(childIndex), path.Append(childIndex), depth + 1));
                childIndex++;
            }

            nestedMessage = source.HasNestedMessage() ? RedactMessageCore(source.GetNestedMessage(), path, true) : null;
            if (options.GetRedactBodies() && body && source.HasContent())
            {
                rawContent = null;
                decodedContent = new EmailContentOperations().CreateMemoryCopy(Encoding.UTF8.GetBytes(options.GetBodyReplacement()), false);
                ingestMetadata = null;
                transferEncoding = "7bit";
                ReplaceHeader(headers, "Content-Transfer-Encoding", "7bit", EmailHeaderValueKind.Unknown);
                changes.Add(new EmailRedactionChange(EmailRedactionKind.Body, path, "body", 1));
            }
        }

        if (options.GetRedactAttachmentNames() && attachment && new EmailFilename().Decode(source) != null)
        {
            attachmentCounter++;
            string replacement = options.GetAttachmentNamePrefix() + "-" + attachmentCounter.ToString(CultureInfo.InvariantCulture);
            if (disposition != null && HasParameter(disposition.GetParameters(), "filename"))
            {
                disposition = ReplaceParameterWithDispositionAndNameAndValue(disposition, "filename", replacement);
                ReplaceHeader(headers, "Content-Disposition", FormatDisposition(disposition), EmailHeaderValueKind.Disposition);
            }

            if (HasParameter((mediaType ?? throw new InvalidOperationException()).GetParameters(), "name"))
            {
                mediaType = ReplaceParameterWithMediaTypeAndNameAndValue(mediaType, "name", replacement);
                ReplaceHeader(headers, "Content-Type", FormatMediaType(mediaType), EmailHeaderValueKind.MediaType);
            }

            changes.Add(new EmailRedactionChange(EmailRedactionKind.AttachmentName, path, "filename", 1));
        }

        MimeEntity redacted = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndPartialMessageMetadataAndExternalBodyMetadataAndBodyIngestMetadataAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(source.GetKind(), headers, mediaType, disposition, transferEncoding, contentId, source.HasContentLocation() ? source.GetContentLocation() : null, source.HasContentLanguage() ? source.GetContentLanguage() : null, source.HasContentDescription() ? source.GetContentDescription() : null, source.HasPartialMessageMetadata() ? source.GetPartialMessageMetadata() : null, source.HasExternalBodyMetadata() ? source.GetExternalBodyMetadata() : null, ingestMetadata, rawContent, decodedContent, children, nestedMessage, preamble, epilogue, source.HasSourceRange() ? source.GetSourceRange() : null, source.GetOwnIssues());
        if (changes.Count > initialChanges)
        {
            redacted.MarkDirty();
        }

        return redacted;
    }

    EmailHeaders RedactHeaders(EmailHeaders? source, EmailPath path)
    {
        EmailHeaders redacted = new EmailHeadersOperations().CreateWith();
        int index = 0;
        while (index < (source ?? throw new InvalidOperationException()).GetCount())
        {
            EmailHeader header = source.GetAt(index);
            string name = header.GetName();
            if (options.GetRemoveReceivedHeaders() && string.Equals(name, "Received", StringComparison.OrdinalIgnoreCase))
            {
                changes.Add(new EmailRedactionChange(EmailRedactionKind.Received, path, name, 1));
                index++;
                continue;
            }

            if (options.GetRemoveAuthenticationHeaders() && IsAuthenticationHeader(name))
            {
                changes.Add(new EmailRedactionChange(EmailRedactionKind.Authentication, path, name, 1));
                index++;
                continue;
            }

            EmailHeader replacement = header.Clone();
            if (options.GetRedactAddresses() && IsAddressHeader(name))
            {
                EmailRedactedHeaderValue value = RedactAddressValue(header);
                replacement = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, value.GetBytes(), header.GetValueKind());
                changes.Add(new EmailRedactionChange(EmailRedactionKind.Address, path, name, value.GetCount()));
            }

            if (options.GetRedactMessageIds() && IsIdentifierHeader(name))
            {
                EmailRedactedHeaderValue value = RedactIdentifierValue(header, name);
                replacement = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, value.GetBytes(), header.GetValueKind());
                changes.Add(new EmailRedactionChange(EmailRedactionKind.MessageId, path, name, value.GetCount()));
            }

            if (options.GetRedactSubjects() && string.Equals(name, "Subject", StringComparison.OrdinalIgnoreCase))
            {
                replacement = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, Encoding.UTF8.GetBytes(options.GetSubjectReplacement()), EmailHeaderValueKind.Unstructured);
                changes.Add(new EmailRedactionChange(EmailRedactionKind.Subject, path, name, 1));
            }

            if (options.GetRedactIpAddresses())
            {
                string original = Encoding.UTF8.GetString(replacement.GetRawValue());
                string withoutIps = RedactIpAddresses(original);
                if (!string.Equals(original, withoutIps, StringComparison.Ordinal))
                {
                    replacement = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, Encoding.UTF8.GetBytes(withoutIps), replacement.GetValueKind());
                    changes.Add(new EmailRedactionChange(EmailRedactionKind.IpAddress, path, name, 1));
                }
            }

            redacted.Add(replacement);
            index++;
        }

        return redacted;
    }

    EmailRedactedHeaderValue RedactAddressValue(EmailHeader header)
    {
        EmailAddressParseResult parsed = new EmailAddressParser().ParseListWithInputAndMode(header.GetUnfoldedValue(), EmailAddressParseMode.Tolerant);
        EmailAddressList original = parsed.GetAddressList();
        List<EmailAddressListEntry> entries = new List<EmailAddressListEntry>();
        int count = 0;
        int index = 0;
        while (index < original.GetCount())
        {
            EmailAddressListEntry entry = original.GetAt(index);
            if (entry.IsMailbox())
            {
                entries.Add(new EmailAddressListEntryOperations().CreateMailbox(MapAddress(entry.GetMailbox())));
                count++;
            }
            else
            {
                EmailAddressGroup? group = entry.GetGroup();
                List<EmailAddress> addresses = new List<EmailAddress>();
                int addressIndex = 0;
                while (addressIndex < (group ?? throw new InvalidOperationException()).GetAddressCount())
                {
                    addresses.Add(MapAddress(group.GetAddress(addressIndex)));
                    addressIndex++;
                    count++;
                }

                groupCounter++;
                EmailAddressGroup replacement = new EmailAddressGroup("redacted-group-" + groupCounter.ToString(CultureInfo.InvariantCulture), addresses, new byte[0], new EmailSourcePosition(0, 1, 1), false, false);
                entries.Add(new EmailAddressListEntryOperations().CreateGroup(replacement));
            }

            index++;
        }

        if (entries.Count == 0)
        {
            entries.Add(new EmailAddressListEntryOperations().CreateMailbox(CreateNextAddress()));
            count = 1;
        }

        EmailAddressList list = new EmailAddressList(entries, new byte[0]);
        return new EmailRedactedHeaderValue(new EmailAddressFormatter().FormatListWithList(list).GetBytes(), count);
    }

    EmailRedactedHeaderValue RedactIdentifierValue(EmailHeader header, string name)
    {
        bool single = string.Equals(name, "Message-ID", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Content-ID", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Resent-Message-ID", StringComparison.OrdinalIgnoreCase);
        EmailMessageIdParseResult parsed = single ? new EmailMessageIdParser().ParseMessageIdWithInputAndMode(header.GetUnfoldedValue(), EmailMessageIdParseMode.Tolerant) : new EmailMessageIdParser().ParseReferencesWithInputAndMode(header.GetUnfoldedValue(), EmailMessageIdParseMode.Tolerant);
        EmailMessageIdList identifiers = parsed.GetIdentifiers();
        StringBuilder output = new StringBuilder();
        int index = 0;
        while (index < identifiers.GetCount())
        {
            if (index > 0)
            {
                output.Append(' ');
            }

            output.Append(FormatIdentifier(MapIdentifier(identifiers.GetAt(index))));
            index++;
        }

        if (identifiers.GetCount() == 0)
        {
            output.Append(FormatIdentifier(CreateNextIdentifier()));
        }

        return new EmailRedactedHeaderValue(Encoding.ASCII.GetBytes(output.ToString()), identifiers.GetCount() == 0 ? 1 : identifiers.GetCount());
    }

    EmailAddress MapAddress(EmailAddress? original)
    {
        string key = (original ?? throw new InvalidOperationException()).GetLocalPart() + "@" + original.GetDomain();
        if (!addressMap.ContainsKey(key))
        {
            addressMap[key] = CreateNextAddress();
        }

        return addressMap[key];
    }

    EmailAddress CreateNextAddress()
    {
        addressCounter++;
        string local = "user" + addressCounter.ToString(CultureInfo.InvariantCulture);
        byte[] localBytes = Encoding.ASCII.GetBytes(local);
        byte[] domainBytes = Encoding.ASCII.GetBytes(options.GetPseudonymDomain());
        return new EmailAddress(null, local, options.GetPseudonymDomain(), new byte[0], localBytes, domainBytes, new EmailSourcePosition(0, 1, 1), false, false, false, new List<string>());
    }

    EmailMessageId MapIdentifier(EmailMessageId original)
    {
        string key = original.GetIdLeft() + "@" + original.GetIdRight();
        if (!identifierMap.ContainsKey(key))
        {
            identifierMap[key] = CreateNextIdentifier();
        }

        return identifierMap[key];
    }

    EmailMessageId CreateNextIdentifier()
    {
        identifierCounter++;
        string left = "id" + identifierCounter.ToString(CultureInfo.InvariantCulture);
        return new EmailMessageId(left, options.GetPseudonymDomain(), false, new byte[0], new EmailSourcePosition(0, 1, 1), false, false);
    }

    EmailMessageId? ParseSingleIdentifier(EmailHeader? header)
    {
        EmailMessageIdParseResult parsed = new EmailMessageIdParser().ParseContentIdWithInputAndMode((header ?? throw new InvalidOperationException()).GetUnfoldedValue(), EmailMessageIdParseMode.Tolerant);
        return parsed.HasSingleIdentifier() ? parsed.GetIdentifier() : null;
    }

    EmailEnvelope RedactEnvelope(EmailEnvelope? source, EmailPath path)
    {
        EmailAddress? sender = (source ?? throw new InvalidOperationException()).HasSender() ? MapAddress(source.GetSender()) : null;
        List<EmailAddress> recipients = new List<EmailAddress>();
        int index = 0;
        while (index < source.GetRecipientCount())
        {
            recipients.Add(MapAddress(source.GetRecipientAt(index)));
            index++;
        }

        int count = recipients.Count + (sender == null ? 0 : 1) + (source.HasMboxFromLine() ? 1 : 0);
        if (count > 0)
        {
            changes.Add(new EmailRedactionChange(EmailRedactionKind.Address, path, "envelope", count));
        }

        return new EmailEnvelopeOperations().CreateWithSenderAndRecipientsAndMboxFromLineAndMboxFromLineRange(sender, recipients, null, null);
    }

    void PreparePaths(MimeEntity? root)
    {
        List<EmailAttachmentView> views = new EmailAttachmentFinder().EnumerateWithRootAndMaximumDepthAndMaximumEntityCount(root, options.GetMaximumDepth(), options.GetMaximumEntityCount());
        int index = 0;
        while (index < views.Count)
        {
            EmailAttachmentView view = views[index];
            if (view.IsBody())
            {
                bodyPaths.Add(view.GetPath().GetDisplayText());
            }
            else
            {
                attachmentPaths.Add(view.GetPath().GetDisplayText());
            }

            index++;
        }
    }

    static bool HasEntityPayload(MimeEntity entity)
    {
        return entity.HasContent() || entity.GetChildCount() > 0 || entity.HasNestedMessage() || entity.HasPreamble() || entity.HasEpilogue();
    }

    static bool HasParameter(EmailParameterList parameters, string name)
    {
        return parameters.GetFirst(name) != null || parameters.GetDecoded(name) != null;
    }

    static EmailDisposition ReplaceParameterWithDispositionAndNameAndValue(EmailDisposition disposition, string name, string value)
    {
        return new EmailDisposition(disposition.GetDispositionType(), ReplaceParameterWithOriginalAndNameAndValue(disposition.GetParameters(), name, value), new byte[0], disposition.GetSourcePosition());
    }

    static EmailMediaType ReplaceParameterWithMediaTypeAndNameAndValue(EmailMediaType mediaType, string name, string value)
    {
        return new EmailMediaType(mediaType.GetTopLevelType(), mediaType.GetSubtype(), ReplaceParameterWithOriginalAndNameAndValue(mediaType.GetParameters(), name, value), new byte[0], mediaType.GetSourcePosition(), false);
    }

    static EmailParameterList ReplaceParameterWithOriginalAndNameAndValue(EmailParameterList original, string name, string value)
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

        byte[] bytes = Encoding.UTF8.GetBytes(value);
        EmailSourcePosition position = new EmailSourcePosition(0, 1, 1);
        physical.Add(new EmailParameter(name, bytes, bytes, true, new byte[0], position));
        decoded.Add(new EmailRfc2231DecodedParameter(name, bytes, "utf-8", true, null, false, true, false, 1, new List<EmailRfc2231Parameter>()));
        return new EmailParameterList(physical, decoded);
    }

    static void SynchronizeContentHeaders(EmailHeaders messageHeaders, EmailHeaders? entityHeaders)
    {
        string[] names = new string[] { "Content-Type", "Content-Transfer-Encoding", "Content-Disposition", "Content-ID" };
        int index = 0;
        while (index < names.Length)
        {
            List<EmailHeader> replacements = (entityHeaders ?? throw new InvalidOperationException()).GetAllNamed(names[index]);
            if (replacements.Count > 0)
            {
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
            }

            index++;
        }
    }

    static void ReplaceHeader(EmailHeaders headers, string name, string value, EmailHeaderValueKind kind)
    {
        EmailHeader replacement = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, Encoding.UTF8.GetBytes(value), kind);
        if (headers.Contains(name))
        {
            headers.ReplaceAll(name, replacement);
        }
        else
        {
            headers.Add(replacement);
        }
    }

    static string FormatIdentifier(EmailMessageId identifier)
    {
        return Encoding.ASCII.GetString(new EmailMessageIdFormatter().FormatWithIdentifier(identifier).GetBytes());
    }

    static string FormatDisposition(EmailDisposition disposition)
    {
        return Encoding.UTF8.GetString(new EmailValueWriter().WriteDispositionWithValue(disposition).GetBytes());
    }

    static string FormatMediaType(EmailMediaType mediaType)
    {
        return Encoding.UTF8.GetString(new EmailValueWriter().WriteMediaTypeWithValue(mediaType).GetBytes());
    }

    static string RedactIpAddresses(string value)
    {
        string redacted = Regex.Replace(value, "(?<![A-Za-z0-9])(?:[0-9]{1,3}\\.){3}[0-9]{1,3}(?![A-Za-z0-9])", "0.0.0.0", RegexOptions.CultureInvariant);
        redacted = Regex.Replace(redacted, "\\[(?:IPv6:)?[0-9A-Fa-f:]+\\]", "[redacted-ip]", RegexOptions.CultureInvariant);
        return Regex.Replace(redacted, "(?<![0-9A-Fa-f:])(?=[0-9A-Fa-f:]*[A-Fa-f])(?:[0-9A-Fa-f]{0,4}:){2,7}[0-9A-Fa-f]{0,4}(?![0-9A-Fa-f:])", "::", RegexOptions.CultureInvariant);
    }

    static bool IsAddressHeader(string name)
    {
        return EqualsAny(name, new string[] { "From", "Sender", "Reply-To", "To", "Cc", "Bcc", "Return-Path", "Resent-From", "Resent-Sender", "Resent-Reply-To", "Resent-To", "Resent-Cc", "Resent-Bcc", "Mail-Reply-To", "Mail-Followup-To", "Delivered-To", "X-Original-To", "Errors-To" });
    }

    static bool IsIdentifierHeader(string name)
    {
        return EqualsAny(name, new string[] { "Message-ID", "Content-ID", "Resent-Message-ID", "In-Reply-To", "References" });
    }

    static bool IsAuthenticationHeader(string name)
    {
        return EqualsAny(name, new string[] { "Authentication-Results", "Original-Authentication-Results", "Received-SPF", "DKIM-Signature", "DomainKey-Signature", "ARC-Authentication-Results", "ARC-Message-Signature", "ARC-Seal" });
    }

    static bool EqualsAny(string value, string[] choices)
    {
        int index = 0;
        while (index < choices.Length)
        {
            if (string.Equals(value, choices[index], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
internal class EmailRedactedHeaderValue
{
    byte[] bytes;
    int count;

    internal EmailRedactedHeaderValue(byte[] bytes, int count)
    {
        this.bytes = bytes;
        this.count = count;
    }

    internal byte[] GetBytes()
    {
        byte[] copy = new byte[bytes.Length];
        bytes.CopyTo(copy, 0);
        return copy;
    }

    internal int GetCount() { return count; }
}
