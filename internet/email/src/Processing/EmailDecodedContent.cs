using System;
using System.IO;

namespace email;
internal class EmailDecodedContentSink
{
    Stream? output;
    EmailHashingStream? hashing;
    long maximumBytes;
    bool stopAtMaximum;
    long byteCount;
    string limitName;
    long reportedLimit;
    long reportedObserved;

    internal EmailDecodedContentSink(Stream? output, EmailHashingStream? hashing, long maximumBytes, bool stopAtMaximum)
    {
        if (maximumBytes < 0) throw new ArgumentOutOfRangeException("maximumBytes");
        this.output = output;
        this.hashing = hashing;
        this.maximumBytes = maximumBytes;
        this.stopAtMaximum = stopAtMaximum;
        limitName = "MaxDecodedContentBytes";
        reportedLimit = maximumBytes;
        reportedObserved = 0;
        byteCount = 0;
    }

    internal static EmailDecodedContentSink CreateWithOutputAndHashingAndMaximumBytesAndStopAtMaximum(Stream? output, EmailHashingStream? hashing, long maximumBytes, bool stopAtMaximum)
    {
        return new EmailDecodedContentSink(output, hashing, maximumBytes, stopAtMaximum);
    }

    internal static EmailDecodedContentSink CreateWithOutputAndHashingAndMaximumBytesAndStopAtMaximumAndLimitNameAndReportedLimitAndReportedObserved(Stream? output, EmailHashingStream? hashing, long maximumBytes, bool stopAtMaximum, string limitName, long reportedLimit, long reportedObserved)
    {
        if (maximumBytes < 0)
        {
            throw new ArgumentOutOfRangeException("maximumBytes");
        }

        EmailDecodedContentSink value = new EmailDecodedContentSink(output, hashing, maximumBytes, stopAtMaximum);
        value.limitName = limitName;
        value.reportedLimit = reportedLimit;
        value.reportedObserved = reportedObserved;
        return value;
    }

    internal long GetMaximumBytes()
    {
        return maximumBytes;
    }

    internal bool StopsAtMaximum()
    {
        return stopAtMaximum;
    }

    internal bool WantsMore()
    {
        return !stopAtMaximum || byteCount < maximumBytes;
    }

    internal long GetByteCount()
    {
        return byteCount;
    }

    internal EmailLimitException CreateLimitException(long observed)
    {
        long selectedObserved = reportedObserved == 0 ? observed : reportedObserved;
        return new EmailLimitException(limitName, reportedLimit, selectedObserved);
    }

    internal void Write(byte[] value, int offset, int count)
    {
        if (count == 0)
        {
            return;
        }

        int selected = count;
        long remaining = maximumBytes - byteCount;
        if (selected > remaining)
        {
            if (!stopAtMaximum)
            {
                long observed = selected > long.MaxValue - byteCount ? long.MaxValue : byteCount + selected;
                throw CreateLimitException(observed);
            }

            selected = remaining <= 0 ? 0 : (int)remaining;
        }

        if (selected > 0 && output != null)
        {
            output.Write(value, offset, selected);
        }

        if (selected > 0 && hashing != null)
        {
            hashing.WriteSegment(value, offset, selected);
        }

        byteCount += selected;
    }
}
