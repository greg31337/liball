using System;
using System.Text;

namespace email;
internal class EmailMimeMetadataCandidate
{
    EmailMediaType mediaType;
    EmailDisposition? disposition;
    string? transferEncodingName;
    EmailMessageId? contentId;
    string? contentLocation;
    EmailContentLanguage? contentLanguage;
    string? contentDescription;
    MimeEntityKind kind;
    EmailIssueList issues;
    bool conflicts;

    internal EmailMimeMetadataCandidate(EmailMediaType mediaType, EmailDisposition? disposition, string? transferEncodingName, EmailMessageId? contentId, string? contentLocation, EmailContentLanguage? contentLanguage, string? contentDescription, MimeEntityKind kind, EmailIssueList issues, bool conflicts)
    {
        this.mediaType = mediaType;
        this.disposition = disposition;
        this.transferEncodingName = transferEncodingName;
        this.contentId = contentId;
        this.contentLocation = contentLocation;
        this.contentLanguage = contentLanguage;
        this.contentDescription = contentDescription;
        this.kind = kind;
        this.issues = issues;
        this.conflicts = conflicts;
    }

    internal EmailMediaType GetMediaType() { return mediaType; }
    internal EmailDisposition? GetDisposition() { return disposition; }
    internal string? GetTransferEncodingName() { return transferEncodingName; }
    internal EmailMessageId? GetContentId() { return contentId; }
    internal string? GetContentLocation() { return contentLocation; }
    internal EmailContentLanguage? GetContentLanguage() { return contentLanguage; }
    internal string? GetContentDescription() { return contentDescription; }
    internal MimeEntityKind GetKind() { return kind; }
    internal EmailIssueList GetIssues() { return issues; }
    internal bool HasConflicts() { return conflicts; }
}

internal class EmailMimeMetadataRefresher
{
    EmailHeaders headers;
    EmailMediaType? parentMediaType;
    EmailParseOptions options;
    EmailStructuredValueParseMode mode;
    EmailIssueList issues;
    bool conflicts;

    internal EmailMimeMetadataRefresher(EmailHeaders headers, EmailMediaType? parentMediaType, EmailParseOptions options)
    {
        this.headers = headers;
        this.parentMediaType = parentMediaType;
        this.options = options;
        mode = options.GetMode() == EmailParseMode.Strict ? EmailStructuredValueParseMode.Strict : EmailStructuredValueParseMode.Tolerant;
        issues = new EmailIssueList();
        conflicts = false;
    }

    internal EmailMimeMetadataCandidate Parse()
    {
        string[] singletonNames = new string[] { "Content-Type", "Content-Disposition", "Content-Transfer-Encoding", "Content-ID", "Content-Location", "Content-Language", "Content-Description" };
        int singletonIndex = 0;
        while (singletonIndex < singletonNames.Length)
        {
            AddDuplicateConflict(singletonNames[singletonIndex]);
            singletonIndex++;
        }

        EmailMediaType mediaType = ParseMediaType();
        EmailDisposition? disposition = ParseDisposition();
        string? transferEncoding = ParseTransferEncoding();
        EmailMessageId? contentId = ParseContentId();
        string? location = ParseLocation();
        EmailContentLanguage? language = ParseLanguage();
        string? description = ParseDescription();
        MimeEntityKind kind = GetKind(mediaType);
        return new EmailMimeMetadataCandidate(mediaType, disposition, transferEncoding, contentId, location, language, description, kind, issues, conflicts);
    }

    EmailMediaType ParseMediaType()
    {
        EmailHeader? header = headers.GetFirst("Content-Type");
        if (header == null) { return new EmailMimeDefaults().CreateDefaultMediaTypeWithParentMediaType(parentMediaType); }
        EmailStructuredValueParserOptions parserOptions = CreateStructuredOptions();
        EmailMediaTypeParseResult result = new EmailMediaTypeParser().ParseWithInputAndOptionsAndInitialPosition(header.GetUnfoldedValue(), parserOptions, header.GetValueSourcePosition());
        issues.AddRange(result.GetIssues());
        return result.GetValue();
    }

    EmailDisposition? ParseDisposition()
    {
        EmailHeader? header = headers.GetFirst("Content-Disposition");
        if (header == null) { return null; }
        EmailDispositionParseResult result = new EmailDispositionParser().ParseWithInputAndOptionsAndInitialPosition(header.GetUnfoldedValue(), CreateStructuredOptions(), header.GetValueSourcePosition());
        issues.AddRange(result.GetIssues());
        return result.GetValue();
    }

    string? ParseTransferEncoding()
    {
        EmailHeader? header = headers.GetFirst("Content-Transfer-Encoding");
        if (header == null) { return null; }
        EmailStandardHeaderParseResult result = new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndOptionsAndInitialPosition("Content-Transfer-Encoding", header.GetUnfoldedValue(), CreateStructuredOptions(), header.GetValueSourcePosition());
        issues.AddRange(result.GetIssues());
        EmailStandardHeaderValue? value = result.GetValue();
        if (value == null || value.IsMalformed()) { return null; }
        string text = Encoding.ASCII.GetString(header.GetUnfoldedValue()).Trim();
        return text.Length == 0 ? null : text;
    }

