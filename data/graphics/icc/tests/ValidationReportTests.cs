using icc;

namespace icc.Tests;

public class ValidationReportTests
{
    [Test]
    public void EveryStableRuleIdRetainsExactDiagnosticMetadata()
    {
        string[] expectedRuleIds = new string[]
        {
            "ICC.VALIDATION.DIAGNOSTIC",
            "ICC.VALIDATION.LIMIT",
            "ICC.HDR.SIZE",
            "ICC.HDR.SIGNATURE",
            "ICC.HDR.VERSION",
            "ICC.HDR.RESERVED",
            "ICC.HDR.DATETIME",
            "ICC.HDR.CLASS",
            "ICC.HDR.SUBCLASS",
            "ICC.HDR.COLORSPACE",
            "ICC.HDR.PCS",
            "ICC.HDR.FLAGS",
            "ICC.HDR.PLATFORM",
            "ICC.HDR.REGISTERED_SIGNATURE",
            "ICC.HDR.ATTRIBUTES",
            "ICC.HDR.INTENT",
            "ICC.HDR.ILLUMINANT",
            "ICC.HDR.PROFILE_ID",
            "ICC.HDR.SPECTRAL_PCS",
            "ICC.HDR.MCS",
            "ICC.TABLE.COUNT",
            "ICC.TABLE.BOUNDS",
            "ICC.TABLE.ALIGNMENT",
            "ICC.TABLE.DUPLICATE_SIGNATURE",
            "ICC.TABLE.OVERLAP",
            "ICC.TABLE.GAP",
            "ICC.TABLE.PADDING",
            "ICC.PROFILE.EXACT_END",
            "ICC.TAG.TYPE_COMPATIBILITY",
            "ICC.TAG.RESERVED",
            "ICC.TAG.SIZE",
            "ICC.TAG.NESTED_OFFSET",
            "ICC.TAG.TEXT_ENCODING",
            "ICC.DATA.CHANNEL_COUNT",
            "ICC.DATA.CURVE",
            "ICC.DATA.CLUT_DIMENSIONS",
            "ICC.DATA.MATRIX_INVERTIBILITY",
            "ICC.MPE.CHANNEL_CONTINUITY",
            "ICC.MAX.CALCULATOR_SAFETY",
            "ICC.MAX.COMPRESSION_LIMIT",
            "ICC.SEMANTIC.REQUIRED_TAG",
            "ICC.SEMANTIC.FORBIDDEN_TAG",
            "ICC.SEMANTIC.PROFILE_CLASS",
            "ICC.MAX.HIERARCHY",
            "ICC.MAX.SPECTRAL",
            "ICC.MAX.PCC",
            "ICC.MAX.MCS",
            "ICC.SECURITY.RESOURCE_LIMIT",
            "ICC.QUALITY.ROUND_TRIP",
            "ICC.QUALITY.SMOOTHNESS",
            "ICC.EMBEDDED.PROFILE"
        };
        string[] declaredRuleIds = typeof(IccValidationRuleIds).GetFields(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();
        IccProfile profile = new IccProfile();
        MakeHeaderValid(profile);
        profile.ValidationIssues = expectedRuleIds.Select((ruleId, index) =>
            CreateIssue(ruleId, (IccValidationSeverity)(index % 4),
                $"message-{index}", $"Audit[{index}]", 1000 + index,
                index + 1, $"remediation-{index}")).ToArray();
        IccValidationPolicy policy = new IccValidationPolicy();

        IccValidationReport report = new IccProfileValidator()
            .ValidateProfileDataReport(profile, policy);
        IccValidationIssue[] retained = report.Issues
            .Where(issue => issue.Path.StartsWith("Audit[", StringComparison.Ordinal))
            .OrderBy(issue => issue.Location.Offset).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(declaredRuleIds, Is.Unique);
            Assert.That(declaredRuleIds.OrderBy(value => value),
                Is.EqualTo(expectedRuleIds.OrderBy(value => value)));
            Assert.That(retained, Has.Length.EqualTo(expectedRuleIds.Length));
            for (int index = 0; index < retained.Length; index++)
            {
                Assert.That(retained[index].RuleId, Is.EqualTo(expectedRuleIds[index]));
                Assert.That(retained[index].Severity,
                    Is.EqualTo((IccValidationSeverity)(index % 4)));
                Assert.That(retained[index].Message, Is.EqualTo($"message-{index}"));
                Assert.That(retained[index].Path, Is.EqualTo($"Audit[{index}]"));
                Assert.That(retained[index].Location.Offset, Is.EqualTo(1000 + index));
                Assert.That(retained[index].Location.Length, Is.EqualTo(index + 1));
                Assert.That(retained[index].Remediation,
                    Is.EqualTo($"remediation-{index}"));
            }
        });
    }

    [TestCase(IccValidationMode.Tolerant, IccValidationSeverity.Information, true)]
    [TestCase(IccValidationMode.Tolerant, IccValidationSeverity.Warning, true)]
    [TestCase(IccValidationMode.Tolerant, IccValidationSeverity.Error, true)]
    [TestCase(IccValidationMode.Tolerant, IccValidationSeverity.Fatal, false)]
    [TestCase(IccValidationMode.Conforming, IccValidationSeverity.Information, true)]
    [TestCase(IccValidationMode.Conforming, IccValidationSeverity.Warning, true)]
    [TestCase(IccValidationMode.Conforming, IccValidationSeverity.Error, false)]
    [TestCase(IccValidationMode.Conforming, IccValidationSeverity.Fatal, false)]
    [TestCase(IccValidationMode.SecurityStrict, IccValidationSeverity.Information, true)]
    [TestCase(IccValidationMode.SecurityStrict, IccValidationSeverity.Warning, false)]
    [TestCase(IccValidationMode.SecurityStrict, IccValidationSeverity.Error, false)]
    [TestCase(IccValidationMode.SecurityStrict, IccValidationSeverity.Fatal, false)]
    public void EveryModeAppliesItsSeverityThreshold(IccValidationMode mode,
        IccValidationSeverity severity, bool expectedValid)
    {
        IccProfile profile = new IccProfile();
        MakeHeaderValid(profile);
        profile.ValidationIssues = new IccValidationIssue[]
        {
            CreateIssue("TEST.MODE", severity, "mode issue", "Audit.Mode",
                500, 4, "mode remediation")
        };
        IccValidationPolicy policy = new IccValidationPolicy();
        policy.Mode = mode;
        policy.SuppressedRuleIds = new string[] { IccValidationRuleIds.RequiredTag };

        IccValidationReport report = new IccProfileValidator()
            .ValidateProfileDataReport(profile, policy);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.EqualTo(expectedValid));
            Assert.That(report.Result.Status, Is.EqualTo(expectedValid
                ? IccOperationStatus.Success : IccOperationStatus.InvalidData));
            Assert.That(report.Issues.Single(issue => issue.RuleId == "TEST.MODE").Severity,
                Is.EqualTo(severity));
        });
    }

    [Test]
    public void DiagnosticLimitRetainsStableLimitIssueAndFirstOmittedLocation()
    {
        IccProfile profile = new IccProfile();
        MakeHeaderValid(profile);
        profile.ValidationIssues = new IccValidationIssue[]
        {
            CreateIssue("TEST.ERROR.A", IccValidationSeverity.Error,
                "first", "Audit.First", 20, 2, "repair first"),
            CreateIssue("TEST.ERROR.B", IccValidationSeverity.Error,
                "second", "Audit.Second", 40, 4, "repair second"),
            CreateIssue("TEST.WARNING", IccValidationSeverity.Warning,
                "third", "Audit.Third", 60, 6, "repair third")
        };
        IccValidationPolicy policy = new IccValidationPolicy();
        policy.ResourceLimits.MaximumDiagnosticCount = 2;
        policy.SuppressedRuleIds = new string[] { IccValidationRuleIds.RequiredTag };

        IccValidationReport report = new IccProfileValidator()
            .ValidateProfileDataReport(profile, policy);
        policy.SuppressedRuleIds = new string[]
        {
            IccValidationRuleIds.RequiredTag,
            IccValidationRuleIds.ValidationLimit
        };
        IccValidationReport suppressed = new IccProfileValidator()
            .ValidateProfileDataReport(profile, policy);

        Assert.Multiple(() =>
        {
            Assert.That(report.TotalIssueCount, Is.EqualTo(3));
            Assert.That(report.Issues, Has.Length.EqualTo(2));
            Assert.That(report.Issues[0].RuleId, Is.EqualTo("TEST.ERROR.A"));
            Assert.That(report.Issues[1].RuleId,
                Is.EqualTo(IccValidationRuleIds.ValidationLimit));
            Assert.That(report.Issues[1].Severity, Is.EqualTo(IccValidationSeverity.Warning));
            Assert.That(report.Issues[1].Path, Is.EqualTo("Validation.Issues"));
            Assert.That(report.Issues[1].Location.Offset, Is.EqualTo(40));
            Assert.That(report.Issues[1].Location.Length, Is.EqualTo(4));
            Assert.That(report.Issues[1].Message, Does.Contain("2 diagnostics"));
            Assert.That(report.Issues[1].Remediation, Does.Contain("maximum diagnostic count"));
            Assert.That(report.WasTruncated, Is.True);
            Assert.That(report.IsValid, Is.False);
            Assert.That(suppressed.Issues.Select(issue => issue.RuleId),
                Is.EqualTo(new string[] { "TEST.ERROR.A", "TEST.ERROR.B" }));
            Assert.That(suppressed.WasTruncated, Is.True);
        });
    }

    [Test]
    public void EqualSeverityIssuesUseEveryDeterministicOrderingKey()
    {
        IccValidationIssue[] source = new IccValidationIssue[]
        {
            CreateIssue("RULE.B", IccValidationSeverity.Error,
                "message-b", "Path.B", -1, 1, "repair"),
            CreateIssue("RULE.B", IccValidationSeverity.Error,
                "message-b", "Path.B", 10, 1, "repair"),
            CreateIssue("RULE.A", IccValidationSeverity.Error,
                "message-b", "Path.B", 10, 1, "repair"),
            CreateIssue("RULE.A", IccValidationSeverity.Error,
                "message-b", "Path.A", 10, 1, "repair"),
            CreateIssue("RULE.A", IccValidationSeverity.Error,
                "message-a", "Path.A", 10, 2, "repair")
        };
        IccValidationIssue[] first = ValidateOrdered(source);
        IccValidationIssue[] second = ValidateOrdered(source.Reverse().ToArray());

        string[] expected = new string[]
        {
            "RULE.A|Path.A|message-a|10|2",
            "RULE.A|Path.A|message-b|10|1",
            "RULE.A|Path.B|message-b|10|1",
            "RULE.B|Path.B|message-b|10|1",
            "RULE.B|Path.B|message-b|-1|1"
        };
        Assert.Multiple(() =>
        {
            Assert.That(DescribeIssues(first), Is.EqualTo(expected));
            Assert.That(DescribeIssues(second), Is.EqualTo(expected));
        });
    }

    [Test]
    public void ReportAggregatesDeduplicatesNormalizesSortsAndLimitsIssues()
    {
        IIccProfileValidator validator = new IccProfileValidator();
        IccProfile profile = new IccProfile();
        MakeHeaderValid(profile);
        IccValidationIssue warning = CreateIssue(IccValidationRuleIds.HeaderReserved,
            IccValidationSeverity.Warning, "reserved", "Header.Reserved", 50, 4, "zero it");
        IccValidationIssue incomplete = new IccValidationIssue();
        incomplete.Severity = (IccValidationSeverity)99;
        incomplete.Location = null!;
        IccValidationIssue information = CreateIssue(IccValidationRuleIds.QualitySmoothness,
            IccValidationSeverity.Information, "quality", "Tags[2]", 10, 2, "inspect it");
        profile.Header.ValidationIssues = new IccValidationIssue[] { warning };
        profile.ValidationIssues = new IccValidationIssue[] { warning, incomplete, information };
        IccTag failedTag = new IccTag();
        failedTag.IsDecodeAttempted = true;
        failedTag.DecodeResult.Status = IccOperationStatus.ResourceLimitExceeded;
        failedTag.DecodeResult.Message = "expanded data exceeded its limit";
        failedTag.DecodeResult.Location.Offset = 30;
        failedTag.DecodeResult.Location.Length = 8;
        failedTag.Signature = IccTagSignatures.Copyright;
        failedTag.TableEntry.Signature = failedTag.Signature;
        failedTag.TableEntry.Offset = 144;
        failedTag.TableEntry.Size = 8;
        failedTag.TableEntry.OriginalIndex = 0;
        profile.Header.DeclaredSize = 152;
        profile.TagTableEntries = new IccTagTableEntry[] { failedTag.TableEntry };
        profile.Tags = new IccTag[] { failedTag };
        IccValidationPolicy policy = new IccValidationPolicy();
        policy.Mode = IccValidationMode.SecurityStrict;
        policy.ResourceLimits.MaximumDiagnosticCount = 3;
        policy.SuppressedRuleIds = new string[] { IccValidationRuleIds.RequiredTag };

        IccValidationReport report = validator.ValidateProfileDataReport(profile, policy);

        Assert.Multiple(() =>
        {
            Assert.That(report.TotalIssueCount, Is.EqualTo(4));
            Assert.That(report.FatalCount, Is.EqualTo(1));
            Assert.That(report.ErrorCount, Is.EqualTo(1));
            Assert.That(report.WarningCount, Is.EqualTo(1));
            Assert.That(report.InformationCount, Is.EqualTo(1));
            Assert.That(report.Issues, Has.Length.EqualTo(3));
            Assert.That(report.WasTruncated, Is.True);
            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(report.Issues.Select(issue => issue.Severity), Is.EqualTo(new[]
            {
                IccValidationSeverity.Fatal,
                IccValidationSeverity.Error,
                IccValidationSeverity.Warning
            }));
            Assert.That(report.Issues[0].RuleId, Is.EqualTo(IccValidationRuleIds.SecurityResourceLimit));
            Assert.That(report.Issues[0].Location.Offset, Is.EqualTo(30));
            Assert.That(report.Issues[1].RuleId, Is.EqualTo(IccValidationRuleIds.ValidationDiagnostic));
            Assert.That(report.Issues[1].Message, Is.Not.Empty);
            Assert.That(report.Issues[1].Path, Is.Not.Empty);
            Assert.That(report.Issues[1].Remediation, Is.Not.Empty);
            Assert.That(report.Policy, Is.Not.SameAs(policy));
            Assert.That(report.Policy.ResourceLimits, Is.Not.SameAs(policy.ResourceLimits));
        });
    }

    [Test]
    public void ValidationModesAndSuppressionControlValidityWithoutMutatingSourceIssues()
    {
        IIccProfileValidator validator = new IccProfileValidator();
        IccProfile profile = new IccProfile();
        MakeHeaderValid(profile);
        IccValidationIssue warning = CreateIssue(IccValidationRuleIds.HeaderReserved,
            IccValidationSeverity.Warning, "warning", "Header", 8, 4, "clear it");
        IccValidationIssue error = CreateIssue(IccValidationRuleIds.TagSize,
            IccValidationSeverity.Error, "error", "Tags[0]", 40, 8, "repair it");
        profile.ValidationIssues = new IccValidationIssue[] { error, warning };
        IccValidationPolicy tolerant = new IccValidationPolicy();
        tolerant.Mode = IccValidationMode.Tolerant;
        tolerant.SuppressedRuleIds = new string[] { IccValidationRuleIds.RequiredTag };
        IccValidationPolicy conforming = new IccValidationPolicy();
        conforming.Mode = IccValidationMode.Conforming;
        conforming.SuppressedRuleIds = new string[] { IccValidationRuleIds.RequiredTag };
        IccValidationPolicy strictSuppressed = new IccValidationPolicy();
        strictSuppressed.Mode = IccValidationMode.SecurityStrict;
        strictSuppressed.SuppressedRuleIds = new string[]
        {
            IccValidationRuleIds.TagSize,
            IccValidationRuleIds.HeaderReserved,
            IccValidationRuleIds.RequiredTag
        };

        IccValidationReport tolerantReport = validator.ValidateProfileDataReport(profile, tolerant);
        IccValidationReport conformingReport = validator.ValidateProfileDataReport(profile, conforming);
        IccValidationReport suppressedReport = validator.ValidateProfileDataReport(profile, strictSuppressed);

        Assert.Multiple(() =>
        {
            Assert.That(tolerantReport.IsValid, Is.True);
            Assert.That(tolerantReport.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(conformingReport.IsValid, Is.False);
            Assert.That(conformingReport.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(suppressedReport.IsValid, Is.True);
            Assert.That(suppressedReport.Issues, Is.Empty);
            Assert.That(profile.ValidationIssues, Is.EqualTo(new IccValidationIssue[] { error, warning }));
            Assert.That(tolerantReport.Issues[0], Is.Not.SameAs(error));
        });
    }

    [Test]
    public void EncodedInputsProduceDetailedStableIssuesAcrossInputForms()
    {
        IIccProfileValidator validator = new IccProfileValidator();
        byte[] bytes = CreateRecoverableProfile();
        IccReadOptions options = new IccReadOptions();
        options.ValidationMode = IccValidationMode.Conforming;

        IccValidationReport byteReport = validator.ValidateProfileBytesReport(bytes, options);
        IccValidationReport memoryReport = validator.ValidateProfileMemoryReport(bytes, options);
        using MemoryStream stream = new MemoryStream(bytes);
        IccValidationReport streamReport = validator.ValidateProfileStreamReport(stream, options);
        IccOperationResult memoryResult = validator.ValidateProfileMemory(
            bytes, options);
        using MemoryStream resultStream = new MemoryStream(bytes);
        IccOperationResult streamResult = validator.ValidateProfileStream(
            resultStream, options);

        Assert.Multiple(() =>
        {
            Assert.That(byteReport.IsValid, Is.False);
            Assert.That(byteReport.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(byteReport.TotalIssueCount, Is.EqualTo(7));
            Assert.That(byteReport.Issues.Select(issue => issue.RuleId),
                Does.Contain(IccValidationRuleIds.HeaderFileSignature));
            Assert.That(byteReport.Issues.Select(issue => issue.RuleId),
                Does.Contain(IccValidationRuleIds.HeaderReserved));
            Assert.That(byteReport.Issues.Count(issue => issue.RuleId
                == IccValidationRuleIds.RequiredTag), Is.EqualTo(5));
            Assert.That(byteReport.Issues[0].Path, Is.EqualTo("Header.ProfileFileSignature"));
            Assert.That(byteReport.Issues[0].Location.Offset, Is.EqualTo(36));
            Assert.That(byteReport.Issues[0].Location.Length, Is.EqualTo(4));
            Assert.That(byteReport.Issues[0].Remediation, Does.Contain("acsp"));
            Assert.That(memoryReport.Issues.Select(issue => issue.RuleId),
                Is.EqualTo(byteReport.Issues.Select(issue => issue.RuleId)));
            Assert.That(streamReport.Issues.Select(issue => issue.RuleId),
                Is.EqualTo(byteReport.Issues.Select(issue => issue.RuleId)));
            Assert.That(validator.ValidateProfileBytes(bytes, options).Status,
                Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(memoryResult.Status,
                Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(streamResult.Status,
                Is.EqualTo(IccOperationStatus.InvalidData));
        });
    }

    [Test]
    public void ZeroIssueLimitRetainsCountsValidityAndPrimaryLocation()
    {
        IIccProfileValidator validator = new IccProfileValidator();
        IccProfile profile = new IccProfile();
        MakeHeaderValid(profile);
        profile.ValidationIssues = new IccValidationIssue[]
        {
            CreateIssue(IccValidationRuleIds.TagSize, IccValidationSeverity.Error,
                "bad tag", "Tags[0]", 77, 9, "replace it")
        };
        IccValidationPolicy policy = new IccValidationPolicy();
        policy.ResourceLimits.MaximumDiagnosticCount = 0;
        policy.SuppressedRuleIds = new string[] { IccValidationRuleIds.RequiredTag };

        IccValidationReport report = validator.ValidateProfileDataReport(profile, policy);

        Assert.Multiple(() =>
        {
            Assert.That(report.Issues, Is.Empty);
            Assert.That(report.TotalIssueCount, Is.EqualTo(1));
            Assert.That(report.ErrorCount, Is.EqualTo(1));
            Assert.That(report.WasTruncated, Is.True);
            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Result.Location.Offset, Is.EqualTo(77));
            Assert.That(report.Result.Location.Length, Is.EqualTo(9));
        });
    }

    [Test]
    public void EncodedInputSizeLimitReturnsAFatalBoundedReport()
    {
        IIccProfileValidator validator = new IccProfileValidator();
        IccReadOptions options = new IccReadOptions();
        options.ResourceLimits.MaximumProfileBytes = IccConstants.MinimumProfileSize;
        byte[] oversized = new byte[IccConstants.MinimumProfileSize + 1];

        IccValidationReport report = validator.ValidateProfileBytesReport(oversized, options);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Result.Status, Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.That(report.FatalCount, Is.EqualTo(1));
            Assert.That(report.Issues, Has.Length.EqualTo(1));
            Assert.That(report.Issues[0].RuleId, Is.EqualTo(IccValidationRuleIds.SecurityResourceLimit));
            Assert.That(report.Issues[0].Path, Is.EqualTo("Profile"));
            Assert.That(report.Issues[0].Location.Length, Is.EqualTo(oversized.Length));
            Assert.That(report.Issues[0].Remediation, Is.Not.Empty);
        });
    }

    private static IccValidationIssue CreateIssue(string ruleId, IccValidationSeverity severity,
        string message, string path, long offset, long length, string remediation)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = ruleId;
        issue.Severity = severity;
        issue.Message = message;
        issue.Path = path;
        issue.Location.Offset = offset;
        issue.Location.Length = length;
        issue.Remediation = remediation;
        return issue;
    }

    private static IccValidationIssue[] ValidateOrdered(IccValidationIssue[] issues)
    {
        IccProfile profile = new IccProfile();
        MakeHeaderValid(profile);
        profile.ValidationIssues = issues;
        IccValidationPolicy policy = new IccValidationPolicy();
        policy.SuppressedRuleIds = new string[] { IccValidationRuleIds.RequiredTag };
        return new IccProfileValidator().ValidateProfileDataReport(profile, policy).Issues
            .Where(issue => issue.RuleId.StartsWith("RULE.", StringComparison.Ordinal))
            .ToArray();
    }

    private static string[] DescribeIssues(IccValidationIssue[] issues)
    {
        return issues.Select(issue => $"{issue.RuleId}|{issue.Path}|{issue.Message}|{issue.Location.Offset}|{issue.Location.Length}")
            .ToArray();
    }

    private static byte[] CreateRecoverableProfile()
    {
        byte[] bytes = new byte[IccConstants.MinimumProfileSize];
        WriteUInt32(bytes, 0, IccConstants.MinimumProfileSize);
        WriteUInt32(bytes, 8, 0x04400000);
        WriteUInt32(bytes, 12, IccProfileClassSignatures.Display);
        WriteUInt32(bytes, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(bytes, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt16(bytes, 24, 2026);
        WriteUInt16(bytes, 26, 1);
        WriteUInt16(bytes, 28, 1);
        WriteUInt32(bytes, 36, 0x42414421);
        WriteUInt32(bytes, 68, (uint)IccStandardColorimetry.D50EncodedX);
        WriteUInt32(bytes, 72, (uint)IccStandardColorimetry.D50EncodedY);
        WriteUInt32(bytes, 76, (uint)IccStandardColorimetry.D50EncodedZ);
        bytes[100] = 1;
        WriteUInt32(bytes, 128, 0);
        return bytes;
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }

    private static void WriteUInt16(byte[] destination, int offset, int value)
    {
        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)value;
    }

    private static void MakeHeaderValid(IccProfile profile)
    {
        profile.Header.DeclaredSize = IccConstants.MinimumProfileSize;
        profile.Header.Version = HeaderCodec.DecodeVersion(0x04400000);
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
    }
}
