using System.Text;
using email;

namespace email.Tests;

public class EmailCalendarTests
{
    [Test]
    public void ParsesMethodParticipantsDatesZonesSequenceStatusAndFoldedExtensions()
    {
        EmailCalendarParseResult result = new EmailCalendarParser().ParseWithInputUsingString(FullCalendar());

        Assert.That(result.IsSuccess(), Is.True);
        EmailCalendar calendar = result.GetCalendar()!;
        EmailCalendarComponent calendarEvent = calendar!.GetEvents().Single();
        EmailCalendarParticipant attendee = calendarEvent.GetAttendees().Single();

        Assert.That(calendar.GetMethod(), Is.EqualTo(EmailCalendarMethod.Request));
        Assert.That(calendar.GetMethodName(), Is.EqualTo("REQUEST"));
        Assert.That(calendar.GetTimeZones().Single().GetTimeZoneId(), Is.EqualTo("America/Los_Angeles"));
        Assert.That(calendarEvent.GetUid(), Is.EqualTo("meeting-7@example.test"));
        Assert.That(calendarEvent.GetOrganizer()!.GetCalendarAddress(), Is.EqualTo("mailto:organizer@example.test"));
        Assert.That(calendarEvent.GetOrganizer()!.GetCommonName(), Is.EqualTo("Organizer"));
        Assert.That(attendee.GetCommonName(), Is.EqualTo("Doe, Jane"));
        Assert.That(attendee.GetRole(), Is.EqualTo("REQ-PARTICIPANT"));
        Assert.That(attendee.GetParticipationStatus(), Is.EqualTo("ACCEPTED"));
        Assert.That(attendee.HasRsvp(), Is.True);
        Assert.That(attendee.GetRsvp(), Is.True);
        Assert.That(calendarEvent.GetStart()!.GetTimeZoneId(), Is.EqualTo("America/Los_Angeles"));
        Assert.That(calendarEvent.GetStart()!.GetHour(), Is.EqualTo(9));
        Assert.That(calendarEvent.GetStamp()!.IsUtc(), Is.True);
        Assert.That(calendarEvent.GetSequence(), Is.EqualTo(2));
        Assert.That(calendarEvent.GetStatus(), Is.EqualTo(EmailCalendarStatus.Confirmed));
        Assert.That(calendarEvent.GetFirstProperty("DESCRIPTION")!.GetTextValue(), Is.EqualTo("First line continued text\nSecond line"));
        Assert.That(calendarEvent.GetExtensionProperties().Single().GetName(), Is.EqualTo("X-TRACE-ID"));
        Assert.That(calendarEvent.GetExtensionProperties().Single().GetRawValue(), Is.EqualTo("trace-42"));
    }

    [Test]
    public void ParsesImipEntityAndRequiresMatchingMimeMethod()
    {
        MimeEntity matching = new EmailEntityFactory().CreateCalendar(FullCalendar(), "request", "utf-8");
        MimeEntity mismatched = new EmailEntityFactory().CreateCalendar(FullCalendar(), "cancel", "utf-8");
        MimeEntity plainCalendar = new EmailEntityFactory().CreateContent(FullCalendar(), "text", "calendar", "utf-8");

        EmailCalendarParseResult accepted = new EmailCalendarParser().ParseWithEntity(matching);
        EmailCalendarParseResult rejected = new EmailCalendarParser().ParseWithEntity(mismatched);
        EmailCalendarParseResult plain = new EmailCalendarParser().ParseWithEntity(plainCalendar);

        Assert.That(accepted.IsSuccess(), Is.True);
        Assert.That(accepted.IsImip(), Is.True);
        Assert.That(accepted.IsMethodConsistent(), Is.True);
        Assert.That(accepted.GetMimeMethod(), Is.EqualTo("REQUEST"));
        Assert.That(rejected.IsSuccess(), Is.False);
        Assert.That(rejected.IsImip(), Is.False);
        Assert.That(rejected.IsMethodConsistent(), Is.False);
        Assert.That(rejected.GetIssues().ContainsCode(EmailIssueCode.CalendarMethodMismatch), Is.True);
        Assert.That(plain.IsSuccess(), Is.True);
        Assert.That(plain.IsImip(), Is.False);
        Assert.That(plain.HasMimeMethod(), Is.False);
    }

