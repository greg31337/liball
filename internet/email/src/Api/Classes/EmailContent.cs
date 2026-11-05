using System;
using System.IO;

namespace email;
/// <summary>
/// Provides bounded access to message content held in memory, a file, a stream, or a slice of another content source.
/// </summary>
/// <remarks>
/// Callers should observe the ownership and lifetime methods when the instance retains streams, temporary storage, or source buffers.
/// </remarks>
public class EmailContent : IDisposable, IEmailContentContract
{
    EmailContentKind kind;
    EmailContentOwnership ownership;
    byte[]? memory;
    string? filePath;
    Stream? stream;
    long streamStartPosition;
    long knownLength;
    bool repeatable;
    bool streamOpened;
    bool clearSensitiveBytes;
    bool closed;
    EmailContent? sliceSource;
    long sliceOffset;
    EmailContent()
    {
        kind = EmailContentKind.Memory;
        ownership = EmailContentOwnership.Borrowed;
        memory = null;
        filePath = null;
        stream = null;
        streamStartPosition = 0;
        knownLength = -1;
        repeatable = false;
        streamOpened = false;
        clearSensitiveBytes = false;
        closed = false;
        sliceSource = null;
        sliceOffset = 0;
    }

    internal static EmailContent CreateMemoryImplementation(byte[]? value, EmailContentOwnership ownership, bool clearSensitiveBytes)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        ValidateOwnership(ownership);
        if (ownership == EmailContentOwnership.Borrowed && clearSensitiveBytes)
        {
            throw new ArgumentException("Borrowed memory cannot be cleared by the content owner.", "clearSensitiveBytes");
        }

