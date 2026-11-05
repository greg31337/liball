using System.Net;
using System.Text;
using email;

namespace email.Tests;

public class EmailFeedbackReportTests
{
    [Test]
    public void ParsesSourceAuthenticationRecipientAndReportedMetadata()
    {
        EmailFeedbackReportParseResult result = ParseReport(
            "Feedback-Type: auth-failure\r\n"
            + "User-Agent: ExampleReporter/2.0\r\n"
            + "Version: 1\r\n"
            + "Original-Envelope-Id: envelope-9\r\n"
            + "Original-Mail-From: <sender@example.test>\r\n"
            + "Arrival-Date: Tue, 8 Mar 2005 14:00:00 -0400\r\n"
            + "Reporting-MTA: dns; mx.example.test\r\n"
            + "Source-IP: 2001:db8::7\r\n"
            + "Source-Port: 2525\r\n"
            + "Incidents: 4294967295\r\n"
            + "Auth-Failure: spf\r\n"
            + "Delivery-Result: reject\r\n"
            + "Authentication-Results: mx.example.test; spf=fail smtp.mailfrom=sender@example.test\r\n"
            + "SPF-DNS: txt: example.test: \"v=spf1 -all\"\r\n"
            + "Original-Rcpt-To: <first@example.test>\r\n"
            + "Original-Rcpt-To: <second@example.test>\r\n"
            + "Reported-Domain: EXAMPLE.NET\r\n"
            + "Reported-URI: https://example.net/abuse?id=7\r\n"
            + "Reported-URI: mailto:abuse@example.net\r\n"
            + "X-Provider-Trace: retained\r\n",
            OriginalHeaders());

        EmailFeedbackReport value = result.GetFeedbackReport()!;

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(value!.GetFeedbackType(), Is.EqualTo(EmailFeedbackType.AuthenticationFailure));
        Assert.That(value.GetFeedbackTypeName(), Is.EqualTo("auth-failure"));
        Assert.That(value.GetUserAgent(), Is.EqualTo("ExampleReporter/2.0"));
        Assert.That(value.GetVersion(), Is.EqualTo(1));
        Assert.That(value.GetOriginalEnvelopeId(), Is.EqualTo("envelope-9"));
        Assert.That(value.GetOriginalMailFrom()!.GetAddress()!.GetLocalPart(), Is.EqualTo("sender"));
        Assert.That(value.HasArrivalDate(), Is.True);
        Assert.That(value.GetReportingMta()!.GetValue(), Is.EqualTo("mx.example.test"));
        Assert.That(value.GetSourceIp(), Is.EqualTo(IPAddress.Parse("2001:db8::7")));
        Assert.That(value.GetSourcePort(), Is.EqualTo(2525));
        Assert.That(value.GetIncidents(), Is.EqualTo(uint.MaxValue));
        Assert.That(value.GetAuthenticationResults().Single().GetAuthenticationServiceIdentifier(), Is.EqualTo("mx.example.test"));
        Assert.That(value.GetAuthenticationResults().Single().GetResultCount(), Is.EqualTo(1));
        Assert.That(value.GetAuthenticationFailure(), Is.EqualTo(EmailFeedbackAuthenticationFailure.Spf));
        Assert.That(value.GetDeliveryResult(), Is.EqualTo(EmailFeedbackDeliveryResult.Reject));
        Assert.That(value.GetSpfDnsValues().Single(), Does.Contain("v=spf1 -all"));
        Assert.That(value.GetOriginalRecipients().Select(path => path.GetAddress()!.GetLocalPart()), Is.EqualTo(new[] { "first", "second" }));
        Assert.That(value.GetReportedDomains().Single(), Is.EqualTo("example.net"));
        Assert.That(value.GetReportedUris().Count, Is.EqualTo(2));
        Assert.That(value.GetExtensionFields().Single().GetDecodedValue(), Is.EqualTo("retained"));
        Assert.That(value.HasOriginalHeaders(), Is.True);
        value.GetOriginalHeaders()!.Clear();
        Assert.That(value.GetOriginalHeaders()!.Contains("Subject"), Is.True);
    }

