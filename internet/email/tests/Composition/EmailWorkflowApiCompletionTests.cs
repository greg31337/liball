using System.Text;
using email;

namespace email.Tests;

public class EmailWorkflowApiCompletionTests
{
    [Test]
    public void AttachmentSourceFactoriesAndClearOperationsPreserveSafeDefaults()
    {
        EmailAttachmentSourceOperations operations = new EmailAttachmentSourceOperations();
        EmailAttachmentSource source = operations.CreateMemoryWithValue(Encoding.ASCII.GetBytes("payload"));

        Assert.That(source.HasFilename(), Is.False);
        source.SetFilename("note.txt");
        Assert.That(source.HasFilename(), Is.True);
        source.ClearFilename();
        Assert.That(source.HasFilename(), Is.False);

        source.SetMediaType("text/plain");
        Assert.That(source.GetMediaType().IsType("text", "plain"), Is.True);
        source.ClearMediaType();
        Assert.That(source.GetMediaType().IsType("application", "octet-stream"), Is.True);

        EmailMessageId contentId = ContentId("<asset@example.test>");
        source.SetContentId(contentId);
        Assert.That(source.GetContentId(), Is.SameAs(contentId));
        source.ClearContentId();
        Assert.That(source.HasContentId(), Is.False);

        source.SetContentLocation("images/asset.bin");
        Assert.That(source.GetContentLocation(), Is.EqualTo("images/asset.bin"));
        source.ClearContentLocation();
        Assert.That(source.HasContentLocation(), Is.False);

        source.SetDescription("download payload");
        Assert.That(source.HasDescription(), Is.True);
        Assert.That(source.GetDescription(), Is.EqualTo("download payload"));
        source.ClearDescription();
        Assert.That(source.HasDescription(), Is.False);

        string path = Path.Combine(Path.GetTempPath(), "email-workflow-source-" + Guid.NewGuid().ToString("N") + ".bin");
        File.WriteAllBytes(path, Encoding.ASCII.GetBytes("file payload"));
        try
        {
            EmailAttachmentSource file = operations.CreateFileWithPathAndFilename(path, "renamed.txt");
            Assert.That(file.GetFilename(), Is.EqualTo("renamed.txt"));
            Assert.That(file.GetContent().GetKind(), Is.EqualTo(EmailContentKind.File));
            Assert.That(Encoding.ASCII.GetString(file.GetContent().Materialize(100)), Is.EqualTo("file payload"));
            file.GetContent().Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void ComposerClearOperationsRemoveHeadersRecipientsAndOptionalBodies()
    {
        EmailComposerOptions options = RelaxedComposerOptions();
        EmailComposer composer = new EmailComposer(options);
        options.SetMaximumAddressCount(1);
        Assert.That(composer.GetOptions().GetMaximumAddressCount(), Is.EqualTo(EmailComposerOptionsConstants.DefaultMaximumAddressCount));

        EmailAddress author = Address("author@example.test");
        EmailAddress recipient = Address("recipient@example.test");
        composer.SetSender(Address("sender@example.test"));
        composer.SetFrom(author);
        composer.AddReplyTo(Address("reply@example.test"));
        composer.AddTo(recipient);
        composer.AddCc(Address("copy@example.test"));
        composer.AddBcc(Address("hidden@example.test"));
        composer.SetEnvelopeSender(Address("bounce@example.test"));
        composer.AddEnvelopeRecipient(Address("envelope@example.test"));
        composer.SetDate(Date("Fri, 21 Nov 1997 09:55:06 -0600"));
        composer.SetMessageId(MessageId("<message@example.test>"));
        composer.SetInReplyTo(MessageId("<parent@example.test>"));
        composer.AddReference(MessageId("<root@example.test>"));
        composer.SetAutoSubmitted("auto-generated");
        composer.AddCustomField("X-Workflow", "temporary");
        composer.SetAmpHtml("<p>amp</p>");
        composer.SetWatchHtml("<p>watch</p>");
        composer.SetCalendar("BEGIN:VCALENDAR\r\nEND:VCALENDAR\r\n", "REQUEST");

        composer.ClearSender();
        composer.ClearFrom();
        composer.ClearReplyTo();
        composer.ClearTo();
        composer.ClearCc();
        composer.ClearBcc();
        composer.ClearEnvelopeSender();
        composer.ClearEnvelopeRecipients();
        composer.ClearDate();
        composer.ClearMessageId();
        composer.ClearInReplyTo();
        composer.ClearReferences();
        composer.ClearAutoSubmitted();
        composer.ClearCustomFields();
        composer.ClearAmpHtml();
        composer.ClearWatchHtml();
        composer.ClearCalendar();
        composer.SetText("remaining body");

        EmailMessage message = composer.Build();
        foreach (string name in new[] { "Sender", "From", "Reply-To", "To", "Cc", "Bcc", "Date", "Message-ID", "In-Reply-To", "References", "Auto-Submitted", "X-Workflow" })
        {
            Assert.That(message.GetHeaders()!.Contains(name), Is.False, name);
        }

        Assert.That(message.GetEnvelope()!.HasSender(), Is.False);
        Assert.That(message.GetEnvelope()!.GetRecipientCount(), Is.Zero);
        Assert.That(message.GetBody()!.GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(new EmailTextProcessor().DecodeWithEntity(message.GetBody()!).GetText(), Is.EqualTo("remaining body"));
    }

    [Test]
    public void EnvelopeInsertionRemovalAndResentClearingKeepRecipientOrderExplicit()
    {
        EmailAddress first = Address("first@example.test");
        EmailAddress middle = Address("middle@example.test");
        EmailAddress last = Address("last@example.test");
        EmailEnvelope envelope = new EmailEnvelope();
        envelope.AddRecipient(first);
        envelope.AddRecipient(last);
        envelope.InsertRecipient(1, middle);
        Assert.That(envelope.GetRecipients().Select(value => value.GetLocalPart()), Is.EqualTo(new[] { "first", "middle", "last" }));
        Assert.That(envelope.RemoveRecipientAt(1), Is.SameAs(middle));
        Assert.That(envelope.GetRecipients().Select(value => value.GetLocalPart()), Is.EqualTo(new[] { "first", "last" }));

        EmailResentOptions options = new EmailResentOptions();
        EmailResentBuilder resent = new EmailResentBuilder(ParseMessage("From: original@example.test\r\n\r\nbody"), options);
        options.SetMaximumAddressCount(1);
        Assert.That(resent.GetOptions().GetMaximumAddressCount(), Is.EqualTo(EmailResentOptionsConstants.DefaultMaximumAddressCount));

        resent.SetDate(Date("Fri, 21 Nov 1997 09:55:06 -0600"));
        resent.ClearDate();
        resent.SetDate(Date("Sat, 22 Nov 1997 10:00:00 -0600"));
        resent.SetFrom(Address("old@example.test"));
        resent.ClearFrom();
        resent.SetFrom(Address("resender@example.test"));
        resent.SetSender(Address("agent@example.test"));
        resent.ClearSender();
        resent.AddTo(Address("old-recipient@example.test"));
        resent.ClearTo();
        resent.AddTo(Address("recipient@example.test"));
        resent.AddCc(Address("copy@example.test"));
        resent.ClearCc();
        resent.AddBcc(Address("hidden@example.test"));
        resent.ClearBcc();
        resent.SetMessageId(MessageId("<resent@example.test>"));
        resent.ClearMessageId();
        resent.SetEnvelopeSender(Address("bounce@example.test"));
        resent.ClearEnvelopeSender();
        resent.AddEnvelopeRecipient(Address("explicit@example.test"));
        resent.ClearEnvelopeRecipients();

        EmailMessage message = resent.Build();
        Assert.That(message.GetHeaders()!.Contains("Resent-Sender"), Is.False);
        Assert.That(message.GetHeaders()!.Contains("Resent-Cc"), Is.False);
        Assert.That(message.GetHeaders()!.Contains("Resent-Bcc"), Is.False);
        Assert.That(message.GetHeaders()!.Contains("Resent-Message-ID"), Is.False);
        Assert.That(message.GetEnvelope()!.GetSender()!.GetLocalPart(), Is.EqualTo("resender"));
        Assert.That(message.GetEnvelope()!.GetRecipients().Select(value => value.GetLocalPart()), Is.EqualTo(new[] { "recipient" }));
    }

    [Test]
    public void ReplyAndForwardConvenienceOverloadsRetainRecipientAndBodyPolicies()
    {
        EmailMessage original = ParseMessage(
            "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\n"
            + "From: Alice <alice@example.test>\r\n"
            + "To: Me <me@example.test>, Bob <bob@example.test>\r\n"
            + "Cc: Carol <carol@example.test>\r\n"
            + "Message-ID: <parent@example.test>\r\n"
            + "Subject: Topic\r\n\r\n"
            + "original body");
        EmailAddress author = Address("me@example.test");
        EmailReplyBuilder replies = new EmailReplyBuilder();

        EmailMessage reply = replies.CreateReplyWithOriginalAndAuthor(original, author).Build();
        EmailMessage replyText = replies.CreateReplyWithOriginalAndAuthorAndReplyText(original, author, "new reply").Build();
        EmailMessage replyAll = replies.CreateReplyAllWithOriginalAndAuthor(original, author).Build();
        EmailMessage replyAllText = replies.CreateReplyAllWithOriginalAndAuthorAndReplyText(original, author, "new reply all").Build();
        Assert.That(HeaderAddressLocalParts(reply, "To"), Is.EqualTo(new[] { "alice" }));
        Assert.That(BodyText(replyText), Does.StartWith("new reply\r\n\r\n"));
        Assert.That(HeaderAddressLocalParts(replyAll, "To"), Is.EqualTo(new[] { "alice", "bob" }));
        Assert.That(HeaderAddressLocalParts(replyAll, "Cc"), Is.EqualTo(new[] { "carol" }));
        Assert.That(BodyText(replyAllText), Does.StartWith("new reply all\r\n\r\n"));

        EmailForwardBuilder forwards = new EmailForwardBuilder();
        EmailMessage inline = forwards.CreateInlineWithOriginalAndAuthor(original, author).Build();
        EmailMessage inlineText = forwards.CreateInlineWithOriginalAndAuthorAndForwardText(original, author, "forward note").Build();
        EmailMessage attached = forwards.CreateAttachedMessageWithOriginalAndAuthor(original, author).Build();
        EmailMessage attachedText = forwards.CreateAttachedMessageWithOriginalAndAuthorAndForwardText(original, author, "attached note").Build();
        Assert.That(BodyText(inline), Does.Contain("Forwarded message"));
        Assert.That(BodyText(inlineText), Does.StartWith("forward note"));
        Assert.That(attached.GetBody()!.GetChildAt(1).GetNestedMessage(), Is.Not.Null);
        Assert.That(new EmailTextProcessor().DecodeWithEntity(attachedText.GetBody()!.GetChildAt(0)).GetText(), Is.EqualTo("attached note"));
    }

    [Test]
    public void ReportBuilderConvenienceOperationsCreateTheRequestedReportShape()
    {
        EmailMessage original = ParseMessage("From: author@example.test\r\nSubject: original\r\n\r\nbody");
        EmailAddress reporter = Address("daemon@example.test");
        EmailAddress recipient = Address("author@example.test");
        EmailReportOptions options = new EmailReportOptions();
        options.SetComposerOptions(RelaxedComposerOptions());
        options.SetOriginalContent(EmailReportOriginalContent.None);
        EmailReportBuilder builder = new EmailReportBuilder();

        EmailMessage bounce = builder.CreateBounceWithOriginalAndReporterAndRecipientAndHumanReadableAndDeliveryStatusAndOptions(
            original, reporter, recipient, "failed", Encoding.ASCII.GetBytes("Status: 5.0.0\r\n"), options).Build();
        EmailMessage notification = builder.CreateDispositionNotificationWithOriginalAndReporterAndRecipientAndHumanReadableAndReport(
            original, reporter, recipient, "displayed", Encoding.ASCII.GetBytes("Disposition: manual-action/MDN-sent-manually; displayed\r\n")).Build();
        EmailMessage feedback = builder.CreateFeedbackReportWithOriginalAndReporterAndRecipientAndHumanReadableAndReport(
            original, reporter, recipient, "abuse", Encoding.ASCII.GetBytes("Feedback-Type: abuse\r\nVersion: 1\r\n")).Build();
        EmailMessage generic = builder.CreateReport(
            EmailReportKind.DeliveryStatus, original, reporter, recipient, "status", Encoding.ASCII.GetBytes("Status: 2.0.0\r\n"), options).Build();

        Assert.That(ReportType(bounce), Is.EqualTo("delivery-status"));
        Assert.That(bounce.GetBody()!.GetChildAt(1).GetKind(), Is.EqualTo(MimeEntityKind.DeliveryStatus));
        Assert.That(ReportType(notification), Is.EqualTo("disposition-notification"));
        Assert.That(notification.GetBody()!.GetChildAt(1).GetKind(), Is.EqualTo(MimeEntityKind.DispositionNotification));
        Assert.That(ReportType(feedback), Is.EqualTo("feedback-report"));
        Assert.That(feedback.GetBody()!.GetChildAt(1).GetKind(), Is.EqualTo(MimeEntityKind.FeedbackReport));
        Assert.That(ReportType(generic), Is.EqualTo("delivery-status"));
    }

    [Test]
    public void CalendarBuilderConveniencesRetainParticipantsDueDatesAndExtensions()
    {
        EmailCalendarBuilder builder = new EmailCalendarBuilder("-//Example//Workflow//EN", "VTODO");
        builder.SetMethod("REQUEST");
        builder.ClearMethod();
        builder.SetUid("todo@example.test");
        builder.SetOrganizer("mailto:organizer@example.test");
        builder.AddAttendee("mailto:attendee@example.test");
        EmailCalendarDateTime due = new EmailCalendarDateTimeOperations().CreateWithYearAndMonthAndDayAndHourAndMinuteAndSecondAndUtcAndTimeZoneId(2026, 8, 25, 17, 0, 0, true, null!);
        builder.SetDue(due);
        builder.AddCalendarExtension(new EmailCalendarPropertyOperations().CreateText("X-WR-CALNAME", "Workflow"));
        EmailCalendarComponent alarm = new EmailCalendarComponent("VALARM", new List<EmailCalendarProperty>(), new List<EmailCalendarComponent>());
        builder.AddChildComponent(alarm);

        EmailCalendar calendar = builder.Build();
        EmailCalendarComponent todo = calendar.GetComponentsNamed("VTODO").Single();
        Assert.That(calendar.HasMethod(), Is.False);
        Assert.That(calendar.GetExtensionProperties().Single().GetTextValue(), Is.EqualTo("Workflow"));
        Assert.That(todo.GetOrganizer()!.GetCalendarAddress(), Is.EqualTo("mailto:organizer@example.test"));
        Assert.That(todo.GetAttendees().Single().GetCalendarAddress(), Is.EqualTo("mailto:attendee@example.test"));
        Assert.That(todo.GetDue()!.GetOriginalValue(), Is.EqualTo(due.GetOriginalValue()));
        Assert.That(todo.GetComponentAt(0), Is.SameAs(alarm));
    }

    static EmailComposerOptions RelaxedComposerOptions()
    {
        EmailComposerOptions options = new EmailComposerOptions();
        options.SetRequireFrom(false);
        options.SetRequireDate(false);
        options.SetRequireMessageId(false);
        options.SetRequireRecipient(false);
        return options;
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.UTF8.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.HasMessage(), Is.True);
        return result.GetMessage()!;
    }

    static EmailAddress Address(string source)
    {
        EmailAddressParseResult result = new EmailAddressParser().ParseMailbox(Encoding.UTF8.GetBytes(source), EmailAddressParseMode.Strict);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetMailbox()!;
    }

    static EmailDateTime Date(string source)
    {
        return new EmailDateParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(source), EmailDateParseMode.Strict).GetValue()!;
    }

    static EmailMessageId MessageId(string source)
    {
        return new EmailMessageIdParser().ParseMessageIdWithInputAndMode(Encoding.ASCII.GetBytes(source), EmailMessageIdParseMode.Strict).GetIdentifier()!;
    }

    static EmailMessageId ContentId(string source)
    {
        return new EmailMessageIdParser().ParseContentIdWithInputAndMode(Encoding.ASCII.GetBytes(source), EmailMessageIdParseMode.Strict).GetIdentifier()!;
    }

    static List<string> HeaderAddressLocalParts(EmailMessage message, string fieldName)
    {
        EmailAddressList value = new EmailAddressParser().ParseListWithInputAndMode(message.GetHeaders()!.GetFirst(fieldName)!.GetUnfoldedValue(), EmailAddressParseMode.Strict).GetAddressList();
        List<string> result = new List<string>();
        int index = 0;
        while (index < value.GetCount())
        {
            result.Add(value.GetAt(index).GetMailbox()!.GetLocalPart().ToLowerInvariant());
            index++;
        }

        return result;
    }

    static string BodyText(EmailMessage message)
    {
        return new EmailTextProcessor().DecodeWithEntity(new EmailBodySelector().GetPlainTextWithMessage(message)!).GetText();
    }

    static string ReportType(EmailMessage message)
    {
        EmailRfc2231DecodedParameter parameter = message.GetBody()!.GetMediaType()!.GetParameters().GetDecoded("report-type")!;
        return Encoding.ASCII.GetString(parameter.GetDecodedBytes());
    }
}
