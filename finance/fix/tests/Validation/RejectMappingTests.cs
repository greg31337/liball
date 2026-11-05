using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class RejectMappingTests
{
    [TestCase("invalid-tag-number", 0)]
    [TestCase("missing-begin-string", 1)]
    [TestCase("missing-body-length", 1)]
    [TestCase("missing-msg-type", 1)]
    [TestCase("missing-checksum", 1)]
    [TestCase("required-header-field-missing", 1)]
    [TestCase("required-trailer-field-missing", 1)]
    [TestCase("required-field-missing", 1)]
    [TestCase("required-group-member-missing", 1)]
    [TestCase("missing-group-delimiter", 1)]
    [TestCase("conditionally-required-member-missing", 1)]
    [TestCase("field-discriminator-missing", 1)]
    [TestCase("field-not-allowed", 2)]
    [TestCase("field-in-wrong-section", 2)]
    [TestCase("group-not-allowed", 2)]
    [TestCase("group-member-not-allowed", 2)]
    [TestCase("forbidden-field-present", 2)]
    [TestCase("forbidden-component-present", 2)]
    [TestCase("forbidden-group-present", 2)]
    [TestCase("forbidden-group-member-present", 2)]
    [TestCase("user-defined-field-in-session-section", 2)]
    [TestCase("unknown-field", 3)]
    [TestCase("empty-envelope-field", 4)]
    [TestCase("empty-session-field", 4)]
    [TestCase("empty-field-value", 4)]
    [TestCase("field-value-out-of-range", 5)]
    [TestCase("invalid-code-set-value", 5)]
    [TestCase("body-length-mismatch", 5)]
    [TestCase("checksum-mismatch", 5)]
    [TestCase("begin-string-value-mismatch", 5)]
    [TestCase("msg-type-value-mismatch", 5)]
    [TestCase("begin-string-mismatch", 5)]
    [TestCase("data-length-mismatch", 5)]
    [TestCase("invalid-field-discriminator", 5)]
    [TestCase("invalid-body-length", 6)]
    [TestCase("invalid-checksum", 6)]
    [TestCase("invalid-datatype-value", 6)]
    [TestCase("invalid-timestamp-value", 6)]
    [TestCase("invalid-group-count", 6)]
    [TestCase("invalid-field-value", 6)]
    [TestCase("unsupported-encrypt-method", 7)]
    [TestCase("invalid-encrypt-method", 7)]
    [TestCase("decryption-problem", 7)]
    [TestCase("signature-problem", 8)]
    [TestCase("signature-length-mismatch", 8)]
    [TestCase("unsupported-message-type", 11)]
    [TestCase("duplicate-envelope-field", 13)]
    [TestCase("duplicate-header-field", 13)]
    [TestCase("duplicate-trailer-field", 13)]
    [TestCase("duplicate-body-field", 13)]
    [TestCase("duplicate-group-member", 13)]
    [TestCase("duplicate-group", 13)]
    [TestCase("invalid-envelope-position", 14)]
    [TestCase("header-field-order-invalid", 14)]
    [TestCase("body-field-order-invalid", 14)]
    [TestCase("trailer-field-order-invalid", 14)]
    [TestCase("length-data-not-adjacent", 14)]
    [TestCase("group-member-out-of-order", 15)]
    [TestCase("invalid-group-delimiter", 15)]
    [TestCase("group-count-mismatch", 16)]
    [TestCase("application-version-mismatch", 18)]
    [TestCase("default-application-version-mismatch", 18)]
    [TestCase("message-size-limit-exceeded", 99)]
    [TestCase("field-count-limit-exceeded", 99)]
    [TestCase("field-size-limit-exceeded", 99)]
    [TestCase("group-depth-limit-exceeded", 99)]
    [TestCase("group-entry-limit-exceeded", 99)]
    public void StableIssueCodesMapToSessionRejectReasons(string code, int expectedReason)
    {
        ValidationIssue issue = new ValidationIssue { Severity = ValidationSeverity.Error, Code = code };

        RejectMapper.MapValidationIssue(issue, new FixMessage());

        Assert.Multiple(() =>
        {
            Assert.That(issue.Code, Is.EqualTo(code));
            Assert.That(issue.HasSessionRejectReason, Is.True);
            Assert.That(issue.SessionRejectReason, Is.EqualTo(expectedReason));
        });
    }

    [TestCase("invalid-validation-limit")]
    [TestCase("dictionary-expansion-limit")]
    [TestCase("unresolved-component-reference")]
    [TestCase("unresolved-group-reference")]
    [TestCase("unsupported-field-datatype")]
    [TestCase("unresolved-code-set")]
    [TestCase("invalid-group-delimiter-definition")]
    [TestCase("scenario-inheritance-limit")]
    [TestCase("recursive-scenario-reference")]
    [TestCase("unresolved-parent-scenario")]
    [TestCase("unresolved-field-discriminator")]
    [TestCase("unsupported-discriminator-code-set")]
    public void DictionaryAndConfigurationFailuresDoNotInventRejectReasons(string code)
    {
        ValidationIssue issue = new ValidationIssue { Severity = ValidationSeverity.Error, Code = code };

        RejectMapper.MapValidationIssue(issue, new FixMessage());

        Assert.Multiple(() =>
        {
            Assert.That(issue.HasSessionRejectReason, Is.False);
            Assert.That(issue.HasBusinessRejectReason, Is.False);
        });
    }

    [TestCase("unsupported-message-type", 3)]
    [TestCase("unsupported-scenario", 4)]
    [TestCase("conditionally-required-member-missing", 5)]
    [TestCase("field-discriminator-missing", 5)]
    [TestCase("conditionally-forbidden-member-present", 0)]
    [TestCase("scenario-rule-failed", 0)]
    [TestCase("invalid-field-discriminator", 0)]
    [TestCase("unsupported-conditional-rule", 0)]
    [TestCase("unsupported-scenario-rule", 0)]
    public void ApplicationIssuesMapToBusinessRejectReasons(string code, int expectedReason)
    {
        ValidationIssue issue = new ValidationIssue { Severity = ValidationSeverity.Fatal, Code = code };

        RejectMapper.MapValidationIssue(issue, new FixMessage());

        Assert.Multiple(() =>
        {
            Assert.That(issue.Code, Is.EqualTo(code));
            Assert.That(issue.HasBusinessRejectReason, Is.True);
            Assert.That(issue.BusinessRejectReason, Is.EqualTo(expectedReason));
        });
    }

    [Test]
    public void ReferenceFieldsComeFromTheIssueAndRejectedMessage()
    {
        FixMessage message = new FixMessage
        {
            MsgType = "D",
            HeaderFields = [Field(FixTags.MsgSeqNum, "27")]
        };
        ValidationIssue explicitType = new ValidationIssue
        {
            Severity = ValidationSeverity.Error,
            Code = "invalid-code-set-value",
            HasTag = true,
            Tag = 54,
            MsgType = "G"
        };
        ValidationIssue fallbackType = new ValidationIssue
        {
            Severity = ValidationSeverity.Error,
            Code = "required-field-missing",
            HasTag = true,
            Tag = 44
        };

        RejectMapper.MapValidationIssues([explicitType, fallbackType], message);

        Assert.Multiple(() =>
        {
            Assert.That(explicitType.HasRefTagId, Is.True);
            Assert.That(explicitType.RefTagId, Is.EqualTo(54));
            Assert.That(explicitType.RefMsgType, Is.EqualTo("G"));
            Assert.That(explicitType.HasRefSeqNum, Is.True);
            Assert.That(explicitType.RefSeqNum, Is.EqualTo(27));
            Assert.That(fallbackType.RefTagId, Is.EqualTo(44));
            Assert.That(fallbackType.RefMsgType, Is.EqualTo("D"));
            Assert.That(fallbackType.RefSeqNum, Is.EqualTo(27));
        });
    }

    [Test]
    public void MissingOrMalformedMessageReferencesRemainUnavailable()
    {
        ValidationIssue issue = new ValidationIssue
        {
            Severity = ValidationSeverity.Error,
            Code = "unknown-field",
            HasTag = false,
            MsgType = ""
        };
        FixMessage missing = new FixMessage();
        FixMessage malformed = new FixMessage { MsgType = "D", HeaderFields = [Field(FixTags.MsgSeqNum, "0")] };

        RejectMapper.MapValidationIssue(issue, missing);
        Assert.Multiple(() =>
        {
            Assert.That(issue.HasRefTagId, Is.False);
            Assert.That(issue.RefMsgType, Is.Empty);
            Assert.That(issue.HasRefSeqNum, Is.False);
        });

        RejectMapper.MapValidationIssue(issue, malformed);
        Assert.Multiple(() =>
        {
            Assert.That(issue.RefMsgType, Is.EqualTo("D"));
            Assert.That(issue.HasRefSeqNum, Is.False);
        });
    }

    [Test]
    public void WarningsReceiveReferenceContextWithoutImplyingAReject()
    {
        ValidationIssue warning = new ValidationIssue
        {
            Severity = ValidationSeverity.Warning,
            Code = "conditional-rule-indeterminate",
            HasTag = true,
            Tag = 44,
            MsgType = "D"
        };

        RejectMapper.MapValidationIssue(warning, new FixMessage { HeaderFields = [Field(FixTags.MsgSeqNum, "9")] });

        Assert.Multiple(() =>
        {
            Assert.That(warning.HasRefTagId, Is.True);
            Assert.That(warning.RefTagId, Is.EqualTo(44));
            Assert.That(warning.RefMsgType, Is.EqualTo("D"));
            Assert.That(warning.HasRefSeqNum, Is.True);
            Assert.That(warning.RefSeqNum, Is.EqualTo(9));
            Assert.That(warning.HasSessionRejectReason, Is.False);
            Assert.That(warning.HasBusinessRejectReason, Is.False);
        });
    }

    [Test]
    public void ExplicitMappingsArePreservedAndRepeatedMappingIsIdempotent()
    {
        ValidationIssue issue = new ValidationIssue
        {
            Severity = ValidationSeverity.Error,
            Code = "venue-specific-rule",
            HasSessionRejectReason = true,
            SessionRejectReason = 5,
            HasBusinessRejectReason = true,
            BusinessRejectReason = 1
        };
        FixMessage message = new FixMessage { MsgType = "U1", HeaderFields = [Field(FixTags.MsgSeqNum, "3")] };

        RejectMapper.MapValidationIssue(issue, message);
        RejectMapper.MapValidationIssue(issue, message);

        Assert.Multiple(() =>
        {
            Assert.That(issue.Code, Is.EqualTo("venue-specific-rule"));
            Assert.That(issue.SessionRejectReason, Is.EqualTo(5));
            Assert.That(issue.BusinessRejectReason, Is.EqualTo(1));
            Assert.That(issue.RefMsgType, Is.EqualTo("U1"));
            Assert.That(issue.RefSeqNum, Is.EqualTo(3));
        });
    }

    private static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.ASCII.GetBytes(value) };
    }
}
