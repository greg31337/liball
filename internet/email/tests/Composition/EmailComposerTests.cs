using System.Text;
using email;

namespace email.Tests;

public class EmailComposerTests
{
    [Test]
    public void BuildCreatesAddressHeadersAndSeparateDeduplicatedEnvelope()
    {
        EmailAddress author = ParseAddress("author@example.test");
        EmailAddress agent = ParseAddress("agent@example.test");
        EmailAddress reply = ParseAddress("reply@example.test");
        EmailAddress visible = ParseAddress("Visible Person <visible@example.test>");
        EmailAddress copied = ParseAddress("copied@example.test");
        EmailAddress hidden = ParseAddress("hidden@example.test");
        EmailAddress explicitRecipient = ParseAddress("explicit@example.test");
        EmailComposer composer = new EmailComposerOperations().CreateWith();
        composer.AddFrom(author);
        composer.AddFrom(ParseAddress("coauthor@example.test"));
        composer.SetSender(agent);
        composer.AddReplyTo(reply);
        composer.AddTo(visible);
        composer.AddCc(copied);
        composer.AddBcc(hidden);
        composer.AddEnvelopeRecipient(hidden);
        composer.AddEnvelopeRecipient(explicitRecipient);

        EmailMessage message = composer.Build();

        Assert.That(HeaderText(message, "From"), Is.EqualTo("author@example.test, coauthor@example.test"));
        Assert.That(HeaderText(message, "Sender"), Is.EqualTo("agent@example.test"));
        Assert.That(HeaderText(message, "Reply-To"), Is.EqualTo("reply@example.test"));
        Assert.That(HeaderText(message, "To"), Is.EqualTo("Visible Person <visible@example.test>"));
        Assert.That(HeaderText(message, "Cc"), Is.EqualTo("copied@example.test"));
        Assert.That(message.GetHeaders()!.Contains("Bcc"), Is.False);
        Assert.That(message.GetEnvelope()!.GetSender(), Is.SameAs(agent));
        Assert.That(EnvelopeLocalParts(message), Is.EqualTo(new[] { "visible", "copied", "hidden", "explicit" }));
        Assert.That(Text(new EmailWriter().ToArrayWithMessageAndOptions(message, new EmailWriteOptionsOperations().CreateExactPreservation())), Does.Not.Contain("hidden@example.test"));
    }

    [Test]
    public void BuildFormatsSubjectDateIdentifiersPriorityAndCustomFields()
    {
        EmailComposer composer = RequiredComposer();
        composer.SetSubject("Quarterly café update");
        composer.SetDate(ParseDate("Fri, 21 Nov 1997 09:55:06 -0600"));
        composer.SetMessageId(ParseIdentifier("<message@example.test>"));
        composer.SetInReplyTo(ParseIdentifier("<parent@example.test>"));
        composer.AddReference(ParseIdentifier("<root@example.test>"));
        composer.AddReference(ParseIdentifier("<parent@example.test>"));
        composer.SetPriority(EmailPriorityKind.Urgent);
        composer.AddCustomField("X-Campaign", "launch");
        composer.AddCustomField("X-Note", "naïve");

        EmailMessage message = composer.Build();

        Assert.That(HeaderText(message, "Date"), Is.EqualTo("Fri, 21 Nov 1997 09:55:06 -0600"));
        Assert.That(HeaderText(message, "Message-ID"), Is.EqualTo("<message@example.test>"));
        Assert.That(HeaderText(message, "In-Reply-To"), Is.EqualTo("<parent@example.test>"));
        Assert.That(HeaderText(message, "References"), Is.EqualTo("<root@example.test> <parent@example.test>"));
        Assert.That(message.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("Quarterly café update"));
        Assert.That(HeaderText(message, "Priority"), Is.EqualTo("urgent"));
        Assert.That(HeaderText(message, "X-Priority"), Is.EqualTo("1"));
        Assert.That(HeaderText(message, "Importance"), Is.EqualTo("high"));
        Assert.That(HeaderText(message, "MIME-Version"), Is.EqualTo("1.0"));
        Assert.That(HeaderText(message, "X-Campaign"), Is.EqualTo("launch"));
        Assert.That(message.GetHeaders()!.GetFirst("X-Note")!.GetDecodedValue(), Is.EqualTo("naïve"));
        Assert.That(composer.Validate().IsValid(), Is.True);
    }

