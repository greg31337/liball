using System.Text;
using email;

namespace email.Tests;

public class EmailDeliveryStatusTests
{
    [Test]
    public void ParsesPerMessageAndRecipientFieldsWithTypedValuesAndExtensions()
    {
        EmailDeliveryStatusParseResult result = ParseReport(
            "message/delivery-status",
            "Reporting-MTA: dns; mx.example.test\r\n"
            + "Received-From-MTA: dns; relay.example.test\r\n"
            + "Original-Envelope-Id: envelope-7\r\n"
            + "Arrival-Date: Fri, 21 Nov 1997 09:55:06 -0600\r\n"
            + "X-Message-Trace: trace-1\r\n\r\n"
            + "Original-Recipient: rfc822; old@example.test\r\n"
            + "Final-Recipient: rfc822; missing@example.test\r\n"
            + "Action: failed\r\n"
            + "Status: 5.1.1\r\n"
            + "Remote-MTA: dns; destination.example.test\r\n"
            + "Diagnostic-Code: smtp; 550 mailbox unavailable\r\n"
            + "X-Recipient-Trace: trace-2\r\n\r\n"
            + "Final-Recipient: rfc822; waiting@example.test\r\n"
            + "Action: delayed\r\n"
            + "Status: 4.2.0\r\n");

        EmailDeliveryStatus value = result.GetDeliveryStatus()!;
        EmailDeliveryStatusRecipient failed = value!.GetRecipientAt(0);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(value.IsInternationalized(), Is.False);
        Assert.That(value.GetMessageFields()!.GetReportingMta()!.GetTypeName(), Is.EqualTo("dns"));
        Assert.That(value.GetMessageFields()!.GetReportingMta()!.GetValue(), Is.EqualTo("mx.example.test"));
        Assert.That(value.GetMessageFields()!.GetOriginalEnvelopeId(), Is.EqualTo("envelope-7"));
        Assert.That(value.GetMessageFields()!.HasArrivalDate(), Is.True);
        Assert.That(value.GetMessageFields()!.GetExtensionFields().Single().GetName(), Is.EqualTo("X-Message-Trace"));
        Assert.That(value.GetRecipientCount(), Is.EqualTo(2));
        Assert.That(failed.GetFinalRecipient()!.GetValue(), Is.EqualTo("missing@example.test"));
        Assert.That(failed.GetAction(), Is.EqualTo(EmailDeliveryStatusAction.Failed));
        Assert.That(failed.GetStatus()!.GetText(), Is.EqualTo("5.1.1"));
        Assert.That(failed.GetDiagnosticCode()!.GetTypeName(), Is.EqualTo("smtp"));
        Assert.That(failed.GetDiagnosticCode()!.GetValue(), Is.EqualTo("550 mailbox unavailable"));
        Assert.That(failed.GetExtensionFields().Single().GetDecodedValue(), Is.EqualTo("trace-2"));
        Assert.That(value.GetRecipientAt(1).GetAction(), Is.EqualTo(EmailDeliveryStatusAction.Delayed));
        value.GetRecipients().Clear();
        Assert.That(value.GetRecipientCount(), Is.EqualTo(2));
    }

