using System.Net;
using System.Text;
using email;

namespace email.Tests;

public class EmailSpecializedFormatInteropTests
{
    [Test]
    public void Rfc3464MultiRecipientVectorParsesCommentsAndFoldedDiagnostics()
    {
        EmailMessage message = ParseMessage(
            "From: Mail Delivery Subsystem <mailer-daemon@example.test>\r\n"
            + "To: sender@example.test\r\nSubject: Delivery status\r\nMIME-Version: 1.0\r\n"
            + "Content-Type: multipart/report; report-type=delivery-status; boundary=dsn-vector\r\n\r\n"
            + "--dsn-vector\r\nContent-Type: text/plain; charset=us-ascii\r\n\r\nDelivery failed.\r\n"
            + "--dsn-vector\r\nContent-Type: message/delivery-status\r\n\r\n"
            + "Reporting-MTA: dns; outbound.example.test\r\n\r\n"
            + "Original-Recipient: rfc822; first@example.test\r\n"
            + "Final-Recipient: rfc822; first@example.test\r\n"
            + "Action: failed\r\nStatus: 5.0.0 (permanent failure)\r\n"
            + "Diagnostic-Code: smtp; 550 first address is not a\r\n registered mailbox\r\n"
            + "Remote-MTA: dns; inbound.example.test\r\n\r\n"
            + "Original-Recipient: rfc822; second@example.test\r\n"
            + "Final-Recipient: rfc822; second@example.test\r\n"
            + "Action: delayed\r\nStatus: 4.0.0 (temporary lookup failure)\r\n"
            + "Will-Retry-Until: Fri, 15 Aug 2025 12:00:00 +0000\r\n"
            + "--dsn-vector--\r\n");

        EmailDeliveryStatusParseResult result = new EmailDeliveryStatusParser().ParseWithMessage(message);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetDeliveryStatus()!.GetRecipientCount(), Is.EqualTo(2));
        Assert.That(result.GetDeliveryStatus()!.GetRecipientAt(0).GetStatus()!.GetText(), Is.EqualTo("5.0.0"));
        Assert.That(result.GetDeliveryStatus()!.GetRecipientAt(0).GetDiagnosticCode()!.GetValue(), Is.EqualTo("550 first address is not a registered mailbox"));
        Assert.That(result.GetDeliveryStatus()!.GetRecipientAt(1).GetStatus()!.GetText(), Is.EqualTo("4.0.0"));
        Assert.That(result.GetDeliveryStatus()!.GetRecipientAt(1).HasWillRetryUntil(), Is.True);
    }

    [Test]
    public void DsnCommentNormalizationHandlesNestedEscapesAndPreservesQuotedParentheses()
    {
        EmailDeliveryStatusParseResult result = new EmailDeliveryStatusParser().ParseWithMessage(ReportMessage(
            "delivery-status",
            "message/delivery-status",
            "Reporting-MTA: dns; mx.example.test (routing detail)\r\n\r\n"
            + "Final-Recipient: rfc822; reader@example.test\r\n"
            + "Action: failed (terminal action)\r\n"
            + "Status: 5.1.1 (outer (escaped \\) parenthesis))\r\n"
            + "Diagnostic-Code: smtp; 550 \"mailbox (archived)\" (remote detail)\r\n",
            false));

        EmailDeliveryStatusRecipient recipient = result.GetDeliveryStatus()!.GetRecipientAt(0);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetDeliveryStatus()!.GetMessageFields()!.GetReportingMta()!.GetValue(), Is.EqualTo("mx.example.test"));
        Assert.That(recipient.GetAction(), Is.EqualTo(EmailDeliveryStatusAction.Failed));
        Assert.That(recipient.GetStatus()!.GetText(), Is.EqualTo("5.1.1"));
        Assert.That(recipient.GetDiagnosticCode()!.GetValue(), Is.EqualTo("550 \"mailbox (archived)\""));
        Assert.That(recipient.GetFieldBlock().GetFirstField("Status")!.GetDecodedValue(), Does.Contain("escaped"));
    }

    [Test]
    public void Rfc8098DisplayedNotificationVectorParsesThroughTheWholeMessage()
    {
        EmailMessage message = ParseMessage(
            "From: Reader <reader@example.test>\r\nTo: Author <author@example.test>\r\n"
            + "Subject: Disposition notification\r\nMIME-Version: 1.0\r\n"
            + "Content-Type: multipart/report; report-type=disposition-notification; boundary=mdn-vector\r\n\r\n"
            + "--mdn-vector\r\nContent-Type: text/plain\r\n\r\nThe message was displayed.\r\n"
            + "--mdn-vector\r\nContent-Type: message/disposition-notification\r\n\r\n"
            + "Reporting-UA: reader.example.test; ExampleMail 1.0\r\n"
            + "Original-Recipient: rfc822; reader@example.test\r\n"
            + "Final-Recipient: rfc822; reader@example.test\r\n"
            + "Original-Message-ID: <original@example.test>\r\n"
            + "Disposition: manual-action/MDN-sent-manually; displayed\r\n"
            + "--mdn-vector--\r\n");

        EmailDispositionNotificationParseResult result = new EmailDispositionNotificationParser().ParseWithMessage(message);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetNotification()!.GetReportingUserAgent()!.GetName(), Is.EqualTo("reader.example.test"));
        Assert.That(result.GetNotification()!.GetReportingUserAgent()!.GetProduct(), Is.EqualTo("ExampleMail 1.0"));
        Assert.That(result.GetNotification()!.GetOriginalRecipient()!.GetValue(), Is.EqualTo("reader@example.test"));
        Assert.That(result.GetNotification()!.GetOriginalMessageId()!.GetIdRight(), Is.EqualTo("example.test"));
        Assert.That(result.GetNotification()!.GetDisposition()!.GetDispositionType(), Is.EqualTo(EmailDispositionNotificationType.Displayed));
    }

    [Test]
    public void Rfc5965RequiredFieldsVectorProjectsTheReportedMessage()
    {
        EmailMessage message = ParseMessage(
            "From: Abuse Desk <abuse@example.test>\r\nTo: operator@example.test\r\n"
            + "Subject: Abuse report\r\nMIME-Version: 1.0\r\n"
            + "Content-Type: multipart/report; report-type=feedback-report; boundary=arf-vector\r\n\r\n"
            + "--arf-vector\r\nContent-Type: text/plain; charset=us-ascii\r\n\r\nAn abusive message was reported.\r\n"
            + "--arf-vector\r\nContent-Type: message/feedback-report\r\n\r\n"
            + "Feedback-Type: abuse\r\nUser-Agent: ExampleReporter/1.0\r\nVersion: 1\r\n"
            + "Source-IP: 192.0.2.1\r\n"
            + "--arf-vector\r\nContent-Type: message/rfc822\r\nContent-Disposition: inline\r\n\r\n"
            + "From: unwanted@example.test\r\nTo: victim@example.test\r\nSubject: Unwanted message\r\n"
            + "Message-ID: <reported@example.test>\r\nContent-Type: text/plain\r\n\r\nreported body\r\n"
            + "--arf-vector--\r\n");

        EmailFeedbackReportParseResult result = new EmailFeedbackReportParser().ParseWithMessage(message);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetFeedbackReport()!.GetFeedbackType(), Is.EqualTo(EmailFeedbackType.Abuse));
        Assert.That(result.GetFeedbackReport()!.GetUserAgent(), Is.EqualTo("ExampleReporter/1.0"));
        Assert.That(result.GetFeedbackReport()!.GetSourceIp(), Is.EqualTo(IPAddress.Parse("192.0.2.1")));
        Assert.That(result.GetFeedbackReport()!.HasOriginalMessage(), Is.True);
        Assert.That(result.GetFeedbackReport()!.GetOriginalMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("Unwanted message"));
    }

    [Test]
    public void Rfc5545EventVectorRoundTripsStandardAndExtensionProperties()
    {
        const string source = "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Example//Calendar//EN\r\n"
            + "BEGIN:VEVENT\r\nUID:event-123401@example.test\r\nDTSTAMP:19970901T130000Z\r\n"
            + "DTSTART:19970903T163000Z\r\nDTEND:19970903T190000Z\r\n"
            + "SUMMARY:Annual Employee Review\r\nCLASS:PRIVATE\r\n"
            + "CATEGORIES:BUSINESS,HUMAN RESOURCES\r\nX-CONFERENCE-ID:review-7\r\n"
            + "END:VEVENT\r\nEND:VCALENDAR\r\n";

        EmailCalendarParseResult parsed = new EmailCalendarParser().ParseWithInputUsingString(source);
        EmailCalendar calendar = parsed.GetCalendar()!;
        EmailCalendarComponent calendarEvent = calendar!.GetComponentsNamed("VEVENT").Single();
        byte[] written = new EmailCalendarWriter().WriteWithCalendar(calendar);
        EmailCalendarParseResult reparsed = new EmailCalendarParser().ParseWithInputUsingReadOnlySpan(written);

        Assert.That(parsed.IsSuccess(), Is.True);
        Assert.That(calendarEvent.GetUid(), Is.EqualTo("event-123401@example.test"));
        Assert.That(calendarEvent.GetStart()!.IsUtc(), Is.True);
        Assert.That(calendarEvent.GetEnd()!.IsUtc(), Is.True);
        Assert.That(calendarEvent.GetFirstProperty("SUMMARY")!.GetTextValue(), Is.EqualTo("Annual Employee Review"));
        Assert.That(calendarEvent.GetExtensionProperties().Single().GetName(), Is.EqualTo("X-CONFERENCE-ID"));
        Assert.That(reparsed.IsSuccess(), Is.True);
        Assert.That(reparsed.GetCalendar()!.GetComponentsNamed("VEVENT").Single().GetFirstProperty("X-CONFERENCE-ID")!.GetTextValue(), Is.EqualTo("review-7"));
    }

    [Test]
    public void ReportBuildersPreserveExtensionsAcrossCanonicalMimeSerialization()
    {
        EmailMessage original = ParseMessage(
            "From: author@example.test\r\nTo: reader@example.test\r\n"
            + "Message-ID: <original@example.test>\r\nSubject: Original\r\n\r\nbody");
        EmailReportOptions options = ReportOptions();

        EmailDeliveryStatus delivery = new EmailDeliveryStatusParser().ParseWithMessage(ReportMessage(
            "delivery-status",
            "message/delivery-status",
            "Reporting-MTA: dns; mx.example.test\r\nX-Message-Trace: dsn-message\r\n\r\n"
            + "Final-Recipient: rfc822; reader@example.test\r\nAction: delivered\r\nStatus: 2.0.0\r\n"
            + "X-Recipient-Trace: dsn-recipient\r\n",
            false)).GetDeliveryStatus()!;
        EmailDispositionNotification notification = new EmailDispositionNotificationParser().ParseWithMessage(ReportMessage(
            "disposition-notification",
            "message/disposition-notification",
            "Final-Recipient: rfc822; reader@example.test\r\n"
            + "Original-Message-ID: <original@example.test>\r\n"
            + "Disposition: manual-action/MDN-sent-manually; displayed\r\nX-Mdn-Trace: mdn-extension\r\n",
            false)).GetNotification()!;
        EmailFeedbackReport feedback = new EmailFeedbackReportParser().ParseWithMessage(ReportMessage(
            "feedback-report",
            "message/feedback-report",
            "Feedback-Type: abuse\r\nUser-Agent: ExampleReporter/1.0\r\nVersion: 1\r\n"
            + "X-Arf-Trace: arf-extension\r\n",
            true)).GetFeedbackReport()!;

        EmailMessage deliveryRoundTrip = CanonicalRoundTrip(new EmailDeliveryStatusWriter().CreateReportWithDeliveryStatusAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(delivery!, original, Address("daemon@example.test"), Address("author@example.test"), "Delivered", options).Build());
        EmailMessage notificationRoundTrip = CanonicalRoundTrip(new EmailDispositionNotificationWriter().CreateReportWithNotificationAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(notification!, original, Address("reader@example.test"), Address("author@example.test"), "Displayed", options).Build());
        EmailMessage feedbackRoundTrip = CanonicalRoundTrip(new EmailFeedbackReportWriter().CreateReportWithReportAndOriginalAndReporterAndRecipientAndHumanReadableAndOptions(feedback!, original, Address("abuse@example.test"), Address("operator@example.test"), "Abuse report", options).Build());

        EmailDeliveryStatus reparsedDelivery = new EmailDeliveryStatusParser().ParseWithMessage(deliveryRoundTrip).GetDeliveryStatus()!;
        EmailDispositionNotification reparsedNotification = new EmailDispositionNotificationParser().ParseWithMessage(notificationRoundTrip).GetNotification()!;
        EmailFeedbackReport reparsedFeedback = new EmailFeedbackReportParser().ParseWithMessage(feedbackRoundTrip).GetFeedbackReport()!;
        Assert.That(reparsedDelivery!.GetMessageFields()!.GetExtensionFields().Single().GetDecodedValue(), Is.EqualTo("dsn-message"));
        Assert.That(reparsedDelivery.GetRecipientAt(0).GetExtensionFields().Single().GetDecodedValue(), Is.EqualTo("dsn-recipient"));
        Assert.That(reparsedNotification!.GetExtensionFields().Single().GetDecodedValue(), Is.EqualTo("mdn-extension"));
        Assert.That(reparsedFeedback!.GetExtensionFields().Single().GetDecodedValue(), Is.EqualTo("arf-extension"));
    }

    [Test]
    public void CalendarBuilderSurvivesCanonicalMimeSerialization()
    {
        EmailCalendarBuilder builder = new EmailCalendarBuilder("-//Example//Calendar//EN", "VEVENT");
        builder.SetMethod("REQUEST");
        builder.SetUid("builder-event@example.test");
        builder.SetStamp(new EmailCalendarDateTimeOperations().CreateWithYearAndMonthAndDayAndHourAndMinuteAndSecondAndUtcAndTimeZoneId(2026, 8, 10, 20, 0, 0, true, null!));
        builder.SetStart(new EmailCalendarDateTimeOperations().CreateWithYearAndMonthAndDayAndHourAndMinuteAndSecondAndUtcAndTimeZoneId(2026, 8, 11, 9, 0, 0, true, null!));
        builder.SetEnd(new EmailCalendarDateTimeOperations().CreateWithYearAndMonthAndDayAndHourAndMinuteAndSecondAndUtcAndTimeZoneId(2026, 8, 11, 10, 0, 0, true, null!));
        builder.SetOrganizerWithCommonName("mailto:organizer@example.test", "Organizer");
        builder.AddAttendeeWithDetails("mailto:reader@example.test", "Reader", "REQ-PARTICIPANT", "NEEDS-ACTION", true, true);
        builder.AddComponentExtension(new EmailCalendarPropertyOperations().CreateText("X-BUILDER-TRACE", "calendar-extension"));
        EmailMessage message = new EmailMessageOperations().CreateWithHeaders(new EmailHeadersOperations().CreateWith());
        message.SetBody(new EmailCalendarWriter().CreateImipEntityWithCalendar(builder.Build()));

        EmailCalendarParseResult result = new EmailCalendarParser().ParseWithMessage(CanonicalRoundTrip(message));

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.IsImip(), Is.True);
        Assert.That(result.GetCalendar()!.GetMethodName(), Is.EqualTo("REQUEST"));
        Assert.That(result.GetCalendar()!.GetComponentsNamed("VEVENT").Single().GetUid(), Is.EqualTo("builder-event@example.test"));
        Assert.That(result.GetCalendar()!.GetComponentsNamed("VEVENT").Single().GetFirstProperty("X-BUILDER-TRACE")!.GetTextValue(), Is.EqualTo("calendar-extension"));
    }

    [Test]
    public void MalformedSpecializedBodiesReturnPartialValuesWithStableIssues()
    {
        EmailDeliveryStatusParseResult delivery = new EmailDeliveryStatusParser().ParseWithMessage(ReportMessage(
            "delivery-status",
            "message/delivery-status",
            "X-Only: retained\r\n\r\nFinal-Recipient: broken\r\nAction: failed\r\nStatus: 5.bad.1\r\n",
            false));
        EmailDispositionNotificationParseResult notification = new EmailDispositionNotificationParser().ParseWithMessage(ReportMessage(
            "disposition-notification",
            "message/disposition-notification",
            "Final-Recipient: broken\r\nDisposition: manual-action/MDN-sent-manually displayed\r\n",
            false));
        EmailFeedbackReportParseResult feedback = new EmailFeedbackReportParser().ParseWithMessage(ReportMessage(
            "feedback-report",
            "message/feedback-report",
            "Feedback-Type: bad type\r\nUser-Agent: \r\nX-Only: retained\r\n",
            true));
        EmailCalendarParseResult calendar = new EmailCalendarParser().ParseWithInputUsingString(
            "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Example//EN\r\n"
            + "BEGIN:VEVENT\r\nUID:broken@example.test\r\nDTSTAMP:20260810T200000Z\r\n"
            + "END:VTODO\r\nEND:VCALENDAR\r\n");

        Assert.That(delivery.HasDeliveryStatus(), Is.True);
        Assert.That(delivery.GetIssues().ContainsCode(EmailIssueCode.DeliveryStatusRequiredFieldMissing), Is.True);
        Assert.That(delivery.GetIssues().ContainsCode(EmailIssueCode.DeliveryStatusCodeInvalid), Is.True);
        Assert.That(notification.HasNotification(), Is.True);
        Assert.That(notification.GetIssues().ContainsCode(EmailIssueCode.DispositionNotificationValueInvalid), Is.True);
        Assert.That(feedback.HasFeedbackReport(), Is.True);
        Assert.That(feedback.GetIssues().ContainsCode(EmailIssueCode.FeedbackReportRequiredFieldMissing), Is.True);
        Assert.That(feedback.GetIssues().ContainsCode(EmailIssueCode.FeedbackReportValueInvalid), Is.True);
        Assert.That(calendar.HasCalendar(), Is.True);
        Assert.That(calendar.GetIssues().ContainsCode(EmailIssueCode.CalendarComponentMismatch), Is.True);
    }

    static EmailMessage ReportMessage(string reportType, string machineType, string fields, bool includeOriginal)
    {
        string source = "MIME-Version: 1.0\r\nContent-Type: multipart/report; report-type=" + reportType + "; boundary=report-vector\r\n\r\n"
            + "--report-vector\r\nContent-Type: text/plain\r\n\r\nhuman\r\n"
            + "--report-vector\r\nContent-Type: " + machineType + "\r\n\r\n" + fields;
        if (includeOriginal)
        {
            source += "--report-vector\r\nContent-Type: text/rfc822-headers\r\n\r\n"
                + "From: sender@example.test\r\nSubject: Reported\r\n\r\n";
        }

        source += "--report-vector--\r\n";
        return ParseMessage(source);
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

    static EmailMessage CanonicalRoundTrip(EmailMessage message)
    {
        EmailWriteOptions options = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        options.SetDeterministicOutput(true);
        options.SetDeterministicSeed(Encoding.ASCII.GetBytes("specialized format interop"));
        EmailWriteResult written = new EmailWriter().WriteMessageWithMessageAndOptions(message, options);
        Assert.That(written.IsSuccess(), Is.True);
        EmailParseResult parsed = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(written.GetBytes(), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(parsed.HasMessage(), Is.True);
        Assert.That(parsed.IsComplete(), Is.True);
        return parsed.GetMessage()!;
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult parsed = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.UTF8.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(parsed.HasMessage(), Is.True);
        return parsed.GetMessage()!;
    }
}