    [Test]
    public void ValidateReportsMissingRequiredMetadataAndEnvelopeRecipient()
    {
        EmailComposer composer = new EmailComposerOperations().CreateWith();

        EmailValidationResult result = composer.Validate();

        Assert.That(result.IsValid(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderValidationRequiredFieldMissing), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.CompositionInvalid), Is.True);

        EmailComposerOptions relaxedOptions = new EmailComposerOptions();
        relaxedOptions.SetRequireFrom(false);
        relaxedOptions.SetRequireDate(false);
        relaxedOptions.SetRequireRecipient(false);
        EmailComposer relaxed = new EmailComposerOperations().CreateWithOptions(relaxedOptions);
        Assert.That(relaxed.Validate().IsValid(), Is.True);
    }

    [Test]
    public void SafeSettersRejectInjectionManagedFieldsAndInvalidStructuredValues()
    {
        EmailComposer composer = new EmailComposerOperations().CreateWith();
        EmailMessageId malformed = new EmailMessageId("bad space", "example.test", false, new byte[0], new EmailSourcePosition(0, 1, 1), false, false);

        Assert.Throws<ArgumentException>(() => composer.SetSubject("safe\r\nBcc: injected@example.test"));
        Assert.Throws<ArgumentException>(() => composer.AddCustomField("X-Test\r\nBcc", "value"));
        Assert.Throws<ArgumentException>(() => composer.AddCustomField("X-Test", "value\nInjected: true"));
        Assert.Throws<ArgumentException>(() => composer.AddCustomField("Subject", "bypass"));
        Assert.Throws<ArgumentException>(() => composer.AddCustomField("Content-Type", "text/html"));
        Assert.Throws<ArgumentException>(() => composer.SetMessageId(malformed));
        Assert.Throws<ArgumentOutOfRangeException>(() => composer.SetPriority(EmailPriorityKind.Unknown));
        Assert.Throws<ArgumentNullException>(() => composer.AddTo(null!));
    }

    [Test]
    public void OptionsEnforceSmtpUtf8AndConfiguredLimitsUsingDefensiveSnapshot()
    {
        EmailAddress international = CreateAddress("müller", "example.test");
        Assert.Throws<ArgumentException>(() => new EmailComposerOperations().CreateWith().AddTo(international));

        EmailComposerOptions internationalOptions = new EmailComposerOptions();
        internationalOptions.SetAllowSmtpUtf8(true);
        EmailComposer internationalComposer = new EmailComposerOperations().CreateWithOptions(internationalOptions);
        internationalOptions.SetAllowSmtpUtf8(false);
        internationalComposer.SetFrom(international);
        internationalComposer.AddTo(international);
        internationalComposer.SetDate(ParseDate("Fri, 21 Nov 1997 09:55:06 -0600"));
        EmailMessage internationalMessage = internationalComposer.Build();
        Assert.That(HeaderText(internationalMessage, "From"), Is.EqualTo("müller@example.test"));
        Assert.That(internationalComposer.Validate().IsValid(), Is.True);

        EmailComposerOptions limitedOptions = new EmailComposerOptions();
        limitedOptions.SetMaximumAddressCount(1);
        limitedOptions.SetMaximumCustomFieldCount(1);
        EmailComposer limited = new EmailComposerOperations().CreateWithOptions(limitedOptions);
        limited.AddTo(ParseAddress("one@example.test"));
        Assert.Throws<EmailLimitException>(() => limited.AddCc(ParseAddress("two@example.test")));
        limited.AddCustomField("X-One", "12345");
        Assert.Throws<EmailLimitException>(() => limited.AddCustomField("X-Two", "x"));

        EmailComposerOptions headerLimitedOptions = new EmailComposerOptions();
        headerLimitedOptions.SetMaximumHeaderValueBytes(5);
        EmailComposer headerLimited = new EmailComposerOperations().CreateWithOptions(headerLimitedOptions);
        Assert.Throws<EmailLimitException>(() => headerLimited.SetSubject("123456"));
        Assert.Throws<ArgumentOutOfRangeException>(() => limitedOptions.SetMaximumAddressCount(0));
    }

    [Test]
    public void RepeatedBuildsAreIndependentAndClearingMetadataTakesEffect()
    {
        EmailComposer composer = RequiredComposer();
        composer.SetSubject("first");
        composer.SetPriority(EmailPriorityKind.NonUrgent);
        composer.AddCustomField("X-Test", "one");
        EmailMessage first = composer.Build();
        first.GetHeaders()!.GetFirst("Subject")!.SetValue("mutated");

        composer.ClearSubject();
        composer.ClearPriority();
        Assert.That(composer.RemoveCustomFields("x-test"), Is.EqualTo(1));
        EmailMessage second = composer.Build();

        Assert.That(HeaderText(first, "Subject"), Is.EqualTo("mutated"));
        Assert.That(second.GetHeaders()!.Contains("Subject"), Is.False);
        Assert.That(second.GetHeaders()!.Contains("Priority"), Is.False);
        Assert.That(second.GetHeaders()!.Contains("X-Test"), Is.False);
        Assert.That(first.GetBody(), Is.Not.SameAs(second.GetBody()));
        Assert.That(first.GetEnvelope(), Is.Not.SameAs(second.GetEnvelope()));
    }

    static EmailComposer RequiredComposer()
    {
        EmailComposer composer = new EmailComposerOperations().CreateWith();
        composer.SetFrom(ParseAddress("author@example.test"));
        composer.AddTo(ParseAddress("recipient@example.test"));
        composer.SetDate(ParseDate("Fri, 21 Nov 1997 09:55:06 -0600"));
        return composer;
    }

    static EmailAddress ParseAddress(string value)
    {
        EmailAddressParseResult result = new EmailAddressParser().ParseMailbox(Encoding.UTF8.GetBytes(value), EmailAddressParseMode.Strict);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetMailbox()!;
    }

    static EmailAddress CreateAddress(string localPart, string domain)
    {
        return new EmailAddress(null, localPart, domain, new byte[0], Encoding.UTF8.GetBytes(localPart), Encoding.UTF8.GetBytes(domain), new EmailSourcePosition(0, 1, 1), false, true, false, new List<string>());
    }

    static EmailDateTime ParseDate(string value)
    {
        EmailDateParseResult result = new EmailDateParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(value), EmailDateParseMode.Strict);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetValue()!;
    }

    static EmailMessageId ParseIdentifier(string value)
    {
        EmailMessageIdParseResult result = new EmailMessageIdParser().ParseMessageIdWithInputAndMode(Encoding.ASCII.GetBytes(value), EmailMessageIdParseMode.Strict);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetIdentifier()!;
    }

    static string HeaderText(EmailMessage message, string name)
    {
        return Encoding.UTF8.GetString(message.GetHeaders()!.GetFirst(name)!.GetUnfoldedValue()).Trim();
    }

    static List<string> EnvelopeLocalParts(EmailMessage message)
    {
        List<string> values = new List<string>();
        int index = 0;
        while (index < message.GetEnvelope()!.GetRecipientCount())
        {
            values.Add(message.GetEnvelope()!.GetRecipientAt(index).GetLocalPart());
            index++;
        }

        return values;
    }

    static string Text(byte[] value)
    {
        return Encoding.UTF8.GetString(value);
    }
}
