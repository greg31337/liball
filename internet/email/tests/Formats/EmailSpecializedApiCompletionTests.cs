using System.Text;
using email;

namespace email.Tests;

public class EmailSpecializedApiCompletionTests
{
    [Test]
    public void CalendarModelsAndOverloadsExposeDatesParticipantsAndSourceMetadata()
    {
        const string source = "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Example//Completion//EN\r\nMETHOD:REQUEST\r\n"
            + "BEGIN:VEVENT\r\nUID:event@example.test\r\nDTSTAMP:20260825T120000Z\r\nDTSTART:20260826T120000Z\r\nCREATED:20260825T120000Z\r\nLAST-MODIFIED:20260825T130000Z\r\n"
            + "ORGANIZER;CN=Organizer:mailto:organizer@example.test\r\n"
            + "ATTENDEE;ROLE=REQ-PARTICIPANT;PARTSTAT=ACCEPTED:mailto:attendee@example.test\r\n"
            + "X-TRACE;X-MODE=one;X-MODE=two:value\r\n"
            + "BEGIN:VALARM\r\nACTION:DISPLAY\r\nTRIGGER:-PT15M\r\nDESCRIPTION:Reminder\r\nEND:VALARM\r\nEND:VEVENT\r\nEND:VCALENDAR\r\n";
        EmailCalendar calendar = new EmailCalendarParser().ParseWithInputUsingString(source).GetCalendar()!;
        Assert.That(calendar.GetPropertyCount(), Is.EqualTo(3));
        Assert.That(calendar.GetPropertyAt(1).GetName(), Is.EqualTo("PRODID"));
        Assert.That(calendar.GetProductIdentifier(), Is.EqualTo("-//Example//Completion//EN"));
        Assert.That(calendar.GetComponentCount(), Is.EqualTo(1));
        EmailCalendarComponent calendarEvent = calendar.GetComponentAt(0);
        Assert.That(calendarEvent.GetPropertyCount(), Is.EqualTo(8));
        Assert.That(calendarEvent.GetPropertyAt(0).GetName(), Is.EqualTo("UID"));
        Assert.That(calendarEvent.GetComponentCount(), Is.EqualTo(1));
        Assert.That(calendarEvent.GetComponentsNamed("VALARM").Count, Is.EqualTo(1));
        Assert.That(calendarEvent.GetCreated(), Is.Not.Null);
        Assert.That(calendarEvent.GetLastModified(), Is.Not.Null);

        EmailCalendarParticipant organizer = calendarEvent.GetOrganizer()!;
        Assert.That(organizer.HasCommonName(), Is.True);
        Assert.That(organizer.GetProperty().GetName(), Is.EqualTo("ORGANIZER"));
        EmailCalendarParticipant attendee = calendarEvent.GetAttendees().Single();
        Assert.That(attendee.HasRole(), Is.True);
        Assert.That(attendee.HasParticipationStatus(), Is.True);

        EmailCalendarProperty trace = calendarEvent.GetFirstProperty("X-TRACE")!;
        Assert.That(trace.HasSourcePosition(), Is.True);
        Assert.That(trace.GetSourcePosition(), Is.Not.Null);
        Assert.That(trace.GetParameterCount(), Is.EqualTo(2));
        Assert.That(trace.GetParameterAt(0).GetName(), Is.EqualTo("X-MODE"));
        Assert.That(trace.GetParametersNamed("X-MODE").Count, Is.EqualTo(2));
        Assert.That(trace.GetParameterAt(0).GetValues(), Is.EqualTo(new[] { "one" }));

        EmailCalendarDateTime date = new EmailCalendarDateTimeOperations().CreateWithYearAndMonthAndDay(2026, 8, 25);
        Assert.That(date.GetYear(), Is.EqualTo(2026));
        Assert.That(date.GetMonth(), Is.EqualTo(8));
        Assert.That(date.GetDay(), Is.EqualTo(25));
        Assert.That(date.GetMinute(), Is.Zero);
        Assert.That(date.GetSecond(), Is.Zero);
        Assert.That(date.IsFloating(), Is.False);

        MimeEntity entity = new EmailCalendarWriter().CreateEntityWithCalendar(calendar);
        Assert.That(entity.GetMediaType()!.IsType("text", "calendar"), Is.True);
        Assert.That(new EmailCalendarWriter().CreateEntityWithCalendarAndOptions(calendar, new EmailCalendarWriteOptions()).GetKind(), Is.EqualTo(MimeEntityKind.Calendar));
        Assert.That(new EmailCalendarWriter().CreateImipEntityWithCalendarAndOptions(calendar, new EmailCalendarWriteOptions()).GetKind(), Is.EqualTo(MimeEntityKind.Calendar));
        Assert.That(new EmailCalendarParser().ParseWithEntityAndOptions(entity, new EmailCalendarParseOptions()).IsSuccess(), Is.True);
        EmailMessage message = new EmailMessageOperations().CreateWith();
        message.SetBody(entity);
        Assert.That(new EmailCalendarParser().ParseWithMessageAndOptions(message, new EmailCalendarParseOptions()).IsSuccess(), Is.True);
    }

