using icc;

namespace icc.Tests;

public class MultiStageLutTagCodecTests
{
    [TestCase(true, false, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    [TestCase(true, true, true)]
    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, true)]
    [TestCase(false, true, true)]
    public void RoundTripsEveryLegalStageCombination(bool aToB, bool hasMatrix, bool hasClut)
    {
        IccMultiStageLut source = CreateLut(aToB, hasMatrix, hasClut);

        byte[] payload = aToB
            ? MultiStageLutTagCodec.SerializeLutAToB(source)
            : MultiStageLutTagCodec.SerializeLutBToA(source);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, true);
        IccMultiStageLut decoded = state.Data.MultiStageLut!;

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(payload, 0), Is.EqualTo(aToB ? IccTypeSignatures.LutAToB : IccTypeSignatures.LutBToA));
            Assert.That(ReadUInt32(payload, 12), Is.GreaterThanOrEqualTo(32));
            Assert.That(ReadUInt32(payload, 16) != 0, Is.EqualTo(hasMatrix));
            Assert.That(ReadUInt32(payload, 20) != 0, Is.EqualTo(hasMatrix));
            Assert.That(ReadUInt32(payload, 24) != 0, Is.EqualTo(hasClut));
            Assert.That(ReadUInt32(payload, 28) != 0, Is.EqualTo(hasClut));
            Assert.That(payload.Length % 4, Is.Zero);
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Lut));
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
            Assert.That(decoded.InputChannels, Is.EqualTo(source.InputChannels));
            Assert.That(decoded.OutputChannels, Is.EqualTo(source.OutputChannels));
            Assert.That(decoded.BCurves, Has.Length.EqualTo(source.BCurves.Length));
            Assert.That(decoded.MCurves, Has.Length.EqualTo(source.MCurves.Length));
            Assert.That(decoded.ACurves, Has.Length.EqualTo(source.ACurves.Length));
            Assert.That(decoded.Matrix != null, Is.EqualTo(hasMatrix));
            Assert.That(decoded.Clut != null, Is.EqualTo(hasClut));
            Assert.That(decoded.PcsEncoding, Is.EqualTo(IccPcsEncodingKind.Version4));
        });

        if (hasMatrix)
        {
            Assert.Multiple(() =>
            {
                Assert.That(decoded.Matrix!.Rows, Is.EqualTo(3));
                Assert.That(decoded.Matrix.Columns, Is.EqualTo(4));
                Assert.That(decoded.Matrix.Values, Is.EqualTo(source.Matrix!.Values).Within(1.0 / 65536.0));
                Assert.That(decoded.MCurves[0].Kind, Is.EqualTo(IccCurveKind.Gamma));
                Assert.That(decoded.MCurves[0].Gamma, Is.EqualTo(1.5).Within(1.0 / 256.0));
            });
        }

        if (hasClut)
        {
            Assert.Multiple(() =>
            {
                Assert.That(decoded.Clut!.GridPoints, Is.EqualTo(source.Clut!.GridPoints));
                Assert.That(decoded.Clut.Values, Is.EqualTo(source.Clut.Values).Within(1.0 / 65535.0));
                Assert.That(decoded.ACurves[0].Kind, Is.EqualTo(IccCurveKind.Parametric));
                Assert.That(decoded.ACurves[0].Parametric!.Parameters[0], Is.EqualTo(2.0).Within(1.0 / 65536.0));
            });
        }
    }

    [Test]
    public void CanonicalWriterSharesIdenticalCurveSetsAndDecoderAcceptsSharedOffsets()
    {
        IccMultiStageLut source = CreateLut(true, true, false);
        source.BCurves = CreateIdentityCurves(3);
        source.MCurves = CreateIdentityCurves(3);

        byte[] payload = MultiStageLutTagCodec.SerializeLutAToB(source);
        uint bOffset = ReadUInt32(payload, 12);
        uint mOffset = ReadUInt32(payload, 20);
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(mOffset, Is.EqualTo(bOffset));
            Assert.That(ReadUInt32(payload, 16), Is.GreaterThan(bOffset));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiStageLut!.BCurves, Has.Length.EqualTo(3));
            Assert.That(state.Data.MultiStageLut.MCurves, Has.Length.EqualTo(3));
        });
    }

    [Test]
    public void MatrixCoefficientsPrecedeItsThreeOffsetsOnTheWire()
    {
        IccMultiStageLut source = CreateLut(true, true, false);

        byte[] payload = MultiStageLutTagCodec.SerializeLutAToB(source);
        int matrixOffset = (int)ReadUInt32(payload, 16);

        Assert.Multiple(() =>
        {
            Assert.That(ReadS15Fixed16(payload, matrixOffset), Is.EqualTo(1.0));
            Assert.That(ReadS15Fixed16(payload, matrixOffset + 12), Is.EqualTo(0.0));
            Assert.That(ReadS15Fixed16(payload, matrixOffset + 16), Is.EqualTo(1.0));
            Assert.That(ReadS15Fixed16(payload, matrixOffset + 32), Is.EqualTo(1.0));
            Assert.That(ReadS15Fixed16(payload, matrixOffset + 36), Is.EqualTo(0.125));
            Assert.That(ReadS15Fixed16(payload, matrixOffset + 40), Is.EqualTo(-0.25));
            Assert.That(ReadS15Fixed16(payload, matrixOffset + 44), Is.EqualTo(0.5));
        });
    }

    [Test]
    public void DecoderFollowsOffsetsWhenElementsAreNotStoredInProcessingOrder()
    {
        IccMultiStageLut source = CreateLut(true, true, false);
        byte[] canonical = MultiStageLutTagCodec.SerializeLutAToB(source);
        int oldB = (int)ReadUInt32(canonical, 12);
        int oldMatrix = (int)ReadUInt32(canonical, 16);
        int oldM = (int)ReadUInt32(canonical, 20);
        int bSize = oldMatrix - oldB;
        int matrixSize = oldM - oldMatrix;
        int mSize = canonical.Length - oldM;
        byte[] reordered = canonical.Take(32).Concat(canonical.Skip(oldMatrix).Take(matrixSize))
            .Concat(canonical.Skip(oldM).Take(mSize)).Concat(canonical.Skip(oldB).Take(bSize)).ToArray();
        WriteUInt32(reordered, 12, (uint)(32 + matrixSize + mSize));
        WriteUInt32(reordered, 16, 32);
        WriteUInt32(reordered, 20, (uint)(32 + matrixSize));

        TagDecodeState state = DecodePayload(reordered, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiStageLut!.BCurves, Has.Length.EqualTo(3));
            Assert.That(state.Data.MultiStageLut.MCurves[0].Gamma, Is.EqualTo(1.5).Within(1.0 / 256.0));
            Assert.That(state.Data.MultiStageLut.Matrix!.Values[3], Is.EqualTo(0.125).Within(1.0 / 65536.0));
        });
    }

    [Test]
    public void DecoderReadsOneByteClutPrecisionAndVariableGridDimensions()
    {
        IccMultiStageLut source = CreateLut(true, false, true);
        source.InputChannels = 1;
        source.OutputChannels = 1;
        source.BCurves = CreateIdentityCurves(1);
        source.ACurves = CreateIdentityCurves(1);
        source.Clut!.InputChannels = 1;
        source.Clut.OutputChannels = 1;
        source.Clut.GridPoints = new[] { 2 };
        source.Clut.Values = new[] { 0.0, 1.0 };
        byte[] payload = MultiStageLutTagCodec.SerializeLutAToB(source);
        int clutOffset = (int)ReadUInt32(payload, 24);
        payload[clutOffset + 16] = 1;
        payload[clutOffset + 20] = 0;
        payload[clutOffset + 21] = 255;
        payload[clutOffset + 22] = 0;
        payload[clutOffset + 23] = 0;

        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiStageLut!.Clut!.GridPoints, Is.EqualTo(new[] { 2 }));
            Assert.That(state.Data.MultiStageLut.Clut.Values, Is.EqualTo(new[] { 0.0, 1.0 }));
        });
    }

    [Test]
    public void VersionDispatchRejectsTheV4TypesInV2AndDecodesThemInV4()
    {
        byte[] payload = MultiStageLutTagCodec.SerializeLutAToB(CreateLut(true, false, false));

        TagDecodeState version2 = DecodePayload(payload, IccVersionKind.Version2, false);
        TagDecodeState version4 = DecodePayload(payload, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(version2.Result.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(version2.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(version2.Result.Message, Does.Contain("ICC.1 version 2"));
            Assert.That(version4.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(version4.Data.MultiStageLut!.PcsEncoding, Is.EqualTo(IccPcsEncodingKind.Version4));
        });
    }

    [Test]
    public void LutBToARequiresAColorimetricPcsWhenProfileContextIsAvailable()
    {
        byte[] payload = MultiStageLutTagCodec.SerializeLutBToA(CreateLut(false, false, false));
        TagDecodeState spectral = CreateDecodeState(payload, IccVersionKind.Version4, false);
        spectral.HasProfileContext = true;
        spectral.ProfileConnectionSpace = IccPcsKind.Spectral;
        TagCodecDispatcher.DecodeTagPayload(spectral);
        TagDecodeState xyz = CreateDecodeState(payload, IccVersionKind.Version4, false);
        xyz.HasProfileContext = true;
        xyz.ProfileConnectionSpace = IccPcsKind.Xyz;
        TagCodecDispatcher.DecodeTagPayload(xyz);

        Assert.Multiple(() =>
        {
            Assert.That(spectral.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(spectral.Result.Message, Does.Contain("PCSXYZ or PCSLAB"));
            Assert.That(xyz.Result.Status, Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [Test]
    public void DecoderRejectsIllegalStagePairsOffsetsGapsAndCurvePadding()
    {
        byte[] withClut = MultiStageLutTagCodec.SerializeLutAToB(CreateLut(true, false, true));
        byte[] missingA = withClut.ToArray();
        WriteUInt32(missingA, 28, 0);
        byte[] unalignedB = withClut.ToArray();
        WriteUInt32(unalignedB, 12, 33);
        byte[] trailing = withClut.Append((byte)0).ToArray();

        byte[] overlap = MultiStageLutTagCodec.SerializeLutAToB(CreateLut(true, true, false));
        WriteUInt32(overlap, 16, ReadUInt32(overlap, 12));

        IccMultiStageLut gammaLut = CreateLut(true, false, false);
        gammaLut.BCurves = new[] { CreateGammaCurve(1.5), CreateGammaCurve(1.5), CreateGammaCurve(1.5) };
        byte[] badPadding = MultiStageLutTagCodec.SerializeLutAToB(gammaLut);
        badPadding[47] = 1;

        TagDecodeState pairState = DecodePayload(missingA, IccVersionKind.Version4, false);
        TagDecodeState offsetState = DecodePayload(unalignedB, IccVersionKind.Version4, false);
        TagDecodeState trailingState = DecodePayload(trailing, IccVersionKind.Version4, false);
        TagDecodeState overlapState = DecodePayload(overlap, IccVersionKind.Version4, false);
        TagDecodeState paddingState = DecodePayload(badPadding, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(pairState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(pairState.Result.Message, Does.Contain("CLUT and A-curve stages together"));
            Assert.That(offsetState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(offsetState.Result.Message, Does.Contain("four-byte aligned"));
            Assert.That(trailingState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(trailingState.Result.Message, Does.Contain("trailing or unreferenced data"));
            Assert.That(overlapState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(overlapState.Result.Message, Does.Contain("overlap illegally"));
            Assert.That(paddingState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(paddingState.Result.Message, Does.Contain("alignment padding"));
        });
    }

    [Test]
    public void DecoderRejectsMalformedClutHeaderAndMatrixTransition()
    {
        byte[] valid = MultiStageLutTagCodec.SerializeLutAToB(CreateLut(true, false, true));
        int clutOffset = (int)ReadUInt32(valid, 24);
        byte[] badUnusedGrid = valid.ToArray();
        badUnusedGrid[clutOffset + 15] = 2;
        byte[] badPrecision = valid.ToArray();
        badPrecision[clutOffset + 16] = 3;

        byte[] matrix = MultiStageLutTagCodec.SerializeLutAToB(CreateLut(true, true, true));
        matrix[9] = 2;

        TagDecodeState gridState = DecodePayload(badUnusedGrid, IccVersionKind.Version4, false);
        TagDecodeState precisionState = DecodePayload(badPrecision, IccVersionKind.Version4, false);
        TagDecodeState matrixState = DecodePayload(matrix, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(gridState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(gridState.Result.Message, Does.Contain("Unused CLUT"));
            Assert.That(precisionState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(precisionState.Result.Message, Does.Contain("precision"));
            Assert.That(matrixState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(matrixState.Result.Message, Does.Contain("matrix stage requires three channels"));
        });
    }

    [Test]
    public void WritersRejectIllegalCombinationsAndMismatchedChannelTransitions()
    {
        IccMultiStageLut missingB = CreateLut(true, false, false);
        missingB.BCurves = new IccCurve[0];
        IccMultiStageLut matrixWithoutM = CreateLut(true, true, false);
        matrixWithoutM.MCurves = new IccCurve[0];
        IccMultiStageLut clutWithoutA = CreateLut(true, false, true);
        clutWithoutA.ACurves = new IccCurve[0];
        IccMultiStageLut transitionWithoutClut = CreateLut(true, false, false);
        transitionWithoutClut.OutputChannels = 2;
        transitionWithoutClut.BCurves = CreateIdentityCurves(2);
        IccMultiStageLut badMatrixShape = CreateLut(false, true, false);
        badMatrixShape.Matrix!.Columns = 3;
        IccMultiStageLut badClutSize = CreateLut(true, false, true);
        badClutSize.Clut!.Values = new double[1];
        IccMultiStageLut badValue = CreateLut(true, false, true);
        badValue.Clut!.Values[0] = 1.1;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => MultiStageLutTagCodec.SerializeLutAToB(missingB));
            Assert.Throws<IccProfileWriteException>(() => MultiStageLutTagCodec.SerializeLutAToB(matrixWithoutM));
            Assert.Throws<IccProfileWriteException>(() => MultiStageLutTagCodec.SerializeLutAToB(clutWithoutA));
            Assert.Throws<IccProfileWriteException>(() => MultiStageLutTagCodec.SerializeLutAToB(transitionWithoutClut));
            Assert.Throws<IccProfileWriteException>(() => MultiStageLutTagCodec.SerializeLutBToA(badMatrixShape));
            Assert.Throws<IccProfileWriteException>(() => MultiStageLutTagCodec.SerializeLutAToB(badClutSize));
            Assert.Throws<IccProfileWriteException>(() => MultiStageLutTagCodec.SerializeLutAToB(badValue));
        });
    }

    [Test]
    public void DecodedMultiStageDataClonesWithoutMutableAliasing()
    {
        byte[] payload = MultiStageLutTagCodec.SerializeLutBToA(CreateLut(false, true, true));
        TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, false);

        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);
        IccMultiStageLut original = state.Data.MultiStageLut!;
        IccMultiStageLut copied = clone.MultiStageLut!;

        Assert.Multiple(() =>
        {
            Assert.That(copied, Is.Not.SameAs(original));
            Assert.That(copied.BCurves, Is.Not.SameAs(original.BCurves));
            Assert.That(copied.BCurves[0], Is.Not.SameAs(original.BCurves[0]));
            Assert.That(copied.Matrix!.Values, Is.Not.SameAs(original.Matrix!.Values));
            Assert.That(copied.Clut!.Values, Is.Not.SameAs(original.Clut!.Values));
        });
    }

    private static IccMultiStageLut CreateLut(bool aToB, bool hasMatrix, bool hasClut)
    {
        IccMultiStageLut lut = new IccMultiStageLut();
        if (hasClut)
        {
            lut.InputChannels = aToB ? 2 : (hasMatrix ? 3 : 2);
            lut.OutputChannels = aToB ? (hasMatrix ? 3 : 2) : 2;
        }
        else
        {
            lut.InputChannels = 3;
            lut.OutputChannels = 3;
        }

        int bCount = aToB ? lut.OutputChannels : lut.InputChannels;
        int mCount = aToB ? lut.OutputChannels : lut.InputChannels;
        int aCount = aToB ? lut.InputChannels : lut.OutputChannels;
        lut.BCurves = CreateIdentityCurves(bCount);
        if (hasMatrix)
        {
            lut.Matrix = new IccMatrix();
            lut.Matrix.Rows = 3;
            lut.Matrix.Columns = 4;
            lut.Matrix.Values = new[]
            {
                1.0, 0.0, 0.0, 0.125,
                0.0, 1.0, 0.0, -0.25,
                0.0, 0.0, 1.0, 0.5
            };
            lut.MCurves = new IccCurve[mCount];
            int index = 0;
            while (index < mCount)
            {
                lut.MCurves[index] = CreateGammaCurve(1.5 + (index * 0.25));
                index++;
            }
        }

        if (hasClut)
        {
            lut.ACurves = new IccCurve[aCount];
            int index = 0;
            while (index < aCount)
            {
                lut.ACurves[index] = CreateParametricCurve(2.0 + index);
                index++;
            }

            lut.Clut = new IccClut();
            lut.Clut.InputChannels = lut.InputChannels;
            lut.Clut.OutputChannels = lut.OutputChannels;
            lut.Clut.GridPoints = new int[lut.InputChannels];
            int valueCount = lut.OutputChannels;
            index = 0;
            while (index < lut.InputChannels)
            {
                lut.Clut.GridPoints[index] = index == 0 ? 2 : 3;
                valueCount *= lut.Clut.GridPoints[index];
                index++;
            }

            lut.Clut.Values = CreateRamp(valueCount);
        }

        return lut;
    }

    private static IccCurve[] CreateIdentityCurves(int count)
    {
        IccCurve[] curves = new IccCurve[count];
        int index = 0;
        while (index < count)
        {
            curves[index] = new IccCurve();
            index++;
        }

        return curves;
    }

    private static IccCurve CreateGammaCurve(double gamma)
    {
        IccCurve curve = new IccCurve();
        curve.Kind = IccCurveKind.Gamma;
        curve.Gamma = gamma;
        return curve;
    }

    private static IccCurve CreateParametricCurve(double gamma)
    {
        IccCurve curve = new IccCurve();
        curve.Kind = IccCurveKind.Parametric;
        curve.Parametric = new IccParametricCurve();
        curve.Parametric.FunctionType = 0;
        curve.Parametric.Parameters = new[] { gamma };
        return curve;
    }

    private static double[] CreateRamp(int count)
    {
        double[] values = new double[count];
        int index = 0;
        while (index < count)
        {
            values[index] = count == 1 ? 0.0 : (double)index / (count - 1);
            index++;
        }

        return values;
    }

    private static TagDecodeState DecodePayload(byte[] payload, IccVersionKind version, bool preserveOriginalBytes)
    {
        TagDecodeState state = CreateDecodeState(payload, version, preserveOriginalBytes);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static TagDecodeState CreateDecodeState(byte[] payload, IccVersionKind version, bool preserveOriginalBytes)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        return new TagDecodeState(source, 0, payload.Length, 0, version, preserveOriginalBytes);
    }

    private static uint ReadUInt32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24)
            | ((uint)source[offset + 1] << 16)
            | ((uint)source[offset + 2] << 8)
            | source[offset + 3];
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }

    private static double ReadS15Fixed16(byte[] source, int offset)
    {
        uint encoded = ReadUInt32(source, offset);
        int signed = encoded <= int.MaxValue ? (int)encoded : -1 - (int)(uint.MaxValue - encoded);
        return signed / 65536.0;
    }
}
