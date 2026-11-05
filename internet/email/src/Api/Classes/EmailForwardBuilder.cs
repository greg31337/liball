using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Builds forward from caller-supplied values.
/// </summary>
public class EmailForwardBuilder : IEmailForwardBuilderContract
{
    internal static EmailComposer CreateInlineImplementationWithOriginalAndAuthor(EmailMessage original, EmailAddress author)
    {
        return CreateInlineImplementationWithOriginalAndAuthorAndOptions(original, author, new EmailForwardOptions());
    }

    /// <summary>
    /// Creates an inline configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateInlineWithOriginalAndAuthor(EmailMessage original, EmailAddress author)
    {
        return EmailForwardBuilder.CreateInlineImplementationWithOriginalAndAuthor(original, author);
    }

    internal static EmailComposer CreateInlineImplementationWithOriginalAndAuthorAndForwardText(EmailMessage original, EmailAddress author, string forwardText)
    {
        EmailForwardOptions options = new EmailForwardOptions();
        options.SetForwardText(forwardText);
        return CreateInlineImplementationWithOriginalAndAuthorAndOptions(original, author, options);
    }

    /// <summary>
    /// Creates an inline configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <param name = "forwardText">The forward text used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateInlineWithOriginalAndAuthorAndForwardText(EmailMessage original, EmailAddress author, string forwardText)
    {
        return EmailForwardBuilder.CreateInlineImplementationWithOriginalAndAuthorAndForwardText(original, author, forwardText);
    }

    internal static EmailComposer CreateInlineImplementationWithOriginalAndAuthorAndOptions(EmailMessage original, EmailAddress author, EmailForwardOptions options)
    {
        ValidateInputs(original, author, options);
        EmailForwardOptions snapshot = options.Clone();
        EmailComposer composer = CreateComposer(original, author, snapshot);
        AddInlineBodies(composer, original, snapshot);
        AddInlineAttachments(composer, original, snapshot);
        return composer;
    }

    /// <summary>
    /// Creates an inline configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateInlineWithOriginalAndAuthorAndOptions(EmailMessage original, EmailAddress author, EmailForwardOptions options)
    {
        return EmailForwardBuilder.CreateInlineImplementationWithOriginalAndAuthorAndOptions(original, author, options);
    }

    internal static EmailComposer CreateAttachedMessageImplementationWithOriginalAndAuthor(EmailMessage original, EmailAddress author)
    {
        return CreateAttachedMessageImplementationWithOriginalAndAuthorAndOptions(original, author, new EmailForwardOptions());
    }

    /// <summary>
    /// Creates an attached message configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateAttachedMessageWithOriginalAndAuthor(EmailMessage original, EmailAddress author)
    {
        return EmailForwardBuilder.CreateAttachedMessageImplementationWithOriginalAndAuthor(original, author);
    }

    internal static EmailComposer CreateAttachedMessageImplementationWithOriginalAndAuthorAndForwardText(EmailMessage original, EmailAddress author, string forwardText)
    {
        EmailForwardOptions options = new EmailForwardOptions();
        options.SetForwardText(forwardText);
        return CreateAttachedMessageImplementationWithOriginalAndAuthorAndOptions(original, author, options);
    }

    /// <summary>
    /// Creates an attached message configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <param name = "forwardText">The forward text used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateAttachedMessageWithOriginalAndAuthorAndForwardText(EmailMessage original, EmailAddress author, string forwardText)
    {
        return EmailForwardBuilder.CreateAttachedMessageImplementationWithOriginalAndAuthorAndForwardText(original, author, forwardText);
    }

    internal static EmailComposer CreateAttachedMessageImplementationWithOriginalAndAuthorAndOptions(EmailMessage original, EmailAddress author, EmailForwardOptions options)
    {
        ValidateInputs(original, author, options);
        EmailForwardOptions snapshot = options.Clone();
        EmailComposer composer = CreateComposer(original, author, snapshot);
        EmailMessage embeddedMessage = CreateEmbeddedMessage(original, snapshot);
        MimeEntity embedded = CreateEmbeddedEntity(embeddedMessage, snapshot.GetAttachedFilename());
        List<MimeEntity> children = new List<MimeEntity>();
        children.Add(CreateIntroduction(snapshot));
        children.Add(embedded);
        MimeEntity mixed = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", children);
        composer.SetRawEntity(GenerateBoundaries(mixed));
        return composer;
    }