    [Test]
    public void ReportsRequiredActionStatusAndStatusClassFailuresWithoutDiscardingBlocks()
    {
        EmailDeliveryStatusParseResult result = ParseReport(
            "message/delivery-status",
            "X-Only: retained\r\n\r\n"
            + "Action: failed\r\nStatus: 2.0.0\r\n\r\n"
            + "Final-Recipient: rfc822; user@example.test\r\nAction: mystery\r\nStatus: 5.+1.1\r\n");

        Assert.That(result.HasDeliveryStatus(), Is.True);
        Assert.That(result.GetDeliveryStatus()!.GetRecipientCount(), Is.EqualTo(2));
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DeliveryStatusRequiredFieldMissing), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DeliveryStatusStatusMismatch), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DeliveryStatusActionInvalid), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DeliveryStatusCodeInvalid), Is.True);
        Assert.Throws<InvalidOperationException>(() => new EmailDeliveryStatusWriter().WriteWithDeliveryStatus(result.GetDeliveryStatus()!));
    }

    [Test]
    public void ParsesInternationalizedFieldsAndRejectsWrongOrMalformedEncodings()
    {
        string fields = "Reporting-MTA: dns; mx.example.test\r\n\r\n"
            + "Final-Recipient: utf-8; 用户@例子.test\r\n"
            + "Action: failed\r\nStatus: 5.1.1\r\n"
            + "Diagnostic-Code: smtp; 550 收件箱不存在\r\n";
        EmailDeliveryStatusParseResult global = ParseReport("message/global-delivery-status", fields);
        EmailDeliveryStatusParseResult regular = ParseReport("message/delivery-status", fields);

        Assert.That(global.IsSuccess(), Is.True);
        Assert.That(global.GetDeliveryStatus()!.IsInternationalized(), Is.True);
        Assert.That(global.GetDeliveryStatus()!.GetRecipientAt(0).GetFinalRecipient()!.GetValue(), Is.EqualTo("用户@例子.test"));
        Assert.That(global.GetDeliveryStatus()!.GetRecipientAt(0).GetDiagnosticCode()!.GetValue(), Is.EqualTo("550 收件箱不存在"));
        Assert.That(regular.GetIssues().ContainsCode(EmailIssueCode.DeliveryStatusInternationalizationMismatch), Is.True);

        byte[] invalid = Encoding.ASCII.GetBytes("Reporting-MTA: dns; mx.example\r\n\r\nFinal-Recipient: utf-8; user@");
        invalid = invalid.Concat(new byte[] { 0xff }).Concat(Encoding.ASCII.GetBytes(".test\r\nAction: failed\r\nStatus: 5.1.1\r\n")).ToArray();
        EmailDeliveryStatusParseResult malformed = ParseReportBytes("message/global-delivery-status", invalid);
        Assert.That(malformed.GetIssues().ContainsCode(EmailIssueCode.DeliveryStatusInternationalizationMismatch), Is.True);
    }

    [Test]
    public void ProjectsReturnedOriginalHeadersAndFullMessages()
    {
        MimeEntity headersReport = ParseEntity(Encoding.UTF8.GetBytes(
            "Content-Type: multipart/report; boundary=r; report-type=delivery-status\r\n\r\n"
            + "--r\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nhuman\r\n"
            + "--r\r\nContent-Type: message/global-delivery-status\r\n\r\n"
            + ValidFields("utf-8; 用户@例子.test")
            + "--r\r\nContent-Type: message/global-headers\r\nContent-Transfer-Encoding: 8bit\r\n\r\n"
            + "From: sender@example.test\r\nSubject: 原始主题\r\n\r\n"
            + "--r--\r\n"));
        EmailDeliveryStatus headers = new EmailDeliveryStatusParser().ParseWithRoot(headersReport).GetDeliveryStatus()!;

        Assert.That(headers!.HasOriginalHeaders(), Is.True);
        Assert.That(headers.GetOriginalHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("原始主题"));
        headers.GetOriginalHeaders()!.Clear();
        Assert.That(headers.GetOriginalHeaders()!.Contains("Subject"), Is.True);

        EmailReportOptions options = ReportOptions(false);
        options.SetOriginalContent(EmailReportOriginalContent.FullMessage);
        EmailDeliveryStatus source = ParseReport("message/delivery-status", ValidFields("rfc822; user@example.test")).GetDeliveryStatus()!;
        EmailMessage generated = new EmailDeliveryStatusWriter().CreateReportWithDeliveryStatusAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(source!, OriginalMessage(false), Address("daemon@example.test"), Address("author@example.test"), "failed", options).Build();
        EmailDeliveryStatus full = new EmailDeliveryStatusParser().ParseWithMessage(generated).GetDeliveryStatus()!;
        Assert.That(full!.HasOriginalMessage(), Is.True);
        Assert.That(full.GetOriginalMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("Original subject"));
        Assert.That(full.GetOriginalMessage()!.GetHeaders()!.Contains("Bcc"), Is.False);
    }

    [Test]
    public void WriterPreservesExtensionsAndRoundTripsThroughMultipartReport()
    {
        EmailDeliveryStatus value = ParseReport(
            "message/delivery-status",
            "Reporting-MTA: dns; mx.example.test\nX-Message: one\n\n"
            + "Final-Recipient: rfc822; user@example.test\nAction: delivered\nStatus: 2.0.0\nX-Recipient: two\n").GetDeliveryStatus()!;

        byte[] written = new EmailDeliveryStatusWriter().WriteWithDeliveryStatus(value!);
        Assert.That(Encoding.ASCII.GetString(written), Is.EqualTo(
            "Reporting-MTA: dns; mx.example.test\r\nX-Message: one\r\n\r\n"
            + "Final-Recipient: rfc822; user@example.test\r\nAction: delivered\r\nStatus: 2.0.0\r\nX-Recipient: two\r\n"));

        EmailMessage message = new EmailDeliveryStatusWriter().CreateReportWithDeliveryStatusAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(value, OriginalMessage(false), Address("daemon@example.test"), Address("author@example.test"), "delivered", ReportOptions(false)).Build();
        EmailDeliveryStatusParseResult reparsed = new EmailDeliveryStatusParser().ParseWithMessage(message);
        Assert.That(reparsed.IsSuccess(), Is.True);
        Assert.That(reparsed.GetDeliveryStatus()!.GetMessageFields()!.GetExtensionFields().Single().GetDecodedValue(), Is.EqualTo("one"));
        Assert.That(reparsed.GetDeliveryStatus()!.GetRecipientAt(0).GetExtensionFields().Single().GetDecodedValue(), Is.EqualTo("two"));
    }

    [Test]
    public void InternationalWriterSelectsGlobalPartsAndRequiresSmtpUtf8()
    {
        EmailDeliveryStatus value = ParseReport(
            "message/global-delivery-status",
            "Reporting-MTA: dns; mx.example.test\r\n\r\n"
            + "Final-Recipient: utf-8; 用户@例子.test\r\nAction: failed\r\nStatus: 5.1.1\r\n").GetDeliveryStatus()!;
        Assert.Throws<InvalidOperationException>(() => new EmailDeliveryStatusWriter().CreateReportWithDeliveryStatusAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(value!, OriginalMessage(true), Address("daemon@example.test"), Address("author@example.test"), "failed", ReportOptions(false)));

        EmailMessage message = new EmailDeliveryStatusWriter().CreateReportWithDeliveryStatusAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(value!, OriginalMessage(true), Address("daemon@example.test"), Address("author@example.test"), "failed", ReportOptions(true)).Build();
        MimeEntity root = message.GetBody()!;

        Assert.That(root!.GetChildAt(1).GetMediaType()!.IsType("message", "global-delivery-status"), Is.True);
        Assert.That(root.GetChildAt(1).GetTransferEncodingName(), Is.EqualTo("8bit"));
        Assert.That(root.GetChildAt(2).GetMediaType()!.IsType("message", "global-headers"), Is.True);
        Assert.That(Encoding.UTF8.GetString(root.GetChildAt(1).GetContent()!.Materialize(10000)), Does.Contain("用户@例子.test"));
        Assert.That(new EmailDeliveryStatusParser().ParseWithMessage(message).IsSuccess(), Is.True);

        EmailReportOptions fullOptions = ReportOptions(true);
        fullOptions.SetOriginalContent(EmailReportOriginalContent.FullMessage);
        EmailMessage fullMessage = new EmailDeliveryStatusWriter().CreateReportWithDeliveryStatusAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(value, OriginalMessage(true), Address("daemon@example.test"), Address("author@example.test"), "failed", fullOptions).Build();
        Assert.That(fullMessage.GetBody()!.GetChildAt(2).GetMediaType()!.IsType("message", "global"), Is.True);
        Assert.That(new EmailDeliveryStatusParser().ParseWithMessage(fullMessage).GetDeliveryStatus()!.HasOriginalMessage(), Is.True);
    }

    [Test]
    public void WriterAndParserValidatePublicInputsAndOutputLimits()
    {
        EmailDeliveryStatus value = ParseReport("message/delivery-status", ValidFields("rfc822; user@example.test")).GetDeliveryStatus()!;
        Assert.Throws<ArgumentNullException>(() => new EmailDeliveryStatusParser().ParseWithMessage((EmailMessage)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailDeliveryStatusParser().ParseWithRoot((MimeEntity)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailDeliveryStatusWriter().WriteWithDeliveryStatus(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailDeliveryStatusWriter().WriteWithDeliveryStatusAndMaximumBytes(value!, 0));
        EmailLimitException limit = Assert.Throws<EmailLimitException>(() => new EmailDeliveryStatusWriter().WriteWithDeliveryStatusAndMaximumBytes(value!, 10));
        Assert.That(limit.GetLimitName(), Is.EqualTo("MaxReportBytes"));
        Assert.Throws<ArgumentNullException>(() => new EmailDeliveryStatusOperations().CreateWithInternationalizedAndMessageFieldsAndRecipients(false, null!, new List<EmailDeliveryStatusRecipient>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailDeliveryStatusCode(3, 0, 0));
    }

    static string ValidFields(string recipient)
    {
        return "Reporting-MTA: dns; mx.example.test\r\n\r\n"
            + "Final-Recipient: " + recipient + "\r\n"
            + "Action: failed\r\nStatus: 5.1.1\r\n";
    }

    static EmailDeliveryStatusParseResult ParseReport(string mediaType, string fields)
    {
        return ParseReportBytes(mediaType, Encoding.UTF8.GetBytes(fields));
    }

    static EmailDeliveryStatusParseResult ParseReportBytes(string mediaType, byte[] fields)
    {
        byte[] prefix = Encoding.ASCII.GetBytes(
            "Content-Type: multipart/report; boundary=r; report-type=delivery-status\r\n\r\n"
            + "--r\r\nContent-Type: text/plain\r\n\r\nhuman\r\n"
            + "--r\r\nContent-Type: " + mediaType + "\r\n\r\n");
        byte[] suffix = Encoding.ASCII.GetBytes("--r--\r\n");
        return new EmailDeliveryStatusParser().ParseWithRoot(ParseEntity(prefix.Concat(fields).Concat(suffix).ToArray()));
    }

    static MimeEntity ParseEntity(byte[] source)
    {
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(source);
        Assert.That(result.HasEntity(), Is.True);
        return result.GetEntity()!;
    }

    static EmailMessage OriginalMessage(bool internationalized)
    {
        string subject = internationalized ? "原始主题" : "Original subject";
        byte[] source = Encoding.UTF8.GetBytes(
            "From: author@example.test\r\nTo: recipient@example.test\r\n"
            + "Bcc: hidden@example.test\r\nSubject: " + subject + "\r\n"
            + "Content-Type: text/plain; charset=utf-8\r\n\r\noriginal body");
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.HasMessage(), Is.True);
        return result.GetMessage()!;
    }

    static EmailReportOptions ReportOptions(bool smtpUtf8)
    {
        EmailComposerOptions composer = new EmailComposerOptions();
        composer.SetRequireDate(false);
        composer.SetRequireMessageId(false);
        composer.SetAllowSmtpUtf8(smtpUtf8);
        EmailReportOptions options = new EmailReportOptions();
        options.SetComposerOptions(composer);
        return options;
    }

    static EmailAddress Address(string value)
    {
        return new EmailAddressParser().ParseMailbox(Encoding.ASCII.GetBytes(value), EmailAddressParseMode.Strict).GetMailbox()!;
    }
}
