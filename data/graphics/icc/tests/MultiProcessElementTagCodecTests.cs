using icc;

namespace icc.Tests;

public class MultiProcessElementTagCodecTests
{
    [Test]
    public void RoundTripsSegmentedCurvesAffineMatrixAndNDimensionalFloatClut()
    {
        IccCurve firstCurve = CreateCurve(CreateFormula(double.NegativeInfinity, 0, 2.0, 1.0, 0.0, 0.0));
        IccCurve secondCurve = CreateCurve(
            CreateFormula(double.NegativeInfinity, 1, 2.0, 1.0, 0.0, 0.0, 0.0),
            CreateSampled(0.25, 0.2, 0.3),
            CreateFormula(0.75, 2, 1.0, 0.5, 0.25, 0.125, 0.0));
        IccProcessingElement curves = CreateCurveSet(firstCurve, secondCurve);
        IccProcessingElement matrix = CreateMatrix(2, 3, new double[]
        {
            1.0, 0.0, 0.125,
            0.0, 1.0, -0.25,
            0.5, 0.25, 0.75
        });
        IccProcessingElement clut = CreateClut(3, 1, new int[] { 2, 2, 2 }, new double[]
        {
            -2.0, -0.5, 0.0, 0.25, 0.5, 1.0, 1.5, 4.0
        });

        byte[] payload = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(2, 1, curves, matrix, clut));
        TagDecodeState state = DecodePayload(payload);
        IccMultiProcessElements decoded = state.Data.MultiProcessElements!;
        IccCurveSegment[] decodedSegments = decoded.Elements[0].Curves[1].Segments;
        byte[] roundTrip = MultiProcessElementsTagCodec.SerializeMultiProcessElements(decoded);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(decoded.Elements.Select(element => element.Kind), Is.EqualTo(new[]
            {
                IccProcessingElementKind.CurveSet,
                IccProcessingElementKind.Matrix,
                IccProcessingElementKind.Clut
            }));
            Assert.That(decoded.Elements[0].Curves, Has.Length.EqualTo(2));
            Assert.That(decodedSegments.Select(segment => segment.Kind), Is.EqualTo(new[]
            {
                IccCurveSegmentKind.Formula,
                IccCurveSegmentKind.Sampled,
                IccCurveSegmentKind.Formula
            }));
            Assert.That(decodedSegments.Select(segment => segment.FormulaType), Is.EqualTo(new[] { 1, 0, 2 }));
            Assert.That(decodedSegments.Select(segment => segment.BreakPoint), Is.EqualTo(new[] { double.NegativeInfinity, 0.25, 0.75 }));
            Assert.That(decodedSegments[1].Samples, Is.EqualTo(new[] { 0.2, 0.3 }).Within(0.000001));
            Assert.That(decoded.Elements[1].Matrix!.Rows, Is.EqualTo(3));
            Assert.That(decoded.Elements[1].Matrix!.Columns, Is.EqualTo(3));
            Assert.That(decoded.Elements[1].Matrix!.Values, Is.EqualTo(matrix.Matrix!.Values).Within(0.000001));
            Assert.That(decoded.Elements[2].Clut!.GridPoints, Is.EqualTo(new[] { 2, 2, 2 }));
            Assert.That(decoded.Elements[2].Clut!.Values, Is.EqualTo(clut.Clut!.Values).Within(0.000001));
            Assert.That(decoded.Elements[2].InputRanges.Select(range => range.Minimum), Is.EqualTo(new[] { 0.0, 0.0, 0.0 }));
            Assert.That(decoded.Elements[2].InputRanges.Select(range => range.Maximum), Is.EqualTo(new[] { 1.0, 1.0, 1.0 }));
            Assert.That(decoded.Elements.All(element => element.RawData.Bytes.Length > 0), Is.True);
            Assert.That(roundTrip, Is.EqualTo(payload));
        });
    }

    [Test]
    public void MatrixWireOrderStoresCoefficientsBeforeOutputConstants()
    {
        IccProcessingElement matrix = CreateMatrix(2, 3, new double[]
        {
            1.0, 2.0, 10.0,
            3.0, 4.0, 20.0,
            5.0, 6.0, 30.0
        });
        byte[] payload = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(2, 3, matrix));
        int elementOffset = (int)ReadUInt32(payload, 16);
        double[] wireValues = new double[9];
        int index = 0;
        while (index < wireValues.Length)
        {
            wireValues[index] = ReadFloat32(payload, elementOffset + 12 + (index * 4));
            index++;
        }

        TagDecodeState state = DecodePayload(payload);

        Assert.Multiple(() =>
        {
            Assert.That(wireValues, Is.EqualTo(new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 10.0, 20.0, 30.0 }));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiProcessElements!.Elements[0].Matrix!.Values, Is.EqualTo(matrix.Matrix!.Values));
        });
    }

    [Test]
    public void CurveSetSharesEqualNestedCurvesAndClonePreservesFormulaSelectors()
    {
        IccCurve curve = CreateCurve(CreateFormula(double.NegativeInfinity, 2, 1.0, 2.0, 3.0, 4.0, 5.0));
        byte[] payload = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(2, 2, CreateCurveSet(curve, curve)));
        int elementOffset = (int)ReadUInt32(payload, 16);
        TagDecodeState state = DecodePayload(payload);
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(payload, elementOffset + 12), Is.EqualTo(ReadUInt32(payload, elementOffset + 20)));
            Assert.That(ReadUInt32(payload, elementOffset + 16), Is.EqualTo(ReadUInt32(payload, elementOffset + 24)));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiProcessElements!.Elements[0].Curves[0], Is.SameAs(state.Data.MultiProcessElements.Elements[0].Curves[1]));
            Assert.That(clone.MultiProcessElements!.Elements[0].Curves[0], Is.Not.SameAs(state.Data.MultiProcessElements.Elements[0].Curves[0]));
            Assert.That(clone.MultiProcessElements.Elements[0].Curves[0].Segments[0].FormulaType, Is.EqualTo(2));
        });
    }

    [Test]
    public void DecoderRejectsMalformedSegmentedCurveData()
    {
        IccCurve curve = CreateCurve(
            CreateFormula(double.NegativeInfinity, 1, 2.0, 1.0, 0.0, 0.0, 0.0),
            CreateSampled(0.25, 0.2, 0.3),
            CreateFormula(0.75, 2, 1.0, 0.5, 0.25, 0.125, 0.0));
        byte[] valid = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, 1, CreateCurveSet(curve)));
        int elementOffset = (int)ReadUInt32(valid, 16);
        int curveOffset = elementOffset + (int)ReadUInt32(valid, elementOffset + 12);
        int firstSegmentOffset = curveOffset + 20;
        int sampledSegmentOffset = firstSegmentOffset + 32;

        byte[] noSegments = valid.ToArray();
        WriteUInt16(noSegments, curveOffset + 8, 0);
        byte[] descendingBreakPoints = valid.ToArray();
        WriteFloat32(descendingBreakPoints, curveOffset + 12, 0.75f);
        WriteFloat32(descendingBreakPoints, curveOffset + 16, 0.25f);
        byte[] duplicateBreakPoints = valid.ToArray();
        WriteFloat32(duplicateBreakPoints, curveOffset + 12, 0.25f);
        WriteFloat32(duplicateBreakPoints, curveOffset + 16, 0.25f);
        byte[] invalidFormula = valid.ToArray();
        WriteUInt16(invalidFormula, firstSegmentOffset + 8, 3);
        byte[] noSamples = valid.ToArray();
        WriteUInt32(noSamples, sampledSegmentOffset + 8, 0);
        byte[] nonFiniteBreakPoint = valid.ToArray();
        WriteUInt32(nonFiniteBreakPoint, curveOffset + 12, 0x7f800000);

        Assert.Multiple(() =>
        {
            AssertInvalid(noSegments, "at least one segment");
            AssertInvalid(descendingBreakPoints, "strictly increase");
            AssertInvalid(duplicateBreakPoints, "strictly increase");
            AssertInvalid(invalidFormula, "selector 3");
            AssertInvalid(noSamples, "at least one sample");
            AssertInvalid(nonFiniteBreakPoint, "finite normal float32Number");
        });
    }

    [Test]
    public void DecoderRejectsMalformedMatrixAndClutData()
    {
        byte[] matrixPayload = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, 1, CreateMatrix(1, 1, new[] { 1.0, 0.0 })));
        int matrixOffset = (int)ReadUInt32(matrixPayload, 16);
        WriteUInt32(matrixPayload, matrixOffset + 12, 0x7fc00000);

        byte[] shortMatrix = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(1, 1, CreateMatrix(1, 1, new[] { 1.0, 0.0 })));
        int shortMatrixOffset = (int)ReadUInt32(shortMatrix, 16);
        WriteUInt16(shortMatrix, shortMatrixOffset + 10, 2);

        IccProcessingElement clut = CreateClut(2, 1, new[] { 2, 3 }, new double[] { -1.0, 0.0, 0.5, 1.0, 2.0, 3.0 });
        byte[] clutGrid = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(2, 1, clut));
        int clutOffset = (int)ReadUInt32(clutGrid, 16);
        clutGrid[clutOffset + 12] = 1;
        byte[] clutReservedGrid = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(2, 1, clut));
        int clutReservedOffset = (int)ReadUInt32(clutReservedGrid, 16);
        clutReservedGrid[clutReservedOffset + 14] = 2;
        byte[] clutValue = MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(2, 1, clut));
        int clutValueOffset = (int)ReadUInt32(clutValue, 16);
        WriteUInt32(clutValue, clutValueOffset + 28, 1);

        Assert.Multiple(() =>
        {
            AssertInvalid(matrixPayload, "matrix coefficient");
            AssertInvalid(shortMatrix, "requires exactly");
            AssertInvalid(clutGrid, "grid dimension 0");
            AssertInvalid(clutReservedGrid, "unused CLUT grid-point bytes");
            AssertInvalid(clutValue, "CLUT value 0");
        });
    }

    [Test]
    public void WriterRejectsInvalidSegmentedCurveModels()
    {
        IccCurve finiteStart = CreateCurve(CreateFormula(0.0, 0, 1.0, 1.0, 0.0, 0.0));
        IccCurve sampledLast = CreateCurve(
            CreateFormula(double.NegativeInfinity, 0, 1.0, 1.0, 0.0, 0.0),
            CreateSampled(0.5, 0.25));
        IccCurve descending = CreateCurve(
            CreateFormula(double.NegativeInfinity, 0, 1.0, 1.0, 0.0, 0.0),
            CreateSampled(0.75, 0.25),
            CreateFormula(0.5, 0, 1.0, 1.0, 0.0, 0.0));
        IccCurve duplicate = CreateCurve(
            CreateFormula(double.NegativeInfinity, 0, 1.0, 1.0, 0.0, 0.0),
            CreateSampled(0.5, 0.25),
            CreateFormula(0.5, 0, 1.0, 1.0, 0.0, 0.0));
        IccCurve invalidSelector = CreateCurve(CreateFormula(double.NegativeInfinity, 3, 1.0, 1.0, 0.0, 0.0));
        IccCurve nonFiniteParameter = CreateCurve(CreateFormula(double.NegativeInfinity, 0, double.NaN, 1.0, 0.0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => SerializeCurve(finiteStart));
            Assert.Throws<IccProfileWriteException>(() => SerializeCurve(sampledLast));
            Assert.Throws<IccProfileWriteException>(() => SerializeCurve(descending));
            Assert.Throws<IccProfileWriteException>(() => SerializeCurve(duplicate));
            Assert.Throws<IccProfileWriteException>(() => SerializeCurve(invalidSelector));
            Assert.Throws<IccProfileWriteException>(() => SerializeCurve(nonFiniteParameter));
        });
    }

    [Test]
    public void WriterRejectsInvalidMatrixAndClutModels()
    {
        IccMultiProcessElements nullElement = new IccMultiProcessElements();
        nullElement.InputChannels = 1;
        nullElement.OutputChannels = 1;
        nullElement.Elements = new IccProcessingElement[1];
        IccProcessingElement badMatrixShape = CreateMatrix(2, 1, new[] { 1.0, 2.0, 3.0 });
        badMatrixShape.Matrix!.Columns = 2;
        IccProcessingElement badMatrixValue = CreateMatrix(1, 1, new[] { double.PositiveInfinity, 0.0 });
        IccProcessingElement badClutGrid = CreateClut(1, 1, new[] { 2 }, new[] { 0.0, 1.0 });
        badClutGrid.Clut!.GridPoints[0] = 1;
        IccProcessingElement badClutCount = CreateClut(1, 1, new[] { 2 }, new[] { 0.0, 1.0 });
        badClutCount.Clut!.Values = new[] { 0.0 };
        IccProcessingElement badClutValue = CreateClut(1, 1, new[] { 2 }, new[] { 0.0, double.NaN });

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => MultiProcessElementsTagCodec.SerializeMultiProcessElements(nullElement));
            Assert.Throws<IccProfileWriteException>(() => SerializeElement(2, 1, badMatrixShape));
            Assert.Throws<IccProfileWriteException>(() => SerializeElement(1, 1, badMatrixValue));
            Assert.Throws<IccProfileWriteException>(() => SerializeElement(1, 1, badClutGrid));
            Assert.Throws<IccProfileWriteException>(() => SerializeElement(1, 1, badClutCount));
            Assert.Throws<IccProfileWriteException>(() => SerializeElement(1, 1, badClutValue));
        });
    }

    private static byte[] SerializeCurve(IccCurve curve)
    {
        return SerializeElement(1, 1, CreateCurveSet(curve));
    }

    private static byte[] SerializeElement(int inputChannels, int outputChannels, IccProcessingElement element)
    {
        return MultiProcessElementsTagCodec.SerializeMultiProcessElements(CreatePipeline(inputChannels, outputChannels, element));
    }

    private static IccCurve CreateCurve(params IccCurveSegment[] segments)
    {
        IccCurve curve = new IccCurve();
        curve.Kind = IccCurveKind.Segmented;
        curve.Segments = segments;
        return curve;
    }

    private static IccCurveSegment CreateFormula(double breakPoint, int formulaType, params double[] parameters)
    {
        IccCurveSegment segment = new IccCurveSegment();
        segment.Kind = IccCurveSegmentKind.Formula;
        segment.FormulaType = formulaType;
        segment.BreakPoint = breakPoint;
        segment.Parameters = parameters;
        return segment;
    }

    private static IccCurveSegment CreateSampled(double breakPoint, params double[] samples)
    {
        IccCurveSegment segment = new IccCurveSegment();
        segment.Kind = IccCurveSegmentKind.Sampled;
        segment.BreakPoint = breakPoint;
        segment.Samples = samples;
        return segment;
    }

    private static IccProcessingElement CreateCurveSet(params IccCurve[] curves)
    {
        IccProcessingElement element = new IccProcessingElement();
        element.Signature = IccProcessingElementSignatures.CurveSet;
        element.Kind = IccProcessingElementKind.CurveSet;
        element.InputChannels = curves.Length;
        element.OutputChannels = curves.Length;
        element.Curves = curves;
        return element;
    }

    private static IccProcessingElement CreateMatrix(int inputChannels, int outputChannels, double[] values)
    {
        IccMatrix matrix = new IccMatrix();
        matrix.Rows = outputChannels;
        matrix.Columns = inputChannels + 1;
        matrix.Values = values;
        IccProcessingElement element = new IccProcessingElement();
        element.Signature = IccProcessingElementSignatures.Matrix;
        element.Kind = IccProcessingElementKind.Matrix;
        element.InputChannels = inputChannels;
        element.OutputChannels = outputChannels;
        element.Matrix = matrix;
        return element;
    }

    private static IccProcessingElement CreateClut(int inputChannels, int outputChannels, int[] gridPoints, double[] values)
    {
        IccClut clut = new IccClut();
        clut.InputChannels = inputChannels;
        clut.OutputChannels = outputChannels;
        clut.GridPoints = gridPoints;
        clut.Values = values;
        IccProcessingElement element = new IccProcessingElement();
        element.Signature = IccProcessingElementSignatures.Clut;
        element.Kind = IccProcessingElementKind.Clut;
        element.InputChannels = inputChannels;
        element.OutputChannels = outputChannels;
        element.Clut = clut;
        return element;
    }

    private static IccMultiProcessElements CreatePipeline(int inputChannels, int outputChannels, params IccProcessingElement[] elements)
    {
        IccMultiProcessElements pipeline = new IccMultiProcessElements();
        pipeline.InputChannels = inputChannels;
        pipeline.OutputChannels = outputChannels;
        pipeline.Elements = elements;
        return pipeline;
    }

    private static void AssertInvalid(byte[] payload, string message)
    {
        TagDecodeState state = DecodePayload(payload);
        Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
        Assert.That(state.Result.Message, Does.Contain(message));
    }

    private static TagDecodeState DecodePayload(byte[] payload)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, IccVersionKind.Version4, false);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static double ReadFloat32(byte[] source, int offset)
    {
        int bits = unchecked((int)ReadUInt32(source, offset));
        return BitConverter.Int32BitsToSingle(bits);
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

    private static void WriteFloat32(byte[] destination, int offset, float value)
    {
        WriteUInt32(destination, offset, unchecked((uint)BitConverter.SingleToInt32Bits(value)));
    }
}
