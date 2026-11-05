using System.Collections.Generic;

namespace fix;

internal static class RejectMapper
{
    internal const int NoReason = -1;
    internal const int InvalidTagNumber = 0;
    internal const int RequiredTagMissing = 1;
    internal const int TagNotDefinedForMessage = 2;
    internal const int UndefinedTag = 3;
    internal const int TagWithoutValue = 4;
    internal const int IncorrectValue = 5;
    internal const int IncorrectDataFormat = 6;
    internal const int DecryptionProblem = 7;
    internal const int SignatureProblem = 8;
    internal const int CompIdProblem = 9;
    internal const int SendingTimeAccuracyProblem = 10;
    internal const int InvalidMessageType = 11;
    internal const int DuplicateTag = 13;
    internal const int TagOutOfOrder = 14;
    internal const int GroupFieldsOutOfOrder = 15;
    internal const int IncorrectGroupCount = 16;
    internal const int UnsupportedApplicationVersion = 18;
    internal const int OtherSessionReason = 99;
    internal const int OtherBusinessReason = 0;
    internal const int UnsupportedBusinessMessageType = 3;
    internal const int ApplicationNotAvailable = 4;
    internal const int ConditionallyRequiredFieldMissing = 5;

    internal static void MapValidationIssues(List<ValidationIssue> issues, FixMessage message)
    {
        for (int index = 0; index < issues.Count; index++)
        {
            MapValidationIssue(issues[index], message);
        }
    }

    internal static void MapValidationIssue(ValidationIssue issue, FixMessage message)
    {
        MapReferenceFields(issue, message);
        if (issue.Severity != ValidationSeverity.Error && issue.Severity != ValidationSeverity.Fatal)
        {
            return;
        }
        if (!issue.HasSessionRejectReason)
        {
            int sessionReason = ResolveSessionReason(issue.Code);
            if (sessionReason != NoReason)
            {
                issue.HasSessionRejectReason = true;
                issue.SessionRejectReason = sessionReason;
            }
        }
        if (!issue.HasBusinessRejectReason)
        {
            int businessReason = ResolveBusinessReason(issue.Code);
            if (businessReason != NoReason)
            {
                issue.HasBusinessRejectReason = true;
                issue.BusinessRejectReason = businessReason;
            }
        }
    }

    internal static void MapReferenceFields(ValidationIssue issue, FixMessage message)
    {
        if (issue.HasTag && issue.Tag > 0)
        {
            issue.HasRefTagId = true;
            issue.RefTagId = issue.Tag;
        }
        if (issue.MsgType.Length != 0)
        {
            issue.RefMsgType = issue.MsgType;
        }
        else if (message.MsgType.Length != 0)
        {
            issue.RefMsgType = message.MsgType;
        }
        FixField? sequenceField = EnvelopeValidator.FindFirstMessageField(message, FixTags.MsgSeqNum);
        if (sequenceField == null)
        {
            return;
        }
        ScalarParseResult sequence = ScalarParser.ParseScalar(sequenceField.Value, FixFieldType.SeqNum, FixDefaults.MaxFieldSize);
        if (sequence.Success)
        {
            issue.HasRefSeqNum = true;
            issue.RefSeqNum = sequence.IntegerValue;
        }
    }

    internal static int ResolveSessionReason(string code)
    {
        if (code == "invalid-tag-number") return InvalidTagNumber;
        if (IsMissingRequiredCode(code)) return RequiredTagMissing;
        if (IsTagNotDefinedCode(code)) return TagNotDefinedForMessage;
        if (code == "unknown-field") return UndefinedTag;
        if (IsEmptyValueCode(code)) return TagWithoutValue;
        if (IsIncorrectValueCode(code)) return IncorrectValue;
        if (IsIncorrectFormatCode(code)) return IncorrectDataFormat;
        if (code == "decryption-problem" || code == "unsupported-encrypt-method" || code == "invalid-encrypt-method") return DecryptionProblem;
        if (code == "signature-problem" || code == "signature-length-mismatch") return SignatureProblem;
        if (IsCompIdCode(code)) return CompIdProblem;
        if (code == "sending-time-latency-exceeded" || code == "orig-sending-time-after-sending-time") return SendingTimeAccuracyProblem;
        if (code == "unsupported-message-type" || code == "unexpected-logon-message-type" || code == "unexpected-logout-message-type") return InvalidMessageType;
        if (IsDuplicateCode(code) || code == "duplicate-logon-field" || code == "duplicate-logout-text") return DuplicateTag;
        if (IsTagOrderCode(code)) return TagOutOfOrder;
        if (IsGroupOrderCode(code)) return GroupFieldsOutOfOrder;
        if (code == "group-count-mismatch") return IncorrectGroupCount;
        if (code == "application-version-mismatch" || code == "default-application-version-mismatch" || code == "unsupported-default-application-version") return UnsupportedApplicationVersion;
        if (code == "message-size-limit-exceeded" || code == "field-count-limit-exceeded" || code == "field-size-limit-exceeded" || code == "group-depth-limit-exceeded" || code == "group-entry-limit-exceeded") return OtherSessionReason;
        return NoReason;
    }

