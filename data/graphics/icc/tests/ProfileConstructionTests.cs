using icc;

namespace icc.Tests;

public class ProfileConstructionTests
{
    [Test]
    public void DefaultSkeletonHasCanonicalVersionFourHeaderAndRequiredMetadata()
    {
        IIccProfileBuilder builder = new IccProfileBuilder();
        IccProfile first = builder.CreateProfileSkeleton(new IccProfileBuildOptions());
        IccProfile second = builder.CreateProfileSkeleton(new IccProfileBuildOptions());
        IccTag description = FindTag(first, IccTagSignatures.ProfileDescription);
        IccTag copyright = FindTag(first, IccTagSignatures.Copyright);
        IccTag white = FindTag(first, IccTagSignatures.MediaWhitePoint);
        IccWriteOptions writeOptions = new IccWriteOptions();
        IccValidationReport report = new IccProfileValidator().ValidateProfileDataReport(
            first, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(first.Header.Version.Kind, Is.EqualTo(IccVersionKind.Version4));
            Assert.That(first.Header.Version.PackedValue, Is.EqualTo(0x04000000u));
            Assert.That(first.Header.ProfileClass, Is.EqualTo(IccProfileClass.Display));
            Assert.That(first.Header.ProfileClassSignature, Is.EqualTo(IccProfileClassSignatures.Display));
            Assert.That(first.Header.DataColorSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Rgb));
            Assert.That(first.Header.DataColorSpace, Is.EqualTo(IccColorSpaceKind.Rgb));
            Assert.That(first.Header.ProfileConnectionSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Xyz));
            Assert.That(first.Header.ProfileConnectionSpace, Is.EqualTo(IccPcsKind.Xyz));
            Assert.That(first.Header.ProfileFileSignature, Is.EqualTo(IccConstants.ProfileFileSignature));
            Assert.That(first.Header.RenderingIntentKind, Is.EqualTo(IccRenderingIntent.Perceptual));
            Assert.That(first.Header.PcsIlluminantX, Is.EqualTo(IccStandardColorimetry.D50EncodedX));
            Assert.That(first.Header.PcsIlluminantY, Is.EqualTo(IccStandardColorimetry.D50EncodedY));
            Assert.That(first.Header.PcsIlluminantZ, Is.EqualTo(IccStandardColorimetry.D50EncodedZ));
            Assert.That(first.Header.CreationDateTime.Year, Is.EqualTo(2000));
            Assert.That(first.Header.CreationDateTime.Month, Is.EqualTo(1));
            Assert.That(first.Header.CreationDateTime.Day, Is.EqualTo(1));
            Assert.That(description.Data.Kind, Is.EqualTo(IccTagDataKind.LocalizedText));
            Assert.That(description.Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.MultiLocalizedUnicode));
            Assert.That(description.Data.LocalizedStrings.Single().Text, Is.EqualTo("ICC profile"));
            Assert.That(copyright.Data.Kind, Is.EqualTo(IccTagDataKind.LocalizedText));
            Assert.That(copyright.Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.MultiLocalizedUnicode));
            Assert.That(copyright.Data.LocalizedStrings.Single().Text, Is.Empty);
            Assert.That(white.Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.Xyz));
            Assert.That(white.Data.XyzValues.Single().X, Is.EqualTo(IccStandardColorimetry.D50X).Within(0.00002));
            Assert.That(white.Data.XyzValues.Single().Y, Is.EqualTo(IccStandardColorimetry.D50Y).Within(0.00002));
            Assert.That(white.Data.XyzValues.Single().Z, Is.EqualTo(IccStandardColorimetry.D50Z).Within(0.00002));
            Assert.That(first.Tags.Any(tag => tag.Signature == IccTagSignatures.ChromaticAdaptation), Is.False);
            Assert.That(first.Header.DeclaredSize, Is.EqualTo(first.OriginalBytes.Length));
            Assert.That(new IccProfileWriter().MeasureProfileBytes(first, writeOptions), Is.EqualTo(first.OriginalBytes.Length));
            Assert.That(first.OriginalBytes, Is.EqualTo(second.OriginalBytes));
            Assert.That(report.Issues.Where(issue => issue.Severity == IccValidationSeverity.Error
                || issue.Severity == IccValidationSeverity.Fatal)
                .All(issue => issue.RuleId == IccValidationRuleIds.RequiredTag), Is.True);
        });
    }

    [Test]
    public void VersionTwoSkeletonUsesLegacyTextAndCopiesExplicitCreationAndAdaptationData()
    {
        IccProfileBuildOptions options = VersionTwoOptions();
        options.ProfileClass = IccProfileClass.Input;
        options.RenderingIntent = IccRenderingIntent.MediaRelativeColorimetric;
        options.Descriptions = new IccLocalizedString[]
        {
            new IccLocalizedString { Language = "fr", Country = "FR", Text = "Profil d'entrée" }
        };
        options.Copyright.Text = "Copyright 2026";
        options.CreationDateTime = new IccDateTime
        {
            Year = 2024,
            Month = 2,
            Day = 29,
            Hour = 12,
            Minute = 34,
            Second = 56
        };
        options.MediaWhitePoint = new IccXyzColor { X = 0.9505, Y = 1.0, Z = 1.0888 };
        options.ChromaticAdaptationMatrix = new IccMatrix
        {
            Rows = 3,
            Columns = 3,
            Values = new double[] { 1.05, 0.01, -0.02, 0.02, 0.98, 0.01, 0.0, 0.01, 0.92 }
        };

        IccProfile profile = new IccProfileBuilder().CreateProfileSkeleton(options);
        options.CreationDateTime.Year = 2001;
        options.ChromaticAdaptationMatrix.Values[0] = 9.0;
        IccTag description = FindTag(profile, IccTagSignatures.ProfileDescription);
        IccTag copyright = FindTag(profile, IccTagSignatures.Copyright);
        IccTag adaptation = FindTag(profile, IccTagSignatures.ChromaticAdaptation);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.Version.Kind, Is.EqualTo(IccVersionKind.Version2));
            Assert.That(profile.Header.Version.PackedValue, Is.EqualTo(0x02400000u));
            Assert.That(profile.Header.ProfileClass, Is.EqualTo(IccProfileClass.Input));
            Assert.That(profile.Header.RenderingIntentKind,
                Is.EqualTo(IccRenderingIntent.MediaRelativeColorimetric));
            Assert.That(profile.Header.CreationDateTime.Year, Is.EqualTo(2024));
            Assert.That(profile.Header.CreationDateTime.Month, Is.EqualTo(2));
            Assert.That(profile.Header.CreationDateTime.Day, Is.EqualTo(29));
            Assert.That(profile.Header.CreationDateTime.Hour, Is.EqualTo(12));
            Assert.That(profile.Header.CreationDateTime.Minute, Is.EqualTo(34));
            Assert.That(profile.Header.CreationDateTime.Second, Is.EqualTo(56));
            Assert.That(description.Data.Kind, Is.EqualTo(IccTagDataKind.Text));
            Assert.That(description.Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.TextDescription));
            Assert.That(description.Data.Text!.Text, Is.EqualTo("Profil d'entrée"));
            Assert.That(copyright.Data.Kind, Is.EqualTo(IccTagDataKind.Text));
            Assert.That(copyright.Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.Text));
            Assert.That(copyright.Data.Text!.Text, Is.EqualTo("Copyright 2026"));
            Assert.That(adaptation.Data.Kind, Is.EqualTo(IccTagDataKind.FixedPointArray));
            Assert.That(adaptation.Data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.S15Fixed16Array));
            Assert.That(adaptation.Data.FixedPointValues, Has.Length.EqualTo(9));
            Assert.That(adaptation.Data.FixedPointValues[0], Is.EqualTo(1.05).Within(0.00002));
        });
    }

    [Test]
    public void DeviceLinkSkeletonTreatsTheConnectionSignatureAsAnOutputSpace()
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = IccProfileClass.DeviceLink;
        options.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Cmyk;

        IccProfile profile = new IccProfileBuilder().CreateProfileSkeleton(options);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.ProfileClassSignature, Is.EqualTo(IccProfileClassSignatures.DeviceLink));
            Assert.That(profile.Header.ProfileConnectionSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Cmyk));
            Assert.That(profile.Header.ProfileConnectionSpace, Is.EqualTo(IccPcsKind.None));
            Assert.That(profile.Tags.Any(tag => tag.Signature == IccTagSignatures.MediaWhitePoint), Is.False);
            Assert.That(profile.Tags.Any(tag => tag.Signature == IccTagSignatures.ChromaticAdaptation), Is.False);
            Assert.That(profile.Tags.Any(tag => tag.Signature == IccTagSignatures.ProfileDescription), Is.True);
            Assert.That(profile.Tags.Any(tag => tag.Signature == IccTagSignatures.Copyright), Is.True);
        });
    }

    [Test]
    public void SkeletonRejectsUnsupportedGenerationsAndInvalidMetadataBeforeWriting()
    {
        IccProfileBuilder builder = new IccProfileBuilder();
        IccProfileBuildOptions versionFive = new IccProfileBuildOptions();
        versionFive.Version.Kind = IccVersionKind.Version5;
        versionFive.Version.Major = 5;
        IccProfileBuildOptions invalidDate = new IccProfileBuildOptions();
        invalidDate.CreationDateTime.Year = 2023;
        invalidDate.CreationDateTime.Month = 2;
        invalidDate.CreationDateTime.Day = 29;
        IccProfileBuildOptions nonD50Display = new IccProfileBuildOptions();
        nonD50Display.MediaWhitePoint.X = 0.95;
        IccProfileBuildOptions singularAdaptation = new IccProfileBuildOptions();
        singularAdaptation.ChromaticAdaptationMatrix = new IccMatrix
        {
            Rows = 3,
            Columns = 3,
            Values = new double[] { 1.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0 }
        };
        IccProfileBuildOptions nonAsciiCopyright = VersionTwoOptions();
        nonAsciiCopyright.Copyright.Text = "Droit d’auteur";
        IccProfileBuildOptions badLocale = new IccProfileBuildOptions();
        badLocale.Descriptions = new IccLocalizedString[]
        {
            new IccLocalizedString { Language = "EN", Country = "us", Text = "Invalid locale" }
        };
        IccProfileBuildOptions deviceLinkAdaptation = new IccProfileBuildOptions();
        deviceLinkAdaptation.ProfileClass = IccProfileClass.DeviceLink;
        deviceLinkAdaptation.ChromaticAdaptationMatrix = new IccMatrix
        {
            Rows = 3,
            Columns = 3,
            Values = new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 }
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() => builder.CreateProfileSkeleton(versionFive));
            Assert.Throws<IccProfileValidationException>(() => builder.CreateProfileSkeleton(invalidDate));
            Assert.Throws<IccProfileValidationException>(() => builder.CreateProfileSkeleton(nonD50Display));
            Assert.Throws<IccProfileValidationException>(() => builder.CreateProfileSkeleton(singularAdaptation));
            Assert.Throws<IccProfileValidationException>(() => builder.CreateProfileSkeleton(nonAsciiCopyright));
            Assert.Throws<IccProfileValidationException>(() => builder.CreateProfileSkeleton(badLocale));
            Assert.Throws<IccProfileValidationException>(() => builder.CreateProfileSkeleton(deviceLinkAdaptation));
        });
    }

    private static IccProfileBuildOptions VersionTwoOptions()
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.Version.Kind = IccVersionKind.Version2;
        options.Version.Major = 2;
        options.Version.Minor = 4;
        return options;
    }

    private static IccTag FindTag(IccProfile profile, uint signature)
    {
        return profile.Tags.Single(tag => tag.Signature == signature);
    }
}
