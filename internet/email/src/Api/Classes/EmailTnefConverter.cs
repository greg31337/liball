using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with TNEF converter.
/// </summary>
public class EmailTnefConverter : IEmailTnefConverterContract
{
    internal static EmailTnefConversionResult ConvertImplementationWithEntity(MimeEntity entity)
    {
        return ConvertImplementationWithEntityAndOptions(entity, new EmailTnefConversionOptions());
    }

    /// <summary>
    /// Converts the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The TNEF conversion result produced by the operation.</returns>
    public EmailTnefConversionResult ConvertWithEntity(MimeEntity entity)
    {
        return EmailTnefConverter.ConvertImplementationWithEntity(entity);
    }

    internal static EmailTnefConversionResult ConvertImplementationWithEntityAndOptions(MimeEntity entity, EmailTnefConversionOptions options)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailTnefConversionOptions snapshot = options.Clone();
        EmailTnefParseResult parsed = new EmailTnefReader().ParseWithEntityAndOptions(entity, snapshot.GetReaderOptions());
        return ConvertParsed(parsed, snapshot);
    }

    /// <summary>
    /// Converts the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The TNEF conversion result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailTnefConversionResult ConvertWithEntityAndOptions(MimeEntity entity, EmailTnefConversionOptions options)
    {
        return EmailTnefConverter.ConvertImplementationWithEntityAndOptions(entity, options);
    }

    internal static EmailTnefConversionResult ConvertImplementationWithParsed(EmailTnefParseResult parsed)
    {
        return ConvertImplementationWithParsedAndOptions(parsed, new EmailTnefConversionOptions());
    }

    /// <summary>
    /// Converts the supplied value.
    /// </summary>
    /// <param name = "parsed">The parsed used by the operation.</param>
    /// <returns>The TNEF conversion result produced by the operation.</returns>
    public EmailTnefConversionResult ConvertWithParsed(EmailTnefParseResult parsed)
    {
        return EmailTnefConverter.ConvertImplementationWithParsed(parsed);
    }

    internal static EmailTnefConversionResult ConvertImplementationWithParsedAndOptions(EmailTnefParseResult parsed, EmailTnefConversionOptions options)
    {
        if (parsed == null)
            throw new ArgumentNullException("parsed");
        if (options == null)
            throw new ArgumentNullException("options");
        return ConvertParsed(parsed, options.Clone());
    }

    /// <summary>
    /// Converts the supplied value.
    /// </summary>
    /// <param name = "parsed">The parsed used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The TNEF conversion result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailTnefConversionResult ConvertWithParsedAndOptions(EmailTnefParseResult parsed, EmailTnefConversionOptions options)
    {
        return EmailTnefConverter.ConvertImplementationWithParsedAndOptions(parsed, options);
    }

    static EmailTnefConversionResult ConvertParsed(EmailTnefParseResult parsed, EmailTnefConversionOptions options)
    {
        EmailIssueList issues = parsed.GetIssues();
        List<EmailTnefConvertedAttachment> mappings = new List<EmailTnefConvertedAttachment>();
        if (!parsed.HasMessage())
        {
            AddIssue(issues, EmailIssueSeverity.Error, EmailIssueCode.TnefConversionFailed, "No parsed TNEF message is available for MIME conversion.", "The original parse result and diagnostics were retained.");
            return new EmailTnefConversionResult(parsed, null, null, mappings, issues);
        }

        EmailTnefMessage? message = parsed.GetMessage();
        EmailTnefConversionBudget budget = new EmailTnefConversionBudget(options.GetMaximumEntityCount());
        MimeEntity? converted = BuildMessageBody(message, options, budget, issues, mappings);
        if (converted == null)
            AddIssue(issues, EmailIssueSeverity.Error, EmailIssueCode.TnefConversionFailed, "The TNEF message contains no convertible body or attachment content.", "The original TNEF message, attributes, properties, and diagnostics were retained.");
        return new EmailTnefConversionResult(parsed, message, converted, mappings, issues);
    }

    static MimeEntity? BuildMessageBody(EmailTnefMessage? message, EmailTnefConversionOptions options, EmailTnefConversionBudget budget, EmailIssueList issues, List<EmailTnefConvertedAttachment> mappings)
    {
        AddRetentionDiagnostic(message, issues);
        MimeEntity? body = BuildBodies((message ?? throw new InvalidOperationException()).GetBodies(), options, budget, issues);
        List<MimeEntity> inline = new List<MimeEntity>();
        List<MimeEntity> regular = new List<MimeEntity>();
        List<EmailTnefAttachment> attachments = message.GetAttachments();
        int index = 0;
        while (index < attachments.Count)
        {
            EmailTnefAttachment attachment = attachments[index];
            MimeEntity? part = BuildAttachment(attachment, options, budget, issues, mappings);
            if (part != null)
            {
                if (part.GetKind() != MimeEntityKind.Message && part.HasContentId() && body != null)
                    inline.Add(part);
                else
                    regular.Add(part);
            }

            index++;
        }

        if (inline.Count > 0 && body != null)
        {
            List<MimeEntity> relatedChildren = new List<MimeEntity>();
            relatedChildren.Add(body);
            relatedChildren.AddRange(inline);
            body = CreateMultipart("related", relatedChildren, budget);
        }
        else if (inline.Count > 0)
        {
            regular.AddRange(inline);
        }

        if (regular.Count == 0)
            return body;
        if (body == null && regular.Count == 1)
            return regular[0];
        List<MimeEntity> mixedChildren = new List<MimeEntity>();
        if (body != null)
            mixedChildren.Add(body);
        mixedChildren.AddRange(regular);
        return CreateMultipart("mixed", mixedChildren, budget);
    }

    static MimeEntity? BuildBodies(List<EmailTnefBody> bodies, EmailTnefConversionOptions options, EmailTnefConversionBudget budget, EmailIssueList issues)
    {
        List<MimeEntity> alternatives = new List<MimeEntity>();
        int index = 0;
        while (index < bodies.Count)
        {
            EmailTnefBody body = bodies[index];
            MimeEntity? part = null;
            if (body.GetKind() == EmailTnefBodyKind.RichText && !options.GetIncludeRichTextBody())
            {
                AddIssue(issues, EmailIssueSeverity.Information, EmailIssueCode.TnefConversionLossy, "The RTF body alternative was not emitted by conversion policy.", "The original compressed and decoded RTF bytes remain available on the TNEF body model.");
            }
            else if (body.GetKind() == EmailTnefBodyKind.PlainText && body.HasText())
            {
                budget.AddEntity();
                part = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset(body.GetText(), "plain", "utf-8");
            }
            else if (body.GetKind() == EmailTnefBodyKind.Html && body.HasText())
            {
                budget.AddEntity();
                part = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset(body.GetText(), "html", "utf-8");
            }
            else if (body.GetKind() == EmailTnefBodyKind.RichText && body.GetContent().Length > 0)
            {
                budget.AddEntity();
                part = new EmailEntityFactory().CreateBinaryWithValueAndTypeAndSubtype(body.GetContent(), "text", "rtf");
            }
            else
            {
                AddIssue(issues, EmailIssueSeverity.Warning, EmailIssueCode.TnefConversionLossy, "A TNEF body alternative has no usable converted content.", "Its original bytes remain available on the TNEF body model.");
            }

            if (part != null)
                alternatives.Add(part);
            index++;
        }

        if (alternatives.Count == 0)
            return null;
        if (alternatives.Count == 1)
            return alternatives[0];
        return CreateMultipart("alternative", alternatives, budget);
    }

    static MimeEntity? BuildAttachment(EmailTnefAttachment attachment, EmailTnefConversionOptions options, EmailTnefConversionBudget budget, EmailIssueList issues, List<EmailTnefConvertedAttachment> mappings)
    {
        MimeEntity? part = null;
        if (attachment.HasEmbeddedMessage())
        {
            EmailTnefParseResult? embeddedParse = attachment.GetEmbeddedParseResult();
            issues.AddRange((embeddedParse ?? throw new InvalidOperationException()).GetIssues());
            MimeEntity? nestedBody = BuildMessageBody(attachment.GetEmbeddedMessage(), options, budget, issues, mappings);
            EmailHeaders headers = CreateEmbeddedHeaders(attachment.GetEmbeddedMessage(), issues);
            EmailMessage nestedMessage = new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(headers, nestedBody, new EmailEnvelopeOperations().CreateWith(), null, embeddedParse.GetIssues());
            EmailMessageId? contentId = ParseContentId(attachment.GetContentId(), issues);
            EmailDisposition disposition = CreateDisposition("attachment", SafeFilename(attachment.GetFilename(), issues));
            budget.AddEntity();
            part = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Message, new EmailHeadersOperations().CreateWith(), new EmailMimeDefaults().CreateMessageMediaType(false), disposition, null, contentId, SafeLocation(attachment.GetContentLocation(), issues), null, null, null, null, new List<MimeEntity>(), nestedMessage, null, null, null, new EmailIssueList());
        }
        else if (attachment.HasContent())
        {
            EmailIssueList partIssues = new EmailIssueList();
            EmailMediaType mediaType = ParseAttachmentMediaType(attachment.GetContentType(), partIssues);
            string? filename = SafeFilename(attachment.GetFilename(), partIssues);
            EmailMessageId? contentId = ParseContentId(attachment.GetContentId(), partIssues);
            string? location = SafeLocation(attachment.GetContentLocation(), partIssues);
            string dispositionType = contentId == null ? "attachment" : "inline";
            EmailDisposition disposition = CreateDisposition(dispositionType, filename);
            budget.AddEntity();
            part = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Leaf, new EmailHeadersOperations().CreateWith(), mediaType, disposition, "base64", contentId, location, null, null, null, new EmailContentOperations().CreateMemoryCopy(attachment.GetContent(), false), new List<MimeEntity>(), null, null, null, null, partIssues);
            issues.AddRange(partIssues);
        }
        else
        {
            AddIssue(issues, EmailIssueSeverity.Warning, EmailIssueCode.TnefAttachmentSkipped, "A TNEF attachment has neither by-value content nor a parsed embedded message.", "The original attachment attributes and properties were retained without producing a MIME part.");
        }

        mappings.Add(new EmailTnefConvertedAttachment(attachment, part));
        return part;
    }

    static EmailHeaders CreateEmbeddedHeaders(EmailTnefMessage? message, EmailIssueList issues)
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWith();
        if ((message ?? throw new InvalidOperationException()).HasSubject())
        {
            string? subject = message.GetSubject();
            if (IsSafeMetadata(subject))
                headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Subject", Encoding.UTF8.GetBytes(" " + subject), EmailHeaderValueKind.Unstructured));
            else
                AddIssue(issues, EmailIssueSeverity.Warning, EmailIssueCode.TnefConversionLossy, "An embedded TNEF subject contains prohibited line-break or NUL characters.", "The subject remains available on the original TNEF message but was not emitted as a MIME header.");
        }

        return headers;
    }

    static EmailMediaType ParseAttachmentMediaType(string? value, EmailIssueList issues)
    {
        if (value != null && IsAscii(value))
        {
            EmailMediaTypeParseResult parsed = new EmailMediaTypeParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(value), EmailStructuredValueParseMode.Strict);
            if (parsed.IsSuccess() && parsed.IsComplete() && IsLeafMediaType(parsed.GetValue()))
                return parsed.GetValue();
        }

        if (value != null)
            AddIssue(issues, EmailIssueSeverity.Warning, EmailIssueCode.TnefContentTypeInvalid, "A TNEF attachment MIME tag is invalid or names a container type.", "The attachment was emitted as application/octet-stream and the original MIME tag was retained.");
        return CreateMediaType("application", "octet-stream");
    }

    static EmailMessageId? ParseContentId(string? value, EmailIssueList issues)
    {
        if (value == null)
            return null;
        string normalized = value;
        if (!(value.Length >= 2 && value[0] == '<' && value[value.Length - 1] == '>'))
            normalized = "<" + value + ">";
        if (IsAscii(normalized))
        {
            EmailMessageIdParseResult parsed = new EmailMessageIdParser().ParseContentIdWithInputAndMode(Encoding.ASCII.GetBytes(normalized), EmailMessageIdParseMode.Strict);
            if (parsed.IsSuccess() && parsed.IsComplete() && parsed.HasSingleIdentifier())
                return parsed.GetIdentifier();
        }

        AddIssue(issues, EmailIssueSeverity.Warning, EmailIssueCode.TnefContentIdInvalid, "A TNEF attachment content ID is not a valid MIME Content-ID value.", "The content ID remained on the original attachment but was omitted from the MIME part.");
        return null;
    }

    static string? SafeFilename(string? value, EmailIssueList issues)
    {
        if (value == null)
            return null;
        if (IsSafeMetadata(value))
            return value;
        AddIssue(issues, EmailIssueSeverity.Warning, EmailIssueCode.TnefConversionLossy, "A TNEF attachment filename contains prohibited line-break or NUL characters.", "The original filename was retained but omitted from Content-Disposition.");
        return null;
    }

    static string? SafeLocation(string? value, EmailIssueList issues)
    {
        if (value == null)
            return null;
        if (IsSafeMetadata(value))
            return value;
        AddIssue(issues, EmailIssueSeverity.Warning, EmailIssueCode.TnefConversionLossy, "A TNEF attachment content location contains prohibited line-break or NUL characters.", "The original content location was retained but omitted from the MIME part.");
        return null;
    }

    static EmailDisposition CreateDisposition(string type, string? filename)
    {
        EmailParameterList parameters = EmptyParameters();
        if (filename != null)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(filename);
            EmailSourcePosition position = new EmailSourcePosition(0, 1, 1);
            List<EmailParameter> physical = new List<EmailParameter>();
            physical.Add(new EmailParameter("filename", bytes, bytes, true, new byte[0], position));
            List<EmailRfc2231DecodedParameter> decoded = new List<EmailRfc2231DecodedParameter>();
            decoded.Add(new EmailRfc2231DecodedParameter("filename", bytes, "utf-8", true, null, false, true, false, 1, new List<EmailRfc2231Parameter>()));
            parameters = new EmailParameterList(physical, decoded);
        }

        return new EmailDisposition(type, parameters, new byte[0], new EmailSourcePosition(0, 1, 1));
    }

    static MimeEntity CreateMultipart(string subtype, List<MimeEntity> children, EmailTnefConversionBudget budget)
    {
        budget.AddEntity();
        return new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren(subtype, children);
    }

    static EmailMediaType CreateMediaType(string type, string subtype)
    {
        return new EmailMediaType(type, subtype, EmptyParameters(), new byte[0], new EmailSourcePosition(0, 1, 1), false);
    }

    static EmailParameterList EmptyParameters()
    {
        return new EmailParameterList(new List<EmailParameter>(), new List<EmailRfc2231DecodedParameter>());
    }

    static bool IsLeafMediaType(EmailMediaType value)
    {
        if (string.Equals(value.GetTopLevelType(), "multipart", StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.Equals(value.GetTopLevelType(), "message", StringComparison.OrdinalIgnoreCase))
            return false;
        return !value.IsType("application", "ms-tnef") && !value.IsType("application", "vnd.ms-tnef") && !value.IsType("application", "x-tnef");
    }

    static bool IsAscii(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
                return false;
            index++;
        }

        return true;
    }

    static bool IsSafeMetadata(string? value)
    {
        return (value ?? throw new InvalidOperationException()).Length > 0 && value.IndexOf('\r') < 0 && value.IndexOf('\n') < 0 && value.IndexOf('\0') < 0;
    }

    static void AddRetentionDiagnostic(EmailTnefMessage? message, EmailIssueList issues)
    {
        int propertyCount = (message ?? throw new InvalidOperationException()).GetProperties().Count;
        int attachmentPropertyCount = 0;
        List<EmailTnefAttachment> attachments = message.GetAttachments();
        int index = 0;
        while (index < attachments.Count)
        {
            attachmentPropertyCount += attachments[index].GetProperties().Count;
            index++;
        }

        int recipientCount = message.GetRecipientCount();
        if (propertyCount == 0 && attachmentPropertyCount == 0 && recipientCount == 0)
            return;
        AddIssue(issues, EmailIssueSeverity.Information, EmailIssueCode.TnefConversionLossy, propertyCount + " original TNEF message properties, " + attachmentPropertyCount + " attachment properties, and " + recipientCount + " recipient rows were retained outside the generated MIME body tree.", "Inspect the conversion result's original TNEF message and attachment mappings when those values are needed.");
    }

    static void AddIssue(EmailIssueList issues, EmailIssueSeverity severity, EmailIssueCode code, string description, string recovery)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, null, null, null, recovery));
    }
}
