using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Reads a message as a bounded sequence of structural header and body tokens.
/// </summary>
public class EmailTokenReader : IDisposable, IEmailTokenReaderContract
{
    EmailParseOptions? options;
    List<EmailTokenEntry>? entries;
    EmailTokenEntry? current;
    EmailTokenEntry? activeBody;
    EmailMessage? message;
    int nextIndex;
    int nextEntityIdentifier;
    long nextBodyOffset;
    long currentBodyOffset;
    int currentBodyLength;
    bool started;
    bool closed;
    bool complete;
    long consumedByteCount;
    long sourceByteCount;
    /// <summary>
    /// Creates a token reader using the supplied policy.
    /// </summary>
    public EmailTokenReader()
    {
        Initialize(new EmailParseOptions());
    }

    internal static EmailTokenReader CreateImplementationWith()
    {
        return new EmailTokenReader();
    }

    internal static EmailTokenReader CreateImplementationWithOptions(EmailParseOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailTokenReader value = new EmailTokenReader();
        value.Initialize(options);
        return value;
    }

    internal static EmailTokenReader CreateImplementationWithInputUsingValueArray(byte[] input)
    {
        EmailTokenReader value = new EmailTokenReader();
        value.Start(input);
        return value;
    }

    internal static EmailTokenReader CreateImplementationWithInputUsingReadOnlyMemory(ReadOnlyMemory<byte> input)
    {
        EmailTokenReader value = new EmailTokenReader();
        value.StartMemory(input);
        return value;
    }

    internal static EmailTokenReader CreateImplementationWithInputUsingStream(Stream input)
    {
        EmailTokenReader value = new EmailTokenReader();
        value.StartStream(input);
        return value;
    }

    internal static EmailTokenReader CreateImplementationWithInputAndOptionsUsingValueArrayAndEmailParseOptions(byte[] input, EmailParseOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailTokenReader value = new EmailTokenReader();
        value.Initialize(options);
        value.Start(input);
        return value;
    }

    internal static EmailTokenReader CreateImplementationWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(ReadOnlyMemory<byte> input, EmailParseOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailTokenReader value = new EmailTokenReader();
        value.Initialize(options);
        value.StartMemory(input);
        return value;
    }

    internal static EmailTokenReader CreateImplementationWithInputAndOptionsUsingStreamAndEmailParseOptions(Stream input, EmailParseOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailTokenReader value = new EmailTokenReader();
        value.Initialize(options);
        value.StartStream(input);
        return value;
    }

    void Initialize(EmailParseOptions suppliedOptions)
    {
        options = suppliedOptions.Clone();
        entries = new List<EmailTokenEntry>();
        current = null;
        activeBody = null;
        message = null;
        nextIndex = 0;
        nextEntityIdentifier = 0;
        nextBodyOffset = 0;
        currentBodyOffset = 0;
        currentBodyLength = 0;
        started = false;
        closed = false;
        complete = false;
        consumedByteCount = 0;
        sourceByteCount = 0;
    }

