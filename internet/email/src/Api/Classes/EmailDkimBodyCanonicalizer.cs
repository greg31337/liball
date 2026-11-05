using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a DKIM body canonicalizer.
/// </summary>
public class EmailDkimBodyCanonicalizer : IDisposable, IEmailDkimBodyCanonicalizerContract
{
    EmailDkimBodyCanonicalizationOptions options;
    EmailHashingStream hash;
    byte[] hashByte;
    long sourceByteCount;
    long canonicalizedByteCount;
    long pendingEmptyLineCount;
    bool pendingCarriageReturn;
    bool pendingWhitespace;
    bool currentLineHasContent;
    bool emittedContent;
    bool complete;
    EmailDkimBodyCanonicalizationResult? result;
    /// <summary>
    /// Creates a new DKIM body canonicalizer.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDkimBodyCanonicalizer(EmailDkimBodyCanonicalizationOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        this.options = options.Clone();
        hash = new EmailHashingStream(this.options.GetHashAlgorithm());
        hashByte = new byte[1];
        sourceByteCount = 0;
        canonicalizedByteCount = 0;
        pendingEmptyLineCount = 0;
        pendingCarriageReturn = false;
        pendingWhitespace = false;
        currentLineHasContent = false;
        emittedContent = false;
        complete = false;
        result = null;
    }

    /// <summary>
    /// Processes the next input segment while retaining state for later segments.
    /// </summary>
    /// <remarks>
    /// Call this method in input order, then call <c>Complete</c> once no more input remains.
    /// </remarks>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public byte[] Feed(ReadOnlySpan<byte> input)
    {
        EnsureActive();
        if (input.Length > options.GetMaximumInputBytes() - sourceByteCount)
            throw new EmailLimitException("MaxDkimBodyInputBytes", options.GetMaximumInputBytes(), sourceByteCount + input.Length);
        sourceByteCount += input.Length;
        List<byte> output = new List<byte>();
        int index = 0;
        while (index < input.Length)
        {
            byte value = input[index];
            if (pendingCarriageReturn)
            {
                pendingCarriageReturn = false;
                if (value == 10)
                {
                    ProcessLineEnd(output);
                    index++;
                    continue;
                }

                ProcessContent(13, output);
            }

            if (value == 13)
                pendingCarriageReturn = true;
            else
                ProcessContent(value, output);
            index++;
        }

        return output.ToArray();
    }

    /// <summary>
    /// Finishes incremental processing and returns the final output or status.
    /// </summary>
    /// <remarks>
    /// Calling this method finalizes buffered state; use the instance's status method before attempting further input.
    /// </remarks>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] Complete()
    {
        EnsureActive();
        List<byte> output = new List<byte>();
        if (pendingCarriageReturn)
        {
            pendingCarriageReturn = false;
            ProcessContent(13, output);
        }

        pendingWhitespace = false;
        if (currentLineHasContent)
            EmitLineBreak(output);
        if (options.GetKind() == EmailDkimCanonicalizationKind.Simple && !emittedContent)
            EmitLineBreak(output);
        pendingEmptyLineCount = 0;
        EmailHashResult hashResult = hash.Complete();
        result = new EmailDkimBodyCanonicalizationResult(options.GetKind(), hashResult, sourceByteCount, canonicalizedByteCount, options.HasBodyLengthLimit(), options.GetBodyLengthLimit());
        complete = true;
        return output.ToArray();
    }

    /// <summary>
    /// Gets the result.
    /// </summary>
    /// <returns>The current result.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public EmailDkimBodyCanonicalizationResult GetResult()
    {
        if (!complete || result == null)
            throw new InvalidOperationException("DKIM body canonicalization is not complete.");
        return result;
    }

    /// <summary>
    /// Closes the supplied value.
    /// </summary>
    public void Close()
    {
        if (complete)
            return;
        hash.Close();
        complete = true;
    }

    /// <summary>
    /// Releases streams, temporary storage, and other resources retained by this instance.
    /// </summary>
    public void Dispose()
    {
        Close();
    }

    void ProcessContent(byte value, List<byte> output)
    {
        if (options.GetKind() == EmailDkimCanonicalizationKind.Relaxed && (value == 32 || value == 9))
        {
            pendingWhitespace = true;
            return;
        }

        FlushEmptyLines(output);
        if (options.GetKind() == EmailDkimCanonicalizationKind.Relaxed && pendingWhitespace)
        {
            Emit(32, output);
            pendingWhitespace = false;
        }

        Emit(value, output);
        currentLineHasContent = true;
    }

    void ProcessLineEnd(List<byte> output)
    {
        pendingWhitespace = false;
        if (currentLineHasContent)
        {
            EmitLineBreak(output);
            currentLineHasContent = false;
        }
        else
        {
            pendingEmptyLineCount++;
        }
    }

    void FlushEmptyLines(List<byte> output)
    {
        while (pendingEmptyLineCount > 0)
        {
            EmitLineBreak(output);
            pendingEmptyLineCount--;
        }
    }

    void EmitLineBreak(List<byte> output)
    {
        Emit(13, output);
        Emit(10, output);
    }

    void Emit(byte value, List<byte> output)
    {
        if (canonicalizedByteCount >= options.GetMaximumCanonicalizedBytes())
            throw new EmailLimitException("MaxDkimCanonicalizedBodyBytes", options.GetMaximumCanonicalizedBytes(), canonicalizedByteCount + 1);
        if (!options.HasBodyLengthLimit() || canonicalizedByteCount < options.GetBodyLengthLimit())
        {
            if (hash.GetByteCount() >= options.GetMaximumHashedBytes())
                throw new EmailLimitException("MaxDkimHashedBodyBytes", options.GetMaximumHashedBytes(), hash.GetByteCount() + 1);
            hashByte[0] = value;
            hash.WriteSegment(hashByte, 0, 1);
        }

        output.Add(value);
        canonicalizedByteCount++;
        emittedContent = true;
    }

    void EnsureActive()
    {
        if (complete)
            throw new InvalidOperationException("DKIM body canonicalization is already complete.");
    }
}
