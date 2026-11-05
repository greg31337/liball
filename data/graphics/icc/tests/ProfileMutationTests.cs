using icc;

namespace icc.Tests;

public class ProfileMutationTests
{
    private const uint PrivateTagSignature = 0x70727631;
    private const uint SecondPrivateTagSignature = 0x70727632;
    private const uint LowPrivateTagSignature = 0x30303031;
    private const uint PrivateTypeSignature = 0x70727654;

    [Test]
    public void AddsReplacesAndRemovesPrivatePayloadWithoutAliasingOrDataLoss()
    {
        IIccProfileBuilder builder = new IccProfileBuilder();
        IccProfile source = ValidProfile(builder);
        IccTag firstTag = PrivateTag(0x11);

        IccProfile added = builder.SetProfileTag(source, firstTag);
        firstTag.Data.RawData.Bytes[8] = 0xff;
        IccTag secondTag = PrivateTag(0x22);
        IccProfile replaced = builder.SetProfileTag(added, secondTag);
        secondTag.Data.RawData.Bytes[8] = 0xee;
        IccProfile removed = builder.RemoveProfileTag(replaced,
            PrivateTagSignature);
        IccProfile unchanged = builder.RemoveProfileTag(removed,
            PrivateTagSignature);

        Assert.Multiple(() =>
        {
            Assert.That(source.Tags.Any(tag => tag.Signature
                == PrivateTagSignature), Is.False);
            Assert.That(FindTag(added, PrivateTagSignature).Data.RawData.Bytes,
                Is.EqualTo(PrivatePayload(0x11)));
            Assert.That(FindTag(replaced, PrivateTagSignature).Data.RawData.Bytes,
                Is.EqualTo(PrivatePayload(0x22)));
            Assert.That(replaced.Tags.Count(tag => tag.Signature
                == PrivateTagSignature), Is.EqualTo(1));
            Assert.That(removed.Tags.Any(tag => tag.Signature
                == PrivateTagSignature), Is.False);
            Assert.That(unchanged.OriginalBytes, Is.EqualTo(removed.OriginalBytes));
            Assert.That(IsValid(added), Is.True);
            Assert.That(IsValid(replaced), Is.True);
            Assert.That(IsValid(removed), Is.True);
        });
    }

    [Test]
    public void ReplacesHeaderAndPreservesTagsThroughAValidatedIndependentCopy()
    {
        IIccProfileBuilder builder = new IccProfileBuilder();
        IccProfile source = builder.SetProfileTag(ValidProfile(builder),
            PrivateTag(0x41));
        IccProfileHeader replacement = builder.CloneProfile(source).Header;
        replacement.RenderingIntent =
            (int)IccRenderingIntent.MediaRelativeColorimetric;
        replacement.RenderingIntentKind =
            IccRenderingIntent.MediaRelativeColorimetric;

        IccProfile result = builder.SetProfileHeader(source, replacement);
        replacement.RenderingIntent = (int)IccRenderingIntent.Saturation;
        replacement.RawBytes[0] = 0xff;

        Assert.Multiple(() =>
        {
            Assert.That(source.Header.RenderingIntentKind,
                Is.EqualTo(IccRenderingIntent.Perceptual));
            Assert.That(result.Header.RenderingIntentKind,
                Is.EqualTo(IccRenderingIntent.MediaRelativeColorimetric));
            Assert.That(result.Header.RenderingIntent,
                Is.EqualTo((int)IccRenderingIntent.MediaRelativeColorimetric));
            Assert.That(FindTag(result, PrivateTagSignature).Data.RawData.Bytes,
                Is.EqualTo(PrivatePayload(0x41)));
            Assert.That(result.Tags.Select(tag => tag.Signature),
                Is.EqualTo(source.Tags.Select(tag => tag.Signature)));
            Assert.That(result.Header.DeclaredSize,
                Is.EqualTo(result.OriginalBytes.Length));
            Assert.That(IsValid(result), Is.True);
        });
    }

