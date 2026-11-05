using System.IO;
using System.Text;
using email;

namespace email.Tests;

public class EmailMessageAnalyzerTests
{
    [Test]
    public void MeasuresExactRawMessageAndDecodedPayloadSizes()
    {
        const string source = "Content-Type: text/plain\r\nContent-Transfer-Encoding: base64\r\n\r\nSGVsbG8=";
        EmailMessageSummary summary = new EmailMessageAnalyzer().AnalyzeWithMessage(ParseMessage(source));

        Assert.That(summary.HasRawMessageByteCount(), Is.True);
        Assert.That(summary.GetRawMessageByteCount(), Is.EqualTo(Encoding.ASCII.GetByteCount(source)));
        Assert.That(summary.GetRawContentByteCount(), Is.EqualTo(8));
        Assert.That(summary.IsRawContentByteCountComplete(), Is.True);
        Assert.That(summary.GetDecodedContentByteCount(), Is.EqualTo(5));
        Assert.That(summary.IsDecodedContentByteCountComplete(), Is.True);
        Assert.That(summary.GetPartCount(), Is.EqualTo(1));
        Assert.That(summary.GetLeafPartCount(), Is.EqualTo(1));
        Assert.That(summary.GetContainerPartCount(), Is.EqualTo(0));
        Assert.That(summary.GetMaximumDepth(), Is.EqualTo(0));
        Assert.That(summary.GetDistinctMediaTypeCount(), Is.EqualTo(1));
        Assert.That(summary.GetMediaTypeAt(0).GetMediaType(), Is.EqualTo("text/plain"));
        Assert.That(summary.GetMediaTypeEntityCount("TEXT", "PLAIN"), Is.EqualTo(1));
    }

    [Test]
    public void SummarizesPartsMediaTypesAndDownloadableAttachmentsInTraversalOrder()
    {
        const string source = "Content-Type: multipart/mixed; boundary=m\r\n\r\n"
            + "--m\r\nContent-Type: multipart/alternative; boundary=a\r\n\r\n"
            + "--a\r\nContent-Type: text/plain\r\n\r\nplain\r\n"
            + "--a\r\nContent-Type: text/html\r\n\r\n<p>html</p>\r\n--a--\r\n"
            + "--m\r\nContent-Type: application/pdf\r\nContent-Disposition: attachment; filename=file.pdf\r\n\r\nPDF\r\n"
            + "--m--\r\n";

        EmailMessageSummary summary = new EmailMessageAnalyzer().SummarizeWithMessage(ParseMessage(source));

        Assert.That(summary.GetPartCount(), Is.EqualTo(5));
        Assert.That(summary.GetLeafPartCount(), Is.EqualTo(3));
        Assert.That(summary.GetContainerPartCount(), Is.EqualTo(2));
        Assert.That(summary.GetMaximumDepth(), Is.EqualTo(2));
        Assert.That(summary.GetDistinctMediaTypeCount(), Is.EqualTo(5));
        Assert.That(summary.GetMediaTypeAt(0).GetMediaType(), Is.EqualTo("multipart/mixed"));
        Assert.That(summary.GetMediaTypeEntityCount("text", "plain"), Is.EqualTo(1));
        Assert.That(summary.GetAttachmentCount(), Is.EqualTo(1));
        Assert.That(summary.GetNestedMessageCount(), Is.EqualTo(0));
        Assert.That(summary.GetAttachmentRawByteCount(), Is.GreaterThanOrEqualTo(3));
        Assert.That(summary.GetAttachmentDecodedByteCount(), Is.EqualTo(summary.GetAttachmentRawByteCount()));
        Assert.That(summary.IsAttachmentRawByteCountComplete(), Is.True);
        Assert.That(summary.IsAttachmentDecodedByteCountComplete(), Is.True);
    }

