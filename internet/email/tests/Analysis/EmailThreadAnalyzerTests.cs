using System.Text;
using email;

namespace email.Tests;

public class EmailThreadAnalyzerTests
{
    [Test]
    public void ExtractsCanonicalIdentifiersReferenceRootAndImmediateParent()
    {
        EmailMessage message = ParseMessage(
            "Message-ID: <Child@EXAMPLE.TEST>\r\n"
            + "References: <root@Example.Test> <parent@example.test> <parent@EXAMPLE.TEST>\r\n"
            + "In-Reply-To: <other@example.test>\r\n"
            + "Subject: Re: Quarterly Report\r\n\r\nbody");

        EmailThreadIdentity identity = new EmailThreadAnalyzer().ExtractWithMessage(message);

        Assert.That(identity.GetMessageIdentifierCount(), Is.EqualTo(1));
        Assert.That(identity.GetCanonicalMessageIdentifier(), Is.EqualTo("<Child@example.test>"));
        Assert.That(identity.GetReferenceCount(), Is.EqualTo(2));
        Assert.That(new EmailThreadAnalyzer().CanonicalizeIdentifier(identity.GetReferenceAt(0)), Is.EqualTo("<root@example.test>"));
        Assert.That(identity.GetInReplyToCount(), Is.EqualTo(1));
        Assert.That(identity.HasParentCandidate(), Is.True);
        Assert.That(new EmailThreadAnalyzer().CanonicalizeIdentifier(identity.GetParentCandidate()), Is.EqualTo("<parent@example.test>"));
        Assert.That(identity.GetThreadIdentitySource(), Is.EqualTo(EmailThreadIdentitySource.References));
        Assert.That(identity.GetThreadKey(), Is.EqualTo("id:<root@example.test>"));
        Assert.That(identity.GetNormalizedSubject(), Is.EqualTo("quarterly report"));
        Assert.That(identity.UsesSubjectFallback(), Is.False);
    }

    [Test]
    public void SingleInReplyToSuppliesIdentityAndAmbiguousValuesDoNotGuessAParent()
    {
        EmailThreadIdentity single = new EmailThreadAnalyzer().ExtractWithMessage(ParseMessage(
            "In-Reply-To: <parent@example.test>\r\nSubject: Re: Topic\r\n\r\nbody"));
        Assert.That(single.GetThreadIdentitySource(), Is.EqualTo(EmailThreadIdentitySource.InReplyTo));
        Assert.That(single.GetThreadKey(), Is.EqualTo("id:<parent@example.test>"));
        Assert.That(single.HasParentCandidate(), Is.True);

        EmailThreadIdentity ambiguous = new EmailThreadAnalyzer().ExtractWithMessage(ParseMessage(
            "Message-ID: <child@example.test>\r\n"
            + "In-Reply-To: <one@example.test> <two@example.test>\r\n"
            + "Subject: Re: Topic\r\n\r\nbody"));
        Assert.That(ambiguous.GetInReplyToCount(), Is.EqualTo(2));
        Assert.That(ambiguous.HasParentCandidate(), Is.False);
        Assert.That(ambiguous.GetThreadIdentitySource(), Is.EqualTo(EmailThreadIdentitySource.MessageId));
        Assert.That(ambiguous.GetThreadKey(), Is.EqualTo("id:<child@example.test>"));
    }

    [Test]
    public void SubjectFallbackNormalizationIsExplicitConservativeAndCultureInvariant()
    {
        Assert.That(new EmailThreadAnalyzer().NormalizeSubjectWithValue("  Re[12]: FW: [FWD:  Quarterly   Report ] (fwd) "), Is.EqualTo("quarterly report"));
        Assert.That(new EmailThreadAnalyzer().NormalizeSubjectWithValue("ＡＷ (2):  Status\t Update"), Is.EqualTo("status update"));
        Assert.That(new EmailThreadAnalyzer().NormalizeSubjectWithValue("SV: Topic"), Is.EqualTo("topic"));
        Assert.That(new EmailThreadAnalyzer().NormalizeSubjectWithValue("[Project] Re: Topic"), Is.EqualTo("[project] re: topic"));
        Assert.That(new EmailThreadAnalyzer().NormalizeSubjectWithValue("Re:\r\n\0 Topic"), Is.EqualTo("topic"));

        EmailThreadIdentity fallback = new EmailThreadAnalyzer().ExtractWithMessage(ParseMessage(
            "Subject: =?utf-8?Q?RE=3A_Caf=C3=A9_Update?=\r\n\r\nbody"));
        Assert.That(fallback.HasMessageIdentifier(), Is.False);
        Assert.That(fallback.UsesSubjectFallback(), Is.True);
        Assert.That(fallback.GetThreadKey(), Is.EqualTo("subject:café update"));

        EmailThreadOptions disabled = new EmailThreadOptions();
        disabled.SetEnableSubjectFallback(false);
        EmailThreadIdentity withoutFallback = new EmailThreadAnalyzer().ExtractWithMessageAndOptions(ParseMessage("Subject: Re: Topic\r\n\r\nbody"), disabled);
        Assert.That(withoutFallback.HasNormalizedSubject(), Is.True);
        Assert.That(withoutFallback.HasThreadKey(), Is.False);
        Assert.That(withoutFallback.GetThreadIdentitySource(), Is.EqualTo(EmailThreadIdentitySource.None));
    }

