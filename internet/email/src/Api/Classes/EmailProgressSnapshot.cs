using System;

namespace email;
/// <summary>
/// Represents a progress snapshot.
/// </summary>
public class EmailProgressSnapshot : IEmailProgressSnapshotContract
{
    long sourceBytesRead;
    long outputBytesWritten;
    long decodedBytesProduced;
    long headerCount;
    long entityCount;
    long operationCount;
    /// <summary>
    /// Creates a new progress snapshot.
    /// </summary>
    /// <param name = "sourceBytesRead">The source bytes or data to process.</param>
    /// <param name = "outputBytesWritten">The source bytes or data to process.</param>
    /// <param name = "decodedBytesProduced">The source bytes or data to process.</param>
    /// <param name = "headerCount">The number of items or bytes to process.</param>
    /// <param name = "entityCount">The number of items or bytes to process.</param>
    /// <param name = "operationCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailProgressSnapshot(long sourceBytesRead, long outputBytesWritten, long decodedBytesProduced, long headerCount, long entityCount, long operationCount)
    {
        if (sourceBytesRead < 0)
        {
            throw new ArgumentOutOfRangeException("sourceBytesRead");
        }

        if (outputBytesWritten < 0)
        {
            throw new ArgumentOutOfRangeException("outputBytesWritten");
        }

        if (decodedBytesProduced < 0)
        {
            throw new ArgumentOutOfRangeException("decodedBytesProduced");
        }

        if (headerCount < 0)
        {
            throw new ArgumentOutOfRangeException("headerCount");
        }

        if (entityCount < 0)
        {
            throw new ArgumentOutOfRangeException("entityCount");
        }

        if (operationCount < 0)
        {
            throw new ArgumentOutOfRangeException("operationCount");
        }

        this.sourceBytesRead = sourceBytesRead;
        this.outputBytesWritten = outputBytesWritten;
        this.decodedBytesProduced = decodedBytesProduced;
        this.headerCount = headerCount;
        this.entityCount = entityCount;
        this.operationCount = operationCount;
    }

    /// <summary>
    /// Gets the source bytes read.
    /// </summary>
    /// <returns>The current source bytes read.</returns>
    public long GetSourceBytesRead()
    {
        return sourceBytesRead;
    }

    /// <summary>
    /// Gets the output bytes written.
    /// </summary>
    /// <returns>The current output bytes written.</returns>
    public long GetOutputBytesWritten()
    {
        return outputBytesWritten;
    }

    /// <summary>
    /// Gets the decoded bytes produced.
    /// </summary>
    /// <returns>The current decoded bytes produced.</returns>
    public long GetDecodedBytesProduced()
    {
        return decodedBytesProduced;
    }

    /// <summary>
    /// Gets the header count.
    /// </summary>
    /// <returns>The current header count.</returns>
    public long GetHeaderCount()
    {
        return headerCount;
    }

    /// <summary>
    /// Gets the entity count.
    /// </summary>
    /// <returns>The current entity count.</returns>
    public long GetEntityCount()
    {
        return entityCount;
    }

    /// <summary>
    /// Gets the operation count.
    /// </summary>
    /// <returns>The current operation count.</returns>
    public long GetOperationCount()
    {
        return operationCount;
    }
}