    EmailMessageId? ParseContentId()
    {
        EmailHeader? header = headers.GetFirst("Content-ID");
        if (header == null) { return null; }
        EmailMessageIdParserOptions parserOptions = new EmailMessageIdParserOptions();
        parserOptions.SetMode(mode == EmailStructuredValueParseMode.Strict ? EmailMessageIdParseMode.Strict : EmailMessageIdParseMode.Tolerant);
        parserOptions.SetMaximumIdentifiers(1);
        parserOptions.SetMaximumInputBytes(options.GetLimits().GetParsingLimits().GetMaxLineBytes());
        EmailMessageIdParseResult result = new EmailMessageIdParser().ParseContentIdWithInputAndOptionsAndInitialPosition(header.GetUnfoldedValue(), parserOptions, header.GetValueSourcePosition());
        issues.AddRange(result.GetIssues());
        return result.GetIdentifier();
    }

    string? ParseLocation()
    {
        EmailHeader? header = headers.GetFirst("Content-Location");
        if (header == null) { return null; }
        EmailStandardHeaderParseResult result = new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndOptionsAndInitialPosition("Content-Location", header.GetUnfoldedValue(), CreateStructuredOptions(), header.GetValueSourcePosition());
        issues.AddRange(result.GetIssues());
        EmailStandardHeaderValue? value = result.GetValue();
        return value == null ? null : value.GetPrimaryValue();
    }

    EmailContentLanguage? ParseLanguage()
    {
        EmailHeader? header = headers.GetFirst("Content-Language");
        if (header == null) { return null; }
        EmailContentLanguageParseResult result = new EmailContentLanguageParser().ParseWithInputAndOptionsAndInitialPosition(header.GetUnfoldedValue(), CreateStructuredOptions(), header.GetValueSourcePosition());
        issues.AddRange(result.GetIssues());
        return result.GetValue();
    }

    string? ParseDescription()
    {
        EmailHeader? header = headers.GetFirst("Content-Description");
        if (header == null) { return null; }
        EmailHeaderDecodeResult result = EmailHeaderDecoder.Decode(header.GetUnfoldedValue(), EmailHeaderValueKind.Unstructured, mode, header.GetValueSourcePosition(), mode != EmailStructuredValueParseMode.Strict);
        issues.AddRange(result.GetIssues());
        string value = result.GetValue();
        return value.Length == 0 ? null : value;
    }

    EmailStructuredValueParserOptions CreateStructuredOptions()
    {
        EmailParsingLimits limits = options.GetLimits().GetParsingLimits();
        EmailStructuredValueParserOptions parserOptions = new EmailStructuredValueParserOptions();
        parserOptions.SetMode(mode);
        parserOptions.SetMaximumInputBytes(limits.GetMaxLineBytes());
        parserOptions.SetMaximumParameters(limits.GetMaxParametersPerHeader());
        parserOptions.SetMaximumDecodedParameterBytes(options.GetLimits().GetDecodingLimits().GetMaxDecodedBytes());
        return parserOptions;
    }

    void AddDuplicateConflict(string name)
    {
        int count = headers.GetCountNamed(name);
        if (count <= 1) { return; }
        conflicts = true;
        EmailHeader? first = headers.GetFirst(name);
        EmailIssueSeverity severity = mode == EmailStructuredValueParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.MimeMetadataConflict, "The MIME metadata field occurs more than once.", first == null ? null : first.GetValueSourcePosition(), null, name, "The first field was used and every raw field was retained."));
    }

    static MimeEntityKind GetKind(EmailMediaType mediaType)
    {
        if (string.Equals(mediaType.GetTopLevelType(), "multipart", StringComparison.OrdinalIgnoreCase))
        {
            if (mediaType.IsType("multipart", "signed")) { return MimeEntityKind.Signed; }
            if (mediaType.IsType("multipart", "encrypted")) { return MimeEntityKind.Encrypted; }
            return MimeEntityKind.Multipart;
        }
        if (mediaType.IsType("message", "rfc822") || mediaType.IsType("message", "global")) { return MimeEntityKind.Message; }
        if (mediaType.IsType("message", "partial")) { return MimeEntityKind.PartialMessage; }
        if (mediaType.IsType("message", "external-body")) { return MimeEntityKind.ExternalBody; }
        if (mediaType.IsType("message", "delivery-status") || mediaType.IsType("message", "global-delivery-status")) { return MimeEntityKind.DeliveryStatus; }
        if (mediaType.IsType("message", "disposition-notification") || mediaType.IsType("message", "global-disposition-notification")) { return MimeEntityKind.DispositionNotification; }
        if (mediaType.IsType("message", "feedback-report")) { return MimeEntityKind.FeedbackReport; }
        if (mediaType.IsType("text", "calendar")) { return MimeEntityKind.Calendar; }
        if (mediaType.IsType("application", "ms-tnef") || mediaType.IsType("application", "vnd.ms-tnef") || mediaType.IsType("application", "x-tnef")) { return MimeEntityKind.Tnef; }
        return MimeEntityKind.Leaf;
    }
}
