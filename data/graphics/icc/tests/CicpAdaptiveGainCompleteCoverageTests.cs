using icc;

namespace icc.Tests;

public class CicpAdaptiveGainCompleteCoverageTests
{
    [TestCase(IccProfileClass.Input, IccColorSpaceKind.Rgb, 0)]
    [TestCase(IccProfileClass.Display, IccColorSpaceKind.Rgb, 0)]
    [TestCase(IccProfileClass.Input, IccColorSpaceKind.Xyz, 0)]
    [TestCase(IccProfileClass.Display, IccColorSpaceKind.Xyz, 0)]
    [TestCase(IccProfileClass.Input, IccColorSpaceKind.YCbCr, 1)]
    [TestCase(IccProfileClass.Display, IccColorSpaceKind.YCbCr, 255)]
    public void CicpRoundTripsBoundaryCodePointsInEveryPermittedContext(
        IccProfileClass profileClass, IccColorSpaceKind colorSpace,
        int matrixCoefficients)
    {
        IccCicp source = new IccCicp();
        source.ColorPrimaries = 0;
        source.TransferCharacteristics = 255;
        source.MatrixCoefficients = matrixCoefficients;
        source.FullRange = true;

        byte[] encoded = CicpTagCodec.SerializeCicp(source, profileClass, colorSpace);
        TagDecodeState state = DecodeCicp(encoded, IccVersionKind.Version4,
            profileClass, colorSpace, false);
        IccCicp decoded = state.Data.Cicp!;
        byte[] writeback = CicpTagCodec.SerializeCicp(decoded, profileClass,
            colorSpace);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(decoded.ColorPrimaries, Is.Zero);
            Assert.That(decoded.TransferCharacteristics, Is.EqualTo(255));
            Assert.That(decoded.MatrixCoefficients, Is.EqualTo(matrixCoefficients));
            Assert.That(decoded.FullRange, Is.True);
            Assert.That(writeback, Is.EqualTo(encoded));
            Assert.That(CicpTagCodec.SerializeCicp(decoded, profileClass,
                colorSpace), Is.EqualTo(encoded));
        });
    }

    [TestCase(IccVersionKind.Version4, IccProfileClass.Input,
        IccColorSpaceKind.Rgb, 2, true, true)]
    [TestCase(IccVersionKind.Version4, IccProfileClass.Display,
        IccColorSpaceKind.Rgb, 2, true, true)]
    [TestCase(IccVersionKind.Version4, IccProfileClass.Display,
        IccColorSpaceKind.Rgb, 2, false, false)]
    [TestCase(IccVersionKind.Version4, IccProfileClass.Display,
        IccColorSpaceKind.Rgb, 1, true, false)]
    [TestCase(IccVersionKind.Version5, IccProfileClass.Display,
        IccColorSpaceKind.Rgb, 2, true, false)]
    [TestCase(IccVersionKind.Version4, IccProfileClass.Input,
        IccColorSpaceKind.Xyz, 2, true, false)]
    [TestCase(IccVersionKind.Version4, IccProfileClass.Input,
        IccColorSpaceKind.YCbCr, 2, true, false)]
    public void CicpAmendmentResolvesUnspecifiedPrimariesOnlyFromV4RgbMatrixTags(
        IccVersionKind version, IccProfileClass profileClass,
        IccColorSpaceKind colorSpace, int primaries, bool hasMatrixPrimaries,
        bool expected)
    {
        int matrixCoefficients = colorSpace == IccColorSpaceKind.YCbCr ? 9 : 0;
        byte[] encoded = CreateCicpPayload(primaries, 16,
            matrixCoefficients, false);

        TagDecodeState state = DecodeCicp(encoded, version, profileClass,
            colorSpace, hasMatrixPrimaries);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Cicp!.UsesProfilePrimaries,
                Is.EqualTo(expected));
            Assert.That(state.Result.Message.Contains("matrix columns"),
                Is.EqualTo(expected));
        });
    }

    [Test]
    public void CicpRejectsOutOfRangeModelsAndEveryForbiddenContext()
    {
        IccCicp negativePrimaries = ValidCicp();
        negativePrimaries.ColorPrimaries = -1;
        IccCicp largeTransfer = ValidCicp();
        largeTransfer.TransferCharacteristics = 256;
        IccCicp largeMatrix = ValidCicp();
        largeMatrix.MatrixCoefficients = 256;
        byte[] rgbMatrix = CreateCicpPayload(1, 1, 9, false);
        byte[] yCbCrMatrix = CreateCicpPayload(1, 1, 0, false);
        byte[] ordinary = CreateCicpPayload(1, 1, 0, false);

        TagDecodeState wrongClass = DecodeCicp(ordinary,
            IccVersionKind.Version4, IccProfileClass.Output,
            IccColorSpaceKind.Rgb, false);
        TagDecodeState wrongSpace = DecodeCicp(ordinary,
            IccVersionKind.Version4, IccProfileClass.Display,
            IccColorSpaceKind.Cmyk, false);
        TagDecodeState badRgbMatrix = DecodeCicp(rgbMatrix,
            IccVersionKind.Version4, IccProfileClass.Display,
            IccColorSpaceKind.Rgb, false);
        TagDecodeState badYcbcrMatrix = DecodeCicp(yCbCrMatrix,
            IccVersionKind.Version4, IccProfileClass.Display,
            IccColorSpaceKind.YCbCr, false);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() =>
                CicpTagCodec.SerializeCicp(null!, IccProfileClass.Display,
                    IccColorSpaceKind.Rgb));
            Assert.Throws<IccProfileWriteException>(() =>
                CicpTagCodec.SerializeCicp(negativePrimaries,
                    IccProfileClass.Display, IccColorSpaceKind.Rgb));
            Assert.Throws<IccProfileWriteException>(() =>
                CicpTagCodec.SerializeCicp(largeTransfer,
                    IccProfileClass.Display, IccColorSpaceKind.Rgb));
            Assert.Throws<IccProfileWriteException>(() =>
                CicpTagCodec.SerializeCicp(largeMatrix,
                    IccProfileClass.Display, IccColorSpaceKind.Rgb));
            AssertInvalid(wrongClass, "Input or Display");
            AssertInvalid(wrongSpace, "RGB, YCbCr, or XYZ");
            AssertInvalid(badRgbMatrix, "MatrixCoefficients to be zero");
            AssertInvalid(badYcbcrMatrix, "MatrixCoefficients to be non-zero");
        });
    }

    [Test]
    public void AdaptiveGainMaximumNodesHeadroomsAndApproximationMetadataRoundTrip()
    {
        IccAdaptiveGainCurveNode[] red = LinearNodes(32, 0.0);
        IccAdaptiveGainCurveNode[] green = LinearNodes(32, 0.0);
        IccAdaptiveGainCurveNode[] blue = LinearNodes(2, 0.25);
        IccAdaptiveGainCurve source = Curve(red, green, blue);
        source.TargetHeadroom = float.MaxValue;
        source.AlternateHeadroom = -float.MaxValue;
        source.BackwardCompatibleTargetHeadrooms = new[] { 0.0, 1.0, -1.0 };
        source.FunctionData = FunctionAndApproximationMetadata();

        byte[] encoded = SerializeAdaptive(source);
        TagDecodeState state = DecodeAdaptive(encoded, false,
            IccProfileClass.Display, false, false);
        IccAdaptiveGainCurve decoded = state.Data.AdaptiveGainCurve!;
        IccTagData cloneData = ProfileClone.CloneTagDataStandalone(state.Data);
        IccAdaptiveGainCurve clone = cloneData.AdaptiveGainCurve!;

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(decoded.RedNodes, Has.Length.EqualTo(32));
            Assert.That(decoded.BlueNodes, Has.Length.EqualTo(2));
            Assert.That(ReadUInt32(encoded, 100), Is.EqualTo(ReadUInt32(encoded, 108)));
            Assert.That(ReadUInt32(encoded, 104), Is.EqualTo(ReadUInt32(encoded, 112)));
            Assert.That(decoded.RedNodes, Is.SameAs(decoded.GreenNodes));
            Assert.That(clone.RedNodes, Is.SameAs(clone.GreenNodes));
            Assert.That(clone.RedNodes, Is.Not.SameAs(decoded.RedNodes));
            Assert.That(decoded.TargetHeadroom, Is.EqualTo((double)float.MaxValue));
            Assert.That(decoded.AlternateHeadroom, Is.EqualTo((double)-float.MaxValue));
            Assert.That(decoded.BackwardCompatibleTargetHeadrooms,
                Is.EqualTo(new[] { 0.0, 1.0, -1.0 }));
            Assert.That(decoded.FunctionData, Is.EqualTo(source.FunctionData));
            Assert.That(SerializeAdaptive(decoded), Is.EqualTo(encoded));
            Assert.That(SerializeAdaptive(decoded), Is.EqualTo(encoded));
        });
    }

    [Test]
    public void AdaptiveGainPublicEvaluationMatchesIndependentCubicHermiteValues()
    {
        IccAdaptiveGainCurveNode[] red = new[]
        {
            Node(0.0, 0.1, 0.5),
            Node(0.4, 0.7, -0.25),
            Node(1.0, 0.9, 0.75)
        };
        IccAdaptiveGainCurveNode[] green = new[]
        {
            Node(0.0, 0.2, 1.0),
            Node(1.0, 0.8, -0.5)
        };
        IccAdaptiveGainCurveNode[] blue = new[]
        {
            Node(0.2, 0.3, 2.0),
            Node(0.8, 0.6, 0.0)
        };
        IccAdaptiveGainCurve curve = Curve(red, green, blue);
        IccToneCurveEvaluator evaluator = new IccToneCurveEvaluator();

        double[] actual = evaluator.EvaluateAdaptiveGainCurve(curve,
            new[] { 0.25, 0.75, -1.0 });
        double[] expected = new[]
        {
            Hermite(red[0], red[1], 0.25),
            Hermite(green[0], green[1], 0.75),
            blue[0].Gain
        };
        double[] upper = evaluator.EvaluateAdaptiveGainCurve(curve,
            new[] { 2.0, 2.0, 2.0 });

        IccAdaptiveGainCurveNode[] overflowing = new[]
        {
            Node(-double.MaxValue, 0.0, 1.0),
            Node(double.MaxValue, 1.0, 1.0)
        };
        IccAdaptiveGainCurve bad = Curve(overflowing, green, blue);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected).Within(1e-14));
            Assert.That(upper, Is.EqualTo(new[] { 0.9, 0.8, 0.6 })
                .Within(1e-14));
            Assert.Throws<IccProfileValidationException>(() =>
                evaluator.EvaluateAdaptiveGainCurve(bad,
                    new[] { 0.0, 0.5, 0.5 }));
        });
    }

    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    public void ImageSpecificAdaptiveGainRequiresBothProfileFlags(
        bool embedded, bool dependent, bool accepted)
    {
        IccAdaptiveGainCurve curve = Curve(LinearNodes(2, 0.0),
            LinearNodes(2, 0.0), LinearNodes(2, 0.0));
        curve.Identifier[0] = 0x12;
        curve.Identifier[15] = 0x34;
        curve.IsImageSpecific = true;

        if (!accepted)
        {
            Assert.Throws<IccProfileWriteException>(() =>
                AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(curve,
                    IccProfileClass.Input, IccColorSpaceKind.Rgb,
                    embedded, dependent));
            return;
        }

        byte[] encoded = AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(
            curve, IccProfileClass.Input, IccColorSpaceKind.Rgb,
            embedded, dependent);
        TagDecodeState state = DecodeAdaptive(encoded, true,
            IccProfileClass.Input, embedded, dependent);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.AdaptiveGainCurve!.IsImageSpecific, Is.True);
            Assert.That(state.Data.AdaptiveGainCurve.Identifier,
                Is.EqualTo(curve.Identifier));
        });
    }

    [Test]
    public void NonImageSpecificAdaptiveGainIsIndependentOfEmbeddingFlags()
    {
        IccAdaptiveGainCurve curve = Curve(LinearNodes(2, 0.0),
            LinearNodes(2, 0.0), LinearNodes(2, 0.0));
        byte[] encoded = AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(
            curve, IccProfileClass.Display, IccColorSpaceKind.Rgb, true, true);

        TagDecodeState state = DecodeAdaptive(encoded, true,
            IccProfileClass.Display, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.AdaptiveGainCurve!.IsImageSpecific, Is.False);
            Assert.That(state.Result.Message, Does.Contain("non-image-specific"));
        });
    }

    [Test]
    public void AdaptiveGainDecoderRejectsUnsafePositionTopologiesAndWrongCurveSize()
    {
        IccAdaptiveGainCurve source = Curve(LinearNodes(2, 0.0),
            LinearNodes(3, 0.1), LinearNodes(4, 0.2));
        byte[] valid = SerializeAdaptive(source);
        int redOffset = (int)ReadUInt32(valid, 100);

        byte[] shortPayload = valid.Take(155).ToArray();
        byte[] hugePosition = valid.ToArray();
        WriteUInt32(hugePosition, 100, uint.MaxValue);
        byte[] overlap = valid.ToArray();
        WriteUInt32(overlap, 108, (uint)(redOffset + 4));
        byte[] conflictingShare = valid.ToArray();
        WriteUInt32(conflictingShare, 108, (uint)redOffset);
        byte[] trailing = valid.Concat(new byte[4]).ToArray();
        byte[] wrongCurveSize = valid.ToArray();
        WriteUInt32(wrongCurveSize, redOffset, 3);

        Assert.Multiple(() =>
        {
            AssertInvalid(DecodeAdaptiveWithoutContext(shortPayload),
                "requires a 128-byte header");
            AssertInvalid(DecodeAdaptiveWithoutContext(hugePosition),
                "supported managed range");
            AssertInvalid(DecodeAdaptiveWithoutContext(overlap),
                "overlap illegally");
            AssertInvalid(DecodeAdaptiveWithoutContext(conflictingShare),
                "same element size");
            AssertInvalid(DecodeAdaptiveWithoutContext(trailing),
                "trailing or unreferenced data");
            AssertInvalid(DecodeAdaptiveWithoutContext(wrongCurveSize),
                "require exactly");
        });
    }

    [Test]
    public void AdaptiveGainWriterRejectsNullAndSubnormalData()
    {
        IccAdaptiveGainCurve subnormalHeadroom = Curve(LinearNodes(2, 0.0),
            LinearNodes(2, 0.0), LinearNodes(2, 0.0));
        subnormalHeadroom.TargetHeadroom = float.Epsilon;
        IccAdaptiveGainCurve subnormalMetadata = Curve(LinearNodes(2, 0.0),
            LinearNodes(2, 0.0), LinearNodes(2, 0.0));
        IndependentBigEndian.WriteUInt32(subnormalMetadata.FunctionData, 0, 1);
        IccAdaptiveGainCurve subnormalNode = Curve(new[]
        {
            Node(0.0, 0.0, 0.0),
            Node(1.0, float.Epsilon, 0.0)
        }, LinearNodes(2, 0.0), LinearNodes(2, 0.0));

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() =>
                AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(null!,
                    IccProfileClass.Display, IccColorSpaceKind.Rgb,
                    false, false));
            Assert.Throws<IccProfileWriteException>(() =>
                SerializeAdaptive(subnormalHeadroom));
            Assert.Throws<IccProfileWriteException>(() =>
                SerializeAdaptive(subnormalMetadata));
            Assert.Throws<IccProfileWriteException>(() =>
                SerializeAdaptive(subnormalNode));
        });
    }

    private static IccCicp ValidCicp()
    {
        IccCicp cicp = new IccCicp();
        cicp.ColorPrimaries = 1;
        cicp.TransferCharacteristics = 1;
        cicp.MatrixCoefficients = 0;
        return cicp;
    }

    private static byte[] CreateCicpPayload(int primaries, int transfer,
        int matrix, bool fullRange)
    {
        byte[] payload = IccBinaryFixture.CreateTagPayload(
            IccTypeSignatures.Cicp, 12);
        payload[8] = (byte)primaries;
        payload[9] = (byte)transfer;
        payload[10] = (byte)matrix;
        payload[11] = fullRange ? (byte)1 : (byte)0;
        return payload;
    }

    private static TagDecodeState DecodeCicp(byte[] payload,
        IccVersionKind version, IccProfileClass profileClass,
        IccColorSpaceKind colorSpace, bool hasMatrixPrimaries)
    {
        TagDecodeState state = State(payload, version);
        state.HasProfileContext = true;
        state.TagSignature = IccTagSignatures.Cicp;
        state.ProfileClass = profileClass;
        state.DataColorSpace = colorSpace;
        state.HasMatrixPrimaries = hasMatrixPrimaries;
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static IccAdaptiveGainCurve Curve(
        IccAdaptiveGainCurveNode[] red, IccAdaptiveGainCurveNode[] green,
        IccAdaptiveGainCurveNode[] blue)
    {
        IccAdaptiveGainCurve curve = new IccAdaptiveGainCurve();
        curve.RedNodes = red;
        curve.GreenNodes = green;
        curve.BlueNodes = blue;
        return curve;
    }

    private static IccAdaptiveGainCurveNode[] LinearNodes(int count,
        double gainOffset)
    {
        IccAdaptiveGainCurveNode[] nodes = new IccAdaptiveGainCurveNode[count];
        int index = 0;
        while (index < count)
        {
            double input = (double)index / (count - 1);
            nodes[index] = Node(input, input + gainOffset, 1.0);
            index++;
        }

        return nodes;
    }

    private static IccAdaptiveGainCurveNode Node(double input, double gain,
        double slope)
    {
        IccAdaptiveGainCurveNode node = new IccAdaptiveGainCurveNode();
        node.Input = input;
        node.Gain = gain;
        node.Slope = slope;
        return node;
    }

    private static byte[] FunctionAndApproximationMetadata()
    {
        byte[] metadata = new byte[52];
        int index = 0;
        while (index < 9)
        {
            IndependentBigEndian.WriteSingle(metadata, index * 4,
                (index + 1) * 0.125f);
            index++;
        }

        IndependentBigEndian.WriteUInt32(metadata, 36, 0x01020304);
        IndependentBigEndian.WriteUInt32(metadata, 40, 0x05060708);
        IndependentBigEndian.WriteUInt32(metadata, 44, 0x10203040);
        IndependentBigEndian.WriteUInt32(metadata, 48, 0x50607080);
        return metadata;
    }

    private static double Hermite(IccAdaptiveGainCurveNode first,
        IccAdaptiveGainCurveNode second, double input)
    {
        double width = second.Input - first.Input;
        double t = (input - first.Input) / width;
        double t2 = t * t;
        double t3 = t2 * t;
        return (((2.0 * t3) - (3.0 * t2) + 1.0) * first.Gain)
            + ((t3 - (2.0 * t2) + t) * width * first.Slope)
            + (((-2.0 * t3) + (3.0 * t2)) * second.Gain)
            + ((t3 - t2) * width * second.Slope);
    }

    private static byte[] SerializeAdaptive(IccAdaptiveGainCurve curve)
    {
        return AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(curve,
            IccProfileClass.Display, IccColorSpaceKind.Rgb, false, false);
    }

    private static TagDecodeState DecodeAdaptive(byte[] payload,
        bool hasContext, IccProfileClass profileClass, bool embedded,
        bool dependent)
    {
        TagDecodeState state = State(payload, IccVersionKind.Version4);
        state.HasProfileContext = hasContext;
        state.TagSignature = IccTagSignatures.AdaptiveGainCurve;
        state.ProfileClass = profileClass;
        state.DataColorSpace = IccColorSpaceKind.Rgb;
        state.IsEmbedded = embedded;
        state.RequiresEmbeddedData = dependent;
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static TagDecodeState DecodeAdaptiveWithoutContext(byte[] payload)
    {
        return DecodeAdaptive(payload, false, IccProfileClass.Unknown,
            false, false);
    }

    private static TagDecodeState State(byte[] payload, IccVersionKind version)
    {
        return new TagDecodeState(new TagDecodeSource(payload), 0,
            payload.Length, 0, version, true);
    }

    private static void AssertInvalid(TagDecodeState state, string message)
    {
        Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
        Assert.That(state.Result.Message, Does.Contain(message).IgnoreCase);
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
        IndependentBigEndian.WriteUInt32(destination, offset, value);
    }
}
