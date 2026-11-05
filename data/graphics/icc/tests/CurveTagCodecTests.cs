using icc;

namespace icc.Tests;

public class CurveTagCodecTests
{
    [TestCase(IccVersionKind.Version2)]
    [TestCase(IccVersionKind.Version4)]
    [TestCase(IccVersionKind.Version5)]
    public void CurveTypeDecodesIdentityInEverySupportedGeneration(IccVersionKind version)
    {
        IccCurve source = new IccCurve();
        byte[] payload = CurveTagCodec.SerializeCurve(source);

        TagDecodeState state = DecodePayload(payload, version, false);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(12));
            Assert.That(ReadUInt32(payload, 8), Is.Zero);
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Curve));
            Assert.That(state.Data.Curve!.Kind, Is.EqualTo(IccCurveKind.Identity));
            Assert.That(state.Data.Curve.Gamma, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void CurveTypeRoundTripsGammaAndSampledForms()
    {
        IccCurve gamma = new IccCurve();
        gamma.Kind = IccCurveKind.Gamma;
        gamma.Gamma = 2.2;
        IccCurve sampled = new IccCurve();
        sampled.Kind = IccCurveKind.Sampled;
        sampled.Samples = new[] { 0.0, 0.25, 1.0 };

        byte[] gammaPayload = CurveTagCodec.SerializeCurve(gamma);
        byte[] sampledPayload = CurveTagCodec.SerializeCurve(sampled);
        TagDecodeState gammaState = DecodePayload(gammaPayload, IccVersionKind.Version4, false);
        TagDecodeState sampledState = DecodePayload(sampledPayload, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(gammaPayload, 8), Is.EqualTo(1));
            Assert.That(gammaState.Data.Curve!.Kind, Is.EqualTo(IccCurveKind.Gamma));
            Assert.That(gammaState.Data.Curve.Gamma, Is.EqualTo(563.0 / 256.0));
            Assert.That(ReadUInt32(sampledPayload, 8), Is.EqualTo(3));
            Assert.That(sampledState.Data.Curve!.Kind, Is.EqualTo(IccCurveKind.Sampled));
            Assert.That(sampledState.Data.Curve.Samples[0], Is.Zero);
            Assert.That(sampledState.Data.Curve.Samples[1], Is.EqualTo(16384.0 / 65535.0));
            Assert.That(sampledState.Data.Curve.Samples[2], Is.EqualTo(1.0));
        });
    }

    [Test]
    public void CurveTypeAcceptsOnlyZeroFourByteAlignmentPadding()
    {
        IccCurve gamma = new IccCurve();
        gamma.Kind = IccCurveKind.Gamma;
        gamma.Gamma = 2.0;
        byte[] canonical = CurveTagCodec.SerializeCurve(gamma);
        byte[] zeroPadded = new byte[16];
        System.Array.Copy(canonical, zeroPadded, canonical.Length);
        byte[] nonzeroPadded = zeroPadded.ToArray();
        nonzeroPadded[15] = 1;

        TagDecodeState accepted = DecodePayload(zeroPadded,
            IccVersionKind.Version2, true);
        TagDecodeState rejected = DecodePayload(nonzeroPadded,
            IccVersionKind.Version2, true);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Result.Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(accepted.Data.Curve!.Kind,
                Is.EqualTo(IccCurveKind.Gamma));
            Assert.That(accepted.Data.Curve.Gamma, Is.EqualTo(2.0));
            Assert.That(accepted.Data.RawData.Bytes, Is.EqualTo(zeroPadded));
            Assert.That(rejected.Result.Status,
                Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(rejected.Result.Message,
                Does.Contain("zero bytes").And.Contain("alignment"));
        });
    }

    [Test]
    public void ParametricCurveRoundTripsEveryIccOneFunctionAndExactCount()
    {
        double[][] parameters = new double[][]
        {
            new[] { 2.0 },
            new[] { 2.0, 1.0, -0.5 },
            new[] { 2.0, 1.0, -0.5, 0.25 },
            new[] { 2.0, 1.0, 0.0, 0.5, 0.25 },
            new[] { 2.0, 1.0, 0.0, 0.5, 0.25, 0.125, 0.0625 }
        };
        int[] expectedCounts = new[] { 1, 3, 4, 5, 7 };

        int functionType = 0;
        while (functionType <= 4)
        {
            IccParametricCurve source = new IccParametricCurve();
            source.FunctionType = functionType;
            source.Parameters = parameters[functionType];
            byte[] payload = CurveTagCodec.SerializeParametricCurve(source);
            TagDecodeState state = DecodePayload(payload, IccVersionKind.Version4, false);

            Assert.Multiple(() =>
            {
                Assert.That(payload, Has.Length.EqualTo(12 + (expectedCounts[functionType] * 4)));
                Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
                Assert.That(state.Data.Curve!.Kind, Is.EqualTo(IccCurveKind.Parametric));
                Assert.That(state.Data.Curve.Parametric!.FunctionType, Is.EqualTo(functionType));
                Assert.That(state.Data.Curve.Parametric.Parameters, Is.EqualTo(parameters[functionType]));
            });

            functionType++;
        }
    }

    [Test]
    public void ParametricCurveMathUsesTheCorrectedIcc2022Equations()
    {
        IccParametricCurve function0 = CreateParametric(0, 2.0);
        IccParametricCurve function1 = CreateParametric(1, 2.0, 2.0, -0.5);
        IccParametricCurve function2 = CreateParametric(2, 2.0, 2.0, -0.5, 0.1);
        IccParametricCurve function3 = CreateParametric(3, 2.0, 1.0, 0.0, 0.5, 0.25);
        IccParametricCurve function4 = CreateParametric(4, 2.0, 1.0, 0.0, 0.5, 0.25, 0.1, 0.05);

        Assert.Multiple(() =>
        {
            Assert.That(CurveMath.EvaluateParametricCurve(function0, 0.5), Is.EqualTo(0.25));
            Assert.That(CurveMath.EvaluateParametricCurve(function1, 0.1), Is.Zero);
            Assert.That(CurveMath.EvaluateParametricCurve(function1, 0.5), Is.EqualTo(0.25));
            Assert.That(CurveMath.EvaluateParametricCurve(function2, 0.1), Is.EqualTo(0.1));
            Assert.That(CurveMath.EvaluateParametricCurve(function2, 0.5), Is.EqualTo(0.35).Within(1e-12));
            Assert.That(CurveMath.EvaluateParametricCurve(function3, 0.2), Is.EqualTo(0.1));
            Assert.That(CurveMath.EvaluateParametricCurve(function3, 0.5), Is.EqualTo(0.25));
            Assert.That(CurveMath.EvaluateParametricCurve(function4, 0.2), Is.EqualTo(0.15).Within(1e-12));
            Assert.That(CurveMath.EvaluateParametricCurve(function4, 0.5), Is.EqualTo(0.35).Within(1e-12));
            Assert.That(CurveMath.EvaluateParametricCurve(CreateParametric(0, 0.25), 0.0), Is.Zero);
            Assert.That(CurveMath.EvaluateParametricCurve(CreateParametric(0, 0.25), 2.0), Is.EqualTo(1.0));
        });
    }

    [Test]
    public void CurveCodecsRejectIncorrectSizesReservedBytesAndParameterCounts()
    {
        byte[] missingSample = new byte[14];
        WriteUInt32(missingSample, 0, IccTypeSignatures.Curve);
        WriteUInt32(missingSample, 8, 2);
        byte[] identityWithExtraData = new byte[14];
        WriteUInt32(identityWithExtraData, 0, IccTypeSignatures.Curve);

        byte[] validParametric = CurveTagCodec.SerializeParametricCurve(CreateParametric(0, 2.0));
        byte[] extraParameter = new byte[validParametric.Length + 4];
        System.Array.Copy(validParametric, extraParameter, validParametric.Length);
        byte[] reserved = validParametric.ToArray();
        reserved[10] = 1;

        TagDecodeState missingState = DecodePayload(missingSample, IccVersionKind.Version4, true);
        TagDecodeState extraCurveState = DecodePayload(identityWithExtraData, IccVersionKind.Version4, false);
        TagDecodeState parameterState = DecodePayload(extraParameter, IccVersionKind.Version4, false);
        TagDecodeState reservedState = DecodePayload(reserved, IccVersionKind.Version4, false);

        Assert.Multiple(() =>
        {
            Assert.That(missingState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(missingState.Result.Message, Does.Contain("requires exactly 16 bytes"));
            Assert.That(missingState.Data.RawData.Bytes, Is.EqualTo(missingSample));
            Assert.That(extraCurveState.Result.Message, Does.Contain("requires exactly 12 bytes"));
            Assert.That(parameterState.Result.Message, Does.Contain("exactly 1 parameters"));
            Assert.That(reservedState.Result.Message, Does.Contain("reserved bytes"));
        });
    }

    [Test]
    public void FutureIccMaxParametricFunctionFallsBackWhileIccOneRejectsIt()
    {
        byte[] payload = new byte[12];
        WriteUInt32(payload, 0, IccTypeSignatures.ParametricCurve);
        WriteUInt16(payload, 8, 5);

        TagDecodeState version4 = DecodePayload(payload, IccVersionKind.Version4, false);
        TagDecodeState version5 = DecodePayload(payload, IccVersionKind.Version5, false);

        Assert.Multiple(() =>
        {
            Assert.That(version4.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(version4.Result.Message, Does.Contain("0 through 4"));
            Assert.That(version5.Result.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(version5.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(version5.Data.RawData.Bytes, Is.EqualTo(payload));
        });
    }

    [Test]
    public void CurveWritersRejectInvalidRepresentationsAndValues()
    {
        IccCurve oneSample = new IccCurve();
        oneSample.Kind = IccCurveKind.Sampled;
        oneSample.Samples = new[] { 0.5 };
        IccCurve invalidSample = new IccCurve();
        invalidSample.Kind = IccCurveKind.Sampled;
        invalidSample.Samples = new[] { 0.0, 1.1 };
        IccParametricCurve wrongCount = CreateParametric(4, 1.0);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => CurveTagCodec.SerializeCurve(oneSample));
            Assert.Throws<IccProfileWriteException>(() => CurveTagCodec.SerializeCurve(invalidSample));
            Assert.Throws<IccProfileWriteException>(() => CurveTagCodec.SerializeParametricCurve(wrongCount));
            Assert.Throws<IccProfileValidationException>(() => CurveMath.EvaluateParametricCurve(wrongCount, 0.5));
        });
    }

    private static IccParametricCurve CreateParametric(int functionType, params double[] parameters)
    {
        IccParametricCurve curve = new IccParametricCurve();
        curve.FunctionType = functionType;
        curve.Parameters = parameters;
        return curve;
    }

    private static TagDecodeState DecodePayload(byte[] payload, IccVersionKind version, bool preserveOriginalBytes)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        TagDecodeState state = new TagDecodeState(source, 0, payload.Length, 0, version, preserveOriginalBytes);
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
