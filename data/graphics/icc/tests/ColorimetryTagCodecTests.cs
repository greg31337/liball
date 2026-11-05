using icc;

namespace icc.Tests;

public class ColorimetryTagCodecTests
{
    [Test]
    public void ChromaticityTypeRoundTripsCustomAndStandardizedCoordinates()
    {
        IccChromaticity custom = new IccChromaticity();
        custom.XCoordinates = new[] { 0.25, 0.5 };
        custom.YCoordinates = new[] { 0.125, 0.75 };
        IccChromaticity bt2020 = new IccChromaticity();
        bt2020.ColorantEncoding = 6;
        bt2020.XCoordinates = new[] { 0.708, 0.170, 0.131 };
        bt2020.YCoordinates = new[] { 0.292, 0.797, 0.046 };

        byte[] customPayload = ChromaticityTagCodec.SerializeChromaticity(custom, IccVersionKind.Version4);
        byte[] bt2020Payload = ChromaticityTagCodec.SerializeChromaticity(bt2020, IccVersionKind.Version4);
        TagDecodeState customState = DecodePayload(customPayload, IccVersionKind.Version4, false);
        TagDecodeState bt2020State = DecodePayload(bt2020Payload, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(customPayload, Has.Length.EqualTo(28));
            Assert.That(customState.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(customState.Data.Kind, Is.EqualTo(IccTagDataKind.Chromaticity));
            Assert.That(customState.Data.Chromaticity!.XCoordinates, Is.EqualTo(custom.XCoordinates));
            Assert.That(customState.Data.Chromaticity.YCoordinates, Is.EqualTo(custom.YCoordinates));
            Assert.That(bt2020Payload, Has.Length.EqualTo(36));
            Assert.That(bt2020State.Data.Chromaticity!.ColorantEncoding, Is.EqualTo(6));
            Assert.That(bt2020State.Data.Chromaticity.XCoordinates[0], Is.EqualTo(ReadU16Fixed16(bt2020Payload, 12)));
        });
    }

