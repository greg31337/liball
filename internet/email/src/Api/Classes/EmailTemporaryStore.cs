using System;
using System.IO;

namespace email;
/// <summary>
/// Represents a temporary store.
/// </summary>
public class EmailTemporaryStore : IDisposable, IEmailTemporaryStoreContract
{
    EmailStorageOptions options;
    MemoryStream? memory;
    FileStream? file;
    string? temporaryPath;
    long length;
    bool completed;
    bool closed;
    /// <summary>
    /// Creates a new temporary store.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailTemporaryStore(EmailStorageOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        this.options = options.Clone();
        memory = new MemoryStream();
        file = null;
        temporaryPath = null;
        length = 0;
        completed = false;
        closed = false;
    }

    /// <summary>
    /// Gets the length.
    /// </summary>
    /// <returns>The current length.</returns>
    public long GetLength()
    {
        return length;
    }

    /// <summary>
    /// Determines whether the current value or state is using temporary file.
    /// </summary>
    /// <returns><c>true</c> when the value or state is using temporary file; otherwise, <c>false</c>.</returns>
    public bool IsUsingTemporaryFile()
    {
        return temporaryPath != null;
    }

    /// <summary>
    /// Determines whether the temporary path is present or available.
    /// </summary>
    /// <returns><c>true</c> when the temporary path is available; otherwise, <c>false</c>.</returns>
    public bool HasTemporaryPath()
    {
        return temporaryPath != null;
    }

    /// <summary>
    /// Gets the temporary path.
    /// </summary>
    /// <returns>The current temporary path.</returns>
    public string? GetTemporaryPath()
    {
        return temporaryPath;
    }

    /// <summary>
    /// Determines whether the current value or state is completed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is completed; otherwise, <c>false</c>.</returns>
    public bool IsCompleted()
    {
        return completed;
    }

    /// <summary>
    /// Determines whether the current value or state is closed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is closed; otherwise, <c>false</c>.</returns>
    public bool IsClosed()
    {
        return closed;
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public void Write(ReadOnlySpan<byte> value)
    {
        EnsureWritable();
        long newLength = new EmailSafeArithmetic().AddNonNegative(length, value.Length);
        long memoryThreshold = options.GetLimits().GetMemoryThresholdBytes();
        bool mustSpill = file != null || newLength > memoryThreshold || newLength > int.MaxValue;
        if (mustSpill && newLength > options.GetLimits().GetMaxTemporaryBytes())
        {
            throw new EmailLimitException("MaxTemporaryBytes", options.GetLimits().GetMaxTemporaryBytes(), newLength);
        }

        if (mustSpill && file == null)
        {
            SpillToTemporaryFile();
        }

        if (file != null)
        {
            file.Write(value);
        }
        else
        {
            (memory ?? throw new InvalidOperationException()).Write(value);
        }

        length = newLength;
    }

    /// <summary>
    /// Writes the supplied byte to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public void WriteByte(byte value)
    {
        EnsureWritable();
        long newLength = new EmailSafeArithmetic().AddNonNegative(length, 1);
        long memoryThreshold = options.GetLimits().GetMemoryThresholdBytes();
        bool mustSpill = file != null || newLength > memoryThreshold || newLength > int.MaxValue;
        if (mustSpill && newLength > options.GetLimits().GetMaxTemporaryBytes())
        {
            throw new EmailLimitException("MaxTemporaryBytes", options.GetLimits().GetMaxTemporaryBytes(), newLength);
        }

        if (mustSpill && file == null)
        {
            SpillToTemporaryFile();
        }

        if (file != null)
        {
            file.WriteByte(value);
        }
        else
        {
            (memory ?? throw new InvalidOperationException()).WriteByte(value);
        }

        length = newLength;
    }

    /// <summary>
    /// Finishes incremental processing and returns the final output or status.
    /// </summary>
    /// <remarks>
    /// Calling this method finalizes buffered state; use the instance's status method before attempting further input.
    /// </remarks>
    /// <returns>The content produced by the operation.</returns>
    public EmailContent Complete()
    {
        EnsureWritable();
        if (file == null)
        {
            byte[] value = (memory ?? throw new InvalidOperationException()).ToArray();
            ClearAndCloseMemory();
            memory = null;
            EmailContent memoryContent = new EmailContentOperations().CreateMemory(value, EmailContentOwnership.Owned, options.GetClearsSensitiveBytes());
            completed = true;
            closed = true;
            return memoryContent;
        }

        file.Flush(true);
        file.Close();
        file = null;
        EmailContent fileContent = new EmailContentOperations().CreateTemporaryFile(temporaryPath);
        temporaryPath = null;
        completed = true;
        closed = true;
        return fileContent;
    }

    /// <summary>
    /// Closes the supplied value.
    /// </summary>
    public void Close()
    {
        if (closed)
        {
            return;
        }

        if (file != null)
        {
            file.Close();
            file = null;
        }

        if (temporaryPath != null)
        {
            File.Delete(temporaryPath);
            temporaryPath = null;
        }

        if (memory != null)
        {
            ClearAndCloseMemory();
            memory = null;
        }

        closed = true;
    }

    /// <summary>
    /// Releases streams, temporary storage, and other resources retained by this instance.
    /// </summary>
    public void Dispose()
    {
        Close();
    }

    void SpillToTemporaryFile()
    {
        string fileName = "email-" + Guid.NewGuid().ToString("N") + ".tmp";
        temporaryPath = Path.Combine(options.GetTemporaryDirectory(), fileName);
        file = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, options.GetCopyBufferBytes(), FileOptions.SequentialScan);
        (memory ?? throw new InvalidOperationException()).Position = 0;
        memory.CopyTo(file, options.GetCopyBufferBytes());
        ClearAndCloseMemory();
        memory = null;
    }

    void ClearAndCloseMemory()
    {
        if (options.GetClearsSensitiveBytes())
        {
            byte[] internalBuffer = (memory ?? throw new InvalidOperationException()).GetBuffer();
            Array.Clear(internalBuffer, 0, internalBuffer.Length);
        }

        (memory ?? throw new InvalidOperationException()).Close();
    }

    void EnsureWritable()
    {
        if (closed)
        {
            throw new ObjectDisposedException("EmailTemporaryStore");
        }
    }
}