    [Test]
    public void PartialEntityOverloadsAndCollectorReportMissingAndDuplicateSequences()
    {
        EmailMessage first = Partial("group", 1, 3, "Subject: inner\r\n\r\none\r\n");
        EmailMessage duplicate = Partial("group", 1, 0, "other\r\n");
        EmailMessage third = Partial("group", 3, 3, "three");
        List<MimeEntity> entities = new List<MimeEntity> { first.GetBody()!, duplicate.GetBody()!, third.GetBody()! };
        EmailPartialMessageAssemblyOptions options = new EmailPartialMessageAssemblyOptions();
        options.SetReparse(false);
        EmailPartialMessageAssembler assembler = new EmailPartialMessageAssembler();
        EmailPartialMessageAssemblyResult basic = assembler.AssembleEntitiesWithFragments(entities);
        EmailPartialMessageAssemblyResult result = assembler.AssembleEntitiesWithFragmentsAndOptions(entities, options);
        Assert.That(basic.GetFragmentId(), Is.EqualTo("group"));
        Assert.That(result.HasExpectedFragmentCount(), Is.True);
        Assert.That(result.GetExpectedFragmentCount(), Is.EqualTo(3));
        Assert.That(result.GetMissingFragmentCount(), Is.EqualTo(1));
        Assert.That(result.GetMissingFragmentNumberAt(0), Is.EqualTo(2));
        Assert.That(result.GetDuplicateFragmentCount(), Is.EqualTo(1));
        Assert.That(result.GetDuplicateFragmentNumberAt(0), Is.EqualTo(1));
        Assert.That(result.GetInnerMessage(), Is.Null);

        EmailPartialMessageCollector collector = new EmailPartialMessageCollectorOperations().CreateWith();
        collector.AddEntity(first.GetBody()!);
        collector.AddEntity(ParseEntity("Content-Type: message/partial\r\n\r\nrejected"));
        Assert.That(collector.GetObservedFragmentCount(), Is.EqualTo(2));
        Assert.That(collector.GetRejectedFragmentCount(), Is.EqualTo(1));
        Assert.That(collector.GetIssues().GetCount(), Is.GreaterThan(0));
        EmailPartialMessageFragment fragment = collector.Assemble("group").GetFragmentAt(0);
        Assert.That(fragment.GetFragmentId(), Is.EqualTo("group"));
        Assert.That(fragment.GetMetadata()!.GetParameters().GetCount(), Is.GreaterThan(0));
        Assert.That(fragment.GetMetadata()!.HasFragmentNumber(), Is.True);
    }

