using icc;

namespace icc.Tests;

public class BuilderFinalizationTests
{
    [Test]
    public void RawProfileConstructionCanonicalizesValidatesAndComputesProfileId()
    {
        IIccProfileBuilder builder = new IccProfileBuilder();
        IccProfileBuildOptions options = IdentityOptions();
        IccProfile source = builder.CreateXyzIdentityProfile(options);
        IccTag[] reversed = source.Tags.Reverse().ToArray();

        IccProfile first = builder.CreateProfile(source.Header, reversed);
        IccProfile second = builder.CreateProfile(source.Header, source.Tags);
        IccValidationReport report = new IccProfileValidator()
            .ValidateProfileDataReport(first, new IccValidationPolicy());
        IccProfileId computed = new IccProfileIdCalculator()
            .ComputeProfileIdBytes(first.OriginalBytes);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True);
            Assert.That(first.OriginalBytes, Is.EqualTo(second.OriginalBytes));
            Assert.That(first.Tags.Select(tag => tag.Signature), Is.Ordered);
            Assert.That(first.Header.ProfileId.IsPresent, Is.True);
            Assert.That(first.Header.ProfileId.Bytes, Is.EqualTo(computed.Bytes));
            Assert.That(first.Header.CreationDateTime.Year, Is.EqualTo(2026));
            Assert.That(first.Header.CreationDateTime.Month, Is.EqualTo(9));
            Assert.That(first.Header.CreationDateTime.Day, Is.EqualTo(13));
        });
    }

    [Test]
    public void RawProfileConstructionRejectsAConformanceFailure()
    {
        IIccProfileBuilder builder = new IccProfileBuilder();
        IccProfile source = builder.CreateXyzIdentityProfile(IdentityOptions());
        IccTag[] incomplete = source.Tags.Where(tag =>
            tag.Signature != IccTagSignatures.ProfileDescription).ToArray();

        IccProfileValidationException error = Assert.Throws<IccProfileValidationException>(
            () => builder.CreateProfile(source.Header, incomplete))!;

        Assert.That(error.Message, Does.Contain(IccValidationRuleIds.RequiredTag));
    }

    [Test]
    public void ConstructionEntryPointsRejectNullOptionsAsInvalidInput()
    {
        IIccProfileBuilder builder = new IccProfileBuilder();

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CreateProfileSkeleton(null!));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CreateRgbProfile(null!));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CreateGrayProfile(null!));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CreateXyzIdentityProfile(null!));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CreateAbstractProfile(null!));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CreateNamedColorProfile(null!));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CreateOutputProfile(null!));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CreateColorEncodingProfile(null!));
        });
    }

    [Test]
    public void RawProfileConstructionRejectsNullAndDuplicateTagsAsInvalidInput()
    {
        IIccProfileBuilder builder = new IccProfileBuilder();
        IccProfile source = builder.CreateXyzIdentityProfile(IdentityOptions());
        IccTag duplicate = source.Tags[0];
        IccTag[] duplicates = source.Tags.Concat(new IccTag[] { duplicate })
            .ToArray();
        IccTag[] nullTag = new IccTag[] { null! };

        IccProfileValidationException nullError =
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CreateProfile(source.Header, nullTag))!;
        IccProfileValidationException duplicateError =
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CreateProfile(source.Header, duplicates))!;

        Assert.Multiple(() =>
        {
            Assert.That(nullError.Message,
                Is.EqualTo("Profile tag 0 cannot be null."));
            Assert.That(duplicateError.Message,
                Is.EqualTo($"Profile tag {source.Tags.Length} duplicates signature 0x{duplicate.Signature:X8}."));
        });
    }

    private static IccProfileBuildOptions IdentityOptions()
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = IccProfileClass.ColorSpace;
        options.DataColorSpaceSignature = IccColorSpaceSignatures.Xyz;
        options.CreationDateTime.Year = 2026;
        options.CreationDateTime.Month = 9;
        options.CreationDateTime.Day = 13;
        options.CreationDateTime.Hour = 8;
        options.CreationDateTime.Minute = 30;
        options.CreationDateTime.Second = 15;
        return options;
    }
}
