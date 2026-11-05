namespace http.Tests;

public class OctetInfrastructureTests
{
    [Test]
    public void BoundedBufferAccumulatesBytesAndReturnsOwnedSnapshots()
    {
        BoundedByteBuffer buffer = new BoundedByteBuffer(40);

        OctetOperationResult first = buffer.AppendByte(0x41);
        OctetOperationResult second = buffer.AppendBytes("bcdefghijklmnopq"u8);
        byte[] snapshot = buffer.ToArray();
        snapshot[0] = 0xff;

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(first.Written, Is.EqualTo(1));
            Assert.That(second.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(second.Written, Is.EqualTo(16));
            Assert.That(buffer.GetCount(), Is.EqualTo(17));
            Assert.That(buffer.GetLimit(), Is.EqualTo(40));
            Assert.That(buffer.ToArray(), Is.EqualTo("Abcdefghijklmnopq"u8.ToArray()));
        });
    }

    [Test]
    public void BoundedBufferRejectsLimitExhaustionWithoutPartialAppend()
    {
        BoundedByteBuffer buffer = new BoundedByteBuffer(4);
        buffer.AppendBytes(new byte[] { 1, 2, 3 });

        OctetOperationResult result = buffer.AppendBytes(new byte[] { 4, 5 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.IsLimitExceeded, Is.True);
            Assert.That(result.IsMalformed, Is.False);
            Assert.That(result.RequiredSize, Is.EqualTo(5));
            Assert.That(result.Written, Is.Zero);
            Assert.That(buffer.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void BoundedBufferCopiesAllOrNothingAndCanBeCleared()
    {
        BoundedByteBuffer buffer = new BoundedByteBuffer(8);
        byte[] tooSmall = [0xcc, 0xcc, 0xcc];
        byte[] output = [0xcc, 0xcc, 0xcc, 0xcc, 0xcc];
        buffer.AppendBytes(new byte[] { 1, 2, 3 });

        OctetOperationResult insufficient = buffer.CopyTo(tooSmall, 1);
        OctetOperationResult copied = buffer.CopyTo(output, 1);
        buffer.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(insufficient.Status, Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(insufficient.RequiredSize, Is.EqualTo(4));
            Assert.That(tooSmall, Is.EqualTo(new byte[] { 0xcc, 0xcc, 0xcc }));
            Assert.That(copied.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(copied.Written, Is.EqualTo(3));
            Assert.That(output, Is.EqualTo(new byte[] { 0xcc, 1, 2, 3, 0xcc }));
            Assert.That(buffer.GetCount(), Is.Zero);
            Assert.That(buffer.ToArray(), Is.Empty);
        });
    }

    [Test]
    public void ReaderReturnsBytesWithoutConsumingOnIncompleteInput()
    {
        OctetOperationResult single = OctetReader.ReadByte(new byte[] { 0x10, 0x20 }, 1, 8);
        OctetOperationResult sequence = OctetReader.ReadBytes(new byte[] { 1, 2, 3, 4 }, 1, 2, 8);
        OctetOperationResult incomplete = OctetReader.ReadBytes(new byte[] { 1, 2 }, 1, 2, 8);

        Assert.Multiple(() =>
        {
            Assert.That(single.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(single.Value, Is.EqualTo(0x20));
            Assert.That(single.Consumed, Is.EqualTo(1));
            Assert.That(sequence.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(sequence.Bytes, Is.EqualTo(new byte[] { 2, 3 }));
            Assert.That(sequence.Consumed, Is.EqualTo(2));
            Assert.That(incomplete.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(incomplete.Consumed, Is.Zero);
            Assert.That(incomplete.RequiredSize, Is.EqualTo(3));
        });
    }

    [Test]
    public void ReaderDistinguishesMalformedInputFromLimitExhaustion()
    {
        OctetOperationResult malformed = OctetReader.ExpectByte("GET"u8, 0, (byte)'P', 8);
        OctetOperationResult invalidOffset = OctetReader.ReadByte("GET"u8, 4, 8);
        OctetOperationResult limited = OctetReader.ReadBytes("GET"u8, 1, 2, 2);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(malformed.IsMalformed, Is.True);
            Assert.That(malformed.IsLimitExceeded, Is.False);
            Assert.That(malformed.Consumed, Is.Zero);
            Assert.That(invalidOffset.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(invalidOffset.IsMalformed, Is.True);
            Assert.That(limited.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(limited.IsLimitExceeded, Is.True);
            Assert.That(limited.IsMalformed, Is.False);
            Assert.That(limited.Consumed, Is.Zero);
        });
    }

    [Test]
    public void WriterWritesExactBytesAtTheRequestedOffset()
    {
        byte[] output = [0xcc, 0xcc, 0xcc, 0xcc, 0xcc];

        OctetOperationResult result = OctetWriter.WriteBytes(new byte[] { 1, 2, 3 }, output, 1, 5);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Written, Is.EqualTo(3));
            Assert.That(result.RequiredSize, Is.EqualTo(4));
            Assert.That(output, Is.EqualTo(new byte[] { 0xcc, 1, 2, 3, 0xcc }));
        });
    }

    [Test]
    public void WriterReportsInsufficientOutputWithoutPartialWrites()
    {
        byte[] output = [0xcc, 0xcc, 0xcc];

        OctetOperationResult result = OctetWriter.WriteBytes(new byte[] { 1, 2, 3 }, output, 1, 8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(result.RequiredSize, Is.EqualTo(4));
            Assert.That(result.Written, Is.Zero);
            Assert.That(result.IsMalformed, Is.False);
            Assert.That(result.IsLimitExceeded, Is.False);
            Assert.That(output, Is.EqualTo(new byte[] { 0xcc, 0xcc, 0xcc }));
        });
    }

    [Test]
    public void WriterDistinguishesMalformedValuesFromLimitExhaustion()
    {
        byte[] malformedOutput = [0xcc, 0xcc, 0xcc, 0xcc];
        byte[] limitedOutput = [0xcc, 0xcc, 0xcc, 0xcc];

        OctetOperationResult malformed = OctetWriter.WriteAsciiString("A\u0080", malformedOutput, 1, 4);
        OctetOperationResult limited = OctetWriter.WriteBytes(new byte[] { 1, 2, 3 }, limitedOutput, 1, 3);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(malformed.IsMalformed, Is.True);
            Assert.That(malformed.IsLimitExceeded, Is.False);
            Assert.That(malformedOutput, Is.EqualTo(new byte[] { 0xcc, 0xcc, 0xcc, 0xcc }));
            Assert.That(limited.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(limited.IsMalformed, Is.False);
            Assert.That(limited.IsLimitExceeded, Is.True);
            Assert.That(limited.Written, Is.Zero);
            Assert.That(limitedOutput, Is.EqualTo(new byte[] { 0xcc, 0xcc, 0xcc, 0xcc }));
        });
    }

    [Test]
    public void WriterHandlesSingleOctetsAndValidAscii()
    {
        byte[] output = [0xcc, 0xcc, 0xcc, 0xcc, 0xcc];

        OctetOperationResult first = OctetWriter.WriteByte(0x3a, output, 0, 5);
        OctetOperationResult second = OctetWriter.WriteAsciiString(" OK", output, 1, 5);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(first.Written, Is.EqualTo(1));
            Assert.That(second.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(second.Written, Is.EqualTo(3));
            Assert.That(output, Is.EqualTo(new byte[] { 0x3a, 0x20, 0x4f, 0x4b, 0xcc }));
        });
    }
}
