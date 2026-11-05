using System.Text;
using email;

namespace email.Tests;

public class EmailResentBuilderTests
{
    [Test]
    public void BuildPrependsACompleteBlockAndPreservesOriginalMessageContent()
    {
        EmailMessage original = ParseMessage(
            "Resent-Date: Thu, 20 Nov 1997 09:55:06 -0600\r\n"
            + "Resent-From: older@example.test\r\n"
            + "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\n"
            + "From: author@example.test\r\n"
            + "To: first@example.test\r\n"
            + "Bcc: original-hidden@example.test\r\n"
            + "Subject: original\r\n\r\n"
            + "unchanged body");
        original.GetEnvelope()!.SetSender(Address("old-envelope@example.test"));
        original.GetEnvelope()!.AddRecipient(Address("old-recipient@example.test"));
        EmailContent originalContent = original.GetBody()!.GetContent()!;
        EmailResentBuilder builder = new EmailResentBuilderOperations().CreateWithOriginal(original);
        builder.SetDate(Date("Sat, 22 Nov 1997 10:00:00 -0600"));
        builder.SetFrom(Address("resender@example.test"));
        builder.AddTo(Address("new@example.test"));
        builder.AddCc(Address("copy@example.test"));
        builder.AddBcc(Address("hidden@example.test"));
        builder.AddEnvelopeRecipient(Address("new@example.test"));
        builder.AddEnvelopeRecipient(Address("extra@example.test"));
        builder.SetMessageId(Identifier("<resent@example.test>"));

        EmailMessage resent = builder.Build();

        Assert.That(resent.GetHeaders()!.GetAt(0).GetName(), Is.EqualTo("Resent-Date"));
        Assert.That(resent.GetHeaders()!.GetAt(1).GetName(), Is.EqualTo("Resent-From"));
        Assert.That(resent.GetHeaders()!.GetAt(2).GetName(), Is.EqualTo("Resent-To"));
        Assert.That(resent.GetHeaders()!.GetAt(3).GetName(), Is.EqualTo("Resent-Cc"));
        Assert.That(resent.GetHeaders()!.GetAt(4).GetName(), Is.EqualTo("Resent-Bcc"));
        Assert.That(resent.GetHeaders()!.GetAt(5).GetName(), Is.EqualTo("Resent-Message-ID"));
        Assert.That(resent.GetHeaders()!.GetAt(6).GetDecodedValue(), Does.Contain("Thu, 20 Nov 1997"));
        Assert.That(new EmailHeaderValidator().ValidateWithHeaders(resent.GetHeaders()!).IsValid(), Is.True);
        Assert.That(resent.GetBody(), Is.Not.SameAs(original.GetBody()));
        Assert.That(resent.GetBody().GetContent(), Is.SameAs(originalContent));
        Assert.That(Encoding.ASCII.GetString(resent.GetBody().GetContent().Materialize(100)), Is.EqualTo("unchanged body"));
        Assert.That(resent.GetEnvelope()!.GetSender()!.GetLocalPart(), Is.EqualTo("resender"));
        Assert.That(EnvelopeLocalParts(resent), Is.EqualTo(new List<string> { "new", "copy", "hidden", "extra" }));
        Assert.That(original.GetHeaders()!.GetAt(0).GetDecodedValue(), Does.Contain("Thu, 20 Nov 1997"));
        Assert.That(original.GetHeaders()!.GetCountNamed("Resent-Date"), Is.EqualTo(1));
        Assert.That(original.GetEnvelope()!.GetRecipientAt(0).GetLocalPart(), Is.EqualTo("old-recipient"));

        string wire = Encoding.ASCII.GetString(new EmailWriter().ToArrayWithMessageAndOptions(resent, new EmailWriteOptionsOperations().CreateCanonicalInternet()));
        Assert.That(wire, Does.Contain("Resent-To: new@example.test"));
        Assert.That(wire, Does.Not.Contain("Resent-Bcc:"));
        Assert.That(wire, Does.Not.Contain("\r\nBcc:"));
    }

    [Test]
    public void MultipleResentFromRequiresSenderAndUsesItForTheEnvelope()
    {
        EmailResentBuilder builder = RequiredBuilder();
        builder.AddFrom(Address("second@example.test"));
        Assert.Throws<InvalidOperationException>(() => builder.Build());

        builder.SetSender(Address("agent@example.test"));
        EmailMessage message = builder.Build();
        Assert.That(message.GetHeaders()!.GetAt(2).GetName(), Is.EqualTo("Resent-Sender"));
        Assert.That(message.GetEnvelope()!.GetSender()!.GetLocalPart(), Is.EqualTo("agent"));
        Assert.That(new EmailHeaderValidator().ValidateWithHeaders(message.GetHeaders()!).IsValid(), Is.True);

        EmailResentBuilder prohibited = RequiredBuilder();
        prohibited.SetSender(Address("agent@example.test"));
        Assert.Throws<InvalidOperationException>(() => prohibited.Build());
    }

    [Test]
    public void RequiredFieldsAndConfiguredLimitsAreEnforced()
    {
        EmailMessage original = ParseMessage("From: author@example.test\r\n\r\nbody");
        EmailResentBuilder missing = new EmailResentBuilderOperations().CreateWithOriginal(original);
        Assert.Throws<InvalidOperationException>(() => missing.Build());
        missing.SetDate(Date("Sat, 22 Nov 1997 10:00:00 -0600"));
        missing.SetFrom(Address("resender@example.test"));
        Assert.Throws<InvalidOperationException>(() => missing.Build());

        EmailResentOptions options = new EmailResentOptions();
        options.SetMaximumAddressCount(2);
        EmailResentBuilder limited = new EmailResentBuilderOperations().CreateWithOriginalAndOptions(original, options);
        limited.SetDate(Date("Sat, 22 Nov 1997 10:00:00 -0600"));
        limited.SetFrom(Address("resender@example.test"));
        limited.AddTo(Address("one@example.test"));
        Assert.Throws<EmailLimitException>(() => limited.AddCc(Address("two@example.test")));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumAddressCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumHeaderValueBytes(0));
        Assert.Throws<ArgumentNullException>(() => new EmailResentBuilderOperations().CreateWithOriginal(null!));
        Assert.Throws<ArgumentNullException>(() => new EmailResentBuilderOperations().CreateWithOriginalAndOptions(original, null!));
    }

    static EmailResentBuilder RequiredBuilder()
    {
        EmailMessage original = ParseMessage("Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: author@example.test\r\n\r\nbody");
        EmailResentBuilder builder = new EmailResentBuilderOperations().CreateWithOriginal(original);
        builder.SetDate(Date("Sat, 22 Nov 1997 10:00:00 -0600"));
        builder.SetFrom(Address("resender@example.test"));
        builder.AddTo(Address("recipient@example.test"));
        return builder;
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

    static EmailDateTime Date(string value)
    {
        EmailDateParseResult result = new EmailDateParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(value), EmailDateParseMode.Strict);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetValue()!;
    }

    static EmailMessageId Identifier(string value)
    {
        EmailMessageIdParseResult result = new EmailMessageIdParser().ParseMessageIdWithInputAndMode(Encoding.ASCII.GetBytes(value), EmailMessageIdParseMode.Strict);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetIdentifier()!;
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
}
