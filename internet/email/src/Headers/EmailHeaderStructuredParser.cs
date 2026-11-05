using System;
using System.Collections.Generic;

namespace email;

internal class EmailHeaderStructuredResult
{
    object? value;
    EmailIssueList issues;

    internal EmailHeaderStructuredResult(object? value, EmailIssueList issues)
    {
        this.value = value;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    internal object? GetValue()
    {
        return value;
    }

    internal EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }
}
internal class EmailHeaderStructuredParser
{
    internal static EmailHeaderStructuredResult Parse(EmailHeaderValueKind kind, byte[]? input, EmailStructuredValueParseMode mode, EmailSourcePosition? position, string fieldName, bool allowBrokenRfc2231Parameters)
    {
        if (kind == EmailHeaderValueKind.Mailbox || kind == EmailHeaderValueKind.AddressGroup || kind == EmailHeaderValueKind.AddressList)
        {
            return ParseAddress(kind, input, mode, position);
        }

        if (kind == EmailHeaderValueKind.Date)
        {
            EmailDateParserOptions options = new EmailDateParserOptions();
            options.SetMode(ToDateMode(mode));
            EmailDateParseResult result = new EmailValueParser().ParseDateWithInputAndOptionsAndInitialPosition(input, options, position);
            return new EmailHeaderStructuredResult(result.GetValue(), result.GetIssues());
        }

        if (kind == EmailHeaderValueKind.MessageId || kind == EmailHeaderValueKind.ContentId || kind == EmailHeaderValueKind.References || kind == EmailHeaderValueKind.InReplyTo)
        {
            return ParseIdentifier(kind, input, mode, position);
        }

        if (kind == EmailHeaderValueKind.Parameters || kind == EmailHeaderValueKind.MediaType || kind == EmailHeaderValueKind.Disposition || kind == EmailHeaderValueKind.ContentLanguage)
        {
            return ParseMime(kind, input, mode, position, allowBrokenRfc2231Parameters);
        }

        if (kind == EmailHeaderValueKind.Received)
        {
            EmailReceivedParserOptions options = new EmailReceivedParserOptions();
            options.SetMode(ToReceivedMode(mode));
            EmailReceivedParseResult result = new EmailValueParser().ParseReceivedWithInputAndOptionsAndInitialPosition(input, options, position);
            return new EmailHeaderStructuredResult(result.GetValue(), result.GetIssues());
        }

        if (kind == EmailHeaderValueKind.AuthenticationResults || kind == EmailHeaderValueKind.ReceivedSpf)
        {
            return ParseAuthentication(kind, input, mode, position);
        }

        if (kind == EmailHeaderValueKind.DkimSignature || kind == EmailHeaderValueKind.ArcMessageSignature || kind == EmailHeaderValueKind.ArcSeal || kind == EmailHeaderValueKind.ArcAuthenticationResults)
        {
            return ParseSignature(kind, input, mode, position);
        }

        if (kind >= EmailHeaderValueKind.ReturnPath && kind <= EmailHeaderValueKind.Trace)
        {
            EmailStructuredValueParserOptions options = new EmailStructuredValueParserOptions();
            options.SetMode(mode);
            EmailStandardHeaderParseResult result = new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndOptionsAndInitialPosition(fieldName, input, options, position);
            return new EmailHeaderStructuredResult(result.GetValue(), result.GetIssues());
        }

        return ParseMetadata(kind, input, mode, position, fieldName);
    }

    static EmailHeaderStructuredResult ParseAddress(EmailHeaderValueKind kind, byte[]? input, EmailStructuredValueParseMode mode, EmailSourcePosition? position)
    {
        EmailAddressParserOptions options = new EmailAddressParserOptions();
        options.SetMode(ToAddressMode(mode));
        EmailAddressParseResult? result = new EmailValueParser().ParseAddressListWithInputAndOptionsAndInitialPosition(input, options, position);
        object? value = result.GetAddressList();
        EmailIssueList issues = result.GetIssues();
        if (kind == EmailHeaderValueKind.Mailbox)
        {
            value = result.GetMailbox();
            if (value == null)
            {
                AddKindIssue(issues, mode, "The header value is not exactly one mailbox.", position);
            }
        }
        else if (kind == EmailHeaderValueKind.AddressGroup)
        {
            value = result.GetGroup();
            if (value == null)
            {
                AddKindIssue(issues, mode, "The header value is not exactly one address group.", position);
            }
        }

        return new EmailHeaderStructuredResult(value, issues);
    }

