using icc;

namespace icc.Tests;

public class HeaderValidationTests
{
    [Test]
    public void ValidVersion2Version4AndVersion5HeadersHaveNoHeaderIssues()
    {
        IIccProfileValidator validator = new IccProfileValidator();
        IccReadOptions options = new IccReadOptions();

        IccValidationReport version2 = validator.ValidateProfileBytesReport(CreateProfileBytes(0x02100000), options);
        IccValidationReport version4 = validator.ValidateProfileBytesReport(CreateProfileBytes(0x04400000), options);
        IccValidationReport version5 = validator.ValidateProfileBytesReport(CreateProfileBytes(0x05000000), options);

        Assert.Multiple(() =>
        {
            Assert.That(version2.Issues, Has.Length.EqualTo(5));
            Assert.That(version2.Issues, Has.All.Matches<IccValidationIssue>(issue =>
                issue.RuleId == IccValidationRuleIds.RequiredTag));
            Assert.That(version4.Issues, Has.Length.EqualTo(5));
            Assert.That(version4.Issues, Has.All.Matches<IccValidationIssue>(issue =>
                issue.RuleId == IccValidationRuleIds.RequiredTag));
            Assert.That(version5.IsValid, Is.True);
            Assert.That(version5.Issues, Is.Empty);
        });
    }

