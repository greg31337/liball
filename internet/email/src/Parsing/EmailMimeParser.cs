using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailMimeParseState
{
    EmailParseOptions options;
    int partCount;
    long temporaryByteCount;
    int temporaryFileCount;
    List<EmailContent> temporaryContents;
    bool complete;

    internal EmailMimeParseState(EmailParseOptions options)
    {
        this.options = options;
        partCount = 0;
        temporaryByteCount = 0;
        temporaryFileCount = 0;
        temporaryContents = new List<EmailContent>();
        complete = true;
    }

    internal bool EnsureDepth(int depth, EmailIssueList issues, long offset)
    {
        int maximum = options.GetLimits().GetParsingLimits().GetMaxNestingDepth();
        if (depth > maximum)
        {
            if (!options.GetCompatibilityOptions().GetReturnPartialResults())
            {
                throw new EmailLimitException("MaxNestingDepth", maximum, depth);
            }

            AddLimitIssue(issues, "MaxNestingDepth", maximum, depth, offset, "The entity was retained as opaque content without parsing deeper structure.");
            complete = false;
            return false;
        }

        return true;
    }

    internal bool AddPart(EmailIssueList issues, long offset)
    {
        int proposed = partCount + 1;
        int maximum = options.GetLimits().GetParsingLimits().GetMaxPartCount();
        if (proposed > maximum)
        {
            if (!options.GetCompatibilityOptions().GetReturnPartialResults())
            {
                throw new EmailLimitException("MaxPartCount", maximum, proposed);
            }

            AddLimitIssue(issues, "MaxPartCount", maximum, proposed, offset, "Every complete MIME part before the limit was retained and remaining parts were not parsed.");
            complete = false;
            return false;
        }

        partCount = proposed;
        return true;
    }

    internal void MarkIncomplete()
    {
        complete = false;
    }

    internal EmailContent RegisterStoredContent(EmailContent content)
    {
        if (content.GetKind() != EmailContentKind.TemporaryFile)
        {
            return content;
        }

        EmailTemporaryStorageLimits limits = options.GetStorageOptions().GetLimits();
        long proposedBytes = new EmailSafeArithmetic().AddNonNegative(temporaryByteCount, content.GetLength());
        int proposedFiles = temporaryFileCount + 1;
        if (proposedBytes > limits.GetMaxTemporaryBytes())
        {
            CloseTemporaryContents(content);
            throw new EmailLimitException("MaxTemporaryBytes", limits.GetMaxTemporaryBytes(), proposedBytes);
        }

        if (proposedFiles > limits.GetMaxTemporaryFileCount())
        {
            CloseTemporaryContents(content);
            throw new EmailLimitException("MaxTemporaryFileCount", limits.GetMaxTemporaryFileCount(), proposedFiles);
        }

        temporaryByteCount = proposedBytes;
        temporaryFileCount = proposedFiles;
        temporaryContents.Add(content);
        return content;
    }

    void CloseTemporaryContents(EmailContent current)
    {
        current.Close();
        int index = 0;
        while (index < temporaryContents.Count)
        {
            temporaryContents[index].Close();
            index++;
        }

        temporaryContents.Clear();
    }

    internal bool IsComplete()
    {
        return complete;
    }

    static void AddLimitIssue(EmailIssueList issues, string limitName, long limit, long observed, long offset, string recovery)
    {
        string description = "MIME parsing reached the recoverable " + limitName + " limit of " + limit + " at " + observed + ".";
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.LimitExceeded, description, new EmailSourcePosition(offset, 1, 1), null, null, recovery));
    }
}
internal class EmailMultipartBody
{
    List<MimeEntity> children;
    EmailContent preamble;
    EmailContent? epilogue;

    internal EmailMultipartBody(List<MimeEntity> children, EmailContent preamble, EmailContent? epilogue)
    {
        this.children = children;
        this.preamble = preamble;
        this.epilogue = epilogue;
    }

    internal List<MimeEntity> GetChildren()
    {
        return children;
    }

    internal EmailContent GetPreamble()
    {
        return preamble;
    }

    internal EmailContent? GetEpilogue()
    {
        return epilogue;
    }
}
