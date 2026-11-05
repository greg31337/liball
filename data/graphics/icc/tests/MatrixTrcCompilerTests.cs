using icc;

namespace icc.Tests;

public class MatrixTrcCompilerTests
{
    [Test]
    public void RgbPlansUseChannelOrderedCurvesMatrixColumnsAndInverseOrder()
    {
        IccProfile profile = CreateRgbProfile(
            Curve(2.0), Curve(1.0), Curve(1.0),
            Xyz(2.0, 0.0, 0.0), Xyz(0.0, 3.0, 0.0), Xyz(0.0, 0.0, 4.0));

        MatrixTrcPlan forward = MatrixTrcCompiler.Compile(profile, IccTransformDirection.DeviceToPcs);
        MatrixTrcPlan reverse = MatrixTrcCompiler.Compile(profile, IccTransformDirection.PcsToDevice);
        double[] device = new double[] { 0.5, 0.25, 0.75 };
        double[] pcs = Execute(forward, device);
        double[] roundTrip = Execute(reverse, pcs);

        Assert.Multiple(() =>
        {
            Assert.That(forward.SourceChannels, Is.EqualTo(3));
            Assert.That(forward.DestinationChannels, Is.EqualTo(3));
            Assert.That(forward.Stages.Select(stage => stage.Kind), Is.EqualTo(new[] { PipelineStageKind.Curves, PipelineStageKind.Matrix }));
            Assert.That(forward.Stages[0].Curves.Select(curve => curve.Gamma), Is.EqualTo(new[] { 2.0, 1.0, 1.0 }));
            Assert.That(forward.Stages[1].Matrix!.Values, Is.EqualTo(new[] { 2.0, 0.0, 0.0, 0.0, 3.0, 0.0, 0.0, 0.0, 4.0 }));
            Assert.That(pcs, Is.EqualTo(new[] { 0.5, 0.75, 3.0 }).Within(1e-14));
            Assert.That(reverse.Stages.Select(stage => stage.Kind), Is.EqualTo(new[] { PipelineStageKind.Matrix, PipelineStageKind.Curves }));
            Assert.That(reverse.Stages[0].Matrix!.Values, Is.EqualTo(new[] { 0.5, 0.0, 0.0, 0.0, 1.0 / 3.0, 0.0, 0.0, 0.0, 0.25 }).Within(1e-15));
            Assert.That(reverse.Stages[1].InvertCurves, Is.True);
            Assert.That(roundTrip, Is.EqualTo(device).Within(1e-14));
        });
    }

    [Test]
    public void GrayPlansMapTrcLuminanceToD50XyzAndBackThroughY()
    {
        IccProfile profile = CreateProfile(IccProfileClass.Output, IccColorSpaceSignatures.Gray, IccPcsKind.Xyz,
            Tag(IccTagSignatures.GrayTrc, Curve(2.0)));

        MatrixTrcPlan forward = MatrixTrcCompiler.Compile(profile, IccTransformDirection.DeviceToPcs);
        MatrixTrcPlan reverse = MatrixTrcCompiler.Compile(profile, IccTransformDirection.PcsToDevice);
        double[] pcs = Execute(forward, new double[] { 0.5 });
        double[] device = Execute(reverse, pcs);

        Assert.Multiple(() =>
        {
            Assert.That(forward.IsGray, Is.True);
            Assert.That(forward.SourceChannels, Is.EqualTo(1));
            Assert.That(forward.DestinationChannels, Is.EqualTo(3));
            Assert.That(forward.Stages.Select(stage => stage.Kind), Is.EqualTo(new[] { PipelineStageKind.Curves, PipelineStageKind.Matrix }));
            Assert.That(pcs, Is.EqualTo(new[]
            {
                IccStandardColorimetry.D50X * 0.25,
                IccStandardColorimetry.D50Y * 0.25,
                IccStandardColorimetry.D50Z * 0.25
            }).Within(1e-14));
            Assert.That(reverse.SourceChannels, Is.EqualTo(3));
            Assert.That(reverse.DestinationChannels, Is.EqualTo(1));
            Assert.That(reverse.Stages[0].Matrix!.Values, Is.EqualTo(new[] { 0.0, 1.0, 0.0 }));
            Assert.That(reverse.Stages[1].InvertCurves, Is.True);
            Assert.That(device[0], Is.EqualTo(0.5).Within(1e-14));
        });
    }

    [Test]
    public void PlanSnapshotsCurvesColumnsAndOptionalLuminanceMetadata()
    {
        IccCurve redCurve = Curve(2.0);
        IccXyzNumber redColumn = Xyz(1.0, 0.0, 0.0);
        IccXyzNumber luminance = Xyz(0.0, 120.0, 0.0);
        IccProfile profile = CreateRgbProfile(redCurve, Curve(1.0), Curve(1.0), redColumn, Xyz(0.0, 1.0, 0.0), Xyz(0.0, 0.0, 1.0));
        profile.Tags = profile.Tags.Concat(new[] { Tag(IccTagSignatures.Luminance, luminance) }).ToArray();

        MatrixTrcPlan plan = MatrixTrcCompiler.Compile(profile, IccTransformDirection.DeviceToPcs);
        redCurve.Gamma = 7.0;
        redColumn.X = 9.0;
        luminance.Y = 500.0;

        Assert.Multiple(() =>
        {
            Assert.That(plan.Luminance, Is.Not.Null);
            Assert.That(plan.Luminance!.X, Is.Zero);
            Assert.That(plan.Luminance.Y, Is.EqualTo(120.0));
            Assert.That(plan.Luminance.Z, Is.Zero);
            Assert.That(plan.Stages[0].Curves[0].Gamma, Is.EqualTo(2.0));
            Assert.That(plan.Stages[1].Matrix!.Values[0], Is.EqualTo(1.0));
        });
    }

