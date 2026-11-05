using System;

namespace email;

internal class EmailSignatureParseState
{
    EmailIssueList issues;
    int consumedByteCount;
    int sourceByteCount;

    internal EmailSignatureParseState(EmailIssueList issues, int consumedByteCount, int sourceByteCount)
    {
        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        if (consumedByteCount < 0 || sourceByteCount < 0 || consumedByteCount > sourceByteCount)
        {
            throw new ArgumentOutOfRangeException("consumedByteCount");
        }

        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.consumedByteCount = consumedByteCount;
        this.sourceByteCount = sourceByteCount;
    }

    internal EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    internal int GetConsumedByteCount()
    {
        return consumedByteCount;
    }

    internal int GetSourceByteCount()
    {
        return sourceByteCount;
    }

    internal bool IsComplete()
    {
        return consumedByteCount == sourceByteCount;
    }

    internal bool IsSuccess()
    {
        return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    internal bool HasFatalError()
    {
        return issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal);
    }
}
