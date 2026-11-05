using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailValidationWorker
{
    EmailValidationOptions options;
    EmailIssueList syntaxIssues;
    EmailIssueList semanticIssues;
    int validatedEntityCount;
    int validatedHeaderCount;

    internal EmailValidationWorker(EmailValidationOptions options)
    {
        this.options = options;
        syntaxIssues = new EmailIssueList();
        semanticIssues = new EmailIssueList();
        validatedEntityCount = 0;
        validatedHeaderCount = 0;
    }

    internal EmailValidationResult Validate(EmailMessage message)
    {
        ValidateMessage(message, null);
        if (message.HasBody())
        {
            ValidateTree(message.GetBody());
        }

        return CreateResult();
    }

    internal EmailValidationResult ValidateMimeEntity(MimeEntity entity)
    {
        ValidateTree(entity);
        return CreateResult();
    }

    EmailValidationResult CreateResult()
    {
        return new EmailValidationResult(syntaxIssues, semanticIssues, validatedEntityCount, validatedHeaderCount);
    }

    void ValidateTree(MimeEntity? root)
    {
        List<MimeEntity> entities = new EmailTraversal().DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(root, options.GetMaximumDepth(), options.GetMaximumEntityCount());
        int index = 0;
        while (index < entities.Count)
        {
            MimeEntity entity = entities[index];
            EmailPath? path = new EmailTraversal().GetPathWithRootAndTargetAndMaximumDepthAndMaximumEntityCount(root, entity, options.GetMaximumDepth(), options.GetMaximumEntityCount());
            ValidateEntity(entity, path);
            if (entity.HasNestedMessage())
            {
                ValidateMessage(entity.GetNestedMessage(), path);
            }

            index++;
        }

        validatedEntityCount += entities.Count;
        if (options.GetValidateSemantics())
        {
            ValidateDuplicateContentIds(root, entities);
        }
    }

    void ValidateMessage(EmailMessage? message, EmailPath? path)
    {
        AddExistingIssues((message ?? throw new InvalidOperationException()).GetOwnIssues(), path);
        EmailHeaders headers = GetMessageHeaders(message.GetHeaders());
        ValidateHeaders(headers, options.GetMessageHeaderOptions(), path);
        if (!options.GetValidateSemantics())
        {
            return;
        }

        if (!message.HasBody())
        {
            AddSemantic(EmailIssueSeverity.Error, EmailIssueCode.ValidationMessageBodyMissing, "The message has no MIME body entity.", null, path, null, "Add an explicit body entity, including an empty text body when appropriate.");
            return;
        }

        if (options.GetRequireMimeVersion() && UsesMime(message) && !(message.GetHeaders() ?? throw new InvalidOperationException()).Contains("MIME-Version"))
        {
            AddSemantic(EmailIssueSeverity.Error, EmailIssueCode.ValidationMimeVersionMissing, "The message uses MIME content but has no MIME-Version field.", null, path, "MIME-Version", "Add 'MIME-Version: 1.0' without changing the MIME body.");
        }
    }

    void ValidateEntity(MimeEntity entity, EmailPath? path)
    {
        AddExistingIssues(entity.GetOwnIssues(), path);
        ValidateHeaders(entity.GetHeaders(), options.GetEntityHeaderOptions(), path);
        if (options.GetValidateSyntax())
        {
            ValidateBoundary(entity, path);
            ValidateTransferSyntax(entity, path);
        }

        if (options.GetValidateSemantics())
        {
            ValidateEntitySemantics(entity, path);
        }
    }

    void ValidateHeaders(EmailHeaders? headers, EmailHeaderValidationOptions headerOptions, EmailPath? path)
    {
        EmailHeaderValidationResult result = new EmailHeaderValidator().ValidateWithHeadersAndOptions(headers, headerOptions);
        validatedHeaderCount += result.GetValidatedHeaderCount();
        EmailIssueList issues = result.GetIssues();
        int index = 0;
        while (index < issues.GetCount())
        {
            AddExistingIssue(issues.GetAt(index), path);
            index++;
        }
    }

    void ValidateBoundary(MimeEntity entity, EmailPath? path)
    {
        if (!entity.IsContainer())
        {
            return;
        }

        string? boundary = GetParameterText(entity.GetMediaType(), "boundary");
        if (boundary == null)
        {
            AddSyntax(EmailIssueSeverity.Error, EmailIssueCode.ValidationMimeBoundaryMissing, "The multipart content type has no boundary parameter.", (entity.GetMediaType() ?? throw new InvalidOperationException()).GetSourcePosition(), path, "Content-Type", "Add a unique boundary parameter and serialize matching delimiter lines.");
        }
        else if (!IsBoundaryValid(boundary))
        {
            AddSyntax(EmailIssueSeverity.Error, EmailIssueCode.ValidationMimeBoundaryInvalid, "The multipart boundary is empty, too long, or contains a character not permitted by MIME.", (entity.GetMediaType() ?? throw new InvalidOperationException()).GetSourcePosition(), path, "Content-Type", "Choose a boundary of 1 to 70 permitted ASCII characters that does not end in space.");
        }
    }

    void ValidateEntitySemantics(MimeEntity entity, EmailPath? path)
    {
        ValidateTransferEncoding(entity, path);
        ValidateCharset(entity, path);
        if ((entity.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "alternative") && entity.GetChildCount() < 2)
        {
            AddSemantic(EmailIssueSeverity.Warning, EmailIssueCode.ValidationAlternativeStructure, "Multipart/alternative contains fewer than two representations.", (entity.GetMediaType() ?? throw new InvalidOperationException()).GetSourcePosition(), path, "Content-Type", "Add another representation or use the single child directly.");
        }

        if ((entity.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "related"))
        {
            ValidateRelatedStart(entity, (path ?? throw new InvalidOperationException()));
        }

        if (entity.GetKind() == MimeEntityKind.PartialMessage)
        {
            ValidatePartialMessage(entity, (path ?? throw new InvalidOperationException()));
        }
        else if (entity.GetKind() == MimeEntityKind.ExternalBody)
        {
            ValidateExternalBody(entity, (path ?? throw new InvalidOperationException()));
        }
        else if (entity.GetKind() == MimeEntityKind.Message && !(entity.GetNestedMessage() ?? throw new InvalidOperationException()).HasBody())
        {
            AddSemantic(EmailIssueSeverity.Error, EmailIssueCode.ValidationNestedMessageBodyMissing, "The embedded message has no MIME body entity.", (entity.GetMediaType() ?? throw new InvalidOperationException()).GetSourcePosition(), path, "Content-Type", "Add an explicit body to the embedded message.");
        }

        if ((entity.GetKind() == MimeEntityKind.Signed || entity.GetKind() == MimeEntityKind.Encrypted) && GetParameterText(entity.GetMediaType(), "protocol") == null)
        {
            AddSemantic(EmailIssueSeverity.Error, EmailIssueCode.ValidationSecurityProtocolMissing, "The security multipart has no protocol parameter.", (entity.GetMediaType() ?? throw new InvalidOperationException()).GetSourcePosition(), path, "Content-Type", "Declare the media type used by the signature or encryption control part.");
        }
    }

    void ValidateTransferSyntax(MimeEntity entity, EmailPath? path)
    {
        if (!entity.HasRawContent() || !entity.HasKnownTransferEncoding())
        {
            return;
        }

        EmailTransferEncodingKind kind = entity.GetTransferEncodingKind();
        byte[] bytes = (entity.GetRawContent() ?? throw new InvalidOperationException()).Materialize(options.GetMaximumContentBytes());
        EmailIssueList issues;
        if (kind == EmailTransferEncodingKind.Base64)
        {
            issues = new EmailBase64().Decode(bytes, EmailBase64DecodeMode.Strict, options.GetMaximumContentBytes()).GetIssues();
        }
        else if (kind == EmailTransferEncodingKind.QuotedPrintable)
        {
            issues = new EmailQuotedPrintable().Decode(bytes, EmailQuotedPrintableDecodeMode.Strict, options.GetMaximumContentBytes()).GetIssues();
        }
        else
        {
            issues = new EmailTransferEncoding().ValidateWithInputAndKindAndMaximumBytesAndMaximumLineBytes(bytes, kind, options.GetMaximumContentBytes(), EmailTransferEncodingConstants.DefaultMaximumLineBytes).GetIssues();
        }

        AddContentSyntaxIssues(issues, entity, path);
    }

    void AddContentSyntaxIssues(EmailIssueList issues, MimeEntity entity, EmailPath? path)
    {
        int index = 0;
        while (index < issues.GetCount())
        {
            EmailIssue issue = issues.GetAt(index);
            EmailSourcePosition? position = issue.HasSourcePosition() ? issue.GetSourcePosition() : GetFieldPosition(entity, "Content-Transfer-Encoding");
            string? recovery = issue.HasRecoveryDescription() ? issue.GetRecoveryDescription() : "Correct the encoded body without changing its decoded meaning.";
            syntaxIssues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(issue.GetSeverity(), issue.GetCode(), issue.GetDescription(), position, path, "Content-Transfer-Encoding", recovery));
            index++;
        }
    }

    void ValidateTransferEncoding(MimeEntity entity, EmailPath? path)
    {
        if (entity.HasTransferEncoding() && !entity.HasKnownTransferEncoding())
        {
            AddSemantic(EmailIssueSeverity.Error, EmailIssueCode.ValidationTransferEncodingUnsupported, "The content-transfer-encoding is syntactically valid but unsupported.", GetFieldPosition(entity, "Content-Transfer-Encoding"), path, "Content-Transfer-Encoding", "Select a supported transfer encoding before decoding or transport.");
            return;
        }

        if ((entity.IsContainer() || entity.GetKind() == MimeEntityKind.Message) && entity.HasKnownTransferEncoding())
        {
            EmailTransferEncodingKind kind = entity.GetTransferEncodingKind();
            if (kind == EmailTransferEncodingKind.Base64 || kind == EmailTransferEncodingKind.QuotedPrintable)
            {
                AddSemantic(EmailIssueSeverity.Error, EmailIssueCode.ValidationContainerTransferEncoding, "Multipart and embedded-message entities cannot use Base64 or quoted-printable as their enclosing transfer encoding.", GetFieldPosition(entity, "Content-Transfer-Encoding"), path, "Content-Transfer-Encoding", "Use 7bit, 8bit, or binary on the container and encode its leaf parts individually.");
            }
        }
    }

    void ValidateCharset(MimeEntity entity, EmailPath? path)
    {
        if (!string.Equals((entity.GetMediaType() ?? throw new InvalidOperationException()).GetTopLevelType(), "text", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string? charset = GetParameterText(entity.GetMediaType(), "charset");
        if (charset == null)
        {
            return;
        }

        EmailCharsetLookupResult lookup = new EmailCharsetRegistry().Lookup(charset);
        if (!lookup.IsFound())
        {
            AddSemantic(EmailIssueSeverity.Error, EmailIssueCode.ValidationCharsetUnsupported, "The declared text charset is not supported by the configured charset registry.", (entity.GetMediaType() ?? throw new InvalidOperationException()).GetSourcePosition(), path, "Content-Type", "Register the charset or relabel the content only after verifying its bytes.");
        }
    }

    void ValidateRelatedStart(MimeEntity entity, EmailPath path)
    {
        string? start = GetParameterText(entity.GetMediaType(), "start");
        if (start == null)
        {
            return;
        }

        int index = 0;
        while (index < entity.GetChildCount())
        {
            MimeEntity child = entity.GetChildAt(index);
            if (child.HasContentId() && ContentIdMatches(child.GetContentId(), start))
            {
                return;
            }

            index++;
        }

        AddSemantic(EmailIssueSeverity.Error, EmailIssueCode.ValidationRelatedStartUnresolved, "The multipart/related start parameter does not identify a direct child.", (entity.GetMediaType() ?? throw new InvalidOperationException()).GetSourcePosition(), path, "Content-Type", "Correct the start content ID or add the identified root part as a direct child.");
    }

    void ValidatePartialMessage(MimeEntity entity, EmailPath path)
    {
        EmailPartialMessageMetadata? metadata = entity.GetPartialMessageMetadata();
        if (metadata == null || !metadata.HasRequiredMetadata() || !metadata.IsSequenceConsistent())
        {
            AddSemantic(EmailIssueSeverity.Error, EmailIssueCode.ValidationPartialMessageMetadata, "Message/partial metadata is missing its id or number, or its number exceeds the declared total.", (entity.GetMediaType() ?? throw new InvalidOperationException()).GetSourcePosition(), path, "Content-Type", "Supply a non-empty id, a positive number, and a total no smaller than the number.");
        }
    }

    void ValidateExternalBody(MimeEntity entity, EmailPath path)
    {
        EmailExternalBodyMetadata? metadata = entity.GetExternalBodyMetadata();
        if (metadata == null || !metadata.HasAccessType() || !metadata.HasValidAccessType() || !metadata.HasKnownPermission())
        {
            AddSemantic(EmailIssueSeverity.Error, EmailIssueCode.ValidationExternalBodyMetadata, "Message/external-body metadata has no valid access type or uses an unknown permission.", (entity.GetMediaType() ?? throw new InvalidOperationException()).GetSourcePosition(), path, "Content-Type", "Supply a registered access-type and a read or read-write permission.");
        }
    }

    void ValidateDuplicateContentIds(MimeEntity? root, List<MimeEntity> entities)
    {
        Dictionary<string, MimeEntity> firstById = new Dictionary<string, MimeEntity>(StringComparer.Ordinal);
        int index = 0;
        while (index < entities.Count)
        {
            MimeEntity entity = entities[index];
            if (entity.HasContentId())
            {
                string key = new EmailRelatedUri().NormalizeContentIdWithIdentifier(entity.GetContentId());
                if (firstById.ContainsKey(key))
                {
                    EmailPath? path = new EmailTraversal().GetPathWithRootAndTargetAndMaximumDepthAndMaximumEntityCount(root, entity, options.GetMaximumDepth(), options.GetMaximumEntityCount());
                    AddSemantic(EmailIssueSeverity.Error, EmailIssueCode.ValidationDuplicateContentId, "The Content-ID duplicates another entity in the same MIME tree: " + key + ".", (entity.GetContentId() ?? throw new InvalidOperationException()).GetSourcePosition(), path, "Content-ID", "Assign a unique Content-ID and update references deliberately.");
                }
                else
                {
                    firstById.Add(key, entity);
                }
            }

            index++;
        }
    }

    void AddExistingIssues(EmailIssueList issues, EmailPath? path)
    {
        int index = 0;
        while (index < issues.GetCount())
        {
            AddExistingIssue(issues.GetAt(index), path);
            index++;
        }
    }

    void AddExistingIssue(EmailIssue issue, EmailPath? path)
    {
        bool semantic = IsSemanticCode(issue.GetCode());
        if (semantic && !options.GetValidateSemantics() || !semantic && !options.GetValidateSyntax())
        {
            return;
        }

        EmailPath? evidencePath = issue.HasEntityPath() ? issue.GetEntityPath() : path;
        EmailSourcePosition? position = issue.HasSourcePosition() ? issue.GetSourcePosition() : null;
        string? field = issue.HasFieldName() ? issue.GetFieldName() : null;
        string? recovery = issue.HasRecoveryDescription() ? issue.GetRecoveryDescription() : null;
        EmailIssue copy = new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(issue.GetSeverity(), issue.GetCode(), issue.GetDescription(), position, evidencePath, field, recovery);
        if (semantic)
        {
            semanticIssues.Add(copy);
        }
        else
        {
            syntaxIssues.Add(copy);
        }
    }

    void AddSyntax(EmailIssueSeverity severity, EmailIssueCode code, string description, EmailSourcePosition position, EmailPath? path, string field, string recovery)
    {
        syntaxIssues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, path, field, recovery));
    }

    void AddSemantic(EmailIssueSeverity severity, EmailIssueCode code, string description, EmailSourcePosition? position, EmailPath? path, string? field, string recovery)
    {
        semanticIssues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, path, field, recovery));
    }

    static EmailHeaders GetMessageHeaders(EmailHeaders? source)
    {
        EmailHeaders selected = new EmailHeadersOperations().CreateWith();
        int index = 0;
        while (index < (source ?? throw new InvalidOperationException()).GetCount())
        {
            EmailHeader header = source.GetAt(index);
            if (!header.GetName().StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
            {
                selected.Add(header);
            }

            index++;
        }

        return selected;
    }

    static bool UsesMime(EmailMessage message)
    {
        int index = 0;
        while (index < (message.GetHeaders() ?? throw new InvalidOperationException()).GetCount())
        {
            if ((message.GetHeaders() ?? throw new InvalidOperationException()).GetAt(index).GetName().StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index++;
        }

        MimeEntity? body = message.GetBody();
        return (body ?? throw new InvalidOperationException()).GetKind() != MimeEntityKind.Leaf || !(body.GetMediaType() ?? throw new InvalidOperationException()).WasDefaulted() || body.HasTransferEncoding() || body.HasDisposition() || body.HasContentId() || body.HasContentLocation() || body.HasContentLanguage() || body.HasContentDescription();
    }

    static string? GetParameterText(EmailMediaType? mediaType, string name)
    {
        EmailRfc2231DecodedParameter? decoded = (mediaType ?? throw new InvalidOperationException()).GetParameters().GetDecoded(name);
        if (decoded != null)
        {
            return Encoding.UTF8.GetString(decoded.GetDecodedBytes());
        }

        EmailParameter? physical = mediaType.GetParameters().GetFirst(name);
        return physical == null ? null : Encoding.UTF8.GetString(physical.GetValueBytes());
    }

    static EmailSourcePosition? GetFieldPosition(MimeEntity entity, string name)
    {
        EmailHeader? header = (entity.GetHeaders() ?? throw new InvalidOperationException()).GetFirst(name);
        return header == null ? (entity.GetMediaType() ?? throw new InvalidOperationException()).GetSourcePosition() : header.GetValueSourcePosition();
    }

    static bool IsBoundaryValid(string value)
    {
        if (value.Length < 1 || value.Length > 70 || value[value.Length - 1] == ' ')
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            bool alphaNumeric = current >= '0' && current <= '9' || current >= 'A' && current <= 'Z' || current >= 'a' && current <= 'z';
            bool punctuation = current == '\'' || current == '(' || current == ')' || current == '+' || current == '_' || current == ',' || current == '-' || current == '.' || current == '/' || current == ':' || current == '=' || current == '?';
            if (!alphaNumeric && !punctuation && current != ' ')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool ContentIdMatches(EmailMessageId? identifier, string expected)
    {
        string value = expected.Trim();
        if (value.Length >= 2 && value[0] == '<' && value[value.Length - 1] == '>')
        {
            value = value.Substring(1, value.Length - 2);
        }

        string domain = (identifier ?? throw new InvalidOperationException()).IsRightDomainLiteral() ? "[" + identifier.GetIdRight() + "]" : identifier.GetIdRight();
        string actual = identifier.GetIdLeft() + "@" + domain;
        if (identifier.IsRightDomainLiteral())
        {
            return string.Equals(actual, value, StringComparison.Ordinal);
        }

        int separator = value.LastIndexOf('@');
        if (separator < 1 || separator + 1 >= value.Length)
        {
            return false;
        }

        return string.Equals(identifier.GetIdLeft(), value.Substring(0, separator), StringComparison.Ordinal)
            && string.Equals(identifier.GetIdRight(), value.Substring(separator + 1), StringComparison.OrdinalIgnoreCase);
    }

    static bool IsSemanticCode(EmailIssueCode code)
    {
        return code == EmailIssueCode.DateSemanticInvalid
            || code == EmailIssueCode.DateDayOfWeekMismatch
            || code == EmailIssueCode.AddressInternationalizationRequired
            || code == EmailIssueCode.HeaderValidationCardinality
            || code == EmailIssueCode.HeaderValidationRequiredFieldMissing
            || code == EmailIssueCode.HeaderValidationContext
            || code == EmailIssueCode.HeaderValidationResentBlock
            || code == EmailIssueCode.HeaderValidationSmtpUtf8Required
            || code == EmailIssueCode.HeaderValidationBccPolicy
            || code == EmailIssueCode.HeaderDuplicateSingleton
            || code == EmailIssueCode.MessageStructureInvalid
            || code == EmailIssueCode.PartialMessageMalformed
            || code == EmailIssueCode.ExternalBodyMalformed;
    }
}