    [Test]
    public void ExternalBodyMetadataPreservesEveryCallerVisibleLocatorField()
    {
        MimeEntity ftp = ParseEntity(
            "Content-Type: message/external-body; access-type=ftp; size=42; permission=read-write; name=file.bin; site=files.example.test; directory=\"/pub\"; mode=ascii; server=server.example.test; subject=fetch; url=\"https://example.test/file.bin\"\r\n\r\n"
            + "Content-Type: application/octet-stream\r\n\r\nphantom");
        EmailExternalBodyMetadata metadata = ftp.GetExternalBodyMetadata()!;
        Assert.That(metadata.GetAccessType(), Is.EqualTo("ftp"));
        Assert.That(metadata.HasSize(), Is.True);
        Assert.That(metadata.HasPermission(), Is.True);
        Assert.That(metadata.GetPermission(), Is.EqualTo("read-write"));
        Assert.That(metadata.HasName(), Is.True);
        Assert.That(metadata.GetName(), Is.EqualTo("file.bin"));
        Assert.That(metadata.HasSite(), Is.True);
        Assert.That(metadata.GetSite(), Is.EqualTo("files.example.test"));
        Assert.That(metadata.HasDirectory(), Is.True);
        Assert.That(metadata.GetDirectory(), Is.EqualTo("/pub"));
        Assert.That(metadata.HasMode(), Is.True);
        Assert.That(metadata.GetMode(), Is.EqualTo("ascii"));
        Assert.That(metadata.HasServer(), Is.True);
        Assert.That(metadata.HasSubject(), Is.True);
        Assert.That(metadata.HasUrl(), Is.True);
        Assert.That(metadata.GetReferencedHeaderRange().GetLength(), Is.GreaterThan(0));
    }

