namespace icc;

internal class ValidationReportCore
{
    internal ValidationReportCore()
    {
    }

    internal IccValidationReport Build(IccProfile profile, IccValidationPolicy policy)
    {
        ValidatePolicy(policy);
        System.Collections.Generic.List<IccValidationIssue> issues =
            new System.Collections.Generic.List<IccValidationIssue>();
        if (profile == null)
        {
            AddCandidate(issues, CreateStructuralIssue(IccValidationSeverity.Fatal,
                "A profile data model is required.", "Profile",
                "Provide a non-null decoded profile data model."), policy);
        }
        else
        {
            CollectProfileIssues(profile, issues, policy);
        }

        SortIssues(issues);
        IccValidationReport report = new IccValidationReport();
        report.Policy = ClonePolicy(policy);
        PopulateCounts(report, issues);
        report.Issues = CopyLimitedIssues(issues,
            policy.ResourceLimits.MaximumDiagnosticCount, policy);
        report.WasTruncated = report.Issues.Length < report.TotalIssueCount;
        FinalizeReport(report, issues);
        return report;
    }

    internal IccValidationReport BuildBytes(System.ReadOnlySpan<byte> profileBytes, IccReadOptions options)
    {
        ValidateReadOptions(options);
        if (profileBytes.Length > options.ResourceLimits.MaximumProfileBytes)
        {
            IccValidationPolicy policy = CreatePolicy(options);
            return CreateProbeReport(ProfileReaderCore.CreateResult(
                IccOperationStatus.ResourceLimitExceeded,
                $"The profile input contains {profileBytes.Length} bytes, exceeding the configured maximum of {options.ResourceLimits.MaximumProfileBytes} bytes.",
                0, profileBytes.Length), policy);
        }
        return BuildMemory(profileBytes.ToArray(), options);
    }

    internal IccValidationReport BuildMemory(System.ReadOnlyMemory<byte> profileMemory, IccReadOptions options)
    {
        ValidateReadOptions(options);
        IccValidationPolicy policy = CreatePolicy(options);
        if (profileMemory.Length > options.ResourceLimits.MaximumProfileBytes)
        {
            return CreateProbeReport(ProfileReaderCore.CreateResult(
                IccOperationStatus.ResourceLimitExceeded,
                $"The profile input contains {profileMemory.Length} bytes, exceeding the configured maximum of {options.ResourceLimits.MaximumProfileBytes} bytes.",
                0, profileMemory.Length), policy);
        }
        IccReadOptions recovery = CreateRecoveryOptions(options);
        ProfileReaderCore reader = new ProfileReaderCore();
        IccOperationResult probe = reader.ProbeMemory(profileMemory, recovery);
        if (probe.Status != IccOperationStatus.Success)
        {
            return CreateProbeReport(probe, policy);
        }
        IccProfile profile = reader.ReadMemory(profileMemory, recovery);
        return Build(profile, policy);
    }

    internal IccValidationReport BuildStream(System.IO.Stream profileStream, IccReadOptions options)
    {
        ValidateReadOptions(options);
        IccValidationPolicy policy = CreatePolicy(options);
        if (profileStream == null)
        {
            return CreateProbeReport(ProfileReaderCore.CreateResult(IccOperationStatus.Failed,
                "A profile stream cannot be null.", 0, 0), policy);
        }
        if (!profileStream.CanRead)
        {
            return CreateProbeReport(ProfileReaderCore.CreateResult(IccOperationStatus.Failed,
                "The profile stream is not readable.", 0, 0), policy);
        }

        int maximum = options.ResourceLimits.MaximumProfileBytes;
        using (System.IO.MemoryStream buffer = new System.IO.MemoryStream())
        {
            byte[] block = new byte[4096];
            int read = profileStream.Read(block, 0, block.Length);
            while (read != 0)
            {
                if (buffer.Length + read > maximum)
                {
                    return CreateProbeReport(ProfileReaderCore.CreateResult(
                        IccOperationStatus.ResourceLimitExceeded,
                        $"The profile stream exceeds the configured maximum of {maximum} bytes.",
                        0, buffer.Length + read), policy);
                }
                buffer.Write(block, 0, read);
                read = profileStream.Read(block, 0, block.Length);
            }
            return BuildMemory(buffer.ToArray(), options);
        }
    }

