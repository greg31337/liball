using System.IO;

namespace avif.Tests;

public class PublicApiBoundaryTests
{
    [Test]
    public void ArrayAndSpanEntryPointsAgreeAndReturnIndependentResults()
    {
        AvifImage image = CreateImage(3, 2, 47);
        AvifEncodeResult encoded = Avif.Encode(image);
        byte[] firstCopy = encoded.GetData();
        byte[] secondCopy = encoded.GetData();
        firstCopy[3] = 0;

        AvifProbeResult arrayProbe = Avif.Probe(secondCopy);
        AvifProbeResult spanProbe = Avif.Probe(secondCopy.AsSpan());
        AvifReadResult spanRead = Avif.Read(secondCopy.AsSpan());
        AvifReadResult spanInspect = Avif.Inspect(secondCopy.AsSpan());
        AvifValidationResult spanValidation = Avif.Validate(secondCopy.AsSpan());
        string readDump = (spanRead.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).Dump();
        System.Array.Clear(secondCopy);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(encoded.GetData()[3], Is.Not.Zero);
            Assert.That(arrayProbe.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(spanProbe.GetCode(), Is.EqualTo(arrayProbe.GetCode()));
            Assert.That(spanProbe.GetCapabilities().GetMimeType(),
                Is.EqualTo(arrayProbe.GetCapabilities().GetMimeType()));
            Assert.That(spanRead.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(spanInspect.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(spanValidation.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((spanRead.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).Dump(), Is.EqualTo(readDump));
            Assert.That((spanInspect.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetItemCount(), Is.EqualTo(1));
        });

        image.Close();
    }

    [Test]
    public void SeekableAndChunkedNonSeekableStreamsRemainCallerOwned()
    {
        AvifImage image = CreateImage(4, 3, 63);
        MemoryStream seekableDestination = new MemoryStream();
        AvifResultCode seekableWrite = Avif.Write(seekableDestination, image);
        seekableDestination.Position = 0;
        AvifDecodeResult seekableDecode = Avif.Decode(seekableDestination);

        ChunkedWriteStream nonSeekableDestination = new ChunkedWriteStream();
        AvifWriter writer = AvifWriter.Create();
        uint itemId = writer.AddImage(image);
        AvifEncodeResult finished = writer.Finish();
        AvifResultCode nonSeekableWrite = writer.Write(nonSeekableDestination);
        ChunkedReadStream nonSeekableSource = new ChunkedReadStream(
            nonSeekableDestination.ToArray(),
            1);
        AvifReader reader = AvifReader.Create(nonSeekableSource);
        AvifResultCode parse = reader.Parse();
        AvifDecodeResult decoded = reader.DecodePrimaryImage();

        Assert.Multiple(() =>
        {
            Assert.That(seekableWrite, Is.EqualTo(AvifResultCode.Success));
            Assert.That(seekableDestination.CanRead, Is.True);
            Assert.That(seekableDecode.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(itemId, Is.EqualTo(1));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(nonSeekableWrite, Is.EqualTo(AvifResultCode.Success));
            Assert.That(nonSeekableDestination.Disposed, Is.False);
            Assert.That(parse, Is.EqualTo(AvifResultCode.Success));
            Assert.That(nonSeekableSource.ReadCount, Is.GreaterThan(1));
            Assert.That(nonSeekableSource.Disposed, Is.False);
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
        });
        AssertPlaneValue(seekableDecode.GetImage(), 63);
        AssertPlaneValue(decoded.GetImage(), 63);

        image.Close();
        (seekableDecode.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
        writer.Close();
        seekableDestination.Dispose();
        nonSeekableDestination.Dispose();
        nonSeekableSource.Dispose();
    }

    [Test]
    public void IncrementalReaderCopiesSpanChunksAndMissingRangeArrays()
    {
        AvifImage image = CreateImage(3, 3, 81);
        byte[] encoded = Avif.Encode(image).GetData();
        byte[] firstChunk = encoded.AsSpan(0, 8).ToArray();
        AvifIncrementalReader reader = AvifIncrementalReader.Create();

        Assert.That(reader.Append(firstChunk.AsSpan()),
            Is.EqualTo(AvifResultCode.Success));
        System.Array.Clear(firstChunk);
        Assert.That(reader.ParseAvailable(), Is.EqualTo(AvifResultCode.NeedMoreData));
        AvifByteRange?[] firstRanges = reader.GetMissingRanges();
        Assert.That(firstRanges, Has.Length.EqualTo(1));
        firstRanges[0] = null;
        AvifByteRange[] secondRanges = reader.GetMissingRanges();

        int offset = 8;
        while (offset < encoded.Length)
        {
            int length = System.Math.Min(7, encoded.Length - offset);
            Assert.That(reader.Append(encoded.AsSpan(offset, length)),
                Is.EqualTo(AvifResultCode.Success));
            offset += length;
        }
        Assert.That(reader.Complete(), Is.EqualTo(AvifResultCode.Success));
        AvifDecodeResult decoded = reader.DecodeAvailable();

        Assert.Multiple(() =>
        {
            Assert.That(secondRanges, Has.Length.EqualTo(1));
            Assert.That(secondRanges[0], Is.Not.Null);
            Assert.That(secondRanges[0].GetOffset(), Is.EqualTo(8));
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.GetDecodedRowCount(), Is.EqualTo(3));
            Assert.That(reader.GetReceivedByteCount(), Is.EqualTo(encoded.Length));
            Assert.That(reader.IsComplete(), Is.True);
        });
        AssertPlaneValue(decoded.GetImage(), 81);

        image.Close();
        (decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
    }

    [Test]
    public void ReadersAndWritersSnapshotConfiguredResourceLimits()
    {
        AvifImage image = CreateImage(2, 2, 29);
        byte[] encoded = Avif.Encode(image).GetData();

        AvifDecoderOptions allowedOptions = new AvifDecoderOptions();
        allowedOptions.Limits.MaxSourceBytes = encoded.Length;
        AvifReader allowed = AvifReader.Create(encoded, allowedOptions);
        allowedOptions.Limits.MaxSourceBytes = 1;

        AvifDecoderOptions blockedOptions = new AvifDecoderOptions();
        blockedOptions.Limits.MaxSourceBytes = encoded.Length - 1;
        AvifReader blocked = AvifReader.Create(encoded, blockedOptions);
        blockedOptions.Limits.MaxSourceBytes = encoded.Length;

        AvifEncoderOptions writerOptions = new AvifEncoderOptions();
        (writerOptions.Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxSourceBytes = 65536;
        AvifWriter writer = AvifWriter.Create(writerOptions);
        writerOptions.Limits.MaxSourceBytes = 1;
        uint itemId = writer.AddImage(image);
        AvifEncodeResult written = writer.Finish();

        Assert.Multiple(() =>
        {
            Assert.That(allowed.Parse(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(blocked.Parse(), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(itemId, Is.EqualTo(1));
            Assert.That(written.GetCode(), Is.EqualTo(AvifResultCode.Success));
        });

        image.Close();
        allowed.Close();
        blocked.Close();
        writer.Close();
    }

    [Test]
    public void MalformedInputPublishesDefensiveStructuredDiagnostics()
    {
        byte[] truncatedFileType = new byte[]
        {
            0, 0, 0, 20,
            (byte)'f', (byte)'t', (byte)'y', (byte)'p',
            (byte)'a', (byte)'v', (byte)'i', (byte)'f'
        };
        AvifValidationResult validation = Avif.Validate(truncatedFileType);
        AvifDocument? document = validation.GetDocument();

        Assert.That(validation.GetCode(), Is.EqualTo(AvifResultCode.TruncatedData));
        Assert.That(validation.GetFindingCount(), Is.GreaterThan(0));
        AvifDiagnostic? first = validation.GetFinding(0);
        Assert.That(first, Is.Not.Null);
        AvifResultCode originalCode = (first ?? throw new InvalidOperationException("Invalid internal state.")).Code;
        string originalMessage = first.Message;
        string originalPath = first.BoxPath;
        first.Code = AvifResultCode.Success;
        first.Message = "changed";
        first.BoxPath = "/changed";
        AvifDiagnostic? again = (document ?? throw new InvalidOperationException("Invalid internal state.")).GetFinding(0);

        Assert.Multiple(() =>
        {
            Assert.That(originalCode, Is.Not.EqualTo(AvifResultCode.Success));
            Assert.That(originalMessage, Is.Not.Empty);
            Assert.That(again, Is.Not.SameAs(first));
            Assert.That((again ?? throw new InvalidOperationException("Invalid internal state.")).Code, Is.EqualTo(originalCode));
            Assert.That(again.Message, Is.EqualTo(originalMessage));
            Assert.That(again.BoxPath, Is.EqualTo(originalPath));
            Assert.That(again.Severity,
                Is.AnyOf(AvifDiagnosticSeverity.Error, AvifDiagnosticSeverity.Fatal));
        });
    }

    [Test]
    public void CancellationAndClosedLifecycleReturnStableCodes()
    {
        AvifImage image = CreateImage(2, 1, 117);
        byte[] encoded = Avif.Encode(image).GetData();
        AvifDecoderOptions cancelledOptions = new AvifDecoderOptions();
        AvifReader reader = AvifReader.Create(encoded, cancelledOptions);
        Assert.That(reader.Parse(), Is.EqualTo(AvifResultCode.Success));
        cancelledOptions.Cancellation.Cancel();
        AvifDecodeResult cancelled = reader.DecodePrimaryImage();
        Assert.That(cancelled.GetCode(), Is.EqualTo(AvifResultCode.Cancelled));
        Assert.That(reader.Close(), Is.EqualTo(AvifResultCode.Success));

        AvifIncrementalReader incremental = AvifIncrementalReader.Create();
        incremental.Append(encoded.AsSpan(0, 8));
        Assert.That(incremental.Close(), Is.EqualTo(AvifResultCode.Success));

        Av1Encoder encoder = new Av1Encoder();
        Assert.That(encoder.AddFrame(image, 1), Is.EqualTo(AvifResultCode.Success));
        AvifEncodeResult av1 = encoder.Finish();
        Av1DecoderOptions av1Options = new Av1DecoderOptions();
        Av1Decoder decoder = new Av1Decoder();
        Assert.That(decoder.Configure(av1Options), Is.EqualTo(AvifResultCode.Success));
        av1Options.Cancellation.Cancel();
        AvifDecodeResult av1Cancelled = decoder.Decode(av1.GetData().AsSpan());
        Assert.That(av1Cancelled.GetCode(), Is.EqualTo(AvifResultCode.Cancelled));
        Assert.That(encoder.Close(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(decoder.Close(), Is.EqualTo(AvifResultCode.Success));

        Assert.Multiple(() =>
        {
            Assert.That(reader.Parse(), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(reader.DecodePrimaryImage().GetCode(),
                Is.EqualTo(AvifResultCode.Closed));
            Assert.That(incremental.Append(new byte[] { 1 }),
                Is.EqualTo(AvifResultCode.Closed));
            Assert.That(incremental.DecodeAvailable().GetCode(),
                Is.EqualTo(AvifResultCode.Closed));
            Assert.That(encoder.AddFrame(image, 1), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(encoder.Finish().GetCode(), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(decoder.Reset(), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(reader.Close(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(incremental.Close(), Is.EqualTo(AvifResultCode.Success));
        });

        image.Close();
    }

    static AvifImage CreateImage(int width, int height, byte value)
    {
        AvifImage image = AvifImage.Create(
            width,
            height,
            8,
            AvifChromaFormat.Monochrome,
            false);
        AvifPlane? plane = image.GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < height)
        {
            (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(row).Fill(value);
            row++;
        }
        return image;
    }

    static void AssertPlaneValue(AvifImage? image, byte value)
    {
        Assert.That(image, Is.Not.Null);
        AvifPlane? plane = (image ?? throw new InvalidOperationException("Invalid internal state.")).GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            byte[] values = plane.GetRowSpan(row).ToArray();
            int column = 0;
            while (column < values.Length)
            {
                Assert.That(values[column], Is.EqualTo(value));
                column++;
            }
            row++;
        }
    }

    class ChunkedReadStream : Stream
    {
        MemoryStream source;
        int maximumReadSize;

        internal bool Disposed;
        internal int ReadCount;

        internal ChunkedReadStream(byte[] data, int maximumReadSize)
        {
            source = new MemoryStream(data, false);
            this.maximumReadSize = maximumReadSize;
            Disposed = false;
            ReadCount = 0;
        }

        public override bool CanRead => !Disposed;
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
            ReadCount++;
            return source.Read(buffer, offset, System.Math.Min(count, maximumReadSize));
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
            Disposed = true;
            source.Dispose();
            base.Dispose(disposing);
        }
    }

    class ChunkedWriteStream : Stream
    {
        MemoryStream destination = new MemoryStream();

        internal bool Disposed;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => !Disposed;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        internal byte[] ToArray()
        {
            return destination.ToArray();
        }

        public override void Flush()
        {
            destination.Flush();
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
            destination.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            destination.Dispose();
            base.Dispose(disposing);
        }
    }
}