    internal static int ResolveBusinessReason(string code)
    {
        if (code == "unsupported-message-type") return UnsupportedBusinessMessageType;
        if (code == "unsupported-scenario") return ApplicationNotAvailable;
        if (code == "conditionally-required-member-missing" || code == "field-discriminator-missing") return ConditionallyRequiredFieldMissing;
        if (code == "conditionally-forbidden-member-present" || code == "scenario-rule-failed" || code == "invalid-field-discriminator" || code == "unsupported-conditional-rule" || code == "unsupported-scenario-rule") return OtherBusinessReason;
        return NoReason;
    }

    internal static bool IsMissingRequiredCode(string code)
    {
        return code == "missing-begin-string" || code == "missing-body-length" || code == "missing-msg-type" || code == "missing-checksum" || code == "missing-sending-time" || code == "missing-orig-sending-time" || code == "missing-encrypt-method" || code == "missing-heart-bt-int" || code == "missing-default-application-version" || code == "required-header-field-missing" || code == "required-trailer-field-missing" || code == "required-field-missing" || code == "required-group-member-missing" || code == "missing-group-delimiter" || code == "conditionally-required-member-missing" || code == "field-discriminator-missing";
    }

    internal static bool IsTagNotDefinedCode(string code)
    {
        return code == "field-not-allowed" || code == "field-in-wrong-section" || code == "group-not-allowed" || code == "group-member-not-allowed" || code == "forbidden-field-present" || code == "forbidden-component-present" || code == "forbidden-group-present" || code == "forbidden-group-member-present" || code == "user-defined-field-in-session-section";
    }

    internal static bool IsEmptyValueCode(string code)
    {
        return code == "empty-envelope-field" || code == "empty-session-field" || code == "empty-field-value";
    }

    internal static bool IsIncorrectValueCode(string code)
    {
        return code == "field-value-out-of-range" || code == "invalid-code-set-value" || code == "body-length-mismatch" || code == "checksum-mismatch" || code == "begin-string-value-mismatch" || code == "msg-type-value-mismatch" || code == "begin-string-mismatch" || code == "data-length-mismatch" || code == "invalid-field-discriminator" || code == "invalid-poss-dup-flag" || code == "orig-sending-time-without-poss-dup" || code == "heart-bt-int-mismatch" || code == "invalid-reset-seq-num-flag" || code == "reset-logon-sequence-number-not-one" || code == "raw-data-length-mismatch" || code == "raw-data-length-pair-missing";
    }

    internal static bool IsIncorrectFormatCode(string code)
    {
        return code == "invalid-body-length" || code == "invalid-checksum" || code == "invalid-datatype-value" || code == "invalid-timestamp-value" || code == "invalid-group-count" || code == "invalid-field-value" || code == "invalid-sending-time" || code == "invalid-orig-sending-time" || code == "invalid-heart-bt-int" || code == "invalid-next-expected-msg-seq-num" || code == "invalid-default-application-version";
    }

    internal static bool IsCompIdCode(string code)
    {
        return code == "sender-comp-id-mismatch" || code == "sender-sub-id-mismatch" || code == "sender-location-id-mismatch" || code == "target-comp-id-mismatch" || code == "target-sub-id-mismatch" || code == "target-location-id-mismatch";
    }

    internal static bool IsDuplicateCode(string code)
    {
        return code == "duplicate-envelope-field" || code == "duplicate-header-field" || code == "duplicate-trailer-field" || code == "duplicate-body-field" || code == "duplicate-group" || code == "duplicate-group-member";
    }

    internal static bool IsTagOrderCode(string code)
    {
        return code == "invalid-envelope-position" || code == "header-field-order-invalid" || code == "body-field-order-invalid" || code == "trailer-field-order-invalid" || code == "length-data-not-adjacent";
    }

    internal static bool IsGroupOrderCode(string code)
    {
        return code == "group-member-out-of-order" || code == "invalid-group-delimiter";
    }
}