    internal void CollectProfileIssues(IccProfile profile,
        System.Collections.Generic.List<IccValidationIssue> issues, IccValidationPolicy policy)
    {
        SecurityStrictValidationResult security = SecurityStrictValidationCore.Validate(profile, policy);
        AddIssueArray(issues, security.Issues, "SecurityStrict", policy);
        if (profile.Header == null)
        {
            AddCandidate(issues, CreateStructuralIssue(IccValidationSeverity.Fatal,
                "The profile header is missing.", "Header", "Provide a complete profile header."), policy);
        }
        else
        {
            if (profile.Header.ValidationIssues == null
                || profile.Header.ValidationIssues.Length
                    <= policy.ResourceLimits.MaximumDiagnosticCount)
            {
                AddIssueArray(issues, profile.Header.ValidationIssues,
                    "Header.ValidationIssues", policy);
            }
            AddIssueArray(issues, HeaderValidationCore.Validate(profile), "Header", policy);
        }
        bool tagTableValidationIsBounded = profile.TagTableEntries == null
            || profile.TagTableEntries.Length <= policy.ResourceLimits.MaximumTagCount;
        if (tagTableValidationIsBounded)
        {
            AddIssueArray(issues, TagTableValidationCore.Validate(profile),
                "TagTableEntries", policy);
        }
        if (!security.MayContinue)
        {
            if (profile.ValidationIssues == null || profile.ValidationIssues.Length
                <= policy.ResourceLimits.MaximumDiagnosticCount)
            {
                AddIssueArray(issues, profile.ValidationIssues, "ValidationIssues", policy);
            }
            return;
        }
        AddIssueArray(issues, TagPayloadValidationCore.Validate(profile, policy), "Tags", policy);
        AddIssueArray(issues, Icc1TagValidationCore.Validate(profile, policy), "Tags", policy);
        AddIssueArray(issues, Icc1ConformanceValidationCore.Validate(profile), "Tags", policy);
        AddIssueArray(issues, Icc1SemanticValidationCore.Validate(profile), "Tags", policy);
        AddIssueArray(issues, Icc2ValidationCore.Validate(profile, policy), "Tags", policy);
        AddIssueArray(issues, Icc2PolicyValidationCore.Validate(profile, policy),
            "Tags", policy);
        AddIssueArray(issues, EmbeddedProfileValidationCore.Validate(profile, policy),
            "Tags", policy);
        AddIssueArray(issues, profile.ValidationIssues, "ValidationIssues", policy);
        if (profile.Tags == null)
        {
            AddCandidate(issues, CreateStructuralIssue(IccValidationSeverity.Fatal,
                "The profile tag collection is missing.", "Tags", "Provide a non-null tag collection."), policy);
            return;
        }

        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag == null)
            {
                AddCandidate(issues, CreateStructuralIssue(IccValidationSeverity.Fatal,
                    $"Profile tag {index} is null.", $"Tags[{index}]",
                    "Replace the null entry with a complete tag or remove it."), policy);
            }
            else if (tag.IsDecodeAttempted && tag.DecodeResult == null)
            {
                AddCandidate(issues, CreateStructuralIssue(IccValidationSeverity.Error,
                    $"Profile tag {index} has no decode outcome.", $"Tags[{index}].DecodeResult",
                    "Attach the decode operation result to the attempted tag."), policy);
            }
            else if (tag.IsDecodeAttempted && IsFailureStatus(tag.DecodeResult.Status))
            {
                IccValidationIssue decodeIssue = CreateDecodeIssue(tag, tag.DecodeResult,
                    index, policy);
                if (!ContainsTagRule(issues, decodeIssue.RuleId, index))
                {
                    AddCandidate(issues, decodeIssue, policy);
                }
            }
            else if (!tag.IsDecodeAttempted)
            {
                IccOperationResult? retainedResult = Icc1TagValidationCore.DecodeRetainedPayload(
                    profile, tag, policy);
                if (retainedResult != null && IsFailureStatus(retainedResult.Status))
                {
                    IccValidationIssue decodeIssue = CreateDecodeIssue(tag, retainedResult,
                        index, policy);
                    if (!ContainsTagRule(issues, decodeIssue.RuleId, index))
                    {
                        AddCandidate(issues, decodeIssue, policy);
                    }
                }
            }
            index = index + 1;
        }
    }

    internal void AddIssueArray(System.Collections.Generic.List<IccValidationIssue> issues,
        IccValidationIssue[]? source, string path, IccValidationPolicy policy)
    {
        if (source == null)
        {
            AddCandidate(issues, CreateStructuralIssue(IccValidationSeverity.Fatal,
                $"The {path} collection is null.", path,
                "Provide a non-null validation-issue collection."), policy);
            return;
        }
        int index = 0;
        while (index < source.Length)
        {
            IccValidationIssue issue = source[index];
            if (issue == null)
            {
                AddCandidate(issues, CreateStructuralIssue(IccValidationSeverity.Error,
                    $"Validation issue {index} in {path} is null.", $"{path}[{index}]",
                    "Remove the null diagnostic or replace it with a complete validation issue."), policy);
            }
            else
            {
                AddCandidate(issues, NormalizeIssue(issue), policy);
            }
            index = index + 1;
        }
    }

    internal void AddCandidate(System.Collections.Generic.List<IccValidationIssue> issues,
        IccValidationIssue issue, IccValidationPolicy policy)
    {
        if (IsSuppressed(issue.RuleId, policy.SuppressedRuleIds))
        {
            return;
        }
        int index = 0;
        while (index < issues.Count)
        {
            if (IsDuplicate(issues[index], issue))
            {
                return;
            }
            index = index + 1;
        }
        issues.Add(issue);
    }

    internal IccValidationIssue NormalizeIssue(IccValidationIssue source)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = source.RuleId == null || source.RuleId.Length == 0
            ? IccValidationRuleIds.ValidationDiagnostic : source.RuleId;
        issue.Severity = IsSeverityDefined(source.Severity) ? source.Severity : IccValidationSeverity.Error;
        issue.Message = source.Message == null || source.Message.Length == 0
            ? "The profile contains a validation issue." : source.Message;
        issue.Path = source.Path == null || source.Path.Length == 0 ? "Profile" : source.Path;
        issue.Remediation = source.Remediation == null || source.Remediation.Length == 0
            ? "Review and correct the reported profile data." : source.Remediation;
        if (source.Location != null && source.Location.Offset >= -1 && source.Location.Length >= 0)
        {
            issue.Location.Offset = source.Location.Offset;
            issue.Location.Length = source.Location.Length;
        }
        return issue;
    }

    internal IccValidationIssue CreateStructuralIssue(IccValidationSeverity severity,
        string message, string path, string remediation)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = IccValidationRuleIds.ValidationDiagnostic;
        issue.Severity = severity;
        issue.Message = message;
        issue.Path = path;
        issue.Remediation = remediation;
        return issue;
    }

    internal IccValidationIssue CreateDecodeIssue(IccTag tag, IccOperationResult decodeResult,
        int index, IccValidationPolicy policy)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = ResolveDecodeRule(decodeResult.Status, decodeResult.Message);
        bool permittedPrivate = decodeResult.Status == IccOperationStatus.UnsupportedFeature
            && IsPermittedPrivatePayload(tag, policy);
        issue.Severity = permittedPrivate ? IccValidationSeverity.Information
            : decodeResult.Status == IccOperationStatus.ResourceLimitExceeded
                ? IccValidationSeverity.Fatal : IccValidationSeverity.Error;
        issue.Message = decodeResult.Message == null || decodeResult.Message.Length == 0
            ? $"Profile tag {index} could not be decoded." : decodeResult.Message;
        issue.Path = $"Tags[{index}]";
        issue.Remediation = decodeResult.Status == IccOperationStatus.UnsupportedFeature
            ? "Use a supported tag type or preserve the complete opaque payload."
            : "Correct the tag payload and decode it again.";
        if (decodeResult.Location != null && decodeResult.Location.Offset >= 0)
        {
            issue.Location.Offset = decodeResult.Location.Offset;
            issue.Location.Length = decodeResult.Location.Length;
        }
        else if (tag.TableEntry != null)
        {
            issue.Location.Offset = tag.TableEntry.Offset;
            issue.Location.Length = tag.TableEntry.Size;
        }
        return issue;
    }

    internal string ResolveDecodeRule(IccOperationStatus status, string message)
    {
        if (status == IccOperationStatus.ResourceLimitExceeded)
        {
            if (ContainsAnyTextFour(message, "decompress", "compressed", "zlib", "inflate"))
            {
                return IccValidationRuleIds.CompressionLimit;
            }
            return IccValidationRuleIds.SecurityResourceLimit;
        }
        if (ContainsText(message, "embedded ICC.2")
            || ContainsText(message, "embeddedProfileType"))
        {
            return IccValidationRuleIds.EmbeddedProfile;
        }
        if (status == IccOperationStatus.UnsupportedFeature)
        {
            return IccValidationRuleIds.TagTypeCompatibility;
        }
        if (ContainsAnyTextFour(message, "reserved", "shall be zero", "must be zero", "requires zero"))
        {
            return IccValidationRuleIds.TagReserved;
        }
        if (ContainsAnyTextSix(message, "decompress", "compressed", "zlib", "inflate", "Adler", "checksum"))
        {
            return IccValidationRuleIds.CompressionLimit;
        }
        if (ContainsAnyTextSeven(message, "offset", "position", "aligned", "alignment", "overlap", "gap", "trailing"))
        {
            return IccValidationRuleIds.TagNestedOffset;
        }
        if (ContainsAnyTextFour(message, "channel count", "input channels", "output channels", "channel-count"))
        {
            return IccValidationRuleIds.ChannelCount;
        }
        if (ContainsAnyTextFour(message, "CLUT", "grid-point", "grid dimension", "LUT dimensions"))
        {
            return IccValidationRuleIds.ClutDimensions;
        }
        if (ContainsAnyTextFive(message, "curveType", "parametric curve", "curve function", "curve sample", "gamma"))
        {
            return IccValidationRuleIds.CurveValidity;
        }
        if (ContainsAnyTextSeven(message, "UTF-8", "UTF-16", "ASCII", "encoding", "textType", "NULL byte", "XML"))
        {
            return IccValidationRuleIds.TextEncoding;
        }
        if (ContainsAnyTextSeven(message, "permits", "tag type", "type signature", "not defined", "container identifier", "PCS encoding", "PCSXYZ"))
        {
            return IccValidationRuleIds.TagTypeCompatibility;
        }
        return IccValidationRuleIds.TagSize;
    }

    internal bool ContainsAnyTextFour(string message, string first, string second, string third,
        string fourth)
    {
        return ContainsText(message, first) || ContainsText(message, second)
            || ContainsText(message, third) || ContainsText(message, fourth);
    }

    internal bool ContainsAnyTextFive(string message, string first, string second, string third,
        string fourth, string fifth)
    {
        return ContainsText(message, first) || ContainsText(message, second)
            || ContainsText(message, third) || ContainsText(message, fourth)
            || ContainsText(message, fifth);
    }

    internal bool ContainsAnyTextSix(string message, string first, string second, string third,
        string fourth, string fifth, string sixth)
    {
        return ContainsText(message, first) || ContainsText(message, second)
            || ContainsText(message, third) || ContainsText(message, fourth)
            || ContainsText(message, fifth) || ContainsText(message, sixth);
    }

    internal bool ContainsAnyTextSeven(string message, string first, string second, string third,
        string fourth, string fifth, string sixth, string seventh)
    {
        return ContainsText(message, first) || ContainsText(message, second)
            || ContainsText(message, third) || ContainsText(message, fourth)
            || ContainsText(message, fifth) || ContainsText(message, sixth)
            || ContainsText(message, seventh);
    }

    internal bool ContainsText(string message, string value)
    {
        return message != null && message.Contains(value, System.StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsPermittedPrivatePayload(IccTag tag, IccValidationPolicy policy)
    {
        if (!policy.AllowPrivateSignatures || tag.Data == null || tag.Data.RawData == null
            || !tag.Data.RawData.HasTypeSignature)
        {
            return false;
        }
        uint typeSignature = tag.Data.RawData.TypeSignature;
        return !TagPayloadValidationCore.IsRegisteredTypeSignature(typeSignature)
            && HeaderValidationCore.IsPrintableSignature(typeSignature);
    }

    internal bool ContainsTagRule(System.Collections.Generic.List<IccValidationIssue> issues,
        string ruleId, int tagIndex)
    {
        string prefix = $"Tags[{tagIndex}]";
        int index = 0;
        while (index < issues.Count)
        {
            IccValidationIssue issue = issues[index];
            if (System.String.Equals(issue.RuleId, ruleId, System.StringComparison.Ordinal)
                && issue.Path.StartsWith(prefix, System.StringComparison.Ordinal))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal bool IsFailureStatus(IccOperationStatus status)
    {
        return status == IccOperationStatus.InvalidData
            || status == IccOperationStatus.ResourceLimitExceeded
            || status == IccOperationStatus.UnsupportedFeature
            || status == IccOperationStatus.Conflict || status == IccOperationStatus.Failed;
    }

    internal bool IsSeverityDefined(IccValidationSeverity severity)
    {
        return severity == IccValidationSeverity.Information || severity == IccValidationSeverity.Warning
            || severity == IccValidationSeverity.Error || severity == IccValidationSeverity.Fatal;
    }

    internal bool IsSuppressed(string ruleId, string[] suppressedRuleIds)
    {
        int index = 0;
        while (index < suppressedRuleIds.Length)
        {
            if (System.String.Equals(ruleId, suppressedRuleIds[index], System.StringComparison.Ordinal))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal bool IsDuplicate(IccValidationIssue first, IccValidationIssue second)
    {
        return first.Severity == second.Severity
            && first.Location.Offset == second.Location.Offset
            && first.Location.Length == second.Location.Length
            && System.String.Equals(first.RuleId, second.RuleId, System.StringComparison.Ordinal)
            && System.String.Equals(first.Path, second.Path, System.StringComparison.Ordinal);
    }

    internal void SortIssues(System.Collections.Generic.List<IccValidationIssue> issues)
    {
        int index = 1;
        while (index < issues.Count)
        {
            IccValidationIssue issue = issues[index];
            int target = index;
            while (target > 0 && CompareIssues(issue, issues[target - 1]) < 0)
            {
                issues[target] = issues[target - 1];
                target = target - 1;
            }
            issues[target] = issue;
            index = index + 1;
        }
    }

    internal int CompareIssues(IccValidationIssue first, IccValidationIssue second)
    {
        int severity = ((int)second.Severity).CompareTo((int)first.Severity);
        if (severity != 0)
        {
            return severity;
        }
        long firstOffset = first.Location.Offset < 0 ? long.MaxValue : first.Location.Offset;
        long secondOffset = second.Location.Offset < 0 ? long.MaxValue : second.Location.Offset;
        int offset = firstOffset.CompareTo(secondOffset);
        if (offset != 0)
        {
            return offset;
        }
        int rule = System.String.Compare(first.RuleId, second.RuleId, System.StringComparison.Ordinal);
        if (rule != 0)
        {
            return rule;
        }
        int path = System.String.Compare(first.Path, second.Path, System.StringComparison.Ordinal);
        if (path != 0)
        {
            return path;
        }
        return System.String.Compare(first.Message, second.Message, System.StringComparison.Ordinal);
    }

    internal IccValidationIssue[] CopyLimitedIssues(
        System.Collections.Generic.List<IccValidationIssue> issues, int maximum,
        IccValidationPolicy policy)
    {
        bool truncated = issues.Count > maximum;
        int count = System.Math.Min(issues.Count, maximum);
        IccValidationIssue[] result = new IccValidationIssue[count];
        ProfileCloneContext context = new ProfileCloneContext();
        bool retainLimitIssue = truncated && count > 0
            && !IsSuppressed(IccValidationRuleIds.ValidationLimit,
                policy.SuppressedRuleIds);
        int issueCount = retainLimitIssue ? count - 1 : count;
        int index = 0;
        while (index < issueCount)
        {
            result[index] = ProfileClone.CloneValidationIssue(issues[index], index, context);
            index = index + 1;
        }
        if (retainLimitIssue)
        {
            result[count - 1] = CreateLimitIssue(issues.Count - issueCount,
                issues[issueCount]);
        }
        return result;
    }

    internal IccValidationIssue CreateLimitIssue(int omittedCount,
        IccValidationIssue firstOmitted)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = IccValidationRuleIds.ValidationLimit;
        issue.Severity = IccValidationSeverity.Warning;
        issue.Message = $"Validation omitted {omittedCount} diagnostics due to the configured retention limit.";
        issue.Path = "Validation.Issues";
        issue.Remediation = "Raise the maximum diagnostic count to retain additional validation issues.";
        issue.Location.Offset = firstOmitted.Location.Offset;
        issue.Location.Length = firstOmitted.Location.Length;
        return issue;
    }

    internal void PopulateCounts(IccValidationReport report,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        report.TotalIssueCount = issues.Count;
        int index = 0;
        while (index < issues.Count)
        {
            switch (issues[index].Severity)
            {
                case IccValidationSeverity.Information:
                    report.InformationCount = report.InformationCount + 1;
                    break;
                case IccValidationSeverity.Warning:
                    report.WarningCount = report.WarningCount + 1;
                    break;
                case IccValidationSeverity.Error:
                    report.ErrorCount = report.ErrorCount + 1;
                    break;
                case IccValidationSeverity.Fatal:
                    report.FatalCount = report.FatalCount + 1;
                    break;
                default:
                    break;
            }
            index = index + 1;
        }
    }

    internal void FinalizeReport(IccValidationReport report,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        bool invalid = false;
        int index = 0;
        while (index < issues.Count)
        {
            if (Invalidates(issues[index].Severity, report.Policy.Mode))
            {
                invalid = true;
            }
            index = index + 1;
        }
        report.IsValid = !invalid;
        IccOperationStatus status = invalid ? IccOperationStatus.InvalidData
            : report.WasTruncated ? IccOperationStatus.ResourceLimitExceeded
            : IccOperationStatus.Success;
        string retained = report.WasTruncated
            ? $"; retained {report.Issues.Length} due to the configured maximum"
            : string.Empty;
        report.Result = CreateResult(status,
            $"Validation found {report.TotalIssueCount} issues ({report.FatalCount} fatal, {report.ErrorCount} errors, {report.WarningCount} warnings, {report.InformationCount} informational){retained}.");
        if (issues.Count > 0)
        {
            report.Result.Location.Offset = issues[0].Location.Offset;
            report.Result.Location.Length = issues[0].Location.Length;
        }
    }

    internal bool Invalidates(IccValidationSeverity severity, IccValidationMode mode)
    {
        if (mode == IccValidationMode.Tolerant)
        {
            return severity == IccValidationSeverity.Fatal;
        }
        if (mode == IccValidationMode.Conforming)
        {
            return severity == IccValidationSeverity.Error || severity == IccValidationSeverity.Fatal;
        }
        return severity == IccValidationSeverity.Warning || severity == IccValidationSeverity.Error
            || severity == IccValidationSeverity.Fatal;
    }

    internal IccValidationReport CreateProbeReport(IccOperationResult probe, IccValidationPolicy policy)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = ResolveProbeRule(probe);
        issue.Severity = probe.Status == IccOperationStatus.ResourceLimitExceeded
            ? IccValidationSeverity.Fatal : IccValidationSeverity.Error;
        issue.Message = probe.Message;
        issue.Path = ResolveProbePath(issue.RuleId);
        issue.Remediation = ResolveProbeRemediation(issue.RuleId, probe.Status);
        issue.Location.Offset = probe.Location.Offset;
        issue.Location.Length = probe.Location.Length;
        System.Collections.Generic.List<IccValidationIssue> issues =
            new System.Collections.Generic.List<IccValidationIssue>();
        AddCandidate(issues, issue, policy);
        IccValidationReport report = new IccValidationReport();
        report.Policy = ClonePolicy(policy);
        PopulateCounts(report, issues);
        report.Issues = CopyLimitedIssues(issues,
            policy.ResourceLimits.MaximumDiagnosticCount, policy);
        report.WasTruncated = report.Issues.Length < report.TotalIssueCount;
        FinalizeReport(report, issues);
        report.IsValid = false;
        report.Result.Status = probe.Status;
        return report;
    }

    internal string ResolveProbeRule(IccOperationResult probe)
    {
        if (probe.Status == IccOperationStatus.ResourceLimitExceeded)
        {
            return IccValidationRuleIds.SecurityResourceLimit;
        }
        if (probe.Message != null && probe.Message.Contains("tag count", System.StringComparison.OrdinalIgnoreCase))
        {
            return IccValidationRuleIds.TagTableCount;
        }
        if (probe.Message != null && (probe.Message.Contains("tag table", System.StringComparison.OrdinalIgnoreCase)
            || probe.Message.Contains("tag-table entry", System.StringComparison.OrdinalIgnoreCase)))
        {
            return IccValidationRuleIds.TagTableBounds;
        }
        if (probe.Message != null && (probe.Message.Contains("bounded input", System.StringComparison.OrdinalIgnoreCase)
            || probe.Message.Contains("trailing", System.StringComparison.OrdinalIgnoreCase)))
        {
            return IccValidationRuleIds.ProfileExactEnd;
        }
        return IccValidationRuleIds.ValidationDiagnostic;
    }

    internal string ResolveProbePath(string ruleId)
    {
        if (System.String.Equals(ruleId, IccValidationRuleIds.TagTableCount, System.StringComparison.Ordinal))
        {
            return "TagTableEntries";
        }
        if (System.String.Equals(ruleId, IccValidationRuleIds.TagTableBounds, System.StringComparison.Ordinal))
        {
            return "TagTableEntries";
        }
        if (System.String.Equals(ruleId, IccValidationRuleIds.ProfileExactEnd, System.StringComparison.Ordinal))
        {
            return "Profile.TrailingBytes";
        }
        return "Profile";
    }

    internal string ResolveProbeRemediation(string ruleId, IccOperationStatus status)
    {
        if (status == IccOperationStatus.ResourceLimitExceeded)
        {
            return "Reduce the input size or explicitly raise the corresponding trusted resource limit.";
        }
        if (System.String.Equals(ruleId, IccValidationRuleIds.TagTableCount, System.StringComparison.Ordinal))
        {
            return "Encode a bounded tag count whose complete table fits inside the profile.";
        }
        if (System.String.Equals(ruleId, IccValidationRuleIds.TagTableBounds, System.StringComparison.Ordinal))
        {
            return "Keep the complete tag table and every payload range within the declared profile.";
        }
        if (System.String.Equals(ruleId, IccValidationRuleIds.ProfileExactEnd, System.StringComparison.Ordinal))
        {
            return "Remove bytes outside the exact declared ICC profile.";
        }
        return "Correct the encoded profile structure before validation.";
    }

    internal IccOperationResult CreateResult(IccOperationStatus status, string message)
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = status;
        result.Message = message;
        return result;
    }

    internal void ValidatePolicy(IccValidationPolicy policy)
    {
        if (policy == null || policy.ResourceLimits == null || policy.RequiredRuleIds == null
            || policy.SuppressedRuleIds == null
            || policy.Icc2RequiredTagSignatures == null
            || policy.Icc2OptionalTagSignatures == null
            || policy.Icc2ForbiddenTagSignatures == null
            || policy.Icc2SupportedProcessingElementSignatures == null
            || policy.Icc2ChannelRules == null
            || policy.Icc2ProfileSubclassRules == null)
        {
            throw new IccProfileValidationException(
                "Validation policy, rule collections, and resource limits cannot be null.");
        }
        if (policy.Mode != IccValidationMode.Tolerant && policy.Mode != IccValidationMode.Conforming
            && policy.Mode != IccValidationMode.SecurityStrict)
        {
            throw new IccProfileValidationException("The validation policy mode is not defined.");
        }
        if (policy.ResourceLimits.MaximumDiagnosticCount < 0)
        {
            throw new IccProfileValidationException("The maximum validation issue count cannot be negative.");
        }
        ValidateRuleIds(policy.RequiredRuleIds, "required");
        ValidateRuleIds(policy.SuppressedRuleIds, "suppressed");
        ValidateIcc2Policy(policy);
    }

    internal void ValidateIcc2Policy(IccValidationPolicy policy)
    {
        ValidateSignatureList(policy.Icc2RequiredTagSignatures,
            "required ICC.2 tag");
        ValidateSignatureList(policy.Icc2OptionalTagSignatures,
            "optional ICC.2 tag");
        ValidateSignatureList(policy.Icc2ForbiddenTagSignatures,
            "forbidden ICC.2 tag");
        ValidateSignatureList(policy.Icc2SupportedProcessingElementSignatures,
            "supported ICC.2 processing element");
        int index = 0;
        while (index < policy.Icc2RequiredTagSignatures.Length)
        {
            uint signature = policy.Icc2RequiredTagSignatures[index];
            if (Icc2PolicyValidationCore.ContainsSignature(
                    policy.Icc2OptionalTagSignatures, signature)
                || Icc2PolicyValidationCore.ContainsSignature(
                    policy.Icc2ForbiddenTagSignatures, signature))
            {
                throw new IccProfileValidationException(
                    $"ICC.2 tag '{FourCcCodec.Format(signature)}' cannot appear in conflicting policy lists.");
            }
            index = index + 1;
        }
        index = 0;
        while (index < policy.Icc2OptionalTagSignatures.Length)
        {
            uint signature = policy.Icc2OptionalTagSignatures[index];
            if (Icc2PolicyValidationCore.ContainsSignature(
                policy.Icc2ForbiddenTagSignatures, signature))
            {
                throw new IccProfileValidationException(
                    $"ICC.2 tag '{FourCcCodec.Format(signature)}' cannot be both optional and forbidden.");
            }
            index = index + 1;
        }
        ValidateChannelPolicyRules(policy.Icc2ChannelRules);
        ValidateSubclassPolicyRules(policy.Icc2ProfileSubclassRules);
    }

    internal void ValidateSignatureList(uint[] signatures, string description)
    {
        int index = 0;
        while (index < signatures.Length)
        {
            if (signatures[index] == 0)
            {
                throw new IccProfileValidationException(
                    $"The {description} signature at index {index} cannot be zero.");
            }
            int prior = 0;
            while (prior < index)
            {
                if (signatures[prior] == signatures[index])
                {
                    throw new IccProfileValidationException(
                        $"The {description} signature '{FourCcCodec.Format(signatures[index])}' is duplicated.");
                }
                prior = prior + 1;
            }
            index = index + 1;
        }
    }

    internal void ValidateChannelPolicyRules(IccValidationChannelRule[] rules)
    {
        int index = 0;
        while (index < rules.Length)
        {
            IccValidationChannelRule rule = rules[index];
            if (rule == null || rule.InputChannels < 0
                || rule.InputChannels > ushort.MaxValue || rule.OutputChannels < 0
                || rule.OutputChannels > ushort.MaxValue
                || (rule.InputChannels == 0 && rule.OutputChannels == 0))
            {
                throw new IccProfileValidationException(
                    $"ICC.2 channel policy rule {index} is null or has invalid unconstrained or out-of-range channel counts.");
            }
            int prior = 0;
            while (prior < index)
            {
                IccValidationChannelRule previous = rules[prior];
                if (previous.TagSignature == rule.TagSignature
                    && previous.ProcessingElementSignature
                        == rule.ProcessingElementSignature)
                {
                    throw new IccProfileValidationException(
                        $"ICC.2 channel policy rule {index} duplicates an earlier tag and element target.");
                }
                prior = prior + 1;
            }
            index = index + 1;
        }
    }

    internal void ValidateSubclassPolicyRules(IccValidationSubclassRule[] rules)
    {
        int index = 0;
        while (index < rules.Length)
        {
            IccValidationSubclassRule rule = rules[index];
            if (rule == null)
            {
                throw new IccProfileValidationException(
                    $"ICC.2 subclass policy rule {index} cannot be null.");
            }
            bool invalid = rule.ProfileSubclassSignature == 0
                || rule.MinimumMajorVersion < 0
                || rule.MinimumMajorVersion > byte.MaxValue
                || rule.MinimumMinorVersion < 0
                || rule.MinimumMinorVersion > byte.MaxValue
                || rule.MaximumMajorVersion < 0
                || rule.MaximumMajorVersion > byte.MaxValue
                || rule.MaximumMinorVersion < 0
                || rule.MaximumMinorVersion > byte.MaxValue
                || rule.MinimumMajorVersion > rule.MaximumMajorVersion
                || (rule.MinimumMajorVersion == rule.MaximumMajorVersion
                    && rule.MinimumMinorVersion > rule.MaximumMinorVersion)
                || (rule.ProfileClass != IccProfileClass.Unknown
                    && Icc2TagRegistry.GetProfileClassMask(rule.ProfileClass) == 0);
            if (invalid)
            {
                throw new IccProfileValidationException(
                    $"ICC.2 subclass policy rule {index} is null or has an invalid signature, version interval, or profile class.");
            }
            index = index + 1;
        }
    }

    internal void ValidateRuleIds(string[] ruleIds, string description)
    {
        int index = 0;
        while (index < ruleIds.Length)
        {
            if (ruleIds[index] == null || ruleIds[index].Length == 0)
            {
                throw new IccProfileValidationException(
                    $"The {description} validation rule ID at index {index} cannot be null or empty.");
            }
            index = index + 1;
        }
    }

    internal void ValidateReadOptions(IccReadOptions options)
    {
        ProfileCodec.ValidateEnvelopeOptions(options);
        if (options.ValidationMode != IccValidationMode.Tolerant
            && options.ValidationMode != IccValidationMode.Conforming
            && options.ValidationMode != IccValidationMode.SecurityStrict)
        {
            throw new IccProfileValidationException("The read validation mode is not defined.");
        }
    }

    internal IccValidationPolicy CreatePolicy(IccReadOptions options)
    {
        IccValidationPolicy policy = new IccValidationPolicy();
        policy.Mode = options.ValidationMode;
        policy.ResourceLimits = CloneLimits(options.ResourceLimits);
        return policy;
    }

    internal IccReadOptions CreateRecoveryOptions(IccReadOptions options)
    {
        IccReadOptions recovery = new IccReadOptions();
        recovery.Mode = IccReadMode.Recovery;
        recovery.ValidationMode = options.ValidationMode;
        recovery.ResourceLimits = CloneLimits(options.ResourceLimits);
        if (recovery.ResourceLimits.MaximumDiagnosticCount < int.MaxValue)
        {
            recovery.ResourceLimits.MaximumDiagnosticCount =
                recovery.ResourceLimits.MaximumDiagnosticCount + 1;
        }
        recovery.CopyInputBytes = true;
        recovery.DecodeTagsLazily = false;
        recovery.RequireExactProfileSize = options.RequireExactProfileSize;
        recovery.PreserveOriginalBytes = options.PreserveOriginalBytes;
        return recovery;
    }

    internal IccValidationPolicy ClonePolicy(IccValidationPolicy source)
    {
        IccValidationPolicy policy = new IccValidationPolicy();
        policy.Mode = source.Mode;
        policy.RequiredRuleIds = CloneStrings(source.RequiredRuleIds);
        policy.SuppressedRuleIds = CloneStrings(source.SuppressedRuleIds);
        policy.AllowPrivateSignatures = source.AllowPrivateSignatures;
        policy.Icc2RequiredTagSignatures = CloneUnsignedIntegers(
            source.Icc2RequiredTagSignatures);
        policy.Icc2OptionalTagSignatures = CloneUnsignedIntegers(
            source.Icc2OptionalTagSignatures);
        policy.Icc2ForbiddenTagSignatures = CloneUnsignedIntegers(
            source.Icc2ForbiddenTagSignatures);
        policy.Icc2ForbidUnlistedTags = source.Icc2ForbidUnlistedTags;
        policy.Icc2SupportedProcessingElementSignatures = CloneUnsignedIntegers(
            source.Icc2SupportedProcessingElementSignatures);
        policy.Icc2ForbidUnlistedProcessingElements =
            source.Icc2ForbidUnlistedProcessingElements;
        policy.Icc2ChannelRules = CloneChannelRules(source.Icc2ChannelRules);
        policy.Icc2RequireProfileSubclass = source.Icc2RequireProfileSubclass;
        policy.Icc2ProfileSubclassRules = CloneSubclassRules(
            source.Icc2ProfileSubclassRules);
        policy.ResourceLimits = CloneLimits(source.ResourceLimits);
        return policy;
    }

    internal uint[] CloneUnsignedIntegers(uint[] source)
    {
        uint[] clone = new uint[source.Length];
        source.CopyTo(clone, 0);
        return clone;
    }

    internal IccValidationChannelRule[] CloneChannelRules(
        IccValidationChannelRule[] source)
    {
        IccValidationChannelRule[] clone =
            new IccValidationChannelRule[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            IccValidationChannelRule item = new IccValidationChannelRule();
            item.TagSignature = source[index].TagSignature;
            item.ProcessingElementSignature =
                source[index].ProcessingElementSignature;
            item.InputChannels = source[index].InputChannels;
            item.OutputChannels = source[index].OutputChannels;
            clone[index] = item;
            index = index + 1;
        }
        return clone;
    }

    internal IccValidationSubclassRule[] CloneSubclassRules(
        IccValidationSubclassRule[] source)
    {
        IccValidationSubclassRule[] clone =
            new IccValidationSubclassRule[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            IccValidationSubclassRule item = new IccValidationSubclassRule();
            item.ProfileSubclassSignature = source[index].ProfileSubclassSignature;
            item.MinimumMajorVersion = source[index].MinimumMajorVersion;
            item.MinimumMinorVersion = source[index].MinimumMinorVersion;
            item.MaximumMajorVersion = source[index].MaximumMajorVersion;
            item.MaximumMinorVersion = source[index].MaximumMinorVersion;
            item.ProfileClass = source[index].ProfileClass;
            item.DataColorSpaceSignature = source[index].DataColorSpaceSignature;
            item.ProfileConnectionSpaceSignature =
                source[index].ProfileConnectionSpaceSignature;
            item.SpectralPcsSignature = source[index].SpectralPcsSignature;
            item.MaterialConnectionSpaceSignature =
                source[index].MaterialConnectionSpaceSignature;
            clone[index] = item;
            index = index + 1;
        }
        return clone;
    }

    internal string[] CloneStrings(string[] source)
    {
        string[] clone = new string[source.Length];
        source.CopyTo(clone, 0);
        return clone;
    }

    internal IccResourceLimits CloneLimits(IccResourceLimits source)
    {
        IccResourceLimits limits = new IccResourceLimits();
        limits.MaximumProfileBytes = source.MaximumProfileBytes;
        limits.MaximumCarrierBytes = source.MaximumCarrierBytes;
        limits.MaximumTagCount = source.MaximumTagCount;
        limits.MaximumTagBytes = source.MaximumTagBytes;
        limits.MaximumTotalTagBytes = source.MaximumTotalTagBytes;
        limits.MaximumNestingDepth = source.MaximumNestingDepth;
        limits.MaximumDecompressedBytes = source.MaximumDecompressedBytes;
        limits.MaximumCalculatorInstructions = source.MaximumCalculatorInstructions;
        limits.MaximumCalculatorStackDepth = source.MaximumCalculatorStackDepth;
        limits.MaximumCalculatorStorageValues = source.MaximumCalculatorStorageValues;
        limits.MaximumDiagnosticCount = source.MaximumDiagnosticCount;
        return limits;
    }
}
