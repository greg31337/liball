using icc;

namespace icc.Tests;

public class AdaptiveGainCurveTagCodecTests
{
    [Test]
    public void RoundTripsHeaderMetadataAndSharedRgbNodeTables()
    {
        IccAdaptiveGainCurveNode[] shared = new[]
        {
            CreateNode(0.0, 0.0, 0.0),
            CreateNode(0.5, 0.25, 1.0),
            CreateNode(1.0, 1.0, 0.0)
        };
        IccAdaptiveGainCurve source = CreateCurve(shared, shared, new[]
        {
            CreateNode(0.0, 0.1, 0.5),
            CreateNode(1.0, 0.9, 0.5)
        });
        source.TargetHeadroom = 1.5;
        source.AlternateHeadroom = 3.0;
        source.FunctionData = Enumerable.Range(1, 52).Select(value => (byte)value).ToArray();
        source.BackwardCompatibleTargetHeadrooms = new[] { 1.0, 2.0, 4.0 };

        byte[] payload = AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(source, IccProfileClass.Display, IccColorSpaceKind.Rgb, false, false);
        TagDecodeState state = DecodePayload(payload, false, IccTagSignatures.AdaptiveGainCurve, IccProfileClass.Display, IccColorSpaceKind.Rgb, false, false);
        IccAdaptiveGainCurve decoded = state.Data.AdaptiveGainCurve!;
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);
        byte[] roundTrip = AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(decoded, IccProfileClass.Display, IccColorSpaceKind.Rgb, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(196));
            Assert.That(ReadUInt32(payload, 0), Is.EqualTo(IccTypeSignatures.AdaptiveGainCurve));
            Assert.That(ReadUInt32(payload, 8), Is.EqualTo(1));
            Assert.That(ReadUInt32(payload, 100), Is.EqualTo(128));
            Assert.That(ReadUInt32(payload, 104), Is.EqualTo(40));
            Assert.That(ReadUInt32(payload, 108), Is.EqualTo(128));
            Assert.That(ReadUInt32(payload, 112), Is.EqualTo(40));
            Assert.That(ReadUInt32(payload, 116), Is.EqualTo(168));
            Assert.That(ReadUInt32(payload, 120), Is.EqualTo(28));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Result.Message, Does.Contain("non-image-specific"));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.AdaptiveGainCurve));
            Assert.That(decoded.IsImageSpecific, Is.False);
            Assert.That(decoded.TargetHeadroom, Is.EqualTo(1.5));
            Assert.That(decoded.AlternateHeadroom, Is.EqualTo(3.0));
            Assert.That(decoded.FunctionData, Is.EqualTo(source.FunctionData));
            Assert.That(decoded.BackwardCompatibleTargetHeadrooms, Is.EqualTo(source.BackwardCompatibleTargetHeadrooms));
            Assert.That(decoded.RedNodes, Is.SameAs(decoded.GreenNodes));
            Assert.That(decoded.BlueNodes.Select(node => node.Gain), Is.EqualTo(new[] { 0.1, 0.9 }).Within(0.000001));
            Assert.That(roundTrip, Is.EqualTo(payload));
            Assert.That(clone.AdaptiveGainCurve!.FunctionData, Is.EqualTo(source.FunctionData));
            Assert.That(clone.AdaptiveGainCurve.FunctionData, Is.Not.SameAs(decoded.FunctionData));
            Assert.That(clone.AdaptiveGainCurve.BackwardCompatibleTargetHeadrooms, Is.EqualTo(source.BackwardCompatibleTargetHeadrooms));
            Assert.That(clone.AdaptiveGainCurve.RedNodes, Is.SameAs(clone.AdaptiveGainCurve.GreenNodes));
            Assert.That(clone.AdaptiveGainCurve.RedNodes, Is.Not.SameAs(decoded.RedNodes));
        });
    }

    [Test]
    public void EnforcesImageSpecificGuidProfileClassColourSpaceAndFlags()
    {
        IccAdaptiveGainCurve imageSpecific = CreateCurve(CreateLinearNodes(), CreateLinearNodes(), CreateLinearNodes());
        imageSpecific.Identifier[0] = 1;
        imageSpecific.Identifier[15] = 2;
        imageSpecific.IsImageSpecific = true;
        byte[] payload = AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(imageSpecific, IccProfileClass.Input, IccColorSpaceKind.Rgb, true, true);

        TagDecodeState valid = DecodePayload(payload, true, IccTagSignatures.AdaptiveGainCurve, IccProfileClass.Input, IccColorSpaceKind.Rgb, true, true);
        TagDecodeState wrongTag = DecodePayload(payload, true, IccTagSignatures.Cicp, IccProfileClass.Input, IccColorSpaceKind.Rgb, true, true);
        TagDecodeState wrongClass = DecodePayload(payload, true, IccTagSignatures.AdaptiveGainCurve, IccProfileClass.Output, IccColorSpaceKind.Rgb, true, true);
        TagDecodeState wrongSpace = DecodePayload(payload, true, IccTagSignatures.AdaptiveGainCurve, IccProfileClass.Input, IccColorSpaceKind.Cmyk, true, true);
        TagDecodeState missingEmbeddedFlag = DecodePayload(payload, true, IccTagSignatures.AdaptiveGainCurve, IccProfileClass.Input, IccColorSpaceKind.Rgb, false, true);
        TagDecodeState missingDependencyFlag = DecodePayload(payload, true, IccTagSignatures.AdaptiveGainCurve, IccProfileClass.Input, IccColorSpaceKind.Rgb, true, false);

        IccAdaptiveGainCurve inconsistentNonSpecific = CreateCurve(CreateLinearNodes(), CreateLinearNodes(), CreateLinearNodes());
        inconsistentNonSpecific.Identifier[0] = 1;
        IccAdaptiveGainCurve inconsistentSpecific = CreateCurve(CreateLinearNodes(), CreateLinearNodes(), CreateLinearNodes());
        inconsistentSpecific.IsImageSpecific = true;

        Assert.Multiple(() =>
        {
            Assert.That(valid.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(valid.Data.AdaptiveGainCurve!.IsImageSpecific, Is.True);
            Assert.That(valid.Data.AdaptiveGainCurve.Identifier, Is.EqualTo(imageSpecific.Identifier));
            AssertInvalid(wrongTag, "only for the ADGC tag");
            AssertInvalid(wrongClass, "Input or Display");
            AssertInvalid(wrongSpace, "RGB");
            AssertInvalid(missingEmbeddedFlag, "embedded");
            AssertInvalid(missingDependencyFlag, "dependent");
            Assert.Throws<IccProfileWriteException>(() => AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(imageSpecific, IccProfileClass.Output, IccColorSpaceKind.Rgb, true, true));
            Assert.Throws<IccProfileWriteException>(() => AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(imageSpecific, IccProfileClass.Input, IccColorSpaceKind.Cmyk, true, true));
            Assert.Throws<IccProfileWriteException>(() => AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(imageSpecific, IccProfileClass.Input, IccColorSpaceKind.Rgb, false, true));
            Assert.Throws<IccProfileWriteException>(() => AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(inconsistentNonSpecific, IccProfileClass.Display, IccColorSpaceKind.Rgb, false, false));
            Assert.Throws<IccProfileWriteException>(() => AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(inconsistentSpecific, IccProfileClass.Display, IccColorSpaceKind.Rgb, true, true));
        });
    }

    [Test]
    public void DecoderRejectsMalformedHeadersPositionsAndNodeTables()
    {
        byte[] valid = AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(CreateCurve(CreateLinearNodes(), CreateLinearNodes(), CreateLinearNodes()), IccProfileClass.Display, IccColorSpaceKind.Rgb, false, false);
        int curveOffset = (int)ReadUInt32(valid, 100);
        byte[] reserved = valid.ToArray();
        reserved[124] = 1;
        byte[] functionType = valid.ToArray();
        WriteUInt32(functionType, 8, 2);
        byte[] nonFiniteHeadroom = valid.ToArray();
        WriteUInt32(nonFiniteHeadroom, 28, 0x7f800000);
        byte[] nonFiniteFunctionMetadata = valid.ToArray();
        WriteUInt32(nonFiniteFunctionMetadata, 60, 0x7fc00000);
        byte[] headerPosition = valid.ToArray();
        WriteUInt32(headerPosition, 100, 124);
        byte[] oneNode = valid.ToArray();
        WriteUInt32(oneNode, curveOffset, 1);
        byte[] tooManyNodes = valid.ToArray();
        WriteUInt32(tooManyNodes, curveOffset, 33);
        byte[] nonFiniteNode = valid.ToArray();
        WriteUInt32(nonFiniteNode, curveOffset + 8, 0x7fc00000);
        byte[] duplicateInput = valid.ToArray();
        WriteUInt32(duplicateInput, curveOffset + 16, ReadUInt32(duplicateInput, curveOffset + 4));

        Assert.Multiple(() =>
        {
            AssertInvalid(DecodePayload(reserved), "reserved bytes");
            AssertInvalid(DecodePayload(functionType), "function type 2");
            AssertInvalid(DecodePayload(nonFiniteHeadroom), "header float values");
            AssertInvalid(DecodePayload(nonFiniteFunctionMetadata), "header float values");
            AssertInvalid(DecodePayload(headerPosition), "beyond the header");
            AssertInvalid(DecodePayload(oneNode), "between 2 and 32");
            AssertInvalid(DecodePayload(tooManyNodes), "between 2 and 32");
            AssertInvalid(DecodePayload(nonFiniteNode), "finite normal float32Number");
            AssertInvalid(DecodePayload(duplicateInput), "strictly increasing");
        });
    }

    [Test]
    public void WriterRejectsInvalidHeaderArraysAndNodeSequences()
    {
        IccAdaptiveGainCurve badIdentifier = CreateCurve(CreateLinearNodes(), CreateLinearNodes(), CreateLinearNodes());
        badIdentifier.Identifier = new byte[15];
        IccAdaptiveGainCurve badFunctionData = CreateCurve(CreateLinearNodes(), CreateLinearNodes(), CreateLinearNodes());
        badFunctionData.FunctionData = new byte[51];
        IccAdaptiveGainCurve badHeadrooms = CreateCurve(CreateLinearNodes(), CreateLinearNodes(), CreateLinearNodes());
        badHeadrooms.BackwardCompatibleTargetHeadrooms = new double[2];
        IccAdaptiveGainCurve badFunctionValue = CreateCurve(CreateLinearNodes(), CreateLinearNodes(), CreateLinearNodes());
        WriteUInt32(badFunctionValue.FunctionData, 24, 0x7f800000);
        IccAdaptiveGainCurve oneNode = CreateCurve(new[] { CreateNode(0.0, 0.0, 0.0) }, CreateLinearNodes(), CreateLinearNodes());
        IccAdaptiveGainCurve unordered = CreateCurve(new[] { CreateNode(0.5, 0.0, 0.0), CreateNode(0.5, 1.0, 0.0) }, CreateLinearNodes(), CreateLinearNodes());
        IccAdaptiveGainCurve nonFinite = CreateCurve(new[] { CreateNode(0.0, 0.0, 0.0), CreateNode(1.0, double.NaN, 0.0) }, CreateLinearNodes(), CreateLinearNodes());
        IccAdaptiveGainCurve nonFiniteHeadroom = CreateCurve(CreateLinearNodes(), CreateLinearNodes(), CreateLinearNodes());
        nonFiniteHeadroom.TargetHeadroom = double.PositiveInfinity;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => Serialize(badIdentifier));
            Assert.Throws<IccProfileWriteException>(() => Serialize(badFunctionData));
            Assert.Throws<IccProfileWriteException>(() => Serialize(badHeadrooms));
            Assert.Throws<IccProfileWriteException>(() => Serialize(badFunctionValue));
            Assert.Throws<IccProfileWriteException>(() => Serialize(oneNode));
            Assert.Throws<IccProfileWriteException>(() => Serialize(unordered));
            Assert.Throws<IccProfileWriteException>(() => Serialize(nonFinite));
            Assert.Throws<IccProfileWriteException>(() => Serialize(nonFiniteHeadroom));
        });
    }

    [Test]
    public void EvaluatesPiecewiseCubicHermiteCurvesAndClampsEndpoints()
    {
        IccAdaptiveGainCurveNode[] smoothStep = new[]
        {
            CreateNode(0.0, 0.0, 0.0),
            CreateNode(1.0, 1.0, 0.0)
        };
        IccAdaptiveGainCurveNode[] piecewise = new[]
        {
            CreateNode(0.0, 0.0, 1.0),
            CreateNode(0.5, 0.5, 1.0),
            CreateNode(1.0, 1.0, 1.0)
        };

        Assert.Multiple(() =>
        {
            Assert.That(AdaptiveGainCurveTagCodec.EvaluateCurve(smoothStep, -1.0), Is.Zero);
            Assert.That(AdaptiveGainCurveTagCodec.EvaluateCurve(smoothStep, 0.25), Is.EqualTo(0.15625).Within(0.0000001));
            Assert.That(AdaptiveGainCurveTagCodec.EvaluateCurve(smoothStep, 0.5), Is.EqualTo(0.5).Within(0.0000001));
            Assert.That(AdaptiveGainCurveTagCodec.EvaluateCurve(smoothStep, 0.75), Is.EqualTo(0.84375).Within(0.0000001));
            Assert.That(AdaptiveGainCurveTagCodec.EvaluateCurve(smoothStep, 2.0), Is.EqualTo(1.0));
            Assert.That(AdaptiveGainCurveTagCodec.EvaluateCurve(piecewise, 0.25), Is.EqualTo(0.25).Within(0.0000001));
            Assert.That(AdaptiveGainCurveTagCodec.EvaluateCurve(piecewise, 0.75), Is.EqualTo(0.75).Within(0.0000001));
            Assert.Throws<IccProfileValidationException>(() => AdaptiveGainCurveTagCodec.EvaluateCurve(new[] { CreateNode(0.0, 0.0, 0.0) }, 0.5));
            Assert.Throws<IccProfileValidationException>(() => AdaptiveGainCurveTagCodec.EvaluateCurve(smoothStep, double.NaN));
        });
    }

    [Test]
    public void VersionDispatchRestrictsAdaptiveGainCurveTypeToVersion4()
    {
        byte[] payload = Serialize(CreateCurve(CreateLinearNodes(), CreateLinearNodes(), CreateLinearNodes()));
        TagDecodeState version2 = DecodePayload(payload, IccVersionKind.Version2);
        TagDecodeState version4 = DecodePayload(payload, IccVersionKind.Version4);
        TagDecodeState version5 = DecodePayload(payload, IccVersionKind.Version5);

        Assert.Multiple(() =>
        {
            Assert.That(version2.Result.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(version4.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(version5.Result.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
        });
    }

    private static byte[] Serialize(IccAdaptiveGainCurve curve)
    {
        return AdaptiveGainCurveTagCodec.SerializeAdaptiveGainCurve(curve, IccProfileClass.Display, IccColorSpaceKind.Rgb, false, false);
    }

    private static IccAdaptiveGainCurve CreateCurve(IccAdaptiveGainCurveNode[] red, IccAdaptiveGainCurveNode[] green, IccAdaptiveGainCurveNode[] blue)
    {
        IccAdaptiveGainCurve curve = new IccAdaptiveGainCurve();
        curve.RedNodes = red;
        curve.GreenNodes = green;
        curve.BlueNodes = blue;
        return curve;
    }

    private static IccAdaptiveGainCurveNode[] CreateLinearNodes()
    {
        return new[] { CreateNode(0.0, 0.0, 1.0), CreateNode(1.0, 1.0, 1.0) };
    }

    private static IccAdaptiveGainCurveNode CreateNode(double input, double gain, double slope)
    {
        IccAdaptiveGainCurveNode node = new IccAdaptiveGainCurveNode();
        node.Input = input;
        node.Gain = gain;
        node.Slope = slope;
        return node;
    }

    private static void AssertInvalid(TagDecodeState state, string message)
    {
        Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
        Assert.That(state.Result.Message, Does.Contain(message));
    }

    private static TagDecodeState DecodePayload(byte[] payload)
    {
        return DecodePayload(payload, false, 0, IccProfileClass.Unknown, IccColorSpaceKind.Unknown, false, false);
    }

    private static TagDecodeState DecodePayload(byte[] payload, IccVersionKind version)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, version, true);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static TagDecodeState DecodePayload(byte[] payload, bool hasContext, uint tagSignature, IccProfileClass profileClass, IccColorSpaceKind dataColorSpace, bool isEmbedded, bool requiresEmbeddedData)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, IccVersionKind.Version4, true);
        state.HasProfileContext = hasContext;
        state.TagSignature = tagSignature;
        state.ProfileClass = profileClass;
        state.DataColorSpace = dataColorSpace;
        state.IsEmbedded = isEmbedded;
        state.RequiresEmbeddedData = requiresEmbeddedData;
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
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
}
