using raw;

namespace raw.Tests;

public class RawDescriptorTests
{
    [Test]
    public void GrayRgbBgrRgbaAndBgraPresetsDefineExactChannelOrderAndGeometry()
    {
        RawDescriptor gray = RawDescriptor.CreateGray(3, 2, 12, 16, RawByteOrder.BigEndian);
        RawDescriptor rgb = RawDescriptor.CreateRgb(3, 2, 8, 8, RawByteOrder.LittleEndian);
        RawDescriptor bgr = RawDescriptor.CreateBgr(3, 2, 8, 8, RawByteOrder.LittleEndian);
        RawDescriptor rgba = RawDescriptor.CreateRgba(3, 2, 8, 8, RawByteOrder.LittleEndian);
        RawDescriptor bgra = RawDescriptor.CreateBgra(3, 2, 8, 8, RawByteOrder.LittleEndian);

        Assert.Multiple(() =>
        {
            Assert.That(gray.Channels.Select(channel => channel.Name), Is.EqualTo(new string[] { "Gray" }));
            Assert.That(gray.Channels.Select(channel => channel.Kind), Is.EqualTo(new RawChannelKind[] { RawChannelKind.Gray }));
            Assert.That(gray.RowStrideBytes, Is.EqualTo(6));
            Assert.That(gray.DataLength, Is.EqualTo(12));
            Assert.That(gray.ByteOrder, Is.EqualTo(RawByteOrder.BigEndian));
            Assert.That(rgb.Channels.Select(channel => channel.Name), Is.EqualTo(new string[] { "R", "G", "B" }));
            Assert.That(bgr.Channels.Select(channel => channel.Name), Is.EqualTo(new string[] { "B", "G", "R" }));
            Assert.That(rgba.Channels.Select(channel => channel.Name), Is.EqualTo(new string[] { "R", "G", "B", "A" }));
            Assert.That(bgra.Channels.Select(channel => channel.Name), Is.EqualTo(new string[] { "B", "G", "R", "A" }));
            Assert.That(rgb.RowStrideBytes, Is.EqualTo(9));
            Assert.That(rgba.RowStrideBytes, Is.EqualTo(12));
            Assert.That(new RawDescriptor[] { gray, rgb, bgr, rgba, bgra }.All(descriptor => descriptor.Validate(new RawLimits()).IsValid()), Is.True);
        });
    }

