using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Incrementally parses an Internet message supplied in ordered byte segments.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailFeedParser : IDisposable, IEmailFeedParserContract
{
    EmailParseOptions? options;
    EmailTemporaryStore? store;
    EmailContent? inputContent;
    Stream? inputStream;
    EmailParseResult? result;
    long receivedByteCount;
    bool completionAttempted;
    bool completed;
    bool closed;
    /// <summary>
    /// Creates a feed parser using the supplied policy.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailFeedParser(EmailParseOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        Initialize(options);
    }

    internal static EmailFeedParser CreateImplementationWith()
    {
        return new EmailFeedParser(new EmailParseOptions());
    }

    internal static EmailFeedParser CreateImplementationWithOptions(EmailParseOptions options)
    {
        return new EmailFeedParser(options);
    }

    void Initialize(EmailParseOptions suppliedOptions)
    {
        options = suppliedOptions.Clone();
        store = new EmailTemporaryStore(options.GetStorageOptions());
        inputContent = null;
        inputStream = null;
        result = null;
        receivedByteCount = 0;
        completionAttempted = false;
        completed = false;
        closed = false;
    }

    /// <summary>
    /// Processes the next input segment while retaining state for later segments.
    /// </summary>
    /// <remarks>
    /// Call this method in input order, then call <c>Complete</c> once no more input remains.
    /// </remarks>
    /// <param name = "input">The input stream or data to process.</param>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public void Feed(ReadOnlySpan<byte> input)
    {
        EnsureActive();
        long proposed = new EmailSafeArithmetic().AddNonNegative(receivedByteCount, input.Length);
        long maximum = (options ?? throw new InvalidOperationException()).GetLimits().GetParsingLimits().GetMaxMessageBytes();
        if (proposed > maximum)
        {
            throw new EmailLimitException("MaxMessageBytes", maximum, proposed);
        }

        (store ?? throw new InvalidOperationException()).Write(input);
        receivedByteCount = proposed;
    }

    /// <summary>
    /// Finishes incremental processing and returns the final output or status.
    /// </summary>
    /// <remarks>
    /// Calling this method finalizes buffered state; use the instance's status method before attempting further input.
    /// </remarks>
    /// <returns>The parse result produced by the operation.</returns>
    public EmailParseResult Complete()
    {
        EnsureActive();
        completionAttempted = true;
        inputContent = (store ?? throw new InvalidOperationException()).Complete();
        store = null;
        inputStream = inputContent.OpenRead();
        result = new EmailParser().ParseMessageWithInputAndOptionsUsingStreamAndEmailParseOptions(inputStream, options);
        completed = true;
        return result;
    }

    /// <summary>
    /// Determines whether the result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the result is available; otherwise, <c>false</c>.</returns>
    public bool HasResult()
    {
        EnsureOpen();
        return result != null;
    }

    /// <summary>
    /// Gets the result.
    /// </summary>
    /// <returns>The current result.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public EmailParseResult GetResult()
    {
        EnsureOpen();
        if (result == null)
        {
            throw new InvalidOperationException("Complete must succeed before a feed-parser result is available.");
        }

        return result;
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        EnsureOpen();
        return completed;
    }

    /// <summary>
    /// Gets the received byte count.
    /// </summary>
    /// <returns>The current received byte count.</returns>
    public long GetReceivedByteCount()
    {
        EnsureOpen();
        return receivedByteCount;
    }

    /// <summary>
    /// Resets the supplied value.
    /// </summary>
    public void Reset()
    {
        EnsureOpen();
        CloseOwnedContent();
        store = new EmailTemporaryStore((options ?? throw new InvalidOperationException()).GetStorageOptions());
        inputContent = null;
        inputStream = null;
        result = null;
        receivedByteCount = 0;
        completionAttempted = false;
        completed = false;
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

        CloseOwnedContent();
        result = null;
        closed = true;
    }

    /// <summary>
    /// Releases streams, temporary storage, and other resources retained by this instance.
    /// </summary>
    public void Dispose()
    {
        Close();
    }

    void CloseOwnedContent()
    {
        if (store != null)
        {
            store.Close();
            store = null;
        }

        List<EmailContent> contents = new List<EmailContent>();
        if (result != null && result.HasMessage())
        {
            CloseMessage(result.GetMessage(), contents);
        }

        if (inputStream != null)
        {
            inputStream.Close();
            inputStream = null;
        }

        if (inputContent != null)
        {
            CloseContent(inputContent, contents);
            inputContent = null;
        }
    }

    void EnsureActive()
    {
        EnsureOpen();
        if (completionAttempted)
        {
            throw new InvalidOperationException("The feed parser has already been completed.");
        }
    }

    void EnsureOpen()
    {
        if (closed)
        {
            throw new ObjectDisposedException("EmailFeedParser");
        }
    }

    static void CloseMessage(EmailMessage? value, List<EmailContent> contents)
    {
        if ((value ?? throw new InvalidOperationException()).HasBody())
        {
            CloseEntity(value.GetBody(), contents);
        }

        if (value.HasSourceInfo())
        {
            CloseContent((value.GetSourceInfo() ?? throw new InvalidOperationException()).GetContent(), contents);
        }
    }

    static void CloseEntity(MimeEntity? entity, List<EmailContent> contents)
    {
        if ((entity ?? throw new InvalidOperationException()).HasRawContent())
        {
            CloseContent(entity.GetRawContent(), contents);
        }

        if (entity.HasDecodedContent())
        {
            CloseContent(entity.GetDecodedContent(), contents);
        }

        if (entity.HasPreamble())
        {
            CloseContent(entity.GetPreamble(), contents);
        }

        if (entity.HasEpilogue())
        {
            CloseContent(entity.GetEpilogue(), contents);
        }

        int index = 0;
        while (index < entity.GetChildCount())
        {
            CloseEntity(entity.GetChildAt(index), contents);
            index++;
        }

        if (entity.HasNestedMessage())
        {
            CloseMessage(entity.GetNestedMessage(), contents);
        }
    }

    static void CloseContent(EmailContent? content, List<EmailContent> contents)
    {
        if (!ContainsReference(contents, content))
        {
            contents.Add((content ?? throw new InvalidOperationException()));
            content.Close();
        }
    }

    static bool ContainsReference(List<EmailContent> contents, EmailContent? candidate)
    {
        int index = 0;
        while (index < contents.Count)
        {
            if (object.ReferenceEquals(contents[index], candidate))
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