    [TestCase("abuse", EmailFeedbackType.Abuse)]
    [TestCase("auth-failure", EmailFeedbackType.AuthenticationFailure)]
    [TestCase("fraud", EmailFeedbackType.Fraud)]
    [TestCase("not-spam", EmailFeedbackType.NotSpam)]
    [TestCase("other", EmailFeedbackType.Other)]
    [TestCase("virus", EmailFeedbackType.Virus)]
    [TestCase("future-type", EmailFeedbackType.Extension)]
    public void RecognizesRegisteredTypesAndRetainsFutureExtensions(string name, EmailFeedbackType expected)
    {
        string fields = RequiredFields(name);
        if (expected == EmailFeedbackType.AuthenticationFailure) fields += "Auth-Failure: spf\r\nAuthentication-Results: mx.example.test; spf=fail smtp.mailfrom=sender@example.test\r\n";
        EmailFeedbackReportParseResult result = ParseReport(fields, OriginalHeaders());
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetFeedbackReport()!.GetFeedbackType(), Is.EqualTo(expected));
        Assert.That(result.GetFeedbackReport()!.GetFeedbackTypeName(), Is.EqualTo(name));
    }

    [Test]
    public void DiagnosesRequiredDuplicateVersionSourceAndDateFailures()
    {
        EmailFeedbackReportParseResult malformed = ParseReport(
            "Feedback-Type: abuse\r\nFeedback-Type: fraud\r\n"
            + "User-Agent: \r\nVersion: 2\r\n"
            + "Arrival-Date: Tue, 8 Mar 2005 14:00:00 -0400\r\n"
            + "Received-Date: Tue, 8 Mar 2005 14:00:00 -0400\r\n"
            + "Source-IP: 999.1.1.1\r\nSource-Port: 70000\r\nIncidents: 0\r\n"
            + "Original-Rcpt-To: broken\r\nReported-Domain: bad..example\r\nReported-URI: relative/path\r\n"
            + "Authentication-Results: broken\r\n",
            null!);

        Assert.That(malformed.HasFeedbackReport(), Is.True);
        Assert.That(malformed.IsSuccess(), Is.False);
        Assert.That(malformed.GetIssues().ContainsCode(EmailIssueCode.FeedbackReportMalformed), Is.True);
        Assert.That(malformed.GetIssues().ContainsCode(EmailIssueCode.FeedbackReportValueInvalid), Is.True);
        Assert.That(malformed.GetIssues().ContainsCode(EmailIssueCode.FeedbackReportVersionUnsupported), Is.True);
        Assert.That(malformed.GetIssues().ContainsCode(EmailIssueCode.FeedbackReportOriginalContentMissing), Is.True);
        Assert.Throws<InvalidOperationException>(() => new EmailFeedbackReportWriter().WriteWithReport(malformed.GetFeedbackReport()!));

        EmailFeedbackReportParseResult missing = ParseReport("X-Only: retained\r\n", OriginalHeaders());
        Assert.That(missing.GetIssues().ContainsCode(EmailIssueCode.FeedbackReportRequiredFieldMissing), Is.True);
    }

    [Test]
    public void SupportsHistoricReceivedDateAndNullReversePath()
    {
        EmailFeedbackReportParseResult result = ParseReport(
            RequiredFields("abuse")
            + "Received-Date: Tue, 8 Mar 2005 14:00:00 -0400\r\n"
            + "Original-Mail-From: <>\r\n",
            OriginalHeaders());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetFeedbackReport()!.UsesHistoricReceivedDate(), Is.True);
        Assert.That(result.GetFeedbackReport()!.HasArrivalDate(), Is.True);
        Assert.That(result.GetFeedbackReport()!.GetOriginalMailFrom()!.IsNullPath(), Is.True);
        Assert.That(result.GetFeedbackReport()!.HasIncidents(), Is.False);
        Assert.That(result.GetFeedbackReport()!.GetIncidents(), Is.EqualTo(1));
    }

    [Test]
    public void ProjectsReportedOriginalHeadersAndFullMessage()
    {
        EmailFeedbackReport headers = ParseReport(RequiredFields("abuse"), OriginalHeaders()).GetFeedbackReport()!;
        Assert.That(headers!.HasOriginalHeaders(), Is.True);
        Assert.That(headers.GetOriginalHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("Reported subject"));

        EmailReportOptions options = ReportOptions();
        options.SetOriginalContent(EmailReportOriginalContent.FullMessage);
        EmailMessage generated = new EmailFeedbackReportWriter().CreateReportWithReportAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(headers, OriginalMessage(), Address("abuse@example.test"), Address("operator@example.test"), "Abuse report", options).Build();
        EmailFeedbackReport full = new EmailFeedbackReportParser().ParseWithMessage(generated).GetFeedbackReport()!;
        Assert.That(full!.HasOriginalMessage(), Is.True);
        Assert.That(full.GetOriginalMessage()!.GetHeaders()!.Contains("Bcc"), Is.False);
        Assert.That(full.GetOriginalMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("Reported subject"));
    }

    [Test]
    public void WriterPreservesExtensionsAndRoundTripsThroughReportBuilder()
    {
        EmailFeedbackReport value = ParseReport(
            "Feedback-Type: abuse\nUser-Agent: Example/1\nVersion: 1\n"
            + "Source-IP: 192.0.2.1\nX-Trace: one\n",
            OriginalHeaders()).GetFeedbackReport()!;

        byte[] written = new EmailFeedbackReportWriter().WriteWithReport(value!);
        Assert.That(Encoding.ASCII.GetString(written), Is.EqualTo(
            "Feedback-Type: abuse\r\nUser-Agent: Example/1\r\nVersion: 1\r\n"
            + "Source-IP: 192.0.2.1\r\nX-Trace: one\r\n"));

        EmailMessage message = new EmailFeedbackReportWriter().CreateReportWithReportAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(value, OriginalMessage(), Address("abuse@example.test"), Address("operator@example.test"), "Abuse report", ReportOptions()).Build();
        EmailFeedbackReportParseResult reparsed = new EmailFeedbackReportParser().ParseWithMessage(message);
        Assert.That(reparsed.IsSuccess(), Is.True);
        Assert.That(reparsed.GetFeedbackReport()!.GetSourceIp(), Is.EqualTo(IPAddress.Parse("192.0.2.1")));
        Assert.That(reparsed.GetFeedbackReport()!.GetExtensionFields().Single().GetDecodedValue(), Is.EqualTo("one"));
        Assert.That(message.GetBody()!.GetChildAt(1).GetMediaType()!.IsType("message", "feedback-report"), Is.True);
    }

    [Test]
    public void WriterParserAndOriginalContentInputsAreBoundedAndValidated()
    {
        EmailFeedbackReport value = ParseReport(RequiredFields("abuse"), OriginalHeaders()).GetFeedbackReport()!;
        Assert.Throws<ArgumentNullException>(() => new EmailFeedbackReportParser().ParseWithMessage((EmailMessage)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailFeedbackReportParser().ParseWithRoot((MimeEntity)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailFeedbackReportWriter().WriteWithReport(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailFeedbackReportWriter().WriteWithReportAndMaximumBytes(value!, 0));
        EmailLimitException limit = Assert.Throws<EmailLimitException>(() => new EmailFeedbackReportWriter().WriteWithReportAndMaximumBytes(value!, 10));
        Assert.That(limit.GetLimitName(), Is.EqualTo("MaxReportBytes"));

        EmailReportOptions none = ReportOptions();
        none.SetOriginalContent(EmailReportOriginalContent.None);
        Assert.Throws<InvalidOperationException>(() => new EmailFeedbackReportWriter().CreateReportWithReportAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(value!, OriginalMessage(), Address("abuse@example.test"), Address("operator@example.test"), "Abuse", none));
        Assert.Throws<ArgumentException>(() => new EmailFeedbackTypedValue("bad type", "value"));
        Assert.Throws<ArgumentException>(() => new EmailFeedbackPath(false, null));
    }

    static string RequiredFields(string feedbackType)
    {
        return "Feedback-Type: " + feedbackType + "\r\nUser-Agent: ExampleReporter/1.0\r\nVersion: 1\r\n";
    }

    static string OriginalHeaders()
    {
        return "From: sender@example.test\r\nTo: recipient@example.test\r\nSubject: Reported subject\r\n";
    }

    static EmailFeedbackReportParseResult ParseReport(string fields, string originalHeaders)
    {
        byte[] prefix = Encoding.ASCII.GetBytes(
            "Content-Type: multipart/report; boundary=r; report-type=feedback-report\r\n\r\n"
            + "--r\r\nContent-Type: text/plain\r\n\r\nhuman\r\n"
            + "--r\r\nContent-Type: message/feedback-report\r\n\r\n");
        string suffix = originalHeaders == null
            ? "--r--\r\n"
            : "--r\r\nContent-Type: text/rfc822-headers\r\n\r\n" + originalHeaders + "\r\n--r--\r\n";
        byte[] source = prefix.Concat(Encoding.UTF8.GetBytes(fields)).Concat(Encoding.ASCII.GetBytes(suffix)).ToArray();
        EmailEntityParseResult parsed = new EmailParser().ParseEntityWithInputUsingValueArray(source);
        Assert.That(parsed.HasEntity(), Is.True);
        return new EmailFeedbackReportParser().ParseWithRoot(parsed.GetEntity()!);
    }

    static EmailMessage OriginalMessage()
    {
        string source = "From: sender@example.test\r\nTo: recipient@example.test\r\nBcc: hidden@example.test\r\n"
            + "Subject: Reported subject\r\nContent-Type: text/plain\r\n\r\nreported body";
        EmailParseResult parsed = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.ASCII.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(parsed.HasMessage(), Is.True);
        return parsed.GetMessage()!;
    }

    static EmailReportOptions ReportOptions()
    {
        EmailComposerOptions composer = new EmailComposerOptions();
        composer.SetRequireDate(false);
        composer.SetRequireMessageId(false);
        EmailReportOptions options = new EmailReportOptions();
        options.SetComposerOptions(composer);
        return options;
    }

    static EmailAddress Address(string value)
    {
        return new EmailAddressParser().ParseMailbox(Encoding.ASCII.GetBytes(value), EmailAddressParseMode.Strict).GetMailbox()!;
    }
}
