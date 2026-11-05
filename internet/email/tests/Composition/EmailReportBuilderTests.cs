using System.Text;
using email;

namespace email.Tests;

public class EmailReportBuilderTests
{
    [Test]
    public void DeliveryStatusUsesReportShapeAndSafeOriginalHeaders()
    {
        EmailMessage original = OriginalMessage();
        byte[] reportBytes = Encoding.ASCII.GetBytes("Reporting-MTA: dns; mx.example.test\n\nFinal-Recipient: rfc822; user@example.test\nAction: failed\nStatus: 5.1.1");

        EmailMessage report = new EmailReportBuilder().CreateDeliveryStatusWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(original, Address("mailer-daemon@example.test"), Address("author@example.test"), "Delivery failed", reportBytes, RelaxedOptions()).Build();
        MimeEntity root = report.GetBody()!;

        Assert.That(root!.GetMediaType()!.IsType("multipart", "report"), Is.True);
        Assert.That(Parameter(root, "report-type"), Is.EqualTo("delivery-status"));
        Assert.That(Parameter(root, "boundary"), Is.Not.Empty);
        Assert.That(root.GetChildCount(), Is.EqualTo(3));
        Assert.That(Text(root.GetChildAt(0)), Is.EqualTo("Delivery failed"));
        Assert.That(root.GetChildAt(1).GetKind(), Is.EqualTo(MimeEntityKind.DeliveryStatus));
        Assert.That(root.GetChildAt(1).GetMediaType()!.IsType("message", "delivery-status"), Is.True);
        Assert.That(Encoding.ASCII.GetString(Content(root.GetChildAt(1))), Is.EqualTo("Reporting-MTA: dns; mx.example.test\r\n\r\nFinal-Recipient: rfc822; user@example.test\r\nAction: failed\r\nStatus: 5.1.1\r\n"));
        Assert.That(root.GetChildAt(2).GetMediaType()!.IsType("text", "rfc822-headers"), Is.True);
        string originalHeaders = Encoding.ASCII.GetString(Content(root.GetChildAt(2)));
        Assert.That(originalHeaders, Does.Contain("Subject: Original subject"));
        Assert.That(originalHeaders, Does.Not.Contain("Bcc:"));
        Assert.That(originalHeaders, Does.Not.Contain("original body"));
        Assert.That(report.GetHeaders()!.GetFirst("Auto-Submitted")!.GetUnfoldedValue(), Is.EqualTo(Encoding.ASCII.GetBytes("auto-generated")));
        Assert.That(report.GetEnvelope()!.HasSender(), Is.False);
        Assert.That(report.GetEnvelope()!.GetRecipientAt(0).GetLocalPart(), Is.EqualTo("author"));
        Assert.That(original.GetHeaders()!.Contains("Bcc"), Is.True);
    }

    [Test]
    public void BounceWrapsAClonedFullMessageWithoutBlindRecipientDisclosure()
    {
        EmailMessage original = OriginalMessage();
        EmailContent originalContent = original.GetBody()!.GetContent()!;
        EmailMessage bounce = new EmailReportBuilder().CreateBounceWithOriginalAndReporterAndRecipientAndHumanReadableAndDeliveryStatus(
            original,
            Address("mailer-daemon@example.test"),
            Address("author@example.test"),
            "The message could not be delivered.",
            Encoding.ASCII.GetBytes("Reporting-MTA: dns; mx.example.test\r\n\r\nFinal-Recipient: rfc822; missing@example.test\r\nAction: failed\r\nStatus: 5.1.1\r\n")).Build();
        MimeEntity nestedPart = bounce.GetBody()!.GetChildAt(2);
        EmailMessage nested = nestedPart.GetNestedMessage()!;

        Assert.That(nestedPart.GetKind(), Is.EqualTo(MimeEntityKind.Message));
        Assert.That(nested!.GetHeaders()!.Contains("Bcc"), Is.False);
        Assert.That(nested.GetBody(), Is.Not.SameAs(original.GetBody()));
        Assert.That(nested.GetBody().GetContent(), Is.SameAs(originalContent));
        Assert.That(Encoding.ASCII.GetString(nested.GetBody().GetContent().Materialize(100)), Is.EqualTo("original body"));
        nested.GetHeaders()!.RemoveAll("Subject");
        Assert.That(original.GetHeaders()!.Contains("Subject"), Is.True);
        Assert.That(bounce.GetEnvelope()!.HasSender(), Is.False);

        string wire = Encoding.ASCII.GetString(new EmailWriter().ToArrayWithMessageAndOptions(bounce, new EmailWriteOptionsOperations().CreateCanonicalInternet()));
        Assert.That(wire, Does.Contain("Content-Type: multipart/report"));
        Assert.That(wire, Does.Contain("Content-Type: message/rfc822"));
        Assert.That(wire, Does.Contain("original body"));
        Assert.That(wire, Does.Not.Contain("hidden@example.test"));
    }

