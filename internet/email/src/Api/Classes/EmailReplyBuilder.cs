using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Builds reply from caller-supplied values.
/// </summary>
public class EmailReplyBuilder : IEmailReplyBuilderContract
{
    internal static EmailComposer CreateReplyImplementationWithOriginalAndAuthor(EmailMessage original, EmailAddress author)
    {
        return CreateReplyImplementationWithOriginalAndAuthorAndOptions(original, author, new EmailReplyOptions());
    }

    /// <summary>
    /// Creates a reply configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateReplyWithOriginalAndAuthor(EmailMessage original, EmailAddress author)
    {
        return EmailReplyBuilder.CreateReplyImplementationWithOriginalAndAuthor(original, author);
    }

    internal static EmailComposer CreateReplyImplementationWithOriginalAndAuthorAndReplyText(EmailMessage original, EmailAddress author, string replyText)
    {
        EmailReplyOptions options = new EmailReplyOptions();
        options.SetReplyText(replyText);
        return CreateReplyImplementationWithOriginalAndAuthorAndOptions(original, author, options);
    }

    /// <summary>
    /// Creates a reply configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <param name = "replyText">The reply text used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateReplyWithOriginalAndAuthorAndReplyText(EmailMessage original, EmailAddress author, string replyText)
    {
        return EmailReplyBuilder.CreateReplyImplementationWithOriginalAndAuthorAndReplyText(original, author, replyText);
    }

    internal static EmailComposer CreateReplyImplementationWithOriginalAndAuthorAndOptions(EmailMessage original, EmailAddress author, EmailReplyOptions options)
    {
        return Create(original, author, options, false);
    }

    /// <summary>
    /// Creates a reply configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateReplyWithOriginalAndAuthorAndOptions(EmailMessage original, EmailAddress author, EmailReplyOptions options)
    {
        return EmailReplyBuilder.CreateReplyImplementationWithOriginalAndAuthorAndOptions(original, author, options);
    }

    internal static EmailComposer CreateReplyAllImplementationWithOriginalAndAuthor(EmailMessage original, EmailAddress author)
    {
        return CreateReplyAllImplementationWithOriginalAndAuthorAndOptions(original, author, new EmailReplyOptions());
    }

    /// <summary>
    /// Creates a reply all configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateReplyAllWithOriginalAndAuthor(EmailMessage original, EmailAddress author)
    {
        return EmailReplyBuilder.CreateReplyAllImplementationWithOriginalAndAuthor(original, author);
    }

    internal static EmailComposer CreateReplyAllImplementationWithOriginalAndAuthorAndReplyText(EmailMessage original, EmailAddress author, string replyText)
    {
        EmailReplyOptions options = new EmailReplyOptions();
        options.SetReplyText(replyText);
        return CreateReplyAllImplementationWithOriginalAndAuthorAndOptions(original, author, options);
    }

    /// <summary>
    /// Creates a reply all configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <param name = "replyText">The reply text used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateReplyAllWithOriginalAndAuthorAndReplyText(EmailMessage original, EmailAddress author, string replyText)
    {
        return EmailReplyBuilder.CreateReplyAllImplementationWithOriginalAndAuthorAndReplyText(original, author, replyText);
    }

    internal static EmailComposer CreateReplyAllImplementationWithOriginalAndAuthorAndOptions(EmailMessage original, EmailAddress author, EmailReplyOptions options)
    {
        return Create(original, author, options, true);
    }

    /// <summary>
    /// Creates a reply all configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateReplyAllWithOriginalAndAuthorAndOptions(EmailMessage original, EmailAddress author, EmailReplyOptions options)
    {
        return EmailReplyBuilder.CreateReplyAllImplementationWithOriginalAndAuthorAndOptions(original, author, options);
    }

    internal static string NormalizeSubjectImplementation(string? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        string selected = SanitizeSubject(value).Trim();
        while (HasReplyPrefix(selected))
        {
            selected = RemoveReplyPrefix(selected).TrimStart();
        }

        return selected.Length == 0 ? "Re:" : "Re: " + selected;
    }

    /// <summary>
    /// Normalizes the subject.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public string NormalizeSubject(string? value)
    {
        return EmailReplyBuilder.NormalizeSubjectImplementation(value);
    }

