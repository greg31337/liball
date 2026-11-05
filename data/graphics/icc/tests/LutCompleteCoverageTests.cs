using icc;

namespace icc.Tests;

public class LutCompleteCoverageTests
{
    [TestCase(true, false, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    [TestCase(true, true, true)]
    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, true)]
    [TestCase(false, true, true)]
    public void EveryLegalMultiStageTopologyExecutesInItsDefinedOrder(bool aToB,
        bool hasMatrix, bool hasClut)
    {
        IccMultiStageLut source = CreateTopologyLut(hasMatrix, hasClut);
        byte[] payload = aToB
            ? MultiStageLutTagCodec.SerializeLutAToB(source)
            : MultiStageLutTagCodec.SerializeLutBToA(source);
        uint tagSignature = aToB ? IccTagSignatures.AToB0
            : IccTagSignatures.BToA0;
        TagDecodeState state = Decode(payload, IccVersionKind.Version4,
            tagSignature, IccColorSpaceSignatures.ThreeColor,
            IccPcsKind.Xyz);
        IccProfile profile = CreateProfile(IccVersionKind.Version4,
            IccColorSpaceSignatures.ThreeColor, IccPcsKind.Xyz);
        double[] normalized = new[] { 0.2, 0.4, 0.6 };
        double[] input = aToB ? normalized : DenormalizePcs(
            normalized, IccPcsKind.Xyz, IccPcsEncodingKind.Version4);

        double[] actual = CompileAndExecute(profile,
            CreateTag(tagSignature, state.Data),
            aToB ? IccTransformDirection.DeviceToPcs
                : IccTransformDirection.PcsToDevice,
            IccInterpolationKind.Tetrahedral, input);
        double[] expected = aToB ? DenormalizePcs(normalized,
            IccPcsKind.Xyz, IccPcsEncodingKind.Version4) : normalized;

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(actual, Is.EqualTo(expected).Within(1e-12));
            Assert.That(state.Data.MultiStageLut!.Matrix != null,
                Is.EqualTo(hasMatrix));
            Assert.That(state.Data.MultiStageLut.Clut != null,
                Is.EqualTo(hasClut));
        });
    }

    [TestCaseSource(nameof(ChannelCases))]
    public void EveryLutFamilyRoundTripsAndExecutesEveryDeviceChannelCount(
        int deviceChannels, uint dataColorSpace, IccPcsKind pcsKind)
    {
        IccProfile versionTwo = CreateProfile(IccVersionKind.Version2,
            dataColorSpace, pcsKind);
        IccProfile versionFour = CreateProfile(IccVersionKind.Version4,
            dataColorSpace, pcsKind);
        double[] encodedPcs = new[] { 0.25, 0.5, 0.75 };
        double[] deviceOutput = CreateDeviceValues(deviceChannels);
        double[] deviceInput = Enumerable.Repeat(0.375, deviceChannels).ToArray();
        double[] pcsInput = CreatePcsInput(pcsKind);

        IccLut lut8Forward = CreateClassicLut(deviceChannels, 3,
            IccPcsEncodingKind.Normalized, encodedPcs, true);
        IccLut lut8Reverse = CreateClassicLut(3, deviceChannels,
            IccPcsEncodingKind.Normalized, deviceOutput, true);
        IccLut lut16Forward = CreateClassicLut(deviceChannels, 3,
            IccPcsEncodingKind.Version2, encodedPcs, false);
        IccLut lut16Reverse = CreateClassicLut(3, deviceChannels,
            IccPcsEncodingKind.Version2, deviceOutput, false);
        IccMultiStageLut mAb = CreateMultiStageLut(deviceChannels, 3,
            encodedPcs, true);
        IccMultiStageLut mBa = CreateMultiStageLut(3, deviceChannels,
            deviceOutput, false);

        byte[] lut8ForwardBytes = LutTagCodec.SerializeLut8(lut8Forward);
        byte[] lut8ReverseBytes = LutTagCodec.SerializeLut8(lut8Reverse);
        byte[] lut16ForwardBytes = LutTagCodec.SerializeLut16(lut16Forward);
        byte[] lut16ReverseBytes = LutTagCodec.SerializeLut16(lut16Reverse);
        byte[] mAbBytes = MultiStageLutTagCodec.SerializeLutAToB(mAb);
        byte[] mBaBytes = MultiStageLutTagCodec.SerializeLutBToA(mBa);

        TagDecodeState decoded8Forward = Decode(lut8ForwardBytes,
            IccVersionKind.Version2, IccTagSignatures.AToB0,
            dataColorSpace, pcsKind);
        TagDecodeState decoded8Reverse = Decode(lut8ReverseBytes,
            IccVersionKind.Version2, IccTagSignatures.BToA0,
            dataColorSpace, pcsKind);
        TagDecodeState decoded16Forward = Decode(lut16ForwardBytes,
            IccVersionKind.Version2, IccTagSignatures.AToB0,
            dataColorSpace, pcsKind);
        TagDecodeState decoded16Reverse = Decode(lut16ReverseBytes,
            IccVersionKind.Version2, IccTagSignatures.BToA0,
            dataColorSpace, pcsKind);
        TagDecodeState decodedMAb = Decode(mAbBytes,
            IccVersionKind.Version4, IccTagSignatures.AToB0,
            dataColorSpace, pcsKind);
        TagDecodeState decodedMBa = Decode(mBaBytes,
            IccVersionKind.Version4, IccTagSignatures.BToA0,
            dataColorSpace, pcsKind);

        IccInterpolationKind forwardInterpolation = deviceChannels == 1
            ? IccInterpolationKind.Linear
            : deviceChannels == 3
                ? IccInterpolationKind.Tetrahedral
                : IccInterpolationKind.Multilinear;
        double[] output8Forward = CompileAndExecute(versionTwo,
            CreateTag(IccTagSignatures.AToB0, decoded8Forward.Data),
            IccTransformDirection.DeviceToPcs, forwardInterpolation,
            deviceInput);
        double[] output8Reverse = CompileAndExecute(versionTwo,
            CreateTag(IccTagSignatures.BToA0, decoded8Reverse.Data),
            IccTransformDirection.PcsToDevice,
            IccInterpolationKind.Tetrahedral, pcsInput);
        double[] output16Forward = CompileAndExecute(versionTwo,
            CreateTag(IccTagSignatures.AToB0, decoded16Forward.Data),
            IccTransformDirection.DeviceToPcs, forwardInterpolation,
            deviceInput);
        double[] output16Reverse = CompileAndExecute(versionTwo,
            CreateTag(IccTagSignatures.BToA0, decoded16Reverse.Data),
            IccTransformDirection.PcsToDevice,
            IccInterpolationKind.Tetrahedral, pcsInput);
        double[] outputMAb = CompileAndExecute(versionFour,
            CreateTag(IccTagSignatures.AToB0, decodedMAb.Data),
            IccTransformDirection.DeviceToPcs, forwardInterpolation,
            deviceInput);
        double[] outputMBa = CompileAndExecute(versionFour,
            CreateTag(IccTagSignatures.BToA0, decodedMBa.Data),
            IccTransformDirection.PcsToDevice,
            IccInterpolationKind.Tetrahedral, pcsInput);

        double[] quantizedPcs8 = Quantize(encodedPcs, 255.0);
        double[] quantizedPcs16 = Quantize(encodedPcs, 65535.0);
        double[] quantizedDevice8 = Quantize(deviceOutput, 255.0);
        double[] quantizedDevice16 = Quantize(deviceOutput, 65535.0);
        double[] expected16Pcs = DenormalizePcs(quantizedPcs16,
            pcsKind, IccPcsEncodingKind.Version2);
        double[] expectedMAbPcs = DenormalizePcs(quantizedPcs16,
            pcsKind, IccPcsEncodingKind.Version4);

        Assert.Multiple(() =>
        {
            Assert.That(AllSuccessful(decoded8Forward, decoded8Reverse,
                decoded16Forward, decoded16Reverse, decodedMAb, decodedMBa),
                Is.True);
            Assert.That(decoded8Forward.Data.Lut!.InputChannels,
                Is.EqualTo(deviceChannels));
            Assert.That(decoded8Reverse.Data.Lut!.OutputChannels,
                Is.EqualTo(deviceChannels));
            Assert.That(decodedMAb.Data.MultiStageLut!.InputChannels,
                Is.EqualTo(deviceChannels));
            Assert.That(decodedMBa.Data.MultiStageLut!.OutputChannels,
                Is.EqualTo(deviceChannels));
            Assert.That(LutTagCodec.SerializeLut8(decoded8Forward.Data.Lut),
                Is.EqualTo(lut8ForwardBytes));
            Assert.That(LutTagCodec.SerializeLut8(decoded8Reverse.Data.Lut),
                Is.EqualTo(lut8ReverseBytes));
            Assert.That(LutTagCodec.SerializeLut16(decoded16Forward.Data.Lut!),
                Is.EqualTo(lut16ForwardBytes));
            Assert.That(LutTagCodec.SerializeLut16(decoded16Reverse.Data.Lut!),
                Is.EqualTo(lut16ReverseBytes));
            Assert.That(MultiStageLutTagCodec.SerializeLutAToB(
                decodedMAb.Data.MultiStageLut), Is.EqualTo(mAbBytes));
            Assert.That(MultiStageLutTagCodec.SerializeLutBToA(
                decodedMBa.Data.MultiStageLut), Is.EqualTo(mBaBytes));
            Assert.That(output8Forward, Is.EqualTo(quantizedPcs8).Within(1e-12));
            Assert.That(output8Reverse, Is.EqualTo(quantizedDevice8).Within(1e-12));
            Assert.That(output16Forward, Is.EqualTo(expected16Pcs).Within(1e-10));
            Assert.That(output16Reverse, Is.EqualTo(quantizedDevice16).Within(1e-12));
            Assert.That(outputMAb, Is.EqualTo(expectedMAbPcs).Within(1e-10));
            Assert.That(outputMBa, Is.EqualTo(quantizedDevice16).Within(1e-12));
        });
    }

    [TestCase(IccPcsKind.Lab)]
    [TestCase(IccPcsKind.Xyz)]
    public void PipelineExecutionClipsEveryLutFamilyAtBothInterpolationBoundaries(
        IccPcsKind pcsKind)
    {
        IccProfile versionTwo = CreateProfile(IccVersionKind.Version2,
            IccColorSpaceSignatures.OneColor, pcsKind);
        IccProfile versionFour = CreateProfile(IccVersionKind.Version4,
            IccColorSpaceSignatures.OneColor, pcsKind);
        IccLut lut8 = CreateClassicLut(1, 3,
            IccPcsEncodingKind.Normalized, new double[3], true);
        IccLut lut16 = CreateClassicLut(1, 3,
            IccPcsEncodingKind.Version2, new double[3], false);
        IccMultiStageLut mAb = CreateMultiStageLut(1, 3, new double[3], true);
        SetBoundaryClut(lut8.Clut);
        SetBoundaryClut(lut16.Clut);
        SetBoundaryClut(mAb.Clut!);

        IccTag tag8 = DecodeTag(LutTagCodec.SerializeLut8(lut8),
            IccVersionKind.Version2, IccTagSignatures.AToB0,
            IccColorSpaceSignatures.OneColor, pcsKind);
        IccTag tag16 = DecodeTag(LutTagCodec.SerializeLut16(lut16),
            IccVersionKind.Version2, IccTagSignatures.AToB0,
            IccColorSpaceSignatures.OneColor, pcsKind);
        IccTag tagMAb = DecodeTag(MultiStageLutTagCodec.SerializeLutAToB(mAb),
            IccVersionKind.Version4, IccTagSignatures.AToB0,
            IccColorSpaceSignatures.OneColor, pcsKind);

        double[][] actual8 = ExecuteBoundaries(versionTwo, tag8);
        double[][] actual16 = ExecuteBoundaries(versionTwo, tag16);
        double[][] actualMAb = ExecuteBoundaries(versionFour, tagMAb);
        double[] low = new double[3];
        double[] high = new[] { 1.0, 1.0, 1.0 };

        Assert.Multiple(() =>
        {
            Assert.That(actual8[0], Is.EqualTo(low));
            Assert.That(actual8[1], Is.EqualTo(low));
            Assert.That(actual8[2], Is.EqualTo(new[] { 0.5, 0.5, 0.5 })
                .Within(1.0 / 255.0));
            Assert.That(actual8[3], Is.EqualTo(high));
            Assert.That(actual8[4], Is.EqualTo(high));
            AssertPcsBoundaries(actual16, pcsKind,
                IccPcsEncodingKind.Version2);
            AssertPcsBoundaries(actualMAb, pcsKind,
                IccPcsEncodingKind.Version4);
        });
    }

    [Test]
    public void ClassicAndMultiStageCodecsRejectOverflowingClutDimensions()
    {
        byte[] classic = LutTagCodec.SerializeLut16(CreateClassicLut(1, 1,
            IccPcsEncodingKind.Version2, new[] { 0.5 }, false));
        classic[8] = 15;
        classic[9] = 15;
        classic[10] = 255;

        IccMultiStageLut valid = CreateMultiStageLut(15, 3,
            new[] { 0.25, 0.5, 0.75 }, true);
        byte[] multiStage = MultiStageLutTagCodec.SerializeLutAToB(valid);
        int clutOffset = (int)ReadUInt32(multiStage, 24);
        int index = 0;
        while (index < 15)
        {
            multiStage[clutOffset + index] = 255;
            index = index + 1;
        }

        IccLut classicWrite = CreateClassicLut(15, 15,
            IccPcsEncodingKind.Version2, new double[15], false);
        classicWrite.Clut.GridPoints = Enumerable.Repeat(255, 15).ToArray();
        classicWrite.Clut.Values = new double[0];
        IccMultiStageLut multiStageWrite = CreateMultiStageLut(15, 3,
            new double[3], true);
        multiStageWrite.Clut!.GridPoints = Enumerable.Repeat(255, 15).ToArray();
        multiStageWrite.Clut.Values = new double[0];

        TagDecodeState classicState = DecodeWithoutContext(classic,
            IccVersionKind.Version2);
        TagDecodeState multiStageState = DecodeWithoutContext(multiStage,
            IccVersionKind.Version4);

        Assert.Multiple(() =>
        {
            Assert.That(classicState.Result.Status,
                Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(classicState.Result.Message,
                Does.Contain("exceed the supported managed size"));
            Assert.That(multiStageState.Result.Status,
                Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(multiStageState.Result.Message,
                Does.Contain("exceed the supported managed size"));
            Assert.Throws<IccProfileWriteException>(() =>
                LutTagCodec.SerializeLut16(classicWrite));
            Assert.Throws<IccProfileWriteException>(() =>
                MultiStageLutTagCodec.SerializeLutAToB(multiStageWrite));
            Assert.That(LutTagCodec.CalculateClutValueCount(15, 15, 255),
                Is.EqualTo(-1));
        });
    }

    private static System.Collections.IEnumerable ChannelCases()
    {
        uint[] signatures = new uint[]
        {
            IccColorSpaceSignatures.OneColor,
            IccColorSpaceSignatures.TwoColor,
            IccColorSpaceSignatures.ThreeColor,
            IccColorSpaceSignatures.FourColor,
            IccColorSpaceSignatures.FiveColor,
            IccColorSpaceSignatures.SixColor,
            IccColorSpaceSignatures.SevenColor,
            IccColorSpaceSignatures.EightColor,
            IccColorSpaceSignatures.NineColor,
            IccColorSpaceSignatures.TenColor,
            IccColorSpaceSignatures.ElevenColor,
            IccColorSpaceSignatures.TwelveColor,
            IccColorSpaceSignatures.ThirteenColor,
            IccColorSpaceSignatures.FourteenColor,
            IccColorSpaceSignatures.FifteenColor
        };
        int index = 0;
        while (index < signatures.Length)
        {
            int channels = index + 1;
            IccPcsKind pcs = (channels & 1) == 0
                ? IccPcsKind.Xyz : IccPcsKind.Lab;
            yield return new TestCaseData(channels, signatures[index], pcs)
                .SetName($"EveryLutFamilyRoundTripsAndExecutes_{channels}Channels_{pcs}");
            index = index + 1;
        }
    }

    private static IccLut CreateClassicLut(int inputChannels,
        int outputChannels, IccPcsEncodingKind encoding,
        double[] clutOutput, bool eightBit)
    {
        int tableLength = eightBit ? 256 : 2;
        IccLut lut = new IccLut();
        lut.InputChannels = inputChannels;
        lut.OutputChannels = outputChannels;
        lut.PcsEncoding = encoding;
        lut.Matrix.Rows = 3;
        lut.Matrix.Columns = 3;
        lut.Matrix.Values = new[]
        {
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0
        };
        lut.InputTables = CreateTables(inputChannels, tableLength);
        lut.OutputTables = CreateTables(outputChannels, tableLength);
        lut.Clut = CreateConstantClut(inputChannels, outputChannels,
            clutOutput);
        return lut;
    }

    private static IccMultiStageLut CreateMultiStageLut(int inputChannels,
        int outputChannels, double[] clutOutput, bool aToB)
    {
        IccMultiStageLut lut = new IccMultiStageLut();
        lut.InputChannels = inputChannels;
        lut.OutputChannels = outputChannels;
        lut.PcsEncoding = IccPcsEncodingKind.Version4;
        lut.ACurves = CreateIdentityCurves(aToB ? inputChannels : outputChannels);
        lut.BCurves = CreateIdentityCurves(aToB ? outputChannels : inputChannels);
        lut.Clut = CreateConstantClut(inputChannels, outputChannels,
            clutOutput);
        return lut;
    }

    private static IccMultiStageLut CreateTopologyLut(bool hasMatrix,
        bool hasClut)
    {
        IccMultiStageLut lut = new IccMultiStageLut();
        lut.InputChannels = 3;
        lut.OutputChannels = 3;
        lut.PcsEncoding = IccPcsEncodingKind.Version4;
        lut.BCurves = CreateIdentityCurves(3);
        if (hasMatrix)
        {
            lut.MCurves = CreateIdentityCurves(3);
            lut.Matrix = new IccMatrix();
            lut.Matrix.Rows = 3;
            lut.Matrix.Columns = 4;
            lut.Matrix.Values = new[]
            {
                1.0, 0.0, 0.0, 0.0,
                0.0, 1.0, 0.0, 0.0,
                0.0, 0.0, 1.0, 0.0
            };
        }
        if (hasClut)
        {
            lut.ACurves = CreateIdentityCurves(3);
            lut.Clut = CreateIdentityThreeChannelClut();
        }
        return lut;
    }

    private static IccClut CreateIdentityThreeChannelClut()
    {
        double[] values = new double[24];
        int vertex = 0;
        while (vertex < 8)
        {
            values[vertex * 3] = (vertex & 4) == 0 ? 0.0 : 1.0;
            values[(vertex * 3) + 1] = (vertex & 2) == 0 ? 0.0 : 1.0;
            values[(vertex * 3) + 2] = (vertex & 1) == 0 ? 0.0 : 1.0;
            vertex = vertex + 1;
        }
        IccClut clut = new IccClut();
        clut.InputChannels = 3;
        clut.OutputChannels = 3;
        clut.GridPoints = new[] { 2, 2, 2 };
        clut.Values = values;
        return clut;
    }

    private static IccClut CreateConstantClut(int inputChannels,
        int outputChannels, double[] output)
    {
        int vertexCount = 1 << inputChannels;
        double[] values = new double[vertexCount * outputChannels];
        int vertex = 0;
        while (vertex < vertexCount)
        {
            Array.Copy(output, 0, values, vertex * outputChannels,
                outputChannels);
            vertex = vertex + 1;
        }
        IccClut clut = new IccClut();
        clut.InputChannels = inputChannels;
        clut.OutputChannels = outputChannels;
        clut.GridPoints = Enumerable.Repeat(2, inputChannels).ToArray();
        clut.Values = values;
        return clut;
    }

    private static double[][] CreateTables(int count, int length)
    {
        double[] ramp = new double[length];
        int sample = 0;
        while (sample < length)
        {
            ramp[sample] = (double)sample / (length - 1);
            sample = sample + 1;
        }
        double[][] tables = new double[count][];
        int index = 0;
        while (index < count)
        {
            tables[index] = ramp.ToArray();
            index = index + 1;
        }
        return tables;
    }

    private static IccCurve[] CreateIdentityCurves(int count)
    {
        IccCurve[] curves = new IccCurve[count];
        int index = 0;
        while (index < count)
        {
            IccCurve curve = new IccCurve();
            curve.Kind = IccCurveKind.Identity;
            curves[index] = curve;
            index = index + 1;
        }
        return curves;
    }

    private static void SetBoundaryClut(IccClut clut)
    {
        clut.Values = new[]
        {
            0.0, 0.0, 0.0,
            1.0, 1.0, 1.0
        };
    }

    private static double[][] ExecuteBoundaries(IccProfile profile, IccTag tag)
    {
        double[] inputs = new[] { -0.5, 0.0, 0.5, 1.0, 1.5 };
        double[][] outputs = new double[inputs.Length][];
        int index = 0;
        while (index < inputs.Length)
        {
            outputs[index] = CompileAndExecute(profile, tag,
                IccTransformDirection.DeviceToPcs,
                IccInterpolationKind.Linear, new[] { inputs[index] });
            index = index + 1;
        }
        return outputs;
    }

    private static void AssertPcsBoundaries(double[][] actual,
        IccPcsKind pcsKind, IccPcsEncodingKind encoding)
    {
        double[] low = DenormalizePcs(new double[3], pcsKind,
            encoding);
        double[] middle = DenormalizePcs(
            new[] { 0.5, 0.5, 0.5 }, pcsKind, encoding);
        double[] high = DenormalizePcs(
            new[] { 1.0, 1.0, 1.0 }, pcsKind, encoding);
        Assert.That(actual[0], Is.EqualTo(low).Within(1e-12));
        Assert.That(actual[1], Is.EqualTo(low).Within(1e-12));
        Assert.That(actual[2], Is.EqualTo(middle).Within(1.0 / 255.0));
        Assert.That(actual[3], Is.EqualTo(high).Within(1e-12));
        Assert.That(actual[4], Is.EqualTo(high).Within(1e-12));
    }

    private static double[] CompileAndExecute(IccProfile profile, IccTag tag,
        IccTransformDirection direction, IccInterpolationKind interpolation,
        double[] input)
    {
        LutPipelinePlan plan = LutPipelineCompiler.Compile(profile, tag,
            direction, interpolation);
        int valueLength = PipelineExecutor.GetRequiredValueScratchLength(
            plan.Stages);
        int indexLength = PipelineExecutor.GetRequiredClutIndexScratchLength(
            plan.Stages);
        int weightLength = PipelineExecutor.GetRequiredClutWeightScratchLength(
            plan.Stages);
        double[] output = new double[plan.DestinationChannels];
        PipelineExecutor.ExecutePipeline(plan.Stages, input, output,
            new double[valueLength], new double[valueLength],
            new int[indexLength], new double[weightLength]);
        return output;
    }

    private static IccProfile CreateProfile(IccVersionKind version,
        uint dataColorSpace, IccPcsKind pcsKind)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version = HeaderCodec.DecodeVersion(
            version == IccVersionKind.Version2 ? 0x02100000u : 0x04400000u);
        profile.Header.ProfileClass = IccProfileClass.Input;
        profile.Header.DataColorSpaceSignature = dataColorSpace;
        profile.Header.ProfileConnectionSpace = pcsKind;
        profile.Header.ProfileConnectionSpaceSignature = pcsKind == IccPcsKind.Lab
            ? IccColorSpaceSignatures.Lab : IccColorSpaceSignatures.Xyz;
        return profile;
    }

    private static TagDecodeState Decode(byte[] payload,
        IccVersionKind version, uint tagSignature, uint dataColorSpace,
        IccPcsKind pcsKind)
    {
        TagDecodeState state = CreateDecodeState(payload, version);
        state.HasProfileContext = true;
        state.TagSignature = tagSignature;
        state.ProfileClass = IccProfileClass.Input;
        state.DataColorSpaceSignature = dataColorSpace;
        state.DataColorSpace = HeaderCodec.DecodeColorSpace(dataColorSpace);
        state.ProfileConnectionSpace = pcsKind;
        state.ProfileConnectionSpaceSignature = pcsKind == IccPcsKind.Lab
            ? IccColorSpaceSignatures.Lab : IccColorSpaceSignatures.Xyz;
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static TagDecodeState DecodeWithoutContext(byte[] payload,
        IccVersionKind version)
    {
        TagDecodeState state = CreateDecodeState(payload, version);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static TagDecodeState CreateDecodeState(byte[] payload,
        IccVersionKind version)
    {
        TagDecodeSource source = new TagDecodeSource(payload);
        return new TagDecodeState(source, 0, payload.Length, 0, version,
            true);
    }

    private static IccTag DecodeTag(byte[] payload, IccVersionKind version,
        uint tagSignature, uint dataColorSpace, IccPcsKind pcsKind)
    {
        TagDecodeState state = Decode(payload, version, tagSignature,
            dataColorSpace, pcsKind);
        Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
        return CreateTag(tagSignature, state.Data);
    }

    private static IccTag CreateTag(uint signature, IccTagData data)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.Data = data;
        return tag;
    }

    private static bool AllSuccessful(params TagDecodeState[] states)
    {
        int index = 0;
        while (index < states.Length)
        {
            if (states[index].Result.Status != IccOperationStatus.Success)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    private static double[] CreateDeviceValues(int count)
    {
        double[] values = new double[count];
        int index = 0;
        while (index < count)
        {
            values[index] = (double)(index + 1) / (count + 1);
            index = index + 1;
        }
        return values;
    }

    private static double[] CreatePcsInput(IccPcsKind pcsKind)
    {
        if (pcsKind == IccPcsKind.Lab)
        {
            return new[] { 50.0, 0.0, 0.0 };
        }
        return new[] { 0.5, 0.5, 0.5 };
    }

    private static double[] Quantize(double[] values, double maximum)
    {
        return values.Select(value => System.Math.Round(value * maximum,
            System.MidpointRounding.AwayFromZero) / maximum).ToArray();
    }

    private static double[] DenormalizePcs(double[] values,
        IccPcsKind pcsKind, IccPcsEncodingKind encoding)
    {
        if (pcsKind == IccPcsKind.Xyz)
        {
            double maximum = 65535.0 / 32768.0;
            return values.Select(value => value * maximum).ToArray();
        }
        if (encoding == IccPcsEncodingKind.Version2)
        {
            double maximumLightness = 65280.0 / 65535.0;
            return new[]
            {
                System.Math.Min(values[0], maximumLightness) * 65535.0 / 652.8,
                (values[1] * 65535.0 / 256.0) - 128.0,
                (values[2] * 65535.0 / 256.0) - 128.0
            };
        }
        return new[]
        {
            values[0] * 100.0,
            (values[1] * 255.0) - 128.0,
            (values[2] * 255.0) - 128.0
        };
    }

    private static uint ReadUInt32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24)
            | ((uint)source[offset + 1] << 16)
            | ((uint)source[offset + 2] << 8)
            | source[offset + 3];
    }
}