    [Test]
    public void RecoveredIdentifiersRequireExplicitOptIn()
    {
        EmailMessage message = ParseMessage("Message-ID: bare@EXAMPLE.TEST\r\nSubject: Re: Fallback\r\n\r\nbody");

        EmailThreadIdentity safe = new EmailThreadAnalyzer().ExtractWithMessage(message);
        Assert.That(safe.HasMessageIdentifier(), Is.False);
        Assert.That(safe.UsesSubjectFallback(), Is.True);
        Assert.That(safe.GetIssues().ContainsCode(EmailIssueCode.MessageIdRecoveryApplied), Is.True);

        EmailThreadOptions permissive = new EmailThreadOptions();
        permissive.SetAllowRecoveredIdentifiers(true);
        EmailThreadIdentity recovered = new EmailThreadAnalyzer().ExtractWithMessageAndOptions(message, permissive);
        Assert.That(recovered.GetCanonicalMessageIdentifier(), Is.EqualTo("<bare@example.test>"));
        Assert.That(recovered.GetThreadIdentitySource(), Is.EqualTo(EmailThreadIdentitySource.MessageId));
    }

    [Test]
    public void IdentifierCollectionsAndIssuesAreDefensiveAndDuplicatesAreCollapsed()
    {
        EmailThreadIdentity identity = new EmailThreadAnalyzer().ExtractWithMessage(ParseMessage(
            "Message-ID: <same@example.test>\r\n"
            + "Message-ID: <same@EXAMPLE.TEST>\r\n"
            + "References: noise <root@example.test> <root@EXAMPLE.TEST>\r\n\r\nbody"));
        List<EmailMessageId> messageIds = identity.GetMessageIdentifiers();
        List<EmailMessageId> references = identity.GetReferences();
        EmailIssueList issues = identity.GetIssues();
        messageIds.Clear();
        references.Clear();
        issues.Clear();

        Assert.That(identity.GetMessageIdentifierCount(), Is.EqualTo(1));
        Assert.That(identity.GetReferenceCount(), Is.EqualTo(1));
        Assert.That(identity.GetIssues().GetCount(), Is.GreaterThan(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => identity.GetReferenceAt(1));
    }

    [Test]
    public void ExtractionAndSubjectLimitsFailWithoutPartialIdentity()
    {
        EmailMessage identifiers = ParseMessage(
            "Message-ID: <child@example.test>\r\nReferences: <parent@example.test>\r\n\r\nbody");
        EmailThreadOptions options = new EmailThreadOptions();
        options.SetMaximumIdentifierCount(1);
        AssertLimit("MaxThreadIdentifiers", () => new EmailThreadAnalyzer().ExtractWithMessageAndOptions(identifiers, options));

        options = new EmailThreadOptions();
        options.SetMaximumHeaderCount(1);
        AssertLimit("MaxThreadHeaders", () => new EmailThreadAnalyzer().ExtractWithMessageAndOptions(ParseMessage("X-One: one\r\nSubject: topic\r\n\r\nbody"), options));
        options = new EmailThreadOptions();
        options.SetMaximumIdentifierBytes(5);
        AssertLimit("MaxThreadIdentifierBytes", () => new EmailThreadAnalyzer().ExtractWithMessageAndOptions(identifiers, options));
        options = new EmailThreadOptions();
        options.SetMaximumSubjectBytes(3);
        AssertLimit("MaxThreadSubjectBytes", () => new EmailThreadAnalyzer().ExtractWithMessageAndOptions(ParseMessage("Subject: topic\r\n\r\nbody"), options));
        options = new EmailThreadOptions();
        options.SetMaximumSubjectCharacters(3);
        AssertLimit("MaxThreadSubjectCharacters", () => new EmailThreadAnalyzer().NormalizeSubjectWithValueAndOptions("topic", options));
    }

    [Test]
    public void OptionsCloneAndPublicInputsAreValidated()
    {
        EmailThreadOptions options = new EmailThreadOptions();
        options.SetAllowRecoveredIdentifiers(true);
        options.SetEnableSubjectFallback(false);
        options.SetMaximumIdentifierCount(5);
        EmailThreadOptions clone = options.Clone();
        options.SetAllowRecoveredIdentifiers(false);
        options.SetEnableSubjectFallback(true);
        options.SetMaximumIdentifierCount(6);

        Assert.That(clone.GetAllowRecoveredIdentifiers(), Is.True);
        Assert.That(clone.GetEnableSubjectFallback(), Is.False);
        Assert.That(clone.GetMaximumIdentifierCount(), Is.EqualTo(5));
        Assert.Throws<ArgumentNullException>(() => new EmailThreadAnalyzer().ExtractWithMessage(null!));
        Assert.Throws<ArgumentNullException>(() => new EmailThreadAnalyzer().ExtractWithMessageAndOptions(new EmailMessageOperations().CreateWith(), null!));
        Assert.Throws<ArgumentNullException>(() => new EmailThreadAnalyzer().NormalizeSubjectWithValue(null!));
        Assert.Throws<ArgumentNullException>(() => new EmailThreadAnalyzer().CanonicalizeIdentifier(null));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumHeaderCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumIdentifierCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumIdentifierBytes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumSubjectBytes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumSubjectCharacters(0));
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.UTF8.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetMessage()!;
    }

    static void AssertLimit(string expectedName, TestDelegate action)
    {
        EmailLimitException exception = Assert.Throws<EmailLimitException>(action);
        Assert.That(exception.GetLimitName(), Is.EqualTo(expectedName));
    }
}
