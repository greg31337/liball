using raw;

namespace raw.Tests;

public class PublicDescriptorUtilityTests
{
    [Test]
    public void PublicServiceCreatesGrayAndEveryRgbChannelOrderPreset()
    {
        IRawCodec codec = new RawCodecService();
        RawDescriptor gray = codec.CreateGrayDescriptor(3, 2, 12, 16, RawByteOrder.BigEndian);
        RawDescriptor rgb = codec.CreateRgbDescriptor(3, 2, 8, 8, RawByteOrder.LittleEndian);
        RawDescriptor bgr = codec.CreateBgrDescriptor(3, 2, 8, 8, RawByteOrder.LittleEndian);
        RawDescriptor rgba = codec.CreateRgbaDescriptor(3, 2, 8, 8, RawByteOrder.LittleEndian);
        RawDescriptor bgra = codec.CreateBgraDescriptor(3, 2, 8, 8, RawByteOrder.LittleEndian);

        Assert.Multiple(() =>
        {
            Assert.That(gray.Channels.Select(channel => channel.Kind), Is.EqualTo(new RawChannelKind[] { RawChannelKind.Gray }));
            Assert.That(gray.RowStrideBytes, Is.EqualTo(6));
            Assert.That(gray.DataLength, Is.EqualTo(12));
            Assert.That(gray.ByteOrder, Is.EqualTo(RawByteOrder.BigEndian));
            Assert.That(rgb.Channels.Select(channel => channel.Name), Is.EqualTo(new string[] { "R", "G", "B" }));
            Assert.That(bgr.Channels.Select(channel => channel.Name), Is.EqualTo(new string[] { "B", "G", "R" }));
            Assert.That(rgba.Channels.Select(channel => channel.Name), Is.EqualTo(new string[] { "R", "G", "B", "A" }));
            Assert.That(bgra.Channels.Select(channel => channel.Name), Is.EqualTo(new string[] { "B", "G", "R", "A" }));
            Assert.That(codec.ValidateDescriptor(gray, new RawLimits()).IsValid(), Is.True);
            Assert.That(codec.ValidateDescriptor(rgb, new RawLimits()).IsValid(), Is.True);
            Assert.That(codec.ValidateDescriptor(bgr, new RawLimits()).IsValid(), Is.True);
            Assert.That(codec.ValidateDescriptor(rgba, new RawLimits()).IsValid(), Is.True);
            Assert.That(codec.ValidateDescriptor(bgra, new RawLimits()).IsValid(), Is.True);
        });
    }

