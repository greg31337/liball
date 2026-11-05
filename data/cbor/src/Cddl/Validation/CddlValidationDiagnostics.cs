using System.Collections.Generic;

namespace cbor;

internal class CddlValidationDiagnostics
{
    internal int MaximumIssues;
    internal int MaximumDepth;
    internal int StoredCount;
    internal bool HasIssues;
    internal bool HasErrors;
    internal List<CddlValidationIssue> Issues =
        new List<CddlValidationIssue>();

    internal CddlValidationDiagnostics(
        int maximumIssues,
        bool collectIssues,
        int maximumDepth)
    {
        MaximumIssues = maximumIssues;
        if (!collectIssues && MaximumIssues > 1)
        {
            MaximumIssues = 1;
        }
        MaximumDepth = maximumDepth;
    }

    internal void AddIssue(CddlValidationIssue issue)
    {
        HasIssues = true;
        if (ContainsError(
            issue,
            new List<CddlValidationIssue>(),
            0))
        {
            HasErrors = true;
        }
        if (MaximumIssues <= 0 || StoredCount >= MaximumIssues)
        {
            return;
        }
        CddlValidationIssue? clone = CloneBoundedIssue(
            issue,
            new List<CddlValidationIssue>(),
            0
        );
        if (clone != null)
        {
            Issues.Add(clone);
        }
    }

    internal CddlValidationIssue[] CreateIssueArray()
    {
        return Issues.ToArray();
    }

    internal CddlValidationIssue? CloneBoundedIssue(
        CddlValidationIssue source,
        List<CddlValidationIssue> visited,
        int depth)
    {
        if (StoredCount >= MaximumIssues || ContainsReference(visited, source))
        {
            return null;
        }
        CddlValidationIssue clone = CloneIssueFields(source);
        StoredCount = StoredCount + 1;
        if (MaximumDepth >= 0 && depth >= MaximumDepth)
        {
            return clone;
        }
        visited.Add(source);
        List<CddlValidationIssue> details =
            new List<CddlValidationIssue>();
        int index = 0;
        while (index < source.Details.Length
            && StoredCount < MaximumIssues)
        {
            CddlValidationIssue? detail = CloneBoundedIssue(
                source.Details[index],
                visited,
                depth + 1
            );
            if (detail != null)
            {
                details.Add(detail);
            }
            index = index + 1;
        }
        visited.RemoveAt(visited.Count - 1);
        clone.Details = details.ToArray();
        return clone;
    }

    internal bool ContainsError(
        CddlValidationIssue issue,
        List<CddlValidationIssue> visited,
        int depth)
    {
        if (issue.Severity == CddlDiagnosticSeverity.Error)
        {
            return true;
        }
        if ((MaximumDepth >= 0 && depth >= MaximumDepth)
            || ContainsReference(visited, issue))
        {
            return false;
        }
        visited.Add(issue);
        int index = 0;
        while (index < issue.Details.Length)
        {
            if (ContainsError(issue.Details[index], visited, depth + 1))
            {
                visited.RemoveAt(visited.Count - 1);
                return true;
            }
            index = index + 1;
        }
        visited.RemoveAt(visited.Count - 1);
        return false;
    }

    internal bool ContainsReference(
        List<CddlValidationIssue> issues,
        CddlValidationIssue issue)
    {
        int index = 0;
        while (index < issues.Count)
        {
            if (issues[index] == issue)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal CddlValidationIssue CloneIssueFields(CddlValidationIssue source)
    {
        CddlValidationIssue clone = new CddlValidationIssue();
        clone.Code = source.Code;
        clone.Severity = source.Severity;
        clone.Message = source.Message;
        clone.InstancePath = source.InstancePath;
        clone.ByteOffset = source.ByteOffset;
        clone.SchemaPath = source.SchemaPath;
        clone.Span = CloneSpan(source.Span);
        clone.RuleName = source.RuleName;
        clone.Expected = source.Expected;
        clone.Actual = source.Actual;
        return clone;
    }

    internal CddlSourceSpan CloneSpan(CddlSourceSpan source)
    {
        CddlSourceSpan span = new CddlSourceSpan();
        span.SourceName = source.SourceName;
        span.Start = source.Start;
        span.Length = source.Length;
        span.StartLine = source.StartLine;
        span.StartColumn = source.StartColumn;
        span.EndLine = source.EndLine;
        span.EndColumn = source.EndColumn;
        return span;
    }
}
