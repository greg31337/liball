using icc;

namespace icc.Tests;

public class BrdfEvaluatorTests
{
    [TestCase(BrdfEvaluator.BlinnPhongMonochrome, 4, true)]
    [TestCase(BrdfEvaluator.BlinnPhongColor, 3, false)]
    [TestCase(BrdfEvaluator.CookTorranceOneMonochrome, 6, true)]
    [TestCase(BrdfEvaluator.CookTorranceOneColor, 5, false)]
    [TestCase(BrdfEvaluator.CookTorranceTwoMonochrome, 9, true)]
    [TestCase(BrdfEvaluator.CookTorranceTwoColor, 8, false)]
    [TestCase(BrdfEvaluator.CookTorranceThreeMonochrome, 11, true)]
    [TestCase(BrdfEvaluator.CookTorranceThreeColor, 10, false)]
    [TestCase(BrdfEvaluator.WardMonochrome, 5, true)]
    [TestCase(BrdfEvaluator.WardColor, 4, false)]
    [TestCase(BrdfEvaluator.LafortuneOneMonochrome, 9, true)]
    [TestCase(BrdfEvaluator.LafortuneOneColor, 5, false)]
    [TestCase(BrdfEvaluator.LafortuneTwoMonochrome, 16, true)]
    [TestCase(BrdfEvaluator.LafortuneTwoColor, 9, false)]
    [TestCase(BrdfEvaluator.LafortuneThreeMonochrome, 23, true)]
    [TestCase(BrdfEvaluator.LafortuneThreeColor, 13, false)]
    public void RegisteredModelsExposeStandardParameterMetadata(uint signature, int count, bool monochrome)
    {
        Assert.Multiple(() =>
        {
            Assert.That(BrdfEvaluator.GetParameterCount(signature), Is.EqualTo(count));
            Assert.That(BrdfEvaluator.IsMonochromeModel(signature), Is.EqualTo(monochrome));
        });
    }

