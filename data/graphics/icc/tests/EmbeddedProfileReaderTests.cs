using icc;

namespace icc.Tests;

public class EmbeddedProfileReaderTests
{
    [Test]
    public void DetectsSupportedCarrierSignaturesAcrossInputForms()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] raw = CreateProfile(2000);
        byte[] png = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        byte[] prefixedPng = new byte[3 + png.Length];
        System.Array.Copy(png, 0, prefixedPng, 3, png.Length);
        using MemoryStream stream = new MemoryStream(prefixedPng);
        stream.Position = 3;

        Assert.Multiple(() =>
        {
            Assert.That(reader.DetectCarrierBytes(raw), Is.EqualTo(IccEmbeddedFormat.RawIcc));
            Assert.That(reader.DetectCarrierMemory(png), Is.EqualTo(IccEmbeddedFormat.Png));
            Assert.That(reader.DetectCarrierBytes(new byte[] { 0xff, 0xd8 }),
                Is.EqualTo(IccEmbeddedFormat.Jpeg));
            Assert.That(reader.DetectCarrierBytes(Ascii("II*\0")),
                Is.EqualTo(IccEmbeddedFormat.Tiff));
            Assert.That(reader.DetectCarrierBytes(Ascii("MM\0+")),
                Is.EqualTo(IccEmbeddedFormat.BigTiff));
            Assert.That(reader.DetectCarrierBytes(Ascii("RIFF\0\0\0\0WEBP")),
                Is.EqualTo(IccEmbeddedFormat.WebP));
            Assert.That(reader.DetectCarrierBytes(new byte[]
            {
                0, 0, 0, 12, 0x6a, 0x50, 0x20, 0x20,
                0x0d, 0x0a, 0x87, 0x0a
            }), Is.EqualTo(IccEmbeddedFormat.Jpeg2000));
            Assert.That(reader.DetectCarrierBytes(CreateFtyp("avif")),
                Is.EqualTo(IccEmbeddedFormat.Avif));
            Assert.That(reader.DetectCarrierBytes(CreateFtyp("heic")),
                Is.EqualTo(IccEmbeddedFormat.Heif));
            Assert.That(reader.DetectCarrierBytes(CreateFtyp("mp42")),
                Is.EqualTo(IccEmbeddedFormat.IsoBmff));
            Assert.That(reader.DetectCarrierBytes(new byte[16]),
                Is.EqualTo(IccEmbeddedFormat.Unknown));
            Assert.That(reader.DetectCarrierStream(stream),
                Is.EqualTo(IccEmbeddedFormat.Png));
            Assert.That(stream.Position, Is.EqualTo(3));
        });
    }

    [Test]
    public void EnumeratesAndExtractsRawProfilesInCarrierOrderWithExactLocations()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] first = CreateProfile(2001);
        byte[] second = CreateProfile(2002);
        byte[] carrier = new byte[5 + first.Length + 7 + second.Length + 3];
        System.Array.Copy(first, 0, carrier, 5, first.Length);
        int secondOffset = 5 + first.Length + 7;
        System.Array.Copy(second, 0, carrier, secondOffset, second.Length);

        IccEmbeddedProfileResult result = reader.EnumerateProfilesBytes(carrier,
            new IccEmbeddingOptions());
        IccProfile[] extracted = reader.ExtractProfilesMemory(carrier,
            new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Operation, Is.EqualTo(IccEmbeddedOperation.Enumerate));
            Assert.That(result.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(result.Options.Format, Is.EqualTo(IccEmbeddedFormat.Unknown));
            Assert.That(result.Profiles, Has.Length.EqualTo(2));
            Assert.That(result.Profiles.Select(profile => profile.Index),
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(result.Profiles.All(profile =>
                profile.Format == IccEmbeddedFormat.RawIcc), Is.True);
            Assert.That(result.Profiles[0].Location.Offset, Is.EqualTo(5));
            Assert.That(result.Profiles[0].Location.Length, Is.EqualTo(first.Length));
            Assert.That(result.Profiles[1].Location.Offset, Is.EqualTo(secondOffset));
            Assert.That(result.Profiles[1].Location.Length, Is.EqualTo(second.Length));
            Assert.That(result.Profiles[1].Profile.Location.Offset,
                Is.EqualTo(secondOffset));
            Assert.That(result.Profiles[0].Parts, Has.Length.EqualTo(1));
            Assert.That(result.Profiles[0].Parts[0].Offset, Is.EqualTo(5));
            Assert.That(result.Profiles[0].Parts[0],
                Is.Not.SameAs(result.Profiles[0].Location));
            Assert.That(extracted, Has.Length.EqualTo(2));
            Assert.That(extracted[0].OriginalBytes, Is.EqualTo(first));
            Assert.That(extracted[1].OriginalBytes, Is.EqualTo(second));
        });
    }

    [Test]
    public void StreamEnumerationUsesAbsoluteOffsetsAndReportsItsBufferLimit()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] profile = CreateProfile(2003);
        byte[] prefixed = new byte[11 + profile.Length];
        System.Array.Copy(profile, 0, prefixed, 11, profile.Length);
        using MemoryStream successStream = new MemoryStream(prefixed);
        successStream.Position = 11;
        IccEmbeddingOptions successOptions = new IccEmbeddingOptions();

        IccEmbeddedProfileResult success = reader.EnumerateProfilesStream(
            successStream, successOptions);

        IccEmbeddingOptions limitedOptions = new IccEmbeddingOptions();
        limitedOptions.ResourceLimits.MaximumCarrierBytes = 132;
        using MemoryStream limitedStream = new MemoryStream(new byte[133]);
        IccEmbeddedProfileResult limited = reader.EnumerateProfilesStream(
            limitedStream, limitedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(success.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(success.Profiles.Single().Location.Offset, Is.EqualTo(11));
            Assert.That(success.Profiles.Single().Profile.Location.Offset, Is.EqualTo(11));
            Assert.That(successStream.Position, Is.EqualTo(prefixed.Length));
            Assert.That(limited.Result.Status,
                Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.That(limited.Profiles, Is.Empty);
            Assert.That(limited.Result.Location.Offset, Is.Zero);
            Assert.That(limited.Result.Location.Length, Is.EqualTo(132));
        });
    }

    [Test]
    public void EnumerationReportsNotFoundAndExpectedFormatConflictsWithoutThrowing()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        IccEmbeddedProfileResult missing = reader.EnumerateProfilesBytes(
            new byte[20], new IccEmbeddingOptions());
        IccEmbeddingOptions expectedPng = new IccEmbeddingOptions();
        expectedPng.Format = IccEmbeddedFormat.Png;
        IccEmbeddedProfileResult conflict = reader.EnumerateProfilesBytes(
            CreateProfile(2004), expectedPng);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Result.Status, Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(missing.Result.Location.Offset, Is.Zero);
            Assert.That(missing.Result.Location.Length, Is.EqualTo(20));
            Assert.That(conflict.Result.Status, Is.EqualTo(IccOperationStatus.Conflict));
            Assert.That(conflict.Options.Format, Is.EqualTo(IccEmbeddedFormat.RawIcc));
            Assert.That(conflict.Profiles, Is.Empty);
            Assert.Throws<IccEmbeddingException>(() => reader.ExtractProfilesBytes(
                new byte[20], new IccReadOptions()));
        });
    }

    [Test]
    public void MemoryEnumerationStreamExtractionAndProbeShareBoundedRawResults()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[] profile = CreateProfile(2005);
        byte[] carrier = new byte[9 + profile.Length];
        System.Array.Copy(profile, 0, carrier, 9, profile.Length);
        IccEmbeddedProfileResult enumerated = reader.EnumerateProfilesMemory(
            carrier, new IccEmbeddingOptions());
        using MemoryStream stream = new MemoryStream(carrier);
        stream.Position = 9;
        IccProfile extracted = reader.ExtractProfilesStream(stream,
            new IccReadOptions()).Single();
        IccResourceLimits limits = new IccResourceLimits();
        IccOperationResult success = reader.ProbeEmbeddedProfiles(carrier,
            limits);
        IccResourceLimits limited = new IccResourceLimits();
        limited.MaximumCarrierBytes = carrier.Length - 1;
        IccOperationResult bounded = reader.ProbeEmbeddedProfiles(carrier,
            limited);

        Assert.Multiple(() =>
        {
            Assert.That(enumerated.Result.Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(enumerated.Profiles.Single().Location.Offset,
                Is.EqualTo(9));
            Assert.That(extracted.OriginalBytes, Is.EqualTo(profile));
            Assert.That(extracted.Location.Offset, Is.EqualTo(9));
            Assert.That(stream.Position, Is.EqualTo(carrier.Length));
            Assert.That(success.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(success.Location.Offset, Is.EqualTo(9));
            Assert.That(bounded.Status,
                Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
        });
    }

    private static byte[] CreateProfile(int year)
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = IccProfileClass.ColorSpace;
        options.DataColorSpaceSignature = IccColorSpaceSignatures.Xyz;
        options.CreationDateTime.Year = year;
        return new IccProfileBuilder().CreateXyzIdentityProfile(options).OriginalBytes;
    }

    private static byte[] CreateFtyp(string brand)
    {
        byte[] box = new byte[16];
        box[3] = 16;
        System.Array.Copy(Ascii("ftyp"), 0, box, 4, 4);
        System.Array.Copy(Ascii(brand), 0, box, 8, 4);
        return box;
    }

    private static byte[] Ascii(string value)
    {
        return System.Text.Encoding.ASCII.GetBytes(value);
    }
}
