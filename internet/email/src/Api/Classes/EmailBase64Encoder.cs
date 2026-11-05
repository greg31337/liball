using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Incrementally encodes base64.
/// </summary>
public class EmailBase64Encoder : IEmailBase64EncoderContract
{
    int lineLength;
    int lineColumn;
    byte[] pending;
    int pendingCount;
    long inputByteCount;
    long outputByteCount;
    bool completed;
    /// <summary>
    /// Creates a new base64 encoder.
    /// </summary>
    /// <param name = "lineLength">The line length used by the operation.</param>
    public EmailBase64Encoder(int lineLength)
    {
        new EmailBase64().ValidateLineLength(lineLength);
        this.lineLength = lineLength;
        lineColumn = 0;
        pending = new byte[3];
        pendingCount = 0;
        inputByteCount = 0;
        outputByteCount = 0;
        completed = false;
    }

    /// <summary>
    /// Gets the line length.
    /// </summary>
    /// <returns>The current line length.</returns>
    public int GetLineLength()
    {
        return lineLength;
    }

    /// <summary>
    /// Gets the line column.
    /// </summary>
    /// <returns>The current line column.</returns>
    public int GetLineColumn()
    {
        return lineColumn;
    }

    /// <summary>
    /// Gets the input byte count.
    /// </summary>
    /// <returns>The current input byte count.</returns>
    public long GetInputByteCount()
    {
        return inputByteCount;
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
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return completed;
    }

    /// <summary>
    /// Processes the next input segment while retaining state for later segments.
    /// </summary>
    /// <remarks>
    /// Call this method in input order, then call <c>Complete</c> once no more input remains.
    /// </remarks>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public byte[] Feed(ReadOnlySpan<byte> input)
    {
        if (completed)
        {
            throw new InvalidOperationException("Base64 input cannot be supplied after encoding completes.");
        }

        List<byte> output = new List<byte>();
        int index = 0;
        while (pendingCount < 3 && index < input.Length)
        {
            pending[pendingCount] = input[index];
            pendingCount++;
            index++;
        }

        if (pendingCount == 3)
        {
            EncodeQuantum(pending[0], pending[1], pending[2], 3, output);
            pendingCount = 0;
        }

        while (input.Length - index >= 3)
        {
            EncodeQuantum(input[index], input[index + 1], input[index + 2], 3, output);
            index += 3;
        }

        while (index < input.Length)
        {
            pending[pendingCount] = input[index];
            pendingCount++;
            index++;
        }

        inputByteCount = new EmailSafeArithmetic().AddNonNegative(inputByteCount, input.Length);
        outputByteCount = new EmailSafeArithmetic().AddNonNegative(outputByteCount, output.Count);
        return output.ToArray();
    }

    /// <summary>
    /// Finishes incremental processing and returns the final output or status.
    /// </summary>
    /// <remarks>
    /// Calling this method finalizes buffered state; use the instance's status method before attempting further input.
    /// </remarks>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public byte[] Complete()
    {
        if (completed)
        {
            throw new InvalidOperationException("Base64 encoding has already completed.");
        }

        completed = true;
        List<byte> output = new List<byte>();
        if (pendingCount == 1)
        {
            EncodeQuantum(pending[0], 0, 0, 1, output);
        }
        else if (pendingCount == 2)
        {
            EncodeQuantum(pending[0], pending[1], 0, 2, output);
        }

        pending[0] = 0;
        pending[1] = 0;
        pending[2] = 0;
        pendingCount = 0;
        outputByteCount = new EmailSafeArithmetic().AddNonNegative(outputByteCount, output.Count);
        return output.ToArray();
    }

    void EncodeQuantum(byte first, byte second, byte third, int inputCount, List<byte> output)
    {
        if (lineColumn == lineLength)
        {
            output.Add(13);
            output.Add(10);
            lineColumn = 0;
        }

        output.Add(EncodeValue(first >> 2));
        output.Add(EncodeValue((first & 3) << 4 | second >> 4));
        if (inputCount >= 2)
        {
            output.Add(EncodeValue((second & 15) << 2 | third >> 6));
        }
        else
        {
            output.Add(61);
        }

        if (inputCount == 3)
        {
            output.Add(EncodeValue(third & 63));
        }
        else
        {
            output.Add(61);
        }

        lineColumn += 4;
    }

    static byte EncodeValue(int value)
    {
        if (value < 26)
        {
            return (byte)(65 + value);
        }

        if (value < 52)
        {
            return (byte)(97 + value - 26);
        }

        if (value < 62)
        {
            return (byte)(48 + value - 52);
        }

        if (value == 62)
        {
            return 43;
        }

        return 47;
    }
}