        EmailContent content = new EmailContent();
        content.kind = EmailContentKind.Memory;
        content.ownership = ownership;
        content.memory = value;
        content.knownLength = value.Length;
        content.repeatable = true;
        content.clearSensitiveBytes = clearSensitiveBytes;
        return content;
    }

    internal static EmailContent CreateMemoryCopyImplementation(byte[] value, bool clearSensitiveBytes)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return CreateMemoryImplementation(copy, EmailContentOwnership.Owned, clearSensitiveBytes);
    }

    internal static EmailContent CreateFileImplementation(string path, EmailContentOwnership ownership)
    {
        ValidateOwnership(ownership);
        return CreatePathContent(path, EmailContentKind.File, ownership);
    }

    internal static EmailContent CreateTemporaryFileImplementation(string? path)
    {
        return CreatePathContent(path, EmailContentKind.TemporaryFile, EmailContentOwnership.Owned);
    }

    internal static EmailContent CreateStreamImplementation(Stream value, EmailContentOwnership ownership)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        ValidateOwnership(ownership);
        if (!value.CanRead)
        {
            throw new ArgumentException("The stream must be readable.", "value");
        }

        EmailContent content = new EmailContent();
        content.kind = EmailContentKind.Stream;
        content.ownership = ownership;
        content.stream = value;
        content.repeatable = value.CanSeek;
        if (content.repeatable)
        {
            content.streamStartPosition = value.Position;
            long endPosition = value.Length;
            if (endPosition < content.streamStartPosition)
            {
                throw new ArgumentException("The stream position exceeds its length.", "value");
            }

            content.knownLength = endPosition - content.streamStartPosition;
        }

        return content;
    }

    internal static EmailContent CreateSliceImplementation(EmailContent? source, long offset, long length)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (source.IsClosed())
        {
            throw new ObjectDisposedException("source");
        }

        if (!source.IsRepeatable() || !source.HasKnownLength())
        {
            throw new ArgumentException("A content slice requires a repeatable source with a known length.", "source");
        }

        if (offset < 0 || length < 0 || offset > source.GetLength() || length > source.GetLength() - offset)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        EmailContent content = new EmailContent();
        content.kind = EmailContentKind.Slice;
        content.ownership = EmailContentOwnership.Borrowed;
        content.knownLength = length;
        content.repeatable = true;
        content.sliceSource = source;
        content.sliceOffset = offset;
        return content;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailContentKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the ownership.
    /// </summary>
    /// <returns>The current ownership.</returns>
    public EmailContentOwnership GetOwnership()
    {
        return ownership;
    }

    /// <summary>
    /// Determines whether the current value or state is repeatable.
    /// </summary>
    /// <returns><c>true</c> when the value or state is repeatable; otherwise, <c>false</c>.</returns>
    public bool IsRepeatable()
    {
        return repeatable;
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
    /// Gets the clears sensitive bytes.
    /// </summary>
    /// <returns><c>true</c> when the clears sensitive bytes setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetClearsSensitiveBytes()
    {
        return clearSensitiveBytes;
    }

    /// <summary>
    /// Determines whether the known length is present or available.
    /// </summary>
    /// <returns><c>true</c> when the known length is available; otherwise, <c>false</c>.</returns>
    public bool HasKnownLength()
    {
        return knownLength >= 0;
    }

    /// <summary>
    /// Gets the length.
    /// </summary>
    /// <returns>The current length.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public long GetLength()
    {
        if (knownLength < 0)
        {
            throw new InvalidOperationException("The content length is not known.");
        }

        return knownLength;
    }

    /// <summary>
    /// Determines whether the file path is present or available.
    /// </summary>
    /// <returns><c>true</c> when the file path is available; otherwise, <c>false</c>.</returns>
    public bool HasFilePath()
    {
        return filePath != null;
    }

    /// <summary>
    /// Gets the file path.
    /// </summary>
    /// <returns>The current file path.</returns>
    public string? GetFilePath()
    {
        return filePath;
    }

    /// <summary>
    /// Opens a read for caller-controlled reading or access.
    /// </summary>
    /// <returns>The stream produced by the operation.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public Stream? OpenRead()
    {
        EnsureOpen();
        if (kind == EmailContentKind.Memory)
        {
            return new MemoryStream((memory ?? throw new InvalidOperationException()), false);
        }

        if (kind == EmailContentKind.File || kind == EmailContentKind.TemporaryFile)
        {
            return new FileStream((filePath ?? throw new InvalidOperationException()), FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        if (kind == EmailContentKind.Slice)
        {
            return new MemoryStream(Materialize(knownLength), false);
        }

        if (!repeatable && streamOpened)
        {
            throw new InvalidOperationException("The non-seekable content stream has already been opened.");
        }

        if (repeatable)
        {
            (stream ?? throw new InvalidOperationException()).Position = streamStartPosition;
        }

        streamOpened = true;
        return stream;
    }

    /// <summary>
    /// Opens a read range for caller-controlled reading or access.
    /// </summary>
    /// <param name = "offset">The zero-based byte offset.</param>
    /// <param name = "length">The length used by the operation.</param>
    /// <returns>The stream produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public Stream OpenReadRange(long offset, long length)
    {
        EnsureOpen();
        if (offset < 0 || length < 0 || !HasKnownLength() || offset > knownLength || length > knownLength - offset)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (kind == EmailContentKind.Slice)
        {
            return (sliceSource ?? throw new InvalidOperationException()).OpenReadRange(sliceOffset + offset, length);
        }

        Stream? input = OpenRead();
        if (!(input ?? throw new InvalidOperationException()).CanSeek)
        {
            throw new InvalidOperationException("Range reads require seekable content.");
        }

        input.Position = input.Position + offset;
        return input;
    }

    /// <summary>
    /// Determines whether the current value or state is range read stream borrowed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is range read stream borrowed; otherwise, <c>false</c>.</returns>
    public bool IsRangeReadStreamBorrowed()
    {
        if (kind == EmailContentKind.Slice)
        {
            return (sliceSource ?? throw new InvalidOperationException()).IsRangeReadStreamBorrowed();
        }

        return kind == EmailContentKind.Stream;
    }

    /// <summary>
    /// Materializes the supplied value.
    /// </summary>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public byte[] Materialize(long maximumBytes)
    {
        EnsureOpen();
        if (maximumBytes < 0 || maximumBytes > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("maximumBytes");
        }

        if (knownLength > maximumBytes)
        {
            throw new EmailLimitException("MaxMaterializedBytes", maximumBytes, knownLength);
        }

        if (kind == EmailContentKind.Memory)
        {
            byte[] copy = new byte[(memory ?? throw new InvalidOperationException()).Length];
            memory.CopyTo(copy, 0);
            return copy;
        }

        if (kind == EmailContentKind.File || kind == EmailContentKind.TemporaryFile)
        {
            using (FileStream input = new FileStream((filePath ?? throw new InvalidOperationException()), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return ReadBounded(input, maximumBytes);
            }
        }

        if (kind == EmailContentKind.Slice)
        {
            return ReadSlice(sliceSource, sliceOffset, knownLength, maximumBytes);
        }

        Stream? streamInput = OpenRead();
        return ReadBounded(streamInput, maximumBytes);
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

        if ((kind == EmailContentKind.File || kind == EmailContentKind.TemporaryFile) && ownership == EmailContentOwnership.Owned)
        {
            File.Delete((filePath ?? throw new InvalidOperationException()));
        }

        if (kind == EmailContentKind.Stream && ownership == EmailContentOwnership.Owned)
        {
            (stream ?? throw new InvalidOperationException()).Close();
        }

        if (kind == EmailContentKind.Memory && ownership == EmailContentOwnership.Owned && clearSensitiveBytes)
        {
            Array.Clear((memory ?? throw new InvalidOperationException()), 0, memory.Length);
        }

        memory = null;
        stream = null;
        sliceSource = null;
        closed = true;
    }

    /// <summary>
    /// Releases streams, temporary storage, and other resources retained by this instance.
    /// </summary>
    public void Dispose()
    {
        Close();
    }

    static EmailContent CreatePathContent(string? path, EmailContentKind kind, EmailContentOwnership ownership)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        if (path.Length == 0)
        {
            throw new ArgumentException("File paths cannot be empty.", "path");
        }

        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The content file does not exist.", fullPath);
        }

        EmailContent content = new EmailContent();
        content.kind = kind;
        content.ownership = ownership;
        content.filePath = fullPath;
        content.knownLength = new FileInfo(fullPath).Length;
        content.repeatable = true;
        return content;
    }

    static byte[] ReadBounded(Stream? input, long maximumBytes)
    {
        using (MemoryStream output = new MemoryStream())
        {
            byte[] transfer = new byte[81920];
            long total = 0;
            while (true)
            {
                int read = (input ?? throw new InvalidOperationException()).Read(transfer, 0, transfer.Length);
                if (read == 0)
                {
                    break;
                }

                if (read > maximumBytes - total)
                {
                    throw new EmailLimitException("MaxMaterializedBytes", maximumBytes, total + read);
                }

                output.Write(transfer, 0, read);
                total += read;
            }

            return output.ToArray();
        }
    }

    static byte[] ReadSlice(EmailContent? source, long offset, long length, long maximumBytes)
    {
        if (length > maximumBytes)
        {
            throw new EmailLimitException("MaxMaterializedBytes", maximumBytes, length);
        }

        Stream? input = (source ?? throw new InvalidOperationException()).OpenRead();
        if ((input ?? throw new InvalidOperationException()).CanSeek)
        {
            input.Position += offset;
        }
        else
        {
            SkipBytes(input, offset);
        }

        using (MemoryStream output = new MemoryStream())
        {
            byte[] transfer = new byte[81920];
            long remaining = length;
            while (remaining > 0)
            {
                int requested = remaining < transfer.Length ? (int)remaining : transfer.Length;
                int read = input.Read(transfer, 0, requested);
                if (read == 0)
                {
                    throw new EndOfStreamException("The sliced content source ended before its declared range.");
                }

                output.Write(transfer, 0, read);
                remaining -= read;
            }

            return output.ToArray();
        }
    }

    static void SkipBytes(Stream input, long count)
    {
        byte[] transfer = new byte[81920];
        long remaining = count;
        while (remaining > 0)
        {
            int requested = remaining < transfer.Length ? (int)remaining : transfer.Length;
            int read = input.Read(transfer, 0, requested);
            if (read == 0)
            {
                throw new EndOfStreamException("The sliced content source ended before its declared offset.");
            }

            remaining -= read;
        }
    }

    static void ValidateOwnership(EmailContentOwnership ownership)
    {
        if (ownership < EmailContentOwnership.Borrowed || ownership > EmailContentOwnership.Owned)
        {
            throw new ArgumentOutOfRangeException("ownership");
        }
    }

    void EnsureOpen()
    {
        if (closed)
        {
            throw new ObjectDisposedException("EmailContent");
        }
    }
}
