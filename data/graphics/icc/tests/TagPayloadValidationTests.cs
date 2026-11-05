using System.IO.Compression;
using System.Text;
using icc;

namespace icc.Tests;

public class TagPayloadValidationTests
{
    [Test]
    public void KnownRawPayloadIsAcceptedAndReservedBytesHavePreciseDiagnostics()
    {
        byte[] payload = CreatePayload(IccTypeSignatures.Text,
            new byte[] { (byte)'O', (byte)'K', 0 });
        IccProfile profile = CreateModelProfile(payload, IccTagSignatures.CharacterizationTarget,
            IccVersionKind.Version4);

        IccValidationReport valid = Validate(profile, true);
        payload[6] = 1;
        IccValidationReport invalid = Validate(profile, true);

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid, Is.True);
            Assert.That(valid.Issues, Is.Empty);
            Assert.That(invalid.Issues.Count(issue =>
                issue.RuleId == IccValidationRuleIds.TagReserved), Is.EqualTo(1));
            IccValidationIssue issue = FindRule(invalid, IccValidationRuleIds.TagReserved);
            Assert.That(issue.Path, Is.EqualTo("Tags[0].Data.Reserved"));
            Assert.That(issue.Location.Offset, Is.EqualTo(148));
            Assert.That(issue.Location.Length, Is.EqualTo(4));
            Assert.That(issue.Remediation, Does.Contain("zero"));
        });
    }

    [Test]
    public void PrivateTagAndTypeRegistrationStatusHonorsPolicy()
    {
        uint privateTag = 0x70726976;
        uint privateType = 0x70727654;
        IccProfile profile = CreateModelProfile(CreatePayload(privateType, new byte[0]),
            privateTag, IccVersionKind.Version4);
        IccProfile registeredVendor = CreateModelProfile(CreatePayload(IccTypeSignatures.Text,
            new byte[] { 0 }), IccTagSignatures.MicrosoftGammaRamp,
            IccVersionKind.Version4);

        IccValidationReport allowed = Validate(profile, true);
        IccValidationReport denied = Validate(profile, false);
        IccValidationReport registered = Validate(registeredVendor, false);

        Assert.Multiple(() =>
        {
            Assert.That(allowed.IsValid, Is.True);
            Assert.That(allowed.InformationCount, Is.EqualTo(2));
            Assert.That(allowed.Issues.Select(issue => issue.RuleId),
                Is.All.EqualTo(IccValidationRuleIds.TagTypeCompatibility));
            Assert.That(allowed.Issues.Select(issue => issue.Path), Is.EquivalentTo(new[]
            {
                "Tags[0].Signature",
                "Tags[0].Data.RawData.TypeSignature"
            }));
            Assert.That(denied.IsValid, Is.False);
            Assert.That(denied.ErrorCount, Is.EqualTo(2));
            Assert.That(denied.Issues, Has.All.Matches<IccValidationIssue>(issue =>
                issue.Message.Contains("private", StringComparison.OrdinalIgnoreCase)));
            Assert.That(registered.Issues.Select(issue => issue.RuleId),
                Does.Not.Contain(IccValidationRuleIds.TagTypeCompatibility));
        });
    }

    [Test]
    public void RawExtentAndStringEncodingAreValidatedWithoutTypedDecoding()
    {
        IccProfile invalidUtf8 = CreateModelProfile(CreatePayload(IccTypeSignatures.Utf8,
            new byte[] { 0xe0, 0x80, 0x80 }), IccTagSignatures.Copyright,
            IccVersionKind.Version5);
        IccProfile mismatchedExtent = CreateModelProfile(CreatePayload(IccTypeSignatures.Text,
            new byte[] { 0 }), IccTagSignatures.Copyright, IccVersionKind.Version4);
        mismatchedExtent.Tags[0].TableEntry.Size = mismatchedExtent.Tags[0].TableEntry.Size + 4;
        mismatchedExtent.Header.DeclaredSize = mismatchedExtent.Header.DeclaredSize + 4;
        mismatchedExtent.Location.Length = mismatchedExtent.Location.Length + 4;

        IccValidationReport utf8Report = Validate(invalidUtf8, true);
        IccValidationReport extentReport = Validate(mismatchedExtent, true);

        Assert.Multiple(() =>
        {
            IccValidationIssue encoding = FindRule(utf8Report, IccValidationRuleIds.TextEncoding);
            Assert.That(encoding.Path, Is.EqualTo("Tags[0].Data"));
            Assert.That(encoding.Location.Offset, Is.EqualTo(152));
            Assert.That(encoding.Location.Length, Is.EqualTo(3));
            Assert.That(encoding.Message, Does.Contain("UTF-8"));
            IccValidationIssue extent = FindRule(extentReport, IccValidationRuleIds.TagSize);
            Assert.That(extent.Message, Does.Contain("retained payload"));
            Assert.That(extent.Remediation, Does.Contain("exactly"));
        });
    }

    [Test]
    public void DecoderCountAndOffsetFailuresUseGenericPayloadRules()
    {
        byte[] badCount = CreatePayload(IccTypeSignatures.MultiLocalizedUnicode,
            new byte[8]);
        WriteUInt32(badCount, 8, 100);
        WriteUInt32(badCount, 12, 12);
        byte[] badOffset = CreatePayload(IccTypeSignatures.MultiLocalizedUnicode,
            new byte[23]);
        WriteUInt32(badOffset, 8, 1);
        WriteUInt32(badOffset, 12, 12);
        WriteUInt16(badOffset, 16, 0x656e);
        WriteUInt16(badOffset, 18, 0x5553);
        WriteUInt32(badOffset, 20, 2);
        WriteUInt32(badOffset, 24, 29);
        badOffset[29] = 0;
        badOffset[30] = 65;

        IccValidationReport countReport = ValidateEncodedProfile(badCount,
            IccTagSignatures.Copyright, 0x05000000, new IccReadOptions());
        IccValidationReport offsetReport = ValidateEncodedProfile(badOffset,
            IccTagSignatures.Copyright, 0x05000000, new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(FindRule(countReport, IccValidationRuleIds.TagSize).Message,
                Does.Contain("records"));
            IccValidationIssue offset = FindRule(offsetReport,
                IccValidationRuleIds.TagNestedOffset);
            Assert.That(offset.Message, Does.Contain("aligned"));
            Assert.That(offset.Path, Is.EqualTo("Tags[0]"));
        });
    }

    [Test]
    public void CompressedPayloadExpansionUsesCompressionLimitRule()
    {
        byte[] compressed = Compress(Encoding.UTF8.GetBytes(new string('A', 200)));
        byte[] payload = CreatePayload(IccTypeSignatures.ZippedUtf8, compressed);
        IccReadOptions options = new IccReadOptions();
        options.ResourceLimits.MaximumDecompressedBytes = 32;

        IccValidationReport report = ValidateEncodedProfile(payload,
            IccTagSignatures.CharacterizationTarget, 0x05000000, options);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.False);
            Assert.That(report.FatalCount, Is.EqualTo(1));
            IccValidationIssue issue = FindRule(report, IccValidationRuleIds.CompressionLimit);
            Assert.That(issue.Message, Does.Contain("32 bytes"));
            Assert.That(issue.Remediation, Is.Not.Empty);
        });
    }

    [Test]
    public void LazyPayloadValidationDoesNotForceTagDecoding()
    {
        byte[] payload = CreatePayload(IccTypeSignatures.Text,
            new byte[] { (byte)'A', 0 });
        payload[4] = 1;
        byte[] bytes = CreateEncodedProfile(payload, IccTagSignatures.Copyright,
            0x04400000);
        IccReadOptions options = new IccReadOptions();
        options.DecodeTagsLazily = true;
        options.PreserveOriginalBytes = false;
        IccProfile profile = new IccProfileReader().ReadProfileBytes(bytes, options);

        IccValidationReport report = Validate(profile, true);

        Assert.Multiple(() =>
        {
            Assert.That(report.Issues.Count(issue =>
                issue.RuleId == IccValidationRuleIds.TagReserved), Is.EqualTo(1));
            Assert.That(profile.Tags[0].IsDecodeAttempted, Is.False);
            Assert.That(profile.Tags[0].Data.RawData.Bytes, Is.Empty);
        });
    }

    private static IccValidationReport Validate(IccProfile profile, bool allowPrivate)
    {
        IccValidationPolicy policy = new IccValidationPolicy();
        policy.AllowPrivateSignatures = allowPrivate;
        policy.SuppressedRuleIds = new string[] { IccValidationRuleIds.RequiredTag };
        return new IccProfileValidator().ValidateProfileDataReport(profile, policy);
    }

    private static IccValidationReport ValidateEncodedProfile(byte[] payload,
        uint tagSignature, uint packedVersion, IccReadOptions options)
    {
        byte[] profile = CreateEncodedProfile(payload, tagSignature, packedVersion);
        return new IccProfileValidator().ValidateProfileBytesReport(profile, options);
    }

    private static IccProfile CreateModelProfile(byte[] payload, uint tagSignature,
        IccVersionKind version)
    {
        int payloadEnd = 144 + payload.Length;
        int declaredSize = (payloadEnd + 3) & -4;
        IccProfile profile = new IccProfile();
        profile.Location.Offset = 0;
        profile.Location.Length = declaredSize;
        profile.Header.DeclaredSize = declaredSize;
        profile.Header.Version = HeaderCodec.DecodeVersion(version == IccVersionKind.Version5
            ? 0x05000000u : version == IccVersionKind.Version2 ? 0x02100000u : 0x04400000u);
        profile.Header.ProfileClassSignature = IccProfileClassSignatures.Display;
        profile.Header.ProfileClass = IccProfileClass.Display;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        profile.Header.DataColorSpace = IccColorSpaceKind.Rgb;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Header.CreationDateTime.Year = 2026;
        profile.Header.CreationDateTime.Month = 1;
        profile.Header.CreationDateTime.Day = 1;
        profile.Header.ProfileFileSignature = IccConstants.ProfileFileSignature;
        profile.Header.PcsIlluminantX = IccStandardColorimetry.D50EncodedX;
        profile.Header.PcsIlluminantY = IccStandardColorimetry.D50EncodedY;
        profile.Header.PcsIlluminantZ = IccStandardColorimetry.D50EncodedZ;
        IccTagTableEntry entry = new IccTagTableEntry();
        entry.Signature = tagSignature;
        entry.Offset = 144;
        entry.Size = payload.Length;
        entry.OriginalIndex = 0;
        IccTag tag = new IccTag();
        tag.Signature = tagSignature;
        tag.TableEntry = entry;
        tag.Data.Kind = IccTagDataKind.Raw;
        tag.Data.RawData.Bytes = payload;
        tag.Data.RawData.HasTypeSignature = payload.Length >= 4;
        tag.Data.RawData.TypeSignature = payload.Length >= 4
            ? ReadUInt32(payload, 0) : 0;
        tag.PaddingBytes = new byte[declaredSize - payloadEnd];
        profile.TagTableEntries = new IccTagTableEntry[] { entry };
        profile.Tags = new IccTag[] { tag };
        return profile;
    }

    private static byte[] CreateEncodedProfile(byte[] payload, uint tagSignature,
        uint packedVersion)
    {
        int payloadEnd = 144 + payload.Length;
        int declaredSize = (payloadEnd + 3) & -4;
        byte[] bytes = new byte[declaredSize];
        WriteUInt32(bytes, 0, (uint)declaredSize);
        WriteUInt32(bytes, 8, packedVersion);
        WriteUInt32(bytes, 12, IccProfileClassSignatures.Display);
        WriteUInt32(bytes, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(bytes, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt16(bytes, 24, 2026);
        WriteUInt16(bytes, 26, 1);
        WriteUInt16(bytes, 28, 1);
        WriteUInt32(bytes, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(bytes, 68, (uint)IccStandardColorimetry.D50EncodedX);
        WriteUInt32(bytes, 72, (uint)IccStandardColorimetry.D50EncodedY);
        WriteUInt32(bytes, 76, (uint)IccStandardColorimetry.D50EncodedZ);
        WriteUInt32(bytes, 128, 1);
        WriteUInt32(bytes, 132, tagSignature);
        WriteUInt32(bytes, 136, 144);
        WriteUInt32(bytes, 140, (uint)payload.Length);
        Array.Copy(payload, 0, bytes, 144, payload.Length);
        return bytes;
    }

    private static byte[] CreatePayload(uint typeSignature, byte[] content)
    {
        byte[] payload = new byte[8 + content.Length];
        WriteUInt32(payload, 0, typeSignature);
        Array.Copy(content, 0, payload, 8, content.Length);
        return payload;
    }

    private static byte[] Compress(byte[] source)
    {
        using MemoryStream destination = new MemoryStream();
        using (ZLibStream compression = new ZLibStream(destination,
            CompressionLevel.SmallestSize, true))
        {
            compression.Write(source);
        }
        return destination.ToArray();
    }

    private static IccValidationIssue FindRule(IccValidationReport report, string ruleId)
    {
        return report.Issues.First(issue => issue.RuleId == ruleId);
    }

    private static uint ReadUInt32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24) | ((uint)source[offset + 1] << 16)
            | ((uint)source[offset + 2] << 8) | source[offset + 3];
    }

    private static void WriteUInt16(byte[] destination, int offset, int value)
    {
        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)value;
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
