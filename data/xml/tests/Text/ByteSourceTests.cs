using xml;

namespace xml.Tests;

[TestFixture]
public class ByteSourceTests
{
    [Test]
    public void MemorySourceDetectsAndConsumesByteOrderMarks()
    {
        AssertMemorySource([], ByteOrderMarkKind.None, 0, []);
        AssertMemorySource(new byte[] { 1, 2, 3 }, ByteOrderMarkKind.None, 0, new byte[] { 1, 2, 3 });
        AssertMemorySource(new byte[] { 0xEF, 0xBB }, ByteOrderMarkKind.None, 0, new byte[] { 0xEF, 0xBB });
        AssertMemorySource(new byte[] { 0xEF, 0xBB, 0xBF }, ByteOrderMarkKind.Utf8, 3, []);
        AssertMemorySource(new byte[] { 0xEF, 0xBB, 0xBF, 1, 2 }, ByteOrderMarkKind.Utf8, 3, new byte[] { 1, 2 });
        AssertMemorySource(new byte[] { 0xFE, 0xFF, 1, 2 }, ByteOrderMarkKind.Utf16BigEndian, 2, new byte[] { 1, 2 });
        AssertMemorySource(new byte[] { 0xFF, 0xFE, 1, 2 }, ByteOrderMarkKind.Utf16LittleEndian, 2, new byte[] { 1, 2 });
        AssertMemorySource(new byte[] { 0x00, 0x00, 0xFE, 0xFF, 1, 2 }, ByteOrderMarkKind.Utf32BigEndian, 4, new byte[] { 1, 2 });
        AssertMemorySource(new byte[] { 0xFF, 0xFE, 0x00, 0x00, 1, 2 }, ByteOrderMarkKind.Utf32LittleEndian, 4, new byte[] { 1, 2 });
    }

    [Test]
    public void StreamSourceBuffersShortReadsWithoutLosingPrefetchedContent()
    {
        TrackingStream stream = new TrackingStream(new byte[] { 0xEF, 0xBB, 0xBF, 10, 11, 12, 13, 14, 15, 16, 17 }, 1);
        StreamByteSource source = new StreamByteSource(stream, false, 4);

        Assert.That(source.InspectByteOrderMark(), Is.EqualTo(ByteOrderMarkKind.Utf8));
        Assert.That(source.SourceByteOrderMarkLength(), Is.EqualTo(3));
        Assert.That(stream.ReadCalls, Is.EqualTo(4));
        byte[] first = new byte[1];
        Assert.That(source.ReadSourceBytes(first), Is.EqualTo(1));
        Assert.That(first, Is.EqualTo(new byte[] { 10 }));
        Assert.That(stream.ReadCalls, Is.EqualTo(4));
        byte[] remaining = new byte[7];
        Assert.That(source.ReadSourceBytes(remaining), Is.EqualTo(7));
        Assert.That(remaining, Is.EqualTo(new byte[] { 11, 12, 13, 14, 15, 16, 17 }));
        Assert.That(source.SourceBytePosition(), Is.EqualTo(8));
        Assert.That(source.IsSourceEnd(), Is.True);
        Assert.That(source.ReadSourceBytes(new byte[1]), Is.Zero);
    }