    [TestCase(RawCfaLayout.Rggb, RawCfaColor.Red, RawCfaColor.Green, RawCfaColor.Green, RawCfaColor.Blue)]
    [TestCase(RawCfaLayout.Bggr, RawCfaColor.Blue, RawCfaColor.Green, RawCfaColor.Green, RawCfaColor.Red)]
    [TestCase(RawCfaLayout.Grbg, RawCfaColor.Green, RawCfaColor.Red, RawCfaColor.Blue, RawCfaColor.Green)]
    [TestCase(RawCfaLayout.Gbrg, RawCfaColor.Green, RawCfaColor.Blue, RawCfaColor.Red, RawCfaColor.Green)]
    public void PublicServiceCreatesAndQueriesEveryStandardBayerPhase(RawCfaLayout layout, RawCfaColor first, RawCfaColor second, RawCfaColor third, RawCfaColor fourth)
    {
        IRawCodec codec = new RawCodecService();
        RawCfaPattern pattern = codec.CreateBayerCfaPattern(layout);
        RawDescriptor descriptor = codec.CreateBayerDescriptor(7, 5, 12, 16, RawByteOrder.LittleEndian, layout);

        Assert.Multiple(() =>
        {
            Assert.That(pattern.Colors, Is.EqualTo(new RawCfaColor[] { first, second, third, fourth }));
            Assert.That(codec.GetCfaColor(pattern, 0, 0), Is.EqualTo(first));
            Assert.That(codec.GetCfaColor(pattern, 3, 2), Is.EqualTo(second));
            Assert.That(codec.GetCfaColor(pattern, 2, 3), Is.EqualTo(third));
            Assert.That(codec.GetCfaColor(pattern, 3, 3), Is.EqualTo(fourth));
            Assert.That(codec.ValidateCfaPattern(pattern).IsValid(), Is.True);
            Assert.That((descriptor.Cfa ?? throw new InvalidOperationException()).Layout, Is.EqualTo(layout));
            Assert.That(codec.ValidateDescriptor(descriptor, new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void PublicServiceCreatesAndValidatesArbitraryCfaPatternsAndDescriptors()
    {
        IRawCodec codec = new RawCodecService();
        RawCfaColor[] colors = new RawCfaColor[]
        {
            RawCfaColor.Cyan, RawCfaColor.Magenta, RawCfaColor.Yellow,
            RawCfaColor.Green2, RawCfaColor.White, RawCfaColor.Red
        };
        RawCfaPattern pattern = codec.CreateArbitraryCfaPattern(3, 2, colors);
        RawDescriptor descriptor = codec.CreateArbitraryCfaDescriptor(9, 4, 10, 16, RawByteOrder.BigEndian, 3, 2, colors);
        RawCfaPattern invalid = new RawCfaPattern(RawCfaLayout.Arbitrary, 2, 2, new RawCfaColor[] { RawCfaColor.Red });

        Assert.Multiple(() =>
        {
            Assert.That(pattern.Layout, Is.EqualTo(RawCfaLayout.Arbitrary));
            Assert.That(codec.GetCfaColor(pattern, 4, 3), Is.EqualTo(RawCfaColor.White));
            Assert.That(codec.GetCfaColor(pattern, 8, 5), Is.EqualTo(RawCfaColor.Red));
            Assert.That(codec.ValidateCfaPattern(pattern).IsValid(), Is.True);
            Assert.That(codec.ValidateCfaPattern(invalid).IsValid(), Is.False);
            Assert.That((descriptor.Cfa ?? throw new InvalidOperationException()).Colors, Is.EqualTo(colors));
            Assert.That(codec.ValidateDescriptor(descriptor, new RawLimits()).IsValid(), Is.True);
            Assert.Throws<RawFormatException>(() => codec.GetCfaColor(invalid, 0, 0));
            Assert.Throws<RawFormatException>(() => codec.CreateBayerCfaPattern(RawCfaLayout.Arbitrary));
        });
    }

    [TestCase(10, RawPacking.Raw10, 7, 21)]
    [TestCase(12, RawPacking.Raw12, 8, 24)]
    [TestCase(14, RawPacking.Raw14, 9, 27)]
    public void PublicNamedPackingPresetsComputeOddWidthGeometry(int bitDepth, RawPacking packing, long rowBytes, long dataLength)
    {
        IRawCodec codec = new RawCodecService();
        RawDescriptor descriptor;
        if (packing == RawPacking.Raw10)
        {
            descriptor = codec.CreateRaw10Descriptor(5, 3, RawCfaLayout.Rggb);
        }
        else if (packing == RawPacking.Raw12)
        {
            descriptor = codec.CreateRaw12Descriptor(5, 3, RawCfaLayout.Rggb);
        }
        else
        {
            descriptor = codec.CreateRaw14Descriptor(5, 3, RawCfaLayout.Rggb);
        }

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Packing, Is.EqualTo(packing));
            Assert.That(descriptor.BitDepth, Is.EqualTo(bitDepth));
            Assert.That(descriptor.StoredWordBits, Is.EqualTo(bitDepth));
            Assert.That(codec.GetDescriptorMinimumRowBytes(descriptor, new RawLimits()), Is.EqualTo(rowBytes));
            Assert.That(codec.GetDescriptorRequiredDataLength(descriptor, new RawLimits()), Is.EqualTo(dataLength));
            Assert.That(codec.GetDescriptorTrailingByteCount(descriptor, new RawLimits()), Is.Zero);
            Assert.That(codec.ValidateDescriptorSourceLength(descriptor, dataLength, new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void PublicDescriptorGeometryHonorsPlanarStridesTrailingBytesSourceLengthAndLimits()
    {
        IRawCodec codec = new RawCodecService();
        RawDescriptor descriptor = codec.CreateRgbDescriptor(3, 2, 8, 8, RawByteOrder.LittleEndian);
        descriptor.Layout = RawSampleLayout.Planar;
        descriptor.RowStrideBytes = 4;
        descriptor.PlaneStrideBytes = 10;
        descriptor.DataOffset = 5;
        descriptor.DataLength = 35;
        descriptor.AllowTrailingBytes = true;

        RawValidationReport exact = codec.ValidateDescriptorSourceLength(descriptor, 40, new RawLimits());
        RawValidationReport wrongLength = codec.ValidateDescriptorSourceLength(descriptor, 41, new RawLimits());
        RawLimits limited = CreateFileLimits(34);
        RawValidationReport limitedReport = codec.ValidateDescriptor(descriptor, limited);

        Assert.Multiple(() =>
        {
            Assert.That(exact.IsValid(), Is.True);
            Assert.That(wrongLength.IsValid(), Is.False);
            Assert.That(codec.GetDescriptorMinimumRowBytes(descriptor, new RawLimits()), Is.EqualTo(3));
            Assert.That(codec.GetDescriptorRequiredDataLength(descriptor, new RawLimits()), Is.EqualTo(28));
            Assert.That(codec.GetDescriptorTrailingByteCount(descriptor, new RawLimits()), Is.EqualTo(7));
            Assert.That((limitedReport.Messages ?? throw new InvalidOperationException()).Any(message => message.Code == RawDiagnosticCode.ResourceLimitExceeded), Is.True);
            Assert.Throws<RawLimitException>(() => codec.GetDescriptorRequiredDataLength(descriptor, limited));
        });
    }

    [Test]
    public void PublicRationalOperationsPreserveValidateCompareAndConvertExactValues()
    {
        IRawCodec codec = new RawCodecService();
        RawRational unsignedValue = codec.CreateUnsignedRational(2U, 4U);
        RawRational signedValue = codec.CreateSignedRational(1, 2);
        RawRational different = codec.CreateUnsignedRational(3U, 4U);
        RawRational malformed = codec.CreateSignedRational(-7, 0);

        Assert.Multiple(() =>
        {
            Assert.That(unsignedValue.Signed, Is.False);
            Assert.That(signedValue.Signed, Is.True);
            Assert.That(signedValue.Numerator, Is.EqualTo(1));
            Assert.That(codec.ValidateRational(unsignedValue).IsValid(), Is.True);
            Assert.That(codec.ValidateRational(signedValue).IsValid(), Is.True);
            Assert.That(codec.RationalsEqualExact(unsignedValue, codec.CreateUnsignedRational(2U, 4U)), Is.True);
            Assert.That(codec.RationalsEqualExact(unsignedValue, signedValue), Is.False);
            Assert.That(codec.RationalsEqualValue(unsignedValue, signedValue), Is.True);
            Assert.That(codec.RationalsEqualValue(unsignedValue, different), Is.False);
            Assert.That(codec.ConvertRationalToDouble(signedValue), Is.EqualTo(0.5));
            Assert.That(codec.ConvertRationalToSingle(unsignedValue), Is.EqualTo(0.5F));
            Assert.That((codec.ValidateRational(malformed).Messages ?? throw new InvalidOperationException()).Single().Code, Is.EqualTo(RawDiagnosticCode.InvalidRationalDenominator));
            Assert.Throws<RawFormatException>(() => codec.ConvertRationalToDouble(malformed));
        });
    }

    private static RawLimits CreateFileLimits(long maximum)
    {
        RawLimits limits = new RawLimits();
        limits.MaxFileSizeBytes = maximum;
        limits.MaxTagValueBytes = maximum;
        limits.MaxMetadataBytes = maximum;
        limits.MaxXmpBytes = maximum;
        limits.MaxPreviewBytes = maximum;
        return limits;
    }
}
