using System.Security.Cryptography;
using System.Text;
using email;

namespace email.Tests;

public class EmailStreamingWriterTests
{
    [Test]
    public void StreamAndArrayOutputsMatchForIdentityQuotedPrintableBase64AndMultipart()
    {
        EmailWriteOptions options = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        MimeEntity identity = new EmailEntityFactory().CreateTextWithValue("plain text");
        MimeEntity quotedPrintable = new EmailEntityFactory().CreateTextWithValue("café");
        MimeEntity base64 = new EmailEntityFactory().CreateBinaryWithValue(new byte[] { 0, 1, 2, 255 });
        MimeEntity multipart = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity>() { new EmailEntityFactory().CreateTextWithValue("part") });

        AssertStreamMatchesArray(identity, options);
        AssertStreamMatchesArray(quotedPrintable, options);
        AssertStreamMatchesArray(base64, options);
        AssertStreamMatchesArray(multipart, DeterministicOptions("stream multipart"));
    }

    [Test]
    public void MessageStreamOutputMatchesArrayAndReportsCounts()
    {
        EmailMessage message = new EmailMessageOperations().CreateWith();
        message.GetHeaders()!.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("Subject", Bytes(" streamed")));
        message.SetBody(new EmailEntityFactory().CreateTextWithValue("body"));
        EmailWriteOptions options = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        byte[] expected = new EmailWriter().ToArrayWithMessageAndOptions(message, options);
        MemoryStream output = new MemoryStream();

        EmailStreamWriteResult result = new EmailWriter().WriteMessageWithMessageAndOutputAndOptionsUsingEmailMessageAndStreamAndEmailWriteOptions(message, output, options);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(output.ToArray(), Is.EqualTo(expected));
        Assert.That(result.GetOutputByteCount(), Is.EqualTo(expected.LongLength));
        Assert.That(result.GetRegeneratedByteCount(), Is.EqualTo(expected.LongLength));
        Assert.That(result.GetCopiedByteCount(), Is.Zero);
    }

    [Test]
    public void UnchangedPreservedMessageStreamsAsCopiedSourceBytes()
    {
        byte[] source = Bytes("sUbJeCt: exact\n\nbody with original ending");
        EmailMessage message = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, new EmailParseOptionsOperations().CreatePreserving()).GetMessage()!;
        MemoryStream output = new MemoryStream();

        EmailStreamWriteResult result = new EmailWriter().WriteMessageWithMessageAndOutputAndOptionsUsingEmailMessageAndStreamAndEmailWriteOptions(message!, output, new EmailWriteOptionsOperations().CreateExactPreservation());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(output.ToArray(), Is.EqualTo(source));
        Assert.That(result.GetOutputByteCount(), Is.EqualTo(source.LongLength));
        Assert.That(result.GetCopiedByteCount(), Is.EqualTo(source.LongLength));
        Assert.That(result.GetRegeneratedByteCount(), Is.Zero);
    }

    [Test]
    public void CountingAndHashingSinksDescribeTheExactSerializedBytes()
    {
        MimeEntity entity = new EmailEntityFactory().CreateTextWithValue("hash café");
        EmailWriteOptions options = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        byte[] expected = new EmailWriter().ToArrayWithEntityAndOptions(entity, options);
        EmailCountingStream counter = new EmailCountingStream();
        EmailHashingStream hashing = new EmailHashingStream(EmailHashAlgorithm.Sha256);

        EmailStreamWriteResult counted = new EmailWriter().WriteEntityWithEntityAndOutputAndOptionsUsingMimeEntityAndEmailCountingStreamAndEmailWriteOptions(entity, counter, options);
        EmailStreamWriteResult hashed = new EmailWriter().WriteEntityWithEntityAndOutputAndOptionsUsingMimeEntityAndEmailHashingStreamAndEmailWriteOptions(entity, hashing, options);
        EmailHashResult digest = hashing.Complete();
        EmailHashResult expectedDigest = new EmailHash().ComputeExactWithContentAndAlgorithm(expected, EmailHashAlgorithm.Sha256);

        Assert.That(counted.IsSuccess(), Is.True);
        Assert.That(hashed.IsSuccess(), Is.True);
        Assert.That(counter.GetByteCount(), Is.EqualTo(expected.LongLength));
        Assert.That(new EmailWriter().CountBytesWithEntityAndOptions(entity, options), Is.EqualTo(expected.LongLength));
        Assert.That(digest.GetHashedByteCount(), Is.EqualTo(expected.LongLength));
        Assert.That(digest.GetDigestBytes(), Is.EqualTo(expectedDigest.GetDigestBytes()));
        Assert.That(hashing.IsComplete(), Is.True);
        Assert.Throws<InvalidOperationException>(() => hashing.Write(new byte[] { 1 }));
    }

    [Test]
    public void LargeFileBodyIsEncodedInBoundedChunksWithoutMaterialization()
    {
        string path = Path.Combine(Path.GetTempPath(), "email-streaming-" + Guid.NewGuid().ToString("N") + ".bin");
        byte[] source = new byte[3 * 1024 * 1024];
        RandomNumberGenerator.Fill(source);
        File.WriteAllBytes(path, source);
        try
        {
            EmailMediaType mediaType = new EmailEntityFactory().CreateBinaryWithValue(Array.Empty<byte>()).GetMediaType()!;
            EmailContent content = new EmailContentOperations().CreateFile(path, EmailContentOwnership.Borrowed);
            MimeEntity entity = new EmailEntityFactory().CreateBinaryWithContentAndMediaType(content, mediaType!);
            EmailWriteOptions options = new EmailWriteOptionsOperations().CreateCanonicalInternet();
            options.SetMaximumOutputBytes(10 * 1024 * 1024);
            MaximumWriteStream output = new MaximumWriteStream(120000);

            EmailStreamWriteResult result = new EmailWriter().WriteEntityWithEntityAndOutputAndOptionsUsingMimeEntityAndStreamAndEmailWriteOptions(entity, output, options);

            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(output.GetByteCount(), Is.EqualTo(result.GetOutputByteCount()));
            Assert.That(output.GetMaximumObservedWrite(), Is.LessThanOrEqualTo(120000));
            Assert.That(output.GetWriteCount(), Is.GreaterThan(20));
            Assert.That(result.GetOutputByteCount(), Is.GreaterThan(source.LongLength));
            Assert.That(content.IsClosed(), Is.False);
            content.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void StreamingLimitFailureWritesNoPartialOutput()
    {
        MimeEntity entity = new EmailEntityFactory().CreateBinaryWithValue(new byte[1000]);
        EmailWriteOptions options = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        options.SetMaximumOutputBytes(100);
        MemoryStream output = new MemoryStream();

        EmailStreamWriteResult result = new EmailWriter().WriteEntityWithEntityAndOutputAndOptionsUsingMimeEntityAndStreamAndEmailWriteOptions(entity, output, options);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetOutputByteCount(), Is.Zero);
        Assert.That(output.Length, Is.Zero);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.WritePolicyViolation), Is.True);
    }

    [Test]
    public void OutputSinksValidateSlicesAlgorithmsAndDestinations()
    {
        EmailCountingStream counter = new EmailCountingStream();

        Assert.Throws<ArgumentNullException>(() => counter.WriteSegment(null!, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => counter.WriteSegment(new byte[1], 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailHashingStream((EmailHashAlgorithm)99));
        Assert.Throws<ArgumentException>(() => new EmailWriter().WriteEntityWithEntityAndOutputAndOptionsUsingMimeEntityAndStreamAndEmailWriteOptions(new EmailEntityFactory().CreateTextWithValue("body"), new MemoryStream(new byte[0], false), new EmailWriteOptionsOperations().CreateCanonicalInternet()));
    }

    static void AssertStreamMatchesArray(MimeEntity entity, EmailWriteOptions options)
    {
        byte[] expected = new EmailWriter().ToArrayWithEntityAndOptions(entity, options);
        MemoryStream output = new MemoryStream();

        EmailStreamWriteResult result = new EmailWriter().WriteEntityWithEntityAndOutputAndOptionsUsingMimeEntityAndStreamAndEmailWriteOptions(entity, output, options);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(output.ToArray(), Is.EqualTo(expected));
        Assert.That(result.GetOutputByteCount(), Is.EqualTo(expected.LongLength));
    }

    static EmailWriteOptions DeterministicOptions(string seed)
    {
        EmailWriteOptions options = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        options.SetDeterministicOutput(true);
        options.SetDeterministicSeed(Bytes(seed));
        return options;
    }

    static byte[] Bytes(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }

    class MaximumWriteStream : Stream
    {
        int maximumWrite;
        long byteCount;
        int writeCount;
        int maximumObservedWrite;

        public MaximumWriteStream(int maximumWrite)
        {
            this.maximumWrite = maximumWrite;
            byteCount = 0;
            writeCount = 0;
            maximumObservedWrite = 0;
        }

        public long GetByteCount()
        {
            return byteCount;
        }

        public int GetWriteCount()
        {
            return writeCount;
        }

        public int GetMaximumObservedWrite()
        {
            return maximumObservedWrite;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => byteCount;
        public override long Position
        {
            get { return byteCount; }
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
            if (count > maximumWrite)
            {
                throw new InvalidOperationException("A single write exceeded the configured test chunk size.");
            }

            byteCount += count;
            writeCount++;
            if (count > maximumObservedWrite)
            {
                maximumObservedWrite = count;
            }
        }
    }
}
