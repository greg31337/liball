using icc;

namespace icc.Tests;

public class RgbProfileConstructionTests
{
    [Test]
    public void BuildsDeterministicVersionFourDisplayProfileWithAdaptedMatrixAndMetadata()
    {
        IccProfileBuildOptions options = SrgbOptions(IccProfileClass.Display);
        options.Descriptions = new IccLocalizedString[]
        {
            new IccLocalizedString { Language = "fr", Country = "FR", Text = "Affichage RVB" },
            new IccLocalizedString { Language = "en", Country = "US", Text = "RGB display" }
        };
        options.Copyright.Text = "Copyright example";
        options.RgbDefinition!.WhitePoint.Luminance = 120.0;
        options.RgbDefinition.RedCurve = new IccCurve
        {
            Kind = IccCurveKind.Parametric,
            Parametric = new IccParametricCurve
            {
                FunctionType = 0,
                Parameters = new double[] { 2.2 }
            }
        };
        options.RgbDefinition.Cicp = new IccCicp
        {
            ColorPrimaries = 2,
            TransferCharacteristics = 13,
            MatrixCoefficients = 0,
            FullRange = true
        };
        IIccProfileBuilder builder = new IccProfileBuilder();

        IccProfile profile = builder.CreateRgbProfile(options);
        IccProfile repeated = builder.CreateRgbProfile(options);
        IccXyzNumber red = Xyz(profile, IccTagSignatures.RedMatrixColumn);
        IccXyzNumber green = Xyz(profile, IccTagSignatures.GreenMatrixColumn);
        IccXyzNumber blue = Xyz(profile, IccTagSignatures.BlueMatrixColumn);
        IccTag adaptation = FindTag(profile, IccTagSignatures.ChromaticAdaptation);
        IccTag chromaticity = FindTag(profile, IccTagSignatures.Chromaticity);
        IccTag cicp = FindTag(profile, IccTagSignatures.Cicp);
        IccValidationReport validation = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.ProfileClass, Is.EqualTo(IccProfileClass.Display));
            Assert.That(profile.Header.DataColorSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Rgb));
            Assert.That(profile.Header.ProfileConnectionSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Xyz));
            Assert.That(red.X, Is.EqualTo(0.4360).Within(0.001));
            Assert.That(red.Y, Is.EqualTo(0.2225).Within(0.001));
            Assert.That(red.Z, Is.EqualTo(0.0139).Within(0.001));
            Assert.That(green.X, Is.EqualTo(0.3851).Within(0.001));
            Assert.That(green.Y, Is.EqualTo(0.7169).Within(0.001));
            Assert.That(green.Z, Is.EqualTo(0.0971).Within(0.001));
            Assert.That(blue.X, Is.EqualTo(0.1431).Within(0.001));
            Assert.That(blue.Y, Is.EqualTo(0.0606).Within(0.001));
            Assert.That(blue.Z, Is.EqualTo(0.7141).Within(0.001));
            Assert.That(red.X + green.X + blue.X,
                Is.EqualTo(IccStandardColorimetry.D50X).Within(0.0001));
            Assert.That(red.Y + green.Y + blue.Y,
                Is.EqualTo(IccStandardColorimetry.D50Y).Within(0.0001));
            Assert.That(red.Z + green.Z + blue.Z,
                Is.EqualTo(IccStandardColorimetry.D50Z).Within(0.0001));
            Assert.That(adaptation.Data.FixedPointValues, Has.Length.EqualTo(9));
            Assert.That(adaptation.Data.FixedPointValues[0], Is.EqualTo(1.0479).Within(0.001));
            Assert.That(FindTag(profile, IccTagSignatures.RedTrc).Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.ParametricCurve));
            Assert.That(chromaticity.Data.Chromaticity!.XCoordinates,
                Is.EqualTo(new double[] { 0.64, 0.30, 0.15 }).Within(0.00002));
            Assert.That(Xyz(profile, IccTagSignatures.Luminance).Y, Is.EqualTo(120.0));
            Assert.That(cicp.Data.Cicp!.ColorPrimaries, Is.EqualTo(2));
            Assert.That(cicp.Data.Cicp.UsesProfilePrimaries, Is.True);
            Assert.That(cicp.Data.Cicp.FullRange, Is.True);
            Assert.That(FindTag(profile, IccTagSignatures.ProfileDescription)
                .Data.LocalizedStrings.Select(value => value.Text),
                Is.EqualTo(new string[] { "RGB display", "Affichage RVB" }));
            Assert.That(validation.IsValid, Is.True);
            Assert.That(profile.OriginalBytes, Is.EqualTo(repeated.OriginalBytes));
        });
    }

    [Test]
    public void BuildsVersionTwoInputProfileFromMeasuredXyYPrimaries()
    {
        IccProfileBuildOptions options = SrgbOptions(IccProfileClass.Input);
        options.Version.Kind = IccVersionKind.Version2;
        options.Version.Major = 2;
        options.Version.Minor = 4;
        options.Descriptions = new IccLocalizedString[]
        {
            new IccLocalizedString { Language = "en", Country = "US", Text = "Measured input" }
        };
        options.RgbDefinition!.RedPrimary.Luminance = 0.2126729;
        options.RgbDefinition.GreenPrimary.Luminance = 0.7151522;
        options.RgbDefinition.BluePrimary.Luminance = 0.0721750;

        IccProfile profile = new IccProfileBuilder().CreateRgbProfile(options);
        IccXyzNumber red = Xyz(profile, IccTagSignatures.RedMatrixColumn);
        IccXyzNumber green = Xyz(profile, IccTagSignatures.GreenMatrixColumn);
        IccXyzNumber blue = Xyz(profile, IccTagSignatures.BlueMatrixColumn);
        IccValidationReport validation = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.Version.PackedValue, Is.EqualTo(0x02400000u));
            Assert.That(FindTag(profile, IccTagSignatures.ProfileDescription)
                .Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.TextDescription));
            Assert.That(FindTag(profile, IccTagSignatures.RedTrc)
                .Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.Curve));
            Assert.That(profile.Tags.Any(tag => tag.Signature == IccTagSignatures.Cicp), Is.False);
            Assert.That(red.X + green.X + blue.X,
                Is.EqualTo(IccStandardColorimetry.D50X).Within(0.0001));
            Assert.That(red.Y + green.Y + blue.Y,
                Is.EqualTo(IccStandardColorimetry.D50Y).Within(0.0001));
            Assert.That(red.Z + green.Z + blue.Z,
                Is.EqualTo(IccStandardColorimetry.D50Z).Within(0.0003));
            Assert.That(validation.IsValid, Is.True);
        });
    }

    [Test]
    public void ColorSpaceRgbProfileIsConformingAndSelectableInBothDirections()
    {
        IccProfileBuildOptions options = SrgbOptions(IccProfileClass.ColorSpace);
        options.RgbDefinition!.WhitePoint.X = IccStandardColorimetry.D50ChromaticityX;
        options.RgbDefinition.WhitePoint.Y = IccStandardColorimetry.D50ChromaticityY;

        IccProfile profile = new IccProfileBuilder().CreateRgbProfile(options);
        IntentSelection forward = IntentSelector.SelectIcc1Transform(profile,
            IccTransformDirection.DeviceToPcs, IccRenderingIntent.MediaRelativeColorimetric,
            IccIntentFallback.None, false);
        IntentSelection reverse = IntentSelector.SelectIcc1Transform(profile,
            IccTransformDirection.PcsToDevice, IccRenderingIntent.MediaRelativeColorimetric,
            IccIntentFallback.None, false);
        MatrixTrcPlan plan = MatrixTrcCompiler.Compile(profile,
            IccTransformDirection.PcsToDevice);
        IccValidationReport validation = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.ProfileClass, Is.EqualTo(IccProfileClass.ColorSpace));
            Assert.That(profile.Tags.Any(tag => tag.Signature == IccTagSignatures.ChromaticAdaptation), Is.False);
            Assert.That(profile.Tags.Any(tag => tag.Signature == IccTagSignatures.Chromaticity), Is.False);
            Assert.That(forward.Success, Is.True);
            Assert.That(forward.UsesMatrixTrc, Is.True);
            Assert.That(reverse.Success, Is.True);
            Assert.That(reverse.UsesMatrixTrc, Is.True);
            Assert.That(plan.SourceChannels, Is.EqualTo(3));
            Assert.That(plan.DestinationChannels, Is.EqualTo(3));
            Assert.That(validation.IsValid, Is.True);
        });
    }

    [Test]
    public void RejectsIncompleteInconsistentAndGenerationInappropriateRgbDefinitions()
    {
        IccProfileBuilder builder = new IccProfileBuilder();
        IccProfileBuildOptions missing = new IccProfileBuildOptions();
        IccProfileBuildOptions wrongClass = SrgbOptions(IccProfileClass.Output);
        IccProfileBuildOptions wrongPcs = SrgbOptions(IccProfileClass.Display);
        wrongPcs.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Lab;
        IccProfileBuildOptions singular = SrgbOptions(IccProfileClass.Display);
        singular.RgbDefinition!.GreenPrimary.X = singular.RgbDefinition.RedPrimary.X;
        singular.RgbDefinition.GreenPrimary.Y = singular.RgbDefinition.RedPrimary.Y;
        IccProfileBuildOptions mixedLuminance = SrgbOptions(IccProfileClass.Display);
        mixedLuminance.RgbDefinition!.RedPrimary.Luminance = 0.2;
        IccProfileBuildOptions decreasing = SrgbOptions(IccProfileClass.Display);
        decreasing.RgbDefinition!.RedCurve.Kind = IccCurveKind.Sampled;
        decreasing.RgbDefinition.RedCurve.Samples = new double[] { 0.0, 0.8, 0.7, 1.0 };
        IccProfileBuildOptions versionTwoCicp = SrgbOptions(IccProfileClass.Input);
        versionTwoCicp.Version.Kind = IccVersionKind.Version2;
        versionTwoCicp.Version.Major = 2;
        versionTwoCicp.RgbDefinition!.Cicp = new IccCicp { MatrixCoefficients = 0 };
        IccProfileBuildOptions wrongAdaptation = SrgbOptions(IccProfileClass.Display);
        wrongAdaptation.ChromaticAdaptationMatrix = new IccMatrix
        {
            Rows = 3,
            Columns = 3,
            Values = new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 }
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() => builder.CreateRgbProfile(missing));
            Assert.Throws<IccProfileValidationException>(() => builder.CreateRgbProfile(wrongClass));
            Assert.Throws<IccProfileValidationException>(() => builder.CreateRgbProfile(wrongPcs));
            Assert.Throws<IccProfileValidationException>(() => builder.CreateRgbProfile(singular));
            Assert.Throws<IccProfileValidationException>(() => builder.CreateRgbProfile(mixedLuminance));
            Assert.Throws<IccProfileValidationException>(() => builder.CreateRgbProfile(decreasing));
            Assert.Throws<IccProfileValidationException>(() => builder.CreateRgbProfile(versionTwoCicp));
            Assert.Throws<IccProfileValidationException>(() => builder.CreateRgbProfile(wrongAdaptation));
        });
    }

    private static IccProfileBuildOptions SrgbOptions(IccProfileClass profileClass)
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = profileClass;
        options.RgbDefinition = new IccRgbProfileDefinition();
        options.RgbDefinition.RedPrimary.X = 0.64;
        options.RgbDefinition.RedPrimary.Y = 0.33;
        options.RgbDefinition.GreenPrimary.X = 0.30;
        options.RgbDefinition.GreenPrimary.Y = 0.60;
        options.RgbDefinition.BluePrimary.X = 0.15;
        options.RgbDefinition.BluePrimary.Y = 0.06;
        options.RgbDefinition.WhitePoint.X = IccStandardColorimetry.D65ChromaticityX;
        options.RgbDefinition.WhitePoint.Y = IccStandardColorimetry.D65ChromaticityY;
        options.RgbDefinition.RedCurve = Gamma(2.2);
        options.RgbDefinition.GreenCurve = Gamma(2.2);
        options.RgbDefinition.BlueCurve = Gamma(2.2);
        return options;
    }

    private static IccCurve Gamma(double value)
    {
        IccCurve curve = new IccCurve();
        curve.Kind = IccCurveKind.Gamma;
        curve.Gamma = value;
        return curve;
    }

    private static IccTag FindTag(IccProfile profile, uint signature)
    {
        return profile.Tags.Single(tag => tag.Signature == signature);
    }

    private static IccXyzNumber Xyz(IccProfile profile, uint signature)
    {
        return FindTag(profile, signature).Data.XyzValues.Single();
    }
}
