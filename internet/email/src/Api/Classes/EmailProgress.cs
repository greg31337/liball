using System;

namespace email;
/// <summary>
/// Represents a progress.
/// </summary>
public class EmailProgress : IEmailProgress, IEmailProgressContract
{
    object synchronization;
    long sourceBytesRead;
    long outputBytesWritten;
    long decodedBytesProduced;
    long headerCount;
    long entityCount;
    long operationCount;
    /// <summary>
    /// Creates progress initialized from a snapshot.
    /// </summary>
    public EmailProgress()
    {
        synchronization = new object ();
        sourceBytesRead = 0;
        outputBytesWritten = 0;
        decodedBytesProduced = 0;
        headerCount = 0;
        entityCount = 0;
        operationCount = 0;
    }

    internal static EmailProgress CreateImplementationWith()
    {
        return new EmailProgress();
    }

    internal static EmailProgress CreateImplementationWithInitialValues(EmailProgressSnapshot initialValues)
    {
        if (initialValues == null)
        {
            throw new ArgumentNullException("initialValues");
        }

        EmailProgress value = new EmailProgress();
        value.sourceBytesRead = initialValues.GetSourceBytesRead();
        value.outputBytesWritten = initialValues.GetOutputBytesWritten();
        value.decodedBytesProduced = initialValues.GetDecodedBytesProduced();
        value.headerCount = initialValues.GetHeaderCount();
        value.entityCount = initialValues.GetEntityCount();
        value.operationCount = initialValues.GetOperationCount();
        return value;
    }

    /// <summary>
    /// Gets the source bytes read.
    /// </summary>
    /// <returns>The current source bytes read.</returns>
    public long GetSourceBytesRead()
    {
        lock (synchronization)
        {
            return sourceBytesRead;
        }
    }

    /// <summary>
    /// Adds a source bytes read to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddSourceBytesRead(long value)
    {
        lock (synchronization)
        {
            sourceBytesRead = AddChecked(sourceBytesRead, value, "value");
        }
    }

    /// <summary>
    /// Gets the output bytes written.
    /// </summary>
    /// <returns>The current output bytes written.</returns>
    public long GetOutputBytesWritten()
    {
        lock (synchronization)
        {
            return outputBytesWritten;
        }
    }

    /// <summary>
    /// Adds an output bytes written to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddOutputBytesWritten(long value)
    {
        lock (synchronization)
        {
            outputBytesWritten = AddChecked(outputBytesWritten, value, "value");
        }
    }

    /// <summary>
    /// Gets the decoded bytes produced.
    /// </summary>
    /// <returns>The current decoded bytes produced.</returns>
    public long GetDecodedBytesProduced()
    {
        lock (synchronization)
        {
            return decodedBytesProduced;
        }
    }

    /// <summary>
    /// Adds a decoded bytes produced to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddDecodedBytesProduced(long value)
    {
        lock (synchronization)
        {
            decodedBytesProduced = AddChecked(decodedBytesProduced, value, "value");
        }
    }

    /// <summary>
    /// Gets the header count.
    /// </summary>
    /// <returns>The current header count.</returns>
    public long GetHeaderCount()
    {
        lock (synchronization)
        {
            return headerCount;
        }
    }

    /// <summary>
    /// Adds a header count to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddHeaderCount(long value)
    {
        lock (synchronization)
        {
            headerCount = AddChecked(headerCount, value, "value");
        }
    }

    /// <summary>
    /// Gets the entity count.
    /// </summary>
    /// <returns>The current entity count.</returns>
    public long GetEntityCount()
    {
        lock (synchronization)
        {
            return entityCount;
        }
    }

    /// <summary>
    /// Adds an entity count to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddEntityCount(long value)
    {
        lock (synchronization)
        {
            entityCount = AddChecked(entityCount, value, "value");
        }
    }

    /// <summary>
    /// Gets the operation count.
    /// </summary>
    /// <returns>The current operation count.</returns>
    public long GetOperationCount()
    {
        lock (synchronization)
        {
            return operationCount;
        }
    }

    /// <summary>
    /// Adds an operation count to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddOperationCount(long value)
    {
        lock (synchronization)
        {
            operationCount = AddChecked(operationCount, value, "value");
        }
    }

    /// <summary>
    /// Gets the snapshot.
    /// </summary>
    /// <returns>The current snapshot.</returns>
    public EmailProgressSnapshot GetSnapshot()
    {
        lock (synchronization)
        {
            return new EmailProgressSnapshot(sourceBytesRead, outputBytesWritten, decodedBytesProduced, headerCount, entityCount, operationCount);
        }
    }

    /// <summary>
    /// Resets the supplied value.
    /// </summary>
    public void Reset()
    {
        lock (synchronization)
        {
            sourceBytesRead = 0;
            outputBytesWritten = 0;
            decodedBytesProduced = 0;
            headerCount = 0;
            entityCount = 0;
            operationCount = 0;
        }
    }

    static long AddChecked(long current, long additional, string parameterName)
    {
        if (additional < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        if (additional > long.MaxValue - current)
        {
            throw new OverflowException("The progress counter exceeds Int64.MaxValue.");
        }

        return current + additional;
    }
}
