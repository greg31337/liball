using System.Text;
using email;

namespace email.Tests;

public class EmailBccWriterTests
{
    [Test]
    public void CanonicalOutputOmitsBlindHeadersWithoutMutatingMessageOrEnvelope()
    {
        EmailMessage message = Parse("To: visible@example.test\r\nBcc: hidden@example.test\r\nResent-Bcc: resent-hidden@example.test\r\nSubject: private recipients\r\n\r\nbody");
        EmailEnvelope projected = new EmailEnvelopeOperations().CreateRecipientProjection(message.GetHeaders()!);
        message.GetEnvelope()!.AddRecipient(projected.GetRecipientAt(1));

        EmailWriteResult result = new EmailWriter().WriteMessageWithMessageAndOptions(message, new EmailWriteOptionsOperations().CreateCanonicalInternet());
        string output = Text(result.GetBytes());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(output, Does.Contain("To: visible@example.test\r\n"));
        Assert.That(output, Does.Not.Contain("Bcc:"));
        Assert.That(output, Does.Not.Contain("hidden@example.test"));
        Assert.That(message.GetHeaders()!.Contains("Bcc"), Is.True);
        Assert.That(message.GetHeaders()!.Contains("Resent-Bcc"), Is.True);
        Assert.That(message.GetEnvelope()!.GetRecipientAt(0).GetLocalPart(), Is.EqualTo("hidden"));
    }

    [Test]
    public void MinimalOutputRemovesBccWhilePreservingOtherSourceBytes()
    {
        const string source = "x-Keep: exact\r\nBcc: hidden@example.test\r\nTo: visible@example.test\r\nResent-Bcc: resent@example.test\r\n\r\nbody\noriginal ending";
        EmailMessage message = Parse(source);

        EmailWriteResult result = new EmailWriter().WriteMessageWithMessage(message);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(Text(result.GetBytes()), Is.EqualTo("x-Keep: exact\r\nTo: visible@example.test\r\n\r\nbody\noriginal ending"));
        Assert.That(result.GetCopiedByteCount(), Is.GreaterThan(0));
        Assert.That(result.GetRegeneratedByteCount(), Is.GreaterThan(0));
        Assert.That(message.GetHeaders()!.GetCountNamed("Bcc"), Is.EqualTo(1));
    }

    [Test]
    public void StreamOutputUsesTheSameBlindRecipientPolicy()
    {
        EmailMessage message = Parse("To: visible@example.test\r\nBcc: hidden@example.test\r\n\r\nbody");
        EmailWriteOptions options = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        MemoryStream output = new MemoryStream();

        EmailStreamWriteResult result = new EmailWriter().WriteMessageWithMessageAndOutputAndOptionsUsingEmailMessageAndStreamAndEmailWriteOptions(message, output, options);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(Text(output.ToArray()), Does.Not.Contain("hidden@example.test"));
        Assert.That(result.GetOutputByteCount(), Is.EqualTo(output.Length));
    }

    [Test]
    public void ExactArchiveAndExplicitCanonicalPolicyCanRetainBcc()
    {
        const string source = "Bcc: archive@example.test\r\n\r\nbody";
        EmailMessage exactMessage = Parse(source);
        EmailWriteOptions rejectingExact = new EmailWriteOptions();
        rejectingExact.SetMode(EmailWriteMode.ExactPreservation);
        EmailWriteOptions includingCanonical = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        includingCanonical.SetIncludeBlindRecipientHeaders(true);

        EmailWriteResult exact = new EmailWriter().WriteMessageWithMessageAndOptions(exactMessage, new EmailWriteOptionsOperations().CreateExactPreservation());
        EmailWriteResult rejected = new EmailWriter().WriteMessageWithMessageAndOptions(exactMessage, rejectingExact);
        EmailWriteResult canonical = new EmailWriter().WriteMessageWithMessageAndOptions(exactMessage, includingCanonical);

        Assert.That(Text(exact.GetBytes()), Is.EqualTo(source));
        Assert.That(rejected.IsSuccess(), Is.False);
        Assert.That(rejected.GetBytes(), Is.Empty);
        Assert.That(Text(canonical.GetBytes()), Does.Contain("Bcc: archive@example.test\r\n"));
    }

    [Test]
    public void CanonicalNestedMessagesAlsoOmitBlindRecipientHeaders()
    {
        EmailMessage nested = Parse("Subject: nested\r\nBcc: nested-hidden@example.test\r\n\r\ninside");
        EmailMessage outer = new EmailMessageOperations().CreateWith();
        outer.SetBody(new EmailEntityFactory().CreateNestedMessage(nested));

        EmailWriteResult result = new EmailWriter().WriteMessageWithMessageAndOptions(outer, new EmailWriteOptionsOperations().CreateCanonicalInternet());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(Text(result.GetBytes()), Does.Not.Contain("nested-hidden@example.test"));
        Assert.That(nested.GetHeaders()!.Contains("Bcc"), Is.True);
    }

    [Test]
    public void EnvelopeProjectionIncludesVisibleBlindGroupedAndResentRecipients()
    {
        EmailMessage message = Parse("To: one@example.test\r\nCc: Team: two@example.test, three@example.test;\r\nBcc: hidden@example.test\r\nResent-Bcc: resent@example.test\r\n\r\nbody");

        EmailEnvelope envelope = new EmailEnvelopeOperations().CreateRecipientProjection(message.GetHeaders()!);

        Assert.That(envelope.GetRecipientCount(), Is.EqualTo(5));
        Assert.That(LocalParts(envelope), Is.EqualTo(new[] { "one", "two", "three", "hidden", "resent" }));
        Assert.That(message.GetEnvelope()!.GetRecipientCount(), Is.Zero);
    }

    static List<string> LocalParts(EmailEnvelope envelope)
    {
        List<string> values = new List<string>();
        int index = 0;
        while (index < envelope.GetRecipientCount())
        {
            values.Add(envelope.GetRecipientAt(index).GetLocalPart());
            index++;
        }

        return values;
    }

    static EmailMessage Parse(string value)
    {
        return new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(value), new EmailParseOptionsOperations().CreatePreserving()).GetMessage()!;
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