    static EmailHeaderStructuredResult ParseIdentifier(EmailHeaderValueKind kind, byte[]? input, EmailStructuredValueParseMode mode, EmailSourcePosition? position)
    {
        EmailMessageIdParserOptions options = new EmailMessageIdParserOptions();
        options.SetMode(ToIdentifierMode(mode));
        EmailMessageIdParseResult result;
        if (kind == EmailHeaderValueKind.MessageId)
        {
            result = new EmailValueParser().ParseMessageIdWithInputAndOptionsAndInitialPosition(input, options, position);
        }
        else if (kind == EmailHeaderValueKind.ContentId)
        {
            result = new EmailValueParser().ParseContentIdWithInputAndOptionsAndInitialPosition(input, options, position);
        }
        else if (kind == EmailHeaderValueKind.References)
        {
            result = new EmailValueParser().ParseReferencesWithInputAndOptionsAndInitialPosition(input, options, position);
        }
        else
        {
            result = new EmailValueParser().ParseInReplyToWithInputAndOptionsAndInitialPosition(input, options, position);
        }

        object? value = kind == EmailHeaderValueKind.MessageId || kind == EmailHeaderValueKind.ContentId ? result.GetIdentifier() : result.GetIdentifiers();
        return new EmailHeaderStructuredResult(value, result.GetIssues());
    }

    static EmailHeaderStructuredResult ParseMime(EmailHeaderValueKind kind, byte[]? input, EmailStructuredValueParseMode mode, EmailSourcePosition? position, bool allowBrokenRfc2231Parameters)
    {
        EmailStructuredValueParserOptions options = new EmailStructuredValueParserOptions();
        options.SetMode(mode);
        if (kind == EmailHeaderValueKind.Parameters)
        {
            EmailParameterParseResult result = new EmailValueParser().ParseParametersWithInputAndOptionsAndInitialPosition(input, options, position);
            return ApplyRfc2231Policy(kind, result.GetValue(), result.GetIssues(), allowBrokenRfc2231Parameters, position);
        }

        if (kind == EmailHeaderValueKind.MediaType)
        {
            EmailMediaTypeParseResult result = new EmailValueParser().ParseMediaTypeWithInputAndOptionsAndInitialPosition(input, options, position);
            return ApplyRfc2231Policy(kind, result.GetValue(), result.GetIssues(), allowBrokenRfc2231Parameters, position);
        }

        if (kind == EmailHeaderValueKind.Disposition)
        {
            EmailDispositionParseResult result = new EmailValueParser().ParseDispositionWithInputAndOptionsAndInitialPosition(input, options, position);
            return ApplyRfc2231Policy(kind, result.GetValue(), result.GetIssues(), allowBrokenRfc2231Parameters, position);
        }

        EmailContentLanguageParseResult language = new EmailValueParser().ParseContentLanguageWithInputAndOptionsAndInitialPosition(input, options, position);
        return new EmailHeaderStructuredResult(language.GetValue(), language.GetIssues());
    }

