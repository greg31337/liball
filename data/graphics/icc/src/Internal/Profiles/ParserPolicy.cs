namespace icc;

internal class ReadDiagnosticCollector
{
    internal int MaximumCount;
    internal System.Collections.Generic.List<IccValidationIssue> Issues;

    internal ReadDiagnosticCollector(int maximumCount)
    {
        if (maximumCount < 0)
        {
            throw new IccProfileValidationException("The maximum retained diagnostic count cannot be negative.");
        }

        MaximumCount = maximumCount;
        Issues = new System.Collections.Generic.List<IccValidationIssue>();
    }

    internal bool AddIssue(IccValidationIssue issue)
    {
        if (Issues.Count >= MaximumCount)
        {
            return false;
        }

        Issues.Add(issue);
        return true;
    }

    internal IccValidationIssue[] GetIssues()
    {
        return Issues.ToArray();
    }
}

internal static class ParserPolicy
{
    internal static void ValidateReadMode(IccReadMode readMode)
    {
        switch (readMode)
        {
            case IccReadMode.Strict:
            case IccReadMode.Conforming:
            case IccReadMode.Recovery:
                return;
            default:
                throw new IccProfileValidationException($"The profile read mode value {(int)readMode} is not supported.");
        }
    }

    internal static bool ShouldRejectIssue(IccReadMode readMode, IccValidationSeverity severity)
    {
        if (severity == IccValidationSeverity.Fatal)
        {
            return true;
        }

        if (readMode == IccReadMode.Strict)
        {
            return severity == IccValidationSeverity.Warning || severity == IccValidationSeverity.Error;
        }

        if (readMode == IccReadMode.Conforming)
        {
            return severity == IccValidationSeverity.Error;
        }

        return false;
    }

    internal static void ReportIssue(IccReadMode readMode, ReadDiagnosticCollector diagnostics, IccValidationIssue issue)
    {
        if (ShouldRejectIssue(readMode, issue.Severity))
        {
            ThrowIssue(issue);
        }

        diagnostics.AddIssue(issue);
    }

    internal static void ApplyHeaderIssues(IccProfileHeader header, IccReadMode readMode, ReadDiagnosticCollector diagnostics, long absoluteProfileOffset)
    {
        System.Collections.Generic.List<IccValidationIssue> retainedHeaderIssues = new System.Collections.Generic.List<IccValidationIssue>();
        int index = 0;
        while (index < header.ValidationIssues.Length)
        {
            IccValidationIssue issue = header.ValidationIssues[index];
            if (ShouldRejectIssue(readMode, issue.Severity))
            {
                if (issue.RuleId == IccValidationRuleIds.HeaderVersion)
                {
                    long absoluteVersionOffset = CheckedSize.AddOffsets(absoluteProfileOffset, 8, long.MaxValue);
                    HeaderCodec.EnforceVersionPolicy(header.Version, IccReadMode.Strict, absoluteVersionOffset);
                }

                ThrowIssue(issue);
            }

            if (diagnostics.AddIssue(issue))
            {
                retainedHeaderIssues.Add(issue);
            }

            index = index + 1;
        }

        header.ValidationIssues = retainedHeaderIssues.ToArray();
    }

    internal static void ThrowIssue(IccValidationIssue issue)
    {
        throw new IccProfileFormatException($"{issue.Message} Path: {issue.Path}; byte offset {issue.Location.Offset}, length {issue.Location.Length}.");
    }
}
