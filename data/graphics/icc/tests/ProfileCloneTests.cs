using icc;

namespace icc.Tests;

public class ProfileCloneTests
{
    [Test]
    public void CloneProfileCopiesHeaderAndEnvelopeValues()
    {
        IccProfile source = CreatePopulatedProfile();

        IccProfile clone = ProfileClone.CloneProfile(source);

        Assert.Multiple(() =>
        {
            Assert.That(clone.Header.DeclaredSize, Is.EqualTo(256));
            Assert.That(clone.Header.PreferredCmmSignature, Is.EqualTo(0x434d4d20));
            Assert.That(clone.Header.Version.Kind, Is.EqualTo(IccVersionKind.Version5));
            Assert.That(clone.Header.Version.Major, Is.EqualTo(5));
            Assert.That(clone.Header.Version.Minor, Is.EqualTo(1));
            Assert.That(clone.Header.Version.BugFix, Is.EqualTo(2));
            Assert.That(clone.Header.Version.SubclassMajor, Is.EqualTo(3));
            Assert.That(clone.Header.Version.SubclassMinor, Is.EqualTo(4));
            Assert.That(clone.Header.Version.IsSupported, Is.False);
            Assert.That(clone.Header.Version.IsFutureMinor, Is.True);
            Assert.That(clone.Header.Version.PackedValue, Is.EqualTo(0x05120304));
            Assert.That(clone.Header.ProfileClass, Is.EqualTo(IccProfileClass.Display));
            Assert.That(clone.Header.ProfileClassSignature, Is.EqualTo(0x6d6e7472));
            Assert.That(clone.Header.DataColorSpaceSignature, Is.EqualTo(0x52474220));
            Assert.That(clone.Header.DataColorSpace, Is.EqualTo(IccColorSpaceKind.Rgb));
            Assert.That(clone.Header.ProfileConnectionSpaceSignature, Is.EqualTo(0x58595a20));
            Assert.That(clone.Header.ProfileConnectionSpace, Is.EqualTo(IccPcsKind.Xyz));
            Assert.That(clone.Header.CreationDateTime.Year, Is.EqualTo(2026));
            Assert.That(clone.Header.CreationDateTime.Month, Is.EqualTo(9));
            Assert.That(clone.Header.CreationDateTime.Day, Is.EqualTo(11));
            Assert.That(clone.Header.CreationDateTime.Hour, Is.EqualTo(12));
            Assert.That(clone.Header.CreationDateTime.Minute, Is.EqualTo(34));
            Assert.That(clone.Header.CreationDateTime.Second, Is.EqualTo(56));
            Assert.That(clone.Header.ProfileFileSignature, Is.EqualTo(0x61637370));
            Assert.That(clone.Header.PrimaryPlatformSignature, Is.EqualTo(0x4150504c));
            Assert.That(clone.Header.ProfileFlags, Is.EqualTo(3));
            Assert.That(clone.Header.IsEmbedded, Is.True);
            Assert.That(clone.Header.RequiresEmbeddedData, Is.True);
            Assert.That(clone.Header.RequiresMcsSubset, Is.True);
            Assert.That(clone.Header.UsesExtendedPcsRange, Is.True);
            Assert.That(clone.Header.DeviceManufacturerSignature, Is.EqualTo(0x4d414e55));
            Assert.That(clone.Header.DeviceModelSignature, Is.EqualTo(0x4d4f444c));
            Assert.That(clone.Header.DeviceAttributes, Is.EqualTo(0x0102030405060708));
            Assert.That(clone.Header.RenderingIntent, Is.EqualTo(2));
            Assert.That(clone.Header.RenderingIntentKind, Is.EqualTo(IccRenderingIntent.Saturation));
            Assert.That(clone.Header.PcsIlluminantX, Is.EqualTo(63190));
            Assert.That(clone.Header.PcsIlluminantY, Is.EqualTo(65536));
            Assert.That(clone.Header.PcsIlluminantZ, Is.EqualTo(54061));
            Assert.That(clone.Header.ProfileCreatorSignature, Is.EqualTo(0x43524541));
            Assert.That(clone.Header.ProfileId.IsPresent, Is.True);
            Assert.That(clone.Header.SpectralPcsSignature, Is.EqualTo(0x72737063));
            Assert.That(clone.Header.SpectralPcs, Is.EqualTo(IccColorSpaceKind.Spectral));
            Assert.That(clone.Header.SpectralPcsRangeStart, Is.EqualTo(380));
            Assert.That(clone.Header.SpectralPcsRangeEnd, Is.EqualTo(730));
            Assert.That(clone.Header.SpectralPcsRangeSteps, Is.EqualTo(36));
            Assert.That(clone.Header.BiSpectralPcsRangeStart, Is.EqualTo(400));
            Assert.That(clone.Header.BiSpectralPcsRangeEnd, Is.EqualTo(700));
            Assert.That(clone.Header.BiSpectralPcsRangeSteps, Is.EqualTo(31));
            Assert.That(clone.Header.MaterialConnectionSpaceSignature, Is.EqualTo(0x4d435320));
            Assert.That(clone.Header.MaterialConnectionSpace, Is.EqualTo(IccColorSpaceKind.Multiplex));
            Assert.That(clone.Header.ProfileSubclassSignature, Is.EqualTo(0x53554243));
            Assert.That(clone.Header.IccMaxReservedValue, Is.EqualTo(0x01020304));
            Assert.That(clone.TagTableEntries[0].SharedDataId, Is.EqualTo(7));
            Assert.That(clone.Tags[0].Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(clone.Tags[0].Data.RawData.TypeSignature, Is.EqualTo(0x74657874));
            Assert.That(clone.Tags[0].DecodeResult.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(clone.Tags[0].DecodeResult.Message, Is.EqualTo("retained raw"));
            Assert.That(clone.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(clone.ValidationIssues[0].RuleId, Is.EqualTo(IccValidationRuleIds.TagSize));
            Assert.That(clone.ValidationIssues[0], Is.SameAs(clone.Header.ValidationIssues[0]));
            Assert.That(clone.Location.Offset, Is.EqualTo(20));
            Assert.That(clone.Location.Length, Is.EqualTo(256));
            Assert.That(clone.ReadResult.Status, Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [Test]
    public void CloneProfileOwnsEveryMutableChildAndBuffer()
    {
        IccProfile source = CreatePopulatedProfile();

        IccProfile clone = ProfileClone.CloneProfile(source);
        clone.Header.Version.Major = 9;
        clone.Header.CreationDateTime.Year = 2000;
        clone.Header.ProfileId.Bytes[0] = 90;
        clone.Header.ContextualBytes[0] = 91;
        clone.Header.RawBytes[0] = 92;
        clone.TagTableEntries[0].Size = 99;
        clone.Tags[0].Data.RawData.Bytes[0] = 93;
        clone.Tags[0].PaddingBytes[0] = 94;
        clone.Tags[0].DecodeResult.Location.Offset = 95;
        clone.ValidationIssues[0].Message = "changed";
        clone.OriginalBytes[0] = 96;
        clone.TrailingBytes[0] = 97;
        clone.Location.Offset = 98;

        Assert.Multiple(() =>
        {
            Assert.That(source.Header.Version.Major, Is.EqualTo(5));
            Assert.That(source.Header.CreationDateTime.Year, Is.EqualTo(2026));
            Assert.That(source.Header.ProfileId.Bytes[0], Is.EqualTo(1));
            Assert.That(source.Header.ContextualBytes[0], Is.EqualTo(2));
            Assert.That(source.Header.RawBytes[0], Is.EqualTo(3));
            Assert.That(source.TagTableEntries[0].Size, Is.EqualTo(12));
            Assert.That(source.Tags[0].Data.RawData.Bytes[0], Is.EqualTo(4));
            Assert.That(source.Tags[0].PaddingBytes[0], Is.EqualTo(5));
            Assert.That(source.Tags[0].DecodeResult.Location.Offset, Is.EqualTo(144));
            Assert.That(source.ValidationIssues[0].Message, Is.EqualTo("issue"));
            Assert.That(source.OriginalBytes[0], Is.EqualTo(6));
            Assert.That(source.TrailingBytes[0], Is.EqualTo(7));
            Assert.That(source.Location.Offset, Is.EqualTo(20));
            Assert.That(clone.Header, Is.Not.SameAs(source.Header));
            Assert.That(clone.Header.Version, Is.Not.SameAs(source.Header.Version));
            Assert.That(clone.Header.CreationDateTime, Is.Not.SameAs(source.Header.CreationDateTime));
            Assert.That(clone.Header.ProfileId, Is.Not.SameAs(source.Header.ProfileId));
            Assert.That(clone.Header.ProfileId.Bytes, Is.Not.SameAs(source.Header.ProfileId.Bytes));
            Assert.That(clone.Tags, Is.Not.SameAs(source.Tags));
            Assert.That(clone.Tags[0], Is.Not.SameAs(source.Tags[0]));
            Assert.That(clone.Tags[0].Data, Is.Not.SameAs(source.Tags[0].Data));
            Assert.That(clone.Tags[0].Data.RawData.Bytes, Is.Not.SameAs(source.Tags[0].Data.RawData.Bytes));
            Assert.That(clone.ValidationIssues, Is.Not.SameAs(source.ValidationIssues));
            Assert.That(clone.ValidationIssues[0], Is.Not.SameAs(source.ValidationIssues[0]));
            Assert.That(clone.OriginalBytes, Is.Not.SameAs(source.OriginalBytes));
        });
    }

    [Test]
    public void CloneProfilePreservesSharingOnlyInsideTheClonedGraph()
    {
        IccProfile source = CreatePopulatedProfile();
        source.TagTableEntries = new IccTagTableEntry[] { source.Tags[0].TableEntry, source.Tags[0].TableEntry };
        source.Tags = new IccTag[] { source.Tags[0], source.Tags[0] };
        source.Header.ContextualBytes = source.OriginalBytes;
        source.Tags[0].PaddingBytes = source.OriginalBytes;
        source.Tags[0].DecodeResult = source.ReadResult;
        source.ReadResult.Location = source.Location;

        IccProfile clone = ProfileClone.CloneProfile(source);

        Assert.Multiple(() =>
        {
            Assert.That(clone.TagTableEntries[0], Is.SameAs(clone.TagTableEntries[1]));
            Assert.That(clone.TagTableEntries[0], Is.SameAs(clone.Tags[0].TableEntry));
            Assert.That(clone.Tags[0], Is.SameAs(clone.Tags[1]));
            Assert.That(clone.Header.ContextualBytes, Is.SameAs(clone.OriginalBytes));
            Assert.That(clone.Tags[0].PaddingBytes, Is.SameAs(clone.OriginalBytes));
            Assert.That(clone.Tags[0].DecodeResult, Is.SameAs(clone.ReadResult));
            Assert.That(clone.ReadResult.Location, Is.SameAs(clone.Location));
            Assert.That(clone.Tags[0], Is.Not.SameAs(source.Tags[0]));
            Assert.That(clone.TagTableEntries[0], Is.Not.SameAs(source.TagTableEntries[0]));
            Assert.That(clone.OriginalBytes, Is.Not.SameAs(source.OriginalBytes));
            Assert.That(clone.ReadResult, Is.Not.SameAs(source.ReadResult));
        });
    }

    [Test]
    public void StandaloneTagDataCloneDoesNotAliasRawPayload()
    {
        IccTagData source = new IccTagData();
        source.Kind = IccTagDataKind.Binary;
        source.RawData.TypeSignature = 0x64617461;
        source.RawData.HasTypeSignature = true;
        source.RawData.Bytes = new byte[] { 1, 2, 3 };

        IccTagData clone = ProfileClone.CloneTagDataStandalone(source);
        clone.RawData.Bytes[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(clone.Kind, Is.EqualTo(IccTagDataKind.Binary));
            Assert.That(clone.RawData.TypeSignature, Is.EqualTo(0x64617461));
            Assert.That(clone.RawData.HasTypeSignature, Is.True);
            Assert.That(source.RawData.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(clone.RawData, Is.Not.SameAs(source.RawData));
            Assert.That(clone.RawData.Bytes, Is.Not.SameAs(source.RawData.Bytes));
        });
    }

    [Test]
    public void CloneRejectsNullRequiredGraphMembersWithValidationErrors()
    {
        IccProfile nullHeader = new IccProfile();
        nullHeader.Header = null!;
        IccProfile nullTag = new IccProfile();
        nullTag.Tags = new IccTag[] { null! };
        IccTagData nullRawData = new IccTagData();
        nullRawData.RawData = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() => ProfileClone.CloneProfile(null!));
            Assert.Throws<IccProfileValidationException>(() => ProfileClone.CloneProfile(nullHeader));
            Assert.Throws<IccProfileValidationException>(() => ProfileClone.CloneProfile(nullTag));
            Assert.Throws<IccProfileValidationException>(() => ProfileClone.CloneTagDataStandalone(nullRawData));
        });
    }

    private static IccProfile CreatePopulatedProfile()
    {
        IccProfile profile = new IccProfile();
        profile.Header.DeclaredSize = 256;
        profile.Header.PreferredCmmSignature = 0x434d4d20;
        profile.Header.Version.Kind = IccVersionKind.Version5;
        profile.Header.Version.Major = 5;
        profile.Header.Version.Minor = 1;
        profile.Header.Version.BugFix = 2;
        profile.Header.Version.SubclassMajor = 3;
        profile.Header.Version.SubclassMinor = 4;
        profile.Header.Version.IsSupported = false;
        profile.Header.Version.IsFutureMinor = true;
        profile.Header.Version.PackedValue = 0x05120304;
        profile.Header.ProfileClass = IccProfileClass.Display;
        profile.Header.ProfileClassSignature = 0x6d6e7472;
        profile.Header.DataColorSpaceSignature = 0x52474220;
        profile.Header.DataColorSpace = IccColorSpaceKind.Rgb;
        profile.Header.ProfileConnectionSpaceSignature = 0x58595a20;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Header.CreationDateTime.Year = 2026;
        profile.Header.CreationDateTime.Month = 9;
        profile.Header.CreationDateTime.Day = 11;
        profile.Header.CreationDateTime.Hour = 12;
        profile.Header.CreationDateTime.Minute = 34;
        profile.Header.CreationDateTime.Second = 56;
        profile.Header.ProfileFileSignature = 0x61637370;
        profile.Header.PrimaryPlatformSignature = 0x4150504c;
        profile.Header.ProfileFlags = 3;
        profile.Header.IsEmbedded = true;
        profile.Header.RequiresEmbeddedData = true;
        profile.Header.RequiresMcsSubset = true;
        profile.Header.UsesExtendedPcsRange = true;
        profile.Header.DeviceManufacturerSignature = 0x4d414e55;
        profile.Header.DeviceModelSignature = 0x4d4f444c;
        profile.Header.DeviceAttributes = 0x0102030405060708;
        profile.Header.RenderingIntent = 2;
        profile.Header.RenderingIntentKind = IccRenderingIntent.Saturation;
        profile.Header.PcsIlluminantX = 63190;
        profile.Header.PcsIlluminantY = 65536;
        profile.Header.PcsIlluminantZ = 54061;
        profile.Header.ProfileCreatorSignature = 0x43524541;
        profile.Header.ProfileId.Bytes = new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2 };
        profile.Header.ProfileId.IsPresent = true;
        profile.Header.SpectralPcsSignature = 0x72737063;
        profile.Header.SpectralPcs = IccColorSpaceKind.Spectral;
        profile.Header.SpectralPcsRangeStart = 380;
        profile.Header.SpectralPcsRangeEnd = 730;
        profile.Header.SpectralPcsRangeSteps = 36;
        profile.Header.BiSpectralPcsRangeStart = 400;
        profile.Header.BiSpectralPcsRangeEnd = 700;
        profile.Header.BiSpectralPcsRangeSteps = 31;
        profile.Header.MaterialConnectionSpaceSignature = 0x4d435320;
        profile.Header.MaterialConnectionSpace = IccColorSpaceKind.Multiplex;
        profile.Header.ProfileSubclassSignature = 0x53554243;
        profile.Header.IccMaxReservedValue = 0x01020304;
        profile.Header.ContextualBytes = new byte[] { 2, 0 };
        profile.Header.RawBytes = new byte[] { 3, 0 };

        IccTagTableEntry entry = new IccTagTableEntry();
        entry.Signature = 0x63707274;
        entry.Offset = 144;
        entry.Size = 12;
        entry.OriginalIndex = 0;
        entry.SharedDataId = 7;
        IccTag tag = new IccTag();
        tag.Signature = entry.Signature;
        tag.TableEntry = entry;
        tag.Data.Kind = IccTagDataKind.Raw;
        tag.Data.RawData.TypeSignature = 0x74657874;
        tag.Data.RawData.HasTypeSignature = true;
        tag.Data.RawData.Bytes = new byte[] { 4, 0 };
        tag.PaddingBytes = new byte[] { 5 };
        tag.IsDecodeAttempted = true;
        tag.DecodeResult.Status = IccOperationStatus.InvalidData;
        tag.DecodeResult.Message = "retained raw";
        tag.DecodeResult.Location.Offset = 144;
        tag.DecodeResult.Location.Length = 12;
        profile.TagTableEntries = new IccTagTableEntry[] { entry };
        profile.Tags = new IccTag[] { tag };
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = IccValidationRuleIds.TagSize;
        issue.Message = "issue";
        profile.ValidationIssues = new IccValidationIssue[] { issue };
        profile.Header.ValidationIssues = profile.ValidationIssues;
        profile.OriginalBytes = new byte[] { 6, 0 };
        profile.TrailingBytes = new byte[] { 7, 0 };
        profile.Location.Offset = 20;
        profile.Location.Length = 256;
        profile.ReadResult.Status = IccOperationStatus.Success;
        profile.ReadResult.Message = "read";
        profile.ReadResult.Location.Offset = 20;
        profile.ReadResult.Location.Length = 256;
        return profile;
    }
}