    static EmailHeaderStructuredResult ApplyRfc2231Policy(EmailHeaderValueKind kind, object? value, EmailIssueList issues, bool allowBrokenRfc2231Parameters, EmailSourcePosition? position)
    {
        if (value == null || !HasRfc2231Defect(issues))
        {
            return new EmailHeaderStructuredResult(value, issues);
        }

        if (allowBrokenRfc2231Parameters)
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.Rfc2231CompatibilityRecovery, "A broken RFC 2231 parameter was decoded by an explicitly enabled compatibility rule.", position, null, null, "The decoded parameter value contains the deterministic usable prefix recovered from the malformed parameter set."));
            return new EmailHeaderStructuredResult(value, issues);
        }

        if (kind == EmailHeaderValueKind.Parameters)
        {
            EmailParameterList parameters = (EmailParameterList)value;
            return new EmailHeaderStructuredResult(WithoutDecodedParameters(parameters), issues);
        }

        if (kind == EmailHeaderValueKind.MediaType)
        {
            EmailMediaType mediaType = (EmailMediaType)value;
            EmailMediaType filtered = new EmailMediaType(mediaType.GetTopLevelType(), mediaType.GetSubtype(), WithoutDecodedParameters(mediaType.GetParameters()), mediaType.GetRawBytes(), mediaType.GetSourcePosition(), mediaType.WasDefaulted());
            return new EmailHeaderStructuredResult(filtered, issues);
        }

        EmailDisposition disposition = (EmailDisposition)value;
        EmailDisposition filteredDisposition = new EmailDisposition(disposition.GetDispositionType(), WithoutDecodedParameters(disposition.GetParameters()), disposition.GetRawBytes(), disposition.GetSourcePosition());
        return new EmailHeaderStructuredResult(filteredDisposition, issues);
    }

    static EmailParameterList WithoutDecodedParameters(EmailParameterList parameters)
    {
        return new EmailParameterList(parameters.GetAll(), new List<EmailRfc2231DecodedParameter>());
    }

    static bool HasRfc2231Defect(EmailIssueList issues)
    {
        return issues.ContainsCode(EmailIssueCode.Rfc2231MalformedName) || issues.ContainsCode(EmailIssueCode.Rfc2231MalformedExtendedValue) || issues.ContainsCode(EmailIssueCode.Rfc2231ContinuationGap) || issues.ContainsCode(EmailIssueCode.Rfc2231Duplicate) || issues.ContainsCode(EmailIssueCode.Rfc2231ConflictingForms) || issues.ContainsCode(EmailIssueCode.Rfc2231SegmentLimitExceeded) || issues.ContainsCode(EmailIssueCode.Rfc2231DecodedByteLimitExceeded);
    }

    static EmailHeaderStructuredResult ParseAuthentication(EmailHeaderValueKind kind, byte[]? input, EmailStructuredValueParseMode mode, EmailSourcePosition? position)
    {
        EmailAuthenticationParserOptions options = new EmailAuthenticationParserOptions();
        options.SetMode(mode);
        if (kind == EmailHeaderValueKind.AuthenticationResults)
        {
            EmailAuthenticationResultsParseResult result = new EmailValueParser().ParseAuthenticationResultsWithInputAndOptionsAndInitialPosition(input, options, position);
            return new EmailHeaderStructuredResult(result.GetValue(), result.GetIssues());
        }

        EmailReceivedSpfParseResult spf = new EmailValueParser().ParseReceivedSpfWithInputAndOptionsAndInitialPosition(input, options, position);
        return new EmailHeaderStructuredResult(spf.GetValue(), spf.GetIssues());
    }

    static EmailHeaderStructuredResult ParseSignature(EmailHeaderValueKind kind, byte[]? input, EmailStructuredValueParseMode mode, EmailSourcePosition? position)
    {
        EmailSignatureParserOptions options = new EmailSignatureParserOptions();
        options.SetMode(mode);
        if (kind == EmailHeaderValueKind.DkimSignature)
        {
            EmailDkimSignatureParseResult result = new EmailValueParser().ParseDkimSignatureWithInputAndOptionsAndInitialPosition(input, options, position);
            return new EmailHeaderStructuredResult(result.GetValue(), result.GetIssues());
        }

        if (kind == EmailHeaderValueKind.ArcMessageSignature)
        {
            EmailArcMessageSignatureParseResult result = new EmailValueParser().ParseArcMessageSignatureWithInputAndOptionsAndInitialPosition(input, options, position);
            return new EmailHeaderStructuredResult(result.GetValue(), result.GetIssues());
        }

        if (kind == EmailHeaderValueKind.ArcSeal)
        {
            EmailArcSealParseResult result = new EmailValueParser().ParseArcSealWithInputAndOptionsAndInitialPosition(input, options, position);
            return new EmailHeaderStructuredResult(result.GetValue(), result.GetIssues());
        }

        EmailArcAuthenticationResultsParseResult authentication = new EmailValueParser().ParseArcAuthenticationResultsWithInputAndOptionsAndInitialPosition(input, options, position);
        return new EmailHeaderStructuredResult(authentication.GetValue(), authentication.GetIssues());
    }

    static EmailHeaderStructuredResult ParseMetadata(EmailHeaderValueKind kind, byte[]? input, EmailStructuredValueParseMode mode, EmailSourcePosition? position, string fieldName)
    {
        EmailMetadataParserOptions options = new EmailMetadataParserOptions();
        options.SetMode(mode);
        EmailMetadataParseResult result;
        if (kind == EmailHeaderValueKind.Keywords)
        {
            result = new EmailMetadataParser().ParseKeywordsWithInputAndOptionsAndInitialPosition(input, options, position);
        }
        else if (kind == EmailHeaderValueKind.Priority)
        {
            result = new EmailMetadataParser().ParsePriorityWithInputAndOptionsAndInitialPosition(input, options, position);
        }
        else if (kind == EmailHeaderValueKind.XPriority)
        {
            result = new EmailMetadataParser().ParseXPriorityWithInputAndOptionsAndInitialPosition(input, options, position);
        }
        else if (kind == EmailHeaderValueKind.Importance)
        {
            result = new EmailMetadataParser().ParseImportanceWithInputAndOptionsAndInitialPosition(input, options, position);
        }
        else if (kind == EmailHeaderValueKind.Sensitivity)
        {
            result = new EmailMetadataParser().ParseSensitivityWithInputAndOptionsAndInitialPosition(input, options, position);
        }
        else if (kind == EmailHeaderValueKind.AutoSubmitted)
        {
            result = new EmailMetadataParser().ParseAutoSubmittedWithInputAndOptionsAndInitialPosition(input, options, position);
        }
        else if (kind == EmailHeaderValueKind.ListUnsubscribePost)
        {
            result = new EmailMetadataParser().ParseListUnsubscribePostWithInputAndOptionsAndInitialPosition(input, options, position);
        }
        else if (kind == EmailHeaderValueKind.ResentField)
        {
            result = new EmailMetadataParser().ParseResentFieldWithFieldNameAndInputAndOptionsAndInitialPosition(fieldName, input, options, position);
        }
        else
        {
            EmailListManagementKind listKind = GetListKind(kind);
            result = new EmailMetadataParser().ParseListManagementWithInputAndKindAndOptionsAndInitialPosition(input, listKind, options, position);
        }

        return new EmailHeaderStructuredResult(GetMetadataValue(result), result.GetIssues());
    }

    static object? GetMetadataValue(EmailMetadataParseResult result)
    {
        EmailMetadataValueKind kind = result.GetKind();
        if (kind == EmailMetadataValueKind.Keywords)
        {
            return result.GetKeywords();
        }

        if (kind == EmailMetadataValueKind.Priority)
        {
            return result.GetPriority();
        }

        if (kind == EmailMetadataValueKind.Importance)
        {
            return result.GetImportance();
        }

        if (kind == EmailMetadataValueKind.Sensitivity)
        {
            return result.GetSensitivity();
        }

        if (kind == EmailMetadataValueKind.AutoSubmitted)
        {
            return result.GetAutoSubmitted();
        }

        if (kind == EmailMetadataValueKind.ListManagement)
        {
            return result.GetListManagement();
        }

        if (kind == EmailMetadataValueKind.ListUnsubscribePost)
        {
            return result.GetListUnsubscribePost();
        }

        return result.GetResentField();
    }

    static EmailListManagementKind GetListKind(EmailHeaderValueKind kind)
    {
        if (kind == EmailHeaderValueKind.ListUnsubscribe)
        {
            return EmailListManagementKind.Unsubscribe;
        }

        if (kind == EmailHeaderValueKind.ListSubscribe)
        {
            return EmailListManagementKind.Subscribe;
        }

        if (kind == EmailHeaderValueKind.ListPost)
        {
            return EmailListManagementKind.Post;
        }

        if (kind == EmailHeaderValueKind.ListOwner)
        {
            return EmailListManagementKind.Owner;
        }

        if (kind == EmailHeaderValueKind.ListArchive)
        {
            return EmailListManagementKind.Archive;
        }

        return EmailListManagementKind.Help;
    }

    static void AddKindIssue(EmailIssueList issues, EmailStructuredValueParseMode mode, string description, EmailSourcePosition? position)
    {
        EmailIssueSeverity severity = mode == EmailStructuredValueParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.HeaderMalformed, description, position, null, null, "The parsed address list remains available through the raw header value."));
    }

    static EmailAddressParseMode ToAddressMode(EmailStructuredValueParseMode mode)
    {
        return mode == EmailStructuredValueParseMode.Strict ? EmailAddressParseMode.Strict : EmailAddressParseMode.Tolerant;
    }

    static EmailDateParseMode ToDateMode(EmailStructuredValueParseMode mode)
    {
        return mode == EmailStructuredValueParseMode.Strict ? EmailDateParseMode.Strict : EmailDateParseMode.Tolerant;
    }

    static EmailMessageIdParseMode ToIdentifierMode(EmailStructuredValueParseMode mode)
    {
        return mode == EmailStructuredValueParseMode.Strict ? EmailMessageIdParseMode.Strict : EmailMessageIdParseMode.Tolerant;
    }

    static EmailReceivedParseMode ToReceivedMode(EmailStructuredValueParseMode mode)
    {
        return mode == EmailStructuredValueParseMode.Strict ? EmailReceivedParseMode.Strict : EmailReceivedParseMode.Tolerant;
    }
}
