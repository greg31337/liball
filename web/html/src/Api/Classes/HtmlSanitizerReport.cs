using System;
using System.Threading;
using System.Globalization;

namespace html;
/// <summary>Summarizes bounded sanitizer changes, aggregate counts, cancellation, and report truncation.</summary>
public class HtmlSanitizerReport : IHtmlSanitizerReport
{
    internal HtmlSanitizerChange[] changes;
    internal long[] reasonCounts;
    internal long totalCount;
    internal long droppedCount;
    internal bool truncated;
    internal bool limitExceeded;
    internal bool cancelled;
    internal HtmlSanitizerReport(HtmlSanitizerChange[] changesValue, long[] reasonCountsValue, long totalCountValue, long droppedCountValue, bool truncatedValue, bool limitExceededValue, bool cancelledValue)
    {
        if (changesValue == null)
        {
            throw new ArgumentNullException("changesValue");
        }

        if (reasonCountsValue == null)
        {
            throw new ArgumentNullException("reasonCountsValue");
        }

        if (totalCountValue < 0 || droppedCountValue < 0 || droppedCountValue > totalCountValue)
        {
            throw new ArgumentOutOfRangeException("totalCountValue");
        }

        this.changes = CopyChanges(changesValue);
        this.reasonCounts = CopyCounts(reasonCountsValue);
        this.totalCount = totalCountValue;
        this.droppedCount = droppedCountValue;
        this.truncated = truncatedValue;
        this.limitExceeded = limitExceededValue;
        this.cancelled = cancelledValue;
    }

    /// <summary>Gets the retained sanitizer changes in the order they were applied.</summary>
    /// <returns>A snapshot of the changes; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlSanitizerChange[] GetChanges()
    {
        return CopyChanges(this.changes);
    }

    /// <summary>Gets how many individual sanitizer changes remain available for inspection.</summary>
    /// <returns>The length of the array returned by <see cref = "GetChanges"/>.</returns>
    public int GetRetainedCount()
    {
        return this.changes.Length;
    }

    /// <summary>Gets how many sanitizer changes occurred before report retention limits were applied.</summary>
    /// <returns>The total of retained and dropped change records.</returns>
    public long GetTotalCount()
    {
        return this.totalCount;
    }

    /// <summary>Gets how many sanitizer change records could not be retained.</summary>
    /// <returns>The number omitted from <see cref = "GetChanges"/> because the report limit was exhausted.</returns>
    public long GetDroppedCount()
    {
        return this.droppedCount;
    }

    /// <summary>Gets how many sanitizer changes were attributed to one stable reason.</summary>
    /// <param name = "reason">The reason recorded for the sanitizer decision.</param>
    /// <returns>The total count for that reason, including retained and dropped change records; <c>0</c> for an unknown reason.</returns>
    public long GetReasonCount(HtmlSanitizerChangeReason reason)
    {
        int index = (int)reason;
        if (index <= 0 || index >= this.reasonCounts.Length)
        {
            return 0;
        }

        return this.reasonCounts[index];
    }

    /// <summary>Reports whether the result was truncated by a configured limit.</summary>
    /// <returns><c>true</c> when a configured limit truncated the result; otherwise, <c>false</c>.</returns>
    public bool WasTruncated()
    {
        return this.truncated;
    }

    /// <summary>Reports whether processing exceeded a configured resource limit.</summary>
    /// <returns><c>true</c> when a configured limit was exceeded; otherwise, <c>false</c>.</returns>
    public bool WasLimitExceeded()
    {
        return this.limitExceeded;
    }

    /// <summary>Reports whether cancellation stopped sanitization.</summary>
    /// <returns><c>true</c> when cancellation stopped the operation; otherwise, <c>false</c>.</returns>
    public bool WasCancelled()
    {
        return this.cancelled;
    }

    /// <summary>Reports whether report retention or sanitization output was partial.</summary>
    /// <returns><c>true</c> when the report was truncated, a limit was exceeded, or cancellation occurred; otherwise, <c>false</c>.</returns>
    public bool IsPartial()
    {
        return this.truncated || this.limitExceeded || this.cancelled;
    }

    /// <summary>Gets the stable code explaining why sanitization stopped.</summary>
    /// <returns>The cancellation or sanitizer-limit code, or <see cref = "HtmlDiagnosticCode.None"/> when sanitization was not stopped.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        if (this.cancelled)
        {
            return HtmlDiagnosticCode.OperationCancelled;
        }

        return this.limitExceeded
            ? HtmlDiagnosticCode.SanitizerLimitExceeded
            : HtmlDiagnosticCode.None;
    }

    internal static HtmlSanitizerChange[] CopyChanges(HtmlSanitizerChange[] values)
    {
        HtmlSanitizerChange[] result = new HtmlSanitizerChange[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("A sanitizer change collection cannot contain null.", "changesValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }

    internal static long[] CopyCounts(long[] values)
    {
        long[] result = new long[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] < 0)
            {
                throw new ArgumentException("A sanitizer reason count cannot be negative.", "reasonCountsValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}
