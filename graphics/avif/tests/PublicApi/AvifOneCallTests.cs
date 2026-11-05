using System.IO;

namespace avif.Tests;

public class AvifOneCallTests
{
    [Test]
    public void EncodeProbeInspectValidateAndDecodeRoundTripPlanarSamples()
    {
        AvifImage source = CreateImage();

        AvifEncodeResult encoded = Avif.Encode(source);
        AvifProbeResult probe = Avif.Probe(encoded.GetData());
        AvifReadResult inspected = Avif.Inspect(encoded.GetData());
        AvifValidationResult validated = Avif.Validate(encoded.GetData());
        AvifDecodeResult decoded = Avif.Decode(encoded.GetData());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(encoded.GetDataLength(), Is.GreaterThan(0));
            Assert.That(probe.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(probe.IsAvif(), Is.True);
            Assert.That(probe.GetCapabilities().GetMimeType(), Is.EqualTo("image/avif"));
            Assert.That(inspected.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((inspected.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetItemCount(), Is.EqualTo(1));
            Assert.That(validated.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(3));
            Assert.That((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight(), Is.EqualTo(2));
            Assert.That((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetBitDepth(), Is.EqualTo(10));
            Assert.That((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetChromaFormat(), Is.EqualTo(AvifChromaFormat.Yuv420));
            Assert.That((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetAlphaMode(), Is.EqualTo(AvifAlphaMode.Straight));
        });
        AssertImagesEqual(source, decoded.GetImage());

        source.Close();
        (decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
    }

    [Test]
    public void StreamWriteAndReadLeaveNonSeekableStreamsOpen()
    {
        AvifImage image = CreateImage();
        NonSeekableWriteStream destination = new NonSeekableWriteStream();

        AvifResultCode writeCode = Avif.Write(destination, image);
        byte[] data = destination.ToArray();
        NonSeekableReadStream source = new NonSeekableReadStream(data);
        AvifReadResult read = Avif.Read(source);
        AvifDecodeResult decoded = Avif.Decode(new NonSeekableReadStream(data));

        Assert.Multiple(() =>
        {
            Assert.That(writeCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(destination.Closed, Is.False);
            Assert.That(data, Is.Not.Empty);
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(source.Closed, Is.False);
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
        });

        image.Close();
        (decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
    }

    [Test]
    public void SeekableStreamOperationsStartAtTheCallerPosition()
    {
        AvifImage image = CreateImage();
        byte[] encoded = Avif.Encode(image).GetData();
        byte[] prefixed = new byte[encoded.Length + 5];
        encoded.CopyTo(prefixed, 5);
        MemoryStream readStream = new MemoryStream(prefixed);
        readStream.Position = 5;
        AvifReadResult read = Avif.Read(readStream);
        MemoryStream decodeStream = new MemoryStream(prefixed);
        decodeStream.Position = 5;
        AvifDecodeResult decoded = Avif.Decode(decodeStream);

        Assert.Multiple(() =>
        {
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(readStream.Position, Is.EqualTo(readStream.Length));
            Assert.That(decodeStream.Position, Is.EqualTo(decodeStream.Length));
        });

        image.Close();
        (decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        readStream.Dispose();
        decodeStream.Dispose();
    }

    [Test]
    public void ProbeAndOperationsReturnStableArgumentLimitAndCancellationCodes()
    {
        AvifProbeResult? nullProbe = Avif.Probe((byte[]?)null);
        AvifProbeResult shortProbe = Avif.Probe(new byte[] { 0, 0, 0, 16 });
        AvifProbeResult otherProbe = Avif.Probe(BuildFileType("heic", "mif1"));
        AvifReadResult? nullRead = Avif.Read((byte[]?)null);
        AvifImage image = CreateImage();
        AvifEncoderOptions limited = new AvifEncoderOptions();
        (limited.Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxSourceBytes = 32;
        AvifEncodeResult limitedEncode = Avif.Encode(image, limited);
        AvifDecoderOptions cancelled = new AvifDecoderOptions();
        cancelled.Cancellation.Cancel();
        AvifDecodeResult cancelledDecode = Avif.Decode(
            Avif.Encode(image).GetData(),
            cancelled);

        Assert.Multiple(() =>
        {
            Assert.That(nullProbe.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(shortProbe.GetCode(), Is.EqualTo(AvifResultCode.NeedMoreData));
            Assert.That(otherProbe.GetCode(), Is.EqualTo(AvifResultCode.NotFound));
            Assert.That(otherProbe.GetCapabilities().GetMimeType(), Is.EqualTo("image/heif"));
            Assert.That(nullRead.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(limitedEncode.GetCode(), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(cancelledDecode.GetCode(), Is.EqualTo(AvifResultCode.Cancelled));
        });

        image.Close();
    }

    [Test]
    public void StreamDecodeStopsBufferingAtCancellationAndLeavesInputOpen()
    {
        AvifImage image = CreateImage();
        byte[] encoded = Avif.Encode(image).GetData();
        AvifDecoderOptions options = new AvifDecoderOptions();
        CancellingReadStream stream = new CancellingReadStream(
            encoded,
            options.Cancellation,
            16);

        AvifDecodeResult result = Avif.Decode(stream, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetCode(), Is.EqualTo(AvifResultCode.Cancelled));
            Assert.That(stream.ReadCount, Is.EqualTo(1));
            Assert.That(stream.Closed, Is.False);
            Assert.That(stream.PositionValue, Is.EqualTo(16));
        });

        image.Close();
        stream.Dispose();
    }

    [Test]
    public void NamedByteAndSpanOperationsCoverTheCompletePlanarWorkflow()
    {
        IAvifCodec codec = new AvifCodec();
        AvifImage image = CreateImage();
        AvifEncoderOptions encoderOptions = new AvifEncoderOptions();
        AvifDecoderOptions decoderOptions = new AvifDecoderOptions();
        AvifValidationOptions validationOptions = new AvifValidationOptions();

        AvifEncodeResult encoded = codec.EncodePlanar(image, encoderOptions);
        byte[] data = encoded.GetData();
        AvifProbeResult byteProbe = codec.ProbeBytes(data);
        AvifProbeResult spanProbe = codec.ProbeSpan(data.AsSpan());
        AvifReadResult byteInspection = codec.InspectBytes(
            data,
            validationOptions);
        AvifReadResult spanInspection = codec.InspectSpan(
            data.AsSpan(),
            validationOptions);
        AvifValidationResult byteValidation = codec.ValidateBytes(
            data,
            validationOptions);
        AvifValidationResult spanValidation = codec.ValidateSpan(
            data.AsSpan(),
            validationOptions);
        AvifDecodeResult byteDecode = codec.DecodePlanarBytes(
            data,
            decoderOptions);
        AvifDecodeResult spanDecode = codec.DecodePlanarSpan(
            data.AsSpan(),
            decoderOptions);
        AvifEncodeResult byteTranscode = codec.TranscodeBytes(
            data,
            decoderOptions,
            encoderOptions);
        AvifEncodeResult spanTranscode = codec.TranscodeSpan(
            data.AsSpan(),
            decoderOptions,
            encoderOptions);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(byteProbe.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(spanProbe.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(byteInspection.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(spanInspection.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(byteValidation.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(spanValidation.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(byteDecode.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(spanDecode.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(byteTranscode.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(spanTranscode.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(codec.ProbeBytes(byteTranscode.GetData()).IsAvif(), Is.True);
            Assert.That(codec.ProbeBytes(spanTranscode.GetData()).IsAvif(), Is.True);
        });
        AssertImagesEqual(image, byteDecode.GetImage());
        AssertImagesEqual(image, spanDecode.GetImage());

        image.Close();
        (byteDecode.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (spanDecode.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
    }

    [Test]
    public void NamedStreamOperationsLeaveEveryCallerOwnedStreamOpen()
    {
        IAvifCodec codec = new AvifCodec();
        AvifImage image = CreateImage();
        AvifEncoderOptions encoderOptions = new AvifEncoderOptions();
        AvifDecoderOptions decoderOptions = new AvifDecoderOptions();
        AvifValidationOptions validationOptions = new AvifValidationOptions();
        byte[] data = codec.EncodePlanar(image, encoderOptions).GetData();
        NonSeekableReadStream probeSource = new NonSeekableReadStream(data);
        NonSeekableReadStream inspectSource = new NonSeekableReadStream(data);
        NonSeekableReadStream validateSource = new NonSeekableReadStream(data);
        NonSeekableReadStream decodeSource = new NonSeekableReadStream(data);
        NonSeekableReadStream transcodeSource = new NonSeekableReadStream(data);
        NonSeekableWriteStream destination = new NonSeekableWriteStream();

        AvifProbeResult probe = codec.ProbeStream(probeSource);
        AvifReadResult inspection = codec.InspectStream(
            inspectSource,
            validationOptions);
        AvifValidationResult validation = codec.ValidateStream(
            validateSource,
            validationOptions);
        AvifDecodeResult decode = codec.DecodePlanarStream(
            decodeSource,
            decoderOptions);
        AvifEncodeResult transcode = codec.TranscodeStream(
            transcodeSource,
            decoderOptions,
            encoderOptions);
        AvifResultCode write = codec.WritePlanarStream(
            destination,
            image,
            encoderOptions);

        Assert.Multiple(() =>
        {
            Assert.That(probe.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(inspection.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(validation.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(decode.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(transcode.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(write, Is.EqualTo(AvifResultCode.Success));
            Assert.That(probeSource.Closed, Is.False);
            Assert.That(inspectSource.Closed, Is.False);
            Assert.That(validateSource.Closed, Is.False);
            Assert.That(decodeSource.Closed, Is.False);
            Assert.That(transcodeSource.Closed, Is.False);
            Assert.That(destination.Closed, Is.False);
            Assert.That(codec.ProbeBytes(destination.ToArray()).IsAvif(), Is.True);
        });

        image.Close();
        (decode.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        probeSource.Dispose();
        inspectSource.Dispose();
        validateSource.Dispose();
        decodeSource.Dispose();
        transcodeSource.Dispose();
        destination.Dispose();
    }

    [Test]
    public void NamedFileOperationsCloseHandlesAndPreserveTheSourceFile()
    {
        IAvifCodec codec = new AvifCodec();
        AvifImage image = CreateImage();
        AvifEncoderOptions encoderOptions = new AvifEncoderOptions();
        AvifDecoderOptions decoderOptions = new AvifDecoderOptions();
        AvifValidationOptions validationOptions = new AvifValidationOptions();
        byte[] data = codec.EncodePlanar(image, encoderOptions).GetData();
        string directory = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "one-call-" + Guid.NewGuid().ToString("N"));
        string sourcePath = Path.Combine(directory, "source.avif");
        string destinationPath = Path.Combine(directory, "destination.avif");
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(sourcePath, data);
        AvifDecodeResult decode = new AvifDecodeResult(
            AvifResultCode.InvalidState,
            null,
            null);

        try
        {
            AvifProbeResult probe = codec.ProbeFile(sourcePath);
            AvifReadResult inspection = codec.InspectFile(
                sourcePath,
                validationOptions);
            AvifValidationResult validation = codec.ValidateFile(
                sourcePath,
                validationOptions);
            decode = codec.DecodePlanarFile(sourcePath, decoderOptions);
            AvifEncodeResult transcode = codec.TranscodeFile(
                sourcePath,
                decoderOptions,
                encoderOptions);
            AvifResultCode write = codec.WritePlanarFile(
                destinationPath,
                image,
                encoderOptions);
            using (FileStream exclusiveSource = new FileStream(
                sourcePath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None))
            {
                Assert.That(exclusiveSource.CanRead, Is.True);
            }
            using (FileStream exclusiveDestination = new FileStream(
                destinationPath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None))
            {
                Assert.That(exclusiveDestination.CanWrite, Is.True);
            }

            Assert.Multiple(() =>
            {
                Assert.That(probe.GetCode(), Is.EqualTo(AvifResultCode.Success));
                Assert.That(inspection.GetCode(), Is.EqualTo(AvifResultCode.Success));
                Assert.That(validation.GetCode(), Is.EqualTo(AvifResultCode.Success));
                Assert.That(decode.GetCode(), Is.EqualTo(AvifResultCode.Success));
                Assert.That(transcode.GetCode(), Is.EqualTo(AvifResultCode.Success));
                Assert.That(write, Is.EqualTo(AvifResultCode.Success));
                Assert.That(File.ReadAllBytes(sourcePath), Is.EqualTo(data));
                Assert.That(codec.ProbeFile(destinationPath).IsAvif(), Is.True);
            });
        }
        finally
        {
            image.Close();
            decode.GetImage()?.Close();
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void NamedOperationsReturnStableCodesForMissingSourcesAndDestinations()
    {
        IAvifCodec codec = new AvifCodec();
        AvifImage image = CreateImage();
        string missing = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            Guid.NewGuid().ToString("N") + ".avif");

        Assert.Multiple(() =>
        {
            Assert.That(codec.ProbeBytes(null).GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(codec.ProbeStream(null).GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(codec.ProbeFile(null).GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(codec.ProbeFile(missing).GetCode(), Is.EqualTo(AvifResultCode.IoError));
            Assert.That(codec.InspectFile(missing, new AvifValidationOptions()).GetCode(), Is.EqualTo(AvifResultCode.IoError));
            Assert.That(codec.ValidateFile(missing, new AvifValidationOptions()).GetCode(), Is.EqualTo(AvifResultCode.IoError));
            Assert.That(codec.DecodePlanarFile(missing, new AvifDecoderOptions()).GetCode(), Is.EqualTo(AvifResultCode.IoError));
            Assert.That(codec.TranscodeFile(missing, new AvifDecoderOptions(), new AvifEncoderOptions()).GetCode(), Is.EqualTo(AvifResultCode.IoError));
            Assert.That(codec.WritePlanarFile(null, image, new AvifEncoderOptions()), Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        image.Close();
    }

    static AvifImage CreateImage()
    {
        AvifImage image = AvifImage.Create(
            3,
            2,
            10,
            AvifChromaFormat.Yuv420,
            AvifRange.Limited,
            AvifChromaSamplePosition.Vertical,
            AvifAlphaMode.Straight,
            8,
            new AvifSecurityLimits());
        int kind = 0;
        while (kind <= (int)AvifPlaneKind.Alpha)
        {
            AvifPlane? plane = image.GetPlane((AvifPlaneKind)kind);
            if (plane != null)
            {
                int row = 0;
                while (row < plane.GetHeight())
                {
                    Span<byte> bytes = plane.GetRowSpan(row);
                    int index = 0;
                    while (index < bytes.Length)
                    {
                        bytes[index] = (byte)(kind * 41 + row * 13 + index);
                        index++;
                    }
                    row++;
                }
            }
            kind++;
        }
        return image;
    }

    static void AssertImagesEqual(AvifImage expected, AvifImage? actual)
    {
        int kind = 0;
        while (kind <= (int)AvifPlaneKind.Alpha)
        {
            AvifPlane? expectedPlane = expected.GetPlane((AvifPlaneKind)kind);
            AvifPlane? actualPlane = (actual ?? throw new InvalidOperationException("Invalid internal state.")).GetPlane((AvifPlaneKind)kind);
            Assert.That(actualPlane == null, Is.EqualTo(expectedPlane == null));
            if (expectedPlane != null)
            {
                int row = 0;
                while (row < expectedPlane.GetHeight())
                {
                    Assert.That(
                        (actualPlane ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(row).ToArray(),
                        Is.EqualTo(expectedPlane.GetRowSpan(row).ToArray()));
                    row++;
                }
            }
            kind++;
        }
    }

    static byte[] BuildFileType(string majorBrand, string compatibleBrand)
    {
        byte[] data = new byte[20];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(data, 20);
        WriteFourCc(data, 4, "ftyp");
        WriteFourCc(data, 8, majorBrand);
        WriteFourCc(data, 16, compatibleBrand);
        return data;
    }

    static void WriteFourCc(byte[] data, int offset, string value)
    {
        data[offset] = (byte)value[0];
        data[offset + 1] = (byte)value[1];
        data[offset + 2] = (byte)value[2];
        data[offset + 3] = (byte)value[3];
    }

    class NonSeekableWriteStream : Stream
    {
        MemoryStream destination = new MemoryStream();
        internal bool Closed;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => !Closed;
        public override long Length => destination.Length;
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public byte[] ToArray()
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
            destination.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            destination.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            Closed = true;
            destination.Dispose();
            base.Dispose(disposing);
        }
    }

    class NonSeekableReadStream : Stream
    {
        MemoryStream source;
        internal bool Closed;

        internal NonSeekableReadStream(byte[] data)
        {
            source = new MemoryStream(data, false);
        }

        public override bool CanRead => !Closed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => source.Length;
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
            return source.Read(buffer, offset, count);
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
            Closed = true;
            source.Dispose();
            base.Dispose(disposing);
        }
    }

    class CancellingReadStream : Stream
    {
        MemoryStream source;
        AvifCancellation cancellation;
        int maximumReadSize;
        internal bool Closed;
        internal int ReadCount;
        internal long PositionValue;

        internal CancellingReadStream(
            byte[] data,
            AvifCancellation cancellation,
            int maximumReadSize)
        {
            source = new MemoryStream(data, false);
            this.cancellation = cancellation;
            this.maximumReadSize = maximumReadSize;
        }

        public override bool CanRead => !Closed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => source.Length;
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
            int requested = count < maximumReadSize
                ? count : maximumReadSize;
            int read = source.Read(buffer, offset, requested);
            ReadCount++;
            PositionValue += read;
            cancellation.Cancel();
            return read;
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
            Closed = true;
            source.Dispose();
            base.Dispose(disposing);
        }
    }
}
