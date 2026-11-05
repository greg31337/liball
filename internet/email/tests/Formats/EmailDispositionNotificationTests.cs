using System.Text;
using email;

namespace email.Tests;

public class EmailDispositionNotificationTests
{
    [Test]
    public void ParsesTypedDispositionFieldsLegacyDetailsAndExtensions()
    {
        EmailDispositionNotificationParseResult result = ParseReport(
            "message/disposition-notification",
            "Reporting-UA: Example Mail; 4.2\r\n"
            + "MDN-Gateway: dns; gateway.example.test\r\n"
            + "Original-Recipient: rfc822; old@example.test\r\n"
            + "Final-Recipient: rfc822; reader@example.test\r\n"
            + "Original-Message-ID: <original@example.test>\r\n"
            + "Disposition: automatic-action/MDN-sent-automatically; processed/error, archived\r\n"
            + "Failure: legacy failure detail\r\n"
            + "Error: processing detail\r\n"
            + "Error: second detail\r\n"
            + "Warning: legacy warning detail\r\n"
            + "X-Trace: retained\r\n");

        EmailDispositionNotification value = result.GetNotification()!;
        EmailDispositionNotificationDisposition disposition = value!.GetDisposition()!;

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(value.GetReportingUserAgent()!.GetName(), Is.EqualTo("Example Mail"));
        Assert.That(value.GetReportingUserAgent()!.GetProduct(), Is.EqualTo("4.2"));
        Assert.That(value.GetGateway()!.GetTypeName(), Is.EqualTo("dns"));
        Assert.That(value.GetFinalRecipient()!.GetValue(), Is.EqualTo("reader@example.test"));
        Assert.That(value.GetOriginalMessageId()!.GetIdLeft(), Is.EqualTo("original"));
        Assert.That(disposition!.GetActionMode(), Is.EqualTo(EmailDispositionActionMode.Automatic));
        Assert.That(disposition.GetSendingMode(), Is.EqualTo(EmailDispositionSendingMode.Automatic));
        Assert.That(disposition.GetDispositionType(), Is.EqualTo(EmailDispositionNotificationType.Processed));
        Assert.That(disposition.HasModifier("error"), Is.True);
        Assert.That(disposition.HasModifier("ARCHIVED"), Is.True);
        Assert.That(value.GetFailureValues().Single(), Is.EqualTo("legacy failure detail"));
        Assert.That(value.GetErrorValues(), Is.EqualTo(new[] { "processing detail", "second detail" }));
        Assert.That(value.GetWarningValues().Single(), Is.EqualTo("legacy warning detail"));
        Assert.That(value.GetExtensionFields().Single().GetDecodedValue(), Is.EqualTo("retained"));
        disposition.GetModifiers().Clear();
        Assert.That(disposition.GetModifierCount(), Is.EqualTo(2));
    }