    [Test]
    public void CompilationRejectsNonXyzIneligibleIncompleteAndNonInvertibleProfiles()
    {
        IccProfile lab = CreateRgbProfile(Curve(1.0), Curve(1.0), Curve(1.0), Xyz(1.0, 0.0, 0.0), Xyz(0.0, 1.0, 0.0), Xyz(0.0, 0.0, 1.0));
        lab.Header.ProfileConnectionSpace = IccPcsKind.Lab;
        lab.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Lab;
        IccProfile outputRgb = CreateRgbProfile(Curve(1.0), Curve(1.0), Curve(1.0), Xyz(1.0, 0.0, 0.0), Xyz(0.0, 1.0, 0.0), Xyz(0.0, 0.0, 1.0));
        outputRgb.Header.ProfileClass = IccProfileClass.Output;
        IccProfile incomplete = CreateProfile(IccProfileClass.Display, IccColorSpaceSignatures.Rgb, IccPcsKind.Xyz,
            Tag(IccTagSignatures.RedTrc, Curve(1.0)));
        IccProfile singular = CreateRgbProfile(Curve(1.0), Curve(1.0), Curve(1.0), Xyz(1.0, 0.0, 0.0), Xyz(2.0, 0.0, 0.0), Xyz(0.0, 0.0, 1.0));
        IccProfile nonInvertible = CreateRgbProfile(
            new IccCurve { Kind = IccCurveKind.Sampled, Samples = new[] { 0.0, 0.8, 0.2, 1.0 } },
            Curve(1.0), Curve(1.0), Xyz(1.0, 0.0, 0.0), Xyz(0.0, 1.0, 0.0), Xyz(0.0, 0.0, 1.0));

        Assert.Multiple(() =>
        {
            Assert.Throws<IccTransformException>(() => MatrixTrcCompiler.Compile(lab, IccTransformDirection.DeviceToPcs));
            Assert.Throws<IccTransformException>(() => MatrixTrcCompiler.Compile(outputRgb, IccTransformDirection.DeviceToPcs));
            Assert.Throws<IccTransformException>(() => MatrixTrcCompiler.Compile(incomplete, IccTransformDirection.DeviceToPcs));
            Assert.Throws<IccTransformException>(() => MatrixTrcCompiler.Compile(singular, IccTransformDirection.PcsToDevice));
            Assert.Throws<IccTransformException>(() => MatrixTrcCompiler.Compile(nonInvertible, IccTransformDirection.PcsToDevice));
            Assert.Throws<IccTransformException>(() => MatrixTrcCompiler.Compile(incomplete, IccTransformDirection.DeviceToDevice));
        });
    }

    private static double[] Execute(MatrixTrcPlan plan, double[] input)
    {
        int scratchLength = PipelineExecutor.GetRequiredValueScratchLength(plan.Stages);
        double[] output = new double[plan.DestinationChannels];
        PipelineExecutor.ExecutePipeline(plan.Stages, input, output, new double[scratchLength], new double[scratchLength], new int[0], new double[0]);
        return output;
    }

    private static IccProfile CreateRgbProfile(IccCurve red, IccCurve green, IccCurve blue, IccXyzNumber redColumn, IccXyzNumber greenColumn, IccXyzNumber blueColumn)
    {
        return CreateProfile(IccProfileClass.Display, IccColorSpaceSignatures.Rgb, IccPcsKind.Xyz,
            Tag(IccTagSignatures.RedTrc, red), Tag(IccTagSignatures.GreenTrc, green), Tag(IccTagSignatures.BlueTrc, blue),
            Tag(IccTagSignatures.RedMatrixColumn, redColumn), Tag(IccTagSignatures.GreenMatrixColumn, greenColumn), Tag(IccTagSignatures.BlueMatrixColumn, blueColumn));
    }

    private static IccProfile CreateProfile(IccProfileClass profileClass, uint colorSpace, IccPcsKind pcs, params IccTag[] tags)
    {
        IccProfile profile = new IccProfile();
        profile.Header.ProfileClass = profileClass;
        profile.Header.DataColorSpaceSignature = colorSpace;
        profile.Header.ProfileConnectionSpace = pcs;
        profile.Header.ProfileConnectionSpaceSignature = pcs == IccPcsKind.Xyz ? IccColorSpaceSignatures.Xyz : IccColorSpaceSignatures.Lab;
        profile.Tags = tags;
        return profile;
    }

    private static IccCurve Curve(double gamma)
    {
        return new IccCurve { Kind = IccCurveKind.Gamma, Gamma = gamma };
    }

    private static IccXyzNumber Xyz(double x, double y, double z)
    {
        return new IccXyzNumber { X = x, Y = y, Z = z };
    }

    private static IccTag Tag(uint signature, IccCurve curve)
    {
        return new IccTag { Signature = signature, Data = new IccTagData { Curve = curve } };
    }

    private static IccTag Tag(uint signature, IccXyzNumber xyz)
    {
        return new IccTag { Signature = signature, Data = new IccTagData { XyzValues = new[] { xyz } } };
    }
}
