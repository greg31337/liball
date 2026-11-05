using icc;

namespace icc.Tests;

public class SpectralRangeTagCodecTests
{
    [Test]
    public void DeviceSpectralRangeRoundTripsAndClonesWithoutAliases()
    {
        IccSpectralRanges source = CreateRanges(400.0, 700.0, 31, 0.0, 0.0, 0);
        byte[] payload = SpectralRangeTagCodec.SerializeSpectralRanges(source);
        TagDecodeState state = DecodeDeviceRange(payload, IccColorSpaceSignatures.ReflectanceSpectralPrefix | 31u);
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.SpectralRange));
            Assert.That(state.Data.SpectralRanges, Is.Not.Null);
            Assert.That(state.Data.SpectralRanges!.SpectralRange.StartWavelength, Is.EqualTo(400.0));
            Assert.That(state.Data.SpectralRanges.SpectralRange.EndWavelength, Is.EqualTo(700.0));
            Assert.That(state.Data.SpectralRanges.SpectralRange.Steps, Is.EqualTo(31));
            Assert.That(state.Data.SpectralRanges.BiSpectralRange.Steps, Is.Zero);
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
            Assert.That(NestedTagDataSerializer.Serialize(state.Data, 0, new HierarchicalWriteContext(4)), Is.EqualTo(payload));
            Assert.That(clone.SpectralRanges, Is.Not.SameAs(state.Data.SpectralRanges));
            Assert.That(clone.SpectralRanges!.SpectralRange, Is.Not.SameAs(state.Data.SpectralRanges.SpectralRange));
        });
    }

    [Test]
    public void BiSpectralDeviceRangeUsesTheProductAsItsChannelCount()
    {
        IccSpectralRanges source = CreateRanges(400.0, 700.0, 31, 420.0, 680.0, 27);
        byte[] payload = SpectralRangeTagCodec.SerializeSpectralRanges(source);
        uint signature = IccColorSpaceSignatures.BiSpectralPrefix | (31u * 27u);

        TagDecodeState valid = DecodeDeviceRange(payload, signature);
        TagDecodeState wrongCount = DecodeDeviceRange(payload, IccColorSpaceSignatures.BiSpectralPrefix | 32u);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(valid.Data.SpectralRanges!.BiSpectralRange.StartWavelength, Is.EqualTo(420.0));
            Assert.That(valid.Data.SpectralRanges.BiSpectralRange.Steps, Is.EqualTo(27));
            Assert.That(wrongCount.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(wrongCount.Result.Message, Does.Contain("837 total channels").And.Contain("32 channels"));
        });
    }

    [Test]
    public void RejectsMalformedRangesAndIncompatibleDeviceContexts()
    {
        byte[] valid = SpectralRangeTagCodec.SerializeSpectralRanges(CreateRanges(400.0, 700.0, 31, 0.0, 0.0, 0));
        byte[] reserved = valid.ToArray();
        reserved[4] = 1;
        byte[] biRange = SpectralRangeTagCodec.SerializeSpectralRanges(CreateRanges(400.0, 700.0, 31, 420.0, 680.0, 27));

        TagDecodeState badReserved = DecodeDeviceRange(reserved, IccColorSpaceSignatures.ReflectanceSpectralPrefix | 31u);
        TagDecodeState badCount = DecodeDeviceRange(valid, IccColorSpaceSignatures.ReflectanceSpectralPrefix | 30u);
        TagDecodeState badSecondary = DecodeDeviceRange(biRange, IccColorSpaceSignatures.ReflectanceSpectralPrefix | 31u);
        TagDecodeState nonSpectral = DecodeDeviceRange(valid, IccColorSpaceSignatures.Rgb);
        byte[] wrongTypePayload = new byte[12];
        WriteUInt32(wrongTypePayload, 0, IccTypeSignatures.Float32Array);
        TagDecodeState wrongType = DecodeDeviceRange(wrongTypePayload, IccColorSpaceSignatures.ReflectanceSpectralPrefix | 31u);

        Assert.Multiple(() =>
        {
            Assert.That(badReserved.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(badCount.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(badSecondary.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(nonSpectral.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(wrongType.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(wrongType.Result.Message, Does.Contain("'dsrn'").And.Contain("'fl32'"));
            Assert.Throws<IccProfileWriteException>(() => SpectralRangeTagCodec.SerializeSpectralRanges(CreateRanges(700.0, 400.0, 31, 0.0, 0.0, 0)));
        });
    }

    [Test]
    public void ExtendedDeviceSignaturesRetainOriginalAndAmendedRules()
    {
        uint extended = IccColorSpaceSignatures.ExtendedDevicePrefix | 300u;
        uint reflectance = IccColorSpaceSignatures.ReflectanceSpectralPrefix | 31u;
        uint biSpectral = IccColorSpaceSignatures.BiSpectralPrefix | 837u;

        Assert.Multiple(() =>
        {
            Assert.That(HeaderCodec.DecodeDataColorSpace(extended, IccVersionKind.Version5), Is.EqualTo(IccColorSpaceKind.ExtendedDevice));
            Assert.That(HeaderCodec.DecodeDataColorSpace(reflectance, IccVersionKind.Version5), Is.EqualTo(IccColorSpaceKind.Spectral));
            Assert.That(HeaderCodec.DecodeDataColorSpace(biSpectral, IccVersionKind.Version5), Is.EqualTo(IccColorSpaceKind.BiSpectral));
            Assert.That(HeaderCodec.DecodeDataColorSpace(reflectance, IccVersionKind.Version4), Is.EqualTo(IccColorSpaceKind.Unknown));
            Assert.That(HeaderCodec.DecodeColorSpace(reflectance), Is.EqualTo(IccColorSpaceKind.Unknown));
            Assert.That(ColorSpaceChannelCount.Decode(extended), Is.EqualTo(300));
            Assert.That(ColorSpaceChannelCount.Decode(reflectance), Is.EqualTo(31));
            Assert.That(ColorSpaceChannelCount.Decode(biSpectral), Is.EqualTo(837));
        });
    }

    private static IccSpectralRanges CreateRanges(double start, double end, int steps, double biStart, double biEnd, int biSteps)
    {
        IccSpectralRanges ranges = new IccSpectralRanges();
        ranges.SpectralRange.StartWavelength = start;
        ranges.SpectralRange.EndWavelength = end;
        ranges.SpectralRange.Steps = steps;
        ranges.BiSpectralRange.StartWavelength = biStart;
        ranges.BiSpectralRange.EndWavelength = biEnd;
        ranges.BiSpectralRange.Steps = biSteps;
        return ranges;
    }

    private static TagDecodeState DecodeDeviceRange(byte[] payload, uint dataColorSpaceSignature)
    {
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 200, IccVersionKind.Version5, true);
        state.HasProfileContext = true;
        state.TagSignature = IccTagSignatures.DeviceSpectralRange;
        state.DataColorSpaceSignature = dataColorSpaceSignature;
        state.DataColorSpace = HeaderCodec.DecodeDataColorSpace(dataColorSpaceSignature, IccVersionKind.Version5);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