    [Test]
    public void ReportsInvalidKnownValuesWhilePreservingRawProperties()
    {
        string source = "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Example//EN\r\nMETHOD:REQUEST\r\n"
            + "BEGIN:VEVENT\r\nUID:event@example.test\r\nDTSTAMP:20260810T120000\r\n"
            + "DTSTART;TZID=Missing/Zone:20261340T256199\r\n"
            + "ATTENDEE;RSVP=YES:mailto:reader@example.test\r\n"
            + "SEQUENCE:1\r\nSEQUENCE:-2\r\nSTATUS:COMPLETED\r\nX-RAW;X-MODE=alpha:kept\\,raw\r\n"
            + "END:VEVENT\r\nEND:VCALENDAR\r\n";

        EmailCalendarParseResult result = new EmailCalendarParser().ParseWithInputUsingString(source);
        EmailCalendarComponent calendarEvent = result.GetCalendar()!.GetEvents().Single();

        Assert.That(result.HasCalendar(), Is.True);
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.CalendarDateInvalid), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.CalendarPropertyDuplicate), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.CalendarPropertyInvalid), Is.True);
        Assert.That(calendarEvent.GetPropertiesNamed("SEQUENCE").Count, Is.EqualTo(2));
        Assert.That(calendarEvent.GetFirstProperty("X-RAW")!.GetRawValue(), Is.EqualTo("kept\\,raw"));
        Assert.That(calendarEvent.GetFirstProperty("X-RAW")!.GetTextValue(), Is.EqualTo("kept,raw"));
        Assert.That(calendarEvent.GetFirstProperty("X-RAW")!.GetFirstParameter("X-MODE")!.GetValueAt(0), Is.EqualTo("alpha"));
    }

    [Test]
    public void BuilderAndWriterGenerateFoldedRoundTrippableImipContent()
    {
        EmailCalendarBuilder builder = new EmailCalendarBuilder("-//Example Corp//Calendar 1.0//EN", "VEVENT");
        builder.SetMethod("REQUEST");
        builder.SetUid("generated-1@example.test");
        builder.SetStamp(new EmailCalendarDateTimeOperations().CreateWithYearAndMonthAndDayAndHourAndMinuteAndSecondAndUtcAndTimeZoneId(2026, 8, 10, 20, 0, 0, true, null!));
        builder.SetStart(new EmailCalendarDateTimeOperations().CreateWithYearAndMonthAndDayAndHourAndMinuteAndSecondAndUtcAndTimeZoneId(2026, 8, 11, 9, 30, 0, false, "America/Los_Angeles"));
        builder.SetEnd(new EmailCalendarDateTimeOperations().CreateWithYearAndMonthAndDayAndHourAndMinuteAndSecondAndUtcAndTimeZoneId(2026, 8, 11, 10, 30, 0, false, "America/Los_Angeles"));
        builder.SetOrganizerWithCommonName("mailto:organizer@example.test", "Meeting Organizer");
        builder.AddAttendeeWithDetails("mailto:reader@example.test", "Reader, Example", "REQ-PARTICIPANT", "NEEDS-ACTION", true, true);
        builder.SetSequence(3);
        builder.SetStatus("CONFIRMED");
        builder.AddTimeZone(TimeZone());
        builder.AddComponentExtension(new EmailCalendarPropertyOperations().CreateText("X-LONG-NOTE", "Résumé planning with a deliberately long Unicode description that must be folded without splitting a character."));
        EmailCalendar calendar = builder.Build();

        byte[] written = new EmailCalendarWriter().WriteWithCalendar(calendar);
        AssertPhysicalLinesAreBounded(written, 75);
        MimeEntity entity = new EmailCalendarWriter().CreateImipEntityWithCalendar(calendar);
        EmailCalendarParseResult reparsed = new EmailCalendarParser().ParseWithEntity(entity);
        EmailCalendarComponent calendarEvent = reparsed.GetCalendar()!.GetEvents().Single();

        Assert.That(reparsed.IsSuccess(), Is.True);
        Assert.That(reparsed.IsImip(), Is.True);
        Assert.That(calendarEvent.GetUid(), Is.EqualTo("generated-1@example.test"));
        Assert.That(calendarEvent.GetStart()!.GetTimeZoneId(), Is.EqualTo("America/Los_Angeles"));
        Assert.That(calendarEvent.GetAttendees().Single().GetCommonName(), Is.EqualTo("Reader, Example"));
        Assert.That(calendarEvent.GetFirstProperty("X-LONG-NOTE")!.GetTextValue(), Does.StartWith("Résumé planning"));
        Assert.That(Encoding.UTF8.GetString(written), Does.Contain("\r\n "));
    }

    [Test]
    public void PreservesQuotedExtensionParametersAcrossCanonicalGeneration()
    {
        string source = FullCalendar().Replace(
            "X-TRACE-ID:trace-42\r\n",
            "X-TRACE-ID;X-LABEL=\"alpha,beta\";X-SCOPE=private:trace-42\r\n");
        EmailCalendar parsed = new EmailCalendarParser().ParseWithInputUsingString(source).GetCalendar()!;
        byte[] written = new EmailCalendarWriter().WriteWithCalendar(parsed!);
        EmailCalendar reparsed = new EmailCalendarParser().ParseWithInputUsingReadOnlySpan(written).GetCalendar()!;
        EmailCalendarProperty extension = reparsed!.GetEvents().Single().GetFirstProperty("X-TRACE-ID")!;

        Assert.That(extension!.GetFirstParameter("X-LABEL")!.GetValueCount(), Is.EqualTo(1));
        Assert.That(extension.GetFirstParameter("X-LABEL")!.GetValueAt(0), Is.EqualTo("alpha,beta"));
        Assert.That(extension.GetFirstParameter("X-SCOPE")!.GetValueAt(0), Is.EqualTo("private"));
        Assert.That(Encoding.UTF8.GetString(written), Does.Contain("X-LABEL=\"alpha,beta\""));
    }

    [Test]
    public void UnfoldsBeforeUtf8DecodingWhenAProducerSplitsAMultibyteCharacter()
    {
        byte[] prefix = Encoding.ASCII.GetBytes(
            "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Example//EN\r\n"
            + "BEGIN:VEVENT\r\nUID:utf8@example.test\r\nDTSTAMP:20260810T200000Z\r\nSUMMARY:Caf");
        byte[] suffix = Encoding.ASCII.GetBytes("\r\nEND:VEVENT\r\nEND:VCALENDAR\r\n");
        byte[] source = prefix.Concat(new byte[] { 0xc3, 13, 10, 32, 0xa9 }).Concat(suffix).ToArray();

        EmailCalendarParseResult result = new EmailCalendarParser().ParseWithInputUsingReadOnlySpan(source);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetCalendar()!.GetEvents().Single().GetFirstProperty("SUMMARY")!.GetTextValue(), Is.EqualTo("Café"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.CalendarUtf8Invalid), Is.False);
    }

    [Test]
    public void FindsCalendarInsideMessageMimeTree()
    {
        string source = "Content-Type: multipart/alternative; boundary=a\r\n\r\n"
            + "--a\r\nContent-Type: text/plain\r\n\r\nInvitation\r\n"
            + "--a\r\nContent-Type: text/calendar; method=REQUEST; charset=UTF-8\r\n\r\n"
            + FullCalendar()
            + "--a--\r\n";
        EmailParseResult message = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.UTF8.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());

        EmailCalendarParseResult result = new EmailCalendarParser().ParseWithMessage(message.GetMessage()!);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.IsImip(), Is.True);
        Assert.That(result.GetCalendar()!.GetEvents().Single().GetUid(), Is.EqualTo("meeting-7@example.test"));
    }

    [Test]
    public void EnforcesParseAndWriteLimitsAndClonesOptions()
    {
        EmailCalendarParseOptions parseOptions = new EmailCalendarParseOptions();
        parseOptions.SetMaximumCalendarBytes(10);
        EmailLimitException inputLimit = Assert.Throws<EmailLimitException>(() => new EmailCalendarParser().ParseWithInputAndOptionsUsingStringAndEmailCalendarParseOptions(FullCalendar(), parseOptions));
        Assert.That(inputLimit.GetLimitName(), Is.EqualTo("MaxCalendarBytes"));

        parseOptions.SetMaximumCalendarBytes(EmailCalendarParseOptionsConstants.DefaultMaximumCalendarBytes);
        parseOptions.SetMaximumProperties(2);
        EmailLimitException propertyLimit = Assert.Throws<EmailLimitException>(() => new EmailCalendarParser().ParseWithInputAndOptionsUsingStringAndEmailCalendarParseOptions(FullCalendar(), parseOptions));
        Assert.That(propertyLimit.GetLimitName(), Is.EqualTo("MaxCalendarProperties"));

        EmailCalendarWriteOptions writeOptions = new EmailCalendarWriteOptions();
        writeOptions.SetMaximumOutputBytes(20);
        EmailLimitException outputLimit = Assert.Throws<EmailLimitException>(() => new EmailCalendarWriter().WriteWithCalendarAndOptions(new EmailCalendarParser().ParseWithInputUsingString(FullCalendar()).GetCalendar()!, writeOptions));
        Assert.That(outputLimit.GetLimitName(), Is.EqualTo("MaxCalendarOutputBytes"));

        EmailCalendarParseOptions clone = parseOptions.Clone();
        parseOptions.SetMaximumComponentDepth(2);
        Assert.That(clone.GetMaximumComponentDepth(), Is.EqualTo(16));
        Assert.Throws<ArgumentNullException>(() => new EmailCalendarParser().ParseWithEntity((MimeEntity)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailCalendarWriter().WriteWithCalendar(null!));
        Assert.Throws<ArgumentException>(() => writeOptions.SetCharset("utf-16"));
    }

    static string FullCalendar()
    {
        return "BEGIN:VCALENDAR\r\n"
            + "PRODID:-//Example Corp//Calendar 1.0//EN\r\n"
            + "VERSION:2.0\r\nMETHOD:REQUEST\r\n"
            + "BEGIN:VTIMEZONE\r\nTZID:America/Los_Angeles\r\n"
            + "BEGIN:STANDARD\r\nDTSTART:20251102T020000\r\nTZOFFSETFROM:-0700\r\nTZOFFSETTO:-0800\r\nEND:STANDARD\r\n"
            + "END:VTIMEZONE\r\n"
            + "BEGIN:VEVENT\r\nUID:meeting-7@example.test\r\nDTSTAMP:20260810T200000Z\r\n"
            + "DTSTART;TZID=America/Los_Angeles:20260811T090000\r\n"
            + "DTEND;TZID=America/Los_Angeles:20260811T100000\r\n"
            + "ORGANIZER;CN=Organizer:mailto:organizer@example.test\r\n"
            + "ATTENDEE;CN=\"Doe, Jane\";ROLE=REQ-PARTICIPANT;PARTSTAT=ACCEPTED;RSVP=TRUE:mailto:jane@example.test\r\n"
            + "SEQUENCE:2\r\nSTATUS:CONFIRMED\r\n"
            + "DESCRIPTION:First line continued \r\n text\\nSecond line\r\n"
            + "X-TRACE-ID:trace-42\r\n"
            + "END:VEVENT\r\nEND:VCALENDAR\r\n";
    }

    static EmailCalendarComponent TimeZone()
    {
        List<EmailCalendarProperty> standardProperties = new List<EmailCalendarProperty>();
        standardProperties.Add(new EmailCalendarPropertyOperations().CreateWithNameAndRawValue("DTSTART", "20251102T020000"));
        standardProperties.Add(new EmailCalendarPropertyOperations().CreateWithNameAndRawValue("TZOFFSETFROM", "-0700"));
        standardProperties.Add(new EmailCalendarPropertyOperations().CreateWithNameAndRawValue("TZOFFSETTO", "-0800"));
        EmailCalendarComponent standard = new EmailCalendarComponent("STANDARD", standardProperties, new List<EmailCalendarComponent>());
        List<EmailCalendarProperty> zoneProperties = new List<EmailCalendarProperty>();
        zoneProperties.Add(new EmailCalendarPropertyOperations().CreateText("TZID", "America/Los_Angeles"));
        List<EmailCalendarComponent> children = new List<EmailCalendarComponent>();
        children.Add(standard);
        return new EmailCalendarComponent("VTIMEZONE", zoneProperties, children);
    }

    static void AssertPhysicalLinesAreBounded(byte[] bytes, int maximum)
    {
        int start = 0;
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] == 13 && index + 1 < bytes.Length && bytes[index + 1] == 10)
            {
                Assert.That(index - start, Is.LessThanOrEqualTo(maximum));
                start = index + 2;
                index++;
            }

            index++;
        }

        Assert.That(start, Is.EqualTo(bytes.Length));
    }
}