    [Test]
    public void BlinnPhongEvaluatesFullColorAndMonochromeParameterLayouts()
    {
        double[] color = new double[2];
        double[] monochrome = new double[2];

        IccOperationResult colorResult = BrdfEvaluator.EvaluateFunction(BrdfEvaluator.BlinnPhongColor,
            new double[] { 0.2, 0.3, 4.0, 0.4, 0.1, 8.0 }, new double[] { 0.7, 0.9 }, 0.0, 0.0, 0.0, 0.0, color);
        IccOperationResult monochromeResult = BrdfEvaluator.EvaluateFunction(BrdfEvaluator.BlinnPhongMonochrome,
            new double[] { 2.0, 3.0, 0.1, 2.0 }, new double[] { 0.2, 0.5 }, 0.0, 0.0, 0.0, 0.0, monochrome);

        Assert.Multiple(() =>
        {
            Assert.That(colorResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(color, Is.EqualTo(new double[] { 0.5, 0.5 }).Within(1e-14));
            Assert.That(monochromeResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(monochrome, Is.EqualTo(new double[] { 1.1, 2.6 }).Within(1e-14));
        });
    }

    [Test]
    public void StandardCookTorranceWardAndLafortuneEquationsEvaluateAtNormalIncidence()
    {
        double[] cook = new double[1];
        double[] ward = new double[1];
        double[] angledWard = new double[1];
        double[] lafortune = new double[1];

        IccOperationResult cookResult = BrdfEvaluator.EvaluateFunction(BrdfEvaluator.CookTorranceOneColor,
            new double[] { 0.2, 0.5, 0.5, 1.5, 0.0 }, new double[] { 0.0 }, 0.0, 0.0, 0.0, 0.0, cook);
        IccOperationResult wardResult = BrdfEvaluator.EvaluateFunction(BrdfEvaluator.WardColor,
            new double[] { 0.2, 0.4, 0.5, 0.25 }, new double[] { 0.0 }, 0.0, 0.0, 0.0, 0.0, ward);
        IccOperationResult angledWardResult = BrdfEvaluator.EvaluateFunction(BrdfEvaluator.WardColor,
            new double[] { 0.2, 0.4, 0.5, 0.25 }, new double[] { 0.0 }, 0.0, 0.0, 0.0, 60.0, angledWard);
        IccOperationResult lafortuneResult = BrdfEvaluator.EvaluateFunction(BrdfEvaluator.LafortuneOneColor,
            new double[] { 0.2, 0.0, 0.0, 0.5, 2.0 }, new double[] { 0.0 }, 0.0, 0.0, 0.0, 0.0, lafortune);

        Assert.Multiple(() =>
        {
            Assert.That(cookResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(cook[0], Is.EqualTo(0.2 + (0.08 / Math.PI)).Within(1e-14));
            Assert.That(wardResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(ward[0], Is.EqualTo(0.2 + (0.8 / Math.PI)).Within(1e-14));
            Assert.That(angledWardResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(angledWard[0], Is.EqualTo(0.16165235829346342).Within(1e-14));
            Assert.That(lafortuneResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(lafortune[0], Is.EqualTo(0.45).Within(1e-14));
        });
    }

    [Test]
    public void UnsupportedAndInvalidModelsReturnResultsWithoutChangingOutput()
    {
        double[] unsupportedOutput = new double[] { 7.0 };
        double[] invalidOutput = new double[] { 8.0 };

        IccOperationResult unsupported = BrdfEvaluator.EvaluateFunction(0x66757472, new double[0], new double[] { 0.5 }, 0.0, 0.0, 0.0, 0.0, unsupportedOutput);
        IccOperationResult invalid = BrdfEvaluator.EvaluateFunction(BrdfEvaluator.BlinnPhongColor, new double[] { 0.2, 0.3 }, new double[] { 0.5 }, 0.0, 0.0, 0.0, 0.0, invalidOutput);

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(unsupported.Message, Does.Contain("futr"));
            Assert.That(unsupportedOutput[0], Is.EqualTo(7.0));
            Assert.That(invalid.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(invalidOutput[0], Is.EqualTo(8.0));
        });
    }

    [Test]
    public void ParameterWorkflowExecutesEmbeddedMpeBeforeMonochromeBrdfFunction()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Xyz);
        IccMultiProcessElements parameterPipeline = Pipeline(1, 4, Matrix(1, 4, new double[]
        {
            0.0, 2.0,
            0.0, 3.0,
            0.0, 0.1,
            0.0, 2.0
        }));
        IccBrdf brdf = new IccBrdf
        {
            TypeSignature = StructuredTagProjectionCodec.MonochromeBrdf,
            ModelSignature = BrdfEvaluator.BlinnPhongMonochrome,
            Parameters = new double[] { 4.0 },
            ColorimetricTransform = parameterPipeline
        };
        double[] output = new double[2];

        IccOperationResult result = BrdfEvaluator.EvaluateParameterWorkflow(profile, brdf, new double[] { 0.25 },
            new double[] { 0.2, 0.5 }, 0.0, 0.0, 0.0, 0.0, false, IccInterpolationKind.Multilinear, output);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(output, Is.EqualTo(new double[] { 1.1, 2.6 }).Within(1e-14));
        });
    }

    [Test]
    public void DirectBrdfTransformEncodesViewingAndLightingAnglesAheadOfColorChannels()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Xyz);
        IccTag tag = Tag(IccTagSignatures.BrdfAToB0, GeometryProbePipeline());
        double[] output = new double[3];

