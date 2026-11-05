using icc;

namespace icc.Tests;

public class McsConnectionCompilerTests
{
    [Test]
    public void IdentificationToVisualizationRoutesByNameAndUsesDestinationDefaults()
    {
        IccProfile source = SourceProfile(IccProfileClass.MultiplexIdentification, IccColorSpaceSignatures.Gray,
            new[] { "Gloss", "Texture", "Metallic" }, MatrixElement(1, 3, new[] { 1.0, 0.0, 2.0, 0.0, 3.0, 0.0 }));
        IccProfile destination = VisualizationProfile(new[] { "Metallic", "Gloss", "Missing", "Texture" }, new[] { 0.0, 0.0, 0.75, 0.0 },
            IccTagSignatures.MToB0, IccColorSpaceSignatures.Xyz,
            MatrixElement(4, 3, new[]
            {
                1.0, 0.0, 0.0, 0.0, 0.0,
                0.0, 1.0, 0.0, 0.0, 0.0,
                0.0, 0.0, 1.0, 1.0, 0.0
            }));

        McsConnectionPlan plan = McsConnectionCompiler.Compile(source, destination, IccInterpolationKind.Multilinear);
        destination.Tags[0].Data.MultiplexChannels!.Names[0] = "changed";
        destination.Tags[1].Data.MultiplexChannels!.DefaultValues[2] = 99.0;
        double[] result = Execute(plan, new[] { 0.2 });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Direction, Is.EqualTo(IccTransformDirection.DeviceToPcs));
            Assert.That(plan.SourceTagSignature, Is.EqualTo(IccTagSignatures.AToM0));
            Assert.That(plan.DestinationTagSignature, Is.EqualTo(IccTagSignatures.MToB0));
            Assert.That(plan.ChannelOrder, Is.EqualTo(new[] { 2, 0, -1, 1 }));
            Assert.That(plan.ChannelDefaults, Is.EqualTo(new[] { 0.0, 0.0, 0.75, 0.0 }));
            Assert.That(plan.Stages.Select(stage => stage.Kind), Is.EqualTo(new[] { PipelineStageKind.Matrix, PipelineStageKind.ChannelMap, PipelineStageKind.Matrix }));
            Assert.That(result, Is.EqualTo(new[] { 0.6, 0.2, 1.15 }).Within(1e-12));
        });
    }

    [Test]
    public void InputToMultiplexLinkCreatesADeviceToDeviceConnection()
    {
        IccProfile source = SourceProfile(IccProfileClass.Input, IccColorSpaceSignatures.Gray,
            new[] { "A", "B" }, MatrixElement(1, 2, new[] { 1.0, 0.0, 2.0, 0.0 }));
        IccProfile destination = LinkProfile(IccColorSpaceSignatures.Gray, new[] { "B", "A" },
            MatrixElement(2, 1, new[] { 10.0, 1.0, 0.0 }));

        McsConnectionPlan plan = McsConnectionCompiler.Compile(source, destination, IccInterpolationKind.Multilinear);
        double[] result = Execute(plan, new[] { 0.5 });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Direction, Is.EqualTo(IccTransformDirection.DeviceToDevice));
            Assert.That(plan.DestinationTagSignature, Is.EqualTo(IccTagSignatures.MToA0));
            Assert.That(plan.ChannelOrder, Is.EqualTo(new[] { 1, 0 }));
            Assert.That(result, Is.EqualTo(new[] { 10.5 }).Within(1e-12));
        });
    }

    [Test]
    public void VisualizationFallsBackToItsSpectralMcsTransform()
    {
        IccProfile source = SourceProfile(IccProfileClass.MultiplexIdentification, IccColorSpaceSignatures.Gray,
            new[] { "A", "B" }, MatrixElement(1, 2, new[] { 1.0, 0.0, 2.0, 0.0 }));
        uint spectralPcs = IccColorSpaceSignatures.RadiantSpectralPrefix | 2;
        IccProfile destination = VisualizationProfile(new[] { "A", "B" }, new double[0], IccTagSignatures.MToS0, 0,
            MatrixElement(2, 2, new[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0 }));
        destination.Header.SpectralPcsSignature = spectralPcs;

        McsConnectionPlan plan = McsConnectionCompiler.Compile(source, destination, IccInterpolationKind.Multilinear);

        Assert.Multiple(() =>
        {
            Assert.That(plan.DestinationTagSignature, Is.EqualTo(IccTagSignatures.MToS0));
            Assert.That(plan.DestinationChannels, Is.EqualTo(2));
            Assert.That(Execute(plan, new[] { 0.25 }), Is.EqualTo(new[] { 0.25, 0.5 }).Within(1e-12));
        });
    }

    [Test]
    public void MatchingIsCaseSensitiveAndMissingDefaultsAreZero()
    {
        IccProfile source = SourceProfile(IccProfileClass.MultiplexIdentification, IccColorSpaceSignatures.Gray,
            new[] { "Gloss" }, MatrixElement(1, 1, new[] { 1.0, 0.0 }));
        IccProfile destination = VisualizationProfile(new[] { "gloss", "Other" }, new double[0], IccTagSignatures.MToB0, IccColorSpaceSignatures.Xyz,
            MatrixElement(2, 3, new[]
            {
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0,
                1.0, 1.0, 0.0
            }));

        McsConnectionPlan plan = McsConnectionCompiler.Compile(source, destination, IccInterpolationKind.Multilinear);

        Assert.Multiple(() =>
        {
            Assert.That(plan.ChannelOrder, Is.EqualTo(new[] { -1, -1 }));
            Assert.That(plan.ChannelDefaults, Is.EqualTo(new[] { 0.0, 0.0 }));
            Assert.That(Execute(plan, new[] { 0.8 }), Is.EqualTo(new[] { 0.0, 0.0, 0.0 }));
        });
    }

    [Test]
    public void SourceSubsetFlagRequiresEverySourceNameInDestination()
    {
        IccProfile source = SourceProfile(IccProfileClass.MultiplexIdentification, IccColorSpaceSignatures.Gray,
            new[] { "A", "B" }, MatrixElement(1, 2, new[] { 1.0, 0.0, 2.0, 0.0 }));
        source.Header.RequiresMcsSubset = true;
        IccProfile destination = VisualizationProfile(new[] { "A", "C" }, new double[0], IccTagSignatures.MToB0, IccColorSpaceSignatures.Xyz,
            MatrixElement(2, 3, IdentityTwoToThree()));

        IccTransformException? exception = Assert.Throws<IccTransformException>(() => McsConnectionCompiler.Compile(source, destination, IccInterpolationKind.Multilinear));

        Assert.That(exception!.Message, Does.Contain("source channel 'B'").And.Contain("absent from the destination"));
    }

    [Test]
    public void DestinationSubsetFlagRequiresEveryDestinationNameInSource()
    {
        IccProfile source = SourceProfile(IccProfileClass.MultiplexIdentification, IccColorSpaceSignatures.Gray,
            new[] { "A", "B" }, MatrixElement(1, 2, new[] { 1.0, 0.0, 2.0, 0.0 }));
        IccProfile destination = VisualizationProfile(new[] { "B", "C" }, new double[0], IccTagSignatures.MToB0, IccColorSpaceSignatures.Xyz,
            MatrixElement(2, 3, IdentityTwoToThree()));
        destination.Header.RequiresMcsSubset = true;

        IccTransformException? exception = Assert.Throws<IccTransformException>(() => McsConnectionCompiler.Compile(source, destination, IccInterpolationKind.Multilinear));

        Assert.That(exception!.Message, Does.Contain("destination profile").And.Contain("source channel 'C' is absent"));
    }

    [Test]
    public void BothSubsetFlagsAllowEquivalentSetsInDifferentOrders()
    {
        IccProfile source = SourceProfile(IccProfileClass.MultiplexIdentification, IccColorSpaceSignatures.Gray,
            new[] { "A", "B" }, MatrixElement(1, 2, new[] { 1.0, 0.0, 2.0, 0.0 }));
        source.Header.RequiresMcsSubset = true;
        IccProfile destination = VisualizationProfile(new[] { "B", "A" }, new double[0], IccTagSignatures.MToB0, IccColorSpaceSignatures.Xyz,
            MatrixElement(2, 3, IdentityTwoToThree()));
        destination.Header.RequiresMcsSubset = true;

        McsConnectionPlan plan = McsConnectionCompiler.Compile(source, destination, IccInterpolationKind.Multilinear);

        Assert.That(plan.ChannelOrder, Is.EqualTo(new[] { 1, 0 }));
    }

    [Test]
    public void ConnectionRejectsInvalidClassMissingNamesAndDuplicateConstructedNames()
    {
        IccProfile source = SourceProfile(IccProfileClass.MultiplexIdentification, IccColorSpaceSignatures.Gray,
            new[] { "A" }, MatrixElement(1, 1, new[] { 1.0, 0.0 }));
        IccProfile destination = VisualizationProfile(new[] { "A" }, new double[0], IccTagSignatures.MToB0, IccColorSpaceSignatures.Xyz,
            MatrixElement(1, 3, new[] { 1.0, 0.0, 1.0, 0.0, 1.0, 0.0 }));
        IccProfile wrongClass = VisualizationProfile(new[] { "A" }, new double[0], IccTagSignatures.MToB0, IccColorSpaceSignatures.Xyz,
            MatrixElement(1, 3, new[] { 1.0, 0.0, 1.0, 0.0, 1.0, 0.0 }));
        wrongClass.Header.ProfileClass = IccProfileClass.Output;
        IccProfile missingNames = VisualizationProfile(new[] { "A" }, new double[0], IccTagSignatures.MToB0, IccColorSpaceSignatures.Xyz,
            MatrixElement(1, 3, new[] { 1.0, 0.0, 1.0, 0.0, 1.0, 0.0 }));
        missingNames.Tags = missingNames.Tags.Where(tag => tag.Signature != IccTagSignatures.MultiplexTypeArray).ToArray();
        IccProfile duplicates = VisualizationProfile(new[] { "A", "B" }, new double[0], IccTagSignatures.MToB0, IccColorSpaceSignatures.Xyz,
            MatrixElement(2, 3, IdentityTwoToThree()));
        duplicates.Tags[0].Data.MultiplexChannels!.Names[1] = "A";

        Assert.Multiple(() =>
        {
            Assert.Throws<IccTransformException>(() => McsConnectionCompiler.Compile(source, wrongClass, IccInterpolationKind.Multilinear));
            Assert.Throws<IccTransformException>(() => McsConnectionCompiler.Compile(source, missingNames, IccInterpolationKind.Multilinear));
            Assert.Throws<IccTransformException>(() => McsConnectionCompiler.Compile(source, duplicates, IccInterpolationKind.Multilinear));
        });
    }

    private static IccProfile SourceProfile(IccProfileClass profileClass, uint dataSpace, string[] names, IccProcessingElement transform)
    {
        IccProfile profile = Profile(profileClass, dataSpace, 0, names.Length);
        profile.Tags = new[]
        {
            NamesTag(names),
            TransformTag(IccTagSignatures.AToM0, transform.InputChannels, transform.OutputChannels, transform)
        };
        return profile;
    }

    private static IccProfile LinkProfile(uint dataSpace, string[] names, IccProcessingElement transform)
    {
        IccProfile profile = Profile(IccProfileClass.MultiplexLink, dataSpace, 0, names.Length);
        profile.Tags = new[]
        {
            NamesTag(names),
            TransformTag(IccTagSignatures.MToA0, transform.InputChannels, transform.OutputChannels, transform)
        };
        return profile;
    }

    private static IccProfile VisualizationProfile(string[] names, double[] defaults, uint transformSignature, uint pcs, IccProcessingElement transform)
    {
        IccProfile profile = Profile(IccProfileClass.MultiplexVisualization, IccColorSpaceSignatures.Rgb, pcs, names.Length);
        IccTag transformTag = TransformTag(transformSignature, transform.InputChannels, transform.OutputChannels, transform);
        if (defaults.Length == 0)
        {
            profile.Tags = new[] { NamesTag(names), transformTag };
        }
        else
        {
            profile.Tags = new[] { NamesTag(names), DefaultsTag(defaults), transformTag };
        }
        return profile;
    }

    private static IccProfile Profile(IccProfileClass profileClass, uint dataSpace, uint pcs, int mcsChannels)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = IccVersionKind.Version5;
        profile.Header.ProfileClass = profileClass;
        profile.Header.DataColorSpaceSignature = dataSpace;
        profile.Header.ProfileConnectionSpaceSignature = pcs;
        profile.Header.MaterialConnectionSpaceSignature = IccColorSpaceSignatures.MultiplexPrefix | (uint)mcsChannels;
        profile.Header.MaterialConnectionSpace = IccColorSpaceKind.Multiplex;
        return profile;
    }

    private static IccTag NamesTag(string[] names)
    {
        return new IccTag
        {
            Signature = IccTagSignatures.MultiplexTypeArray,
            Data = new IccTagData
            {
                Kind = IccTagDataKind.TagArray,
                MultiplexChannels = new IccMultiplexChannels { Names = names, Order = Enumerable.Range(0, names.Length).ToArray() }
            }
        };
    }

    private static IccTag DefaultsTag(double[] defaults)
    {
        return new IccTag
        {
            Signature = IccTagSignatures.MultiplexDefaultValues,
            Data = new IccTagData
            {
                Kind = IccTagDataKind.FloatingPointArray,
                MultiplexChannels = new IccMultiplexChannels { DefaultValues = defaults, Order = Enumerable.Range(0, defaults.Length).ToArray() }
            }
        };
    }

    private static IccTag TransformTag(uint signature, int inputs, int outputs, IccProcessingElement element)
    {
        return new IccTag
        {
            Signature = signature,
            Data = new IccTagData
            {
                Kind = IccTagDataKind.MultiProcessElements,
                RawData = new IccRawData { HasTypeSignature = true, TypeSignature = IccTypeSignatures.MultiProcessElements },
                MultiProcessElements = new IccMultiProcessElements
                {
                    InputChannels = inputs,
                    OutputChannels = outputs,
                    InputRanges = Ranges(inputs),
                    OutputRanges = Ranges(outputs),
                    Elements = new[] { element },
                    IsExecutable = true
                }
            }
        };
    }

    private static IccProcessingElement MatrixElement(int inputs, int outputs, double[] values)
    {
        return new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Matrix,
            Kind = IccProcessingElementKind.Matrix,
            InputChannels = inputs,
            OutputChannels = outputs,
            Matrix = new IccMatrix { Rows = outputs, Columns = inputs + 1, Values = values }
        };
    }

    private static IccEncodingRange[] Ranges(int count)
    {
        return Enumerable.Range(0, count).Select(index => new IccEncodingRange { Minimum = float.MinValue, Maximum = float.MaxValue }).ToArray();
    }

    private static double[] IdentityTwoToThree()
    {
        return new[]
        {
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            1.0, 1.0, 0.0
        };
    }

    private static double[] Execute(McsConnectionPlan plan, double[] input)
    {
        int values = PipelineExecutor.GetRequiredValueScratchLength(plan.Stages);
        int indices = PipelineExecutor.GetRequiredClutIndexScratchLength(plan.Stages);
        int weights = PipelineExecutor.GetRequiredClutWeightScratchLength(plan.Stages);
        double[] output = new double[plan.DestinationChannels];
        PipelineExecutor.ExecutePipeline(plan.Stages, input, output, new double[values], new double[values], new int[indices], new double[weights]);
        return output;
    }
}
