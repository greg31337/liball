using icc;

namespace icc.Tests;

public class ProfileLinkerTests
{
    [Test]
    public void SimpleLinkUsesDeterministicVersionFourMpeDefaults()
    {
        IccProfile source = GrayProfile(1.0, "Source", 0, 0, 0);
        IccProfile destination = GrayProfile(1.0, "Destination", 0, 0, 0);
        IccWriteOptions writeOptions = new IccWriteOptions();
        IccProfileLinker linker = new IccProfileLinker();

        IccProfile first = linker.LinkProfileSequence(new IccProfile[] { source, destination },
            IccRenderingIntent.MediaRelativeColorimetric, writeOptions);
        IccProfile second = linker.LinkProfileSequence(new IccProfile[] { source, destination },
            IccRenderingIntent.MediaRelativeColorimetric, writeOptions);
        IccClut clut = FindTag(first, IccTagSignatures.DToB0).Data.MultiProcessElements!.Elements[0].Clut!;

        Assert.Multiple(() =>
        {
            Assert.That(first.Header.Version.Kind, Is.EqualTo(IccVersionKind.Version4));
            Assert.That(clut.GridPoints, Is.EqualTo(new int[] { 17 }));
            Assert.That(first.OriginalBytes, Is.EqualTo(second.OriginalBytes));
        });
    }

