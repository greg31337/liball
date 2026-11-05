using icc;

namespace icc.Tests;

public class LutPipelineCompilerTests
{
    [Test]
    public void Lut16CompilesClassicStagesAndConvertsNormalizedOutputToVersionTwoLab()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Gray, IccPcsKind.Lab);
        IccLut lut = ClassicLut(1, 3, IccPcsEncodingKind.Version2);
        lut.Clut.Values = new[] { 0.2, 0.5, 0.5, 0.8, 0.5, 0.5 };
        IccTag tag = ClassicTag(IccTypeSignatures.Lut16, IccTagSignatures.AToB0, lut);

        LutPipelinePlan plan = LutPipelineCompiler.Compile(profile, tag, IccTransformDirection.DeviceToPcs, IccInterpolationKind.Linear);
        double[] actual = Execute(plan, new[] { 0.5 });
        double[] expected = PcsConverter.DenormalizePcs(new[] { 0.5, 0.5, 0.5 }, IccPcsKind.Lab, IccPcsEncodingKind.Version2);

        Assert.Multiple(() =>
        {
            Assert.That(plan.TypeSignature, Is.EqualTo(IccTypeSignatures.Lut16));
            Assert.That(plan.PcsEncoding, Is.EqualTo(IccPcsEncodingKind.Version2));
            Assert.That(plan.Stages.Select(stage => stage.Kind), Is.EqualTo(new[]
            {
                PipelineStageKind.Curves,
                PipelineStageKind.Clut,
                PipelineStageKind.Curves,
                PipelineStageKind.PcsDenormalize
            }));
            Assert.That(actual, Is.EqualTo(expected).Within(1e-12));
            Assert.That(plan.SourceRanges.Select(range => new[] { range.Minimum, range.Maximum }), Is.EqualTo(new[] { new[] { 0.0, 1.0 } }));
            Assert.That(plan.DestinationRanges[0].Maximum, Is.EqualTo(100.0));
            Assert.That(plan.DestinationRanges[1].Minimum, Is.EqualTo(-128.0));
            Assert.That(plan.DestinationRanges[1].Maximum, Is.EqualTo(PcsConverter.Version2LabMaximum));
        });
    }

    [Test]
    public void Lut8UsesItsMatrixOnlyWhenTheInputDomainIsPcsXyz()
    {
        IccLut forwardLut = ClassicLut(3, 3, IccPcsEncodingKind.Normalized);
        forwardLut.Matrix.Values = new[] { 2.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 };
        IccProfile rgb = Profile(IccColorSpaceSignatures.Rgb, IccPcsKind.Xyz);
        LutPipelinePlan deviceInput = LutPipelineCompiler.Compile(rgb, ClassicTag(IccTypeSignatures.Lut8, IccTagSignatures.AToB0, forwardLut), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Tetrahedral);
        IccProfile xyzData = Profile(IccColorSpaceSignatures.Xyz, IccPcsKind.Xyz);
        LutPipelinePlan xyzInput = LutPipelineCompiler.Compile(xyzData, ClassicTag(IccTypeSignatures.Lut8, IccTagSignatures.AToB0, forwardLut), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear);
        IccLut reverseLut = ClassicLut(3, 3, IccPcsEncodingKind.Normalized);
        LutPipelinePlan pcsInput = LutPipelineCompiler.Compile(rgb, ClassicTag(IccTypeSignatures.Lut8, IccTagSignatures.BToA0, reverseLut), IccTransformDirection.PcsToDevice, IccInterpolationKind.Multilinear);

        Assert.Multiple(() =>
        {
            Assert.That(deviceInput.Stages[0].Kind, Is.EqualTo(PipelineStageKind.Curves));
            Assert.That(deviceInput.Stages.Any(stage => stage.Kind == PipelineStageKind.Matrix), Is.False);
            Assert.That(xyzInput.Stages[0].Kind, Is.EqualTo(PipelineStageKind.Matrix));
            Assert.That(pcsInput.Stages.Select(stage => stage.Kind), Is.EqualTo(new[]
            {
                PipelineStageKind.PcsNormalize,
                PipelineStageKind.Matrix,
                PipelineStageKind.Curves,
                PipelineStageKind.Clut,
                PipelineStageKind.Curves
            }));
            Assert.That(pcsInput.SourceRanges.All(range => range.Minimum == 0.0 && range.Maximum == 1.0), Is.True);
            Assert.That(pcsInput.DestinationRanges.All(range => range.Minimum == 0.0 && range.Maximum == 1.0), Is.True);
        });
    }

    [Test]
    public void LutAToBCompilesAllStagesInForwardOrderAndExecutesOffsets()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Gray, IccPcsKind.Xyz);
        IccMultiStageLut lut = MultiStageLut(1, 3, true, true);
        lut.Matrix!.Values = new[]
        {
            1.0, 0.0, 0.0, 0.1,
            0.0, 1.0, 0.0, 0.2,
            0.0, 0.0, 1.0, 0.3
        };
        IccTag tag = MultiStageTag(IccTypeSignatures.LutAToB, IccTagSignatures.AToB0, lut);

        LutPipelinePlan plan = LutPipelineCompiler.Compile(profile, tag, IccTransformDirection.DeviceToPcs, IccInterpolationKind.Linear);
        double[] actual = Execute(plan, new[] { 0.5 });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Stages.Select(stage => stage.Description), Is.EqualTo(new[]
            {
                "lutAToB A curves",
                "lutAToB CLUT",
                "lutAToB M curves",
                "lutAToB matrix and offsets",
                "lutAToB B curves",
                "denormalize destination PCS values"
            }));
            Assert.That(actual, Is.EqualTo(new[]
            {
                0.6 * PcsConverter.XyzMaximum,
                0.7 * PcsConverter.XyzMaximum,
                0.8 * PcsConverter.XyzMaximum
            }).Within(1e-12));
            Assert.That(plan.DestinationRanges.All(range => range.Maximum == PcsConverter.XyzMaximum), Is.True);
        });
    }

    [Test]
    public void LutBToACompilesAllStagesInReverseOrderAndExecutesNormalizedPcsInput()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Gray, IccPcsKind.Xyz);
        IccMultiStageLut lut = MultiStageLut(3, 1, false, true);
        lut.Clut!.Values = Enumerable.Repeat(0.25, 8).ToArray();
        lut.ACurves[0] = Gamma(2.0);
        IccTag tag = MultiStageTag(IccTypeSignatures.LutBToA, IccTagSignatures.BToA0, lut);

        LutPipelinePlan plan = LutPipelineCompiler.Compile(profile, tag, IccTransformDirection.PcsToDevice, IccInterpolationKind.Tetrahedral);
        double[] actual = Execute(plan, new[] { 0.4 * PcsConverter.XyzMaximum, 0.5 * PcsConverter.XyzMaximum, 0.6 * PcsConverter.XyzMaximum });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Stages.Select(stage => stage.Description), Is.EqualTo(new[]
            {
                "normalize source PCS values",
                "lutBToA B curves",
                "lutBToA matrix and offsets",
                "lutBToA M curves",
                "lutBToA CLUT",
                "lutBToA A curves"
            }));
            Assert.That(actual[0], Is.EqualTo(0.0625).Within(1e-14));
            Assert.That(plan.SourceRanges.All(range => range.Maximum == PcsConverter.XyzMaximum), Is.True);
            Assert.That(plan.DestinationRanges.Single().Maximum, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void MultiStageOptionalPairsAreOmittedWithoutIdentityPlaceholders()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Rgb, IccPcsKind.Lab);
        IccMultiStageLut bOnly = MultiStageLut(3, 3, true, false);
        IccMultiStageLut clutOnly = MultiStageLut(3, 3, true, true);
        clutOnly.Matrix = null;
        clutOnly.MCurves = new IccCurve[0];
        IccMultiStageLut matrixOnly = MultiStageLut(3, 3, true, false);
        matrixOnly.Matrix = IdentityOffsetMatrix();
        matrixOnly.MCurves = IdentityCurves(3);

        LutPipelinePlan bPlan = LutPipelineCompiler.Compile(profile, MultiStageTag(IccTypeSignatures.LutAToB, IccTagSignatures.AToB0, bOnly), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear);
        LutPipelinePlan clutPlan = LutPipelineCompiler.Compile(profile, MultiStageTag(IccTypeSignatures.LutAToB, IccTagSignatures.AToB0, clutOnly), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear);
        LutPipelinePlan matrixPlan = LutPipelineCompiler.Compile(profile, MultiStageTag(IccTypeSignatures.LutAToB, IccTagSignatures.AToB0, matrixOnly), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear);

        Assert.Multiple(() =>
        {
            Assert.That(bPlan.Stages.Select(stage => stage.Kind), Is.EqualTo(new[] { PipelineStageKind.Curves, PipelineStageKind.PcsDenormalize }));
            Assert.That(clutPlan.Stages.Select(stage => stage.Kind), Is.EqualTo(new[] { PipelineStageKind.Curves, PipelineStageKind.Clut, PipelineStageKind.Curves, PipelineStageKind.PcsDenormalize }));
            Assert.That(matrixPlan.Stages.Select(stage => stage.Kind), Is.EqualTo(new[] { PipelineStageKind.Curves, PipelineStageKind.Matrix, PipelineStageKind.Curves, PipelineStageKind.PcsDenormalize }));
        });
    }

    [Test]
    public void CompiledPlanSnapshotsTablesCurvesMatricesAndClutValues()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Gray, IccPcsKind.Xyz);
        IccMultiStageLut lut = MultiStageLut(1, 3, true, true);
        IccTag tag = MultiStageTag(IccTypeSignatures.LutAToB, IccTagSignatures.AToB0, lut);

        LutPipelinePlan plan = LutPipelineCompiler.Compile(profile, tag, IccTransformDirection.DeviceToPcs, IccInterpolationKind.Linear);
        lut.ACurves[0].Gamma = 8.0;
        lut.Clut!.Values[0] = 0.9;
        lut.Matrix!.Values[0] = 7.0;

        Assert.Multiple(() =>
        {
            Assert.That(plan.Stages[0].Curves[0].Gamma, Is.EqualTo(1.0));
            Assert.That(plan.Stages[1].Clut!.Values[0], Is.Zero);
            Assert.That(plan.Stages[3].Matrix!.Values[0], Is.EqualTo(1.0));
        });
    }

    [Test]
    public void CompilationRejectsDirectionTopologyDomainsAndChannelMismatches()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Rgb, IccPcsKind.Lab);
        IccMultiStageLut wrongDirection = MultiStageLut(3, 3, true, false);
        IccMultiStageLut unpaired = MultiStageLut(3, 3, true, false);
        unpaired.MCurves = IdentityCurves(3);
        IccLut badValue = ClassicLut(3, 3, IccPcsEncodingKind.Normalized);
        badValue.InputTables[0][1] = 1.1;
        IccLut shortLut8 = ClassicLut(3, 3, IccPcsEncodingKind.Normalized);
        shortLut8.InputTables[0] = new[] { 0.0, 1.0 };
        IccLut wrongChannels = ClassicLut(4, 3, IccPcsEncodingKind.Normalized);
        IccLut multiDimensional = ClassicLut(3, 3, IccPcsEncodingKind.Normalized);
        IccProfile spectral = Profile(IccColorSpaceSignatures.Rgb, IccPcsKind.Spectral);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccTransformException>(() => LutPipelineCompiler.Compile(profile, MultiStageTag(IccTypeSignatures.LutAToB, IccTagSignatures.AToB0, wrongDirection), IccTransformDirection.PcsToDevice, IccInterpolationKind.Multilinear));
            Assert.Throws<IccTransformException>(() => LutPipelineCompiler.Compile(profile, MultiStageTag(IccTypeSignatures.LutAToB, IccTagSignatures.AToB0, unpaired), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear));
            Assert.Throws<IccTransformException>(() => LutPipelineCompiler.Compile(profile, ClassicTag(IccTypeSignatures.Lut8, IccTagSignatures.AToB0, badValue), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear));
            Assert.Throws<IccTransformException>(() => LutPipelineCompiler.Compile(profile, ClassicTag(IccTypeSignatures.Lut8, IccTagSignatures.AToB0, shortLut8), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear));
            Assert.Throws<IccTransformException>(() => LutPipelineCompiler.Compile(profile, ClassicTag(IccTypeSignatures.Lut8, IccTagSignatures.AToB0, wrongChannels), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear));
            Assert.Throws<IccTransformException>(() => LutPipelineCompiler.Compile(profile, ClassicTag(IccTypeSignatures.Lut8, IccTagSignatures.BToA0, multiDimensional), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear));
            Assert.Throws<IccTransformException>(() => LutPipelineCompiler.Compile(profile, ClassicTag(IccTypeSignatures.Lut8, IccTagSignatures.AToB0, multiDimensional), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Linear));
            Assert.Throws<IccTransformException>(() => LutPipelineCompiler.Compile(spectral, ClassicTag(IccTypeSignatures.Lut8, IccTagSignatures.AToB0, multiDimensional), IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear));
        });
    }

    private static double[] Execute(LutPipelinePlan plan, double[] input)
    {
        int valueLength = PipelineExecutor.GetRequiredValueScratchLength(plan.Stages);
        int indexLength = PipelineExecutor.GetRequiredClutIndexScratchLength(plan.Stages);
        int weightLength = PipelineExecutor.GetRequiredClutWeightScratchLength(plan.Stages);
        double[] output = new double[plan.DestinationChannels];
        PipelineExecutor.ExecutePipeline(plan.Stages, input, output, new double[valueLength], new double[valueLength], new int[indexLength], new double[weightLength]);
        return output;
    }

    private static IccProfile Profile(uint dataColorSpace, IccPcsKind pcs)
    {
        IccProfile profile = new IccProfile();
        profile.Header.DataColorSpaceSignature = dataColorSpace;
        profile.Header.ProfileConnectionSpace = pcs;
        profile.Header.ProfileConnectionSpaceSignature = pcs == IccPcsKind.Xyz ? IccColorSpaceSignatures.Xyz : pcs == IccPcsKind.Lab ? IccColorSpaceSignatures.Lab : 0;
        return profile;
    }

    private static IccTag ClassicTag(uint typeSignature, uint tagSignature, IccLut lut)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Lut;
        data.Lut = lut;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = typeSignature;
        return new IccTag { Signature = tagSignature, Data = data };
    }

    private static IccTag MultiStageTag(uint typeSignature, uint tagSignature, IccMultiStageLut lut)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Lut;
        data.MultiStageLut = lut;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = typeSignature;
        return new IccTag { Signature = tagSignature, Data = data };
    }

    private static IccLut ClassicLut(int inputChannels, int outputChannels, IccPcsEncodingKind encoding)
    {
        IccLut lut = new IccLut();
        lut.InputChannels = inputChannels;
        lut.OutputChannels = outputChannels;
        lut.PcsEncoding = encoding;
        lut.Matrix = new IccMatrix { Rows = 3, Columns = 3, Values = new[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 } };
        int tableLength = encoding == IccPcsEncodingKind.Normalized ? 256 : 2;
        lut.InputTables = Enumerable.Range(0, inputChannels).Select(index => Ramp(tableLength)).ToArray();
        lut.OutputTables = Enumerable.Range(0, outputChannels).Select(index => Ramp(tableLength)).ToArray();
        lut.Clut = new IccClut
        {
            InputChannels = inputChannels,
            OutputChannels = outputChannels,
            GridPoints = Enumerable.Repeat(2, inputChannels).ToArray(),
            Values = Ramp(outputChannels * (1 << inputChannels))
        };
        return lut;
    }

    private static IccMultiStageLut MultiStageLut(int inputChannels, int outputChannels, bool aToB, bool allOptional)
    {
        IccMultiStageLut lut = new IccMultiStageLut();
        lut.InputChannels = inputChannels;
        lut.OutputChannels = outputChannels;
        lut.PcsEncoding = IccPcsEncodingKind.Version4;
        lut.BCurves = IdentityCurves(aToB ? outputChannels : inputChannels);
        if (allOptional)
        {
            lut.ACurves = IdentityCurves(aToB ? inputChannels : outputChannels);
            lut.MCurves = IdentityCurves(aToB ? outputChannels : inputChannels);
            lut.Matrix = IdentityOffsetMatrix();
            lut.Clut = new IccClut
            {
                InputChannels = inputChannels,
                OutputChannels = outputChannels,
                GridPoints = Enumerable.Repeat(2, inputChannels).ToArray(),
                Values = MultiStageClutValues(inputChannels, outputChannels)
            };
        }

        return lut;
    }

    private static double[] MultiStageClutValues(int inputChannels, int outputChannels)
    {
        int points = 1 << inputChannels;
        double[] values = new double[points * outputChannels];
        int point = 0;
        while (point < points)
        {
            double value = point / (double)(points - 1);
            int output = 0;
            while (output < outputChannels)
            {
                values[(point * outputChannels) + output] = value;
                output++;
            }

            point++;
        }

        return values;
    }

    private static IccMatrix IdentityOffsetMatrix()
    {
        return new IccMatrix
        {
            Rows = 3,
            Columns = 4,
            Values = new[] { 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0 }
        };
    }

    private static IccCurve[] IdentityCurves(int count)
    {
        return Enumerable.Range(0, count).Select(index => Gamma(1.0)).ToArray();
    }

    private static IccCurve Gamma(double gamma)
    {
        return new IccCurve { Kind = IccCurveKind.Gamma, Gamma = gamma };
    }

    private static double[] Ramp(int count)
    {
        return Enumerable.Range(0, count).Select(index => index / (double)(count - 1)).ToArray();
    }
}
