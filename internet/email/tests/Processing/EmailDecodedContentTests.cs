using System.Text;
using email;

namespace email.Tests;

public class EmailDecodedContentTests
{
    [Test]
    public void AttachmentOperationsDecodeCopySaveMeasurePrefixAndHashConsistently()
    {
        byte[] expected = Encoding.UTF8.GetBytes("Hello café!\r\n");
        string encoded = Convert.ToBase64String(expected);
        MimeEntity entity = ParseEntity("Content-Type: application/octet-stream\r\nContent-Transfer-Encoding: base64\r\n\r\n" + encoded);
        EmailAttachmentView attachment = new EmailAttachmentView(entity, new EmailPathOperations().CreateRoot(), EmailAttachmentKind.Attachment, "greeting.bin");
        string path = TemporaryPath();
        try
        {
            using MemoryStream copied = new MemoryStream();

            Assert.That(attachment.CopyDecodedContentToWithLimit(copied, 100), Is.EqualTo(expected.LongLength));
            Assert.That(copied.ToArray(), Is.EqualTo(expected));
            Assert.That(attachment.ReadDecodedPrefix(7), Is.EqualTo(expected.Take(7).ToArray()));
            Assert.That(attachment.MeasureDecodedLengthWithLimit(100), Is.EqualTo(expected.LongLength));

            EmailHashResult hash = attachment.ComputeDecodedHashWithLimit(EmailHashAlgorithm.Sha384, 100);
            EmailHashResult expectedHash = new EmailHash().ComputeExactWithContentAndAlgorithm(expected, EmailHashAlgorithm.Sha384);
            Assert.That(hash.GetLowercaseHex(), Is.EqualTo(expectedHash.GetLowercaseHex()));
            Assert.That(hash.GetHashedByteCount(), Is.EqualTo(expected.LongLength));

            Assert.That(attachment.SaveDecodedContentWithLimit(path, 100), Is.EqualTo(expected.LongLength));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(expected));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void EntityOperationsDecodeQuotedPrintableAndPreferRetainedDecodedContent()
    {
        MimeEntity quoted = ParseEntity("Content-Type: text/plain\r\nContent-Transfer-Encoding: quoted-printable\r\n\r\nA=20B=0Aline=3Dtwo");
        using MemoryStream copied = new MemoryStream();

        Assert.That(new EmailDecodedContent().CopyToWithEntityAndOutputAndMaximumBytes(quoted, copied, 100), Is.EqualTo(12));
        Assert.That(Encoding.ASCII.GetString(copied.ToArray()), Is.EqualTo("A B\nline=two"));

        EmailParseOptions options = new EmailParseOptions();
        EmailBodyIngestOptions ingest = options.GetBodyIngestOptions();
        ingest.SetDecodeOnIngest(true);
        options.SetBodyIngestOptions(ingest);
        EmailEntityParseResult parsed = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(
            Encoding.ASCII.GetBytes("Content-Type: text/plain\r\nContent-Transfer-Encoding: base64\r\n\r\ncmV0YWluZWQ="),
            options);
        Assert.That(parsed.IsSuccess(), Is.True);
        Assert.That(parsed.GetEntity()!.HasDecodedContent(), Is.True);
        Assert.That(Encoding.ASCII.GetString(new EmailDecodedContent().ReadPrefixWithEntityAndMaximumPrefixBytes(parsed.GetEntity()!, 20)), Is.EqualTo("retained"));
    }

    [Test]
    public void DirectContentOperationsHonorSliceBoundariesAndReturnSnapshots()
    {
        EmailContent source = new EmailContentOperations().CreateMemoryCopy(Encoding.ASCII.GetBytes("xxpayloadyy"), false);
        EmailContent slice = new EmailContentOperations().CreateSlice(source, 2, 7);
        using MemoryStream copied = new MemoryStream();

        Assert.That(new EmailDecodedContent().CopyToWithContentAndOutputAndMaximumBytes(slice, copied, 7), Is.EqualTo(7));
        Assert.That(Encoding.ASCII.GetString(copied.ToArray()), Is.EqualTo("payload"));
        Assert.That(new EmailDecodedContent().MeasureLengthWithContentAndMaximumBytes(slice, 7), Is.EqualTo(7));
        byte[] prefix = new EmailDecodedContent().ReadPrefixWithContentAndMaximumPrefixBytes(slice, 4);
        Assert.That(Encoding.ASCII.GetString(prefix), Is.EqualTo("payl"));
        prefix[0] = 0;
        Assert.That(Encoding.ASCII.GetString(new EmailDecodedContent().ReadPrefixWithContentAndMaximumPrefixBytes(slice, 4)), Is.EqualTo("payl"));

        EmailHashResult hash = new EmailDecodedContent().ComputeHashWithContentAndAlgorithmAndMaximumBytes(slice, EmailHashAlgorithm.Sha256, 7);
        Assert.That(hash.GetLowercaseHex(), Is.EqualTo(new EmailHash().ComputeExactWithContentAndAlgorithm(Encoding.ASCII.GetBytes("payload"), EmailHashAlgorithm.Sha256).GetLowercaseHex()));
        slice.Close();
        source.Close();
    }

    [Test]
    public void PrefixInspectionStopsEarlyWithoutMaterializingOneShotEncodedContent()
    {
        byte[] decoded = Enumerable.Range(0, 200000).Select(index => (byte)(index % 251)).ToArray();
        byte[] encoded = Encoding.ASCII.GetBytes(Convert.ToBase64String(decoded));
        TrackingContentReadStream stream = new TrackingContentReadStream(encoded);
        MimeEntity entity = ParseEntity("Content-Type: application/octet-stream\r\nContent-Transfer-Encoding: base64\r\n\r\nQQ==");
        entity.ReplaceRawContent(new EmailContentOperations().CreateStream(stream, EmailContentOwnership.Borrowed));

        byte[] prefix = new EmailDecodedContent().ReadPrefixWithEntityAndMaximumPrefixBytes(entity, 11);

        Assert.That(prefix, Is.EqualTo(decoded.Take(11).ToArray()));
        Assert.That(stream.GetBytesRead(), Is.LessThan(encoded.LongLength));
        Assert.That(stream.GetLargestReadRequest(), Is.LessThan(100));
        Assert.That(stream.CanRead, Is.True);
        entity.GetRawContent()!.Close();
        stream.Close();
    }

    [Test]
    public void CopyStreamsLargeOneShotContentInBoundedChunks()
    {
        byte[] value = Enumerable.Range(0, 200000).Select(index => (byte)(index % 253)).ToArray();
        TrackingContentReadStream stream = new TrackingContentReadStream(value);
        EmailContent content = new EmailContentOperations().CreateStream(stream, EmailContentOwnership.Borrowed);
        using MemoryStream output = new MemoryStream();

        Assert.That(new EmailDecodedContent().CopyToWithContentAndOutputAndMaximumBytes(content, output, value.LongLength), Is.EqualTo(value.LongLength));
        Assert.That(output.ToArray(), Is.EqualTo(value));
        Assert.That(stream.GetReadCallCount(), Is.GreaterThan(2));
        Assert.That(stream.GetLargestReadRequest(), Is.LessThanOrEqualTo(EmailDecodedContentConstants.DefaultBufferBytes));
        Assert.That(stream.CanRead, Is.True);
        content.Close();
        stream.Close();
    }

    [Test]
    public void OperationsEnforceLimitsAndValidateUnsupportedSourcesAndArguments()
    {
        EmailContent content = new EmailContentOperations().CreateMemoryCopy(new byte[] { 1, 2, 3, 4 }, false);
        using MemoryStream output = new MemoryStream();
        EmailLimitException contentLimit = Assert.Throws<EmailLimitException>(() => new EmailDecodedContent().CopyToWithContentAndOutputAndMaximumBytes(content, output, 3));
        Assert.That(contentLimit.GetLimitName(), Is.EqualTo("MaxDecodedContentBytes"));
        Assert.That(contentLimit.GetLimit(), Is.EqualTo(3));
        Assert.That(contentLimit.GetObserved(), Is.EqualTo(4));
        Assert.That(output.Length, Is.Zero);

        MimeEntity encoded = ParseEntity("Content-Type: application/octet-stream\r\nContent-Transfer-Encoding: base64\r\n\r\nAQIDBAU=");
        EmailLimitException decodedLimit = Assert.Throws<EmailLimitException>(() => new EmailDecodedContent().MeasureLengthWithEntityAndMaximumBytes(encoded, 3));
        Assert.That(decodedLimit.GetLimitName(), Is.EqualTo("MaxDecodedContentBytes"));
        Assert.That(decodedLimit.GetLimit(), Is.EqualTo(3));

        MimeEntity unsupported = ParseEntity("Content-Type: application/octet-stream\r\nContent-Transfer-Encoding: x-custom\r\n\r\ndata");
        Assert.Throws<InvalidOperationException>(() => new EmailDecodedContent().MeasureLengthWithEntityAndMaximumBytes(unsupported, 100));
        Assert.Throws<InvalidOperationException>(() => new EmailDecodedContent().MeasureLengthWithEntityAndMaximumBytes(new EmailEntityFactory().CreateMultipartWithSubtype("mixed"), 100));
        Assert.Throws<ArgumentNullException>(() => new EmailDecodedContent().CopyToWithContentAndOutputAndMaximumBytes((EmailContent)null!, output, 10));
        Assert.Throws<ArgumentNullException>(() => new EmailDecodedContent().CopyToWithEntityAndOutputAndMaximumBytes((MimeEntity)null!, output, 10));
        Assert.Throws<ArgumentNullException>(() => new EmailDecodedContent().CopyToWithContentAndOutputAndMaximumBytes(content, null!, 10));
        Assert.Throws<ArgumentException>(() => new EmailDecodedContent().CopyToWithContentAndOutputAndMaximumBytes(content, new MemoryStream(new byte[1], false), 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailDecodedContent().CopyToWithContentAndOutputAndMaximumBytes(content, output, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailDecodedContent().ReadPrefixWithContentAndMaximumPrefixBytes(content, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailDecodedContent().ComputeHashWithContentAndAlgorithmAndMaximumBytes(content, (EmailHashAlgorithm)99, 10));
        Assert.Throws<ArgumentNullException>(() => new EmailDecodedContent().SaveWithContentAndPathAndMaximumBytes(content, null!, 10));
        Assert.Throws<ArgumentException>(() => new EmailDecodedContent().SaveWithContentAndPathAndMaximumBytes(content, "", 10));

        content.Close();
        Assert.Throws<ObjectDisposedException>(() => new EmailDecodedContent().MeasureLengthWithContentAndMaximumBytes(content, 10));
    }

    static MimeEntity ParseEntity(string source)
    {
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Encoding.ASCII.GetBytes(source));
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetEntity()!;
    }

    static string TemporaryPath()
    {
        return Path.Combine(Path.GetTempPath(), "email-decoded-content-" + Guid.NewGuid().ToString("N") + ".bin");
    }
}

public class TrackingContentReadStream : Stream
{
    byte[] value;
    int position;
    long bytesRead;
    int readCallCount;
    int largestReadRequest;
    bool closed;

    public TrackingContentReadStream(byte[] value)
    {
        this.value = value;
        position = 0;
        bytesRead = 0;
        readCallCount = 0;
        largestReadRequest = 0;
        closed = false;
    }

    public long GetBytesRead()
    {
        return bytesRead;
    }

    public int GetReadCallCount()
    {
        return readCallCount;
    }

    public int GetLargestReadRequest()
    {
        return largestReadRequest;
    }

    public override bool CanRead => !closed;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (closed)
        {
            throw new ObjectDisposedException("TrackingContentReadStream");
        }

        readCallCount++;
        if (count > largestReadRequest)
        {
            largestReadRequest = count;
        }

        int remaining = value.Length - position;
        int selected = count < remaining ? count : remaining;
        Array.Copy(value, position, buffer, offset, selected);
        position += selected;
        bytesRead += selected;
        return selected;
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
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        closed = true;
        base.Dispose(disposing);
    }
}
