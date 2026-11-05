using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailThreadExtractionBudget
{
    EmailThreadOptions options;
    int headerCount;
    int identifierCount;
    int identifierBytes;
    int subjectBytes;

    internal EmailThreadExtractionBudget(EmailThreadOptions options)
    {
        this.options = options;
        headerCount = 0;
        identifierCount = 0;
        identifierBytes = 0;
        subjectBytes = 0;
    }

    internal void AddHeader()
    {
        headerCount++;
        if (headerCount > options.GetMaximumHeaderCount())
        {
            throw new EmailLimitException("MaxThreadHeaders", options.GetMaximumHeaderCount(), headerCount);
        }
    }

    internal void AddIdentifier()
    {
        identifierCount++;
        if (identifierCount > options.GetMaximumIdentifierCount())
        {
            throw new EmailLimitException("MaxThreadIdentifiers", options.GetMaximumIdentifierCount(), identifierCount);
        }
    }

    internal void AddIdentifierBytes(int value)
    {
        identifierBytes = Add("MaxThreadIdentifierBytes", identifierBytes, value, options.GetMaximumIdentifierBytes());
    }

    internal void AddSubjectBytes(int value)
    {
        subjectBytes = Add("MaxThreadSubjectBytes", subjectBytes, value, options.GetMaximumSubjectBytes());
    }

    static int Add(string name, int current, int added, int maximum)
    {
        long total = (long)current + added;
        if (total > maximum)
        {
            throw new EmailLimitException(name, maximum, total);
        }

        return (int)total;
    }
}
