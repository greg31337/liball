using icc;

namespace icc.Tests;

public class Icc2ProcessingElementCodecTests
{
    [Test]
    public void CurveSetRoundTripsSingleSampledAndExtendedSegmentedCurves()
    {
        IccCurve single = new IccCurve
        {
            Kind = IccCurveKind.SingleSampled,
            InputMinimum = -1.0,
            InputMaximum = 2.0,
            Extension = 1,
            Encoding = 3,
            Samples = new double[] { 0.0, 0.5, 1.0 }
        };
        IccCurve segmented = new IccCurve
        {
            Kind = IccCurveKind.Segmented,
            Segments = new IccCurveSegment[]
            {
                new IccCurveSegment
                {
                    Kind = IccCurveSegmentKind.Formula,
                    FormulaType = 7,
                    BreakPoint = double.NegativeInfinity,
                    Parameters = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 }
                }
            }
        };
        IccProcessingElement source = CreateElement(IccProcessingElementSignatures.CurveSet, IccProcessingElementKind.CurveSet, 2, 2);
        source.Curves = new IccCurve[] { single, segmented };

        byte[] payload = Serialize(source, 2, 2);
        TagDecodeState state = Decode(payload);
        IccProcessingElement decoded = state.Data.MultiProcessElements!.Elements[0];

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiProcessElements.IsExecutable, Is.True);
            Assert.That(decoded.Curves[0].Kind, Is.EqualTo(IccCurveKind.SingleSampled));
            Assert.That(decoded.Curves[0].InputMinimum, Is.EqualTo(-1.0));
            Assert.That(decoded.Curves[0].InputMaximum, Is.EqualTo(2.0));
            Assert.That(decoded.Curves[0].Extension, Is.EqualTo(1));
            Assert.That(decoded.Curves[0].Encoding, Is.EqualTo(3));
            Assert.That(decoded.Curves[0].Samples, Is.EqualTo(new double[] { 0.0, 128.0 / 255.0, 1.0 }));
            Assert.That(decoded.Curves[1].Segments[0].FormulaType, Is.EqualTo(7));
            Assert.That(decoded.Curves[1].Segments[0].Parameters, Is.EqualTo(segmented.Segments[0].Parameters));
            Assert.That(SerializeDecoded(state), Is.EqualTo(payload));
        });
    }

    [TestCase(0, 4)]
    [TestCase(1, 5)]
    [TestCase(2, 5)]
    [TestCase(3, 5)]
    [TestCase(4, 5)]
    [TestCase(5, 6)]
    [TestCase(6, 7)]
    [TestCase(7, 6)]
    public void SegmentedCurvesSupportEveryIcc2FormulaSelector(int formulaType, int parameterCount)
    {
        IccProcessingElement source = CreateElement(IccProcessingElementSignatures.CurveSet, IccProcessingElementKind.CurveSet, 1, 1);
        source.Curves = new IccCurve[]
        {
            new IccCurve
            {
                Kind = IccCurveKind.Segmented,
                Segments = new IccCurveSegment[]
                {
                    new IccCurveSegment
                    {
                        Kind = IccCurveSegmentKind.Formula,
                        FormulaType = formulaType,
                        BreakPoint = double.NegativeInfinity,
                        Parameters = Enumerable.Range(1, parameterCount).Select(value => (double)value).ToArray()
                    }
                }
            }
        };

        TagDecodeState state = Decode(Serialize(source, 1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiProcessElements!.Elements[0].Curves[0].Segments[0].FormulaType, Is.EqualTo(formulaType));
            Assert.That(state.Data.MultiProcessElements.Elements[0].Curves[0].Segments[0].Parameters, Has.Length.EqualTo(parameterCount));
        });
    }

    [Test]
    public void MatrixAndFloatClutUseStructuredIcc2Bodies()
    {
        IccProcessingElement matrix = CreateElement(IccProcessingElementSignatures.Matrix, IccProcessingElementKind.Matrix, 2, 2);
        matrix.Matrix = new IccMatrix
        {
            Rows = 2,
            Columns = 3,
            Values = new double[] { 1.0, 0.0, 0.25, 0.0, 1.0, -0.25 }
        };
        IccProcessingElement clut = CreateElement(IccProcessingElementSignatures.Clut, IccProcessingElementKind.Clut, 2, 1);
        clut.Clut = new IccClut
        {
            InputChannels = 2,
            OutputChannels = 1,
            GridPoints = new int[] { 2, 2 },
            Values = new double[] { 0.0, 0.25, 0.75, 1.0 }
        };

        byte[] payload = Serialize(new IccProcessingElement[] { matrix, clut }, 2, 1);
        TagDecodeState state = Decode(payload);
        IccProcessingElement[] decoded = state.Data.MultiProcessElements!.Elements;

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiProcessElements.IsExecutable, Is.True);
            Assert.That(decoded[0].Matrix!.Values, Is.EqualTo(matrix.Matrix!.Values));
            Assert.That(decoded[1].Clut!.GridPoints, Is.EqualTo(new int[] { 2, 2 }));
            Assert.That(decoded[1].Clut!.Values, Is.EqualTo(clut.Clut!.Values));
            Assert.That(SerializeDecoded(state), Is.EqualTo(payload));
        });
    }

    [TestCase(0, 4)]
    [TestCase(1, 2)]
    [TestCase(2, 2)]
    [TestCase(3, 1)]
    public void ExtendedClutRoundTripsEveryValueEncoding(int encoding, int bytesPerValue)
    {
        IccProcessingElement source = CreateElement(IccProcessingElementSignatures.ExtendedClut, IccProcessingElementKind.ExtendedClut, 1, 2);
        source.Clut = new IccClut
        {
            InputChannels = 1,
            OutputChannels = 2,
            GridPoints = new int[] { 2 },
            Values = new double[] { 0.0, 0.25, 0.75, 1.0 },
            Encoding = encoding,
            InterpolationHint = 2,
            Interpolation = IccInterpolationKind.Tetrahedral
        };

        byte[] payload = Serialize(source, 1, 2);
        TagDecodeState state = Decode(payload);
        IccClut decoded = state.Data.MultiProcessElements!.Elements[0].Clut!;

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(payload, Has.Length.EqualTo(24 + 32 + (4 * bytesPerValue)));
            Assert.That(decoded.Encoding, Is.EqualTo(encoding));
            Assert.That(decoded.InterpolationHint, Is.EqualTo(2));
            Assert.That(decoded.Interpolation, Is.EqualTo(IccInterpolationKind.Tetrahedral));
            Assert.That(decoded.Values[0], Is.Zero);
            Assert.That(decoded.Values[3], Is.EqualTo(1.0));
            Assert.That(SerializeDecoded(state), Is.EqualTo(payload));
        });
    }

    [Test]
    public void TintArrayRoundTripsEncodedOneDimensionalSamples()
    {
        IccProcessingElement source = CreateElement(IccProcessingElementSignatures.TintArray, IccProcessingElementKind.TintArray, 1, 3);
        source.Clut = new IccClut
        {
            InputChannels = 1,
            OutputChannels = 3,
            GridPoints = new int[] { 2 },
            Values = new double[] { 0.0, 0.25, 0.5, 0.5, 0.75, 1.0 },
            Encoding = 2,
            Interpolation = IccInterpolationKind.Linear
        };

        byte[] payload = Serialize(source, 1, 3);
        TagDecodeState state = Decode(payload);
        IccClut decoded = state.Data.MultiProcessElements!.Elements[0].Clut!;

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(decoded.GridPoints, Is.EqualTo(new int[] { 2 }));
            Assert.That(decoded.Encoding, Is.EqualTo(2));
            Assert.That(decoded.Interpolation, Is.EqualTo(IccInterpolationKind.Linear));
            Assert.That(decoded.Values[5], Is.EqualTo(1.0));
            Assert.That(SerializeDecoded(state), Is.EqualTo(payload));
        });
    }

    [Test]
    public void SparseMatrixElementRoundTripsCompactMatrixLut()
    {
        IccProcessingElement source = CreateElement(IccProcessingElementSignatures.SparseMatrix, IccProcessingElementKind.SparseMatrix, 1, 2);
        source.SparseMatrixLut = new IccSparseMatrixArray
        {
            EquivalentOutputChannels = 2,
            Encoding = 1,
            GridPoints = new int[] { 2 },
            Matrices = new IccSparseMatrix[]
            {
                CreateSparseMatrix(0.0),
                CreateSparseMatrix(1.0)
            }
        };

        byte[] payload = Serialize(source, 1, 2);
        TagDecodeState state = Decode(payload);
        IccSparseMatrixArray decoded = state.Data.MultiProcessElements!.Elements[0].SparseMatrixLut!;

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(decoded.GridPoints, Is.EqualTo(new int[] { 2 }));
            Assert.That(decoded.Matrices, Has.Length.EqualTo(2));
            Assert.That(decoded.Matrices[0].Rows, Is.EqualTo(1));
            Assert.That(decoded.Matrices[0].Columns, Is.EqualTo(2));
            Assert.That(decoded.Matrices[1].Values, Is.EqualTo(new double[] { 1.0 }));
            Assert.That(SerializeDecoded(state), Is.EqualTo(payload));
        });
    }

    [Test]
    public void ToneMapRoundTripsLuminanceCurveAndSharedChannelFunctions()
    {
        IccToneMapFunction function = new IccToneMapFunction
        {
            FunctionType = 0,
            Parameters = new double[] { 1.25, -0.1, 0.05 }
        };
        IccProcessingElement source = CreateElement(IccProcessingElementSignatures.ToneMap, IccProcessingElementKind.ToneMap, 3, 2);
        source.ToneMap = new IccToneMap
        {
            LuminanceCurve = new IccCurve
            {
                Kind = IccCurveKind.SingleSampled,
                InputMinimum = 0.0,
                InputMaximum = 4.0,
                Extension = 0,
                Encoding = 1,
                Samples = new double[] { 0.0, 1.0 }
            },
            Functions = new IccToneMapFunction[] { function, function }
        };

        byte[] payload = Serialize(source, 3, 2);
        TagDecodeState state = Decode(payload);
        IccToneMap decoded = state.Data.MultiProcessElements!.Elements[0].ToneMap!;

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(decoded.LuminanceCurve.InputMaximum, Is.EqualTo(4.0));
            Assert.That(decoded.Functions[0].Parameters, Is.EqualTo(function.Parameters).Within(0.000001));
            Assert.That(decoded.Functions[0], Is.SameAs(decoded.Functions[1]));
            Assert.That(SerializeDecoded(state), Is.EqualTo(payload));
        });
    }

    [Test]
    public void DecoderRejectsMalformedIcc2ElementSelectorsAndDimensions()
    {
        IccProcessingElement extended = CreateElement(IccProcessingElementSignatures.ExtendedClut, IccProcessingElementKind.ExtendedClut, 1, 1);
        extended.Clut = new IccClut
        {
            InputChannels = 1,
            OutputChannels = 1,
            GridPoints = new int[] { 2 },
            Values = new double[] { 0.0, 1.0 },
            Encoding = 3
        };
        byte[] badHint = Serialize(extended, 1, 1);
        int elementOffset = (int)ReadUInt32(badHint, 16);
        badHint[elementOffset + 7] = 3;

        IccProcessingElement tint = CreateElement(IccProcessingElementSignatures.TintArray, IccProcessingElementKind.TintArray, 1, 1);
        tint.Clut = new IccClut
        {
            InputChannels = 1,
            OutputChannels = 1,
            GridPoints = new int[] { 2 },
            Values = new double[] { 0.0, 1.0 },
            Encoding = 3
        };
        byte[] badTint = Serialize(tint, 1, 1).Take(44).ToArray();
        WriteUInt32(badTint, 20, 20);

        Assert.Multiple(() =>
        {
            Assert.That(Decode(badHint).Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(Decode(badHint).Result.Message, Does.Contain("interpolation hint"));
            Assert.That(Decode(badTint).Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
        });
    }

    private static IccSparseMatrix CreateSparseMatrix(double value)
    {
        return new IccSparseMatrix
        {
            Rows = 1,
            Columns = 2,
            Encoding = 1,
            RowStartOffsets = new int[] { 0 },
            ColumnIndices = new int[] { 1 },
            Values = new double[] { value }
        };
    }

    private static IccProcessingElement CreateElement(uint signature, IccProcessingElementKind kind, int inputChannels, int outputChannels)
    {
        return new IccProcessingElement
        {
            Signature = signature,
            Kind = kind,
            InputChannels = inputChannels,
            OutputChannels = outputChannels
        };
    }

    private static byte[] Serialize(IccProcessingElement element, int inputChannels, int outputChannels)
    {
        return Serialize(new IccProcessingElement[] { element }, inputChannels, outputChannels);
    }

    private static byte[] Serialize(IccProcessingElement[] elements, int inputChannels, int outputChannels)
    {
        return MultiProcessElementsTagCodec.SerializeMultiProcessElementsVersion5(new IccMultiProcessElements
        {
            InputChannels = inputChannels,
            OutputChannels = outputChannels,
            Elements = elements
        });
    }

    private static byte[] SerializeDecoded(TagDecodeState state)
    {
        return MultiProcessElementsTagCodec.SerializeMultiProcessElementsVersion5(state.Data.MultiProcessElements!);
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

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
