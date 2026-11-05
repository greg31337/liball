using icc;

namespace icc.Tests;

public class ProofingTransformCompilerTests
{
    [Test]
    public void ProofingChainUsesRenderingAndProofingIntentsAtTheirDistinctProfileStages()
    {
        IccProfile source = Profile(MpeTag(IccTagSignatures.DToB0, 2.0));
        IccProfile proof = Profile(MpeTag(IccTagSignatures.BToD0, 3.0), MpeTag(IccTagSignatures.DToB1, 5.0));
        IccProfile destination = Profile(MpeTag(IccTagSignatures.BToD1, 7.0));

        IccColorTransform transform = (IccColorTransform)new IccTransformFactory().CreateProofingTransform(
            source, proof, destination, IccRenderingIntent.Perceptual, IccRenderingIntent.MediaRelativeColorimetric);
        float[] result = transform.TransformSingleColor(new float[] { 0.001f, 0.002f, 0.003f });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new float[] { 0.21f, 0.42f, 0.63f }).Within(0.000002f));
            Assert.That(transform.Core.Plan.Stages.Select(stage => stage.TagSignature), Is.EqualTo(new uint[]
            {
                IccTagSignatures.DToB0,
                IccTagSignatures.BToD0,
                IccTagSignatures.DToB1,
                IccTagSignatures.BToD1
            }));
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.Name.StartsWith("source:")), Is.True);
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.Name.StartsWith("proof rendering:")), Is.True);
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.Name.StartsWith("proof simulation:")), Is.True);
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.Name.StartsWith("destination:")), Is.True);
            Assert.That(transform.Core.Plan.ProofingOptions!.RenderingIntent, Is.EqualTo(IccRenderingIntent.Perceptual));
            Assert.That(transform.Core.Plan.ProofingOptions.ProofingIntent, Is.EqualTo(IccRenderingIntent.MediaRelativeColorimetric));
        });
    }

    [Test]
    public void GamutTagOverridesOutOfGamutResultsWithSnapshottedAlarmForSingleAndInterleavedColors()
    {
        IccProfile source = Profile(MpeTag(IccTagSignatures.DToB0, 2.0));
        IccProfile proof = Profile(MpeTag(IccTagSignatures.BToD0, 3.0), MpeTag(IccTagSignatures.DToB1, 5.0), GamutTag(1.0));
        IccProfile destination = Profile(MpeTag(IccTagSignatures.BToD1, 7.0));
        IccProofingOptions options = Options(true, new double[] { 0.9, 0.1, 0.8 });
        IccColorTransform transform = (IccColorTransform)new IccTransformFactory()
            .CreateConfiguredProofingTransform(source, proof, destination, options);

        options.AlarmColor[0] = 0.0;
        options.EnableGamutCheck = false;
        float[] single = transform.TransformSingleColor(new float[] { 0.001f, 0.002f, 0.003f });
        IccGamutStatus singleStatus = transform.Core.LastGamutStatus;
        float[] interleaved = new float[6];
        transform.TransformInterleavedColors(new float[] { 0.001f, 0.002f, 0.003f, 0.002f, 0.001f, 0.003f },
            3, interleaved, 3, 2);

        Assert.Multiple(() =>
        {
            Assert.That(single, Is.EqualTo(new float[] { 0.9f, 0.1f, 0.8f }).Within(0.000001f));
            Assert.That(singleStatus, Is.EqualTo(IccGamutStatus.Outside));
            Assert.That(interleaved, Is.EqualTo(new float[] { 0.9f, 0.1f, 0.8f, 0.9f, 0.1f, 0.8f }).Within(0.000001f));
            Assert.That(transform.Core.LastGamutStatus, Is.EqualTo(IccGamutStatus.Outside));
            Assert.That(transform.Core.Plan.ProofingOptions!.EnableGamutCheck, Is.True);
            Assert.That(transform.Core.Plan.ProofingOptions.AlarmColor, Is.EqualTo(new double[] { 0.9, 0.1, 0.8 }));
            Assert.That(transform.Core.Plan.GamutStages.Any(stage => stage.TagSignature == IccTagSignatures.Gamut), Is.True);
        });
    }

    [Test]
    public void ZeroGamutTagReportsInsideAndLeavesProofedColorUnchanged()
    {
        IccProfile source = Profile(MpeTag(IccTagSignatures.DToB0, 2.0));
        IccProfile proof = Profile(MpeTag(IccTagSignatures.BToD0, 3.0), MpeTag(IccTagSignatures.DToB1, 5.0), GamutTag(0.0));
        IccProfile destination = Profile(MpeTag(IccTagSignatures.BToD1, 7.0));
        IccColorTransform transform = (IccColorTransform)new IccTransformFactory().CreateConfiguredProofingTransform(
            source, proof, destination, Options(true, new double[] { 1.0, 0.0, 1.0 }));

        float[] result = transform.TransformSingleColor(new float[] { 0.001f, 0.002f, 0.003f });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new float[] { 0.21f, 0.42f, 0.63f }).Within(0.000002f));
            Assert.That(transform.Core.LastGamutStatus, Is.EqualTo(IccGamutStatus.Inside));
        });
    }

    [Test]
    public void EnabledGamutCheckingRequiresTagAndDestinationSizedFiniteAlarm()
    {
        IccProfile source = Profile(MpeTag(IccTagSignatures.DToB0, 1.0));
        IccProfile proofWithoutGamut = Profile(MpeTag(IccTagSignatures.BToD0, 1.0), MpeTag(IccTagSignatures.DToB1, 1.0));
        IccProfile proofWithGamut = Profile(MpeTag(IccTagSignatures.BToD0, 1.0), MpeTag(IccTagSignatures.DToB1, 1.0), GamutTag(0.0));
        IccProfile destination = Profile(MpeTag(IccTagSignatures.BToD1, 1.0));
        IccTransformFactory factory = new IccTransformFactory();

        Assert.Multiple(() =>
        {
            Assert.That(() => factory.CreateConfiguredProofingTransform(source, proofWithoutGamut, destination,
                Options(true, new double[] { 1.0, 0.0, 1.0 })), Throws.TypeOf<IccTransformException>().With.Message.Contains("gamut tag"));
            Assert.That(() => factory.CreateConfiguredProofingTransform(source, proofWithGamut, destination,
                Options(true, new double[] { 1.0, 0.0 })), Throws.TypeOf<IccTransformException>().With.Message.Contains("exactly 3"));
            Assert.That(() => factory.CreateConfiguredProofingTransform(source, proofWithGamut, destination,
                Options(true, new double[] { 1.0, double.NaN, 1.0 })), Throws.TypeOf<IccTransformException>().With.Message.Contains("must be finite"));
        });
    }

    private static IccProofingOptions Options(bool gamut, double[] alarm)
    {
        IccProofingOptions options = new IccProofingOptions();
        options.RenderingIntent = IccRenderingIntent.Perceptual;
        options.ProofingIntent = IccRenderingIntent.MediaRelativeColorimetric;
        options.IntentFallback = IccIntentFallback.None;
        options.Interpolation = IccInterpolationKind.Multilinear;
        options.EnableGamutCheck = gamut;
        options.AlarmColor = alarm;
        options.ClipOutput = true;
        return options;
    }

    private static IccProfile Profile(params IccTag[] tags)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = IccVersionKind.Version4;
        profile.Header.ProfileClass = IccProfileClass.Display;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        profile.Tags = tags;
        return profile;
    }

    private static IccTag MpeTag(uint signature, double factor)
    {
        IccProcessingElement element = new IccProcessingElement();
        element.Signature = IccProcessingElementSignatures.Matrix;
        element.Kind = IccProcessingElementKind.Matrix;
        element.InputChannels = 3;
        element.OutputChannels = 3;
        element.Matrix = new IccMatrix
        {
            Rows = 3,
            Columns = 4,
            Values = new double[]
            {
                factor, 0.0, 0.0, 0.0,
                0.0, factor, 0.0, 0.0,
                0.0, 0.0, factor, 0.0
            }
        };
        IccMultiProcessElements pipeline = new IccMultiProcessElements();
        pipeline.InputChannels = 3;
        pipeline.OutputChannels = 3;
        pipeline.InputRanges = Ranges(3, 0.0, 1000.0);
        pipeline.OutputRanges = Ranges(3, 0.0, 1000.0);
        pipeline.Elements = new IccProcessingElement[] { element };
        pipeline.IsExecutable = true;
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.MultiProcessElements;
        data.MultiProcessElements = pipeline;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.MultiProcessElements;
        return new IccTag { Signature = signature, Data = data };
    }

    private static IccTag GamutTag(double value)
    {
        IccLut lut = new IccLut();
        lut.InputChannels = 3;
        lut.OutputChannels = 1;
        lut.PcsEncoding = IccPcsEncodingKind.Version2;
        lut.Matrix = new IccMatrix
        {
            Rows = 3,
            Columns = 3,
            Values = new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 }
        };
        lut.InputTables = new double[][] { new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 } };
        lut.OutputTables = new double[][] { new double[] { 0.0, 1.0 } };
        lut.Clut = new IccClut
        {
            InputChannels = 3,
            OutputChannels = 1,
            GridPoints = new int[] { 2, 2, 2 },
            Values = new double[] { value, value, value, value, value, value, value, value }
        };
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Lut;
        data.Lut = lut;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.Lut16;
        return new IccTag { Signature = IccTagSignatures.Gamut, Data = data };
    }

    private static IccEncodingRange[] Ranges(int count, double minimum, double maximum)
    {
        IccEncodingRange[] ranges = new IccEncodingRange[count];
        int index = 0;
        while (index < count)
        {
            ranges[index] = new IccEncodingRange { Minimum = minimum, Maximum = maximum };
            index = index + 1;
        }
        return ranges;
    }
}