    [Test]
    public void CountsRecipientsDatesAndAuthenticationEvidenceWithoutTrustClaims()
    {
        const string source = "To: a@example.test, Team: b@example.test, c@example.test;\r\n"
            + "Cc: d@example.test\r\nBcc: e@example.test\r\n"
            + "Resent-To: f@example.test, g@example.test\r\n"
            + "Date: Tue, 02 Jan 2024 12:00:00 +0000\r\n"
            + "Resent-Date: Wed, 03 Jan 2024 13:00:00 +0000\r\n"
            + "Received: from source.example by mx.example; Mon, 01 Jan 2024 11:00:00 +0000\r\n"
            + "DKIM-Signature: v=1; a=rsa-sha256; d=example.test; s=x; h=from; bh=AA==; b=AA==\r\n"
            + "ARC-Seal: i=1; a=rsa-sha256; d=example.test; s=x; t=1; cv=none; b=AA==\r\n"
            + "ARC-Authentication-Results: i=1; mx.example; dkim=pass\r\n"
            + "Authentication-Results: mx.example; dkim=pass\r\n"
            + "Received-SPF: pass client-ip=192.0.2.1\r\n\r\nbody";
        EmailMessage message = ParseMessage(source);
        message.GetEnvelope()!.AddRecipient(Address("envelope-one@example.test"));
        message.GetEnvelope()!.AddRecipient(Address("envelope-two@example.test"));

        EmailMessageSummary summary = new EmailMessageAnalyzer().AnalyzeWithMessage(message);
        EmailSecuritySummary security = summary.GetSecuritySummary();

        Assert.That(summary.GetToRecipientCount(), Is.EqualTo(3));
        Assert.That(summary.GetCcRecipientCount(), Is.EqualTo(1));
        Assert.That(summary.GetBccRecipientCount(), Is.EqualTo(1));
        Assert.That(summary.GetHeaderRecipientCount(), Is.EqualTo(5));
        Assert.That(summary.GetResentRecipientCount(), Is.EqualTo(2));
        Assert.That(summary.GetEnvelopeRecipientCount(), Is.EqualTo(2));
        Assert.That(summary.HasDate(), Is.True);
        Assert.That(summary.GetDate()!.GetYear(), Is.EqualTo(2024));
        Assert.That(summary.GetResentDateCount(), Is.EqualTo(1));
        Assert.That(summary.GetReceivedDateCount(), Is.EqualTo(1));
        Assert.That(security.GetDkimSignatureHeaderCount(), Is.EqualTo(1));
        Assert.That(security.GetArcHeaderCount(), Is.EqualTo(2));
        Assert.That(security.GetAuthenticationResultsHeaderCount(), Is.EqualTo(1));
        Assert.That(security.GetReceivedSpfHeaderCount(), Is.EqualTo(1));
        Assert.That(security.HasAuthenticationEvidence(), Is.True);
        Assert.That(security.HasSecurityEvidence(), Is.True);
        Assert.That(security.ClaimsVerification(), Is.False);
    }