        IccOperationResult result = BrdfEvaluator.EvaluateBrdfTransform(profile, tag, new double[] { 0.25 },
            0.0, 45.0, -90.0, 90.0, IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear, output);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(output, Is.EqualTo(new double[] { 0.5, 0.5, 1.5 }).Within(1e-14));
        });
    }

    [Test]
    public void DirectionalEmissionEncodesViewingAnglesPositionsAndSourceChannels()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Xyz);
        IccTag tag = Tag(IccTagSignatures.DirectionalAToB2, GeometryProbePipeline());
        double[] output = new double[3];

        IccOperationResult result = BrdfEvaluator.EvaluateDirectionalEmission(profile, tag, new double[] { 0.25 },
            0.0, 45.0, -0.25, 0.75, IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear, output);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(output, Is.EqualTo(new double[] { 0.5, 0.5, 0.75 }).Within(1e-14));
        });
    }

    [Test]
    public void DirectionalEmissionRejectsOutOfRangeGeometryBeforeCompilation()
    {
        double[] output = new double[] { 9.0 };

        IccTag tag = new IccTag { Signature = IccTagSignatures.DirectionalAToB0 };
        IccOperationResult result = BrdfEvaluator.EvaluateDirectionalEmission(new IccProfile(), tag, new double[0],
            0.0, 91.0, 0.0, 0.0, IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear, output);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(output[0], Is.EqualTo(9.0));
        });
    }

    [Test]
    public void ReverseDirectionalEmissionUsesPcsChannelsAfterGeometry()
    {
        IccProfile profile = Profile(IccColorSpaceSignatures.Gray, IccColorSpaceSignatures.Xyz);
        IccMultiProcessElements pipeline = Pipeline(7, 1, Matrix(7, 1, new double[] { 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0 }));
        IccTag tag = Tag(IccTagSignatures.DirectionalBToA0, pipeline);
        double[] output = new double[1];

        IccOperationResult result = BrdfEvaluator.EvaluateDirectionalEmission(profile, tag, new double[] { 0.2, 0.3, 0.4 },
            0.0, 0.0, 0.0, 0.0, IccTransformDirection.PcsToDevice, IccInterpolationKind.Multilinear, output);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(output[0], Is.EqualTo(0.2).Within(1e-14));
            Assert.That(() => BrdfEvaluator.EvaluateDirectionalEmission(profile, tag, new double[] { 0.2, 0.3, 0.4 },
                0.0, 0.0, 0.0, 0.0, IccTransformDirection.DeviceToPcs, IccInterpolationKind.Multilinear, output), Throws.TypeOf<IccTransformException>());
        });
    }

    private static IccMultiProcessElements GeometryProbePipeline()
    {
        return Pipeline(5, 3, Matrix(5, 3, new double[]
        {
            1.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 1.0, 1.0, 0.0
        }));
    }

    private static IccProfile Profile(uint dataColorSpace, uint pcs)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = IccVersionKind.Version5;
        profile.Header.DataColorSpaceSignature = dataColorSpace;
        profile.Header.ProfileConnectionSpaceSignature = pcs;
        profile.Header.ProfileConnectionSpace = pcs == IccColorSpaceSignatures.Xyz ? IccPcsKind.Xyz : IccPcsKind.Lab;
        return profile;
    }

    private static IccTag Tag(uint signature, IccMultiProcessElements pipeline)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.MultiProcessElements;
        data.MultiProcessElements = pipeline;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.MultiProcessElements;
        return new IccTag { Signature = signature, Data = data };
    }

    private static IccMultiProcessElements Pipeline(int inputChannels, int outputChannels, params IccProcessingElement[] elements)
    {
        IccMultiProcessElements pipeline = new IccMultiProcessElements();
        pipeline.InputChannels = inputChannels;
        pipeline.OutputChannels = outputChannels;
        pipeline.InputRanges = Ranges(inputChannels);
        pipeline.OutputRanges = Ranges(outputChannels);
        pipeline.Elements = elements;
        pipeline.IsExecutable = true;
        return pipeline;
    }

    private static IccProcessingElement Matrix(int inputChannels, int outputChannels, double[] values)
    {
        return new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Matrix,
            Kind = IccProcessingElementKind.Matrix,
            InputChannels = inputChannels,
            OutputChannels = outputChannels,
            Matrix = new IccMatrix { Rows = outputChannels, Columns = inputChannels + 1, Values = values }
        };
    }

    private static IccEncodingRange[] Ranges(int count)
    {
        IccEncodingRange[] ranges = new IccEncodingRange[count];
        int index = 0;
        while (index < count)
        {
            ranges[index] = new IccEncodingRange { Minimum = float.MinValue, Maximum = float.MaxValue };
            index++;
        }
        return ranges;
    }
}
