using icc;

namespace icc.Tests;

public class OutputProfileConstructionTests
{
    [Test]
    public void BuildsVersionFourXclrOutputWithAllIntentsGamutAndCharacterizationData()
    {
        IccProfileBuildOptions options = OutputOptions(IccVersionKind.Version4,
            IccColorSpaceSignatures.FiveColor);
        options.Descriptions = new IccLocalizedString[]
        {
            new IccLocalizedString
            {
                Language = "en",
                Country = "US",
                Text = "Five-colour press"
            }
        };
        options.OutputDefinition = CompleteDefinition(5, true);
        options.OutputDefinition.Colorants = FiveColorants();
        options.OutputDefinition.Measurement = new IccMeasurement
        {
            Observer = IccMeasurementObserver.Cie1931TwoDegree,
            Backing = new IccXyzColor { X = 0.82, Y = 0.85, Z = 0.70 },
            Geometry = IccMeasurementGeometry.Degree0Or45,
            Flare = 0.01,
            Illuminant = IccStandardIlluminant.D50
        };
        options.OutputDefinition.ViewingConditions = new IccViewingConditions
        {
            Illuminant = new IccXyzColor
            {
                X = IccStandardColorimetry.D50X,
                Y = IccStandardColorimetry.D50Y,
                Z = IccStandardColorimetry.D50Z
            },
            Surround = new IccXyzColor { X = 0.20, Y = 0.20, Z = 0.20 },
            IlluminantKind = IccStandardIlluminant.D50
        };
        IIccProfileBuilder builder = new IccProfileBuilder();

        IccProfile profile = builder.CreateOutputProfile(options);
        IccProfile repeated = builder.CreateOutputProfile(options);
        options.OutputDefinition.ForwardTransforms[0].MultiStageLut!.Clut!.Values[0] = 0.75;
        options.OutputDefinition.Colorants.Colorants[0].Name = "Changed";
        options.OutputDefinition.Measurement.Backing.X = 9.0;
        IccTag forward = FindTag(profile, IccTagSignatures.AToB0);
        IccTag backward = FindTag(profile, IccTagSignatures.BToA0);
        IccTag gamut = FindTag(profile, IccTagSignatures.Gamut);
        IccTag colorants = FindTag(profile, IccTagSignatures.ColorantTable);
        IccTag measurement = FindTag(profile, IccTagSignatures.Measurement);
        IccTag viewing = FindTag(profile, IccTagSignatures.ViewingConditions);
        IccValidationReport validation = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());
        IccOperationResult gamutProbe = new IccGamutAnalyzer().ProbeProfileGamut(profile);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.ProfileClass, Is.EqualTo(IccProfileClass.Output));
            Assert.That(profile.Header.DataColorSpaceSignature,
                Is.EqualTo(IccColorSpaceSignatures.FiveColor));
            Assert.That(forward.Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.LutAToB));
            Assert.That(forward.Data.MultiStageLut!.InputChannels, Is.EqualTo(5));
            Assert.That(forward.Data.MultiStageLut.OutputChannels, Is.EqualTo(3));
            Assert.That(forward.Data.MultiStageLut.Clut!.Values[0], Is.Zero);
            Assert.That(backward.Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.LutBToA));
            Assert.That(backward.Data.MultiStageLut!.InputChannels, Is.EqualTo(3));
            Assert.That(backward.Data.MultiStageLut.OutputChannels, Is.EqualTo(5));
            Assert.That(gamut.Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.Lut16));
            Assert.That(profile.Tags.Count(tag => IsIntentTransform(tag.Signature)),
                Is.EqualTo(6));
            Assert.That(colorants.Data.Colorants!.ColorSpaceSignature,
                Is.EqualTo(IccColorSpaceSignatures.FiveColor));
            Assert.That(colorants.Data.Colorants.Colorants.Select(value => value.Name),
                Is.EqualTo(new[] { "Cyan", "Magenta", "Yellow", "Black", "Orange" }));
            Assert.That(measurement.Data.Measurement!.Backing.X,
                Is.EqualTo(0.82).Within(0.00002));
            Assert.That(measurement.Data.Measurement.Flare,
                Is.EqualTo(0.01).Within(0.00002));
            Assert.That(viewing.Data.ViewingConditions!.IlluminantKind,
                Is.EqualTo(IccStandardIlluminant.D50));
            Assert.That(FindTag(profile, IccTagSignatures.ProfileDescription)
                .Data.LocalizedStrings.Single().Text, Is.EqualTo("Five-colour press"));
            Assert.That(gamutProbe.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(validation.IsValid, Is.True);
            Assert.That(profile.OriginalBytes, Is.EqualTo(repeated.OriginalBytes));
        });
    }

    [Test]
    public void BuildsVersionTwoCmykOutputFromClassicIntentAndGamutPipelines()
    {
        IccProfileBuildOptions options = OutputOptions(IccVersionKind.Version2,
            IccColorSpaceSignatures.Cmyk);
        options.Descriptions = new IccLocalizedString[]
        {
            new IccLocalizedString { Text = "Legacy press" }
        };
        options.OutputDefinition = CompleteDefinition(4, false);

        IccProfile profile = new IccProfileBuilder().CreateOutputProfile(options);
        IccValidationReport validation = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.Version.Kind, Is.EqualTo(IccVersionKind.Version2));
            Assert.That(FindTag(profile, IccTagSignatures.AToB0)
                .Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.Lut16));
            Assert.That(FindTag(profile, IccTagSignatures.BToA2)
                .Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.Lut16));
            Assert.That(FindTag(profile, IccTagSignatures.Gamut)
                .Data.Lut!.OutputChannels, Is.EqualTo(1));
            Assert.That(FindTag(profile, IccTagSignatures.ProfileDescription)
                .Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.TextDescription));
            Assert.That(profile.Tags.Any(tag => tag.Signature
                == IccTagSignatures.ColorantTable), Is.False);
            Assert.That(validation.IsValid, Is.True);
        });
    }

    [Test]
    public void RejectsMissingMalformedIncompatibleAndUnexecutableOutputDefinitions()
    {
        IccProfileBuilder builder = new IccProfileBuilder();
        IccProfileBuildOptions missing = OutputOptions(IccVersionKind.Version4,
            IccColorSpaceSignatures.Cmyk);
        IccProfileBuildOptions wrongClass = ValidOptions();
        wrongClass.ProfileClass = IccProfileClass.Display;
        IccProfileBuildOptions incomplete = ValidOptions();
        incomplete.OutputDefinition!.BackwardTransforms =
            new IccOutputTransformDefinition[2];
        IccProfileBuildOptions ambiguous = ValidOptions();
        ambiguous.OutputDefinition!.ForwardTransforms[0].Lut = ClassicLut(4, 3);
        IccProfileBuildOptions versionTwoMultiStage = OutputOptions(
            IccVersionKind.Version2, IccColorSpaceSignatures.Cmyk);
        versionTwoMultiStage.OutputDefinition = CompleteDefinition(4, true);
        IccProfileBuildOptions missingXclrColorants = OutputOptions(
            IccVersionKind.Version4, IccColorSpaceSignatures.FiveColor);
        missingXclrColorants.OutputDefinition = CompleteDefinition(5, true);
        IccProfileBuildOptions wrongColorants = ValidOptions();
        wrongColorants.OutputDefinition!.Colorants = FiveColorants();
        IccProfileBuildOptions badMeasurement = ValidOptions();
        badMeasurement.OutputDefinition!.Measurement = new IccMeasurement
        {
            Flare = 2.0
        };
        IccProfileBuildOptions wrongChannels = ValidOptions();
        wrongChannels.OutputDefinition!.ForwardTransforms[0] =
            ClassicTransform(3, 3);
        IccProfileBuildOptions gray = OutputOptions(IccVersionKind.Version4,
            IccColorSpaceSignatures.Gray);
        gray.OutputDefinition = CompleteDefinition(1, true);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateOutputProfile(missing));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateOutputProfile(wrongClass));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateOutputProfile(incomplete));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateOutputProfile(ambiguous));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateOutputProfile(versionTwoMultiStage));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateOutputProfile(missingXclrColorants));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateOutputProfile(wrongColorants));
            Assert.Throws<IccProfileWriteException>(
                () => builder.CreateOutputProfile(badMeasurement));
            Assert.Throws<IccTransformException>(
                () => builder.CreateOutputProfile(wrongChannels));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateOutputProfile(gray));
        });
    }

    private static IccProfileBuildOptions ValidOptions()
    {
        IccProfileBuildOptions options = OutputOptions(IccVersionKind.Version4,
            IccColorSpaceSignatures.Cmyk);
        options.OutputDefinition = CompleteDefinition(4, true);
        return options;
    }

    private static IccProfileBuildOptions OutputOptions(IccVersionKind version,
        uint dataSpace)
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = IccProfileClass.Output;
        options.DataColorSpaceSignature = dataSpace;
        options.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Lab;
        options.Version.Kind = version;
        options.Version.Major = version == IccVersionKind.Version2 ? 2 : 4;
        options.Version.Minor = 4;
        return options;
    }

    private static IccOutputProfileDefinition CompleteDefinition(int deviceChannels,
        bool multiStage)
    {
        IccOutputProfileDefinition definition = new IccOutputProfileDefinition();
        definition.ForwardTransforms = new IccOutputTransformDefinition[3];
        definition.BackwardTransforms = new IccOutputTransformDefinition[3];
        int index = 0;
        while (index < 3)
        {
            definition.ForwardTransforms[index] = multiStage
                ? MultiStageTransform(deviceChannels, 3, true)
                : ClassicTransform(deviceChannels, 3);
            definition.BackwardTransforms[index] = multiStage
                ? MultiStageTransform(3, deviceChannels, false)
                : ClassicTransform(3, deviceChannels);
            index++;
        }
        definition.GamutTransform = ClassicTransform(3, 1);
        return definition;
    }

    private static IccOutputTransformDefinition ClassicTransform(int inputChannels,
        int outputChannels)
    {
        IccOutputTransformDefinition transform = new IccOutputTransformDefinition();
        transform.Lut = ClassicLut(inputChannels, outputChannels);
        return transform;
    }

    private static IccLut ClassicLut(int inputChannels, int outputChannels)
    {
        IccLut lut = new IccLut();
        lut.InputChannels = inputChannels;
        lut.OutputChannels = outputChannels;
        lut.Matrix.Rows = 3;
        lut.Matrix.Columns = 3;
        lut.Matrix.Values = new double[]
        {
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0
        };
        lut.InputTables = IdentityTables(inputChannels);
        lut.OutputTables = IdentityTables(outputChannels);
        lut.Clut.InputChannels = inputChannels;
        lut.Clut.OutputChannels = outputChannels;
        lut.Clut.GridPoints = Enumerable.Repeat(2, inputChannels).ToArray();
        lut.Clut.Values = ClutValues(inputChannels, outputChannels);
        lut.PcsEncoding = IccPcsEncodingKind.Version2;
        return lut;
    }

    private static IccOutputTransformDefinition MultiStageTransform(
        int inputChannels, int outputChannels, bool forward)
    {
        IccMultiStageLut lut = new IccMultiStageLut();
        lut.InputChannels = inputChannels;
        lut.OutputChannels = outputChannels;
        lut.BCurves = IdentityCurves(forward ? outputChannels : inputChannels);
        lut.ACurves = IdentityCurves(forward ? inputChannels : outputChannels);
        lut.Clut = new IccClut
        {
            InputChannels = inputChannels,
            OutputChannels = outputChannels,
            GridPoints = Enumerable.Repeat(2, inputChannels).ToArray(),
            Values = ClutValues(inputChannels, outputChannels),
            Interpolation = IccInterpolationKind.Multilinear
        };
        lut.PcsEncoding = IccPcsEncodingKind.Version4;
        IccOutputTransformDefinition transform = new IccOutputTransformDefinition();
        transform.MultiStageLut = lut;
        return transform;
    }

    private static IccCurve[] IdentityCurves(int count)
    {
        return Enumerable.Range(0, count).Select(index => new IccCurve()).ToArray();
    }

    private static double[][] IdentityTables(int channels)
    {
        return Enumerable.Range(0, channels)
            .Select(index => new double[] { 0.0, 1.0 }).ToArray();
    }

    private static double[] ClutValues(int inputChannels, int outputChannels)
    {
        int nodes = 1 << inputChannels;
        double[] values = new double[nodes * outputChannels];
        int node = 0;
        while (node < nodes)
        {
            int output = 0;
            while (output < outputChannels)
            {
                values[(node * outputChannels) + output] =
                    ((node >> (output % inputChannels)) & 1) == 0 ? 0.0 : 1.0;
                output++;
            }
            node++;
        }
        return values;
    }

    private static IccColorantSet FiveColorants()
    {
        IccColorantSet set = new IccColorantSet();
        set.ColorSpaceSignature = IccColorSpaceSignatures.FiveColor;
        set.Colorants = new IccColorant[]
        {
            Colorant("Cyan", new double[] { 55.0, -35.0, -45.0 }),
            Colorant("Magenta", new double[] { 50.0, 75.0, -5.0 }),
            Colorant("Yellow", new double[] { 90.0, -5.0, 90.0 }),
            Colorant("Black", new double[] { 10.0, 0.0, 0.0 }),
            Colorant("Orange", new double[] { 65.0, 35.0, 55.0 })
        };
        return set;
    }

    private static IccColorant Colorant(string name, double[] pcs)
    {
        IccColorant colorant = new IccColorant();
        colorant.Name = name;
        colorant.PcsColor.ColorSpace = IccColorSpaceKind.Lab;
        colorant.PcsColor.ColorSpaceSignature = IccColorSpaceSignatures.Lab;
        colorant.PcsColor.Components = pcs;
        return colorant;
    }

    private static bool IsIntentTransform(uint signature)
    {
        return signature == IccTagSignatures.AToB0
            || signature == IccTagSignatures.AToB1
            || signature == IccTagSignatures.AToB2
            || signature == IccTagSignatures.BToA0
            || signature == IccTagSignatures.BToA1
            || signature == IccTagSignatures.BToA2;
    }

    private static IccTag FindTag(IccProfile profile, uint signature)
    {
        return profile.Tags.Single(tag => tag.Signature == signature);
    }
}
