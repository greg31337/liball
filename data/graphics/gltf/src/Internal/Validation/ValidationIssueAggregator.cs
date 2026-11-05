using System;
using System.Collections.Generic;

namespace gltf;

internal class ValidationIssueAggregation
{
    internal GltfValidationOptions Options = new GltfValidationOptions();
    internal GltfValidationReport Report = new GltfValidationReport();
    internal bool Stopped;
}

internal class ValidationIssueAggregator
{
    internal GltfValidationReport AggregateReports(
        List<GltfValidationReport> reports,
        GltfValidationOptions options)
    {
        if (reports == null)
        {
            throw new ArgumentNullException("reports");
        }

        ValidationIssueAggregation aggregation = CreateAggregation(options);
        int reportIndex = 0;
        while (reportIndex < reports.Count && !aggregation.Stopped)
        {
            GltfValidationReport report = reports[reportIndex];
            if (report == null)
            {
                throw new ArgumentException(
                    $"Validation report {reportIndex} must not be null.",
                    "reports");
            }

            AppendReport(aggregation, report);
            reportIndex++;
        }

        return CompleteAggregation(aggregation);
    }

    internal ValidationIssueAggregation CreateAggregation(GltfValidationOptions options)
    {
        ValidateOptions(options);
        ValidationIssueAggregation result = new ValidationIssueAggregation();
        result.Options = options;
        return result;
    }

