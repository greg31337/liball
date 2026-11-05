using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Reads and interprets line.
/// </summary>
public class EmailLineReader : IEmailLineReaderContract
{
    int maxLineBytes;
    List<byte> content;
    bool pendingCarriageReturn;
    bool ended;
    /// <summary>
    /// Creates a new line reader.
    /// </summary>
    /// <param name = "maxLineBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailLineReader(int maxLineBytes)
    {
        if (maxLineBytes < 1)
        {
            throw new ArgumentOutOfRangeException("maxLineBytes");
        }

        this.maxLineBytes = maxLineBytes;
        content = new List<byte>();
        pendingCarriageReturn = false;
        ended = false;
    }

    /// <summary>
    /// Gets the max line bytes.
    /// </summary>
    /// <returns>The current max line bytes.</returns>
    public int GetMaxLineBytes()
    {
        return maxLineBytes;
    }

    /// <summary>
    /// Gets the buffered byte count.
    /// </summary>
    /// <returns>The current buffered byte count.</returns>
    public int GetBufferedByteCount()
    {
        return content.Count;
    }

    /// <summary>
    /// Reads the next value from the input.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "endOfInput">Whether to enable the end of input behavior.</param>
    /// <returns>The line read result produced by the operation.</returns>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public EmailLineReadResult Read(ReadOnlySpan<byte> input, bool endOfInput)
    {
        if (ended)
        {
            if (input.Length != 0)
            {
                throw new InvalidOperationException("Input cannot be supplied after the reader reached end of input.");
            }

            return new EmailLineReadResultOperations().CreateEndOfInput();
        }

        int consumed = 0;
        if (pendingCarriageReturn)
        {
            if (input.Length == 0 && !endOfInput)
            {
                return new EmailLineReadResultOperations().CreateNeedMoreData(0);
            }

            pendingCarriageReturn = false;
            if (input.Length > 0 && input[0] == 10)
            {
                consumed = 1;
                EmailLineReadResult crlfResult = CreateLineResult(EmailLineEnding.CrLf, consumed);
                if (endOfInput && consumed == input.Length)
                {
                    ended = true;
                }

                return crlfResult;
            }

            EmailLineReadResult crResult = CreateLineResult(EmailLineEnding.Cr, 0);
            if (endOfInput && input.Length == 0)
            {
                ended = true;
            }

            return crResult;
        }

        while (consumed < input.Length)
        {
            byte value = input[consumed];
            if (value == 13)
            {
                consumed++;
                if (consumed < input.Length)
                {
                    if (input[consumed] == 10)
                    {
                        consumed++;
                        EmailLineReadResult crlfResult = CreateLineResult(EmailLineEnding.CrLf, consumed);
                        if (endOfInput && consumed == input.Length)
                        {
                            ended = true;
                        }

                        return crlfResult;
                    }

                    return CreateLineResult(EmailLineEnding.Cr, consumed);
                }

                if (endOfInput)
                {
                    ended = true;
                    return CreateLineResult(EmailLineEnding.Cr, consumed);
                }

                pendingCarriageReturn = true;
                return new EmailLineReadResultOperations().CreateNeedMoreData(consumed);
            }

            if (value == 10)
            {
                consumed++;
                EmailLineReadResult lfResult = CreateLineResult(EmailLineEnding.Lf, consumed);
                if (endOfInput && consumed == input.Length)
                {
                    ended = true;
                }

                return lfResult;
            }

            if (content.Count >= maxLineBytes)
            {
                throw new EmailLimitException("MaxLineBytes", maxLineBytes, (long)content.Count + 1);
            }

            content.Add(value);
            consumed++;
        }

        if (endOfInput)
        {
            ended = true;
            if (content.Count > 0)
            {
                return CreateLineResult(EmailLineEnding.None, consumed);
            }

            return new EmailLineReadResultOperations().CreateEndOfInput();
        }

        return new EmailLineReadResultOperations().CreateNeedMoreData(consumed);
    }

    /// <summary>
    /// Resets the supplied value.
    /// </summary>
    public void Reset()
    {
        content.Clear();
        pendingCarriageReturn = false;
        ended = false;
    }

    EmailLineReadResult CreateLineResult(EmailLineEnding lineEnding, int consumedByteCount)
    {
        EmailLine line = new EmailLine(content.ToArray(), lineEnding);
        content.Clear();
        return new EmailLineReadResultOperations().CreateLine(line, consumedByteCount);
    }
}
