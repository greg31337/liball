using System.Text;
using email;

namespace email.Tests;

public class EmailWriterPublicSurfaceCompletionTests
{
    [Test]
    public void MessageConvenienceAndStreamOverloadsProduceSuccessfulEquivalentOutput()
    {
        EmailMessage message = Message();
        EmailWriter writer = new EmailWriter();
        EmailWriteOptions options = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        byte[] expected = writer.ToArrayWithMessage(message);
        byte[] canonical = writer.ToArrayWithMessageAndOptions(message, options);

        Assert.That(expected, Is.Not.Empty);
        Assert.That(canonical, Is.Not.Empty);
        Assert.That(writer.WriteWithMessage(message).GetBytes(), Is.EqualTo(expected));
        Assert.That(writer.WriteWithMessageAndOptions(message, options).GetBytes(), Is.EqualTo(canonical));

        MemoryStream compatibilityOutput = new MemoryStream();
        EmailStreamWriteResult compatibility = writer.WriteWithMessageAndOutput(message, compatibilityOutput);
        MemoryStream compatibilityOptionsOutput = new MemoryStream();
        EmailStreamWriteResult compatibilityOptions = writer.WriteWithMessageAndOutputAndOptions(message, compatibilityOptionsOutput, options);
        MemoryStream messageOutput = new MemoryStream();
        EmailStreamWriteResult streamed = writer.WriteMessageWithMessageAndOutputUsingEmailMessageAndStream(message, messageOutput);

        Assert.That(compatibility.IsSuccess(), Is.True);
        Assert.That(compatibilityOptions.IsSuccess(), Is.True);
        Assert.That(streamed.IsSuccess(), Is.True);
        Assert.That(compatibilityOutput.ToArray(), Is.EqualTo(expected));
        Assert.That(compatibilityOptionsOutput.ToArray(), Is.EqualTo(canonical));
        Assert.That(messageOutput.ToArray(), Is.EqualTo(expected));
        AssertStreamMetadata(streamed);
    }

    [Test]
    public void MessageCountingHashingAndSmtpSinkOverloadsReportTheirOutput()
    {
        EmailMessage message = Message();
        EmailWriter writer = new EmailWriter();
        EmailWriteOptions options = new EmailWriteOptionsOperations().CreateCanonicalInternet();

        EmailCountingStream counterWithOptions = new EmailCountingStream();
        EmailStreamWriteResult countedWithOptions = writer.WriteMessageWithMessageAndOutputAndOptionsUsingEmailMessageAndEmailCountingStreamAndEmailWriteOptions(message, counterWithOptions, options);
        EmailCountingStream counter = new EmailCountingStream();
        EmailStreamWriteResult counted = writer.WriteMessageWithMessageAndOutputUsingEmailMessageAndEmailCountingStream(message, counter);
        EmailHashingStream hashing = new EmailHashingStream(EmailHashAlgorithm.Sha256);
        EmailStreamWriteResult hashed = writer.WriteMessageWithMessageAndOutputUsingEmailMessageAndEmailHashingStream(message, hashing);
        EmailHashResult digest = hashing.Complete();
        MemoryStream smtpBytes = new MemoryStream();
        SmtpDataWriter smtp = new SmtpDataWriter(smtpBytes);
        EmailStreamWriteResult smtpResult = writer.WriteMessageWithMessageAndOutputUsingEmailMessageAndSmtpDataWriter(message, smtp);
        smtp.Complete();

        Assert.That(countedWithOptions.IsSuccess(), Is.True);
        Assert.That(counted.IsSuccess(), Is.True);
        Assert.That(hashed.IsSuccess(), Is.True);
        Assert.That(smtpResult.IsSuccess(), Is.True);
        Assert.That(counter.GetByteCount(), Is.GreaterThan(0));
        Assert.That(counterWithOptions.GetByteCount(), Is.GreaterThan(0));
        Assert.That(digest.GetHashedByteCount(), Is.EqualTo(counter.GetByteCount()));
        Assert.That(smtp.GetSourceByteCount(), Is.EqualTo(counter.GetByteCount()));
        Assert.That(smtp.GetOutputByteCount(), Is.EqualTo(smtpBytes.Length));
        Assert.That(smtp.GetOutputByteCount(), Is.GreaterThan(smtp.GetSourceByteCount()));
    }

