using icc;

namespace icc.Tests;

public class DeviceLinkTransformCompilerTests
{
    [Test]
    public void AToB0ExecutesDirectlyBetweenHeaderSpacesAndSnapshotsSequenceMetadata()
    {
        IccProfileSequenceEntry entry = new IccProfileSequenceEntry { DeviceModelSignature = 0x4d4f444c };
        IccProfileSequenceIdentifier identifier = new IccProfileSequenceIdentifier();
        identifier.ProfileId.IsPresent = true;
        identifier.ProfileId.Bytes[0] = 0x5a;
        IccProfile profile = DeviceLink(IccVersionKind.Version2, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Cmyk, IccRenderingIntent.Saturation,
            ClassicAToB0(0.25), SequenceTag(entry), IdentifierTag(identifier));
        IccTransformOptions options = Options(IccRenderingIntent.Perceptual, IccIntentFallback.AllowAny);

        IccColorTransform transform = (IccColorTransform)new IccTransformFactory().CreateDeviceLinkTransform(profile, options);
        entry.DeviceModelSignature = 0;
        identifier.ProfileId.Bytes[0] = 0;
        options.ClipOutput = false;
        float[] result = transform.TransformSingleColor(new float[] { 0.2f, 0.4f, 0.6f });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new float[] { 0.2f, 0.4f, 0.6f, 0.25f }).Within(0.000002f));
            Assert.That(transform.GetSourceChannelCount(), Is.EqualTo(3));
            Assert.That(transform.GetDestinationChannelCount(), Is.EqualTo(4));
            Assert.That(transform.Core.Plan.SourceColorSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Rgb));
            Assert.That(transform.Core.Plan.DestinationColorSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Cmyk));
            Assert.That(transform.Core.Plan.ProfileSequence[0].DeviceModelSignature, Is.EqualTo(0x4d4f444c));
            Assert.That(transform.Core.Plan.ProfileSequenceIdentifiers[0].ProfileId.Bytes[0], Is.EqualTo(0x5a));
            Assert.That(transform.Core.Options.ClipOutput, Is.True);
            Assert.That(transform.Core.Stages.Any(stage => stage.Kind == PipelineStageKind.PcsNormalize
                || stage.Kind == PipelineStageKind.PcsDenormalize), Is.False);
            Assert.That(transform.Core.Plan.Stages.All(stage => stage.TagSignature == IccTagSignatures.AToB0), Is.True);
            Assert.That(transform.Core.Plan.Stages.All(stage => stage.Name.Contains("Saturation fallback")), Is.True);
        });
    }

    [Test]
    public void ExecutableDToB0TakesPrecedenceAndUnsupportedDToB0FallsBackToAToB0()
    {
        IccTag dToB = MpeDToB0(0.75, true);
        IccProfile profile = DeviceLink(IccVersionKind.Version4, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Cmyk, IccRenderingIntent.Perceptual, ClassicAToB0(0.25), dToB);
        IccTransformFactory factory = new IccTransformFactory();
        IccTransformOptions options = Options(IccRenderingIntent.Perceptual, IccIntentFallback.None);

        IccColorTransform preferred = (IccColorTransform)factory.CreateDeviceLinkTransform(profile, options);
        float[] preferredResult = preferred.TransformSingleColor(new float[] { 0.2f, 0.4f, 0.6f });
        dToB.Data.MultiProcessElements!.IsExecutable = false;
        IccColorTransform fallback = (IccColorTransform)factory.CreateDeviceLinkTransform(profile, options);
        float[] fallbackResult = fallback.TransformSingleColor(new float[] { 0.2f, 0.4f, 0.6f });

        Assert.Multiple(() =>
        {
            Assert.That(preferredResult, Is.EqualTo(new float[] { 0.2f, 0.4f, 0.6f, 0.75f }).Within(0.000002f));
            Assert.That(preferred.Core.Plan.Stages.All(stage => stage.TagSignature == IccTagSignatures.DToB0), Is.True);
            Assert.That(fallbackResult, Is.EqualTo(new float[] { 0.2f, 0.4f, 0.6f, 0.25f }).Within(0.000002f));
            Assert.That(fallback.Core.Plan.Stages.All(stage => stage.TagSignature == IccTagSignatures.AToB0), Is.True);
        });
    }

    [Test]
    public void DeviceLinkCompilationRejectsIntentAndHeaderOrPipelineSpaceMismatches()
    {
        IccProfile valid = DeviceLink(IccVersionKind.Version4, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Cmyk, IccRenderingIntent.Saturation, ClassicAToB0(0.25));
        IccProfile wrongOutputChannels = DeviceLink(IccVersionKind.Version4, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Rgb, IccRenderingIntent.Perceptual, ClassicAToB0(0.25));
        IccProfile unknownOutput = DeviceLink(IccVersionKind.Version4, IccColorSpaceSignatures.Rgb,
            0x42414420, IccRenderingIntent.Perceptual, ClassicAToB0(0.25));
        IccProfile wrongClass = DeviceLink(IccVersionKind.Version4, IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Cmyk, IccRenderingIntent.Perceptual, ClassicAToB0(0.25));
        wrongClass.Header.ProfileClass = IccProfileClass.Output;
        IccTransformOptions blackPoint = Options(IccRenderingIntent.Saturation, IccIntentFallback.None);
        blackPoint.BlackPointCompensation = true;
        IccTransformFactory factory = new IccTransformFactory();

        Assert.Multiple(() =>
        {
            Assert.That(() => factory.CreateDeviceLinkTransform(valid,
                Options(IccRenderingIntent.Perceptual, IccIntentFallback.None)),
                Throws.TypeOf<IccTransformException>().With.Message.Contains("different rendering intent"));
            Assert.That(() => factory.CreateDeviceLinkTransform(wrongOutputChannels,
                Options(IccRenderingIntent.Perceptual, IccIntentFallback.None)),
                Throws.TypeOf<IccTransformException>().With.Message.Contains("require 3-to-3"));
            Assert.That(() => factory.CreateDeviceLinkTransform(unknownOutput,
                Options(IccRenderingIntent.Perceptual, IccIntentFallback.None)),
                Throws.TypeOf<IccTransformException>().With.Message.Contains("supported input and output"));
            Assert.That(() => factory.CreateDeviceLinkTransform(wrongClass,
                Options(IccRenderingIntent.Perceptual, IccIntentFallback.None)),
                Throws.TypeOf<IccTransformException>().With.Message.Contains("DeviceLink profile class"));
            Assert.That(() => factory.CreateDeviceLinkTransform(valid, blackPoint),
                Throws.TypeOf<IccTransformException>().With.Message.Contains("pre-evaluated DeviceLink"));
        });
    }

    private static IccTransformOptions Options(IccRenderingIntent intent, IccIntentFallback fallback)
    {
        IccTransformOptions options = new IccTransformOptions();
        options.RenderingIntent = intent;
        options.IntentFallback = fallback;
        options.Interpolation = IccInterpolationKind.Multilinear;
        options.ClipOutput = true;
        return options;
    }

    private static IccProfile DeviceLink(IccVersionKind version, uint inputSpace, uint outputSpace,
        IccRenderingIntent intent, params IccTag[] tags)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = version;
        profile.Header.ProfileClass = IccProfileClass.DeviceLink;
        profile.Header.DataColorSpaceSignature = inputSpace;
        profile.Header.ProfileConnectionSpace = IccPcsKind.None;
        profile.Header.ProfileConnectionSpaceSignature = outputSpace;
        profile.Header.RenderingIntentKind = intent;
        profile.Tags = tags;
        return profile;
    }

    private static IccTag ClassicAToB0(double fourthOutput)
    {
        IccLut lut = new IccLut();
        lut.InputChannels = 3;
        lut.OutputChannels = 4;
        lut.PcsEncoding = IccPcsEncodingKind.Version2;
        lut.Matrix = new IccMatrix
        {
            Rows = 3,
            Columns = 3,
            Values = new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 }
        };
        lut.InputTables = new double[][] { new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 } };
        lut.OutputTables = new double[][]
        {
            new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 },
            new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }
        };
        lut.Clut = new IccClut();
        lut.Clut.InputChannels = 3;
        lut.Clut.OutputChannels = 4;
        lut.Clut.GridPoints = new int[] { 2, 2, 2 };
        lut.Clut.Values = DeviceLinkClutValues(fourthOutput);
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Lut;
        data.Lut = lut;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.Lut16;
        return new IccTag { Signature = IccTagSignatures.AToB0, Data = data };
    }

    private static double[] DeviceLinkClutValues(double fourthOutput)
    {
        double[] values = new double[32];
        int first = 0;
        int offset = 0;
        while (first < 2)
        {
            int second = 0;
            while (second < 2)
            {
                int third = 0;
                while (third < 2)
                {
                    values[offset] = first;
                    values[offset + 1] = second;
                    values[offset + 2] = third;
                    values[offset + 3] = fourthOutput;
                    offset = offset + 4;
                    third = third + 1;
                }
                second = second + 1;
            }
            first = first + 1;
        }
        return values;
    }

    private static IccTag MpeDToB0(double fourthOutput, bool executable)
    {
        IccProcessingElement element = new IccProcessingElement();
        element.Signature = IccProcessingElementSignatures.Matrix;
        element.Kind = IccProcessingElementKind.Matrix;
        element.InputChannels = 3;
        element.OutputChannels = 4;
        element.Matrix = new IccMatrix
        {
            Rows = 4,
            Columns = 4,
            Values = new double[]
            {
                1.0, 0.0, 0.0, 0.0,
                0.0, 1.0, 0.0, 0.0,
                0.0, 0.0, 1.0, 0.0,
                0.0, 0.0, 0.0, fourthOutput
            }
        };
        IccMultiProcessElements pipeline = new IccMultiProcessElements();
        pipeline.InputChannels = 3;
        pipeline.OutputChannels = 4;
        pipeline.InputRanges = Ranges(3);
        pipeline.OutputRanges = Ranges(4);
        pipeline.Elements = new IccProcessingElement[] { element };
        pipeline.IsExecutable = executable;
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.MultiProcessElements;
        data.MultiProcessElements = pipeline;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.MultiProcessElements;
        return new IccTag { Signature = IccTagSignatures.DToB0, Data = data };
    }

    private static IccEncodingRange[] Ranges(int count)
    {
        IccEncodingRange[] ranges = new IccEncodingRange[count];
        int index = 0;
        while (index < count)
        {
            ranges[index] = new IccEncodingRange { Minimum = 0.0, Maximum = 1.0 };
            index = index + 1;
        }
        return ranges;
    }

    private static IccTag SequenceTag(IccProfileSequenceEntry entry)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.ProfileSequence;
        data.ProfileSequence = new IccProfileSequenceEntry[] { entry };
        return new IccTag { Signature = IccTagSignatures.ProfileSequenceDescription, Data = data };
    }

    private static IccTag IdentifierTag(IccProfileSequenceIdentifier identifier)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.ProfileSequence;
        data.ProfileSequenceIdentifiers = new IccProfileSequenceIdentifier[] { identifier };
        return new IccTag { Signature = IccTagSignatures.ProfileSequenceIdentifier, Data = data };
    }
}