    [Test]
    public void DiagnosesMissingMalformedDuplicatedAndSeparatedFields()
    {
        EmailDispositionNotificationParseResult result = ParseReport(
            "message/disposition-notification",
            "Reporting-UA: ; invalid\r\n"
            + "Final-Recipient: broken\r\n"
            + "Final-Recipient: rfc822; duplicate@example.test\r\n"
            + "Disposition: automatic-action/MDN-sent-manually displayed\r\n\r\n"
            + "X-Late: retained\r\n");

        Assert.That(result.HasNotification(), Is.True);
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DispositionNotificationMalformed), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DispositionNotificationValueInvalid), Is.True);
        Assert.Throws<InvalidOperationException>(() => new EmailDispositionNotificationWriter().WriteWithNotification(result.GetNotification()!));

        EmailDispositionNotificationParseResult missing = ParseReport("message/disposition-notification", "Reporting-UA: example\r\n");
        Assert.That(missing.GetIssues().ContainsCode(EmailIssueCode.DispositionNotificationRequiredFieldMissing), Is.True);
    }

    [Test]
    public void SupportsGlobalUtf8AndDiagnosesEncodingMismatches()
    {
        string fields = "Final-Recipient: utf-8; 用户@例子.test\r\n"
            + "Disposition: manual-action/MDN-sent-manually; failed/error\r\n"
            + "Failure: 无法显示邮件\r\n"
            + "Error: 收件人拒绝\r\n";
        EmailDispositionNotificationParseResult global = ParseReport("message/global-disposition-notification", fields);
        EmailDispositionNotificationParseResult regular = ParseReport("message/disposition-notification", fields);

        Assert.That(global.IsSuccess(), Is.True);
        Assert.That(global.GetNotification()!.IsInternationalized(), Is.True);
        Assert.That(global.GetNotification()!.GetFinalRecipient()!.GetValue(), Is.EqualTo("用户@例子.test"));
        Assert.That(global.GetNotification()!.GetFailureValues().Single(), Is.EqualTo("无法显示邮件"));
        Assert.That(regular.GetIssues().ContainsCode(EmailIssueCode.DispositionNotificationInternationalizationMismatch), Is.True);

        byte[] invalid = Encoding.ASCII.GetBytes("Final-Recipient: utf-8; user@").Concat(new byte[] { 0xff }).Concat(Encoding.ASCII.GetBytes(".test\r\nDisposition: manual-action/MDN-sent-manually; displayed\r\n")).ToArray();
        EmailDispositionNotificationParseResult malformed = ParseReportBytes("message/global-disposition-notification", invalid, null!);
        Assert.That(malformed.GetIssues().ContainsCode(EmailIssueCode.DispositionNotificationInternationalizationMismatch), Is.True);
    }

    [Test]
    public void ProjectsOriginalHeadersAndValidatesOriginalMessageIdentifier()
    {
        string fields = "Final-Recipient: rfc822; reader@example.test\r\n"
            + "Original-Message-ID: <original@example.test>\r\n"
            + "Disposition: manual-action/MDN-sent-manually; displayed\r\n";
        string originalHeaders = "From: author@example.test\r\nMessage-ID: <original@example.test>\r\nSubject: Original\r\n";
        EmailDispositionNotificationParseResult result = ParseReportBytes("message/disposition-notification", Encoding.ASCII.GetBytes(fields), originalHeaders);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetNotification()!.HasOriginalHeaders(), Is.True);
        Assert.That(result.GetNotification()!.GetOriginalHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("Original"));
        result.GetNotification()!.GetOriginalHeaders()!.Clear();
        Assert.That(result.GetNotification()!.GetOriginalHeaders()!.Contains("Subject"), Is.True);

        EmailDispositionNotificationParseResult mismatch = ParseReportBytes(
            "message/disposition-notification",
            Encoding.ASCII.GetBytes(fields.Replace("original@example.test", "different@example.test")),
            originalHeaders);
        Assert.That(mismatch.GetIssues().ContainsCode(EmailIssueCode.DispositionNotificationValueInvalid), Is.True);
    }

    [Test]
    public void WriterPreservesExtensionsAndRoundTripsWithOriginalMessage()
    {
        EmailDispositionNotification value = ParseReport(
            "message/disposition-notification",
            "Final-Recipient: rfc822; reader@example.test\n"
            + "Original-Message-ID: <original@example.test>\n"
            + "Disposition: automatic-action/MDN-sent-automatically; displayed\n"
            + "X-Trace: one\n").GetNotification()!;

        byte[] written = new EmailDispositionNotificationWriter().WriteWithNotification(value!);
        Assert.That(Encoding.ASCII.GetString(written), Is.EqualTo(
            "Final-Recipient: rfc822; reader@example.test\r\n"
            + "Original-Message-ID: <original@example.test>\r\n"
            + "Disposition: automatic-action/MDN-sent-automatically; displayed\r\n"
            + "X-Trace: one\r\n"));

        EmailReportOptions options = ReportOptions(false);
        options.SetOriginalContent(EmailReportOriginalContent.FullMessage);
        EmailMessage message = new EmailDispositionNotificationWriter().CreateReportWithNotificationAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(value, OriginalMessage(false), Address("reader@example.test"), Address("author@example.test"), "Displayed", options).Build();
        EmailDispositionNotificationParseResult reparsed = new EmailDispositionNotificationParser().ParseWithMessage(message);
        Assert.That(reparsed.IsSuccess(), Is.True);
        Assert.That(reparsed.GetNotification()!.HasOriginalMessage(), Is.True);
        Assert.That(reparsed.GetNotification()!.GetExtensionFields().Single().GetDecodedValue(), Is.EqualTo("one"));
    }

    [Test]
    public void InternationalWriterSelectsGlobalMachineAndOriginalParts()
    {
        EmailDispositionNotification value = ParseReport(
            "message/global-disposition-notification",
            "Final-Recipient: utf-8; 用户@例子.test\r\n"
            + "Original-Message-ID: <original@example.test>\r\n"
            + "Disposition: automatic-action/MDN-sent-automatically; displayed\r\n"
            + "Warning: 已显示\r\n").GetNotification()!;
        Assert.Throws<InvalidOperationException>(() => new EmailDispositionNotificationWriter().CreateReportWithNotificationAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(value!, OriginalMessage(true), Address("reader@example.test"), Address("author@example.test"), "Displayed", ReportOptions(false)));

        EmailMessage message = new EmailDispositionNotificationWriter().CreateReportWithNotificationAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(value!, OriginalMessage(true), Address("reader@example.test"), Address("author@example.test"), "Displayed", ReportOptions(true)).Build();
        MimeEntity root = message.GetBody()!;
        Assert.That(root!.GetChildAt(1).GetMediaType()!.IsType("message", "global-disposition-notification"), Is.True);
        Assert.That(root.GetChildAt(1).GetTransferEncodingName(), Is.EqualTo("8bit"));
        Assert.That(root.GetChildAt(2).GetMediaType()!.IsType("message", "global-headers"), Is.True);
        Assert.That(new EmailDispositionNotificationParser().ParseWithMessage(message).IsSuccess(), Is.True);
    }

    [Test]
    public void ParsesAndWritesRequestedReceiptFieldsAndOptions()
    {
        EmailMessage message = ParseMessage(
            "From: author@example.test\r\n"
            + "To: reader@example.test\r\n"
            + "Disposition-Notification-To: Receipts <receipts@example.test>, audit@example.test\r\n"
            + "Original-Recipient: rfc822; reader@example.test\r\n"
            + "Disposition-Notification-Options: x-confirm=required,yes; x-note=optional,\"two words\"\r\n\r\nbody");
        EmailDispositionNotificationRequestParseResult parsed = new EmailDispositionNotificationParser().ParseRequestWithMessage(message);
        EmailDispositionNotificationRequest request = parsed.GetRequest()!;

        Assert.That(parsed.IsSuccess(), Is.True);
        Assert.That(request!.GetNotificationTo().GetMailboxCount(), Is.EqualTo(2));
        Assert.That(request.GetOriginalRecipient()!.GetValue(), Is.EqualTo("reader@example.test"));
        Assert.That(request.GetOptionAt(0).GetImportance(), Is.EqualTo(EmailDispositionNotificationOptionImportance.Required));
        Assert.That(request.GetOptionAt(1).GetValueAt(0), Is.EqualTo("two words"));
        request.GetOptions().Clear();
        Assert.That(request.GetOptionCount(), Is.EqualTo(2));

        EmailMessage target = ParseMessage("From: author@example.test\r\nTo: reader@example.test\r\n\r\nbody");
        new EmailDispositionNotificationWriter().ApplyRequest(target, request);
        EmailDispositionNotificationRequestParseResult roundTrip = new EmailDispositionNotificationParser().ParseRequestWithMessage(target);
        Assert.That(roundTrip.IsSuccess(), Is.True);
        Assert.That(roundTrip.GetRequest()!.GetNotificationTo().GetMailboxCount(), Is.EqualTo(2));
        Assert.That(roundTrip.GetRequest()!.GetOptionAt(1).GetValueAt(0), Is.EqualTo("two words"));
    }

    [Test]
    public void RequestAndWriterInputsAndLimitsAreValidated()
    {
        EmailMessage malformed = ParseMessage(
            "Disposition-Notification-To: team: a@example.test;\r\n"
            + "Disposition-Notification-Options: x-test=maybe,value\r\n\r\nbody");
        EmailDispositionNotificationRequestParseResult request = new EmailDispositionNotificationParser().ParseRequestWithMessage(malformed);
        Assert.That(request.HasRequest(), Is.False);
        Assert.That(request.GetIssues().ContainsCode(EmailIssueCode.DispositionNotificationRequestMalformed), Is.True);

        EmailDispositionNotification value = ParseReport("message/disposition-notification", ValidFields()).GetNotification()!;
        Assert.Throws<ArgumentNullException>(() => new EmailDispositionNotificationParser().ParseWithMessage((EmailMessage)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailDispositionNotificationParser().ParseWithRoot((MimeEntity)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailDispositionNotificationParser().ParseRequestWithMessage(null!));
        Assert.Throws<ArgumentNullException>(() => new EmailDispositionNotificationWriter().WriteWithNotification(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailDispositionNotificationWriter().WriteWithNotificationAndMaximumBytes(value!, 0));
        EmailLimitException limit = Assert.Throws<EmailLimitException>(() => new EmailDispositionNotificationWriter().WriteWithNotificationAndMaximumBytes(value!, 10));
        Assert.That(limit.GetLimitName(), Is.EqualTo("MaxReportBytes"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailDispositionNotificationDisposition(EmailDispositionActionMode.Unknown, EmailDispositionSendingMode.Manual, EmailDispositionNotificationType.Displayed, new List<string>()));
    }

    static string ValidFields()
    {
        return "Final-Recipient: rfc822; reader@example.test\r\nDisposition: manual-action/MDN-sent-manually; displayed\r\n";
    }

    static EmailDispositionNotificationParseResult ParseReport(string mediaType, string fields)
    {
        return ParseReportBytes(mediaType, Encoding.UTF8.GetBytes(fields), null!);
    }

    static EmailDispositionNotificationParseResult ParseReportBytes(string mediaType, byte[] fields, string originalHeaders)
    {
        byte[] prefix = Encoding.ASCII.GetBytes(
            "Content-Type: multipart/report; boundary=r; report-type=disposition-notification\r\n\r\n"
            + "--r\r\nContent-Type: text/plain\r\n\r\nhuman\r\n"
            + "--r\r\nContent-Type: " + mediaType + "\r\n\r\n");
        string suffixText = originalHeaders == null
            ? "--r--\r\n"
            : "--r\r\nContent-Type: text/rfc822-headers\r\n\r\n" + originalHeaders + "\r\n--r--\r\n";
        MimeEntity entity = ParseEntity(prefix.Concat(fields).Concat(Encoding.ASCII.GetBytes(suffixText)).ToArray());
        return new EmailDispositionNotificationParser().ParseWithRoot(entity);
    }

    static MimeEntity ParseEntity(byte[] source)
    {
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(source);
        Assert.That(result.HasEntity(), Is.True);
        return result.GetEntity()!;
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.UTF8.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.HasMessage(), Is.True);
        return result.GetMessage()!;
    }

    static EmailMessage OriginalMessage(bool internationalized)
    {
        string subject = internationalized ? "原始主题" : "Original";
        return ParseMessage(
            "From: author@example.test\r\nTo: reader@example.test\r\nBcc: hidden@example.test\r\n"
            + "Message-ID: <original@example.test>\r\nSubject: " + subject + "\r\n"
            + "Content-Type: text/plain; charset=utf-8\r\n\r\nbody");
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
