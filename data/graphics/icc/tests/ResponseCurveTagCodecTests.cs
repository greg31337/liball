using icc;

namespace icc.Tests;

public class ResponseCurveTagCodecTests
{
    [Test]
    public void ResponseCurveSetRoundTripsUnitsOffsetsXyzAndPerChannelResponses()
    {
        IccResponseCurve statusT = CreateRgbCurve(IccMeasurementUnitSignatures.StatusT);
        statusT.DeviceCodes = new[]
        {
            new ushort[] { 0, 32768, 65535 },
            new ushort[] { 0, 65535 },
            new ushort[0]
        };
        statusT.Measurements = new[]
        {
            new[] { 0.0, 0.5, 1.0 },
            new[] { -0.25, 1.25 },
            new double[0]
        };
        IccResponseCurve statusE = CreateRgbCurve(IccMeasurementUnitSignatures.StatusE);
        statusE.DeviceCodes = new[]
        {
            new ushort[] { 100 },
            new ushort[] { 200 },
            new ushort[] { 300 }
        };
        statusE.Measurements = new[]
        {
            new[] { 0.125 },
            new[] { 0.25 },
            new[] { 0.375 }
        };

        byte[] payload = ResponseCurveTagCodec.SerializeResponseCurveSet16(new[] { statusT, statusE }, IccColorSpaceSignatures.Rgb);
        TagDecodeState state = DecodePayload(payload, IccColorSpaceSignatures.Rgb, true);
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt16(payload, 8), Is.EqualTo(3));
            Assert.That(ReadUInt16(payload, 10), Is.EqualTo(2));
            Assert.That(ReadUInt32(payload, 12), Is.EqualTo(20));
            Assert.That(ReadUInt32(payload, 16), Is.EqualTo(112));
            Assert.That(ReadUInt32(payload, 20), Is.EqualTo(IccMeasurementUnitSignatures.StatusT));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.ResponseCurves));
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
            Assert.That(state.Data.ResponseCurveChannelCount, Is.EqualTo(3));
            Assert.That(state.Data.ResponseCurves.Select(value => value.MeasurementUnitSignature), Is.EqualTo(new[] { IccMeasurementUnitSignatures.StatusT, IccMeasurementUnitSignatures.StatusE }));
            Assert.That(state.Data.ResponseCurves[0].MaximumColorantXyz[1].Y, Is.EqualTo(0.5).Within(1.0 / 65536.0));
            Assert.That(state.Data.ResponseCurves[0].DeviceCodes[0], Is.EqualTo(new ushort[] { 0, 32768, 65535 }));
            Assert.That(state.Data.ResponseCurves[0].Measurements[1], Is.EqualTo(new[] { -0.25, 1.25 }).Within(1.0 / 65536.0));
            Assert.That(state.Data.ResponseCurves[0].DeviceCodes[2], Is.Empty);
            Assert.That(clone.ResponseCurveChannelCount, Is.EqualTo(3));
            Assert.That(clone.ResponseCurves[0].DeviceCodes[0], Is.Not.SameAs(state.Data.ResponseCurves[0].DeviceCodes[0]));
        });
    }

    [Test]
    public void EmptyMeasurementSetRetainsItsDeclaredChannelCount()
    {
        byte[] payload = ResponseCurveTagCodec.SerializeResponseCurveSet16(new IccResponseCurve[0], IccColorSpaceSignatures.Cmyk);
        TagDecodeState state = DecodePayload(payload, IccColorSpaceSignatures.Cmyk, false);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(12));
            Assert.That(ReadUInt16(payload, 8), Is.EqualTo(4));
            Assert.That(ReadUInt16(payload, 10), Is.Zero);
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.ResponseCurveChannelCount, Is.EqualTo(4));
            Assert.That(state.Data.ResponseCurves, Is.Empty);
        });
    }

    [Test]
    public void DecoderRejectsChannelMismatchInvalidOffsetsAndCountOverflows()
    {
        byte[] valid = ResponseCurveTagCodec.SerializeResponseCurveSet16(new[] { CreateRgbCurve(IccMeasurementUnitSignatures.StatusA) }, IccColorSpaceSignatures.Rgb);
        byte[] badOffset = valid.ToArray();
        WriteUInt32(badOffset, 12, 12);
        byte[] excessiveMeasurementTypes = valid.ToArray();
        WriteUInt16(excessiveMeasurementTypes, 10, ushort.MaxValue);
        byte[] excessiveResponses = valid.ToArray();
        int curveOffset = (int)ReadUInt32(excessiveResponses, 12);
        WriteUInt32(excessiveResponses, curveOffset + 4, uint.MaxValue);

        TagDecodeState mismatchState = DecodePayload(valid, IccColorSpaceSignatures.Cmyk, false);
        TagDecodeState offsetState = DecodePayload(badOffset, IccColorSpaceSignatures.Rgb, false);
        TagDecodeState typeCountState = DecodePayload(excessiveMeasurementTypes, IccColorSpaceSignatures.Rgb, false);
        TagDecodeState responseCountState = DecodePayload(excessiveResponses, IccColorSpaceSignatures.Rgb, false);

        Assert.Multiple(() =>
        {
            Assert.That(mismatchState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(mismatchState.Result.Message, Does.Contain("does not match"));
            Assert.That(offsetState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(offsetState.Result.Message, Does.Contain("aligned, bounded"));
            Assert.That(typeCountState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(typeCountState.Result.Message, Does.Contain("offset table"));
            Assert.That(responseCountState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(responseCountState.Result.Message, Does.Contain("more response16Number values"));
        });
    }

    [Test]
    public void DecoderRejectsUnknownDuplicateUnitsAndUnexpectedTrailingData()
    {
        byte[] valid = ResponseCurveTagCodec.SerializeResponseCurveSet16(
            new[] { CreateRgbCurve(IccMeasurementUnitSignatures.StatusA), CreateRgbCurve(IccMeasurementUnitSignatures.StatusE) },
            IccColorSpaceSignatures.Rgb);
        int firstOffset = (int)ReadUInt32(valid, 12);
        int secondOffset = (int)ReadUInt32(valid, 16);
        byte[] unknownUnit = valid.ToArray();
        WriteUInt32(unknownUnit, firstOffset, 0x62616421);
        byte[] duplicateUnit = valid.ToArray();
        WriteUInt32(duplicateUnit, secondOffset, IccMeasurementUnitSignatures.StatusA);
        byte[] trailing = valid.Append((byte)0).ToArray();

        TagDecodeState unknownState = DecodePayload(unknownUnit, IccColorSpaceSignatures.Rgb, false);
        TagDecodeState duplicateState = DecodePayload(duplicateUnit, IccColorSpaceSignatures.Rgb, false);
        TagDecodeState trailingState = DecodePayload(trailing, IccColorSpaceSignatures.Rgb, false);

        Assert.Multiple(() =>
        {
            Assert.That(unknownState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(unknownState.Result.Message, Does.Contain("unregistered measurement-unit"));
            Assert.That(duplicateState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(duplicateState.Result.Message, Does.Contain("duplicates measurement-unit"));
            Assert.That(trailingState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(trailingState.Result.Message, Does.Contain("unexpected trailing bytes"));
        });
    }

    [Test]
    public void DecoderRejectsResponseReservedBytesAndNonIncreasingDeviceCodes()
    {
        IccResponseCurve curve = CreateRgbCurve(IccMeasurementUnitSignatures.DinIPolarized);
        curve.DeviceCodes[0] = new ushort[] { 100, 200 };
        curve.Measurements[0] = new[] { 0.25, 0.5 };
        byte[] valid = ResponseCurveTagCodec.SerializeResponseCurveSet16(new[] { curve }, IccColorSpaceSignatures.Rgb);
        int curveOffset = (int)ReadUInt32(valid, 12);
        int firstResponseOffset = curveOffset + 4 + (16 * 3);
        byte[] badReserved = valid.ToArray();
        WriteUInt16(badReserved, firstResponseOffset + 2, 1);
        byte[] badOrder = valid.ToArray();
        WriteUInt16(badOrder, firstResponseOffset + 8, 100);

        TagDecodeState reservedState = DecodePayload(badReserved, IccColorSpaceSignatures.Rgb, false);
        TagDecodeState orderState = DecodePayload(badOrder, IccColorSpaceSignatures.Rgb, false);

        Assert.Multiple(() =>
        {
            Assert.That(reservedState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(reservedState.Result.Message, Does.Contain("non-zero reserved bytes"));
            Assert.That(orderState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(orderState.Result.Message, Does.Contain("strictly increasing"));
        });
    }

    [Test]
    public void WriterRejectsUnknownSpacesUnitsDuplicatesAndInconsistentChannels()
    {
        IccResponseCurve invalidUnit = CreateRgbCurve(0x62616421);
        IccResponseCurve first = CreateRgbCurve(IccMeasurementUnitSignatures.StatusM);
        IccResponseCurve duplicate = CreateRgbCurve(IccMeasurementUnitSignatures.StatusM);
        IccResponseCurve mismatched = CreateRgbCurve(IccMeasurementUnitSignatures.StatusI);
        mismatched.Measurements[0] = new[] { 0.25 };
        IccResponseCurve unordered = CreateRgbCurve(IccMeasurementUnitSignatures.DinE);
        unordered.DeviceCodes[0] = new ushort[] { 200, 100 };
        unordered.Measurements[0] = new[] { 0.25, 0.5 };

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => ResponseCurveTagCodec.SerializeResponseCurveSet16(new[] { first }, 0x62616421));
            Assert.Throws<IccProfileWriteException>(() => ResponseCurveTagCodec.SerializeResponseCurveSet16(new[] { invalidUnit }, IccColorSpaceSignatures.Rgb));
            Assert.Throws<IccProfileWriteException>(() => ResponseCurveTagCodec.SerializeResponseCurveSet16(new[] { first, duplicate }, IccColorSpaceSignatures.Rgb));
            Assert.Throws<IccProfileWriteException>(() => ResponseCurveTagCodec.SerializeResponseCurveSet16(new[] { mismatched }, IccColorSpaceSignatures.Rgb));
            Assert.Throws<IccProfileWriteException>(() => ResponseCurveTagCodec.SerializeResponseCurveSet16(new[] { unordered }, IccColorSpaceSignatures.Rgb));
        });
    }

    private static IccResponseCurve CreateRgbCurve(uint measurementUnit)
    {
        IccResponseCurve curve = new IccResponseCurve();
        curve.MeasurementUnitSignature = measurementUnit;
        curve.MaximumColorantXyz = new[]
        {
            new IccXyzNumber { X = 0.5, Y = 0.25, Z = 0.125 },
            new IccXyzNumber { X = 0.25, Y = 0.5, Z = 0.125 },
            new IccXyzNumber { X = 0.125, Y = 0.25, Z = 0.5 }
        };
        curve.DeviceCodes = new[]
        {
            new ushort[] { 0, 65535 },
            new ushort[] { 0, 65535 },
            new ushort[] { 0, 65535 }
        };
        curve.Measurements = new[]
        {
            new[] { 0.0, 1.0 },
            new[] { 0.0, 1.0 },
            new[] { 0.0, 1.0 }
        };
        return curve;
    }

    private static TagDecodeState DecodePayload(byte[] payload, uint dataColorSpaceSignature, bool preserveOriginalBytes)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, IccVersionKind.Version4, preserveOriginalBytes);
        state.HasProfileContext = true;
        state.DataColorSpaceSignature = dataColorSpaceSignature;
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static ushort ReadUInt16(byte[] source, int offset)
    {
        return (ushort)(((uint)source[offset] << 8) | source[offset + 1]);
    }

    private static uint ReadUInt32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24)
            | ((uint)source[offset + 1] << 16)
            | ((uint)source[offset + 2] << 8)
            | source[offset + 3];
    }

    private static void WriteUInt16(byte[] destination, int offset, ushort value)
    {
        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)value;
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
