using System.Text;
using email;

namespace email.Tests;

public class EmailMessageTests
{
    [Test]
    public void MessageWithoutSourceIsExactDirtyButKeepsSharedModelObjects()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWith();
        EmailMessage message = new EmailMessageOperations().CreateWithHeaders(headers);

        Assert.That(message.GetHeaders(), Is.SameAs(headers));
        Assert.That(message.GetEnvelope(), Is.Not.Null);
        Assert.That(message.HasSourceInfo(), Is.False);
        Assert.That(message.IsExactDirty(), Is.True);
        Assert.That(message.IsSemanticDirty(), Is.False);
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactOnly));
        Assert.That(message.GetCleanRanges(), Is.Empty);

        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("Subject", Bytes(" value")));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactAndSemantic));
    }

    [Test]
    public void PreservedMessageStartsCleanWithItsWholeSourceReusable()
    {
        EmailMessage message = SourceMessage();

        Assert.That(message.HasSourceInfo(), Is.True);
        Assert.That(message.GetSourceInfo()!.IsComplete(), Is.True);
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
        Assert.That(message.GetCleanRanges(), Has.Count.EqualTo(1));
        Assert.That(message.GetCleanRanges()[0].EqualsRange(new EmailRange(0, 100)), Is.True);
        Assert.That(message.IsRangeClean(new EmailRange(42, 20)), Is.True);
    }

    [Test]
    public void HeaderValueChangeIsExactAndSemanticAndOnlyInvalidatesItsRange()
    {
        EmailMessage message = SourceMessage();

        message.GetHeaders()!.GetAt(0).SetValue(" two");

        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactAndSemantic));
        List<EmailRange> clean = message.GetCleanRanges();
        Assert.That(clean, Has.Count.EqualTo(1));
        Assert.That(clean[0].EqualsRange(new EmailRange(14, 86)), Is.True);
        Assert.That(message.IsRangeClean(new EmailRange(0, 1)), Is.False);
        Assert.That(message.IsRangeClean(new EmailRange(14, 1)), Is.True);

        message.GetHeaders()!.GetAt(0).ResetToOriginal();
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void FormattingOnlyHeaderChangesAreExactButNotSemantic()
    {
        EmailMessage message = SourceMessage();
        EmailHeader header = message.GetHeaders()!.GetAt(0);

        header.SetName("subject");
        header.SetLineEnding(EmailLineEnding.Lf);

        Assert.That(message.IsExactDirty(), Is.True);
        Assert.That(message.IsSemanticDirty(), Is.False);
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactOnly));
    }

    [Test]
    public void InterpretationChangesCanBeSemanticWithoutChangingExactBytes()
    {
        EmailMessage message = SourceMessage();
        EmailHeader header = message.GetHeaders()!.GetAt(0);

        header.SetValueKind(EmailHeaderValueKind.Unknown);
        header.SetParseMode(EmailStructuredValueParseMode.Strict);

        Assert.That(message.IsExactDirty(), Is.False);
        Assert.That(message.IsSemanticDirty(), Is.True);
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.SemanticOnly));
        Assert.That(message.GetCleanRanges()[0].EqualsRange(new EmailRange(0, 100)), Is.True);
    }

    [Test]
    public void HeaderCollectionChangesInvalidateTheHeaderBlockNotTheBody()
    {
        EmailMessage message = SourceMessage();

        message.GetHeaders()!.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("X-New", Bytes(" value")));

        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactAndSemantic));
        List<EmailRange> clean = message.GetCleanRanges();
        Assert.That(clean, Has.Count.EqualTo(1));
        Assert.That(clean[0].EqualsRange(new EmailRange(40, 60)), Is.True);
        Assert.That(message.IsRangeClean(new EmailRange(42, 58)), Is.True);
    }

    [Test]
    public void EnvelopeRecipientsAreSemanticWhileMboxLineAlsoChangesExactSource()
    {
        byte[] line = Bytes("From old@example.test\n");
        EmailRange lineRange = new EmailRange(0, line.Length);
        EmailEnvelope envelope = new EmailEnvelopeOperations().CreateWithSenderAndRecipientsAndMboxFromLineAndMboxFromLineRange(null, new List<EmailAddress>(), line, lineRange);
        EmailMessage message = SourceMessage(envelope, line.Length);

        envelope.AddRecipient(Address("hidden", "example.test"));

        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.SemanticOnly));
        Assert.That(message.GetCleanRanges()[0].EqualsRange(new EmailRange(0, 100)), Is.True);

        envelope.SetMboxFromLine(Bytes("From new@example.test\n"));

        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactAndSemantic));
        Assert.That(message.GetCleanRanges()[0].EqualsRange(new EmailRange(line.Length, 100 - line.Length)), Is.True);
    }

    [Test]
    public void ExplicitDirtyRangesSplitReusableSourceConservatively()
    {
        EmailMessage message = SourceMessage();

        message.MarkRangeDirty(new EmailRange(40, 20), false);

        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactOnly));
        Assert.That(message.GetCleanRanges(), Has.Count.EqualTo(2));
        Assert.That(message.GetCleanRanges()[0].EqualsRange(new EmailRange(0, 40)), Is.True);
        Assert.That(message.GetCleanRanges()[1].EqualsRange(new EmailRange(60, 40)), Is.True);
        Assert.That(message.IsRangeClean(new EmailRange(5, 5)), Is.True);
        Assert.That(message.IsRangeClean(new EmailRange(45, 5)), Is.False);

        message.MarkSemanticDirty();
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactAndSemantic));
    }

    [Test]
    public void MessageIssuesAggregateHeaderIssuesAndRemainDefensive()
    {
        EmailIssueList headerIssues = new EmailIssueList();
        headerIssues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.HeaderMalformed, "header issue"));
        EmailHeader header = PreservedHeader(0, headerIssues);
        EmailIssueList messageIssues = new EmailIssueList();
        messageIssues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Information, EmailIssueCode.ValidationFailed, "message issue"));
        EmailMessage message = new EmailMessageOperations().CreateWithHeadersAndEnvelopeAndSourceInfoAndIssues(new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { header }), new EmailEnvelopeOperations().CreateWith(), SourceInfo(0), messageIssues);

        EmailIssueList aggregate = message.GetIssues();
        EmailIssueList own = message.GetOwnIssues();
        aggregate.Clear();
        own.Clear();

        Assert.That(message.GetIssues().GetCount(), Is.EqualTo(2));
        Assert.That(message.GetOwnIssues().GetCount(), Is.EqualTo(1));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));

        message.ClearOwnIssues();
        Assert.That(message.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void SourceInfoAndEnvelopeValidateInputsAndReturnDefensiveCollections()
    {
        EmailContent content = new EmailContentOperations().CreateMemory(new byte[100], EmailContentOwnership.Borrowed, false);
        EmailSourceInfo info = new EmailSourceInfoOperations().CreateWithContentAndMessageRangeAndHeaderBlockRangeAndSeparatorRangeAndBodyRangeAndSourceNameAndComplete(content, new EmailRange(0, 100), new EmailRange(10, 30), new EmailRange(40, 2), new EmailRange(42, 58), "message.eml", false);
        EmailEnvelope envelope = new EmailEnvelopeOperations().CreateWith();
        EmailAddress recipient = Address("one", "example.test");
        envelope.AddRecipient(recipient);
        List<EmailAddress> recipients = envelope.GetRecipients();
        recipients.Clear();
        byte[] line = Bytes("From sender@example.test\r\n");
        envelope.SetMboxFromLine(line);
        byte[] returnedLine = envelope.GetMboxFromLine()!;
        returnedLine![0] = (byte)'X';

        Assert.That(info.GetContent(), Is.SameAs(content));
        Assert.That(info.HasSourceName(), Is.True);
        Assert.That(info.GetSourceName(), Is.EqualTo("message.eml"));
        Assert.That(info.IsComplete(), Is.False);
        Assert.That(envelope.GetRecipientCount(), Is.EqualTo(1));
        Assert.That(envelope.GetRecipientAt(0), Is.SameAs(recipient));
        Assert.That(Encoding.ASCII.GetString(envelope.GetMboxFromLine()!), Is.EqualTo("From sender@example.test\r\n"));
        Assert.Throws<ArgumentException>(() => envelope.SetMboxFromLine(Bytes("not an mbox line")));
        Assert.Throws<ArgumentException>(() => envelope.SetMboxFromLine(Bytes("From safe\nInjected")));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailSourceInfoOperations().CreateWithContentAndMessageRange(content, new EmailRange(0, 101)));
        Assert.Throws<ArgumentException>(() => new EmailSourceInfoOperations().CreateWithContentAndMessageRangeAndHeaderBlockRangeAndSeparatorRangeAndBodyRangeAndSourceNameAndComplete(content, new EmailRange(0, 100), new EmailRange(30, 20), new EmailRange(10, 2), null, null, true));
    }

    static EmailMessage SourceMessage()
    {
        return SourceMessage(new EmailEnvelopeOperations().CreateWith(), 0);
    }

    static EmailMessage SourceMessage(EmailEnvelope envelope, int headerStart)
    {
        EmailHeader header = PreservedHeader(headerStart, new EmailIssueList());
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { header });
        return new EmailMessageOperations().CreateWithHeadersAndEnvelopeAndSourceInfoAndIssues(headers, envelope, SourceInfo(headerStart), new EmailIssueList());
    }

    static EmailSourceInfo SourceInfo(int headerStart)
    {
        EmailContent content = new EmailContentOperations().CreateMemory(new byte[100], EmailContentOwnership.Borrowed, false);
        return new EmailSourceInfoOperations().CreateWithContentAndMessageRangeAndHeaderBlockRangeAndSeparatorRangeAndBodyRangeAndSourceNameAndComplete(content, new EmailRange(0, 100), new EmailRange(headerStart, 40 - headerStart), new EmailRange(40, 2), new EmailRange(42, 58), "source.eml", true);
    }

    static EmailHeader PreservedHeader(int start, EmailIssueList issues)
    {
        return new EmailHeaderOperations().CreateWithOriginalNameBytesAndOriginalRawValueAndLineEndingAndSourceRangeAndValuePositionAndValueKindAndParseModeAndIssuesAndExactOriginalRawFieldBytes(Bytes("Subject"), Bytes(" one"), EmailLineEnding.CrLf, new EmailRange(start, 14), new EmailSourcePosition(start + 8, 1, 9), EmailHeaderValueKind.Unstructured, EmailStructuredValueParseMode.Tolerant, issues, Bytes("Subject: one\r\n"));
    }

    static EmailAddress Address(string localPart, string domain)
    {
        byte[] raw = Bytes(localPart + "@" + domain);
        return new EmailAddress(null, localPart, domain, raw, Bytes(localPart), Bytes(domain), new EmailSourcePosition(0, 1, 1), false, false, false, new List<string>());
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }
}