    [Test]
    public void FactorySelectsByteMemoryAndStreamInputsAndRejectsOtherKinds()
    {
        XmlInput memoryInput = new XmlInput();
        memoryInput.Kind = XmlInputKind.ByteMemory;
        memoryInput.Bytes = new byte[] { 4, 5 };
        IByteSource memory = ByteSourceFactory.CreateXmlInputByteSource(memoryInput, 8);
        byte[] bytes = new byte[2];
        Assert.That(memory.ReadSourceBytes(bytes), Is.EqualTo(2));
        Assert.That(bytes, Is.EqualTo(new byte[] { 4, 5 }));

        TrackingStream stream = new TrackingStream(new byte[] { 6 }, 1);
        XmlInput streamInput = new XmlInput();
        streamInput.Kind = XmlInputKind.Stream;
        streamInput.ByteStream = stream;
        IByteSource streamSource = ByteSourceFactory.CreateXmlInputByteSource(streamInput, 8);
        Assert.That(streamSource, Is.InstanceOf<StreamByteSource>());

        XmlInput missingStream = new XmlInput();
        missingStream.Kind = XmlInputKind.Stream;
        Assert.That(() => ByteSourceFactory.CreateXmlInputByteSource(missingStream, 8), Throws.ArgumentException);
        XmlInput characters = new XmlInput();
        characters.Kind = XmlInputKind.String;
        Assert.That(() => ByteSourceFactory.CreateXmlInputByteSource(characters, 8), Throws.ArgumentException);
        Assert.That(() => new StreamByteSource(stream, false, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void ClosingSourceDisposesOnlyOwnedStreamAndIsIdempotent()
    {
        TrackingStream ownedStream = new TrackingStream(new byte[] { 1 }, 1);
        XmlInput ownedInput = new XmlInput();
        ownedInput.Kind = XmlInputKind.Stream;
        ownedInput.ByteStream = ownedStream;
        ownedInput.OwnsResource = true;
        IByteSource owned = ByteSourceFactory.CreateXmlInputByteSource(ownedInput, 4);
        owned.CloseByteSource();
        owned.CloseByteSource();
        Assert.That(ownedStream.DisposeCalls, Is.EqualTo(1));
        Assert.That(() => owned.ReadSourceBytes(new byte[1]), Throws.TypeOf<ObjectDisposedException>());

        TrackingStream borrowedStream = new TrackingStream(new byte[] { 2 }, 1);
        XmlInput borrowedInput = new XmlInput();
        borrowedInput.Kind = XmlInputKind.Stream;
        borrowedInput.ByteStream = borrowedStream;
        borrowedInput.OwnsResource = false;
        IByteSource borrowed = ByteSourceFactory.CreateXmlInputByteSource(borrowedInput, 4);
        borrowed.CloseByteSource();
        borrowed.CloseByteSource();
        Assert.That(borrowedStream.DisposeCalls, Is.Zero);
        Assert.That(borrowedStream.ReadByte(), Is.EqualTo(2));

        MemoryByteSource memory = new MemoryByteSource(new byte[] { 3 });
        memory.CloseByteSource();
        memory.CloseByteSource();
        Assert.That(() => memory.ReadSourceBytes(new byte[1]), Throws.TypeOf<ObjectDisposedException>());
    }

    private static void AssertMemorySource(byte[] input, ByteOrderMarkKind expectedKind, int expectedLength, byte[] expectedContent)
    {
        MemoryByteSource source = new MemoryByteSource(input);
        Assert.That(source.InspectByteOrderMark(), Is.EqualTo(expectedKind));
        Assert.That(source.SourceByteOrderMarkLength(), Is.EqualTo(expectedLength));
        Assert.That(source.SourceBytePosition(), Is.Zero);
        Assert.That(source.IsSourceEnd(), Is.EqualTo(expectedContent.Length == 0));
        byte[] actual = new byte[expectedContent.Length];
        Assert.That(source.ReadSourceBytes(actual), Is.EqualTo(expectedContent.Length));
        Assert.That(actual, Is.EqualTo(expectedContent));
        Assert.That(source.SourceBytePosition(), Is.EqualTo(expectedContent.Length));
        Assert.That(source.IsSourceEnd(), Is.True);
    }

    private sealed class TrackingStream : MemoryStream
    {
        internal int MaximumRead;
        internal int ReadCalls;
        internal int DisposeCalls;

        internal TrackingStream(byte[] bytes, int maximumRead) : base(bytes)
        {
            MaximumRead = maximumRead;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCalls = ReadCalls + 1;
            int limited = count < MaximumRead ? count : MaximumRead;
            return base.Read(buffer, offset, limited);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeCalls = DisposeCalls + 1;
            }

            base.Dispose(disposing);
        }
    }
}