    [Test]
    public void CommonHeaderDefectsReportEveryFieldCategoryAndExactLocations()
    {
        IccProfile profile = CreateValidModel(0x04400000);
        profile.Location.Offset = 1000;
        profile.Header.DeclaredSize = 131;
        profile.Header.Version.Minor = 5;
        profile.Header.ProfileClassSignature = 0;
        profile.Header.ProfileClass = IccProfileClass.Unknown;
        profile.Header.DataColorSpaceSignature = 0;
        profile.Header.DataColorSpace = IccColorSpaceKind.Unknown;
        profile.Header.ProfileConnectionSpaceSignature = 0;
        profile.Header.ProfileConnectionSpace = IccPcsKind.None;
        profile.Header.CreationDateTime.Year = 2023;
        profile.Header.CreationDateTime.Month = 2;
        profile.Header.CreationDateTime.Day = 29;
        profile.Header.ProfileFileSignature = 0x42414421;
        profile.Header.PrimaryPlatformSignature = 0x42414421;
        profile.Header.ProfileFlags = 0x00000100;
        profile.Header.PreferredCmmSignature = 1;
        profile.Header.DeviceManufacturerSignature = 2;
        profile.Header.DeviceModelSignature = 3;
        profile.Header.ProfileCreatorSignature = 4;
        profile.Header.DeviceAttributes = 0x00000100;
        profile.Header.RenderingIntent = 4;
        profile.Header.RenderingIntentKind = (IccRenderingIntent)4;
        profile.Header.PcsIlluminantX = 0;
        profile.Header.PcsIlluminantY = 0;
        profile.Header.PcsIlluminantZ = 0;
        profile.Header.ProfileId.Bytes = new byte[15];
        profile.Header.ProfileId.IsPresent = true;

        IccValidationReport report = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.False);
            AssertRule(report, IccValidationRuleIds.HeaderProfileSize);
            AssertRule(report, IccValidationRuleIds.HeaderVersion);
            AssertRule(report, IccValidationRuleIds.HeaderProfileClass);
            AssertRule(report, IccValidationRuleIds.HeaderColorSpace);
            AssertRule(report, IccValidationRuleIds.HeaderPcs);
            AssertRule(report, IccValidationRuleIds.HeaderDateTime);
            AssertRule(report, IccValidationRuleIds.HeaderFileSignature);
            AssertRule(report, IccValidationRuleIds.HeaderPlatform);
            AssertRule(report, IccValidationRuleIds.HeaderRegisteredSignature);
            AssertRule(report, IccValidationRuleIds.HeaderFlags);
            AssertRule(report, IccValidationRuleIds.HeaderDeviceAttributes);
            AssertRule(report, IccValidationRuleIds.HeaderRenderingIntent);
            AssertRule(report, IccValidationRuleIds.HeaderPcsIlluminant);
            AssertRule(report, IccValidationRuleIds.HeaderProfileId);
            IccValidationIssue platform = FindRule(report, IccValidationRuleIds.HeaderPlatform);
            Assert.That(platform.Path, Is.EqualTo("Header.PrimaryPlatformSignature"));
            Assert.That(platform.Location.Offset, Is.EqualTo(1040));
            Assert.That(platform.Location.Length, Is.EqualTo(4));
            Assert.That(platform.Remediation, Is.Not.Empty);
        });
    }

    [Test]
    public void Version5SpectralMcsSubclassAndReservedFieldsAreValidated()
    {
        IccProfile profile = CreateValidModel(0x05000000);
        profile.Location.Offset = 0;
        profile.Header.ProfileConnectionSpaceSignature = 0;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Spectral;
        profile.Header.SpectralPcsSignature = IccColorSpaceSignatures.ReflectanceSpectralPrefix | 36;
        profile.Header.SpectralPcs = IccColorSpaceKind.Spectral;
        profile.Header.SpectralPcsRangeStart = 380;
        profile.Header.SpectralPcsRangeEnd = 730;
        profile.Header.SpectralPcsRangeSteps = 36;

        IccValidationReport valid = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        profile.Header.SpectralPcsSignature = IccColorSpaceSignatures.ReflectanceSpectralPrefix | 35;
        profile.Header.BiSpectralPcsRangeStart = 400;
        profile.Header.BiSpectralPcsRangeEnd = 700;
        profile.Header.BiSpectralPcsRangeSteps = 31;
        profile.Header.MaterialConnectionSpaceSignature = IccColorSpaceSignatures.MultiplexPrefix | 3;
        profile.Header.MaterialConnectionSpace = IccColorSpaceKind.Multiplex;
        profile.Header.ProfileSubclassSignature = 0x53554243;
        profile.Header.IccMaxReservedValue = 1;

        IccValidationReport invalid = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid, Is.True);
            Assert.That(valid.Issues, Is.Empty);
            AssertRule(invalid, IccValidationRuleIds.HeaderSpectralPcs);
            AssertRule(invalid, IccValidationRuleIds.HeaderMcs);
            AssertRule(invalid, IccValidationRuleIds.HeaderProfileSubclass);
            AssertRule(invalid, IccValidationRuleIds.HeaderReserved);
            Assert.That(FindRule(invalid, IccValidationRuleIds.HeaderSpectralPcs).Location.Offset, Is.EqualTo(100));
            Assert.That(FindRule(invalid, IccValidationRuleIds.HeaderMcs).Location.Offset, Is.EqualTo(116));
        });
    }

    [Test]
    public void ColorEncodingSpaceUsesItsMinimalReservedHeaderForm()
    {
        IccProfile profile = CreateValidModel(0x05000000);
        profile.Location.Offset = 0;
        profile.Header.ProfileClassSignature = IccProfileClassSignatures.ColorEncodingSpace;
        profile.Header.ProfileClass = IccProfileClass.ColorEncodingSpace;
        profile.Header.ProfileConnectionSpaceSignature = 0;
        profile.Header.ProfileConnectionSpace = IccPcsKind.None;
        profile.Header.CreationDateTime = new IccDateTime();
        profile.Header.PcsIlluminantX = 0;
        profile.Header.PcsIlluminantY = 0;
        profile.Header.PcsIlluminantZ = 0;

        IccValidationReport valid = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        profile.Header.PreferredCmmSignature = 0x74657374;
        profile.Header.CreationDateTime.Year = 2026;
        profile.Header.ProfileFlags = IccConstants.EmbeddedProfileFlag;
        profile.Header.IsEmbedded = true;
        IccValidationReport invalid = new IccProfileValidator().ValidateProfileDataReport(
            profile, new IccValidationPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid, Is.True);
            Assert.That(valid.Issues, Is.Empty);
            Assert.That(invalid.Issues.Count(issue => issue.RuleId == IccValidationRuleIds.HeaderReserved),
                Is.EqualTo(3));
            Assert.That(invalid.Issues.Where(issue => issue.RuleId == IccValidationRuleIds.HeaderReserved)
                .Select(issue => issue.Location.Offset), Is.EqualTo(new long[] { 4, 20, 40 }));
        });
    }

    [Test]
    public void StoredProfileIdIsCheckedAgainstRetainedProfileBytes()
    {
        byte[] bytes = CreateProfileBytes(0x04400000);
        IccProfileId id = new IccProfileIdCalculator().ComputeProfileIdBytes(bytes);
        Array.Copy(id.Bytes, 0, bytes, 84, IccConstants.ProfileIdSize);
        IccReadOptions options = new IccReadOptions();
        IccValidationReport valid = new IccProfileValidator().ValidateProfileBytesReport(bytes, options);

        WriteUInt32(bytes, 80, 0x54455354);
        IccValidationReport stale = new IccProfileValidator().ValidateProfileBytesReport(bytes, options);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Issues, Has.Length.EqualTo(5));
            Assert.That(valid.Issues, Has.All.Matches<IccValidationIssue>(issue =>
                issue.RuleId == IccValidationRuleIds.RequiredTag));
            AssertRule(stale, IccValidationRuleIds.HeaderProfileId);
            Assert.That(FindRule(stale, IccValidationRuleIds.HeaderProfileId).Location.Offset, Is.EqualTo(84));
            Assert.That(FindRule(stale, IccValidationRuleIds.HeaderProfileId).Message, Does.Contain("MD5"));
        });
    }

    private static IccProfile CreateValidModel(uint version)
    {
        IccProfile profile = new IccProfile();
        profile.Header.DeclaredSize = IccConstants.MinimumProfileSize;
        profile.Header.Version = HeaderCodec.DecodeVersion(version);
        profile.Header.ProfileClassSignature = IccProfileClassSignatures.Display;
        profile.Header.ProfileClass = IccProfileClass.Display;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        profile.Header.DataColorSpace = IccColorSpaceKind.Rgb;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Header.CreationDateTime.Year = 2024;
        profile.Header.CreationDateTime.Month = 2;
        profile.Header.CreationDateTime.Day = 29;
        profile.Header.ProfileFileSignature = IccConstants.ProfileFileSignature;
        profile.Header.RenderingIntentKind = IccRenderingIntent.Perceptual;
        profile.Header.PcsIlluminantX = IccStandardColorimetry.D50EncodedX;
        profile.Header.PcsIlluminantY = IccStandardColorimetry.D50EncodedY;
        profile.Header.PcsIlluminantZ = IccStandardColorimetry.D50EncodedZ;
        return profile;
    }

    private static byte[] CreateProfileBytes(uint version)
    {
        byte[] bytes = new byte[IccConstants.MinimumProfileSize];
        WriteUInt32(bytes, 0, IccConstants.MinimumProfileSize);
        WriteUInt32(bytes, 8, version);
        WriteUInt32(bytes, 12, IccProfileClassSignatures.Display);
        WriteUInt32(bytes, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(bytes, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt16(bytes, 24, 2024);
        WriteUInt16(bytes, 26, 2);
        WriteUInt16(bytes, 28, 29);
        WriteUInt32(bytes, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(bytes, 68, (uint)IccStandardColorimetry.D50EncodedX);
        WriteUInt32(bytes, 72, (uint)IccStandardColorimetry.D50EncodedY);
        WriteUInt32(bytes, 76, (uint)IccStandardColorimetry.D50EncodedZ);
        return bytes;
    }

    private static void AssertRule(IccValidationReport report, string ruleId)
    {
        Assert.That(report.Issues.Select(issue => issue.RuleId), Does.Contain(ruleId));
    }

    private static IccValidationIssue FindRule(IccValidationReport report, string ruleId)
    {
        return report.Issues.First(issue => issue.RuleId == ruleId);
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
