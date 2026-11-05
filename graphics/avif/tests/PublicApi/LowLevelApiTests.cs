namespace avif.Tests;

public class LowLevelApiTests
{
    [Test]
    public void BoxWriterAndReaderRoundTripNestedBoxesWithIndependentBuffers()
    {
        AvifBoxWriter writer = AvifBoxWriter.Create();
        Assert.That(writer.BeginFullBox("meta", 2, 0x123456),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(writer.EndBox(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(writer.BeginBox("moov"), Is.EqualTo(AvifResultCode.Success));
        Assert.That(writer.BeginBox("free"), Is.EqualTo(AvifResultCode.Success));
        Assert.That(writer.Write(new byte[] { 3, 5, 8 }),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(writer.EndBox(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(writer.EndBox(), Is.EqualTo(AvifResultCode.Success));

        AvifEncodeResult encoded = writer.Finish();
        byte[] source = encoded.GetData();
        AvifBoxReader reader = AvifBoxReader.Create(source);
        source[0] = 0;

        Assert.That(reader.ReadNext(), Is.EqualTo(AvifResultCode.Success));
        AvifBox? meta = reader.GetCurrentBox();
        Assert.Multiple(() =>
        {
            Assert.That((meta ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName(), Is.EqualTo("meta"));
            Assert.That(meta.IsFullBox(), Is.True);
            Assert.That(meta.GetVersion(), Is.EqualTo(2));
            Assert.That(meta.GetFlags(), Is.EqualTo(0x123456));
        });

        Assert.That(reader.ReadNext(), Is.EqualTo(AvifResultCode.Success));
        Assert.That((reader.GetCurrentBox() ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName(), Is.EqualTo("moov"));
        Assert.That(reader.Enter(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(reader.ReadNext(), Is.EqualTo(AvifResultCode.Success));
        Assert.That((reader.GetCurrentBox() ?? throw new InvalidOperationException("Invalid internal state.")).GetPath(), Is.EqualTo("/moov/free"));
        byte[] payload = reader.GetPayload();
        payload[0] = 99;
        Assert.That(reader.GetPayload(), Is.EqualTo(new byte[] { 3, 5, 8 }));
        Assert.That(reader.Skip(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(reader.ReadNext(), Is.EqualTo(AvifResultCode.EndOfData));
        Assert.That(reader.Leave(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(reader.ReadNext(), Is.EqualTo(AvifResultCode.EndOfData));

        Assert.That(reader.Close(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(reader.ReadNext(), Is.EqualTo(AvifResultCode.Closed));
        Assert.That(writer.Close(), Is.EqualTo(AvifResultCode.Success));
    }

    [Test]
    public void BoxWriterEnforcesBoundsAndConstructionLifecycle()
    {
        AvifBoxWriter bounded = AvifBoxWriter.Create(10);
        Assert.That(bounded.BeginBox("free"), Is.EqualTo(AvifResultCode.Success));
        Assert.That(bounded.Write(new byte[] { 1, 2, 3 }),
            Is.EqualTo(AvifResultCode.LimitExceeded));
        Assert.That(bounded.Finish().GetCode(),
            Is.EqualTo(AvifResultCode.LimitExceeded));

        AvifBoxWriter unfinished = AvifBoxWriter.Create();
        Assert.That(unfinished.BeginBox("free"), Is.EqualTo(AvifResultCode.Success));
        Assert.That(unfinished.Finish().GetCode(),
            Is.EqualTo(AvifResultCode.InvalidState));

        AvifBoxWriter complete = AvifBoxWriter.Create();
        complete.BeginBox("free");
        complete.EndBox();
        AvifEncodeResult first = complete.Finish();
        byte[] changed = first.GetData();
        changed[3] = 0;
        Assert.That(complete.Finish().GetData(), Is.Not.EqualTo(changed));
        Assert.That(complete.BeginBox("skip"),
            Is.EqualTo(AvifResultCode.InvalidState));

        bounded.Close();
        unfinished.Close();
        complete.Close();
    }

    [Test]
    public void ObuReaderReturnsDefensiveMetadataPayloadsAndCanReset()
    {
        byte[] source = new byte[] { 0x2A, 0x02, 0x04, 0x80 };
        Av1ObuReader reader = Av1ObuReader.Create(source);
        source[2] = 0;

        Assert.That(reader.ReadNext(), Is.EqualTo(AvifResultCode.Success));
        Assert.Multiple(() =>
        {
            Assert.That(reader.GetObuType(), Is.EqualTo(5));
            Assert.That(reader.HasExtension(), Is.False);
            Assert.That(reader.GetOffset(), Is.Zero);
            Assert.That(reader.GetSize(), Is.EqualTo(4));
            Assert.That(reader.GetSequenceHeader(), Is.Empty);
            Assert.That(reader.GetMetadata(), Is.EqualTo(new byte[] { 0x04, 0x80 }));
        });
        byte[] payload = reader.GetPayload();
        payload[0] = 0;
        Assert.That(reader.GetPayload(), Is.EqualTo(new byte[] { 0x04, 0x80 }));
        Assert.That(reader.ReadNext(), Is.EqualTo(AvifResultCode.EndOfData));
        Assert.That(reader.Reset(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(reader.ReadNext(), Is.EqualTo(AvifResultCode.Success));

        Av1ObuReader truncated = Av1ObuReader.Create(
            new byte[] { 0x2A, 0x04, 0x80 });
        Assert.That(truncated.GetCode(), Is.EqualTo(AvifResultCode.TruncatedData));
        Assert.That(truncated.ReadNext(), Is.EqualTo(AvifResultCode.TruncatedData));

        reader.Close();
        truncated.Close();
    }

    [Test]
    public void LowLevelReadersRejectSourcesBeforeCopyingBeyondConfiguredLimits()
    {
        AvifSecurityLimits boxLimits = new AvifSecurityLimits();
        boxLimits.MaxSourceBytes = 7;
        AvifBoxReader boxReader = AvifBoxReader.Create(
            new byte[] { 0, 0, 0, 8, 102, 114, 101, 101 },
            boxLimits);

        AvifSecurityLimits obuSourceLimits = new AvifSecurityLimits();
        obuSourceLimits.MaxSourceBytes = 3;
        Av1ObuReader sourceLimitedObuReader = Av1ObuReader.Create(
            new byte[] { 0x2A, 2, 0x04, 0x80 },
            obuSourceLimits);

        AvifSecurityLimits obuStorageLimits = new AvifSecurityLimits();
        obuStorageLimits.MaxTemporaryBytes = 1;
        Av1ObuReader storageLimitedObuReader = Av1ObuReader.Create(
            new byte[] { 0x2A, 2, 0x04, 0x80 },
            obuStorageLimits);

        Assert.Multiple(() =>
        {
            Assert.That(boxReader.GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(boxReader.ReadNext(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(sourceLimitedObuReader.GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(sourceLimitedObuReader.ReadNext(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(storageLimitedObuReader.GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
        });

        boxReader.Close();
        sourceLimitedObuReader.Close();
        storageLimitedObuReader.Close();
    }

    [Test]
    public void Av1EncoderAndDecoderRoundTripQueuedFramesWithoutSharingState()
    {
        AvifImage firstImage = CreateImage(17);
        AvifImage secondImage = CreateImage(91);
        Av1Encoder encoder = new Av1Encoder();

        Assert.That(encoder.AddFrame(firstImage, 2),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(encoder.AddFrame(secondImage, 3),
            Is.EqualTo(AvifResultCode.Success));
        (firstImage.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0).Fill(200);
        firstImage.Close();
        secondImage.Close();

        AvifEncodeResult encoded = encoder.Finish();
        byte[] stream = encoded.GetData();
        Av1ObuReader obuReader = Av1ObuReader.Create(stream);
        Assert.That(obuReader.ReadNext(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(obuReader.GetObuType(), Is.EqualTo(1));
        Assert.That(obuReader.GetSequenceHeader(), Is.Not.Empty);

        Av1Decoder decoder = new Av1Decoder();
        AvifDecodeResult first = decoder.Decode(stream);
        stream[0] = 0;
        AvifDecodeResult second = decoder.DecodeNext();
        AvifDecodeResult end = decoder.DecodeNext();

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(first.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(second.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(end.GetCode(), Is.EqualTo(AvifResultCode.EndOfData));
        });
        AssertPlaneValue(first.GetImage(), 17);
        AssertPlaneValue(second.GetImage(), 91);

        (first.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (second.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        obuReader.Close();
        decoder.Close();
        encoder.Close();
    }

    [Test]
    public void Av1WrappersValidateConfigurationCancellationAndLifecycle()
    {
        Av1EncoderOptions invalidEncoderOptions = new Av1EncoderOptions();
        invalidEncoderOptions.WorkerCount = 0;
        Av1Encoder encoder = new Av1Encoder();
        Assert.That(encoder.Configure(invalidEncoderOptions),
            Is.EqualTo(AvifResultCode.InvalidArgument));
        Assert.That(encoder.Finish().GetCode(),
            Is.EqualTo(AvifResultCode.InvalidArgument));

        Av1DecoderOptions cancelledOptions = new Av1DecoderOptions();
        cancelledOptions.Cancellation.Cancel();
        Av1Decoder decoder = new Av1Decoder();
        Assert.That(decoder.Configure(cancelledOptions),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(decoder.Decode(new byte[] { 0x12, 0x00 }).GetCode(),
            Is.EqualTo(AvifResultCode.Cancelled));
        Assert.That(decoder.Reset(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(decoder.DecodeNext().GetCode(),
            Is.EqualTo(AvifResultCode.InvalidState));
        Assert.That(decoder.Close(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(decoder.Reset(), Is.EqualTo(AvifResultCode.Closed));

        encoder.Close();
    }

    [Test]
    public void DecoderOperatingPointSelectionValidatesStreamAndLifecycleState()
    {
        AvifImage image = CreateImage(47);
        Av1Encoder encoder = new Av1Encoder();
        encoder.AddFrame(image, 1);
        byte[] stream = encoder.Finish().GetData();
        Av1Decoder decoder = new Av1Decoder();

        Assert.That(decoder.SelectOperatingPoint(-2),
            Is.EqualTo(AvifResultCode.InvalidArgument));
        Assert.That(decoder.SelectOperatingPoint(0),
            Is.EqualTo(AvifResultCode.Success));
        AvifDecodeResult decoded = decoder.Decode(stream);
        Assert.That(decoder.SelectOperatingPoint(0),
            Is.EqualTo(AvifResultCode.InvalidState));
        Assert.That(decoder.Reset(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(decoder.SelectOperatingPoint(1),
            Is.EqualTo(AvifResultCode.Success));
        AvifDecodeResult missing = decoder.Decode(stream);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(missing.GetCode(), Is.EqualTo(AvifResultCode.NotFound));
        });

        (decoded.GetImage()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .Close();
        Assert.That(decoder.Close(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(decoder.SelectOperatingPoint(0),
            Is.EqualTo(AvifResultCode.Closed));
        encoder.Close();
        image.Close();
    }

    static AvifImage CreateImage(byte value)
    {
        AvifImage image = AvifImage.Create(
            3,
            2,
            8,
            AvifChromaFormat.Monochrome,
            false);
        AvifPlane? plane = image.GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            plane.GetRowSpan(row).Fill(value);
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
            byte[] actual = plane.GetRowSpan(row).ToArray();
            int index = 0;
            while (index < actual.Length)
            {
                Assert.That(actual[index], Is.EqualTo(value));
                index++;
            }
            row++;
        }
    }
}
