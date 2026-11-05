using System.Collections.Generic;

namespace sdp;

internal class SdpValidationIssueLimiter
{
    internal List<SdpValidationIssue> Issues;
    internal List<int> IssueRuleSets;
    internal int MaximumDiagnosticCount;
    internal bool DiagnosticLimitReached;
    internal bool FatalIssueAdded;
    internal int CurrentRuleSet;

    internal SdpValidationIssueLimiter(List<SdpValidationIssue> issues, int maximumDiagnosticCount)
    {
        Issues = issues;
        IssueRuleSets = new List<int>();
        MaximumDiagnosticCount = maximumDiagnosticCount > 0 ? maximumDiagnosticCount : 1;
        DiagnosticLimitReached = false;
        FatalIssueAdded = false;
        CurrentRuleSet = 0;
        int index = 0;
        while (index < issues.Count)
        {
            IssueRuleSets.Add(CurrentRuleSet);
            index = index + 1;
        }
    }

    internal void BeginRuleSet()
    {
        CurrentRuleSet = CurrentRuleSet + 1;
    }

    internal bool AddIssue(SdpValidationIssue issue)
    {
        if (issue.Severity == SdpValidationSeverity.Fatal)
        {
            FatalIssueAdded = true;
        }
        if (DiagnosticLimitReached)
        {
            return false;
        }
        int duplicateIndex = ReadDuplicateIssueIndex(issue);
        if (duplicateIndex >= 0)
        {
            if (IssueRuleSets[duplicateIndex] != CurrentRuleSet)
            {
                Issues[duplicateIndex] = issue;
                IssueRuleSets[duplicateIndex] = CurrentRuleSet;
            }
            return true;
        }
        if (Issues.Count < MaximumDiagnosticCount)
        {
            Issues.Add(issue);
            IssueRuleSets.Add(CurrentRuleSet);
            return true;
        }
        Issues[MaximumDiagnosticCount - 1] = SdpDiagnosticBuilder.CreateValidationLimitMarker(issue);
        IssueRuleSets[MaximumDiagnosticCount - 1] = CurrentRuleSet;
        DiagnosticLimitReached = true;
        return false;
    }

    internal int ReadDuplicateIssueIndex(SdpValidationIssue issue)
    {
        int index = 0;
        while (index < Issues.Count)
        {
            SdpValidationIssue existing = Issues[index];
            bool sameRoot = existing.Code == issue.Code && existing.Scope == issue.Scope && existing.FieldName == issue.FieldName && string.Equals(existing.AttributeName, issue.AttributeName, System.StringComparison.OrdinalIgnoreCase) && existing.MediaIndex == issue.MediaIndex && existing.Path.OccurrenceIndex == issue.Path.OccurrenceIndex && existing.Path.ValueIndex == issue.Path.ValueIndex;
            if (sameRoot && IssueRuleSets[index] != CurrentRuleSet)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }
}