    [TestCase(RawCfaLayout.Rggb, RawCfaColor.Red, RawCfaColor.Green, RawCfaColor.Green, RawCfaColor.Blue)]
    [TestCase(RawCfaLayout.Bggr, RawCfaColor.Blue, RawCfaColor.Green, RawCfaColor.Green, RawCfaColor.Red)]
    [TestCase(RawCfaLayout.Grbg, RawCfaColor.Green, RawCfaColor.Red, RawCfaColor.Blue, RawCfaColor.Green)]
    [TestCase(RawCfaLayout.Gbrg, RawCfaColor.Green, RawCfaColor.Blue, RawCfaColor.Red, RawCfaColor.Green)]
    public void BayerPresetsRetainCanonicalPhaseAndRepeatAtAllCoordinates(RawCfaLayout layout, RawCfaColor first, RawCfaColor second, RawCfaColor third, RawCfaColor fourth)
    {
        RawDescriptor descriptor = RawDescriptor.CreateBayer(7, 5, 12, 16, RawByteOrder.LittleEndian, layout);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Photometric, Is.EqualTo(RawPhotometricInterpretation.ColorFilterArray));
            Assert.That(descriptor.ChannelCount, Is.EqualTo(1));
            Assert.That((descriptor.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Layout, Is.EqualTo(layout));
            Assert.That(descriptor.Cfa.Colors, Is.EqualTo(new RawCfaColor[] { first, second, third, fourth }));
            Assert.That(descriptor.Cfa.GetColor(0, 0), Is.EqualTo(first));
            Assert.That(descriptor.Cfa.GetColor(3, 2), Is.EqualTo(second));
            Assert.That(descriptor.Cfa.GetColor(2, 3), Is.EqualTo(third));
            Assert.That(descriptor.Cfa.GetColor(3, 3), Is.EqualTo(fourth));
            Assert.That(descriptor.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void ArbitraryCfaSupportsNonBayerColorsAndGridSizes()
    {
        RawCfaColor[] colors = new RawCfaColor[]
        {
            RawCfaColor.Cyan, RawCfaColor.Magenta, RawCfaColor.Yellow,
            RawCfaColor.Green2, RawCfaColor.White, RawCfaColor.Red
        };
        RawDescriptor descriptor = RawDescriptor.CreateArbitraryCfa(9, 4, 10, 16, RawByteOrder.BigEndian, 3, 2, colors);

        Assert.Multiple(() =>
        {
            Assert.That((descriptor.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Layout, Is.EqualTo(RawCfaLayout.Arbitrary));
            Assert.That(descriptor.Cfa.RepeatWidth, Is.EqualTo(3));
            Assert.That(descriptor.Cfa.RepeatHeight, Is.EqualTo(2));
            Assert.That(descriptor.Cfa.GetColor(4, 3), Is.EqualTo(RawCfaColor.White));
            Assert.That(descriptor.Cfa.GetColor(8, 5), Is.EqualTo(RawCfaColor.Red));
            Assert.That(descriptor.Cfa.Validate().IsValid(), Is.True);
            Assert.That(descriptor.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [TestCase(RawPacking.Raw10, 10, 7, 21)]
    [TestCase(RawPacking.Raw12, 12, 8, 24)]
    [TestCase(RawPacking.Raw14, 14, 9, 27)]
    public void NamedRawPackingPresetsUseOddWidthGroupGeometry(RawPacking packing, int bitDepth, long rowBytes, long dataLength)
    {
        RawDescriptor descriptor = RawDescriptor.CreatePackedBayer(5, 3, packing, RawCfaLayout.Rggb);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Packing, Is.EqualTo(packing));
            Assert.That(descriptor.BitDepth, Is.EqualTo(bitDepth));
            Assert.That(descriptor.StoredWordBits, Is.EqualTo(bitDepth));
            Assert.That(descriptor.GetMinimumRowBytes(), Is.EqualTo(rowBytes));
            Assert.That(descriptor.RowStrideBytes, Is.EqualTo(rowBytes));
            Assert.That(descriptor.GetRequiredDataLength(), Is.EqualTo(dataLength));
            Assert.That(descriptor.DataLength, Is.EqualTo(dataLength));
            Assert.That(descriptor.Validate(new RawLimits()).Messages, Is.Empty);
        });
    }

    [Test]
    public void PlanarStridesAndExplicitTrailingBytesProduceCheckedPayloadLengths()
    {
        RawDescriptor descriptor = RawDescriptor.CreateRgb(3, 2, 8, 8, RawByteOrder.LittleEndian);
        descriptor.Layout = RawSampleLayout.Planar;
        descriptor.RowStrideBytes = 4;
        descriptor.PlaneStrideBytes = 10;
        descriptor.DataOffset = 5;
        descriptor.DataLength = 28;

        RawValidationReport exact = descriptor.ValidateSourceLength(33, new RawLimits());
        descriptor.DataLength = 35;
        RawValidationReport rejectedTrailing = descriptor.ValidateSourceLength(40, new RawLimits());
        descriptor.AllowTrailingBytes = true;
        RawValidationReport allowedTrailing = descriptor.ValidateSourceLength(40, new RawLimits());
        RawValidationReport wrongSource = descriptor.ValidateSourceLength(41, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.GetMinimumRowBytes(), Is.EqualTo(3));
            Assert.That(descriptor.GetRequiredDataLength(), Is.EqualTo(28));
            Assert.That(descriptor.GetTrailingByteCount(), Is.EqualTo(7));
            Assert.That(exact.IsValid(), Is.True);
            Assert.That(((rejectedTrailing.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "Descriptor.DataLength")), Is.True);
            Assert.That(allowedTrailing.IsValid(), Is.True);
            Assert.That(((wrongSource.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "Descriptor.SourceLength")), Is.True);
        });
    }

    [Test]
    public void GenericPackedSignedAndByteAlignedFloatingDescriptorsValidate()
    {
        RawDescriptor packed = RawDescriptor.CreateGray(5, 2, 12, 16, RawByteOrder.LittleEndian);
        packed.SampleKind = RawSampleKind.SignedInteger;
        packed.Packing = RawPacking.PackedBits;
        packed.StoredWordBits = 12;
        packed.BitOrder = RawBitOrder.LeastSignificantBitFirst;
        packed.RowStrideBytes = 8;
        packed.DataLength = 16;
        RawDescriptor half = RawDescriptor.CreateGray(2, 2, 16, 16, RawByteOrder.BigEndian);
        half.SampleKind = RawSampleKind.Half;
        RawDescriptor single = RawDescriptor.CreateGray(2, 2, 32, 32, RawByteOrder.LittleEndian);
        single.SampleKind = RawSampleKind.Single;

        Assert.Multiple(() =>
        {
            Assert.That(packed.GetMinimumRowBytes(), Is.EqualTo(8));
            Assert.That(packed.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(half.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(single.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void InvalidGeometryChannelsPackingCfaLimitsAndOverflowAreReportedOrRejected()
    {
        RawDescriptor invalid = new RawDescriptor();
        RawValidationReport empty = invalid.Validate(new RawLimits());
        RawDescriptor channels = RawDescriptor.CreateRgb(2, 2, 8, 8, RawByteOrder.LittleEndian);
        channels.Channels[1].Name = "R";
        channels.Cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);
        RawDescriptor packing = RawDescriptor.CreateGray(2, 2, 16, 16, RawByteOrder.LittleEndian);
        packing.SampleKind = RawSampleKind.Single;
        RawLimits limits = new RawLimits();
        limits.MaxWidth = 1;
        RawDescriptor overflow = RawDescriptor.CreateRgb(1, 1, 8, 8, RawByteOrder.LittleEndian);
        overflow.Width = int.MaxValue;
        overflow.Height = int.MaxValue;
        overflow.ChannelCount = 64;
        overflow.Channels = Enumerable.Range(0, 64).Select(index => new RawChannelDescription("C" + index, RawChannelKind.Custom)).ToArray();
        overflow.RowStrideBytes = overflow.GetMinimumRowBytes();
        overflow.DataLength = long.MaxValue;
        RawCfaPattern badPattern = new RawCfaPattern((RawCfaLayout)99, 2, 2, new RawCfaColor[] { RawCfaColor.Red });

        Assert.Multiple(() =>
        {
            Assert.That(((empty.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "Descriptor.Geometry")), Is.True);
            Assert.That(empty.Messages.Any(message => message.Context == "Descriptor.Channels"), Is.True);
            Assert.That(((channels.Validate(new RawLimits()).Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "Descriptor.Channels")), Is.True);
            Assert.That(((channels.Validate(new RawLimits()).Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "Descriptor.Cfa")), Is.True);
            Assert.That(((packing.Validate(new RawLimits()).Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "Descriptor.SampleKind")), Is.True);
            Assert.That(((RawDescriptor.CreateGray(2, 2, 8, 8, RawByteOrder.LittleEndian).Validate(limits).Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Code == RawDiagnosticCode.ResourceLimitExceeded)), Is.True);
            Assert.That(((overflow.Validate(new RawLimits()).Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Code == RawDiagnosticCode.ArithmeticOverflow)), Is.True);
            Assert.That(badPattern.Validate().IsValid(), Is.False);
            Assert.Throws<RawFormatException>(() => overflow.GetRequiredDataLength());
            Assert.Throws<RawFormatException>(() => RawDescriptor.CreateGray(0, 2, 8, 8, RawByteOrder.LittleEndian));
            Assert.Throws<RawFormatException>(() => RawDescriptor.CreatePackedBayer(2, 2, RawPacking.PackedBits, RawCfaLayout.Rggb));
            Assert.Throws<RawFormatException>(() => RawCfaPattern.CreateBayer(RawCfaLayout.Arbitrary));
            Assert.Throws<RawFormatException>(() => badPattern.GetColor(0, 0));
        });
    }
}