    [Test]
    public void ChromaticityTypeEnforcesGenerationSizeAndStandardCoordinates()
    {
        IccChromaticity p3 = new IccChromaticity();
        p3.ColorantEncoding = 5;
        p3.XCoordinates = new[] { 0.680, 0.265, 0.150 };
        p3.YCoordinates = new[] { 0.320, 0.690, 0.060 };
        byte[] payload = ChromaticityTagCodec.SerializeChromaticity(p3, IccVersionKind.Version4);
        byte[] wrongCoordinates = payload.ToArray();
        WriteUInt32(wrongCoordinates, 12, 0);
        byte[] wrongSize = payload.Take(payload.Length - 1).ToArray();
        byte[] reserved = payload.ToArray();
        reserved[4] = 1;

        TagDecodeState coordinateState = DecodePayload(wrongCoordinates, IccVersionKind.Version4, true);
        TagDecodeState sizeState = DecodePayload(wrongSize, IccVersionKind.Version4, false);
        TagDecodeState reservedState = DecodePayload(reserved, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => ChromaticityTagCodec.SerializeChromaticity(p3, IccVersionKind.Version2));
            Assert.Throws<IccProfileWriteException>(() => ChromaticityTagCodec.SerializeChromaticity(p3, IccVersionKind.Version5));
            Assert.That(coordinateState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(coordinateState.Result.Message, Does.Contain("standardized"));
            Assert.That(coordinateState.Data.RawData.Bytes, Is.EqualTo(wrongCoordinates));
            Assert.That(sizeState.Result.Message, Does.Contain("exactly 36 bytes"));
            Assert.That(reservedState.Result.Message, Does.Contain("reserved bytes"));
        });
    }

    [Test]
    public void MeasurementTypeRoundTripsAllFields()
    {
        IccMeasurement measurement = new IccMeasurement();
        measurement.Observer = IccMeasurementObserver.Cie1964TenDegree;
        measurement.Backing.X = 0.5;
        measurement.Backing.Y = 1.0;
        measurement.Backing.Z = -0.25;
        measurement.Geometry = IccMeasurementGeometry.Degree0OrDiffuse;
        measurement.Flare = 0.01;
        measurement.Illuminant = IccStandardIlluminant.D65;

        byte[] payload = MeasurementTagCodec.SerializeMeasurement(measurement);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(36));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Measurement));
            Assert.That(state.Data.Measurement!.Observer, Is.EqualTo(IccMeasurementObserver.Cie1964TenDegree));
            Assert.That(state.Data.Measurement.Backing.X, Is.EqualTo(0.5));
            Assert.That(state.Data.Measurement.Backing.Z, Is.EqualTo(-0.25));
            Assert.That(state.Data.Measurement.Geometry, Is.EqualTo(IccMeasurementGeometry.Degree0OrDiffuse));
            Assert.That(state.Data.Measurement.Flare, Is.EqualTo(ReadU16Fixed16(payload, 28)));
            Assert.That(state.Data.Measurement.Illuminant, Is.EqualTo(IccStandardIlluminant.D65));
        });
    }

    [Test]
    public void ViewingConditionsTypeRoundTripsUnnormalizedXyzValues()
    {
        IccViewingConditions conditions = new IccViewingConditions();
        conditions.Illuminant.X = 91.25;
        conditions.Illuminant.Y = 100.0;
        conditions.Illuminant.Z = 75.0;
        conditions.Surround.X = 18.25;
        conditions.Surround.Y = 20.0;
        conditions.Surround.Z = 15.0;
        conditions.IlluminantKind = IccStandardIlluminant.D50;

        byte[] payload = ViewingConditionsTagCodec.SerializeViewingConditions(conditions);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version2, false);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(36));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.ViewingConditions));
            Assert.That(state.Data.ViewingConditions!.Illuminant.Y, Is.EqualTo(100.0));
            Assert.That(state.Data.ViewingConditions.Surround.Y, Is.EqualTo(20.0));
            Assert.That(state.Data.ViewingConditions.IlluminantKind, Is.EqualTo(IccStandardIlluminant.D50));
        });
    }

    [Test]
    public void MeasurementAndViewingConditionCodecsRejectUndefinedValues()
    {
        IccMeasurement invalidMeasurement = new IccMeasurement();
        invalidMeasurement.Flare = 1.01;
        IccViewingConditions invalidViewing = new IccViewingConditions();
        invalidViewing.IlluminantKind = (IccStandardIlluminant)9;
        byte[] measurementPayload = MeasurementTagCodec.SerializeMeasurement(new IccMeasurement());
        WriteUInt32(measurementPayload, 8, 3);
        byte[] viewingPayload = ViewingConditionsTagCodec.SerializeViewingConditions(new IccViewingConditions());
        WriteUInt32(viewingPayload, 32, 9);
        byte[] shortMeasurement = measurementPayload.Take(35).ToArray();
        byte[] reservedViewing = viewingPayload.ToArray();
        reservedViewing[7] = 1;

        TagDecodeState measurementState = DecodePayload(measurementPayload, IccVersionKind.Version4, false);
        TagDecodeState viewingState = DecodePayload(viewingPayload, IccVersionKind.Version4, false);
        TagDecodeState shortState = DecodePayload(shortMeasurement, IccVersionKind.Version4, false);
        TagDecodeState reservedState = DecodePayload(reservedViewing, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => MeasurementTagCodec.SerializeMeasurement(invalidMeasurement));
            Assert.Throws<IccProfileWriteException>(() => ViewingConditionsTagCodec.SerializeViewingConditions(invalidViewing));
            Assert.That(measurementState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(measurementState.Result.Message, Does.Contain("undefined observer"));
            Assert.That(viewingState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(viewingState.Result.Message, Does.Contain("not defined"));
            Assert.That(shortState.Result.Message, Does.Contain("exactly 36 bytes"));
            Assert.That(reservedState.Result.Message, Does.Contain("reserved bytes"));
        });
    }

    [Test]
    public void CicpTypeRoundTripsAndEnforcesProfileClassAndColourSpaceRules()
    {
        IccCicp rgb = new IccCicp();
        rgb.ColorPrimaries = 9;
        rgb.TransferCharacteristics = 16;
        rgb.MatrixCoefficients = 0;
        rgb.FullRange = true;
        IccCicp yCbCr = new IccCicp();
        yCbCr.ColorPrimaries = 9;
        yCbCr.TransferCharacteristics = 18;
        yCbCr.MatrixCoefficients = 9;

        byte[] rgbPayload = CicpTagCodec.SerializeCicp(rgb, IccProfileClass.Display, IccColorSpaceKind.Rgb);
        byte[] yCbCrPayload = CicpTagCodec.SerializeCicp(yCbCr, IccProfileClass.Input, IccColorSpaceKind.YCbCr);
        TagDecodeState rgbState = DecodePayload(rgbPayload, IccVersionKind.Version4, false);
        TagDecodeState yCbCrState = DecodePayload(yCbCrPayload, IccVersionKind.Version5, false);

        Assert.Multiple(() =>
        {
            Assert.That(rgbPayload, Is.EqualTo(new byte[] { 0x63, 0x69, 0x63, 0x70, 0, 0, 0, 0, 9, 16, 0, 1 }));
            Assert.That(rgbState.Data.Cicp!.FullRange, Is.True);
            Assert.That(yCbCrState.Data.Cicp!.MatrixCoefficients, Is.EqualTo(9));
            Assert.Throws<IccProfileWriteException>(() => CicpTagCodec.SerializeCicp(rgb, IccProfileClass.Output, IccColorSpaceKind.Rgb));
            Assert.Throws<IccProfileWriteException>(() => CicpTagCodec.SerializeCicp(yCbCr, IccProfileClass.Display, IccColorSpaceKind.Rgb));
            Assert.Throws<IccProfileWriteException>(() => CicpTagCodec.SerializeCicp(rgb, IccProfileClass.Display, IccColorSpaceKind.YCbCr));
            Assert.Throws<IccProfileWriteException>(() => CicpTagCodec.SerializeCicp(rgb, IccProfileClass.Display, IccColorSpaceKind.Cmyk));
        });
    }

    [Test]
    public void ProfileDecodeAppliesCicpRestrictionsAndUnspecifiedPrimariesAmendment()
    {
        byte[] amendedProfile = CreateCicpProfile(0x04400000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, 0, 3);
        byte[] lutProfile = CreateCicpProfile(0x04400000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, 0, 2);
        byte[] version5Profile = CreateCicpProfile(0x05000000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, 0, 3);
        byte[] outputProfile = CreateCicpProfile(0x04400000, IccProfileClassSignatures.Output, IccColorSpaceSignatures.Rgb, 0, 3);
        byte[] badRgbMatrix = CreateCicpProfile(0x04400000, IccProfileClassSignatures.Display, IccColorSpaceSignatures.Rgb, 9, 3);

        IccTag amendedTag = ReadFirstTag(amendedProfile);
        IccTag lutTag = ReadFirstTag(lutProfile);
        IccTag version5Tag = ReadFirstTag(version5Profile);
        IccTag outputTag = ReadFirstTag(outputProfile);
        IccTag badMatrixTag = ReadFirstTag(badRgbMatrix);

        Assert.Multiple(() =>
        {
            Assert.That(amendedTag.DecodeResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(amendedTag.Data.Cicp!.UsesProfilePrimaries, Is.True);
            Assert.That(amendedTag.DecodeResult.Message, Does.Contain("matrix columns"));
            Assert.That(lutTag.Data.Cicp!.UsesProfilePrimaries, Is.False);
            Assert.That(version5Tag.Data.Cicp!.UsesProfilePrimaries, Is.False);
            Assert.That(outputTag.DecodeResult.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(outputTag.DecodeResult.Message, Does.Contain("Input or Display"));
            Assert.That(badMatrixTag.DecodeResult.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(badMatrixTag.DecodeResult.Message, Does.Contain("MatrixCoefficients"));
        });
    }

    [Test]
    public void CicpTypeRejectsMalformedFlagSizeAndReservedBytes()
    {
        byte[] badFlag = new byte[] { 0x63, 0x69, 0x63, 0x70, 0, 0, 0, 0, 1, 1, 0, 2 };
        byte[] wrongSize = badFlag.Take(11).ToArray();
        byte[] reserved = badFlag.ToArray();
        reserved[4] = 1;
        reserved[11] = 0;

        TagDecodeState flagState = DecodePayload(badFlag, IccVersionKind.Version4, false);
        TagDecodeState sizeState = DecodePayload(wrongSize, IccVersionKind.Version4, false);
        TagDecodeState reservedState = DecodePayload(reserved, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(flagState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(flagState.Result.Message, Does.Contain("zero or one"));
            Assert.That(sizeState.Result.Message, Does.Contain("exactly 12 bytes"));
            Assert.That(reservedState.Result.Message, Does.Contain("reserved bytes"));
        });
    }

    private static IccTag ReadFirstTag(byte[] profileBytes)
    {
        IccReadOptions options = new IccReadOptions();
        options.PreserveOriginalBytes = false;
        IccProfile profile = ProfileCodec.ReadEnvelope(profileBytes, 0, 0, options);
        TagDecoder.GetDecodedData(profile.Tags[0]);
        return profile.Tags[0];
    }

    private static byte[] CreateCicpProfile(uint version, uint profileClass, uint dataColorSpace, byte matrixCoefficients, int matrixTagCount)
    {
        int tagCount = 1 + matrixTagCount;
        int payloadOffset = 132 + (tagCount * 12);
        int profileSize = payloadOffset + 12 + (matrixTagCount * 20);
        byte[] profile = new byte[profileSize];
        WriteUInt32(profile, 0, (uint)profileSize);
        WriteUInt32(profile, 8, version);
        WriteUInt32(profile, 12, profileClass);
        WriteUInt32(profile, 16, dataColorSpace);
        WriteUInt32(profile, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt32(profile, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(profile, 128, (uint)tagCount);
        WriteEntry(profile, 0, IccTagSignatures.Cicp, payloadOffset, 12);
        WriteUInt32(profile, payloadOffset, IccTypeSignatures.Cicp);
        profile[payloadOffset + 8] = 2;
        profile[payloadOffset + 9] = 16;
        profile[payloadOffset + 10] = matrixCoefficients;
        profile[payloadOffset + 11] = 1;
        uint[] signatures = new[] { IccTagSignatures.RedMatrixColumn, IccTagSignatures.GreenMatrixColumn, IccTagSignatures.BlueMatrixColumn };
        int index = 0;
        while (index < matrixTagCount)
        {
            int offset = payloadOffset + 12 + (index * 20);
            WriteEntry(profile, index + 1, signatures[index], offset, 20);
            WriteUInt32(profile, offset, IccTypeSignatures.Xyz);
            index++;
        }

        return profile;
    }

    private static TagDecodeState DecodePayload(byte[] payload, IccVersionKind version, bool preserveOriginalBytes)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, version, preserveOriginalBytes);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static double ReadU16Fixed16(byte[] source, int offset)
    {
        return ReadUInt32(source, offset) / 65536.0;
    }

    private static uint ReadUInt32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24)
            | ((uint)source[offset + 1] << 16)
            | ((uint)source[offset + 2] << 8)
            | source[offset + 3];
    }

    private static void WriteEntry(byte[] destination, int entryIndex, uint signature, int payloadOffset, int payloadSize)
    {
        int offset = 132 + (entryIndex * 12);
        WriteUInt32(destination, offset, signature);
        WriteUInt32(destination, offset + 4, (uint)payloadOffset);
        WriteUInt32(destination, offset + 8, (uint)payloadSize);
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