    [Test]
    public void ConfiguredMpeLinkUsesSelectedGridAndPreservesIntentSequenceAndDeterministicBytes()
    {
        IccProfile source = GrayProfile(2.0, "Source profile", 0x534f5552, 0x5343414e, 0x11);
        IccProfile destination = GrayProfile(1.0, "Destination profile", 0x44455354, 0x5052494e, 0x22);
        IccProfileLinkOptions linkOptions = new IccProfileLinkOptions();
        linkOptions.RenderingIntent = IccRenderingIntent.MediaRelativeColorimetric;
        linkOptions.GridPointCount = 5;
        linkOptions.EncodingTypeSignature = IccTypeSignatures.MultiProcessElements;
        linkOptions.Interpolation = IccInterpolationKind.Multilinear;
        linkOptions.Descriptions = new IccLocalizedString[]
        {
            new IccLocalizedString { Language = "fr", Country = "FR", Text = "Lien gris" },
            new IccLocalizedString { Language = "en", Country = "US", Text = "Gray link" }
        };
        IccWriteOptions writeOptions = new IccWriteOptions();
        IIccProfileLinker linker = new IccProfileLinker();

        IccProfile linked = linker.LinkConfiguredProfileSequence(
            new IccProfile[] { source, destination }, linkOptions, writeOptions);
        IccProfile repeated = linker.LinkConfiguredProfileSequence(
            new IccProfile[] { source, destination }, linkOptions, writeOptions);
        IccTag transformTag = FindTag(linked, IccTagSignatures.DToB0);
        IccTag sequenceTag = FindTag(linked, IccTagSignatures.ProfileSequenceDescription);
        IccTag identifierTag = FindTag(linked, IccTagSignatures.ProfileSequenceIdentifier);
        IccMultiProcessElements pipeline = transformTag.Data.MultiProcessElements!;
        IccClut clut = pipeline.Elements[0].Clut!;
        IccTransformOptions transformOptions = TransformOptions(IccRenderingIntent.MediaRelativeColorimetric);
        IIccColorTransform original = new IccTransformFactory().CreateProfileSequenceTransform(
            new IccProfile[] { source, destination }, IccRenderingIntent.MediaRelativeColorimetric, IccIntentFallback.None);
        IIccColorTransform baked = new IccTransformFactory().CreateDeviceLinkTransform(linked, transformOptions);

        Assert.Multiple(() =>
        {
            Assert.That(linked.Header.ProfileClass, Is.EqualTo(IccProfileClass.DeviceLink));
            Assert.That(linked.Header.DataColorSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Gray));
            Assert.That(linked.Header.ProfileConnectionSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Gray));
            Assert.That(linked.Header.RenderingIntentKind, Is.EqualTo(IccRenderingIntent.MediaRelativeColorimetric));
            Assert.That(transformTag.Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.MultiProcessElements));
            Assert.That(clut.GridPoints, Is.EqualTo(new int[] { 5 }));
            Assert.That(clut.Values, Has.Length.EqualTo(5));
            Assert.That(sequenceTag.Data.ProfileSequence, Has.Length.EqualTo(2));
            Assert.That(sequenceTag.Data.ProfileSequence[0].DeviceManufacturerSignature, Is.EqualTo(0x534f5552));
            Assert.That(sequenceTag.Data.ProfileSequence[1].TechnologySignature, Is.EqualTo(IccTechnologySignatures.InkJetPrinter));
            Assert.That(identifierTag.Data.ProfileSequenceIdentifiers[0].ProfileId.Bytes[0], Is.EqualTo(0x11));
            Assert.That(identifierTag.Data.ProfileSequenceIdentifiers[1].Descriptions[0].Text, Is.EqualTo("Destination profile"));
            Assert.That(FindTag(linked, IccTagSignatures.ProfileDescription).Data.LocalizedStrings.Select(value => value.Text),
                Is.EqualTo(new string[] { "en", "fr" }.Select(language => language == "en" ? "Gray link" : "Lien gris")));
            Assert.That(baked.TransformSingleColor(new float[] { 0.5f })[0],
                Is.EqualTo(original.TransformSingleColor(new float[] { 0.5f })[0]).Within(0.000001f));
            Assert.That(repeated.OriginalBytes, Is.EqualTo(linked.OriginalBytes));
        });
    }

    [Test]
    public void ConfiguredLinkRetainsRequestedIntentAfterColorimetricFallback()
    {
        IccProfile source = GrayProfile(1.0, "Source", 0, 0, 0);
        IccProfile destination = GrayProfile(1.0, "Destination", 0, 0, 0);
        IccProfileLinkOptions options = new IccProfileLinkOptions();
        options.RenderingIntent = IccRenderingIntent.Saturation;
        options.IntentFallback = IccIntentFallback.AllowColorimetric;
        options.GridPointCount = 3;
        options.Interpolation = IccInterpolationKind.Linear;

        IccProfile linked = new IccProfileLinker().LinkConfiguredProfileSequence(
            new IccProfile[] { source, destination }, options, new IccWriteOptions());
        IIccColorTransform baked = new IccTransformFactory().CreateDeviceLinkTransform(linked,
            TransformOptions(IccRenderingIntent.Saturation));

        Assert.Multiple(() =>
        {
            Assert.That(linked.Header.RenderingIntent, Is.EqualTo((int)IccRenderingIntent.Saturation));
            Assert.That(linked.Header.RenderingIntentKind, Is.EqualTo(IccRenderingIntent.Saturation));
            Assert.That(baked.TransformSingleColor(new float[] { 0.4f }),
                Is.EqualTo(new float[] { 0.4f }).Within(0.000002f));
        });
    }

    [Test]
    public void ConfiguredClassicLinkUsesSelectedLutEncodingAndCanOmitSequenceMetadata()
    {
        IccProfile source = GrayProfile(1.0, "Source", 0, 0, 0);
        IccProfile destination = GrayProfile(1.0, "Destination", 0, 0, 0);
        IccProfileLinkOptions options = new IccProfileLinkOptions();
        options.RenderingIntent = IccRenderingIntent.MediaRelativeColorimetric;
        options.Version.Kind = IccVersionKind.Version2;
        options.Version.Major = 2;
        options.GridPointCount = 3;
        options.EncodingTypeSignature = IccTypeSignatures.Lut8;
        options.Interpolation = IccInterpolationKind.Linear;
        options.IncludeProfileSequence = false;

        IccProfile linked = new IccProfileLinker().LinkConfiguredProfileSequence(
            new IccProfile[] { source, destination }, options, new IccWriteOptions());
        IccTag transform = FindTag(linked, IccTagSignatures.AToB0);
        IccTransformOptions transformOptions = TransformOptions(IccRenderingIntent.MediaRelativeColorimetric);
        transformOptions.Interpolation = IccInterpolationKind.Linear;
        float[] result = new IccTransformFactory().CreateDeviceLinkTransform(linked, transformOptions)
            .TransformSingleColor(new float[] { 0.3f });

        Assert.Multiple(() =>
        {
            Assert.That(linked.Header.Version.Kind, Is.EqualTo(IccVersionKind.Version2));
            Assert.That(transform.Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.Lut8));
            Assert.That(transform.Data.Lut!.Clut.GridPoints, Is.EqualTo(new int[] { 3 }));
            Assert.That(linked.Tags.Any(tag => tag.Signature == IccTagSignatures.ProfileSequenceDescription), Is.False);
            Assert.That(linked.Tags.Any(tag => tag.Signature == IccTagSignatures.ProfileSequenceIdentifier), Is.False);
            Assert.That(result[0], Is.EqualTo(0.3f).Within(0.005f));
        });
    }

    [Test]
    public void ConfiguredLut16LinkPreservesSelectedEncodingGridAndGoldenSamples()
    {
        IccProfile source = GrayProfile(1.0, "Source", 0, 0, 0);
        IccProfile destination = GrayProfile(1.0, "Destination", 0, 0, 0);
        IccProfileLinkOptions options = new IccProfileLinkOptions();
        options.RenderingIntent = IccRenderingIntent.MediaRelativeColorimetric;
        options.Version.Kind = IccVersionKind.Version2;
        options.Version.Major = 2;
        options.GridPointCount = 7;
        options.EncodingTypeSignature = IccTypeSignatures.Lut16;
        options.Interpolation = IccInterpolationKind.Linear;

        IccProfile linked = new IccProfileLinker().LinkConfiguredProfileSequence(
            new IccProfile[] { source, destination }, options, new IccWriteOptions());
        IccTag transformTag = FindTag(linked, IccTagSignatures.AToB0);
        IIccColorTransform transform = new IccTransformFactory().CreateDeviceLinkTransform(linked,
            TransformOptions(IccRenderingIntent.MediaRelativeColorimetric));

        Assert.Multiple(() =>
        {
            Assert.That(transformTag.Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.Lut16));
            Assert.That(transformTag.Data.Lut!.Clut.GridPoints, Is.EqualTo(new int[] { 7 }));
            Assert.That(transformTag.Data.Lut.Clut.Values, Has.Length.EqualTo(7));
            Assert.That(transform.TransformSingleColor(new float[] { 0.0f }),
                Is.EqualTo(new float[] { 0.0f }).Within(0.00002f));
            Assert.That(transform.TransformSingleColor(new float[] { 0.37f }),
                Is.EqualTo(new float[] { 0.37f }).Within(0.00002f));
            Assert.That(transform.TransformSingleColor(new float[] { 1.0f }),
                Is.EqualTo(new float[] { 1.0f }).Within(0.00002f));
        });
    }

    [Test]
    public void LinkerBakesCrossGenerationAbstractChainWithContinuousEndpointSpaces()
    {
        IccProfile source = MatrixMpeProfile(IccVersionKind.Version4, IccProfileClass.Display,
            IccColorSpaceSignatures.Rgb, IccTagSignatures.DToB0, 3, 3,
            new double[]
            {
                1.0, 0.0, 0.0, 0.0,
                0.0, 1.0, 0.0, 0.0,
                0.0, 0.0, 1.0, 0.0
            });
        IccProfile abstractProfile = MatrixMpeProfile(IccVersionKind.Version4,
            IccProfileClass.Abstract, IccColorSpaceSignatures.Xyz,
            IccTagSignatures.DToB0, 3, 3,
            new double[]
            {
                0.5, 0.0, 0.0, 0.0,
                0.0, 0.5, 0.0, 0.0,
                0.0, 0.0, 0.5, 0.0
            });
        IccProfile destination = MatrixMpeProfile(IccVersionKind.Version5,
            IccProfileClass.Display, IccColorSpaceSignatures.Gray,
            IccTagSignatures.BToA0, 3, 1,
            new double[] { 0.0, 2.0, 0.0, 0.0 });
        IccProfile[] sequence = new IccProfile[] { source, abstractProfile, destination };
        IccProfileLinkOptions options = new IccProfileLinkOptions();
        options.RenderingIntent = IccRenderingIntent.Perceptual;
        options.GridPointCount = 2;
        options.Interpolation = IccInterpolationKind.Tetrahedral;
        IccProfileLinker linker = new IccProfileLinker();

        IccOperationResult probe = linker.ProbeProfileSequence(sequence,
            IccRenderingIntent.Perceptual);
        IccProfile linked = linker.LinkConfiguredProfileSequence(sequence, options,
            new IccWriteOptions());
        IccProfile repeated = linker.LinkConfiguredProfileSequence(sequence, options,
            new IccWriteOptions());
        IIccColorTransform original = new IccTransformFactory().CreateProfileSequenceTransform(
            sequence, IccRenderingIntent.Perceptual, IccIntentFallback.None);
        IIccColorTransform baked = new IccTransformFactory().CreateDeviceLinkTransform(linked,
            TransformOptions(IccRenderingIntent.Perceptual));
        float[] input = new float[] { 0.2f, 0.4f, 0.6f };

        Assert.Multiple(() =>
        {
            Assert.That(probe.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(linked.Header.DataColorSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Rgb));
            Assert.That(linked.Header.ProfileConnectionSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Gray));
            Assert.That(linked.Header.RenderingIntentKind, Is.EqualTo(IccRenderingIntent.Perceptual));
            Assert.That(FindTag(linked, IccTagSignatures.ProfileSequenceDescription).Data.ProfileSequence,
                Has.Length.EqualTo(3));
            Assert.That(FindTag(linked, IccTagSignatures.ProfileSequenceIdentifier).Data.ProfileSequenceIdentifiers,
                Has.Length.EqualTo(3));
            Assert.That(original.TransformSingleColor(input),
                Is.EqualTo(new float[] { 0.4f }).Within(0.000002f));
            Assert.That(baked.TransformSingleColor(input),
                Is.EqualTo(new float[] { 0.4f }).Within(0.000002f));
            Assert.That(linked.OriginalBytes, Is.EqualTo(repeated.OriginalBytes));
        });
    }

    [Test]
    public void LinkerReportsIncompatibleSequencesAndRejectsInvalidOrExcessiveSelections()
    {
        IccProfile valid = GrayProfile(1.0, "Valid", 0, 0, 0);
        IccProfile missing = GrayProfile(1.0, "Missing", 0, 0, 0);
        missing.Tags = missing.Tags.Where(tag => tag.Signature != IccTagSignatures.GrayTrc).ToArray();
        IccProfileLinker linker = new IccProfileLinker();
        IccProfileLinkOptions badGrid = new IccProfileLinkOptions();
        badGrid.GridPointCount = 1;
        IccProfileLinkOptions badEncoding = new IccProfileLinkOptions();
        badEncoding.EncodingTypeSignature = IccTypeSignatures.MultiProcessElements;
        badEncoding.Version.Kind = IccVersionKind.Version2;
        badEncoding.Version.Major = 2;
        IccProfileLinkOptions limited = new IccProfileLinkOptions();
        limited.RenderingIntent = IccRenderingIntent.MediaRelativeColorimetric;
        limited.GridPointCount = 17;
        IccWriteOptions writeOptions = new IccWriteOptions();
        writeOptions.ResourceLimits.MaximumTagBytes = 16;

        Assert.Multiple(() =>
        {
            Assert.That(linker.ProbeProfileSequence(new IccProfile[] { valid, valid }, IccRenderingIntent.MediaRelativeColorimetric).Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(linker.ProbeProfileSequence(new IccProfile[] { valid, missing }, IccRenderingIntent.MediaRelativeColorimetric).Status,
                Is.EqualTo(IccOperationStatus.NotFound));
            Assert.Throws<IccProfileValidationException>(() => linker.LinkConfiguredProfileSequence(
                new IccProfile[] { valid, valid }, badGrid, new IccWriteOptions()));
            Assert.Throws<IccProfileValidationException>(() => linker.LinkConfiguredProfileSequence(
                new IccProfile[] { valid, valid }, badEncoding, new IccWriteOptions()));
            Assert.That(() => linker.LinkConfiguredProfileSequence(
                new IccProfile[] { valid, valid }, limited, writeOptions),
                Throws.TypeOf<IccProfileValidationException>().With.Message.Contains("resource limits"));
        });
    }

    private static IccProfile GrayProfile(double gamma, string name, uint manufacturer, uint model, byte id)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = IccVersionKind.Version4;
        profile.Header.ProfileClass = IccProfileClass.Display;
        profile.Header.ProfileClassSignature = IccProfileClassSignatures.Display;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Gray;
        profile.Header.DataColorSpace = IccColorSpaceKind.Gray;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Header.DeviceManufacturerSignature = manufacturer;
        profile.Header.DeviceModelSignature = model;
        profile.Header.ProfileId.Bytes[0] = id;
        profile.Header.ProfileId.IsPresent = id != 0;
        profile.Tags = new IccTag[]
        {
            new IccTag
            {
                Signature = IccTagSignatures.GrayTrc,
                Data = new IccTagData
                {
                    Kind = IccTagDataKind.Curve,
                    Curve = new IccCurve { Kind = IccCurveKind.Gamma, Gamma = gamma }
                }
            },
            new IccTag
            {
                Signature = IccTagSignatures.ProfileDescription,
                Data = new IccTagData
                {
                    Kind = IccTagDataKind.LocalizedText,
                    LocalizedStrings = new IccLocalizedString[]
                    {
                        new IccLocalizedString { Language = "en", Country = "US", Text = name }
                    }
                }
            },
            new IccTag
            {
                Signature = IccTagSignatures.Technology,
                Data = new IccTagData
                {
                    Kind = IccTagDataKind.Signature,
                    Signature = IccTechnologySignatures.InkJetPrinter
                }
            }
        };
        return profile;
    }

    private static IccTransformOptions TransformOptions(IccRenderingIntent intent)
    {
        IccTransformOptions options = new IccTransformOptions();
        options.Direction = IccTransformDirection.DeviceToDevice;
        options.RenderingIntent = intent;
        options.Interpolation = IccInterpolationKind.Multilinear;
        return options;
    }

    private static IccProfile MatrixMpeProfile(IccVersionKind version,
        IccProfileClass profileClass, uint dataSpace, uint tagSignature,
        int inputChannels, int outputChannels, double[] values)
    {
        IccProcessingElement element = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Matrix,
            Kind = IccProcessingElementKind.Matrix,
            InputChannels = inputChannels,
            OutputChannels = outputChannels,
            Matrix = new IccMatrix
            {
                Rows = outputChannels,
                Columns = inputChannels + 1,
                Values = values
            }
        };
        IccMultiProcessElements pipeline = new IccMultiProcessElements
        {
            InputChannels = inputChannels,
            OutputChannels = outputChannels,
            InputRanges = FullRanges(inputChannels),
            OutputRanges = FullRanges(outputChannels),
            Elements = new IccProcessingElement[] { element },
            IsExecutable = true
        };
        IccTagData data = new IccTagData
        {
            Kind = IccTagDataKind.MultiProcessElements,
            MultiProcessElements = pipeline
        };
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.MultiProcessElements;
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = version;
        profile.Header.ProfileClass = profileClass;
        profile.Header.DataColorSpaceSignature = dataSpace;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Tags = new IccTag[] { new IccTag { Signature = tagSignature, Data = data } };
        return profile;
    }

    private static IccEncodingRange[] FullRanges(int count)
    {
        IccEncodingRange[] ranges = new IccEncodingRange[count];
        int index = 0;
        while (index < count)
        {
            ranges[index] = new IccEncodingRange
            {
                Minimum = float.MinValue,
                Maximum = float.MaxValue
            };
            index++;
        }
        return ranges;
    }

    private static IccTag FindTag(IccProfile profile, uint signature)
    {
        return profile.Tags.Single(tag => tag.Signature == signature);
    }
}
