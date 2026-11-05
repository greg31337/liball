using icc;

namespace icc.Tests;

public class ProfileTransformCompilerTests
{
    [Test]
    public void FactoryChainsVersionTwoLabToVersionFourXyzWithPerLinkIntentFallback()
    {
        IccProfile source = VersionTwoLabWhiteSource();
        IccProfile destination = GrayMatrixProfile(IccVersionKind.Version4, 1.0);
        IccTransformFactory factory = new IccTransformFactory();

        IccColorTransform transform = (IccColorTransform)factory.CreateProfileTransform(source, destination,
            IccRenderingIntent.Saturation, IccIntentFallback.AllowColorimetric, IccInterpolationKind.Multilinear);
        float[] output = transform.TransformSingleColor(new float[] { 0.37f });

        Assert.Multiple(() =>
        {
            Assert.That(output[0], Is.EqualTo(1.0f).Within(0.000001f));
            Assert.That(transform.Core.Plan.Options.Direction, Is.EqualTo(IccTransformDirection.DeviceToDevice));
            Assert.That(transform.Core.Plan.Options.RenderingIntent, Is.EqualTo(IccRenderingIntent.Saturation));
            Assert.That(transform.Core.Plan.Options.IntentFallback, Is.EqualTo(IccIntentFallback.AllowColorimetric));
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.Name.Contains("bridge D50 PCSLAB to PCSXYZ")), Is.True);
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.Name.Contains("bridge ICC version 2 PCS values to version 4")), Is.True);
            Assert.That(transform.Core.Plan.Stages.Where(stage => stage.TagSignature != 0).All(stage => stage.TagSignature == IccTagSignatures.AToB1), Is.True);
            Assert.That(transform.Core.Plan.Stages.Where(stage => stage.TagSignature != 0).All(stage => stage.Name.Contains("MediaRelativeColorimetric fallback")), Is.True);
            Assert.That(() => factory.CreateProfileTransform(source, destination, IccRenderingIntent.Saturation,
                IccIntentFallback.None, IccInterpolationKind.Multilinear), Throws.TypeOf<IccTransformException>());
            Assert.That(factory.ProbeProfileTransform(source, destination, IccRenderingIntent.Saturation).Status, Is.EqualTo(IccOperationStatus.NotFound));
        });
    }

    [Test]
    public void FactoryBridgesVersionFourXyzToVersionTwoLabBeforeTheReverseLut()
    {
        IccProfile source = GrayMatrixProfile(IccVersionKind.Version4, 1.0);
        IccProfile destination = VersionTwoLabGrayDestination();
        IccColorTransform transform = (IccColorTransform)new IccTransformFactory().CreateProfileTransform(source, destination,
            IccRenderingIntent.MediaRelativeColorimetric, IccIntentFallback.None, IccInterpolationKind.Multilinear);

        float[] output = transform.TransformSingleColor(new float[] { 0.5f });
        double expectedLightness = ((116.0 * System.Math.Cbrt(0.5)) - 16.0) * 652.8 / 65535.0;

        Assert.Multiple(() =>
        {
            Assert.That(output[0], Is.EqualTo((float)expectedLightness).Within(0.000002f));
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.Name.Contains("bridge PCSXYZ to D50 PCSLAB")), Is.True);
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.Name.Contains("bridge ICC version 4 PCS values to version 2")), Is.True);
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.TagSignature == IccTagSignatures.BToA1), Is.True);
        });
    }

    [Test]
    public void FactoryConnectsVersionFourSourceToVersionFiveDestinationWithGoldenOutput()
    {
        IccProfile source = GrayMatrixProfile(IccVersionKind.Version4, 1.0);
        IccProfile destination = VersionFiveMatrixProfile(IccTagSignatures.BToA1,
            IccColorSpaceSignatures.Gray, 3, 1, new double[] { 0.0, 0.5, 0.0, 0.1 });
        IccTransformFactory factory = new IccTransformFactory();

        IccColorTransform transform = (IccColorTransform)factory.CreateProfileTransform(source, destination,
            IccRenderingIntent.MediaRelativeColorimetric, IccIntentFallback.None,
            IccInterpolationKind.Multilinear);
        float[] output = transform.TransformSingleColor(new float[] { 0.5f });

        Assert.Multiple(() =>
        {
            Assert.That(output, Is.EqualTo(new float[] { 0.35f }).Within(0.000002f));
            Assert.That(factory.ProbeProfileTransform(source, destination,
                IccRenderingIntent.MediaRelativeColorimetric).Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.Name.Contains("version 4 PCS values to version 5")), Is.True);
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.TagSignature == IccTagSignatures.BToA1), Is.True);
        });
    }

    [Test]
    public void FactoryConnectsVersionFiveSourceToVersionFourDestinationWithGoldenOutput()
    {
        IccProfile source = VersionFiveMatrixProfile(IccTagSignatures.AToB1,
            IccColorSpaceSignatures.Gray, 1, 3,
            new double[] { 2.0, 0.0, 3.0, 0.0, 4.0, 0.0 });
        IccProfile destination = GrayMatrixProfile(IccVersionKind.Version4, 1.0);
        IccTransformFactory factory = new IccTransformFactory();

        IccColorTransform transform = (IccColorTransform)factory.CreateProfileTransform(source, destination,
            IccRenderingIntent.MediaRelativeColorimetric, IccIntentFallback.None,
            IccInterpolationKind.Multilinear);
        float[] output = transform.TransformSingleColor(new float[] { 0.2f });

        Assert.Multiple(() =>
        {
            Assert.That(output, Is.EqualTo(new float[] { 0.6f }).Within(0.000002f));
            Assert.That(factory.ProbeProfileTransform(source, destination,
                IccRenderingIntent.MediaRelativeColorimetric).Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.Name.Contains("version 5 PCS values to version 4")), Is.True);
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.TagSignature == IccTagSignatures.AToB1), Is.True);
        });
    }

    [Test]
    public void FactoryDispatchesSelectedVersionFourMpePipelines()
    {
        IccProfile source = MpeProfile(IccTagSignatures.DToB1);
        IccProfile destination = MpeProfile(IccTagSignatures.BToD1);
        IccColorTransform transform = (IccColorTransform)new IccTransformFactory().CreateProfileTransform(source, destination,
            IccRenderingIntent.MediaRelativeColorimetric, IccIntentFallback.None, IccInterpolationKind.Tetrahedral);

        float[] output = transform.TransformSingleColor(new float[] { 0.2f, 0.4f, 0.6f });

        Assert.Multiple(() =>
        {
            Assert.That(output, Is.EqualTo(new float[] { 0.2f, 0.4f, 0.6f }).Within(0.000001f));
            Assert.That(transform.Core.Plan.Stages.Select(stage => stage.TagSignature),
                Is.EqualTo(new uint[] { IccTagSignatures.DToB1, IccTagSignatures.BToD1 }));
        });
    }

    [Test]
    public void FactorySkipsNonExecutableVersionFourDTagsForSameIntentATags()
    {
        IccProfile source = MpeProfile(IccTagSignatures.DToB1);
        IccProfile destination = MpeProfile(IccTagSignatures.BToD1);
        source.Tags[0].Data.MultiProcessElements!.IsExecutable = false;
        destination.Tags[0].Data.MultiProcessElements!.IsExecutable = false;
        IccLut sourceLut = ClassicLut(3, 3, IccPcsEncodingKind.Version2);
        IccLut destinationLut = ClassicLut(3, 3, IccPcsEncodingKind.Version2);
        sourceLut.Clut.Values = Enumerable.Repeat(0.25, 24).ToArray();
        destinationLut.Clut.Values = Enumerable.Repeat(0.75, 24).ToArray();
        source.Tags = source.Tags.Append(LutTag(IccTagSignatures.AToB1, IccTypeSignatures.Lut16, sourceLut)).ToArray();
        destination.Tags = destination.Tags.Append(LutTag(IccTagSignatures.BToA1, IccTypeSignatures.Lut16, destinationLut)).ToArray();

        IccColorTransform transform = (IccColorTransform)new IccTransformFactory().CreateProfileTransform(source, destination,
            IccRenderingIntent.MediaRelativeColorimetric, IccIntentFallback.None, IccInterpolationKind.Multilinear);
        float[] output = transform.TransformSingleColor(new float[] { 0.1f, 0.2f, 0.3f });

        Assert.Multiple(() =>
        {
            Assert.That(output, Is.EqualTo(new float[] { 0.75f, 0.75f, 0.75f }).Within(0.000001f));
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.TagSignature == IccTagSignatures.DToB1), Is.False);
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.TagSignature == IccTagSignatures.BToD1), Is.False);
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.TagSignature == IccTagSignatures.AToB1), Is.True);
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.TagSignature == IccTagSignatures.BToA1), Is.True);
        });
    }

    [Test]
    public void AbsoluteIntentUsesEachProfilesMediaWhiteAtItsOwnLinkBoundary()
    {
        IccProfile source = GrayMatrixProfile(IccVersionKind.Version4, 1.0);
        IccProfile destination = GrayMatrixProfile(IccVersionKind.Version4, 1.0);
        source.Tags = source.Tags.Append(XyzTag(IccTagSignatures.MediaWhitePoint, 0.7, 0.8, 0.6)).ToArray();
        destination.Tags = destination.Tags.Append(XyzTag(IccTagSignatures.MediaWhitePoint, 0.3, 0.4, 0.2)).ToArray();
        IccColorTransform transform = (IccColorTransform)new IccTransformFactory().CreateProfileTransform(source, destination,
            IccRenderingIntent.IccAbsoluteColorimetric, IccIntentFallback.None, IccInterpolationKind.Multilinear);

        float[] output = transform.TransformSingleColor(new float[] { 0.25f });

        Assert.Multiple(() =>
        {
            Assert.That(output[0], Is.EqualTo(0.5f).Within(0.000001f));
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.Name.Contains("media-relative PCS to ICC-absolute PCS")), Is.True);
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.Name.Contains("ICC-absolute PCS to media-relative PCS")), Is.True);
        });
    }

    [Test]
    public void MultiprofileSequenceUsesEveryAdjacentLinkAndSnapshotsTheCompiledPlan()
    {
        IccProfile first = GrayMatrixProfile(IccVersionKind.Version4, 1.0);
        IccProfile middle = GrayMatrixProfile(IccVersionKind.Version4, 2.0);
        IccProfile last = GrayMatrixProfile(IccVersionKind.Version4, 1.0);
        IccProfile[] profiles = new IccProfile[] { first, middle, last };
        IccColorTransform transform = (IccColorTransform)new IccTransformFactory().CreateProfileSequenceTransform(profiles,
            IccRenderingIntent.MediaRelativeColorimetric, IccIntentFallback.None);

        first.Tags[0].Data.Curve!.Gamma = 7.0;
        middle.Tags = Array.Empty<IccTag>();
        last.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Cmyk;
        profiles[0] = new IccProfile();
        transform.Core.Plan.Options.ClipOutput = false;
        transform.Core.Plan.Stages[0].Name = "mutated public description";
        float[] output = transform.TransformSingleColor(new float[] { 0.25f });

        Assert.Multiple(() =>
        {
            Assert.That(output[0], Is.EqualTo(0.25f).Within(0.000001f));
            Assert.That(transform.GetSourceChannelCount(), Is.EqualTo(1));
            Assert.That(transform.GetDestinationChannelCount(), Is.EqualTo(1));
            Assert.That(transform.Core.Stages, Has.Length.EqualTo(8));
            Assert.That(transform.Core.Stages.Count(stage => stage.Description.Contains("profile 1 PcsToDevice")), Is.EqualTo(2));
            Assert.That(transform.Core.Stages.Count(stage => stage.Description.Contains("profile 1 DeviceToPcs")), Is.EqualTo(2));
            Assert.That(transform.Core.Options.ClipOutput, Is.True);
        });
    }

    [Test]
    public void MultiprofileSequenceAppliesIntermediateAbstractProfileOnceWithGoldenOutput()
    {
        IccProfile source = MpeProfile(IccTagSignatures.DToB0);
        IccProfile abstractProfile = MatrixMpeProfile(IccVersionKind.Version4, IccProfileClass.Abstract,
            IccTagSignatures.DToB0, IccColorSpaceSignatures.Xyz, 3, 3,
            new double[]
            {
                2.0, 0.0, 0.0, 0.0,
                0.0, 2.0, 0.0, 0.0,
                0.0, 0.0, 2.0, 0.0
            });
        IccProfile destination = MpeProfile(IccTagSignatures.BToD0);

        IccColorTransform transform = (IccColorTransform)new IccTransformFactory()
            .CreateProfileSequenceTransform(new IccProfile[] { source, abstractProfile, destination },
                IccRenderingIntent.Perceptual, IccIntentFallback.None);
        float[] output = transform.TransformSingleColor(new float[] { 0.1f, 0.2f, 0.3f });

        Assert.Multiple(() =>
        {
            Assert.That(output, Is.EqualTo(new float[] { 0.2f, 0.4f, 0.6f }).Within(0.000002f));
            Assert.That(transform.Core.Plan.Stages.Count(stage => stage.TagSignature == IccTagSignatures.DToB0), Is.EqualTo(2));
            Assert.That(transform.Core.Plan.Stages.Count(stage => stage.Name.Contains("profile 1 DeviceToPcs")), Is.EqualTo(1));
            Assert.That(transform.Core.Plan.Stages.Any(stage => stage.Name.Contains("profile 1 PcsToDevice")), Is.False);
        });
    }

    [Test]
    public void FactoryAndProbeRejectIncompleteOrIncompatibleStandardLinks()
    {
        IccTransformFactory factory = new IccTransformFactory();
        IccProfile valid = GrayMatrixProfile(IccVersionKind.Version4, 1.0);
        IccProfile missingTransform = GrayMatrixProfile(IccVersionKind.Version4, 1.0);
        missingTransform.Tags = Array.Empty<IccTag>();
        IccProfile inconsistentPcs = GrayMatrixProfile(IccVersionKind.Version4, 1.0);
        inconsistentPcs.Header.ProfileConnectionSpace = IccPcsKind.Lab;

        Assert.Multiple(() =>
        {
            Assert.That(factory.ProbeProfileTransform(valid, valid, IccRenderingIntent.MediaRelativeColorimetric).Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(factory.ProbeProfileTransform(valid, missingTransform, IccRenderingIntent.MediaRelativeColorimetric).Status, Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(factory.ProbeProfileTransform(valid, inconsistentPcs, IccRenderingIntent.MediaRelativeColorimetric).Status, Is.EqualTo(IccOperationStatus.Conflict));
            Assert.That(() => factory.CreateProfileSequenceTransform(new IccProfile[] { valid }, IccRenderingIntent.MediaRelativeColorimetric,
                IccIntentFallback.None), Throws.TypeOf<IccTransformException>());
            Assert.That(() => factory.CreateProfileTransform(valid, inconsistentPcs, IccRenderingIntent.MediaRelativeColorimetric,
                IccIntentFallback.None, IccInterpolationKind.Multilinear), Throws.TypeOf<IccTransformException>());
        });
    }

    [Test]
    public void FactoryRejectsNamedColorProfilesAsNumericIccConnections()
    {
        IccProfile valid = GrayMatrixProfile(IccVersionKind.Version4, 1.0);
        IccProfile named = Profile(IccVersionKind.Version5, IccProfileClass.NamedColor,
            IccColorSpaceSignatures.Rgb, IccPcsKind.Xyz);
        named.Tags = new IccTag[]
        {
            new IccTag { Signature = IccTagSignatures.BToA1, Data = new IccTagData() }
        };
        IccTransformFactory factory = new IccTransformFactory();

        Assert.Multiple(() =>
        {
            Assert.That(factory.ProbeProfileTransform(valid, named,
                IccRenderingIntent.MediaRelativeColorimetric).Status,
                Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(() => factory.CreateProfileTransform(valid, named,
                IccRenderingIntent.MediaRelativeColorimetric, IccIntentFallback.None,
                IccInterpolationKind.Multilinear), Throws.TypeOf<IccTransformException>()
                .With.Message.Contains("Named-colour"));
        });
    }

    private static IccProfile VersionTwoLabWhiteSource()
    {
        double lightness = 65280.0 / 65535.0;
        double zeroChroma = 32768.0 / 65535.0;
        IccLut lut = ClassicLut(1, 3, IccPcsEncodingKind.Version2);
        lut.Clut.Values = new double[]
        {
            lightness, zeroChroma, zeroChroma,
            lightness, zeroChroma, zeroChroma
        };
        IccProfile profile = Profile(IccVersionKind.Version2, IccProfileClass.Input, IccColorSpaceSignatures.Gray, IccPcsKind.Lab);
        profile.Tags = new IccTag[] { LutTag(IccTagSignatures.AToB1, IccTypeSignatures.Lut16, lut) };
        return profile;
    }

    private static IccProfile VersionTwoLabGrayDestination()
    {
        IccLut lut = ClassicLut(3, 1, IccPcsEncodingKind.Version2);
        lut.Clut.Values = new double[] { 0.0, 0.0, 0.0, 0.0, 1.0, 1.0, 1.0, 1.0 };
        IccProfile profile = Profile(IccVersionKind.Version2, IccProfileClass.Output, IccColorSpaceSignatures.Gray, IccPcsKind.Lab);
        profile.Tags = new IccTag[] { LutTag(IccTagSignatures.BToA1, IccTypeSignatures.Lut16, lut) };
        return profile;
    }

    private static IccProfile GrayMatrixProfile(IccVersionKind version, double gamma)
    {
        IccProfile profile = Profile(version, IccProfileClass.Display, IccColorSpaceSignatures.Gray, IccPcsKind.Xyz);
        profile.Tags = new IccTag[]
        {
            new IccTag
            {
                Signature = IccTagSignatures.GrayTrc,
                Data = new IccTagData { Kind = IccTagDataKind.Curve, Curve = new IccCurve { Kind = IccCurveKind.Gamma, Gamma = gamma } }
            }
        };
        return profile;
    }

    private static IccProfile MpeProfile(uint signature)
    {
        IccProcessingElement element = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.CurveSet,
            Kind = IccProcessingElementKind.CurveSet,
            InputChannels = 3,
            OutputChannels = 3,
            Curves = IdentityCurves(3, true)
        };
        IccMultiProcessElements pipeline = new IccMultiProcessElements
        {
            InputChannels = 3,
            OutputChannels = 3,
            InputRanges = FullRanges(3),
            OutputRanges = FullRanges(3),
            Elements = new IccProcessingElement[] { element },
            IsExecutable = true
        };
        IccTagData data = new IccTagData { Kind = IccTagDataKind.MultiProcessElements, MultiProcessElements = pipeline };
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.MultiProcessElements;
        IccProfile profile = Profile(IccVersionKind.Version4, IccProfileClass.Display, IccColorSpaceSignatures.Rgb, IccPcsKind.Xyz);
        profile.Tags = new IccTag[] { new IccTag { Signature = signature, Data = data } };
        return profile;
    }

    private static IccProfile VersionFiveMatrixProfile(uint signature, uint dataSpace,
        int inputChannels, int outputChannels, double[] values)
    {
        return MatrixMpeProfile(IccVersionKind.Version5, IccProfileClass.Display, signature,
            dataSpace, inputChannels, outputChannels, values);
    }

    private static IccProfile MatrixMpeProfile(IccVersionKind version, IccProfileClass profileClass,
        uint signature, uint dataSpace, int inputChannels, int outputChannels, double[] values)
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
        IccProfile profile = Profile(version, profileClass, dataSpace, IccPcsKind.Xyz);
        profile.Tags = new IccTag[] { new IccTag { Signature = signature, Data = data } };
        return profile;
    }

    private static IccProfile Profile(IccVersionKind version, IccProfileClass profileClass, uint dataSpace, IccPcsKind pcs)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = version;
        profile.Header.ProfileClass = profileClass;
        profile.Header.DataColorSpaceSignature = dataSpace;
        profile.Header.ProfileConnectionSpace = pcs;
        profile.Header.ProfileConnectionSpaceSignature = pcs == IccPcsKind.Xyz ? IccColorSpaceSignatures.Xyz : IccColorSpaceSignatures.Lab;
        return profile;
    }

    private static IccLut ClassicLut(int inputChannels, int outputChannels, IccPcsEncodingKind encoding)
    {
        IccLut lut = new IccLut();
        lut.InputChannels = inputChannels;
        lut.OutputChannels = outputChannels;
        lut.PcsEncoding = encoding;
        lut.Matrix = new IccMatrix
        {
            Rows = 3,
            Columns = 3,
            Values = new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 }
        };
        lut.InputTables = Enumerable.Range(0, inputChannels).Select(index => new double[] { 0.0, 1.0 }).ToArray();
        lut.OutputTables = Enumerable.Range(0, outputChannels).Select(index => new double[] { 0.0, 1.0 }).ToArray();
        lut.Clut = new IccClut
        {
            InputChannels = inputChannels,
            OutputChannels = outputChannels,
            GridPoints = Enumerable.Repeat(2, inputChannels).ToArray()
        };
        return lut;
    }

    private static IccTag LutTag(uint tagSignature, uint typeSignature, IccLut lut)
    {
        IccTagData data = new IccTagData { Kind = IccTagDataKind.Lut, Lut = lut };
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = typeSignature;
        return new IccTag { Signature = tagSignature, Data = data };
    }

    private static IccTag XyzTag(uint signature, double x, double y, double z)
    {
        return new IccTag
        {
            Signature = signature,
            Data = new IccTagData { Kind = IccTagDataKind.XyzArray, XyzValues = new IccXyzNumber[] { new IccXyzNumber { X = x, Y = y, Z = z } } }
        };
    }

    private static IccCurve[] IdentityCurves(int count, bool segmented)
    {
        IccCurve[] curves = new IccCurve[count];
        int index = 0;
        while (index < count)
        {
            if (segmented)
            {
                curves[index] = new IccCurve
                {
                    Kind = IccCurveKind.Segmented,
                    Segments = new IccCurveSegment[]
                    {
                        new IccCurveSegment
                        {
                            Kind = IccCurveSegmentKind.Formula,
                            FormulaType = 0,
                            BreakPoint = double.NegativeInfinity,
                            Parameters = new double[] { 1.0, 1.0, 0.0, 0.0 }
                        }
                    }
                };
            }
            else
            {
                curves[index] = new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 1.0 };
            }
            index++;
        }
        return curves;
    }

    private static IccEncodingRange[] FullRanges(int count)
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
