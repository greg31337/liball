using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailProgressSnapshot"/>.
/// </summary>
public interface IEmailProgressSnapshotContract
{
    /// <summary>
    /// Gets the source bytes read.
    /// </summary>
    /// <returns>The current source bytes read.</returns>
    long GetSourceBytesRead();
    /// <summary>
    /// Gets the output bytes written.
    /// </summary>
    /// <returns>The current output bytes written.</returns>
    long GetOutputBytesWritten();
    /// <summary>
    /// Gets the decoded bytes produced.
    /// </summary>
    /// <returns>The current decoded bytes produced.</returns>
    long GetDecodedBytesProduced();
    /// <summary>
    /// Gets the header count.
    /// </summary>
    /// <returns>The current header count.</returns>
    long GetHeaderCount();
    /// <summary>
    /// Gets the entity count.
    /// </summary>
    /// <returns>The current entity count.</returns>
    long GetEntityCount();
    /// <summary>
    /// Gets the operation count.
    /// </summary>
    /// <returns>The current operation count.</returns>
    long GetOperationCount();
}
