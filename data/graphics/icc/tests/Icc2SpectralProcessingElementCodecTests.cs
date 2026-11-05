using icc;

namespace icc.Tests;

public class Icc2SpectralProcessingElementCodecTests
{
    [TestCase(IccProcessingElementKind.EmissionClut, 0, 4)]
    [TestCase(IccProcessingElementKind.EmissionClut, 1, 2)]
    [TestCase(IccProcessingElementKind.EmissionClut, 2, 2)]
    [TestCase(IccProcessingElementKind.EmissionClut, 3, 1)]
    [TestCase(IccProcessingElementKind.ReflectanceClut, 0, 4)]
    [TestCase(IccProcessingElementKind.ReflectanceClut, 1, 2)]
    [TestCase(IccProcessingElementKind.ReflectanceClut, 2, 2)]
    [TestCase(IccProcessingElementKind.ReflectanceClut, 3, 1)]
    public void SpectralClutsRoundTripEveryValueEncoding(IccProcessingElementKind kind, int encoding, int bytesPerValue)
    {
        IccProcessingElement source = CreateSpectralClut(kind, encoding);
        byte[] payload = Serialize(source);
        TagDecodeState state = Decode(payload);
        IccProcessingElement decoded = state.Data.MultiProcessElements!.Elements[0];
        int bodyOffset = kind == IccProcessingElementKind.ReflectanceClut ? 40 : 36;

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiProcessElements.IsExecutable, Is.True);
            Assert.That(decoded.Kind, Is.EqualTo(kind));
            Assert.That(decoded.RawData.Bytes, Has.Length.EqualTo(bodyOffset + (9 * bytesPerValue)));
            Assert.That(decoded.Clut!.InputChannels, Is.EqualTo(1));
            Assert.That(decoded.Clut.OutputChannels, Is.EqualTo(3));
            Assert.That(decoded.Clut.GridPoints, Is.EqualTo(new int[] { 2 }));
            Assert.That(decoded.Clut.Encoding, Is.EqualTo(encoding));
            Assert.That(decoded.Clut.InterpolationHint, Is.EqualTo(2));
            Assert.That(decoded.Clut.Interpolation, Is.EqualTo(IccInterpolationKind.Tetrahedral));
            Assert.That(decoded.Clut.Values[0], Is.Zero);
            Assert.That(decoded.Clut.Values[5], Is.EqualTo(1.0));
            Assert.That(decoded.SpectralProcessingData!.Flags, Is.EqualTo(kind == IccProcessingElementKind.ReflectanceClut ? 0x80000001u : 0u));
            Assert.That(decoded.SpectralProcessingData.White.Range.StartWavelength, Is.EqualTo(400.0));
            Assert.That(decoded.SpectralProcessingData.White.Range.EndWavelength, Is.EqualTo(700.0));
            Assert.That(decoded.SpectralProcessingData.White.Range.Steps, Is.EqualTo(3));
            Assert.That(decoded.SpectralProcessingData.White.SpectrumCount, Is.EqualTo(1));
            Assert.That(decoded.SpectralProcessingData.White.Values[0], Is.EqualTo(1.0));
            Assert.That(decoded.SpectralProcessingData.White.Values[2], Is.EqualTo(0.8).Within(0.001));
            Assert.That(Serialize(decoded), Is.EqualTo(payload));
        });
    }

    [TestCase(IccProcessingElementKind.EmissionMatrix, 3, 1u, 4)]
    [TestCase(IccProcessingElementKind.InverseEmissionMatrix, 3, 0u, 4)]
    public void SpectralMatricesRoundTripWhiteMatrixOffsetAndAbsoluteFlag(IccProcessingElementKind kind, int inputChannels, uint flags, int rows)
    {
        IccProcessingElement source = CreateSpectralMatrix(kind, inputChannels, flags, rows);
        byte[] payload = Serialize(source);
        TagDecodeState state = Decode(payload);
        IccProcessingElement decoded = state.Data.MultiProcessElements!.Elements[0];
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiProcessElements.IsExecutable, Is.True);
            Assert.That(decoded.Matrix!.Rows, Is.EqualTo(rows));
            Assert.That(decoded.Matrix.Columns, Is.EqualTo(3));
            Assert.That(decoded.Matrix.Values, Is.EqualTo(source.Matrix!.Values).Within(0.000001));
            Assert.That(decoded.SpectralProcessingData!.Flags, Is.EqualTo(flags));
            Assert.That(decoded.SpectralProcessingData.White.Values, Is.EqualTo(new double[] { 1.0, 0.9, 0.8 }).Within(0.000001));
            Assert.That(clone.MultiProcessElements!.Elements[0].SpectralProcessingData, Is.Not.SameAs(decoded.SpectralProcessingData));
            Assert.That(clone.MultiProcessElements.Elements[0].SpectralProcessingData!.White, Is.Not.SameAs(decoded.SpectralProcessingData.White));
            Assert.That(clone.MultiProcessElements.Elements[0].Matrix, Is.Not.SameAs(decoded.Matrix));
            Assert.That(Serialize(decoded), Is.EqualTo(payload));
        });
    }

    [TestCase(IccProcessingElementKind.EmissionObserver)]
    [TestCase(IccProcessingElementKind.ReflectanceObserver)]
    public void SpectralObserversRoundTripReferenceWhiteAndAbsoluteFlag(IccProcessingElementKind kind)
    {
        IccProcessingElement source = CreateSpectralObserver(kind);
        byte[] payload = Serialize(source);
        TagDecodeState state = Decode(payload);
        IccProcessingElement decoded = state.Data.MultiProcessElements!.Elements[0];

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiProcessElements.IsExecutable, Is.True);
            Assert.That(decoded.RawData.Bytes, Has.Length.EqualTo(32));
            Assert.That(decoded.InputChannels, Is.EqualTo(3));
            Assert.That(decoded.OutputChannels, Is.EqualTo(3));
            Assert.That(decoded.SpectralProcessingData!.Flags, Is.EqualTo(1u));
            Assert.That(decoded.SpectralProcessingData.White.Values, Is.EqualTo(new double[] { 1.0, 0.9, 0.8 }).Within(0.000001));
            Assert.That(Serialize(decoded), Is.EqualTo(payload));
        });
    }

    [Test]
    public void DecoderRejectsMalformedSpectralRangesFlagsGridsAndSizes()
    {
        byte[] emissionClut = Serialize(CreateSpectralClut(IccProcessingElementKind.EmissionClut, 0));
        int emissionOffset = (int)ReadUInt32(emissionClut, 16);
        byte[] badRange = emissionClut.ToArray();
        WriteUInt16(badRange, emissionOffset + 16, 1);

        byte[] reflectanceClut = Serialize(CreateSpectralClut(IccProcessingElementKind.ReflectanceClut, 0));
        int reflectanceOffset = (int)ReadUInt32(reflectanceClut, 16);
        byte[] badGrid = reflectanceClut.ToArray();
        badGrid[reflectanceOffset + 25] = 2;

        byte[] observer = Serialize(CreateSpectralObserver(IccProcessingElementKind.EmissionObserver));
        int observerOffset = (int)ReadUInt32(observer, 16);
        byte[] badFlag = observer.ToArray();
        WriteUInt16(badFlag, observerOffset + 18, 2);

        byte[] inverse = Serialize(CreateSpectralMatrix(IccProcessingElementKind.InverseEmissionMatrix, 3, 0, 4));
        int inverseOffset = (int)ReadUInt32(inverse, 16);
        byte[] badReserved = inverse.ToArray();
        WriteUInt16(badReserved, inverseOffset + 18, 1);

        byte[] matrixChannels = inverse.ToArray();
        WriteUInt16(matrixChannels, inverseOffset + 8, 2);

        byte[] observerChannels = observer.ToArray();
        WriteUInt16(observerChannels, observerOffset + 8, 2);

        byte[] shortObserver = observer.Take(52).ToArray();
        WriteUInt32(shortObserver, 20, 28);

        Assert.Multiple(() =>
        {
            Assert.That(Decode(badRange).Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(Decode(badRange).Result.Message, Does.Contain("wavelength range"));
            Assert.That(Decode(badGrid).Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(Decode(badGrid).Result.Message, Does.Contain("unused spectral CLUT"));
            Assert.That(Decode(badFlag).Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(Decode(badFlag).Result.Message, Does.Contain("absolute flag"));
            Assert.That(Decode(badReserved).Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(Decode(badReserved).Result.Message, Does.Contain("reserved field"));
            Assert.That(Decode(matrixChannels).Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(Decode(matrixChannels).Result.Message, Does.Contain("three input and three output"));
            Assert.That(Decode(observerChannels).Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(Decode(observerChannels).Result.Message, Does.Contain("sample count"));
            Assert.That(Decode(shortObserver).Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(Decode(shortObserver).Result.Message, Does.Contain("exactly 32 bytes"));
        });
    }

    [Test]
    public void WriterRejectsMissingSpectralDataInvalidDimensionsRangeAndValues()
    {
        IccProcessingElement missing = CreateSpectralObserver(IccProcessingElementKind.EmissionObserver);
        missing.SpectralProcessingData = null;
        IccProcessingElement badMatrix = CreateSpectralMatrix(IccProcessingElementKind.EmissionMatrix, 2, 0, 4);
        badMatrix.Matrix!.Rows = 3;
        IccProcessingElement badRange = CreateSpectralClut(IccProcessingElementKind.EmissionClut, 0);
        badRange.SpectralProcessingData!.White.Range.EndWavelength = 400.0;
        IccProcessingElement nonFinite = CreateSpectralObserver(IccProcessingElementKind.ReflectanceObserver);
        nonFinite.SpectralProcessingData!.White.Values[1] = double.NaN;
        IccProcessingElement emissionFlags = CreateSpectralClut(IccProcessingElementKind.EmissionClut, 0);
        emissionFlags.SpectralProcessingData!.Flags = 1;
        IccProcessingElement matrixChannels = CreateSpectralMatrix(IccProcessingElementKind.EmissionMatrix, 2, 0, 4);
        IccProcessingElement observerChannels = CreateSpectralObserver(IccProcessingElementKind.EmissionObserver);
        observerChannels.InputChannels = 2;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => Serialize(missing));
            Assert.Throws<IccProfileWriteException>(() => Serialize(badMatrix));
            Assert.Throws<IccProfileWriteException>(() => Serialize(badRange));
            Assert.Throws<IccProfileWriteException>(() => Serialize(nonFinite));
            Assert.Throws<IccProfileWriteException>(() => Serialize(emissionFlags));
            Assert.Throws<IccProfileWriteException>(() => Serialize(matrixChannels));
            Assert.Throws<IccProfileWriteException>(() => Serialize(observerChannels));
            Assert.That(Icc2ValidationCore.SpectralElementIsValid(matrixChannels), Is.False);
            Assert.That(Icc2ValidationCore.SpectralElementIsValid(observerChannels), Is.False);
        });
    }

    private static IccProcessingElement CreateSpectralClut(IccProcessingElementKind kind, int encoding)
    {
        IccProcessingElement element = CreateElement(kind, 1);
        element.Clut = new IccClut
        {
            InputChannels = 1,
            OutputChannels = 3,
            GridPoints = new int[] { 2 },
            Values = new double[] { 0.0, 0.25, 0.5, 0.5, 0.75, 1.0 },
            Encoding = encoding,
            InterpolationHint = 2,
            Interpolation = IccInterpolationKind.Tetrahedral
        };
        element.SpectralProcessingData = CreateSpectralData(kind == IccProcessingElementKind.ReflectanceClut ? 0x80000001u : 0u);
        return element;
    }

    private static IccProcessingElement CreateSpectralMatrix(IccProcessingElementKind kind, int inputChannels, uint flags, int rows)
    {
        IccProcessingElement element = CreateElement(kind, inputChannels);
        element.Matrix = new IccMatrix
        {
            Rows = rows,
            Columns = 3,
            Values = Enumerable.Range(0, rows * 3).Select(value => value / 10.0).ToArray()
        };
        element.SpectralProcessingData = CreateSpectralData(flags);
        return element;
    }

    private static IccProcessingElement CreateSpectralObserver(IccProcessingElementKind kind)
    {
        IccProcessingElement element = CreateElement(kind, 3);
        element.SpectralProcessingData = CreateSpectralData(1);
        return element;
    }

    private static IccSpectralProcessingData CreateSpectralData(uint flags)
    {
        return new IccSpectralProcessingData
        {
            Flags = flags,
            White = new IccSpectralData
            {
                Range = new IccSpectralRange
                {
                    StartWavelength = 400.0,
                    EndWavelength = 700.0,
                    Steps = 3
                },
                Values = new double[] { 1.0, 0.9, 0.8 },
                SpectrumCount = 1
            }
        };
    }

    private static IccProcessingElement CreateElement(IccProcessingElementKind kind, int inputChannels)
    {
        uint signature = kind == IccProcessingElementKind.EmissionClut ? IccProcessingElementSignatures.EmissionClut
            : kind == IccProcessingElementKind.ReflectanceClut ? IccProcessingElementSignatures.ReflectanceClut
            : kind == IccProcessingElementKind.EmissionMatrix ? IccProcessingElementSignatures.EmissionMatrix
            : kind == IccProcessingElementKind.InverseEmissionMatrix ? IccProcessingElementSignatures.InverseEmissionMatrix
            : kind == IccProcessingElementKind.EmissionObserver ? IccProcessingElementSignatures.EmissionObserver
            : IccProcessingElementSignatures.ReflectanceObserver;
        return new IccProcessingElement
        {
            Signature = signature,
            Kind = kind,
            InputChannels = inputChannels,
            OutputChannels = 3
        };
    }

    private static byte[] Serialize(IccProcessingElement element)
    {
        return MultiProcessElementsTagCodec.SerializeMultiProcessElementsVersion5(new IccMultiProcessElements
        {
            InputChannels = element.InputChannels,
            OutputChannels = element.OutputChannels,
            Elements = new IccProcessingElement[] { element }
        });
    }

    private static TagDecodeState Decode(byte[] payload)
    {
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 0, IccVersionKind.Version5, true);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static uint ReadUInt32(byte[] bytes, int offset)
    {
        return ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];
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
}