    /// <summary>
    /// Creates an attached message configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    public EmailComposer CreateAttachedMessageWithOriginalAndAuthorAndOptions(EmailMessage original, EmailAddress author, EmailForwardOptions options)
    {
        return EmailForwardBuilder.CreateAttachedMessageImplementationWithOriginalAndAuthorAndOptions(original, author, options);
    }

    internal static string NormalizeSubjectImplementation(string? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        string selected = SanitizeText(value).Trim();
        while (HasForwardPrefix(selected))
        {
            selected = RemoveForwardPrefix(selected).TrimStart();
        }

        return selected.Length == 0 ? "Fwd:" : "Fwd: " + selected;
    }

    /// <summary>
    /// Normalizes the subject.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public string NormalizeSubject(string? value)
    {
        return EmailForwardBuilder.NormalizeSubjectImplementation(value);
    }

    static void ValidateInputs(EmailMessage original, EmailAddress author, EmailForwardOptions options)
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
    }

    static EmailComposer CreateComposer(EmailMessage original, EmailAddress author, EmailForwardOptions options)
    {
        EmailComposer composer = new EmailComposerOperations().CreateWithOptions(options.GetComposerOptions());
        composer.SetFrom(author);
        EmailHeader? subject = (original.GetHeaders() ?? throw new InvalidOperationException()).GetFirst("Subject");
        composer.SetSubject(NormalizeSubjectImplementation(subject == null ? "" : subject.GetDecodedValue()));
        return composer;
    }

    static void AddInlineBodies(EmailComposer composer, EmailMessage original, EmailForwardOptions options)
    {
        List<EmailForwardHeaderLine> headers = GetForwardHeaders(original, options);
        string? originalText = options.GetBodyPolicy() == EmailForwardBodyPolicy.IncludeOriginal ? GetOriginalText(original, options.GetMaximumBodyCharacters()) : null;
        composer.SetText(ComposeInlineText(options.GetForwardText(), headers, originalText, options.GetBodyPolicy()));
        if (options.HasForwardHtml())
        {
            string? originalHtml = options.GetBodyPolicy() == EmailForwardBodyPolicy.IncludeOriginal ? GetOriginalHtml(original, options.GetMaximumBodyCharacters()) : null;
            composer.SetHtml(ComposeInlineHtml(options.GetForwardHtml(), headers, originalHtml, options.GetBodyPolicy()));
        }
    }

    static List<EmailForwardHeaderLine> GetForwardHeaders(EmailMessage original, EmailForwardOptions options)
    {
        List<EmailForwardHeaderLine> values = new List<EmailForwardHeaderLine>();
        if (options.GetBodyPolicy() == EmailForwardBodyPolicy.OmitOriginal)
        {
            return values;
        }

        int characters = 0;
        int index = 0;
        while (index < (original.GetHeaders() ?? throw new InvalidOperationException()).GetCount())
        {
            EmailHeader header = (original.GetHeaders() ?? throw new InvalidOperationException()).GetAt(index);
            if (ShouldIncludeSummaryHeader(header.GetName(), options.GetHeaderPolicy()))
            {
                if (values.Count >= options.GetMaximumHeaderCount())
                {
                    throw new EmailLimitException("MaxForwardHeaders", options.GetMaximumHeaderCount(), values.Count + 1);
                }

                string name = SanitizeText(header.GetName()).Trim();
                string value = CollapseWhitespace(SanitizeText(header.GetDecodedValue()));
                int added = name.Length + 2 + value.Length;
                if (added > options.GetMaximumHeaderCharacters() - characters)
                {
                    throw new EmailLimitException("MaxForwardHeaderCharacters", options.GetMaximumHeaderCharacters(), (long)characters + added);
                }

                values.Add(new EmailForwardHeaderLine(name, value));
                characters += added;
            }

            index++;
        }

        return values;
    }

    static bool ShouldIncludeSummaryHeader(string name, EmailForwardHeaderPolicy policy)
    {
        if (policy == EmailForwardHeaderPolicy.Preserve)
        {
            return true;
        }

        if (string.Equals(name, "From", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Date", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Subject", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (policy == EmailForwardHeaderPolicy.Minimal)
        {
            return false;
        }

        return string.Equals(name, "Sender", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Reply-To", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "To", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Cc", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Message-ID", StringComparison.OrdinalIgnoreCase);
    }

    static string ComposeInlineText(string introduction, List<EmailForwardHeaderLine> headers, string? originalBody, EmailForwardBodyPolicy bodyPolicy)
    {
        if (bodyPolicy == EmailForwardBodyPolicy.OmitOriginal)
        {
            return introduction;
        }

        StringBuilder output = new StringBuilder();
        AppendIntroduction(output, introduction, "\r\n\r\n");
        output.Append("---------- Forwarded message ----------\r\n");
        int index = 0;
        while (index < headers.Count)
        {
            output.Append(headers[index].GetName());
            output.Append(": ");
            output.Append(headers[index].GetValue());
            output.Append("\r\n");
            index++;
        }

        if (bodyPolicy == EmailForwardBodyPolicy.IncludeOriginal && originalBody != null)
        {
            output.Append("\r\n");
            output.Append(originalBody);
        }

        return output.ToString();
    }

    static string? ComposeInlineHtml(string? introduction, List<EmailForwardHeaderLine> headers, string? originalBody, EmailForwardBodyPolicy bodyPolicy)
    {
        if (bodyPolicy == EmailForwardBodyPolicy.OmitOriginal)
        {
            return introduction;
        }

        StringBuilder output = new StringBuilder();
        AppendIntroduction(output, introduction, "<br><br>");
        output.Append("<div>---------- Forwarded message ----------</div>");
        int index = 0;
        while (index < headers.Count)
        {
            output.Append("<div><b>");
            output.Append(EscapeHtml(headers[index].GetName()));
            output.Append(":</b> ");
            output.Append(EscapeHtml(headers[index].GetValue()));
            output.Append("</div>");
            index++;
        }

        if (bodyPolicy == EmailForwardBodyPolicy.IncludeOriginal && originalBody != null)
        {
            output.Append("<br><div class=\"forwarded-body\">");
            output.Append(originalBody);
            output.Append("</div>");
        }

        return output.ToString();
    }

    static void AppendIntroduction(StringBuilder output, string? introduction, string separator)
    {
        if ((introduction ?? throw new InvalidOperationException()).Length > 0)
        {
            output.Append(introduction);
            output.Append(separator);
        }
    }

    static string? GetOriginalText(EmailMessage original, int maximumCharacters)
    {
        if (!original.HasBody())
        {
            return null;
        }

        MimeEntity? plain = new EmailBodySelector().GetPlainTextWithMessage(original);
        if (plain != null)
        {
            return DecodeText(plain, maximumCharacters);
        }

        MimeEntity? html = new EmailBodySelector().GetHtmlWithMessage(original);
        if (html == null)
        {
            return null;
        }

        EmailHtmlTextOptions htmlOptions = new EmailHtmlTextOptions();
        htmlOptions.SetLimits(CreateHtmlLimits(maximumCharacters));
        string text = new EmailHtmlProcessor().ToTextWithEntityAndTextOptionsAndHtmlOptions(html, CreateTextOptions(maximumCharacters), htmlOptions).GetText();
        EnsureBodyLength(text, maximumCharacters);
        return text;
    }

    static string? GetOriginalHtml(EmailMessage original, int maximumCharacters)
    {
        if (!original.HasBody())
        {
            return null;
        }

        MimeEntity? html = new EmailBodySelector().GetHtmlWithMessage(original);
        if (html != null)
        {
            EmailHtmlSanitizeOptions htmlOptions = new EmailHtmlSanitizeOptions();
            htmlOptions.SetLimits(CreateHtmlLimits(maximumCharacters));
            string sanitized = new EmailHtmlProcessor().SanitizeWithEntityAndTextOptionsAndHtmlOptions(html, CreateTextOptions(maximumCharacters), htmlOptions).GetHtml();
            EnsureBodyLength(sanitized, maximumCharacters);
            return sanitized;
        }

        MimeEntity? plain = new EmailBodySelector().GetPlainTextWithMessage(original);
        if (plain == null)
        {
            return null;
        }

        string escaped = EscapeHtml(DecodeText(plain, maximumCharacters)).Replace("\r\n", "<br>").Replace("\r", "<br>").Replace("\n", "<br>");
        EnsureBodyLength(escaped, maximumCharacters);
        return escaped;
    }

    static string DecodeText(MimeEntity entity, int maximumCharacters)
    {
        string text = new EmailTextProcessor().DecodeWithEntityAndTextOptions(entity, CreateTextOptions(maximumCharacters)).GetText();
        EnsureBodyLength(text, maximumCharacters);
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

    static void EnsureBodyLength(string value, int maximumCharacters)
    {
        if (value.Length > maximumCharacters)
        {
            throw new EmailLimitException("MaxForwardBodyCharacters", maximumCharacters, value.Length);
        }
    }

    static void AddInlineAttachments(EmailComposer composer, EmailMessage original, EmailForwardOptions options)
    {
        if (options.GetAttachmentPolicy() == EmailForwardAttachmentPolicy.None || !original.HasBody())
        {
            return;
        }

        bool allowInline = options.GetAttachmentPolicy() == EmailForwardAttachmentPolicy.AttachmentsAndInlineResources && options.HasForwardHtml() && options.GetBodyPolicy() == EmailForwardBodyPolicy.IncludeOriginal;
        List<EmailAttachmentView> views = new EmailAttachmentFinder().EnumerateWithMessage(original);
        int includedCount = 0;
        long includedBytes = 0;
        int index = 0;
        while (index < views.Count)
        {
            EmailAttachmentView view = views[index];
            bool inline = allowInline && view.IsInlineResource() && (view.HasContentId() || view.HasContentLocation());
            bool attachment = view.IsAttachment();
            if (inline || attachment)
            {
                if (includedCount >= options.GetMaximumAttachmentCount())
                {
                    throw new EmailLimitException("MaxForwardAttachments", options.GetMaximumAttachmentCount(), includedCount + 1);
                }

                long remaining = options.GetMaximumAttachmentBytes() - includedBytes;
                if (remaining < 1)
                {
                    throw new EmailLimitException("MaxForwardAttachmentBytes", options.GetMaximumAttachmentBytes(), includedBytes + 1);
                }

                byte[] decoded = MaterializeDecoded(view.GetEntity(), remaining);
                includedBytes += decoded.Length;
                EmailAttachmentSource source = CreateAttachmentSource(view, decoded);
                if (inline)
                {
                    composer.AddRelatedResource(source);
                }
                else
                {
                    composer.AddAttachment(source);
                }

                includedCount++;
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
            source.SetContentId(entity.GetContentId());
        if (entity.HasContentLocation())
            source.SetContentLocation(entity.GetContentLocation());
        if (entity.HasContentDescription())
            source.SetDescription(entity.GetContentDescription());
        return source;
    }

    static MimeEntity CreateIntroduction(EmailForwardOptions options)
    {
        MimeEntity text = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset(options.GetForwardText(), "plain", ContainsNonAscii(options.GetForwardText()) ? "utf-8" : "us-ascii");
        if (!options.HasForwardHtml())
        {
            return text;
        }

        MimeEntity html = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset(options.GetForwardHtml(), "html", "utf-8");
        return new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", new List<MimeEntity> { text, html });
    }

    static EmailMessage CreateEmbeddedMessage(EmailMessage original, EmailForwardOptions options)
    {
        EmailHeaders headers = FilterEmbeddedHeaders(original.GetHeaders(), options);
        MimeEntity? body = original.HasBody() ? (original.GetBody() ?? throw new InvalidOperationException()).Clone() : null;
        return new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(headers, body, new EmailEnvelopeOperations().CreateWith(), null, original.GetOwnIssues());
    }

    static EmailHeaders FilterEmbeddedHeaders(EmailHeaders? original, EmailForwardOptions options)
    {
        List<EmailHeader> headers = new List<EmailHeader>();
        int characters = 0;
        int index = 0;
        while (index < (original ?? throw new InvalidOperationException()).GetCount())
        {
            EmailHeader header = original.GetAt(index);
            if (ShouldIncludeEmbeddedHeader(header.GetName(), options.GetHeaderPolicy()))
            {
                if (headers.Count >= options.GetMaximumHeaderCount())
                {
                    throw new EmailLimitException("MaxForwardHeaders", options.GetMaximumHeaderCount(), headers.Count + 1);
                }

                int added = header.GetName().Length + 2 + header.GetUnfoldedValue().Length;
                if (added > options.GetMaximumHeaderCharacters() - characters)
                {
                    throw new EmailLimitException("MaxForwardHeaderCharacters", options.GetMaximumHeaderCharacters(), (long)characters + added);
                }

                headers.Add(header.Clone());
                characters += added;
            }

            index++;
        }

        return new EmailHeadersOperations().CreateWithPreservedHeaders(headers);
    }

    static bool ShouldIncludeEmbeddedHeader(string name, EmailForwardHeaderPolicy policy)
    {
        if (policy == EmailForwardHeaderPolicy.Preserve)
        {
            return true;
        }

        if (policy == EmailForwardHeaderPolicy.Minimal)
        {
            return IsMinimalEmbeddedHeader(name);
        }

        return !IsUnsafeEmbeddedHeader(name);
    }

    static bool IsMinimalEmbeddedHeader(string name)
    {
        return string.Equals(name, "Date", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "From", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Sender", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Reply-To", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "To", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Cc", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Message-ID", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "In-Reply-To", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "References", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Subject", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "MIME-Version", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Content-Transfer-Encoding", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Content-ID", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Content-Description", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Content-Disposition", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Content-Language", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsUnsafeEmbeddedHeader(string name)
    {
        return string.Equals(name, "Bcc", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Resent-Bcc", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Return-Path", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Received", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Authentication-Results", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Original-Authentication-Results", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Received-SPF", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "DKIM-Signature", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "ARC-Message-Signature", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "ARC-Seal", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "ARC-Authentication-Results", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "X-Originating-IP", StringComparison.OrdinalIgnoreCase);
    }

    static MimeEntity CreateEmbeddedEntity(EmailMessage message, string filename)
    {
        EmailDisposition disposition = CreateAttachmentDisposition(filename);
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Message, new EmailHeadersOperations().CreateWith(), new EmailMimeDefaults().CreateMessageMediaType(false), disposition, null, null, null, null, null, null, null, new List<MimeEntity>(), message, null, null, null, new EmailIssueList());
    }

    static EmailDisposition CreateAttachmentDisposition(string filename)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(filename);
        EmailSourcePosition position = new EmailSourcePosition(0, 1, 1);
        List<EmailParameter> physical = new List<EmailParameter>();
        physical.Add(new EmailParameter("filename", bytes, bytes, true, new byte[0], position));
        List<EmailRfc2231DecodedParameter> decoded = new List<EmailRfc2231DecodedParameter>();
        decoded.Add(new EmailRfc2231DecodedParameter("filename", bytes, "utf-8", true, null, false, true, false, 1, new List<EmailRfc2231Parameter>()));
        return new EmailDisposition("attachment", new EmailParameterList(physical, decoded), new byte[0], position);
    }

    static MimeEntity? GenerateBoundaries(MimeEntity value)
    {
        EmailGenerationOptions generation = new EmailGenerationOptions();
        generation.SetGenerateDate(false);
        generation.SetGenerateMessageId(false);
        generation.SetGenerateContentIds(false);
        generation.SetGenerateMimeVersion(false);
        generation.SetGenerateBoundaries(true);
        generation.SetDeterministicOutput(true);
        generation.SetIdentifierDomain("local.invalid");
        generation.SetDeterministicSeed(Encoding.ASCII.GetBytes("email-forward-attached-boundary-v1"));
        generation.ClearGenerationDate();
        generation.SetMaximumDepth(EmailTraversalConstants.DefaultMaximumDepth);
        generation.SetMaximumEntityCount(EmailTraversalConstants.DefaultMaximumEntityCount);
        return new EmailGenerator().GenerateWithEntityAndOptions(value, generation).GetEntity();
    }

    static bool ContainsNonAscii(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool HasForwardPrefix(string value)
    {
        int length = GetForwardPrefixLength(value);
        return length > 0 && length < value.Length && value[length] == ':';
    }

    static string RemoveForwardPrefix(string value)
    {
        int length = GetForwardPrefixLength(value);
        return value.Substring(length + 1);
    }

    static int GetForwardPrefixLength(string value)
    {
        int index;
        if (value.Length >= 3 && IsAsciiEqual(value[0], 'f') && IsAsciiEqual(value[1], 'w') && IsAsciiEqual(value[2], 'd'))
        {
            index = 3;
        }
        else if (value.Length >= 2 && IsAsciiEqual(value[0], 'f') && IsAsciiEqual(value[1], 'w'))
        {
            index = 2;
        }
        else
        {
            return 0;
        }

        if (index < value.Length && value[index] == '[')
        {
            index++;
            int start = index;
            while (index < value.Length && value[index] >= '0' && value[index] <= '9')
                index++;
            if (index == start || index >= value.Length || value[index] != ']')
                return 0;
            index++;
        }

        return index;
    }

    static bool IsAsciiEqual(char value, char lower)
    {
        return value == lower || value == (char)(lower - 32);
    }

    static string CollapseWhitespace(string value)
    {
        StringBuilder output = new StringBuilder();
        bool pending = false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (char.IsWhiteSpace(current))
            {
                pending = output.Length > 0;
            }
            else
            {
                if (pending)
                    output.Append(' ');
                output.Append(current);
                pending = false;
            }

            index++;
        }

        return output.ToString();
    }

    static string SanitizeText(string? value)
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

    static string EscapeHtml(string value)
    {
        return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");
    }
}