    [Test]
    public void EntityStreamCountingHashingAndSmtpOverloadsReportTheirOutput()
    {
        MimeEntity entity = new EmailEntityFactory().CreateTextWithValue("body");
        EmailWriter writer = new EmailWriter();
        EmailWriteOptions options = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        byte[] expected = writer.ToArrayWithEntityAndOptions(entity, options);

        MemoryStream streamOutput = new MemoryStream();
        EmailStreamWriteResult streamed = writer.WriteEntityWithEntityAndOutputUsingMimeEntityAndStream(entity, streamOutput);
        EmailCountingStream counter = new EmailCountingStream();
        EmailStreamWriteResult counted = writer.WriteEntityWithEntityAndOutputUsingMimeEntityAndEmailCountingStream(entity, counter);
        EmailHashingStream hashing = new EmailHashingStream(EmailHashAlgorithm.Sha256);
        EmailStreamWriteResult hashed = writer.WriteEntityWithEntityAndOutputUsingMimeEntityAndEmailHashingStream(entity, hashing);
        hashing.Complete();
        MemoryStream smtpBytes = new MemoryStream();
        SmtpDataWriter smtp = new SmtpDataWriter(smtpBytes);
        EmailStreamWriteResult smtpResult = writer.WriteEntityWithEntityAndOutputUsingMimeEntityAndSmtpDataWriter(entity, smtp);
        smtp.Complete();
        MemoryStream smtpOptionBytes = new MemoryStream();
        SmtpDataWriter smtpOptions = new SmtpDataWriter(smtpOptionBytes);
        EmailStreamWriteResult smtpOptionResult = writer.WriteEntityWithEntityAndOutputAndOptionsUsingMimeEntityAndSmtpDataWriterAndEmailWriteOptions(entity, smtpOptions, options);
        smtpOptions.Complete();

        Assert.That(streamOutput.ToArray(), Is.EqualTo(expected));
        Assert.That(streamed.IsSuccess(), Is.True);
        Assert.That(counted.IsSuccess(), Is.True);
        Assert.That(hashed.IsSuccess(), Is.True);
        Assert.That(smtpResult.IsSuccess(), Is.True);
        Assert.That(smtpOptionResult.IsSuccess(), Is.True);
        Assert.That(counter.GetByteCount(), Is.EqualTo(expected.Length));
    }

    [Test]
    public void CountingTextAndBoundaryConveniencesExposeResultMetadata()
    {
        EmailMessage message = Message();
        MimeEntity entity = message.GetBody()!;
        EmailWriter writer = new EmailWriter();
        EmailWriteOptions writeOptions = new EmailWriteOptionsOperations().CreateCanonicalInternet();

        Assert.That(writer.CountBytesWithMessage(message), Is.GreaterThan(0));
        Assert.That(writer.CountBytesWithMessageAndOptions(message, writeOptions), Is.GreaterThan(0));
        Assert.That(writer.CountBytesWithEntity(entity), Is.EqualTo(writer.CountBytesWithEntityAndOptions(entity, writeOptions)));
        EmailTextWriteResult text = writer.WriteTextWithMessageAndOptions(message, new EmailTextWriteOptions());
        Assert.That(text.HasText(), Is.True);
        Assert.That(text.HasFatalError(), Is.False);

        EmailWriteOptions deterministic = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        deterministic.SetDeterministicOutput(true);
        deterministic.SetDeterministicSeed(Bytes("boundary"));
        EmailBoundaryGenerationResult boundary = new EmailBoundaryGenerator().GenerateWithOptions(deterministic);
        Assert.That(boundary.GetBoundary(), Is.Not.Empty);
        Assert.That(boundary.IsDeterministic(), Is.True);
    }

    static void AssertStreamMetadata(EmailStreamWriteResult result)
    {
        Assert.That(result.GetMode(), Is.EqualTo(EmailWriteMode.MinimalRegeneration));
        Assert.That(result.GetSourceByteCount(), Is.GreaterThanOrEqualTo(0));
        Assert.That(result.GetCopiedRangeCount(), Is.GreaterThanOrEqualTo(0));
        Assert.That(result.GetRegeneratedRangeCount(), Is.GreaterThanOrEqualTo(0));
        Assert.That(result.GetOutputByteCount(), Is.EqualTo(result.GetCopiedByteCount() + result.GetRegeneratedByteCount()));
    }

    static EmailMessage Message()
    {
        return new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(
            Bytes("Subject: parity\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Transfer-Encoding: 7bit\r\n\r\nbody"),
            new EmailParseOptionsOperations().CreatePreserving()).GetMessage()!;
    }

    static byte[] Bytes(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }
}
