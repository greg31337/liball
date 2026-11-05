using icc;

namespace icc.Tests;

public class LutTagCodecTests
{
    [Test]
    public void Lut8RoundTripsFixedTablesMatrixAndMultidimensionalClut()
    {
        IccLut lut = CreateLut(2, 3, 2, 256, 256);
        lut.Matrix.Values = new[] { 1.0, 0.125, 0.0, 0.0, 0.75, 0.0, -0.125, 0.0, 1.0 };
        lut.Clut.Values = new[] { 0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 0.25 };

        byte[] payload = LutTagCodec.SerializeLut8(lut);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version2, true);
        IccLut decoded = state.Data.Lut!;
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(1340));
            Assert.That(payload[8], Is.EqualTo(2));
            Assert.That(payload[9], Is.EqualTo(3));
            Assert.That(payload[10], Is.EqualTo(2));
            Assert.That(payload[11], Is.Zero);
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Lut));
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
            Assert.That(decoded.InputChannels, Is.EqualTo(2));
            Assert.That(decoded.OutputChannels, Is.EqualTo(3));
            Assert.That(decoded.Matrix.Rows, Is.EqualTo(3));
            Assert.That(decoded.Matrix.Columns, Is.EqualTo(3));
            Assert.That(decoded.Matrix.Values, Is.EqualTo(lut.Matrix.Values).Within(1.0 / 65536.0));
            Assert.That(decoded.InputTables, Has.Length.EqualTo(2));
            Assert.That(decoded.InputTables[0], Has.Length.EqualTo(256));
            Assert.That(decoded.OutputTables, Has.Length.EqualTo(3));
            Assert.That(decoded.Clut.GridPoints, Is.EqualTo(new[] { 2, 2 }));
            Assert.That(decoded.Clut.Values, Has.Length.EqualTo(12));
            Assert.That(decoded.Clut.Values[1], Is.EqualTo(0.1).Within(1.0 / 255.0));
            Assert.That(decoded.PcsEncoding, Is.EqualTo(IccPcsEncodingKind.Normalized));
            Assert.That(clone.Lut!.Clut.Values, Is.Not.SameAs(decoded.Clut.Values));
        });
    }

    [Test]
    public void Lut16RoundTripsDeclaredTableCountsAndLegacyIntegerPcsEncoding()
    {
        IccLut lut = CreateLut(3, 2, 2, 3, 4);
        lut.InputTables[0] = new[] { 0.0, 0.25, 1.0 };
        lut.InputTables[1] = new[] { 0.0, 0.5, 1.0 };
        lut.InputTables[2] = new[] { 0.0, 0.75, 1.0 };
        lut.OutputTables[0] = new[] { 0.0, 0.25, 0.75, 1.0 };
        lut.OutputTables[1] = new[] { 0.0, 0.125, 0.875, 1.0 };

        byte[] payload = LutTagCodec.SerializeLut16(lut);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, false);
        IccLut decoded = state.Data.Lut!;

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(118));
            Assert.That(ReadUInt16(payload, 48), Is.EqualTo(3));
            Assert.That(ReadUInt16(payload, 50), Is.EqualTo(4));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(decoded.InputTables.Select(table => table.Length), Is.EqualTo(new[] { 3, 3, 3 }));
            Assert.That(decoded.OutputTables.Select(table => table.Length), Is.EqualTo(new[] { 4, 4 }));
            Assert.That(decoded.Clut.GridPoints, Is.EqualTo(new[] { 2, 2, 2 }));
            Assert.That(decoded.Clut.Values, Has.Length.EqualTo(16));
            Assert.That(decoded.InputTables[0][1], Is.EqualTo(0.25).Within(1.0 / 65535.0));
            Assert.That(decoded.OutputTables[1][1], Is.EqualTo(0.125).Within(1.0 / 65535.0));
            Assert.That(decoded.PcsEncoding, Is.EqualTo(IccPcsEncodingKind.Version2));
        });
    }

    [Test]
    public void DecoderRejectsReservedBytesInvalidDimensionsAndClutAllocationOverflow()
    {
        byte[] valid = LutTagCodec.SerializeLut8(CreateLut(1, 1, 2, 256, 256));
        byte[] reserved = valid.ToArray();
        reserved[11] = 1;
        byte[] zeroChannels = valid.ToArray();
        zeroChannels[8] = 0;
        byte[] oneGridPoint = valid.ToArray();
        oneGridPoint[10] = 1;
        byte[] hugeClut = valid.ToArray();
        hugeClut[8] = 15;
        hugeClut[9] = 15;
        hugeClut[10] = 255;

        TagDecodeState reservedState = DecodePayload(reserved, IccVersionKind.Version4, false);
        TagDecodeState channelState = DecodePayload(zeroChannels, IccVersionKind.Version4, false);
        TagDecodeState gridState = DecodePayload(oneGridPoint, IccVersionKind.Version4, false);
        TagDecodeState overflowState = DecodePayload(hugeClut, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(reservedState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(reservedState.Result.Message, Does.Contain("reserved bytes"));
            Assert.That(channelState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(channelState.Result.Message, Does.Contain("channel counts"));
            Assert.That(gridState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(gridState.Result.Message, Does.Contain("grid-point count"));
            Assert.That(overflowState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(overflowState.Result.Message, Does.Contain("exceed the supported managed size"));
        });
    }

    [Test]
    public void DecoderRequiresExactLutSizesAndLegalLut16TableCounts()
    {
        byte[] lut8 = LutTagCodec.SerializeLut8(CreateLut(1, 1, 2, 256, 256));
        byte[] truncated = lut8.Take(lut8.Length - 1).ToArray();
        byte[] trailing = lut8.Append((byte)0).ToArray();
        byte[] lut16 = LutTagCodec.SerializeLut16(CreateLut(1, 1, 2, 2, 2));
        byte[] badInputCount = lut16.ToArray();
        WriteUInt16(badInputCount, 48, 1);
        byte[] badOutputCount = lut16.ToArray();
        WriteUInt16(badOutputCount, 50, 4097);

        TagDecodeState truncatedState = DecodePayload(truncated, IccVersionKind.Version4, false);
        TagDecodeState trailingState = DecodePayload(trailing, IccVersionKind.Version4, false);
        TagDecodeState inputCountState = DecodePayload(badInputCount, IccVersionKind.Version4, false);
        TagDecodeState outputCountState = DecodePayload(badOutputCount, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(truncatedState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(truncatedState.Result.Message, Does.Contain("exactly"));
            Assert.That(trailingState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(trailingState.Result.Message, Does.Contain("exactly"));
            Assert.That(inputCountState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(inputCountState.Result.Message, Does.Contain("between 2 and 4096"));
            Assert.That(outputCountState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(outputCountState.Result.Message, Does.Contain("between 2 and 4096"));
        });
    }

    [Test]
    public void WritersRejectMismatchedTablesDimensionsClutSizesAndSampleRanges()
    {
        IccLut wrongTableCount = CreateLut(2, 1, 2, 256, 256);
        wrongTableCount.InputTables[1] = new double[255];
        IccLut unequalGrid = CreateLut(2, 1, 2, 256, 256);
        unequalGrid.Clut.GridPoints[1] = 3;
        IccLut wrongClutSize = CreateLut(2, 1, 2, 256, 256);
        wrongClutSize.Clut.Values = new double[3];
        IccLut wrongClutChannels = CreateLut(2, 1, 2, 256, 256);
        wrongClutChannels.Clut.OutputChannels = 2;
        IccLut invalidSample = CreateLut(1, 1, 2, 2, 2);
        invalidSample.InputTables[0][1] = 1.1;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => LutTagCodec.SerializeLut8(wrongTableCount));
            Assert.Throws<IccProfileWriteException>(() => LutTagCodec.SerializeLut8(unequalGrid));
            Assert.Throws<IccProfileWriteException>(() => LutTagCodec.SerializeLut8(wrongClutSize));
            Assert.Throws<IccProfileWriteException>(() => LutTagCodec.SerializeLut8(wrongClutChannels));
            Assert.Throws<IccProfileWriteException>(() => LutTagCodec.SerializeLut16(invalidSample));
        });
    }

    private static IccLut CreateLut(int inputChannels, int outputChannels, int gridPointCount, int inputEntries, int outputEntries)
    {
        IccLut lut = new IccLut();
        lut.InputChannels = inputChannels;
        lut.OutputChannels = outputChannels;
        lut.Matrix.Rows = 3;
        lut.Matrix.Columns = 3;
        lut.Matrix.Values = new[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 };
        lut.InputTables = new double[inputChannels][];
        int index = 0;
        while (index < inputChannels)
        {
            lut.InputTables[index] = CreateRamp(inputEntries);
            index = index + 1;
        }

        lut.OutputTables = new double[outputChannels][];
        index = 0;
        while (index < outputChannels)
        {
            lut.OutputTables[index] = CreateRamp(outputEntries);
            index = index + 1;
        }

        lut.Clut.InputChannels = inputChannels;
        lut.Clut.OutputChannels = outputChannels;
        lut.Clut.GridPoints = new int[inputChannels];
        Array.Fill(lut.Clut.GridPoints, gridPointCount);
        int clutValueCount = LutTagCodec.CalculateClutValueCount(inputChannels, outputChannels, gridPointCount);
        lut.Clut.Values = CreateRamp(clutValueCount);
        return lut;
    }

    private static double[] CreateRamp(int count)
    {
        double[] values = new double[count];
        int index = 0;
        while (index < count)
        {
            values[index] = count == 1 ? 0.0 : (double)index / (count - 1);
            index = index + 1;
        }

        return values;
    }

    private static TagDecodeState DecodePayload(byte[] payload, IccVersionKind version, bool preserveOriginalBytes)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, version, preserveOriginalBytes);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static ushort ReadUInt16(byte[] source, int offset)
    {
        return (ushort)(((uint)source[offset] << 8) | source[offset + 1]);
    }

    private static void WriteUInt16(byte[] destination, int offset, ushort value)
    {
        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)value;
    }
}