    [Test]
    public void TnefModelsAndStreamConversionExposeRetainedSemanticMetadata()
    {
        EmailRange range = new EmailRange(0, 1);
        EmailTnefAttribute attribute = new EmailTnefAttribute(2, 1, 0x00027777, 1, new byte[] { 7 }, 7, 7, true, -1, range, range, range, range);
        Assert.That(attribute.GetIndex(), Is.EqualTo(2));
        Assert.That(attribute.GetRawLevel(), Is.EqualTo(1));
        Assert.That(attribute.GetChecksum(), Is.EqualTo(7));
        Assert.That(attribute.GetCalculatedChecksum(), Is.EqualTo(7));

        EmailTnefProperty property = new EmailTnefProperty(0x0003, 0x4000, EmailTnefNamedPropertyKind.Numeric, "set", 9, null, new List<byte[]> { new byte[] { 42, 0, 0, 0 } }, new List<string?> { null }, new List<long> { 42 }, new List<bool> { true }, new byte[] { 1 }, range, true);
        Assert.That(property.GetTag(), Is.Not.Zero);
        Assert.That(property.IsNamed(), Is.True);
        Assert.That(property.HasTextValueAt(0), Is.False);
        Assert.That(property.HasIntegerValueAt(0), Is.True);
        Assert.That(property.GetIntegerValueAt(0), Is.EqualTo(42));

        EmailTnefBody body = new EmailTnefBody(EmailTnefBodyKind.PlainText, "body", Encoding.UTF8.GetBytes("body"), Encoding.UTF8.GetBytes("body"), "utf-8", property, attribute);
        Assert.That(body.GetCharset(), Is.EqualTo("utf-8"));
        Assert.That(body.HasSourceProperty(), Is.True);
        Assert.That(body.GetSourceProperty(), Is.SameAs(property));
        Assert.That(body.HasSourceAttribute(), Is.True);
        Assert.That(body.GetSourceAttribute(), Is.SameAs(attribute));

        EmailTnefAttachment attachment = new EmailTnefAttachment(3, new List<EmailTnefAttribute> { attribute }, new List<EmailTnefProperty> { property }, "file.bin", "application/octet-stream", "file@example.test", "files/file.bin", new byte[] { 1 }, true, 1, 4, false, null, new EmailIssueList());
        Assert.That(attachment.GetIndex(), Is.EqualTo(3));
        Assert.That(attachment.GetAttributes().Single(), Is.SameAs(attribute));
        Assert.That(attachment.HasFilename(), Is.True);
        Assert.That(attachment.HasContentType(), Is.True);
        Assert.That(attachment.HasContentId(), Is.True);
        Assert.That(attachment.HasContentLocation(), Is.True);
        Assert.That(attachment.GetIssues().GetCount(), Is.Zero);

        EmailTnefRecipient recipient = new EmailTnefRecipient(1, EmailTnefRecipientKind.To, "Recipient", "SMTP", "recipient@example.test", "smtp@example.test", new List<EmailTnefProperty> { property });
        Assert.That(recipient.GetIndex(), Is.EqualTo(1));
        Assert.That(recipient.GetAddressType(), Is.EqualTo("SMTP"));
        Assert.That(recipient.GetEmailAddress(), Is.EqualTo("recipient@example.test"));
        Assert.That(recipient.GetProperties().Single(), Is.SameAs(property));

        EmailTnefHeader header = new EmailTnefHeader(EmailTnefReaderConstants.TnefSignature, 0, new EmailRange(0, 6));
        EmailTnefContainer container = new EmailTnefContainer(header, new List<EmailTnefAttribute> { attribute }, new byte[6], 1);
        Assert.That(container.HasVersion(), Is.False);
        EmailTnefMessage semantic = new EmailTnefMessage(container, new List<EmailTnefProperty> { property }, new List<EmailTnefRecipient> { recipient }, new List<EmailTnefBody> { body }, new List<EmailTnefAttachment> { attachment }, "subject", "IPM.Note");
        Assert.That(semantic.GetContainer(), Is.SameAs(container));
        Assert.That(semantic.HasMessageClass(), Is.True);
        Assert.That(semantic.FindProperties(0x4000).Single(), Is.SameAs(property));

        EmailTnefParseResult parsed = new EmailTnefParseResultOperations().CreateWithContainerAndMessageAndIssuesAndComplete(container, semantic, new EmailIssueList(), true);
        EmailTnefConversionResult conversion = new EmailTnefConverter().ConvertWithParsed(parsed);
        Assert.That(conversion.HasOriginalParseResult(), Is.True);
        Assert.That(conversion.HasOriginalMessage(), Is.True);

        byte[] minimal = new byte[] { 0x78, 0x9f, 0x3e, 0x22, 0x34, 0x12 };
        Assert.That(new EmailTnefReader().ParseWithInputUsingStream(new MemoryStream(minimal, false)).HasContainer(), Is.True);
        MimeEntity tnef = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Tnef, new EmailHeadersOperations().CreateWith(), MediaType("application/ms-tnef"), new EmailContentOperations().CreateMemoryCopy(minimal, false), new List<MimeEntity>(), null);
        Assert.That(new EmailTnefConverter().ConvertWithEntityAndOptions(tnef, new EmailTnefConversionOptions()).HasOriginalParseResult(), Is.True);
    }

    [Test]
    public void DeliveryStatusModelsExposeOptionalFieldsAndMultipartProjection()
    {
        MimeEntity root = ParseEntity(
            "Content-Type: multipart/report; boundary=r; report-type=delivery-status\r\n\r\n"
            + "--r\r\nContent-Type: text/plain\r\n\r\nfailed\r\n"
            + "--r\r\nContent-Type: message/delivery-status\r\n\r\n"
            + "Reporting-MTA: dns; mx.example.test\r\nDSN-Gateway: dns; gateway.example.test\r\nReceived-From-MTA: dns; relay.example.test\r\nOriginal-Envelope-Id: envelope-1\r\n\r\n"
            + "Original-Recipient: rfc822; old@example.test\r\nFinal-Recipient: rfc822; user@example.test\r\nAction: failed\r\nStatus: 5.1.2\r\nRemote-MTA: dns; remote.example.test\r\nDiagnostic-Code: smtp; 550 unavailable\r\nLast-Attempt-Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFinal-Log-ID: log-1\r\n"
            + "--r\r\nContent-Type: text/rfc822-headers\r\n\r\nSubject: original\r\n\r\n--r--\r\n");
        EmailDeliveryStatus status = new EmailDeliveryStatusParser().ParseWithRoot(root).GetDeliveryStatus()!;
        Assert.That(status.HasMultipartReport(), Is.True);
        Assert.That(status.GetMultipartReport(), Is.Not.Null);
        Assert.That(status.HasOriginalContentPart(), Is.True);
        Assert.That(status.GetOriginalContentPart(), Is.Not.Null);
        EmailDeliveryStatusMessage messageFields = status.GetMessageFields()!;
        Assert.That(messageFields.HasDsnGateway(), Is.True);
        Assert.That(messageFields.HasReceivedFromMta(), Is.True);
        Assert.That(messageFields.HasOriginalEnvelopeId(), Is.True);
        EmailDeliveryStatusRecipient recipient = status.GetRecipientAt(0);
        Assert.That(recipient.HasOriginalRecipient(), Is.True);
        Assert.That(recipient.GetActionText(), Is.EqualTo("failed"));
        Assert.That(recipient.HasStatus(), Is.True);
        Assert.That(recipient.GetStatus()!.GetSubject(), Is.EqualTo(1));
        Assert.That(recipient.GetStatus()!.GetDetail(), Is.EqualTo(2));
        Assert.That(recipient.HasRemoteMta(), Is.True);
        Assert.That(recipient.HasDiagnosticCode(), Is.True);
        Assert.That(recipient.HasLastAttemptDate(), Is.True);
        Assert.That(recipient.HasFinalLogId(), Is.True);

        EmailMessage original = ParseMessage("From: author@example.test\r\nMessage-ID: <original@example.test>\r\n\r\nbody");
        EmailComposer report = new EmailDeliveryStatusWriter().CreateReportWithDeliveryStatusAndOriginalAndReporterAndRecipientAndHumanReadable(status, original, Address("daemon@example.test"), Address("author@example.test"), "failed");
        Assert.That(report.Build().GetBody()!.GetMediaType()!.IsType("multipart", "report"), Is.True);
    }

    [Test]
    public void DispositionNotificationModelsExposeOptionalFieldsModifiersAndReportProjection()
    {
        MimeEntity root = ParseEntity(
            "Content-Type: multipart/report; boundary=r; report-type=disposition-notification\r\n\r\n"
            + "--r\r\nContent-Type: text/plain\r\n\r\ndisplayed\r\n"
            + "--r\r\nContent-Type: message/disposition-notification\r\n\r\n"
            + "Reporting-UA: Example; Product/1\r\nMDN-Gateway: dns; gateway.example.test\r\nOriginal-Recipient: rfc822; old@example.test\r\nFinal-Recipient: rfc822; user@example.test\r\nOriginal-Message-ID: <original@example.test>\r\nDisposition: manual-action/MDN-sent-manually; failed/error\r\n"
            + "--r\r\nContent-Type: text/rfc822-headers\r\n\r\nSubject: original\r\n\r\n--r--\r\n");
        EmailDispositionNotification notification = new EmailDispositionNotificationParser().ParseWithRoot(root).GetNotification()!;
        Assert.That(notification.HasReportingUserAgent(), Is.True);
        Assert.That(notification.GetReportingUserAgent()!.HasProduct(), Is.True);
        Assert.That(notification.HasGateway(), Is.True);
        Assert.That(notification.HasOriginalRecipient(), Is.True);
        Assert.That(notification.HasFinalRecipient(), Is.True);
        Assert.That(notification.HasDisposition(), Is.True);
        Assert.That(notification.HasMultipartReport(), Is.True);
        Assert.That(notification.GetMultipartReport(), Is.Not.Null);
        Assert.That(notification.HasOriginalContentPart(), Is.True);
        Assert.That(notification.GetOriginalContentPart(), Is.Not.Null);
        Assert.That(notification.GetDisposition()!.GetDispositionTypeName(), Is.EqualTo("failed"));
        Assert.That(notification.GetDisposition()!.GetModifierAt(0), Is.EqualTo("error"));

        EmailDispositionNotificationOption option = new EmailDispositionNotificationOption("x-confirm", EmailDispositionNotificationOptionImportance.Required, new List<string> { "yes", "always" });
        Assert.That(option.GetValues(), Is.EqualTo(new[] { "yes", "always" }));
        EmailMessage original = ParseMessage("From: author@example.test\r\nMessage-ID: <original@example.test>\r\n\r\nbody");
        EmailComposer report = new EmailDispositionNotificationWriter().CreateReportWithNotificationAndOriginalAndReporterAndRecipientAndHumanReadable(notification, original, Address("reader@example.test"), Address("author@example.test"), "displayed");
        Assert.That(report.Build().GetBody()!.GetMediaType()!.IsType("multipart", "report"), Is.True);
    }

    [Test]
    public void FeedbackReportModelsExposeOptionalEvidenceAndRootOverload()
    {
        MimeEntity root = ParseEntity(
            "Content-Type: multipart/report; boundary=r; report-type=feedback-report\r\n\r\n"
            + "--r\r\nContent-Type: text/plain\r\n\r\nabuse\r\n"
            + "--r\r\nContent-Type: message/feedback-report\r\n\r\n"
            + "Feedback-Type: auth-failure\r\nUser-Agent: Example/1\r\nVersion: 1\r\nOriginal-Envelope-Id: envelope-1\r\nOriginal-Mail-From: <sender@example.test>\r\nReporting-MTA: dns; mx.example.test\r\nSource-IP: 192.0.2.1\r\nSource-Port: 25\r\nAuth-Failure: dkim\r\nDelivery-Result: reject\r\nAuthentication-Results: mx.example.test; dkim=fail header.d=example.test\r\nDKIM-Domain: example.test\r\nDKIM-Identity: sender@example.test\r\nDKIM-Selector: selector\r\n"
            + "--r\r\nContent-Type: text/rfc822-headers\r\n\r\nSubject: original\r\n\r\n--r--\r\n");
        EmailFeedbackReport report = new EmailFeedbackReportParser().ParseWithRootAndOptions(root, new EmailReportParseOptions()).GetFeedbackReport()!;
        Assert.That(report.HasOriginalEnvelopeId(), Is.True);
        Assert.That(report.HasOriginalMailFrom(), Is.True);
        Assert.That(report.GetOriginalMailFrom()!.HasAddress(), Is.True);
        Assert.That(report.HasReportingMta(), Is.True);
        Assert.That(report.HasSourceIp(), Is.True);
        Assert.That(report.HasSourcePort(), Is.True);
        Assert.That(report.GetAuthenticationFailureName(), Is.EqualTo("dkim"));
        Assert.That(report.GetDeliveryResultName(), Is.EqualTo("reject"));
        Assert.That(report.GetDkimDomain(), Is.EqualTo("example.test"));
        Assert.That(report.GetDkimIdentity(), Is.EqualTo("sender@example.test"));
        Assert.That(report.GetDkimSelector(), Is.EqualTo("selector"));
        Assert.That(report.HasMultipartReport(), Is.True);
        Assert.That(report.GetMultipartReport(), Is.Not.Null);
        Assert.That(report.HasOriginalContentPart(), Is.True);
        Assert.That(report.GetOriginalContentPart(), Is.Not.Null);
        Assert.That(new EmailFeedbackTypedValue("dns", "mx.example.test").GetTypeName(), Is.EqualTo("dns"));

        EmailMessage original = ParseMessage("From: author@example.test\r\n\r\nbody");
        EmailComposer generated = new EmailFeedbackReportWriter().CreateReportWithReportAndOriginalAndReporterAndRecipientAndHumanReadable(report, original, Address("abuse@example.test"), Address("author@example.test"), "abuse");
        Assert.That(generated.Build().GetBody()!.GetMediaType()!.IsType("multipart", "report"), Is.True);
    }

    [Test]
    public void ReportFieldBlocksFeedPublicModelFactoriesAndBoundedReportParsing()
    {
        byte[] notificationBytes = Encoding.ASCII.GetBytes("Final-Recipient: rfc822; user@example.test\r\nDisposition: manual-action/MDN-sent-manually; displayed\r\n");
        EmailReportFieldBlockParseResult notificationFields = new EmailReportFieldBlockParser().ParseWithInput(notificationBytes);
        Assert.That(notificationFields.GetSourceByteCount(), Is.EqualTo(notificationBytes.Length));
        Assert.That(notificationFields.GetBlockAt(0).GetIssues().GetCount(), Is.Zero);
        EmailDispositionNotification notification = new EmailDispositionNotificationOperations().CreateWithInternationalizedAndFields(false, notificationFields.GetBlockAt(0));
        Assert.That(notification.HasFinalRecipient(), Is.True);

        EmailReportFieldBlock feedbackFields = new EmailReportFieldBlockParser().ParseWithInput(Encoding.ASCII.GetBytes("Feedback-Type: abuse\r\nUser-Agent: Example/1\r\nVersion: 1\r\n")).GetBlockAt(0);
        EmailFeedbackReport feedback = new EmailFeedbackReportOperations().CreateWithFields(feedbackFields);
        Assert.That(feedback.GetFeedbackType(), Is.EqualTo(EmailFeedbackType.Abuse));

        MimeEntity root = ParseEntity(
            "Content-Type: multipart/report; boundary=r; report-type=delivery-status\r\n\r\n"
            + "--r\r\nContent-Type: text/plain\r\n\r\nstatus\r\n"
            + "--r\r\nContent-Type: message/delivery-status\r\n\r\nReporting-MTA: dns; mx.example.test\r\n\r\nFinal-Recipient: rfc822; user@example.test\r\nAction: delivered\r\nStatus: 2.0.0\r\n"
            + "--r--\r\n");
        EmailMultipartReport report = new EmailReportParser().ParseWithRoot(root).GetReport()!;
        EmailReportFieldBlockParseResult blocks = new EmailReportParser().ParseFieldBlocksWithReportAndOptions(report, new EmailReportParseOptions());
        Assert.That(blocks.GetBlockCount(), Is.EqualTo(2));
    }

    static EmailMessage Partial(string id, long number, long total, string body)
    {
        string source = "Content-Type: message/partial; id=\"" + id + "\"; number=" + number + (total > 0 ? "; total=" + total : "") + "\r\n\r\n" + body;
        return new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.UTF8.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving()).GetMessage()!;
    }

    static MimeEntity ParseEntity(string source)
    {
        return new EmailParser().ParseEntityWithInputUsingValueArray(Encoding.UTF8.GetBytes(source)).GetEntity()!;
    }

    static EmailMessage ParseMessage(string source)
    {
        return new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.UTF8.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving()).GetMessage()!;
    }

    static EmailAddress Address(string source)
    {
        return new EmailAddressParser().ParseMailbox(Encoding.UTF8.GetBytes(source), EmailAddressParseMode.Strict).GetMailbox()!;
    }

    static EmailMediaType MediaType(string source)
    {
        return new EmailMediaTypeParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(source), EmailStructuredValueParseMode.Strict).GetValue();
    }
}
