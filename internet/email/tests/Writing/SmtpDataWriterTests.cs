using System.Text;
using email;

namespace email.Tests;

public class SmtpDataWriterTests
{
    [Test]
    public void NormalizesLineEndingsDotStuffsAndTerminatesAcrossEverySplit()
    {
        byte[] source = Bytes("first\n.second\rthird\r\n..fourth\n");
        byte[] expected = Bytes("first\r\n..second\r\nthird\r\n...fourth\r\n.\r\n");
        int split = 0;
        while (split <= source.Length)
        {
            MemoryStream output = new MemoryStream();
            SmtpDataWriter writer = new SmtpDataWriterOperations().CreateWithOutputUsingStream(output);

            writer.Feed(new ReadOnlySpan<byte>(source, 0, split));
            writer.Feed(new ReadOnlySpan<byte>(source, split, source.Length - split));
            SmtpDataWriteResult result = writer.Complete();

            Assert.That(output.ToArray(), Is.EqualTo(expected), "split " + split);
            Assert.That(result.GetSourceByteCount(), Is.EqualTo(source.LongLength), "split " + split);
            Assert.That(result.GetDataByteCount(), Is.EqualTo(expected.LongLength - 3), "split " + split);
            Assert.That(result.GetOutputByteCount(), Is.EqualTo(expected.LongLength), "split " + split);
            Assert.That(result.GetDotStuffedLineCount(), Is.EqualTo(2), "split " + split);
            Assert.That(result.GetNormalizedLineEndingCount(), Is.EqualTo(3), "split " + split);
            split++;
        }
    }

    [TestCase("", ".\r\n")]
    [TestCase("body", "body\r\n.\r\n")]
    [TestCase("body\r\n", "body\r\n.\r\n")]
    [TestCase("body\n", "body\r\n.\r\n")]
    [TestCase("body\r", "body\r\n.\r\n")]
    [TestCase(".", "..\r\n.\r\n")]
    [TestCase("\r\n.", "\r\n..\r\n.\r\n")]
    public void FinalCrlfAndTerminatorAreExactlyOnce(string source, string expected)
    {
        Assert.That(Text(new SmtpDataWriterOperations().Format(Bytes(source))), Is.EqualTo(expected));
    }

    [Test]
    public void ByteAtATimeFeedingPreservesLineStartState()
    {
        byte[] source = Bytes(".one\r\n.two\nthree\r.four");
        MemoryStream output = new MemoryStream();
        SmtpDataWriter writer = new SmtpDataWriterOperations().CreateWithOutputUsingStream(output);
        int index = 0;
        while (index < source.Length)
        {
            writer.Feed(new ReadOnlySpan<byte>(source, index, 1));
            index++;
        }

        writer.Complete();

        Assert.That(Text(output.ToArray()), Is.EqualTo("..one\r\n..two\r\nthree\r\n..four\r\n.\r\n"));
    }

    [Test]
    public void StreamConvenienceLeavesStreamsOpenAndReportsCounts()
    {
        byte[] source = Bytes("one\ntwo");
        MemoryStream input = new MemoryStream(source, false);
        MemoryStream output = new MemoryStream();

        SmtpDataWriteResult result = new SmtpDataWriterOperations().Write(input, output);

        Assert.That(Text(output.ToArray()), Is.EqualTo("one\r\ntwo\r\n.\r\n"));
        Assert.That(result.GetSourceByteCount(), Is.EqualTo(source.LongLength));
        Assert.That(result.GetOutputByteCount(), Is.EqualTo(output.Length));
        Assert.That(input.CanRead, Is.True);
        Assert.That(output.CanWrite, Is.True);
    }

    [Test]
    public void CountingAndHashingSinksSeeTheCompleteWireRepresentation()
    {
        byte[] source = Bytes(".hidden\nvisible");
        byte[] expected = new SmtpDataWriterOperations().Format(source);
        EmailCountingStream counter = new EmailCountingStream();
        EmailHashingStream hashing = new EmailHashingStream(EmailHashAlgorithm.Sha256);
        SmtpDataWriter countingWriter = new SmtpDataWriterOperations().CreateWithOutputUsingEmailCountingStream(counter);
        SmtpDataWriter hashingWriter = new SmtpDataWriterOperations().CreateWithOutputUsingEmailHashingStream(hashing);

        countingWriter.Feed(source);
        SmtpDataWriteResult counted = countingWriter.Complete();
        hashingWriter.Feed(source);
        SmtpDataWriteResult hashed = hashingWriter.Complete();
        EmailHashResult digest = hashing.Complete();
        EmailHashResult expectedDigest = new EmailHash().ComputeExactWithContentAndAlgorithm(expected, EmailHashAlgorithm.Sha256);

        Assert.That(counter.GetByteCount(), Is.EqualTo(expected.LongLength));
        Assert.That(counted.GetOutputByteCount(), Is.EqualTo(expected.LongLength));
        Assert.That(hashed.GetOutputByteCount(), Is.EqualTo(expected.LongLength));
        Assert.That(digest.GetDigestBytes(), Is.EqualTo(expectedDigest.GetDigestBytes()));
    }

    [Test]
    public void EmailWriterCanStreamDirectlyThroughTheDataFilter()
    {
        EmailMessage message = new EmailMessageOperations().CreateWith();
        message.SetBody(new EmailEntityFactory().CreateTextWithValue("first\n.second"));
        EmailWriteOptions options = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        byte[] serialized = new EmailWriter().ToArrayWithMessageAndOptions(message, options);
        byte[] expected = new SmtpDataWriterOperations().Format(serialized);
        MemoryStream output = new MemoryStream();
        SmtpDataWriter filter = new SmtpDataWriterOperations().CreateWithOutputUsingStream(output);

        EmailStreamWriteResult written = new EmailWriter().WriteMessageWithMessageAndOutputAndOptionsUsingEmailMessageAndSmtpDataWriterAndEmailWriteOptions(message, filter, options);
        SmtpDataWriteResult filtered = filter.Complete();

        Assert.That(written.IsSuccess(), Is.True);
        Assert.That(written.GetOutputByteCount(), Is.EqualTo(serialized.LongLength));
        Assert.That(filtered.GetSourceByteCount(), Is.EqualTo(serialized.LongLength));
        Assert.That(output.ToArray(), Is.EqualTo(expected));
    }

    [Test]
    public void LifecycleAndStreamCapabilitiesAreValidated()
    {
        MemoryStream output = new MemoryStream();
        SmtpDataWriter writer = new SmtpDataWriterOperations().CreateWithOutputUsingStream(output);
        writer.Complete();

        Assert.That(writer.IsComplete(), Is.True);
        Assert.Throws<InvalidOperationException>(() => writer.Feed(new byte[] { 1 }));
        Assert.Throws<InvalidOperationException>(() => writer.Complete());
        Assert.Throws<ArgumentNullException>(() => new SmtpDataWriterOperations().CreateWithOutputUsingStream((Stream)null!));
        Assert.Throws<ArgumentException>(() => new SmtpDataWriterOperations().CreateWithOutputUsingStream(new MemoryStream(new byte[0], false)));
        Assert.Throws<ArgumentException>(() => new SmtpDataWriterOperations().Write(new WriteOnlyStream(), new MemoryStream()));
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }

    class WriteOnlyStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position
        {
            get { return 0; }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }
    }
}