    /// <summary>
    /// Starts the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    public void Start(byte[] input)
    {
        EnsureCanStart();
        StartResult(new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, options));
    }

    /// <summary>
    /// Starts the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    public void StartMemory(ReadOnlyMemory<byte> input)
    {
        EnsureCanStart();
        StartResult(new EmailParser().ParseMessageWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(input, options));
    }

    /// <summary>
    /// Starts the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    public void StartStream(Stream input)
    {
        EnsureCanStart();
        StartResult(new EmailParser().ParseMessageWithInputAndOptionsUsingStreamAndEmailParseOptions(input, options));
    }

    /// <summary>
    /// Reads the next value from the input.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool Read()
    {
        EnsureStarted();
        if (activeBody != null)
        {
            return ReadBodyChunk();
        }

        while (nextIndex < (entries ?? throw new InvalidOperationException()).Count)
        {
            current = entries[nextIndex];
            nextIndex++;
            currentBodyLength = 0;
            if (current.GetKind() == EmailTokenKind.BodyChunk)
            {
                activeBody = current;
                nextBodyOffset = 0;
                if ((activeBody.GetContent() ?? throw new InvalidOperationException()).GetLength() == 0)
                {
                    activeBody = null;
                    continue;
                }

                return ReadBodyChunk();
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the token kind.
    /// </summary>
    /// <returns>The current token kind.</returns>
    public EmailTokenKind GetTokenKind()
    {
        EnsureCurrent();
        return (current ?? throw new InvalidOperationException()).GetKind();
    }

    /// <summary>
    /// Gets the headers.
    /// </summary>
    /// <returns>The current headers.</returns>
    public EmailHeaders? GetHeaders()
    {
        EnsureTokenKind(EmailTokenKind.Headers);
        return (current ?? throw new InvalidOperationException()).GetHeaders();
    }

    /// <summary>
    /// Opens a body chunk for caller-controlled reading or access.
    /// </summary>
    /// <returns>The stream produced by the operation.</returns>
    /// <exception cref = "EndOfStreamException">Thrown when the input ends before the required data is available.</exception>
    public Stream OpenBodyChunk()
    {
        EnsureTokenKind(EmailTokenKind.BodyChunk);
        EmailContent? content = (current ?? throw new InvalidOperationException()).GetContent();
        int length = currentBodyLength;
        byte[] bytes = new byte[length];
        Stream input = (content ?? throw new InvalidOperationException()).OpenReadRange(currentBodyOffset, length);
        int total = 0;
        while (total < length)
        {
            int read = input.Read(bytes, total, length - total);
            if (read == 0)
            {
                throw new EndOfStreamException("The MIME body content ended before the current token chunk.");
            }

            total += read;
        }

        if (!content.IsRangeReadStreamBorrowed())
        {
            input.Close();
        }

        return new MemoryStream(bytes, false);
    }

    /// <summary>
    /// Gets the body chunk byte count.
    /// </summary>
    /// <returns>The current body chunk byte count.</returns>
    public int GetBodyChunkByteCount()
    {
        EnsureTokenKind(EmailTokenKind.BodyChunk);
        return currentBodyLength;
    }

    /// <summary>
    /// Gets the body chunk kind.
    /// </summary>
    /// <returns>The current body chunk kind.</returns>
    public EmailBodyChunkKind GetBodyChunkKind()
    {
        EnsureTokenKind(EmailTokenKind.BodyChunk);
        return (current ?? throw new InvalidOperationException()).GetBodyChunkKind();
    }

    /// <summary>
    /// Gets the issue.
    /// </summary>
    /// <returns>The current issue.</returns>
    public EmailIssue? GetIssue()
    {
        EnsureTokenKind(EmailTokenKind.Error);
        return (current ?? throw new InvalidOperationException()).GetIssue();
    }

    /// <summary>
    /// Skips the body.
    /// </summary>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public void SkipBody()
    {
        EnsureCurrent();
        EmailTokenKind kind = (current ?? throw new InvalidOperationException()).GetKind();
        int identifier = current.GetEntityIdentifier();
        if (identifier < 0 || kind != EmailTokenKind.EntityStart && kind != EmailTokenKind.Headers && kind != EmailTokenKind.BodyChunk)
        {
            throw new InvalidOperationException("SkipBody requires the current entity start, entity headers, or body-chunk token.");
        }

        activeBody = null;
        currentBodyLength = 0;
        int index = nextIndex;
        while (index < (entries ?? throw new InvalidOperationException()).Count)
        {
            EmailTokenEntry candidate = entries[index];
            if (candidate.GetKind() == EmailTokenKind.EntityEnd && candidate.GetEntityIdentifier() == identifier)
            {
                nextIndex = index;
                return;
            }

            index++;
        }

        throw new InvalidOperationException("The current entity has no end token.");
    }

    /// <summary>
    /// Gets the depth.
    /// </summary>
    /// <returns>The current depth.</returns>
    public int GetDepth()
    {
        EnsureCurrent();
        return (current ?? throw new InvalidOperationException()).GetDepth();
    }

    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <returns>The current path.</returns>
    public EmailPath GetPath()
    {
        EnsureCurrent();
        return (current ?? throw new InvalidOperationException()).GetPath();
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        EnsureStarted();
        return complete;
    }

    /// <summary>
    /// Gets the consumed byte count.
    /// </summary>
    /// <returns>The current consumed byte count.</returns>
    public long GetConsumedByteCount()
    {
        EnsureStarted();
        return consumedByteCount;
    }

    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public long GetSourceByteCount()
    {
        EnsureStarted();
        return sourceByteCount;
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

        List<EmailContent> contents = new List<EmailContent>();
        if (message != null)
        {
            CloseMessage(message, contents);
        }

        int index = 0;
        while (index < (entries ?? throw new InvalidOperationException()).Count)
        {
            EmailContent? content = entries[index].GetContent();
            if (content != null)
            {
                CloseContent(content, contents);
            }

            index++;
        }

        entries.Clear();
        current = null;
        activeBody = null;
        message = null;
        closed = true;
    }

    /// <summary>
    /// Releases streams, temporary storage, and other resources retained by this instance.
    /// </summary>
    public void Dispose()
    {
        Close();
    }

    void StartResult(EmailParseResult result)
    {
        started = true;
        complete = result.IsComplete();
        consumedByteCount = result.GetConsumedByteCount();
        sourceByteCount = result.GetSourceByteCount();
        EmailPath root = new EmailPathOperations().CreateRoot();
        if (result.HasMessage())
        {
            message = result.GetMessage();
            AddMessage(message, 0, root);
        }

        EmailIssueList issues = result.GetIssues();
        int issueIndex = 0;
        while (issueIndex < issues.GetCount())
        {
            EmailIssue issue = issues.GetAt(issueIndex);
            EmailPath? issuePath = issue.HasEntityPath() ? issue.GetEntityPath() : root;
            (entries ?? throw new InvalidOperationException()).Add(new EmailTokenEntry(EmailTokenKind.Error, null, null, 0, 0, EmailBodyChunkKind.Content, issue, (issuePath ?? throw new InvalidOperationException()).GetDepth(), issuePath, -1));
            issueIndex++;
        }

        (entries ?? throw new InvalidOperationException()).Add(new EmailTokenEntry(EmailTokenKind.End, null, null, 0, 0, EmailBodyChunkKind.Content, null, 0, root, -1));
    }

    void AddMessage(EmailMessage? value, int depth, EmailPath path)
    {
        (entries ?? throw new InvalidOperationException()).Add(new EmailTokenEntry(EmailTokenKind.MessageStart, null, null, 0, 0, EmailBodyChunkKind.Content, null, depth, path, -1));
        entries.Add(new EmailTokenEntry(EmailTokenKind.Headers, (value ?? throw new InvalidOperationException()).GetHeaders(), null, 0, 0, EmailBodyChunkKind.Content, null, depth, path, -1));
        if (value.HasBody())
        {
            AddEntity(value.GetBody(), depth, path);
        }

        entries.Add(new EmailTokenEntry(EmailTokenKind.MessageEnd, null, null, 0, 0, EmailBodyChunkKind.Content, null, depth, path, -1));
    }

    void AddEntity(MimeEntity? entity, int depth, EmailPath path)
    {
        int identifier = nextEntityIdentifier;
        nextEntityIdentifier++;
        (entries ?? throw new InvalidOperationException()).Add(new EmailTokenEntry(EmailTokenKind.EntityStart, null, null, 0, 0, EmailBodyChunkKind.Content, null, depth, path, identifier));
        entries.Add(new EmailTokenEntry(EmailTokenKind.Headers, (entity ?? throw new InvalidOperationException()).GetHeaders(), null, 0, 0, EmailBodyChunkKind.Content, null, depth, path, identifier));
        if (entity.IsContainer())
        {
            if (entity.HasPreamble())
            {
                AddBodyChunks(entity.GetPreamble(), EmailBodyChunkKind.Preamble, depth, path, identifier);
            }

            int childIndex = 0;
            while (childIndex < entity.GetChildCount())
            {
                AddEntity(entity.GetChildAt(childIndex), depth + 1, path.Append(childIndex));
                childIndex++;
            }

            if (entity.HasEpilogue())
            {
                AddBodyChunks(entity.GetEpilogue(), EmailBodyChunkKind.Epilogue, depth, path, identifier);
            }
        }
        else if (entity.HasNestedMessage())
        {
            EmailPath nestedPath = path.Append(0);
            AddMessage(entity.GetNestedMessage(), depth + 1, nestedPath);
        }
        else if (entity.HasRawContent())
        {
            AddBodyChunks(entity.GetRawContent(), EmailBodyChunkKind.Content, depth, path, identifier);
        }
        else if (entity.HasDecodedContent())
        {
            AddBodyChunks(entity.GetDecodedContent(), EmailBodyChunkKind.Content, depth, path, identifier);
        }

        entries.Add(new EmailTokenEntry(EmailTokenKind.EntityEnd, null, null, 0, 0, EmailBodyChunkKind.Content, null, depth, path, identifier));
    }

    void AddBodyChunks(EmailContent? content, EmailBodyChunkKind kind, int depth, EmailPath path, int entityIdentifier)
    {
        if (!(content ?? throw new InvalidOperationException()).HasKnownLength())
        {
            throw new InvalidOperationException("Token-reader body content requires a known length.");
        }

        int chunkBytes = (options ?? throw new InvalidOperationException()).GetStorageOptions().GetCopyBufferBytes();
        if (content.GetLength() > 0)
        {
            (entries ?? throw new InvalidOperationException()).Add(new EmailTokenEntry(EmailTokenKind.BodyChunk, null, content, 0, chunkBytes, kind, null, depth, path, entityIdentifier));
        }
    }

    bool ReadBodyChunk()
    {
        current = activeBody;
        currentBodyOffset = nextBodyOffset;
        long remaining = ((activeBody ?? throw new InvalidOperationException()).GetContent() ?? throw new InvalidOperationException()).GetLength() - nextBodyOffset;
        int maximum = activeBody.GetContentLength();
        currentBodyLength = remaining < maximum ? (int)remaining : maximum;
        nextBodyOffset += currentBodyLength;
        if (nextBodyOffset == (activeBody.GetContent() ?? throw new InvalidOperationException()).GetLength())
        {
            activeBody = null;
        }

        return true;
    }

    void EnsureCanStart()
    {
        if (closed)
        {
            throw new ObjectDisposedException("EmailTokenReader");
        }

        if (started)
        {
            throw new InvalidOperationException("The token reader has already been started.");
        }
    }

    void EnsureStarted()
    {
        if (closed)
        {
            throw new ObjectDisposedException("EmailTokenReader");
        }

        if (!started)
        {
            throw new InvalidOperationException("The token reader has not been started.");
        }
    }

    void EnsureCurrent()
    {
        EnsureStarted();
        if (current == null)
        {
            throw new InvalidOperationException("Read must produce a token before token data can be inspected.");
        }
    }

    void EnsureTokenKind(EmailTokenKind expected)
    {
        EnsureCurrent();
        if ((current ?? throw new InvalidOperationException()).GetKind() != expected)
        {
            throw new InvalidOperationException("The current token does not expose the requested data.");
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
}
