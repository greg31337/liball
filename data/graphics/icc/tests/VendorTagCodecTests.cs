using icc;

namespace icc.Tests;

public class VendorTagCodecTests
{
    [Test]
    public void DecodesAndLosslesslyReserializesVcgtTablesOutsideCoreTypeDispatch()
    {
        byte[] payload = CreateVcgtTablePayload();
        TagDecodeState state = Decode(payload, IccTagSignatures.VideoCardGammaTable, IccVersionKind.Version5, true);
        TagDecodeState contextFreeState = DecodeWithoutContext(payload, IccVersionKind.Version4);
        byte[] roundTrip = VendorTagCodec.SerializeVideoCardGammaRaw(state.Data.RawData);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
            Assert.That(state.Data.VendorData, Is.Not.Null);
            Assert.That(state.Data.VendorData!.VendorName, Is.EqualTo("Apple"));
            Assert.That(state.Data.VendorData.FormatName, Is.EqualTo("Video Card Gamma Table"));
            Assert.That(state.Data.VendorData.VideoCardGamma, Is.Not.Null);
            Assert.That(state.Data.VendorData.VideoCardGamma!.IsFormula, Is.False);
            Assert.That(state.Data.VendorData.VideoCardGamma.ChannelCount, Is.EqualTo(3));
            Assert.That(state.Data.VendorData.VideoCardGamma.EntryCount, Is.EqualTo(2));
            Assert.That(state.Data.VendorData.VideoCardGamma.EntrySize, Is.EqualTo(2));
            Assert.That(state.Data.VendorData.VideoCardGamma.TableData, Is.EqualTo(payload[18..]));
            Assert.That(roundTrip, Is.EqualTo(payload).And.Not.SameAs(state.Data.RawData.Bytes));
            Assert.That(contextFreeState.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(contextFreeState.Data.VendorData!.TagSignature, Is.EqualTo(0));
            Assert.That(TagCodecDispatcher.IsTypeSupportedByGeneration(IccTypeSignatures.VideoCardGammaTable, IccVersionKind.Version2), Is.False);
            Assert.That(TagCodecDispatcher.IsTypeSupportedByGeneration(IccTypeSignatures.VideoCardGammaTable, IccVersionKind.Version4), Is.False);
            Assert.That(TagCodecDispatcher.IsTypeSupportedByGeneration(IccTypeSignatures.VideoCardGammaTable, IccVersionKind.Version5), Is.False);
            Assert.That(TagTableCodec.UsesContextualColorantSemantics(IccTagSignatures.VideoCardGammaTable), Is.True);
        });
    }

    [Test]
    public void DecodesVcgtFormulaParametersAndRejectsMalformedRepresentations()
    {
        byte[] payload = CreateVcgtFormulaPayload();
        TagDecodeState state = Decode(payload, IccTagSignatures.VideoCardGammaTable, IccVersionKind.Version2, false);
        byte[] badReserved = payload.ToArray();
        badReserved[4] = 1;
        byte[] badFormula = payload.ToArray();
        WriteFixed(badFormula, 12, 0.0);
        byte[] badDimensions = CreateVcgtTablePayload();
        WriteUInt16(badDimensions, 12, 2);
        byte[] wrongType = payload.ToArray();
        WriteUInt32(wrongType, 0, IccTypeSignatures.Curve);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
            Assert.That(state.Data.VendorData!.VideoCardGamma!.IsFormula, Is.True);
            Assert.That(state.Data.VendorData.VideoCardGamma.RedFormula, Is.EqualTo(new[] { 2.2, 0.0, 1.0 }).Within(0.00002));
            Assert.That(state.Data.VendorData.VideoCardGamma.GreenFormula, Is.EqualTo(new[] { 2.0, 0.1, 0.9 }).Within(0.00002));
            Assert.That(state.Data.VendorData.VideoCardGamma.BlueFormula, Is.EqualTo(new[] { 1.8, 0.2, 0.8 }).Within(0.00002));
            AssertInvalid(Decode(badReserved, IccTagSignatures.VideoCardGammaTable, IccVersionKind.Version4, false), "reserved bytes");
            AssertInvalid(Decode(badFormula, IccTagSignatures.VideoCardGammaTable, IccVersionKind.Version4, false), "positive gamma");
            AssertInvalid(Decode(badDimensions, IccTagSignatures.VideoCardGammaTable, IccVersionKind.Version4, false), "one or three channels");
            AssertInvalid(Decode(wrongType, IccTagSignatures.VideoCardGammaTable, IccVersionKind.Version4, false), "requires a vcgt");
            Assert.Throws<IccProfileWriteException>(() => VendorTagCodec.SerializeVideoCardGammaRaw(new IccRawData { Bytes = badFormula }));
        });
    }

    [Test]
    public void DecodesMhc2MatrixLutsAndLuminanceAndPreservesOriginalLayout()
    {
        byte[] payload = CreateMhc2Payload();
        TagDecodeState state = Decode(payload, IccTagSignatures.MicrosoftHdrCalibration, IccVersionKind.Version4, true);
        IccMicrosoftHardwareCalibration calibration = state.Data.VendorData!.MicrosoftHardwareCalibration!;
        byte[] roundTrip = VendorTagCodec.SerializeMicrosoftHardwareCalibrationRaw(state.Data.RawData);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(state.Data.VendorData!.VendorName, Is.EqualTo("Microsoft"));
            Assert.That(state.Data.VendorData.FormatName, Is.EqualTo("Microsoft Hardware Calibration 2"));
            Assert.That(calibration.LutEntryCount, Is.EqualTo(3));
            Assert.That(calibration.MinimumLuminance, Is.EqualTo(0.05).Within(0.00002));
            Assert.That(calibration.PeakLuminance, Is.EqualTo(1000.0));
            Assert.That(calibration.Matrix, Is.EqualTo(new[] { 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0 }));
            Assert.That(calibration.RedLut, Is.EqualTo(new[] { 0.0, 0.5, 1.0 }));
            Assert.That(calibration.GreenLut, Is.EqualTo(calibration.RedLut));
            Assert.That(calibration.BlueLut, Is.EqualTo(calibration.RedLut));
            Assert.That(roundTrip, Is.EqualTo(payload).And.Not.SameAs(state.Data.RawData.Bytes));
            Assert.That(TagCodecDispatcher.IsTypeSupportedByGeneration(IccTypeSignatures.MicrosoftHardwareCalibration, IccVersionKind.Version4), Is.False);
        });
    }

    [Test]
    public void RecognizesIdentityMhc2AndRejectsInvalidOffsetsHeadersAndSamples()
    {
        byte[] identity = CreateIdentityMhc2Payload();
        TagDecodeState identityState = Decode(identity, IccTagSignatures.MicrosoftHdrCalibration, IccVersionKind.Version2, false);
        byte[] excessiveCount = CreateMhc2Payload();
        WriteUInt32(excessiveCount, 8, 4097);
        byte[] badOffset = CreateMhc2Payload();
        WriteUInt32(badOffset, 24, 38);
        byte[] badLutHeader = CreateMhc2Payload();
        WriteUInt32(badLutHeader, 84, IccTypeSignatures.Curve);
        byte[] badSample = CreateMhc2Payload();
        WriteFixed(badSample, 92, -0.1);
        byte[] overlappingMatrix = CreateMhc2Payload();
        WriteUInt32(overlappingMatrix, 20, 84);

        Assert.Multiple(() =>
        {
            Assert.That(identityState.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(identityState.Data.RawData.Bytes, Is.EqualTo(identity));
            Assert.That(identityState.Data.VendorData!.MicrosoftHardwareCalibration!.Matrix, Is.Empty);
            Assert.That(identityState.Data.VendorData.MicrosoftHardwareCalibration.RedLut, Is.Empty);
            AssertInvalid(Decode(excessiveCount, IccTagSignatures.MicrosoftHdrCalibration, IccVersionKind.Version4, false), "exceeds 4096");
            AssertInvalid(Decode(badOffset, IccTagSignatures.MicrosoftHdrCalibration, IccVersionKind.Version4, false), "offset is unaligned");
            AssertInvalid(Decode(badLutHeader, IccTagSignatures.MicrosoftHdrCalibration, IccVersionKind.Version4, false), "sf32 signature");
            AssertInvalid(Decode(badSample, IccTagSignatures.MicrosoftHdrCalibration, IccVersionKind.Version4, false), "inclusive range");
            AssertInvalid(Decode(overlappingMatrix, IccTagSignatures.MicrosoftHdrCalibration, IccVersionKind.Version4, false), "overlaps");
            Assert.Throws<IccProfileWriteException>(() => VendorTagCodec.SerializeMicrosoftHardwareCalibrationRaw(new IccRawData { Bytes = badLutHeader }));
        });
    }

    [Test]
    public void RecognizesRegisteredMicrosoftPrivateRangeWithoutClaimingItsPayloadTypeIsCore()
    {
        byte[] payload = new byte[] { 0x70, 0x72, 0x69, 0x76, 9, 8, 7, 6, 5, 4, 3 };
        uint ms42 = 0x4d533432;
        TagDecodeState state = Decode(payload, ms42, IccVersionKind.Version5, false);
        TagDecodeState outsideRange = Decode(payload, 0x4d535430, IccVersionKind.Version5, true);
        byte[] roundTrip = VendorTagCodec.SerializeRegisteredVendorRaw(state.Data.RawData, ms42);
        IIccSignatureDescriber describer = new IccSignatureDescriber();

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.VendorData!.VendorName, Is.EqualTo("Microsoft"));
            Assert.That(state.Data.VendorData.FormatName, Is.EqualTo("Registered Microsoft Private Payload"));
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
            Assert.That(roundTrip, Is.EqualTo(payload).And.Not.SameAs(state.Data.RawData.Bytes));
            Assert.That(describer.DescribeTagSignature(ms42), Does.StartWith("Registered Microsoft Private Tag"));
            Assert.That(outsideRange.Result.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(outsideRange.Data.VendorData, Is.Null);
            Assert.That(TagCodecDispatcher.IsTypeSupportedByGeneration(0x70726976, IccVersionKind.Version5), Is.False);
            Assert.Throws<IccProfileWriteException>(() => VendorTagCodec.SerializeRegisteredVendorRaw(state.Data.RawData, 0x4d535430));
        });
    }

    [Test]
    public void CloneOwnsRecognizedVendorMetadataAndItsMutableArrays()
    {
        TagDecodeState state = Decode(CreateMhc2Payload(), IccTagSignatures.MicrosoftHdrCalibration, IccVersionKind.Version4, true);
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);
        clone.VendorData!.MicrosoftHardwareCalibration!.Matrix[0] = 9.0;
        clone.VendorData.VendorName = "Changed";

        Assert.Multiple(() =>
        {
            Assert.That(state.Data.VendorData!.MicrosoftHardwareCalibration!.Matrix[0], Is.EqualTo(1.0));
            Assert.That(state.Data.VendorData.VendorName, Is.EqualTo("Microsoft"));
            Assert.That(clone.VendorData, Is.Not.SameAs(state.Data.VendorData));
            Assert.That(clone.VendorData!.MicrosoftHardwareCalibration, Is.Not.SameAs(state.Data.VendorData.MicrosoftHardwareCalibration));
            Assert.That(clone.VendorData.MicrosoftHardwareCalibration!.Matrix, Is.Not.SameAs(state.Data.VendorData.MicrosoftHardwareCalibration!.Matrix));
        });
    }

    private static TagDecodeState Decode(byte[] payload, uint tagSignature, IccVersionKind version, bool preserveOriginalBytes)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 500, version, preserveOriginalBytes);
        state.HasProfileContext = true;
        state.TagSignature = tagSignature;
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static TagDecodeState DecodeWithoutContext(byte[] payload, IccVersionKind version)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 500, version, false);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static byte[] CreateVcgtTablePayload()
    {
        byte[] payload = new byte[30];
        WriteUInt32(payload, 0, IccTypeSignatures.VideoCardGammaTable);
        WriteUInt32(payload, 8, 0);
        WriteUInt16(payload, 12, 3);
        WriteUInt16(payload, 14, 2);
        WriteUInt16(payload, 16, 2);
        byte[] values = new byte[] { 0, 0, 0xff, 0xff, 0, 1, 0xff, 0xfe, 0, 2, 0xff, 0xfd };
        Array.Copy(values, 0, payload, 18, values.Length);
        return payload;
    }

    private static byte[] CreateVcgtFormulaPayload()
    {
        byte[] payload = new byte[48];
        WriteUInt32(payload, 0, IccTypeSignatures.VideoCardGammaTable);
        WriteUInt32(payload, 8, 1);
        double[] values = new[] { 2.2, 0.0, 1.0, 2.0, 0.1, 0.9, 1.8, 0.2, 0.8 };
        for (int index = 0; index < values.Length; index++)
        {
            WriteFixed(payload, 12 + (index * 4), values[index]);
        }

        return payload;
    }

    private static byte[] CreateMhc2Payload()
    {
        byte[] payload = new byte[144];
        WriteUInt32(payload, 0, IccTypeSignatures.MicrosoftHardwareCalibration);
        WriteUInt32(payload, 8, 3);
        WriteFixed(payload, 12, 0.05);
        WriteFixed(payload, 16, 1000.0);
        WriteUInt32(payload, 20, 36);
        WriteUInt32(payload, 24, 84);
        WriteUInt32(payload, 28, 104);
        WriteUInt32(payload, 32, 124);
        double[] matrix = new[] { 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0 };
        for (int index = 0; index < matrix.Length; index++)
        {
            WriteFixed(payload, 36 + (index * 4), matrix[index]);
        }

        WriteMhcLut(payload, 84);
        WriteMhcLut(payload, 104);
        WriteMhcLut(payload, 124);
        return payload;
    }

    private static byte[] CreateIdentityMhc2Payload()
    {
        byte[] payload = new byte[36];
        WriteUInt32(payload, 0, IccTypeSignatures.MicrosoftHardwareCalibration);
        WriteFixed(payload, 12, 0.01);
        WriteFixed(payload, 16, 600.0);
        return payload;
    }

    private static void WriteMhcLut(byte[] payload, int offset)
    {
        WriteUInt32(payload, offset, IccTypeSignatures.S15Fixed16Array);
        WriteFixed(payload, offset + 8, 0.0);
        WriteFixed(payload, offset + 12, 0.5);
        WriteFixed(payload, offset + 16, 1.0);
    }

    private static void WriteFixed(byte[] bytes, int offset, double value)
    {
        BigEndianWriter writer = new BigEndianWriter(new Memory<byte>(bytes, offset, 4), offset);
        IccNumberCodec.WriteS15Fixed16(writer, value);
    }

    private static void WriteUInt16(byte[] bytes, int offset, ushort value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    private static void AssertInvalid(TagDecodeState state, string messagePart)
    {
        Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
        Assert.That(state.Result.Message, Does.Contain(messagePart));
        Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
        Assert.That(state.Data.RawData.Bytes, Is.Not.Empty);
    }
}
