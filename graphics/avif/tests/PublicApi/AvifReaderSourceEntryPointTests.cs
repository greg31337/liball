namespace avif.Tests;

public class AvifReaderSourceEntryPointTests
{
    [Test]
    public void SeekableAndNonSeekableEntryPointsParseFromCallerOwnedStreams()
    {
        IAvifCodec codec = new AvifCodec();
        byte[] encoded = CreateEncodedImage();
        byte[] prefixed = new byte[encoded.Length + 7];
        encoded.CopyTo(prefixed, 7);
        MemoryStream seekable = new MemoryStream(prefixed);
        seekable.Position = 7;
        NonSeekableReadStream nonSeekable =
            new NonSeekableReadStream(encoded);

        AvifReader seekableReader = codec.CreateSeekableStreamReader(
            seekable,
            new AvifDecoderOptions());
        AvifReader nonSeekableReader = codec.CreateNonSeekableStreamReader(
            nonSeekable,
            new AvifDecoderOptions());
        AvifResultCode seekableParse = seekableReader.Parse();
        AvifResultCode nonSeekableParse = nonSeekableReader.Parse();
        AvifDecodeResult seekableDecode =
            seekableReader.DecodePrimaryImage();
        AvifDecodeResult nonSeekableDecode =
            nonSeekableReader.DecodePrimaryImage();
        seekableReader.Close();
        nonSeekableReader.Close();

        Assert.Multiple(() =>
        {
            Assert.That(seekableParse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(nonSeekableParse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(seekableDecode.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(nonSeekableDecode.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(seekable.Position, Is.EqualTo(seekable.Length));
            Assert.That(seekable.CanRead, Is.True);
            Assert.That(nonSeekable.IsClosed, Is.False);
            Assert.That(seekableReader.GetMissingRanges(), Is.Empty);
            Assert.That(nonSeekableReader.GetMissingRanges(), Is.Empty);
        });

        (seekableDecode.GetImage()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Close();
        (nonSeekableDecode.GetImage()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Close();
        seekable.Dispose();
        nonSeekable.Dispose();
    }

    [Test]
    public void StreamEntryPointsRejectTheOppositeStreamKind()
    {
        IAvifCodec codec = new AvifCodec();
        byte[] encoded = CreateEncodedImage();
        MemoryStream seekable = new MemoryStream(encoded);
        NonSeekableReadStream nonSeekable =
            new NonSeekableReadStream(encoded);

        AvifReader seekableAsSequential =
            codec.CreateNonSeekableStreamReader(
                seekable,
                new AvifDecoderOptions());
        AvifReader sequentialAsSeekable =
            codec.CreateSeekableStreamReader(
                nonSeekable,
                new AvifDecoderOptions());

        Assert.Multiple(() =>
        {
            Assert.That(seekableAsSequential.GetCode(),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(sequentialAsSeekable.GetCode(),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(seekableAsSequential.Parse(),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(sequentialAsSeekable.Parse(),
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        seekableAsSequential.Close();
        sequentialAsSeekable.Close();
        seekable.Dispose();
        nonSeekable.Dispose();
    }

    [Test]
    public void RangeReaderReportsExactMissingRangeAndRetriesAfterFulfillment()
    {
        IAvifCodec codec = new AvifCodec();
        byte[] encoded = CreateEncodedImage();
        ControlledRandomAccessSource source =
            new ControlledRandomAccessSource(encoded, 12, 9);
        AvifReader reader = codec.CreateRangeReader(
            source,
            new AvifDecoderOptions());

        AvifResultCode waiting = reader.Parse();
        AvifByteRange[] firstRanges = reader.GetMissingRanges();
        AvifByteRange[] secondRanges = reader.GetMissingRanges();
        source.FulfillMissingRange();
        AvifResultCode parsed = reader.Parse();
        AvifDecodeResult decoded = reader.DecodePrimaryImage();
        reader.Close();
        byte[] direct = new byte[4];
        AvifRandomAccessReadResult? directRead = source.ReadRange(0, direct);

        Assert.Multiple(() =>
        {
            Assert.That(waiting, Is.EqualTo(AvifResultCode.NeedMoreData));
            Assert.That(firstRanges, Has.Length.EqualTo(1));
            Assert.That(firstRanges[0].GetOffset(), Is.EqualTo(12));
            Assert.That(firstRanges[0].GetLength(), Is.EqualTo(9));
            Assert.That(secondRanges, Has.Length.EqualTo(1));
            Assert.That(secondRanges, Is.Not.SameAs(firstRanges));
            Assert.That(secondRanges[0], Is.Not.SameAs(firstRanges[0]));
            Assert.That(parsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.GetMissingRanges(), Is.Empty);
            Assert.That(source.ReadCallCount, Is.EqualTo(3));
            Assert.That(directRead?.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(direct, Is.EqualTo(encoded.AsSpan(0, 4).ToArray()));
        });

        (decoded.GetImage()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Close();
    }

    [Test]
    public void RangeReaderRejectsInvalidSourceResultsAndEnforcesSourceLimit()
    {
        IAvifCodec codec = new AvifCodec();
        byte[] encoded = CreateEncodedImage();
        InvalidRandomAccessSource invalid =
            new InvalidRandomAccessSource(encoded);
        AvifReader invalidReader = codec.CreateRangeReader(
            invalid,
            new AvifDecoderOptions());
        AvifDecoderOptions limitedOptions = new AvifDecoderOptions();
        limitedOptions.Limits.MaxSourceBytes = encoded.Length - 1;
        ControlledRandomAccessSource tooLarge =
            new ControlledRandomAccessSource(encoded, 0, 0);
        tooLarge.FulfillMissingRange();
        AvifReader limitedReader = codec.CreateRangeReader(
            tooLarge,
            limitedOptions);
        AvifReader nullReader = codec.CreateRangeReader(
            null,
            new AvifDecoderOptions());

        Assert.Multiple(() =>
        {
            Assert.That(invalidReader.Parse(),
                Is.EqualTo(AvifResultCode.InvalidData));
            Assert.That(invalidReader.GetMissingRanges(), Is.Empty);
            Assert.That(limitedReader.GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(limitedReader.Parse(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(tooLarge.ReadCallCount, Is.Zero);
            Assert.That(nullReader.GetCode(),
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        invalidReader.Close();
        limitedReader.Close();
        nullReader.Close();
    }

    static byte[] CreateEncodedImage()
    {
        AvifImage image = new AvifImage(
            2,
            1,
            8,
            AvifChromaFormat.Monochrome,
            AvifRange.Full,
            AvifChromaSamplePosition.Unknown,
            AvifAlphaMode.None,
            1,
            new AvifSecurityLimits());
        (image.GetPlane(AvifPlaneKind.Y)
            ?? throw new InvalidOperationException("Invalid internal state."))
            .GetRowSpan(0).Fill(73);
        byte[] encoded = new AvifCodec().EncodePlanar(
            image,
            new AvifEncoderOptions()).GetData();
        image.Close();
        return encoded;
    }
}

internal class ControlledRandomAccessSource : IAvifRandomAccessSource
{
    internal byte[] Data;
    internal long MissingOffset;
    internal long MissingLength;
    internal bool Missing;
    internal int ReadCallCount;

    internal ControlledRandomAccessSource(
        byte[] data,
        long missingOffset,
        long missingLength)
    {
        Data = data;
        MissingOffset = missingOffset;
        MissingLength = missingLength;
        Missing = true;
        ReadCallCount = 0;
    }

    public long GetLength()
    {
        return Data.LongLength;
    }

    public AvifRandomAccessReadResult? ReadRange(
        long offset,
        Span<byte> destination)
    {
        ReadCallCount++;
        long end = offset + destination.Length;
        long missingEnd = MissingOffset + MissingLength;
        if (Missing
            && MissingLength > 0
            && MissingOffset < end
            && missingEnd > offset)
        {
            destination.Fill(204);
            long overlapOffset = MissingOffset > offset
                ? MissingOffset : offset;
            long overlapEnd = missingEnd < end ? missingEnd : end;
            return new AvifRandomAccessReadResult(
                AvifResultCode.NeedMoreData,
                0,
                overlapOffset,
                overlapEnd - overlapOffset);
        }
        Data.AsSpan((int)offset, destination.Length).CopyTo(destination);
        return new AvifRandomAccessReadResult(
            AvifResultCode.Success,
            destination.Length,
            0,
            0);
    }

    internal void FulfillMissingRange()
    {
        Missing = false;
    }
}

internal class InvalidRandomAccessSource : IAvifRandomAccessSource
{
    internal byte[] Data;

    internal InvalidRandomAccessSource(byte[] data)
    {
        Data = data;
    }

    public long GetLength()
    {
        return Data.LongLength;
    }

    public AvifRandomAccessReadResult? ReadRange(
        long offset,
        Span<byte> destination)
    {
        Data.AsSpan((int)offset, destination.Length).CopyTo(destination);
        return new AvifRandomAccessReadResult(
            AvifResultCode.Success,
            destination.Length - 1,
            0,
            0);
    }
}