    [Test]
    public void CountsStructuralSecurityAndIssuesWithoutClaimingVerification()
    {
        EmailMessage message = ParseMessage(
            "Content-Type: multipart/signed; boundary=s; protocol=application/pgp-signature\r\n\r\n"
            + "--s\r\nContent-Type: text/plain\r\n\r\nsigned body\r\n"
            + "--s\r\nContent-Type: application/pgp-signature\r\n\r\nsignature\r\n--s--\r\n");
        int initialIssueCount = message.GetIssues().GetCount();
        int initialWarningCount = message.GetIssues().GetCountBySeverity(EmailIssueSeverity.Warning);
        int initialErrorCount = message.GetIssues().GetCountBySeverity(EmailIssueSeverity.Error);
        message.AddIssue(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.GeneralInvalidInput, "warning"));
        message.AddIssue(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Error, EmailIssueCode.ValidationFailed, "error"));

        EmailMessageSummary summary = new EmailMessageAnalyzer().AnalyzeWithMessage(message);
        EmailSecuritySummary security = summary.GetSecuritySummary();

        Assert.That(security.GetSignedContentCount(), Is.EqualTo(1));
        Assert.That(security.GetSignaturePartCount(), Is.EqualTo(1));
        Assert.That(security.HasSignedContent(), Is.True);
        Assert.That(security.HasEncryptedContent(), Is.False);
        Assert.That(security.ClaimsVerification(), Is.False);
        Assert.That(summary.GetIssueCount(), Is.EqualTo(initialIssueCount + 2));
        Assert.That(summary.GetWarningIssueCount(), Is.EqualTo(initialWarningCount + 1));
        Assert.That(summary.GetErrorIssueCount(), Is.EqualTo(initialErrorCount + 1));

        EmailMessage encrypted = ParseMessage(
            "Content-Type: multipart/encrypted; boundary=e; protocol=application/pgp-encrypted\r\n\r\n"
            + "--e\r\nContent-Type: application/pgp-encrypted\r\n\r\nVersion: 1\r\n"
            + "--e\r\nContent-Type: application/octet-stream\r\n\r\nencrypted\r\n--e--\r\n");
        EmailSecuritySummary encryptedSecurity = new EmailMessageAnalyzer().AnalyzeWithMessage(encrypted).GetSecuritySummary();
        Assert.That(encryptedSecurity.GetEncryptedContentCount(), Is.EqualTo(1));
        Assert.That(encryptedSecurity.GetEncryptionPartCount(), Is.EqualTo(2));

        EmailMessage report = new EmailMessageOperations().CreateWith();
        report.SetBody(new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("report", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("report") }));
        Assert.That(new EmailMessageAnalyzer().AnalyzeWithMessage(report).GetSecuritySummary().GetReportPartCount(), Is.EqualTo(1));
    }

    [Test]
    public void UnknownNonRepeatableContentIsNotConsumedAndProducesIncompleteTotals()
    {
        AnalysisNonSeekableStream stream = new AnalysisNonSeekableStream(new byte[] { 1, 2, 3, 4 });
        EmailContent content = new EmailContentOperations().CreateStream(stream, EmailContentOwnership.Borrowed);
        EmailMediaType type = new EmailMediaTypeParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes("application/octet-stream"), EmailStructuredValueParseMode.Strict).GetValue();
        MimeEntity body = new EmailEntityFactory().CreateBinaryWithContentAndMediaType(content, type);
        EmailMessage message = new EmailMessageOperations().CreateWith();
        message.SetBody(body);

        EmailMessageSummary summary = new EmailMessageAnalyzer().AnalyzeWithMessage(message);

        Assert.That(summary.HasRawMessageByteCount(), Is.False);
        Assert.That(summary.GetRawMessageByteCount(), Is.EqualTo(0));
        Assert.That(summary.IsRawContentByteCountComplete(), Is.False);
        Assert.That(summary.IsDecodedContentByteCountComplete(), Is.False);
        Assert.That(summary.GetDecodedContentByteCount(), Is.EqualTo(0));
        Assert.That(stream.GetReadCount(), Is.EqualTo(0));
    }

    [Test]
    public void SummaryCollectionsAreDefensiveAndMessagesWithoutBodiesAreSupported()
    {
        EmailMessage message = new EmailMessageOperations().CreateWith();
        message.AddIssue(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Information, EmailIssueCode.RecoveryApplied, "information"));
        EmailMessageSummary summary = new EmailMessageAnalyzer().AnalyzeWithMessage(message);
        List<EmailMediaTypeStatistic> media = summary.GetMediaTypes();
        List<EmailDateTime> resent = summary.GetResentDates();
        List<EmailDateTime> received = summary.GetReceivedDates();
        EmailIssueList issues = summary.GetIssues();
        media.Clear();
        resent.Clear();
        received.Clear();
        issues.Clear();

        Assert.That(summary.GetPartCount(), Is.EqualTo(0));
        Assert.That(summary.GetMaximumDepth(), Is.EqualTo(0));
        Assert.That(summary.GetDistinctMediaTypeCount(), Is.EqualTo(0));
        Assert.That(summary.GetIssueCount(), Is.EqualTo(1));
        Assert.That(summary.GetInformationIssueCount(), Is.EqualTo(1));
        Assert.That(summary.GetSecuritySummary().HasSecurityEvidence(), Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() => summary.GetMediaTypeAt(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => summary.GetResentDateAt(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => summary.GetReceivedDateAt(0));
        Assert.Throws<ArgumentNullException>(() => summary.GetMediaTypeEntityCount(null!, "plain"));
    }

    [Test]
    public void AnalysisLimitsOptionsAndInputsAreValidated()
    {
        EmailMessage twoHeaders = ParseMessage("To: a@example.test\r\nCc: b@example.test\r\n\r\nbody");
        EmailMessageAnalysisOptions options = new EmailMessageAnalysisOptions();
        options.SetMaximumHeaderCount(1);
        AssertLimit("MaxAnalysisHeaders", () => new EmailMessageAnalyzer().AnalyzeWithMessageAndOptions(twoHeaders, options));

        options = new EmailMessageAnalysisOptions();
        options.SetMaximumAddressCount(1);
        AssertLimit("MaxAnalysisRecipients", () => new EmailMessageAnalyzer().AnalyzeWithMessageAndOptions(twoHeaders, options));

        options = new EmailMessageAnalysisOptions();
        options.SetMaximumStructuredHeaderBytes(3);
        AssertLimit("MaxAnalysisStructuredHeaderBytes", () => new EmailMessageAnalyzer().AnalyzeWithMessageAndOptions(twoHeaders, options));

        options = new EmailMessageAnalysisOptions();
        options.SetMaximumDecodedContentBytes(4);
        AssertLimit("MaxAnalysisDecodedContentBytes", () => new EmailMessageAnalyzer().AnalyzeWithMessageAndOptions(ParseMessage("\r\nhello"), options));

        options = new EmailMessageAnalysisOptions();
        options.SetMaximumEntityCount(1);
        EmailMessage multipart = new EmailMessageOperations().CreateWith();
        multipart.SetBody(new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("one") }));
        AssertLimit("MaxTraversalEntityCount", () => new EmailMessageAnalyzer().AnalyzeWithMessageAndOptions(multipart, options));

        EmailMessageAnalysisOptions cloneSource = new EmailMessageAnalysisOptions();
        cloneSource.SetMaximumDepth(2);
        cloneSource.SetMaximumEntityCount(3);
        cloneSource.SetMeasureRepeatableDecodedContent(false);
        EmailMessageAnalysisOptions clone = cloneSource.Clone();
        cloneSource.SetMaximumDepth(4);
        Assert.That(clone.GetMaximumDepth(), Is.EqualTo(2));
        Assert.That(clone.GetMaximumEntityCount(), Is.EqualTo(3));
        Assert.That(clone.GetMeasureRepeatableDecodedContent(), Is.False);

        Assert.Throws<ArgumentNullException>(() => new EmailMessageAnalyzer().AnalyzeWithMessage(null!));
        Assert.Throws<ArgumentNullException>(() => new EmailMessageAnalyzer().AnalyzeWithMessageAndOptions(twoHeaders, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumDepth(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumEntityCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumHeaderCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumAddressCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumStructuredHeaderBytes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumDecodedContentBytes(0));
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.ASCII.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.HasMessage(), Is.True);
        return result.GetMessage()!;
    }

    static EmailAddress Address(string source)
    {
        return new EmailAddressParser().ParseMailbox(Encoding.ASCII.GetBytes(source), EmailAddressParseMode.Strict).GetMailbox()!;
    }

    static void AssertLimit(string expectedName, TestDelegate action)
    {
        EmailLimitException exception = Assert.Throws<EmailLimitException>(action);
        Assert.That(exception.GetLimitName(), Is.EqualTo(expectedName));
    }
}

class AnalysisNonSeekableStream : Stream
{
    byte[] value;
    int position;
    int readCount;

    public AnalysisNonSeekableStream(byte[] value)
    {
        this.value = value;
        position = 0;
        readCount = 0;
    }

    public int GetReadCount() { return readCount; }
    public override bool CanRead { get { return true; } }
    public override bool CanSeek { get { return false; } }
    public override bool CanWrite { get { return false; } }
    public override long Length { get { throw new NotSupportedException(); } }
    public override long Position { get { return position; } set { throw new NotSupportedException(); } }
    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        readCount++;
        int available = Math.Min(count, value.Length - position);
        Array.Copy(value, position, buffer, offset, available);
        position += available;
        return available;
    }

    public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
    public override void SetLength(long value) { throw new NotSupportedException(); }
    public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
}