    static EmailComposer Create(EmailMessage original, EmailAddress author, EmailReplyOptions options, bool replyAll)
    {
        if (original == null)
        {
            throw new ArgumentNullException("original");
        }

        if (author == null)
        {
            throw new ArgumentNullException("author");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailReplyOptions snapshot = options.Clone();
        EmailComposer composer = new EmailComposerOperations().CreateWithOptions(snapshot.GetComposerOptions());
        composer.SetFrom(author);
        AddRecipients(composer, original, author, snapshot, replyAll);
        AddSubject(composer, original);
        AddThreading(composer, original, snapshot.GetMaximumReferenceCount());
        AddBodies(composer, original, snapshot);
        AddAttachments(composer, original, snapshot);
        return composer;
    }

    static void AddRecipients(EmailComposer composer, EmailMessage original, EmailAddress author, EmailReplyOptions options, bool replyAll)
    {
        List<EmailAddress> identities = new List<EmailAddress>();
        identities.Add(author);
        int identityIndex = 0;
        while (identityIndex < options.GetIdentityCount())
        {
            AddAddressUnique(identities, options.GetIdentityAt(identityIndex));
            identityIndex++;
        }

        List<EmailAddress> primary = GetHeaderAddresses(original, "Reply-To");
        if (primary.Count == 0)
        {
            primary = GetHeaderAddresses(original, "From");
        }

        if (primary.Count == 0)
        {
            primary = GetHeaderAddresses(original, "Sender");
        }

        List<EmailAddress> selectedTo = new List<EmailAddress>();
        AddCandidates(selectedTo, primary, identities, new List<EmailAddress>());
        if (replyAll)
        {
            AddCandidates(selectedTo, GetHeaderAddresses(original, "To"), identities, new List<EmailAddress>());
        }

        if (selectedTo.Count == 0)
        {
            AddCandidates(selectedTo, GetHeaderAddresses(original, "To"), identities, new List<EmailAddress>());
            AddCandidates(selectedTo, GetHeaderAddresses(original, "Cc"), identities, new List<EmailAddress>());
        }

        if (selectedTo.Count == 0)
        {
            throw new InvalidOperationException("The original message does not contain a reply recipient other than the reply author.");
        }

        int index = 0;
        while (index < selectedTo.Count)
        {
            composer.AddTo(selectedTo[index]);
            index++;
        }

        if (!replyAll)
        {
            return;
        }

        List<EmailAddress> selectedCc = new List<EmailAddress>();
        AddCandidates(selectedCc, GetHeaderAddresses(original, "Cc"), identities, selectedTo);
        index = 0;
        while (index < selectedCc.Count)
        {
            composer.AddCc(selectedCc[index]);
            index++;
        }
    }

    static List<EmailAddress> GetHeaderAddresses(EmailMessage message, string name)
    {
        List<EmailAddress> values = new List<EmailAddress>();
        List<EmailHeader> headers = (message.GetHeaders() ?? throw new InvalidOperationException()).GetAllNamed(name);
        int headerIndex = 0;
        while (headerIndex < headers.Count)
        {
            EmailAddressParserOptions parserOptions = new EmailAddressParserOptions();
            parserOptions.SetMode(EmailAddressParseMode.Tolerant);
            parserOptions.SetAllowInternationalized(true);
            EmailAddressParseResult parsed = new EmailAddressParser().ParseListWithInputAndOptions(headers[headerIndex].GetUnfoldedValue(), parserOptions);
            EmailAddressList list = parsed.GetAddressList();
            int entryIndex = 0;
            while (entryIndex < list.GetCount())
            {
                EmailAddressListEntry entry = list.GetAt(entryIndex);
                if (entry.IsMailbox())
                {
                    AddAddressUnique(values, entry.GetMailbox());
                }
                else
                {
                    EmailAddressGroup? group = entry.GetGroup();
                    int groupIndex = 0;
                    while (groupIndex < (group ?? throw new InvalidOperationException()).GetAddressCount())
                    {
                        AddAddressUnique(values, group.GetAddress(groupIndex));
                        groupIndex++;
                    }
                }

                entryIndex++;
            }

            headerIndex++;
        }

        return values;
    }

    static void AddCandidates(List<EmailAddress> destination, List<EmailAddress> candidates, List<EmailAddress> excluded, List<EmailAddress> alreadySelected)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            EmailAddress candidate = candidates[index];
            if (!ContainsAddress(excluded, candidate) && !ContainsAddress(alreadySelected, candidate))
            {
                AddAddressUnique(destination, candidate);
            }

            index++;
        }
    }

    static void AddAddressUnique(List<EmailAddress> values, EmailAddress? value)
    {
        if (!ContainsAddress(values, value))
        {
            values.Add((value ?? throw new InvalidOperationException()));
        }
    }

    static bool ContainsAddress(List<EmailAddress> values, EmailAddress? expected)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (AddressEquals(values[index], expected))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool AddressEquals(EmailAddress first, EmailAddress? second)
    {
        return string.Equals(first.GetLocalPart(), (second ?? throw new InvalidOperationException()).GetLocalPart(), StringComparison.OrdinalIgnoreCase) && string.Equals(first.GetAsciiDomain(), second.GetAsciiDomain(), StringComparison.OrdinalIgnoreCase);
    }

    static void AddSubject(EmailComposer composer, EmailMessage original)
    {
        EmailHeader? subject = (original.GetHeaders() ?? throw new InvalidOperationException()).GetFirst("Subject");
        composer.SetSubject(NormalizeSubjectImplementation(subject == null ? "" : subject.GetDecodedValue()));
    }

    static void AddThreading(EmailComposer composer, EmailMessage original, int maximumReferenceCount)
    {
        EmailMessageId? parent = GetParentIdentifier(original);
        if (parent != null)
        {
            composer.SetInReplyTo(parent);
        }

        List<EmailMessageId> references = GetReferences(original, maximumReferenceCount);
        if (parent != null)
        {
            RemoveIdentifier(references, parent);
            references.Add(parent);
        }

        references = LimitReferences(references, maximumReferenceCount);
        int index = 0;
        while (index < references.Count)
        {
            composer.AddReference(references[index]);
            index++;
        }
    }

    static EmailMessageId? GetParentIdentifier(EmailMessage original)
    {
        List<EmailHeader> headers = (original.GetHeaders() ?? throw new InvalidOperationException()).GetAllNamed("Message-ID");
        int index = 0;
        while (index < headers.Count)
        {
            EmailMessageIdParseResult parsed = new EmailMessageIdParser().ParseMessageIdWithInputAndMode(headers[index].GetUnfoldedValue(), EmailMessageIdParseMode.Tolerant);
            if (parsed.HasSingleIdentifier() && IsSafeIdentifier(parsed.GetIdentifier()))
            {
                return parsed.GetIdentifier();
            }

            index++;
        }

        return null;
    }

    static List<EmailMessageId> GetReferences(EmailMessage original, int maximumReferenceCount)
    {
        List<EmailMessageId> values = new List<EmailMessageId>();
        List<EmailHeader> headers = (original.GetHeaders() ?? throw new InvalidOperationException()).GetAllNamed("References");
        int headerIndex = 0;
        while (headerIndex < headers.Count)
        {
            EmailMessageIdParserOptions parserOptions = new EmailMessageIdParserOptions();
            parserOptions.SetMode(EmailMessageIdParseMode.Tolerant);
            parserOptions.SetMaximumIdentifiers(maximumReferenceCount == int.MaxValue ? int.MaxValue : maximumReferenceCount + 1);
            EmailMessageIdParseResult parsed = new EmailMessageIdParser().ParseReferencesWithInputAndOptions(headers[headerIndex].GetUnfoldedValue(), parserOptions);
            EmailMessageIdList identifiers = parsed.GetIdentifiers();
            int identifierIndex = 0;
            while (identifierIndex < identifiers.GetCount())
            {
                EmailMessageId identifier = identifiers.GetAt(identifierIndex);
                if (IsSafeIdentifier(identifier) && !ContainsIdentifier(values, identifier))
                {
                    values.Add(identifier);
                }

                identifierIndex++;
            }

            headerIndex++;
        }

        return values;
    }

    static bool IsSafeIdentifier(EmailMessageId? value)
    {
        return new EmailMessageIdFormatter().FormatWithIdentifier(value).IsSuccess();
    }

    static void RemoveIdentifier(List<EmailMessageId> values, EmailMessageId expected)
    {
        int index = values.Count - 1;
        while (index >= 0)
        {
            if (IdentifierEquals(values[index], expected))
            {
                values.RemoveAt(index);
            }

            index--;
        }
    }

    static bool ContainsIdentifier(List<EmailMessageId> values, EmailMessageId expected)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (IdentifierEquals(values[index], expected))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool IdentifierEquals(EmailMessageId first, EmailMessageId second)
    {
        return string.Equals(first.GetIdLeft(), second.GetIdLeft(), StringComparison.Ordinal) && string.Equals(first.GetIdRight(), second.GetIdRight(), StringComparison.OrdinalIgnoreCase) && first.IsRightDomainLiteral() == second.IsRightDomainLiteral();
    }

    static List<EmailMessageId> LimitReferences(List<EmailMessageId> values, int maximumReferenceCount)
    {
        if (values.Count <= maximumReferenceCount)
        {
            return values;
        }

        List<EmailMessageId> limited = new List<EmailMessageId>();
        if (maximumReferenceCount > 1)
        {
            limited.Add(values[0]);
        }

        int start = values.Count - maximumReferenceCount + limited.Count;
        int index = start;
        while (index < values.Count)
        {
            limited.Add(values[index]);
            index++;
        }

        return limited;
    }

    static void AddBodies(EmailComposer composer, EmailMessage original, EmailReplyOptions options)
    {
        string attribution = GetAttribution(original);
        string? quotedText = options.GetQuoteOriginal() ? GetQuotedText(original, options.GetMaximumQuotedCharacters()) : null;
        composer.SetText(ComposeText(options.GetReplyText(), attribution, quotedText));
        if (options.HasReplyHtml())
        {
            string? quotedHtml = options.GetQuoteOriginal() ? GetQuotedHtml(original, options.GetMaximumQuotedCharacters()) : null;
            composer.SetHtml(ComposeHtml(options.GetReplyHtml(), attribution, quotedHtml));
        }
    }

    static string? GetQuotedText(EmailMessage original, int maximumCharacters)
    {
        if (!original.HasBody())
        {
            return null;
        }

        MimeEntity? plain = new EmailBodySelector().GetPlainTextWithMessage(original);
        string? text = null;
        if (plain != null)
        {
            text = DecodeText(plain, maximumCharacters);
        }
        else
        {
            MimeEntity? html = new EmailBodySelector().GetHtmlWithMessage(original);
            if (html != null)
            {
                EmailTextDecodeOptions textOptions = CreateTextOptions(maximumCharacters);
                EmailHtmlTextOptions htmlOptions = new EmailHtmlTextOptions();
                htmlOptions.SetLimits(CreateHtmlLimits(maximumCharacters));
                text = new EmailHtmlProcessor().ToTextWithEntityAndTextOptionsAndHtmlOptions(html, textOptions, htmlOptions).GetText();
            }
        }

        return text == null ? null : QuoteLines(text, maximumCharacters);
    }

    static string? GetQuotedHtml(EmailMessage original, int maximumCharacters)
    {
        if (!original.HasBody())
        {
            return null;
        }

        MimeEntity? html = new EmailBodySelector().GetHtmlWithMessage(original);
        if (html != null)
        {
            EmailTextDecodeOptions textOptions = CreateTextOptions(maximumCharacters);
            EmailHtmlSanitizeOptions htmlOptions = new EmailHtmlSanitizeOptions();
            htmlOptions.SetLimits(CreateHtmlLimits(maximumCharacters));
            string sanitized = new EmailHtmlProcessor().SanitizeWithEntityAndTextOptionsAndHtmlOptions(html, textOptions, htmlOptions).GetHtml();
            EnsureQuotedLength(sanitized, maximumCharacters);
            return sanitized;
        }

        MimeEntity? plain = new EmailBodySelector().GetPlainTextWithMessage(original);
        if (plain == null)
        {
            return null;
        }

        string text = DecodeText(plain, maximumCharacters);
        string escaped = EscapeHtml(text).Replace("\r\n", "<br>").Replace("\r", "<br>").Replace("\n", "<br>");
        EnsureQuotedLength(escaped, maximumCharacters);
        return escaped;
    }

    static string DecodeText(MimeEntity entity, int maximumCharacters)
    {
        string text = new EmailTextProcessor().DecodeWithEntityAndTextOptions(entity, CreateTextOptions(maximumCharacters)).GetText();
        EnsureQuotedLength(text, maximumCharacters);
        return text;
    }

    static EmailTextDecodeOptions CreateTextOptions(int maximumCharacters)
    {
        EmailTextDecodeOptions options = new EmailTextDecodeOptions();
        options.SetMaximumDecodedBytes(maximumCharacters);
        options.SetNewlinePolicy(EmailTextNewlinePolicy.NormalizeToCrlf);
        return options;
    }

    static EmailHtmlLimits CreateHtmlLimits(int maximumCharacters)
    {
        EmailHtmlLimits limits = new EmailHtmlLimits();
        limits.SetMaxInputCharacters(maximumCharacters);
        limits.SetMaxOutputCharacters(maximumCharacters);
        return limits;
    }

    static string QuoteLines(string value, int maximumCharacters)
    {
        string normalized = value.Replace("\r\n", "\n").Replace('\r', '\n');
        StringBuilder quoted = new StringBuilder();
        int start = 0;
        while (start <= normalized.Length)
        {
            int end = normalized.IndexOf('\n', start);
            bool final = end < 0;
            if (final)
            {
                end = normalized.Length;
            }

            string prefix = end == start ? ">" : "> ";
            if (quoted.Length > maximumCharacters - prefix.Length - (end - start))
            {
                throw new EmailLimitException("MaxReplyQuotedCharacters", maximumCharacters, (long)quoted.Length + prefix.Length + end - start);
            }

            quoted.Append(prefix);
            quoted.Append(normalized, start, end - start);
            if (final)
            {
                break;
            }

            if (quoted.Length > maximumCharacters - 2)
            {
                throw new EmailLimitException("MaxReplyQuotedCharacters", maximumCharacters, (long)quoted.Length + 2);
            }

            quoted.Append("\r\n");
            start = end + 1;
        }

        return quoted.ToString();
    }

    static void EnsureQuotedLength(string value, int maximumCharacters)
    {
        if (value.Length > maximumCharacters)
        {
            throw new EmailLimitException("MaxReplyQuotedCharacters", maximumCharacters, value.Length);
        }
    }

    static string ComposeText(string reply, string attribution, string? quoted)
    {
        if (quoted == null)
        {
            return reply;
        }

        string separator = reply.Length == 0 ? "" : "\r\n\r\n";
        return reply + separator + attribution + "\r\n" + quoted;
    }

    static string? ComposeHtml(string? reply, string attribution, string? quoted)
    {
        if (quoted == null)
        {
            return reply;
        }

        string separator = (reply ?? throw new InvalidOperationException()).Length == 0 ? "" : "<br><br>";
        return reply + separator + "<div>" + EscapeHtml(attribution) + "</div><blockquote type=\"cite\">" + quoted + "</blockquote>";
    }

    static string GetAttribution(EmailMessage original)
    {
        List<EmailAddress> senders = GetHeaderAddresses(original, "From");
        string sender = senders.Count == 0 ? "the sender" : FormatAttributionAddress(senders[0]);
        EmailHeader? date = (original.GetHeaders() ?? throw new InvalidOperationException()).GetFirst("Date");
        string? dateText = date == null ? null : SanitizeBodyText(date.GetDecodedValue()).Trim();
        return dateText == null || dateText.Length == 0 ? "On " + sender + " wrote:" : "On " + dateText + ", " + sender + " wrote:";
    }

    static string FormatAttributionAddress(EmailAddress value)
    {
        if (value.HasDisplayName() && (value.GetDisplayName() ?? throw new InvalidOperationException()).Length > 0)
        {
            return SanitizeBodyText(value.GetDisplayName());
        }

        return value.GetLocalPart() + "@" + value.GetDomain();
    }

    static string SanitizeBodyText(string? value)
    {
        StringBuilder output = new StringBuilder();
        int index = 0;
        while (index < (value ?? throw new InvalidOperationException()).Length)
        {
            char current = value[index];
            output.Append(current == '\r' || current == '\n' || current == 0 || current < 32 && current != '\t' || current == 127 ? ' ' : current);
            index++;
        }

        return output.ToString();
    }

    static void AddAttachments(EmailComposer composer, EmailMessage original, EmailReplyOptions options)
    {
        if (options.GetAttachmentPolicy() == EmailReplyAttachmentPolicy.None || !original.HasBody())
        {
            return;
        }

        List<EmailAttachmentView> views = new EmailAttachmentFinder().EnumerateWithMessage(original);
        int includedCount = 0;
        long includedBytes = 0;
        int index = 0;
        while (index < views.Count)
        {
            EmailAttachmentView view = views[index];
            bool inline = view.IsInlineResource() && options.GetAttachmentPolicy() == EmailReplyAttachmentPolicy.AttachmentsAndInlineResources;
            bool attachment = view.IsAttachment();
            if (inline && !view.HasContentId() && !view.HasContentLocation())
            {
                inline = false;
            }

            if (attachment || inline)
            {
                if (includedCount >= options.GetMaximumAttachmentCount())
                {
                    throw new EmailLimitException("MaxReplyAttachments", options.GetMaximumAttachmentCount(), includedCount + 1);
                }

                long remaining = options.GetMaximumAttachmentBytes() - includedBytes;
                if (remaining < 1)
                {
                    throw new EmailLimitException("MaxReplyAttachmentBytes", options.GetMaximumAttachmentBytes(), includedBytes + 1);
                }

                byte[] decoded = MaterializeDecoded(view.GetEntity(), remaining);
                includedBytes += decoded.Length;
                EmailAttachmentSource source = CreateAttachmentSource(view, decoded);
                if (inline)
                {
                    composer.AddRelatedResource(source);
                    includedCount++;
                }
                else
                {
                    composer.AddAttachment(source);
                    includedCount++;
                }
            }

            index++;
        }
    }

    static byte[] MaterializeDecoded(MimeEntity entity, long maximumBytes)
    {
        using (MemoryStream output = new MemoryStream())
        {
            new EmailDecodedContent().CopyToWithEntityAndOutputAndMaximumBytes(entity, output, maximumBytes);
            return output.ToArray();
        }
    }

    static EmailAttachmentSource CreateAttachmentSource(EmailAttachmentView view, byte[] decoded)
    {
        EmailAttachmentSource source = view.HasFilename() ? new EmailAttachmentSourceOperations().CreateMemoryWithValueAndFilename(decoded, view.GetFilename()) : new EmailAttachmentSourceOperations().CreateMemoryWithValue(decoded);
        EmailMediaType? mediaType = view.GetMediaType();
        source.SetMediaType((mediaType ?? throw new InvalidOperationException()).GetTopLevelType() + "/" + mediaType.GetSubtype());
        MimeEntity entity = view.GetEntity();
        if (entity.HasContentId())
        {
            source.SetContentId(entity.GetContentId());
        }

        if (entity.HasContentLocation())
        {
            source.SetContentLocation(entity.GetContentLocation());
        }

        if (entity.HasContentDescription())
        {
            source.SetDescription(entity.GetContentDescription());
        }

        return source;
    }

    static bool HasReplyPrefix(string value)
    {
        if (value.Length < 3 || !IsAsciiEqual(value[0], 'r') || !IsAsciiEqual(value[1], 'e'))
        {
            return false;
        }

        int index = 2;
        if (index < value.Length && value[index] == '[')
        {
            index++;
            int digitStart = index;
            while (index < value.Length && value[index] >= '0' && value[index] <= '9')
            {
                index++;
            }

            if (index == digitStart || index >= value.Length || value[index] != ']')
            {
                return false;
            }

            index++;
        }

        return index < value.Length && value[index] == ':';
    }

    static string RemoveReplyPrefix(string value)
    {
        int index = 2;
        if (value[index] == '[')
        {
            while (value[index] != ']')
            {
                index++;
            }

            index++;
        }

        return value.Substring(index + 1);
    }

    static bool IsAsciiEqual(char value, char lower)
    {
        return value == lower || value == (char)(lower - 32);
    }

    static string SanitizeSubject(string value)
    {
        StringBuilder output = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\r' || current == '\n' || current == 0 || current < 32 && current != '\t' || current == 127)
            {
                output.Append(' ');
            }
            else
            {
                output.Append(current);
            }

            index++;
        }

        return output.ToString();
    }

    static string EscapeHtml(string value)
    {
        return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");
    }
}