    internal void AppendReport(
        ValidationIssueAggregation aggregation,
        GltfValidationReport source)
    {
        if (aggregation == null)
        {
            throw new ArgumentNullException("aggregation");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (aggregation.Stopped)
        {
            return;
        }

        if (source.Issues == null)
        {
            throw new ArgumentException(
                "The source validation report issue collection must not be null.",
                "source");
        }

        int issueIndex = 0;
        while (issueIndex < source.Issues.Count && !aggregation.Stopped)
        {
            GltfValidationIssue issue = source.Issues[issueIndex];
            if (issue == null)
            {
                throw new ArgumentException(
                    $"Validation issue {issueIndex} must not be null.",
                    "source");
            }

            AppendIssue(aggregation, issue);
            issueIndex++;
        }

        if (source.IssueLimitReached)
        {
            aggregation.Report.IssueLimitReached = true;
        }
    }

    internal void AppendIssue(
        ValidationIssueAggregation aggregation,
        GltfValidationIssue source)
    {
        if (aggregation == null)
        {
            throw new ArgumentNullException("aggregation");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (source.Code == null || source.Code.Length == 0)
        {
            throw new ArgumentException(
                "Validation issue codes must not be null or empty.",
                "source");
        }

        if (!IsKnownSeverity(source.Severity))
        {
            throw new ArgumentOutOfRangeException(
                "source",
                "The validation issue severity is not recognized.");
        }

        if (aggregation.Stopped || IsSuppressed(aggregation.Options, source.Code))
        {
            return;
        }

        GltfValidationSeverity severity = GetEffectiveSeverity(
            aggregation.Options,
            source.Code,
            source.Severity);
        if ((int)severity < (int)aggregation.Options.MinimumSeverity)
        {
            return;
        }

        if (aggregation.Report.Issues.Count >= aggregation.Options.MaximumIssueCount)
        {
            aggregation.Report.IssueLimitReached = true;
            aggregation.Stopped = true;
            return;
        }

        GltfValidationIssue issue = CopyIssue(source, severity);
        aggregation.Report.Issues.Add(issue);
        if (severity == GltfValidationSeverity.Error)
        {
            aggregation.Report.IsValid = false;
        }

        if (aggregation.Options.FailFast)
        {
            aggregation.Stopped = true;
        }
    }

    internal GltfValidationReport CompleteAggregation(
        ValidationIssueAggregation aggregation)
    {
        if (aggregation == null)
        {
            throw new ArgumentNullException("aggregation");
        }

        SortIssues(aggregation.Report.Issues);
        return aggregation.Report;
    }

    internal void ValidateOptions(GltfValidationOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (!IsKnownSeverity(options.MinimumSeverity))
        {
            throw new ArgumentOutOfRangeException(
                "options",
                "The minimum validation severity is not recognized.");
        }

        if (options.MaximumIssueCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                "options",
                "The maximum issue count must not be negative.");
        }

        if (options.SuppressedIssueCodes == null)
        {
            throw new ArgumentException(
                "The suppressed issue code collection must not be null.",
                "options");
        }

        int suppressionIndex = 0;
        while (suppressionIndex < options.SuppressedIssueCodes.Count)
        {
            string? code = options.SuppressedIssueCodes[suppressionIndex];
            if (code == null || code.Length == 0)
            {
                throw new ArgumentException(
                    $"Suppressed issue code {suppressionIndex} must not be null or empty.",
                    "options");
            }

            suppressionIndex++;
        }

        if (options.SeverityOverrides == null)
        {
            throw new ArgumentException(
                "The severity override collection must not be null.",
                "options");
        }

        foreach (KeyValuePair<string, GltfValidationSeverity> entry in options.SeverityOverrides)
        {
            if (entry.Key == null || entry.Key.Length == 0)
            {
                throw new ArgumentException(
                    "Severity override issue codes must not be null or empty.",
                    "options");
            }

            if (!IsKnownSeverity(entry.Value))
            {
                throw new ArgumentOutOfRangeException(
                    "options",
                    $"The severity override for issue code '{entry.Key}' is not recognized.");
            }
        }
    }

    internal bool IsKnownSeverity(GltfValidationSeverity severity)
    {
        return severity == GltfValidationSeverity.Information
            || severity == GltfValidationSeverity.Warning
            || severity == GltfValidationSeverity.Error;
    }

    internal bool IsSuppressed(GltfValidationOptions options, string code)
    {
        int index = 0;
        while (index < options.SuppressedIssueCodes.Count)
        {
            if (string.Equals(
                options.SuppressedIssueCodes[index],
                code,
                StringComparison.Ordinal))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal GltfValidationSeverity GetEffectiveSeverity(
        GltfValidationOptions options,
        string code,
        GltfValidationSeverity sourceSeverity)
    {
        if (options.SeverityOverrides.ContainsKey(code))
        {
            return options.SeverityOverrides[code];
        }

        return sourceSeverity;
    }

    internal GltfValidationIssue CopyIssue(
        GltfValidationIssue source,
        GltfValidationSeverity severity)
    {
        GltfValidationIssue result = new GltfValidationIssue();
        result.Code = source.Code;
        result.Severity = severity;
        result.Message = source.Message;
        result.JsonPointer = source.JsonPointer;
        result.HasByteOffset = source.HasByteOffset;
        result.ByteOffset = source.ByteOffset;
        result.ResourceUri = source.ResourceUri;
        if (source.RelatedJsonPointers != null)
        {
            int relatedIndex = 0;
            while (relatedIndex < source.RelatedJsonPointers.Count)
            {
                string? pointer = source.RelatedJsonPointers[relatedIndex];
                if (pointer != null && !ContainsOrdinal(result.RelatedJsonPointers, pointer))
                {
                    result.RelatedJsonPointers.Add(pointer);
                }

                relatedIndex++;
            }
        }

        SortStrings(result.RelatedJsonPointers);
        return result;
    }

    internal bool ContainsOrdinal(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (string.Equals(values[index], value, StringComparison.Ordinal))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void SortStrings(List<string> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            string value = values[index];
            int insertion = index;
            while (insertion > 0
                && string.CompareOrdinal(values[insertion - 1], value) > 0)
            {
                values[insertion] = values[insertion - 1];
                insertion--;
            }

            values[insertion] = value;
            index++;
        }
    }

    internal void SortIssues(List<GltfValidationIssue> issues)
    {
        int index = 1;
        while (index < issues.Count)
        {
            GltfValidationIssue issue = issues[index];
            int insertion = index;
            while (insertion > 0
                && CompareIssues(issues[insertion - 1], issue) > 0)
            {
                issues[insertion] = issues[insertion - 1];
                insertion--;
            }

            issues[insertion] = issue;
            index++;
        }
    }

    internal int CompareIssues(
        GltfValidationIssue left,
        GltfValidationIssue right)
    {
        int result = string.CompareOrdinal(left.JsonPointer, right.JsonPointer);
        if (result != 0)
        {
            return result;
        }

        result = string.CompareOrdinal(left.Code, right.Code);
        if (result != 0)
        {
            return result;
        }

        result = (int)right.Severity - (int)left.Severity;
        if (result != 0)
        {
            return result;
        }

        result = string.CompareOrdinal(left.Message, right.Message);
        if (result != 0)
        {
            return result;
        }

        result = CompareNullableStrings(left.ResourceUri, right.ResourceUri);
        if (result != 0)
        {
            return result;
        }

        if (left.HasByteOffset != right.HasByteOffset)
        {
            return left.HasByteOffset ? 1 : -1;
        }

        if (left.ByteOffset < right.ByteOffset)
        {
            return -1;
        }

        if (left.ByteOffset > right.ByteOffset)
        {
            return 1;
        }

        return CompareStringLists(
            left.RelatedJsonPointers,
            right.RelatedJsonPointers);
    }

    internal int CompareNullableStrings(string? left, string? right)
    {
        if (left == null)
        {
            return right == null ? 0 : -1;
        }

        if (right == null)
        {
            return 1;
        }

        return string.CompareOrdinal(left, right);
    }

    internal int CompareStringLists(List<string> left, List<string> right)
    {
        int count = left.Count < right.Count ? left.Count : right.Count;
        int index = 0;
        while (index < count)
        {
            int result = string.CompareOrdinal(left[index], right[index]);
            if (result != 0)
            {
                return result;
            }

            index++;
        }

        if (left.Count < right.Count)
        {
            return -1;
        }

        if (left.Count > right.Count)
        {
            return 1;
        }

        return 0;
    }
}
