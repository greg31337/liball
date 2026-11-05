using System;

namespace email;
/// <summary>
/// Describes the outcome of a SMTP data write operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class SmtpDataWriteResult : ISmtpDataWriteResultContract
{
    long sourceByteCount;
    long dataByteCount;
    long outputByteCount;
    long dotStuffedLineCount;
    long normalizedLineEndingCount;
    /// <summary>
    /// Creates a new SMTP data write result.
    /// </summary>
    /// <param name = "sourceByteCount">The number of items or bytes to process.</param>
    /// <param name = "dataByteCount">The number of items or bytes to process.</param>
    /// <param name = "outputByteCount">The number of items or bytes to process.</param>
    /// <param name = "dotStuffedLineCount">The number of items or bytes to process.</param>
    /// <param name = "normalizedLineEndingCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public SmtpDataWriteResult(long sourceByteCount, long dataByteCount, long outputByteCount, long dotStuffedLineCount, long normalizedLineEndingCount)
    {
        if (sourceByteCount < 0 || dataByteCount < 0 || outputByteCount < dataByteCount || dotStuffedLineCount < 0 || normalizedLineEndingCount < 0)
        {
            throw new ArgumentOutOfRangeException("sourceByteCount");
        }

        this.sourceByteCount = sourceByteCount;
        this.dataByteCount = dataByteCount;
        this.outputByteCount = outputByteCount;
        this.dotStuffedLineCount = dotStuffedLineCount;
        this.normalizedLineEndingCount = normalizedLineEndingCount;
    }

    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public long GetSourceByteCount()
    {
        return sourceByteCount;
    }

    /// <summary>
    /// Gets the data byte count.
    /// </summary>
    /// <returns>The current data byte count.</returns>
    public long GetDataByteCount()
    {
        return dataByteCount;
    }

    /// <summary>
    /// Gets the output byte count.
    /// </summary>
    /// <returns>The current output byte count.</returns>
    public long GetOutputByteCount()
    {
        return outputByteCount;
    }

    /// <summary>
    /// Gets the dot stuffed line count.
    /// </summary>
    /// <returns>The current dot stuffed line count.</returns>
    public long GetDotStuffedLineCount()
    {
        return dotStuffedLineCount;
    }

    /// <summary>
    /// Gets the normalized line ending count.
    /// </summary>
    /// <returns>The current normalized line ending count.</returns>
    public long GetNormalizedLineEndingCount()
    {
        return normalizedLineEndingCount;
    }
}