    [Test]
    public void NotificationAndFeedbackBuildersSelectTheirSpecializedKinds()
    {
        EmailReportOptions options = RelaxedOptions();
        options.SetOriginalContent(EmailReportOriginalContent.None);
        EmailMessage mdn = new EmailReportBuilder().CreateDispositionNotificationWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(OriginalMessage(), Address("reader@example.test"), Address("author@example.test"), "Displayed", Encoding.ASCII.GetBytes("Final-Recipient: rfc822; reader@example.test\r\nDisposition: manual-action/MDN-sent-manually; displayed\r\n"), options).Build();
        EmailMessage arf = new EmailReportBuilder().CreateFeedbackReportWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(OriginalMessage(), Address("abuse@example.test"), Address("author@example.test"), "Abuse report", Encoding.ASCII.GetBytes("Feedback-Type: abuse\r\nUser-Agent: example\r\nVersion: 1\r\n"), options).Build();

        Assert.That(Parameter(mdn.GetBody()!, "report-type"), Is.EqualTo("disposition-notification"));
        Assert.That(mdn.GetBody()!.GetChildAt(1).GetKind(), Is.EqualTo(MimeEntityKind.DispositionNotification));
        Assert.That(mdn.GetBody()!.GetChildCount(), Is.EqualTo(2));
        Assert.That(Parameter(arf.GetBody()!, "report-type"), Is.EqualTo("feedback-report"));
        Assert.That(arf.GetBody()!.GetChildAt(1).GetKind(), Is.EqualTo(MimeEntityKind.FeedbackReport));
        Assert.That(arf.GetBody()!.GetChildCount(), Is.EqualTo(2));
    }

    [Test]
    public void ReportInputsAreCopiedAndBounded()
    {
        EmailReportOptions options = RelaxedOptions();
        options.SetOriginalContent(EmailReportOriginalContent.None);
        byte[] bytes = Encoding.ASCII.GetBytes("Status: 5.0.0");
        EmailComposer composer = new EmailReportBuilder().CreateDeliveryStatusWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(OriginalMessage(), Address("daemon@example.test"), Address("author@example.test"), "failed", bytes, options);
        bytes[0] = (byte)'X';
        Assert.That(Encoding.ASCII.GetString(Content(composer.Build().GetBody()!.GetChildAt(1))), Is.EqualTo("Status: 5.0.0\r\n"));

        options.SetMaximumHumanReadableCharacters(3);
        Assert.Throws<EmailLimitException>(() => new EmailReportBuilder().CreateDeliveryStatusWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(OriginalMessage(), Address("daemon@example.test"), Address("author@example.test"), "four", new byte[0], options));
        options.SetMaximumHumanReadableCharacters(10);
        options.SetMaximumReportBytes(3);
        Assert.Throws<EmailLimitException>(() => new EmailReportBuilder().CreateDeliveryStatusWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(OriginalMessage(), Address("daemon@example.test"), Address("author@example.test"), "ok", Encoding.ASCII.GetBytes("four"), options));
        Assert.Throws<ArgumentException>(() => new EmailReportBuilder().CreateDeliveryStatusWithOriginalAndReporterAndRecipientAndHumanReadableAndReportAndOptions(OriginalMessage(), Address("daemon@example.test"), Address("author@example.test"), "ok", new byte[] { 255 }, RelaxedOptions()));
        Assert.Throws<ArgumentNullException>(() => new EmailReportBuilder().CreateDeliveryStatusWithOriginalAndReporterAndRecipientAndHumanReadableAndReport(null!, Address("daemon@example.test"), Address("author@example.test"), "ok", new byte[0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetOriginalContent((EmailReportOriginalContent)10));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumReportBytes(0));
    }

    static EmailMessage OriginalMessage()
    {
        return ParseMessage(
            "Return-Path: <bounce@example.test>\r\n"
            + "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\n"
            + "From: author@example.test\r\n"
            + "To: recipient@example.test\r\n"
            + "Bcc: hidden@example.test\r\n"
            + "Subject: Original subject\r\n"
            + "Content-Type: text/plain; charset=us-ascii\r\n\r\n"
            + "original body");
    }

    static EmailReportOptions RelaxedOptions()
    {
        EmailComposerOptions composerOptions = new EmailComposerOptions();
        composerOptions.SetRequireDate(false);
        composerOptions.SetRequireMessageId(false);
        EmailReportOptions options = new EmailReportOptions();
        options.SetComposerOptions(composerOptions);
        return options;
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.ASCII.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetMessage()!;
    }

    static EmailAddress Address(string value)
    {
        EmailAddressParseResult result = new EmailAddressParser().ParseMailbox(Encoding.ASCII.GetBytes(value), EmailAddressParseMode.Strict);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetMailbox()!;
    }

    static string? Parameter(MimeEntity entity, string name)
    {
        EmailRfc2231DecodedParameter parameter = entity.GetMediaType()!.GetParameters().GetDecoded(name)!;
        return parameter == null ? null : Encoding.ASCII.GetString(parameter.GetDecodedBytes())!;
    }

    static string Text(MimeEntity entity)
    {
        return new EmailTextProcessor().DecodeWithEntity(entity).GetText();
    }

    static byte[] Content(MimeEntity entity)
    {
        return entity.GetContent()!.Materialize(EmailReportOptionsConstants.DefaultMaximumReportBytes);
    }
}
