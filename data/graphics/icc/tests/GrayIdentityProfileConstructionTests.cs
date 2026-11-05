using icc;

namespace icc.Tests;

public class GrayIdentityProfileConstructionTests
{
    [Test]
    public void BuildsAdaptedVersionFourGrayDisplayProfileWithExecutableTrc()
    {
        IccProfileBuildOptions options = GrayOptions(IccProfileClass.Display);
        options.Descriptions = new IccLocalizedString[]
        {
            new IccLocalizedString { Language = "fr", Country = "FR", Text = "Affichage gris" },
            new IccLocalizedString { Language = "en", Country = "US", Text = "Gray display" }
        };
        options.GrayDefinition!.WhitePoint.Luminance = 160.0;
        options.GrayDefinition.GrayCurve = new IccCurve
        {
            Kind = IccCurveKind.Parametric,
            Parametric = new IccParametricCurve
            {
                FunctionType = 0,
                Parameters = new double[] { 2.2 }
            }
        };
        IIccProfileBuilder builder = new IccProfileBuilder();

        IccProfile profile = builder.CreateGrayProfile(options);
        IccProfile repeated = builder.CreateGrayProfile(options);
        IccTag adaptation = FindTag(profile, IccTagSignatures.ChromaticAdaptation);
        double[] sourceWhite = new double[]
        {
            options.GrayDefinition.WhitePoint.X / options.GrayDefinition.WhitePoint.Y,
            1.0,
            (1.0 - options.GrayDefinition.WhitePoint.X
                - options.GrayDefinition.WhitePoint.Y) / options.GrayDefinition.WhitePoint.Y
        };
        double[] mappedWhite = MatrixMath.ApplyMatrix3x3(
            adaptation.Data.FixedPointValues, sourceWhite);
        IIccColorTransform transform = new IccTransformFactory().CreateProfileTransform(
            profile, profile, IccRenderingIntent.MediaRelativeColorimetric,
            IccIntentFallback.None, IccInterpolationKind.Linear);
        float[] transformed = transform.TransformSingleColor(new float[] { 0.42f });
        IccValidationReport validation = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.ProfileClass, Is.EqualTo(IccProfileClass.Display));
            Assert.That(profile.Header.DataColorSpaceSignature,
                Is.EqualTo(IccColorSpaceSignatures.Gray));
            Assert.That(profile.Header.ProfileConnectionSpaceSignature,
                Is.EqualTo(IccColorSpaceSignatures.Xyz));
            Assert.That(FindTag(profile, IccTagSignatures.GrayTrc)
                .Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.ParametricCurve));
            Assert.That(mappedWhite[0], Is.EqualTo(IccStandardColorimetry.D50X).Within(0.0005));
            Assert.That(mappedWhite[1], Is.EqualTo(IccStandardColorimetry.D50Y).Within(0.0005));
            Assert.That(mappedWhite[2], Is.EqualTo(IccStandardColorimetry.D50Z).Within(0.0005));
            Assert.That(Xyz(profile, IccTagSignatures.Luminance).Y, Is.EqualTo(160.0));
            Assert.That(FindTag(profile, IccTagSignatures.ProfileDescription)
                .Data.LocalizedStrings.Select(value => value.Text),
                Is.EqualTo(new string[] { "Gray display", "Affichage gris" }));
            Assert.That(transformed[0], Is.EqualTo(0.42f).Within(0.00002f));
            Assert.That(validation.IsValid, Is.True);
            Assert.That(profile.OriginalBytes, Is.EqualTo(repeated.OriginalBytes));
        });
    }

    [Test]
    public void BuildsVersionTwoGrayOutputWithOnlyItsClassRequiredToneCurve()
    {
        IccProfileBuildOptions options = GrayOptions(IccProfileClass.Output);
        options.Version.Kind = IccVersionKind.Version2;
        options.Version.Major = 2;
        options.Version.Minor = 4;
        options.Descriptions = new IccLocalizedString[]
        {
            new IccLocalizedString { Text = "Gray output" }
        };
        options.GrayDefinition!.WhitePoint.X = IccStandardColorimetry.D50ChromaticityX;
        options.GrayDefinition.WhitePoint.Y = IccStandardColorimetry.D50ChromaticityY;
        options.GrayDefinition.WhitePoint.Luminance = 80.0;

        IccProfile profile = new IccProfileBuilder().CreateGrayProfile(options);
        IccValidationReport validation = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.Version.PackedValue, Is.EqualTo(0x02400000u));
            Assert.That(FindTag(profile, IccTagSignatures.ProfileDescription)
                .Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.TextDescription));
            Assert.That(FindTag(profile, IccTagSignatures.Copyright)
                .Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.Text));
            Assert.That(FindTag(profile, IccTagSignatures.GrayTrc)
                .Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.Curve));
            Assert.That(profile.Tags.Any(tag => tag.Signature == IccTagSignatures.Luminance),
                Is.False);
            Assert.That(profile.Tags.Any(tag => tag.Signature == IccTagSignatures.AToB0),
                Is.False);
            Assert.That(profile.Tags.Any(tag => tag.Signature == IccTagSignatures.BToA0),
                Is.False);
            Assert.That(validation.IsValid, Is.True);
        });
    }

    [Test]
    public void BuildsBidirectionalVersionFourXyzIdentityWithVersionFourPcsEncoding()
    {
        IccProfileBuildOptions options = IdentityOptions(IccProfileClass.ColorSpace);
        IIccProfileBuilder builder = new IccProfileBuilder();

        IccProfile profile = builder.CreateXyzIdentityProfile(options);
        IccProfile repeated = builder.CreateXyzIdentityProfile(options);
        IccTag forward = FindTag(profile, IccTagSignatures.AToB0);
        IccTag reverse = FindTag(profile, IccTagSignatures.BToA0);
        IIccColorTransform transform = new IccTransformFactory().CreateProfileTransform(
            profile, profile, IccRenderingIntent.Perceptual,
            IccIntentFallback.None, IccInterpolationKind.Tetrahedral);
        float[] transformed = transform.TransformSingleColor(
            new float[] { 0.2f, 0.4f, 0.6f });
        IccValidationReport validation = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(forward.Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.LutAToB));
            Assert.That(forward.Data.MultiStageLut!.PcsEncoding,
                Is.EqualTo(IccPcsEncodingKind.Version4));
            Assert.That(forward.Data.MultiStageLut.BCurves, Has.Length.EqualTo(3));
            Assert.That(reverse.Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.LutBToA));
            Assert.That(reverse.Data.MultiStageLut!.PcsEncoding,
                Is.EqualTo(IccPcsEncodingKind.Version4));
            Assert.That(FindTag(profile, IccTagSignatures.ProfileDescription)
                .Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.MultiLocalizedUnicode));
            Assert.That(transformed,
                Is.EqualTo(new float[] { 0.2f, 0.4f, 0.6f }).Within(0.00002f));
            Assert.That(validation.IsValid, Is.True);
            Assert.That(profile.OriginalBytes, Is.EqualTo(repeated.OriginalBytes));
        });
    }

    [Test]
    public void BuildsForwardOnlyVersionTwoXyzInputIdentityWithVersionTwoPcsEncoding()
    {
        IccProfileBuildOptions options = IdentityOptions(IccProfileClass.Input);
        options.Version.Kind = IccVersionKind.Version2;
        options.Version.Major = 2;
        options.Version.Minor = 4;

        IccProfile profile = new IccProfileBuilder().CreateXyzIdentityProfile(options);
        IccTag forward = FindTag(profile, IccTagSignatures.AToB0);
        IccValidationReport validation = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(forward.Data.RawData.TypeSignature,
                Is.EqualTo(IccTypeSignatures.Lut16));
            Assert.That(forward.Data.Lut!.PcsEncoding,
                Is.EqualTo(IccPcsEncodingKind.Version2));
            Assert.That(forward.Data.Lut.Clut.GridPoints, Is.EqualTo(new int[] { 2, 2, 2 }));
            Assert.That(profile.Tags.Any(tag => tag.Signature == IccTagSignatures.BToA0),
                Is.False);
            Assert.That(FindTag(profile, IccTagSignatures.ProfileDescription)
                .Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.TextDescription));
            Assert.That(validation.IsValid, Is.True);
        });
    }

    [TestCase(IccProfileClass.Display, true)]
    [TestCase(IccProfileClass.ColorSpace, true)]
    [TestCase(IccProfileClass.Abstract, false)]
    public void XyzIdentityAddsTheTransformsRequiredByItsProfileClass(
        IccProfileClass profileClass, bool expectsReverse)
    {
        IccProfile profile = new IccProfileBuilder().CreateXyzIdentityProfile(
            IdentityOptions(profileClass));
        IccValidationReport validation = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(profile.Tags.Any(tag => tag.Signature == IccTagSignatures.AToB0),
                Is.True);
            Assert.That(profile.Tags.Any(tag => tag.Signature == IccTagSignatures.BToA0),
                Is.EqualTo(expectsReverse));
            Assert.That(validation.IsValid, Is.True);
        });
    }

    [Test]
    public void RejectsIncompleteOrInconsistentGrayAndIdentityDefinitions()
    {
        IccProfileBuilder builder = new IccProfileBuilder();
        IccProfileBuildOptions missingGray = new IccProfileBuildOptions();
        missingGray.DataColorSpaceSignature = IccColorSpaceSignatures.Gray;
        IccProfileBuildOptions wrongGrayClass = GrayOptions(IccProfileClass.ColorSpace);
        IccProfileBuildOptions wrongGrayPcs = GrayOptions(IccProfileClass.Input);
        wrongGrayPcs.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Lab;
        IccProfileBuildOptions badGrayCurve = GrayOptions(IccProfileClass.Display);
        badGrayCurve.GrayDefinition!.GrayCurve = new IccCurve
        {
            Kind = IccCurveKind.Sampled,
            Samples = new double[] { 0.0, 0.8, 0.7, 1.0 }
        };
        IccProfileBuildOptions wrongIdentityClass = IdentityOptions(IccProfileClass.Output);
        IccProfileBuildOptions wrongIdentitySpace = IdentityOptions(IccProfileClass.Input);
        wrongIdentitySpace.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        IccProfileBuildOptions wrongIdentityPcs = IdentityOptions(IccProfileClass.Input);
        wrongIdentityPcs.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Lab;
        IccProfileBuildOptions wrongIdentityWhite = IdentityOptions(IccProfileClass.Input);
        wrongIdentityWhite.MediaWhitePoint.X = 0.9;
        IccProfileBuildOptions wrongIdentityAdaptation = IdentityOptions(IccProfileClass.Input);
        wrongIdentityAdaptation.ChromaticAdaptationMatrix = new IccMatrix
        {
            Rows = 3,
            Columns = 3,
            Values = new double[]
            {
                1.1, 0.0, 0.0,
                0.0, 1.0, 0.0,
                0.0, 0.0, 1.0
            }
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateGrayProfile(missingGray));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateGrayProfile(wrongGrayClass));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateGrayProfile(wrongGrayPcs));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateGrayProfile(badGrayCurve));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateXyzIdentityProfile(wrongIdentityClass));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateXyzIdentityProfile(wrongIdentitySpace));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateXyzIdentityProfile(wrongIdentityPcs));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateXyzIdentityProfile(wrongIdentityWhite));
            Assert.Throws<IccProfileValidationException>(
                () => builder.CreateXyzIdentityProfile(wrongIdentityAdaptation));
        });
    }

    private static IccProfileBuildOptions GrayOptions(IccProfileClass profileClass)
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = profileClass;
        options.DataColorSpaceSignature = IccColorSpaceSignatures.Gray;
        options.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        options.GrayDefinition = new IccGrayProfileDefinition();
        options.GrayDefinition.WhitePoint.X = IccStandardColorimetry.D65ChromaticityX;
        options.GrayDefinition.WhitePoint.Y = IccStandardColorimetry.D65ChromaticityY;
        options.GrayDefinition.GrayCurve = new IccCurve
        {
            Kind = IccCurveKind.Gamma,
            Gamma = 2.2
        };
        return options;
    }

    private static IccProfileBuildOptions IdentityOptions(IccProfileClass profileClass)
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = profileClass;
        options.DataColorSpaceSignature = IccColorSpaceSignatures.Xyz;
        options.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        return options;
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