    [Test]
    public void RejectsTagAndHeaderChangesThatWouldInvalidateTheProfile()
    {
        IIccProfileBuilder builder = new IccProfileBuilder();
        IccProfile profile = ValidProfile(builder);
        IccTag wrongWhiteType = new IccTag
        {
            Signature = IccTagSignatures.MediaWhitePoint,
            Data = new IccTagData
            {
                Kind = IccTagDataKind.Curve,
                Curve = new IccCurve(),
                RawData = new IccRawData
                {
                    HasTypeSignature = true,
                    TypeSignature = IccTypeSignatures.Curve
                }
            }
        };
        IccProfileHeader wrongClass = builder.CloneProfile(profile).Header;
        wrongClass.ProfileClass = IccProfileClass.Output;
        wrongClass.ProfileClassSignature = IccProfileClassSignatures.Output;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() =>
                builder.RemoveProfileTag(profile,
                    IccTagSignatures.MediaWhitePoint));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.SetProfileTag(profile, wrongWhiteType));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.SetProfileHeader(profile, wrongClass));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.SetProfileHeader(null!, wrongClass));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.SetProfileHeader(profile, null!));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.CloneProfile(null!));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.SetProfileTag(null!, PrivateTag(0x11)));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.SetProfileTag(profile, null!));
            Assert.Throws<IccProfileValidationException>(() =>
                builder.RemoveProfileTag(null!, PrivateTagSignature));
        });
    }

    [Test]
    public void ReplacementPreservesDirectorySlotAndCompatiblePayloadSharing()
    {
        IIccProfileBuilder builder = new IccProfileBuilder();
        IccProfile baseProfile = ValidProfile(builder);
        IccTag first = PrivateTag(PrivateTagSignature, 0x51);
        IccTag second = PrivateTag(SecondPrivateTagSignature, 0x51);
        IccProfile shared = builder.CreateProfile(baseProfile.Header,
            baseProfile.Tags.Concat(new IccTag[] { first, second }).ToArray());
        uint[] originalOrder = shared.Tags.Select(tag => tag.Signature).ToArray();

        IccProfile retained = builder.SetProfileTag(shared,
            PrivateTag(PrivateTagSignature, 0x51));
        IccProfile split = builder.SetProfileTag(shared,
            PrivateTag(PrivateTagSignature, 0x52));

        Assert.Multiple(() =>
        {
            Assert.That(FindTag(shared, PrivateTagSignature).TableEntry.Offset,
                Is.EqualTo(FindTag(shared, SecondPrivateTagSignature)
                    .TableEntry.Offset));
            Assert.That(retained.Tags.Select(tag => tag.Signature),
                Is.EqualTo(originalOrder));
            Assert.That(FindTag(retained, PrivateTagSignature).TableEntry.Offset,
                Is.EqualTo(FindTag(retained, SecondPrivateTagSignature)
                    .TableEntry.Offset));
            Assert.That(split.Tags.Select(tag => tag.Signature),
                Is.EqualTo(originalOrder));
            Assert.That(FindTag(split, PrivateTagSignature).TableEntry.Offset,
                Is.Not.EqualTo(FindTag(split, SecondPrivateTagSignature)
                    .TableEntry.Offset));
            Assert.That(FindTag(split, PrivateTagSignature).Data.RawData.Bytes,
                Is.EqualTo(PrivatePayload(0x52)));
            Assert.That(FindTag(split, SecondPrivateTagSignature).Data.RawData.Bytes,
                Is.EqualTo(PrivatePayload(0x51)));
            Assert.That(FindTag(shared, PrivateTagSignature).Data.RawData.Bytes,
                Is.EqualTo(PrivatePayload(0x51)));
            Assert.That(IsValid(retained), Is.True);
            Assert.That(IsValid(split), Is.True);
        });
    }

    [Test]
    public void MutationsRecomputeProfileIdsWithoutChangingTheSourceModel()
    {
        IIccProfileBuilder builder = new IccProfileBuilder();
        IIccProfileIdCalculator calculator = new IccProfileIdCalculator();
        IccProfile source = ValidProfile(builder);
        byte[] sourceId = source.Header.ProfileId.Bytes.ToArray();
        IccProfile clone = builder.CloneProfile(source);
        IccProfile added = builder.SetProfileTag(source, PrivateTag(0x61));
        IccProfileHeader header = builder.CloneProfile(added).Header;
        header.CreationDateTime.Year = 2001;
        IccProfile headerChanged = builder.SetProfileHeader(added, header);
        IccProfile removed = builder.RemoveProfileTag(added,
            PrivateTagSignature);
        clone.Header.ProfileId.Bytes[0] =
            (byte)(clone.Header.ProfileId.Bytes[0] ^ 0xff);

        Assert.Multiple(() =>
        {
            Assert.That(source.Header.ProfileId.Bytes, Is.EqualTo(sourceId));
            Assert.That(added.Header.ProfileId.Bytes,
                Is.EqualTo(calculator.ComputeProfileIdBytes(added.OriginalBytes).Bytes));
            Assert.That(added.Header.ProfileId.Bytes, Is.Not.EqualTo(sourceId));
            Assert.That(headerChanged.Header.ProfileId.Bytes,
                Is.EqualTo(calculator.ComputeProfileIdBytes(
                    headerChanged.OriginalBytes).Bytes));
            Assert.That(headerChanged.Header.ProfileId.Bytes,
                Is.Not.EqualTo(added.Header.ProfileId.Bytes));
            Assert.That(removed.Header.ProfileId.Bytes,
                Is.EqualTo(calculator.ComputeProfileIdBytes(
                    removed.OriginalBytes).Bytes));
            Assert.That(removed.Header.ProfileId.Bytes, Is.EqualTo(sourceId));
            Assert.That(clone.Header.ProfileId.Bytes,
                Is.Not.EqualTo(source.Header.ProfileId.Bytes));
        });
    }

    [Test]
    public void AddedTagUsesPreservationOrderWhileCanonicalWritingSortsIt()
    {
        IIccProfileBuilder builder = new IccProfileBuilder();
        IccProfile source = ValidProfile(builder);
        IccProfile preserved = builder.SetProfileTag(source,
            PrivateTag(LowPrivateTagSignature, 0x71));
        byte[] canonicalBytes = new IccProfileWriter().WriteProfileBytes(
            preserved, new IccWriteOptions());
        IccReadOptions readOptions = new IccReadOptions();
        readOptions.DecodeTagsLazily = false;
        IccProfile canonical = new IccProfileReader().ReadProfileBytes(
            canonicalBytes, readOptions);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.Tags.Last().Signature,
                Is.EqualTo(LowPrivateTagSignature));
            Assert.That(canonical.Tags.First().Signature,
                Is.EqualTo(LowPrivateTagSignature));
            Assert.That(preserved.Tags.Select(tag => tag.Signature),
                Is.Not.EqualTo(canonical.Tags.Select(tag => tag.Signature)));
            Assert.That(FindTag(preserved, LowPrivateTagSignature)
                .Data.RawData.Bytes, Is.EqualTo(PrivatePayload(0x71)));
            Assert.That(FindTag(canonical, LowPrivateTagSignature)
                .Data.RawData.Bytes, Is.EqualTo(PrivatePayload(0x71)));
            Assert.That(IsValid(preserved), Is.True);
            Assert.That(IsValid(canonical), Is.True);
        });
    }

    private static IccProfile ValidProfile(IIccProfileBuilder builder)
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = IccProfileClass.ColorSpace;
        options.DataColorSpaceSignature = IccColorSpaceSignatures.Xyz;
        options.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        return builder.CreateXyzIdentityProfile(options);
    }

    private static IccTag PrivateTag(byte value)
    {
        return PrivateTag(PrivateTagSignature, value);
    }

    private static IccTag PrivateTag(uint signature, byte value)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.Data.Kind = IccTagDataKind.Raw;
        tag.Data.RawData.HasTypeSignature = true;
        tag.Data.RawData.TypeSignature = PrivateTypeSignature;
        tag.Data.RawData.Bytes = PrivatePayload(value);
        return tag;
    }

    private static byte[] PrivatePayload(byte value)
    {
        return new byte[]
        {
            0x70, 0x72, 0x76, 0x54,
            0x00, 0x00, 0x00, 0x00,
            value, 0x33, 0x55, 0x77
        };
    }

    private static IccTag FindTag(IccProfile profile, uint signature)
    {
        return profile.Tags.Single(tag => tag.Signature == signature);
    }

    private static bool IsValid(IccProfile profile)
    {
        return new IccProfileValidator().ValidateProfileDataReport(profile,
            new IccValidationPolicy()).IsValid;
    }
}
