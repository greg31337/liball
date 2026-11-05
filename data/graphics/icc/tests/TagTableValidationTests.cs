using icc;

namespace icc.Tests;

public class TagTableValidationTests
{
    [Test]
    public void ContiguousTableWithExactSharedPayloadAndZeroPaddingIsValid()
    {
        IccProfile profile = CreateProfile(188, 3);
        SetEntry(profile, 0, IccTagSignatures.Copyright, 168, 8, -1, new byte[0]);
        SetEntry(profile, 1, IccTagSignatures.ProfileDescription, 176, 9, 0, new byte[3]);
        SetEntry(profile, 2, IccTagSignatures.MediaWhitePoint, 176, 9, 0, new byte[3]);

        IccValidationReport report = Validate(profile);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True);
            Assert.That(report.Issues, Is.Empty);
        });
    }

    [Test]
    public void CountAlignmentBoundsAndDuplicateSignaturesHaveStableEntryDiagnostics()
    {
        IccProfile profile = CreateProfile(204, 3);
        SetEntry(profile, 0, IccTagSignatures.Copyright, 164, 8, -1, new byte[0]);
        SetEntry(profile, 1, IccTagSignatures.Copyright, 171, 8, -1, new byte[1]);
        SetEntry(profile, 2, IccTagSignatures.ProfileDescription, 196, 20, -1, new byte[0]);
        profile.Tags = new IccTag[] { profile.Tags[0], profile.Tags[1] };

        IccValidationReport report = Validate(profile);

        Assert.Multiple(() =>
        {
            AssertRule(report, IccValidationRuleIds.TagTableCount);
            AssertRule(report, IccValidationRuleIds.TagTableAlignment);
            AssertRule(report, IccValidationRuleIds.TagTableBounds);
            AssertRule(report, IccValidationRuleIds.TagTableDuplicateSignature);
            IccValidationIssue duplicate = FindRule(report,
                IccValidationRuleIds.TagTableDuplicateSignature);
            Assert.That(duplicate.Path, Is.EqualTo("TagTableEntries[1].Signature"));
            Assert.That(duplicate.Location.Offset, Is.EqualTo(144));
            Assert.That(duplicate.Location.Length, Is.EqualTo(4));
            Assert.That(duplicate.Remediation, Is.Not.Empty);
            Assert.That(report.Issues, Has.All.Matches<IccValidationIssue>(issue =>
                issue.Location.Length >= 0 && issue.Path.Length != 0 && issue.Remediation.Length != 0));
        });
    }

    [Test]
    public void SharingPartialOverlapGapsPaddingAndExactEndAreValidated()
    {
        IccProfile profile = CreateProfile(216, 4);
        SetEntry(profile, 0, IccTagSignatures.Copyright, 180, 9, 0,
            new byte[] { 1, 0, 0 });
        SetEntry(profile, 1, IccTagSignatures.ProfileDescription, 180, 9, 1,
            new byte[3]);
        SetEntry(profile, 2, IccTagSignatures.MediaWhitePoint, 188, 8, -1,
            new byte[0]);
        SetEntry(profile, 3, IccTagSignatures.ChromaticAdaptation, 204, 8, -1,
            new byte[0]);

        IccValidationReport report = Validate(profile);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.False);
            AssertRule(report, IccValidationRuleIds.TagTableOverlap);
            AssertRule(report, IccValidationRuleIds.TagTableGap);
            AssertRule(report, IccValidationRuleIds.TagTablePadding);
            AssertRule(report, IccValidationRuleIds.ProfileExactEnd);
            Assert.That(report.Issues.Count(issue =>
                issue.RuleId == IccValidationRuleIds.TagTableOverlap), Is.GreaterThanOrEqualTo(2));
            Assert.That(FindRule(report, IccValidationRuleIds.TagTablePadding).Severity,
                Is.EqualTo(IccValidationSeverity.Warning));
            Assert.That(FindRule(report, IccValidationRuleIds.ProfileExactEnd).Location.Offset,
                Is.EqualTo(212));
            Assert.That(FindRule(report, IccValidationRuleIds.ProfileExactEnd).Location.Length,
                Is.EqualTo(4));
        });
    }

    [Test]
    public void BytesAfterDeclaredProfileAreReportedEvenWhenReadingAllowsThem()
    {
        byte[] bytes = CreateEmptyProfileBytes(135);
        bytes[132] = 1;
        bytes[133] = 2;
        bytes[134] = 3;
        IccReadOptions options = new IccReadOptions();
        options.RequireExactProfileSize = false;

        IccValidationReport report = new IccProfileValidator().ValidateProfileBytesReport(bytes, options);

        Assert.Multiple(() =>
        {
            AssertRule(report, IccValidationRuleIds.ProfileExactEnd);
            IccValidationIssue issue = FindRule(report, IccValidationRuleIds.ProfileExactEnd);
            Assert.That(issue.Path, Is.EqualTo("Profile.TrailingBytes"));
            Assert.That(issue.Location.Offset, Is.EqualTo(132));
            Assert.That(issue.Location.Length, Is.EqualTo(3));
            Assert.That(issue.Message, Does.Contain("hidden bytes"));
        });
    }

    [Test]
    public void FatalEncodedTableDefectsUseTableRulesInsteadOfGenericDiagnostics()
    {
        byte[] badCount = CreateEmptyProfileBytes(132);
        WriteUInt32(badCount, 128, 1);
        byte[] badBounds = CreateEmptyProfileBytes(148);
        WriteUInt32(badBounds, 0, 148);
        WriteUInt32(badBounds, 128, 1);
        WriteUInt32(badBounds, 132, IccTagSignatures.Copyright);
        WriteUInt32(badBounds, 136, 144);
        WriteUInt32(badBounds, 140, 8);

        IccValidationReport countReport = new IccProfileValidator().ValidateProfileBytesReport(
            badCount, new IccReadOptions());
        IccValidationReport boundsReport = new IccProfileValidator().ValidateProfileBytesReport(
            badBounds, new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(countReport.Issues, Has.Length.EqualTo(1));
            Assert.That(countReport.Issues[0].RuleId, Is.EqualTo(IccValidationRuleIds.TagTableBounds));
            Assert.That(countReport.Issues[0].Path, Is.EqualTo("TagTableEntries"));
            Assert.That(boundsReport.Issues, Has.Length.EqualTo(1));
            Assert.That(boundsReport.Issues[0].RuleId, Is.EqualTo(IccValidationRuleIds.TagTableBounds));
            Assert.That(boundsReport.Issues[0].Location.Offset, Is.EqualTo(136));
        });
    }

    [Test]
    public void RecoveryReportsFinalUnreferencedProfileBytesAsExactEndFailure()
    {
        byte[] bytes = CreateEmptyProfileBytes(156);
        WriteUInt32(bytes, 0, 156);
        WriteUInt32(bytes, 128, 1);
        WriteUInt32(bytes, 132, IccTagSignatures.Copyright);
        WriteUInt32(bytes, 136, 144);
        WriteUInt32(bytes, 140, 8);
        WriteUInt32(bytes, 144, IccTypeSignatures.Text);
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Recovery;

        IccValidationReport report = new IccProfileValidator().ValidateProfileBytesReport(bytes, options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Issues.Count(issue =>
                issue.RuleId == IccValidationRuleIds.ProfileExactEnd), Is.EqualTo(1));
            IccValidationIssue issue = FindRule(report, IccValidationRuleIds.ProfileExactEnd);
            Assert.That(issue.Path, Is.EqualTo("Tags"));
            Assert.That(issue.Location.Offset, Is.EqualTo(152));
            Assert.That(issue.Location.Length, Is.EqualTo(4));
        });
    }

    private static IccValidationReport Validate(IccProfile profile)
    {
        IccValidationPolicy policy = new IccValidationPolicy();
        policy.SuppressedRuleIds = new string[] { IccValidationRuleIds.RequiredTag };
        return new IccProfileValidator().ValidateProfileDataReport(profile,
            policy);
    }

    private static IccProfile CreateProfile(int declaredSize, int entryCount)
    {
        IccProfile profile = new IccProfile();
        profile.Location.Offset = 0;
        profile.Location.Length = declaredSize;
        profile.Header.DeclaredSize = declaredSize;
        profile.Header.Version = HeaderCodec.DecodeVersion(0x04400000);
        profile.Header.ProfileClassSignature = IccProfileClassSignatures.Display;
        profile.Header.ProfileClass = IccProfileClass.Display;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        profile.Header.DataColorSpace = IccColorSpaceKind.Rgb;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Header.CreationDateTime.Year = 2024;
        profile.Header.CreationDateTime.Month = 1;
        profile.Header.CreationDateTime.Day = 1;
        profile.Header.ProfileFileSignature = IccConstants.ProfileFileSignature;
        profile.Header.PcsIlluminantX = IccStandardColorimetry.D50EncodedX;
        profile.Header.PcsIlluminantY = IccStandardColorimetry.D50EncodedY;
        profile.Header.PcsIlluminantZ = IccStandardColorimetry.D50EncodedZ;
        profile.TagTableEntries = new IccTagTableEntry[entryCount];
        profile.Tags = new IccTag[entryCount];
        return profile;
    }

    private static void SetEntry(IccProfile profile, int index, uint signature, long offset,
        int size, int sharedDataId, byte[] padding)
    {
        IccTagTableEntry entry = new IccTagTableEntry();
        entry.Signature = signature;
        entry.Offset = offset;
        entry.Size = size;
        entry.OriginalIndex = index;
        entry.SharedDataId = sharedDataId;
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry = entry;
        tag.PaddingBytes = padding;
        profile.TagTableEntries[index] = entry;
        profile.Tags[index] = tag;
    }

    private static byte[] CreateEmptyProfileBytes(int length)
    {
        byte[] bytes = new byte[length];
        WriteUInt32(bytes, 0, 132);
        WriteUInt32(bytes, 8, 0x04400000);
        WriteUInt32(bytes, 12, IccProfileClassSignatures.Display);
        WriteUInt32(bytes, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(bytes, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt16(bytes, 24, 2024);
        WriteUInt16(bytes, 26, 1);
        WriteUInt16(bytes, 28, 1);
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
