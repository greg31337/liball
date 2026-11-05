using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Incrementally encodes quoted-printable.
/// </summary>
public class EmailQuotedPrintableEncoder : IEmailQuotedPrintableEncoderContract
{
    int lineLength;
    EmailQuotedPrintableNewlinePolicy newlinePolicy;
    bool protectFromLines;
    int lineColumn;
    long inputByteCount;
    long outputByteCount;
    bool pendingCarriageReturn;
    bool atSourceLineStart;
    List<byte> linePrefix;
    List<byte> pendingWhitespace;
    bool completed;
    /// <summary>
    /// Creates a new quoted-printable encoder.
    /// </summary>
    /// <param name = "lineLength">The line length used by the operation.</param>
    /// <param name = "newlinePolicy">The newline policy used by the operation.</param>
    /// <param name = "protectFromLines">Whether to enable the protect from lines behavior.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailQuotedPrintableEncoder(int lineLength, EmailQuotedPrintableNewlinePolicy newlinePolicy, bool protectFromLines)
    {
        new EmailQuotedPrintable().ValidateLineLength(lineLength);
        if (newlinePolicy < EmailQuotedPrintableNewlinePolicy.Preserve || newlinePolicy > EmailQuotedPrintableNewlinePolicy.Encode)
        {
            throw new ArgumentOutOfRangeException("newlinePolicy");
        }

        this.lineLength = lineLength;
        this.newlinePolicy = newlinePolicy;
        this.protectFromLines = protectFromLines;
        lineColumn = 0;
        inputByteCount = 0;
        outputByteCount = 0;
        pendingCarriageReturn = false;
        atSourceLineStart = true;
        linePrefix = new List<byte>();
        pendingWhitespace = new List<byte>();
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
    /// Gets the newline policy.
    /// </summary>
    /// <returns>The current newline policy.</returns>
    public EmailQuotedPrintableNewlinePolicy GetNewlinePolicy()
    {
        return newlinePolicy;
    }

    /// <summary>
    /// Gets the protect from lines.
    /// </summary>
    /// <returns><c>true</c> when the protect from lines setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetProtectFromLines()
    {
        return protectFromLines;
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
            throw new InvalidOperationException("Quoted-printable input cannot be supplied after encoding completes.");
        }

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
                    ProcessNewline(13, 10, output);
                    index++;
                    continue;
                }

                ProcessNewline(13, -1, output);
            }

            if (value == 13)
            {
                pendingCarriageReturn = true;
            }
            else if (value == 10)
            {
                ProcessNewline(10, -1, output);
            }
            else
            {
                ProcessContentByte(value, output);
            }

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
            throw new InvalidOperationException("Quoted-printable encoding has already completed.");
        }

        completed = true;
        List<byte> output = new List<byte>();
        if (pendingCarriageReturn)
        {
            pendingCarriageReturn = false;
            ProcessNewline(13, -1, output);
        }

        FlushLinePrefix(output);
        FlushPendingWhitespace(true, output);
        outputByteCount = new EmailSafeArithmetic().AddNonNegative(outputByteCount, output.Count);
        return output.ToArray();
    }

    void ProcessNewline(int first, int second, List<byte> output)
    {
        FlushLinePrefix(output);
        FlushPendingWhitespace(true, output);
        if (newlinePolicy == EmailQuotedPrintableNewlinePolicy.Encode)
        {
            WriteEncoded((byte)first, output);
            if (second >= 0)
            {
                WriteEncoded((byte)second, output);
            }

            atSourceLineStart = false;
            return;
        }

        if (newlinePolicy == EmailQuotedPrintableNewlinePolicy.NormalizeToCrlf)
        {
            output.Add(13);
            output.Add(10);
        }
        else
        {
            output.Add((byte)first);
            if (second >= 0)
            {
                output.Add((byte)second);
            }
        }

        lineColumn = 0;
        atSourceLineStart = true;
    }

    void ProcessContentByte(byte value, List<byte> output)
    {
        if (protectFromLines && atSourceLineStart)
        {
            int expected = GetFromByte(linePrefix.Count);
            if (value == expected)
            {
                linePrefix.Add(value);
                if (linePrefix.Count == 5)
                {
                    WriteEncoded(70, output);
                    WriteLiteral(114, output);
                    WriteLiteral(111, output);
                    WriteLiteral(109, output);
                    ProcessRegularByte(32, output);
                    linePrefix.Clear();
                    atSourceLineStart = false;
                }

                return;
            }

            atSourceLineStart = false;
            FlushLinePrefix(output);
        }
        else
        {
            atSourceLineStart = false;
        }

        ProcessRegularByte(value, output);
    }

    void ProcessRegularByte(byte value, List<byte> output)
    {
        if (value == 32 || value == 9)
        {
            pendingWhitespace.Add(value);
            return;
        }

        FlushPendingWhitespace(false, output);
        if (IsLiteral(value))
        {
            WriteLiteral(value, output);
        }
        else
        {
            WriteEncoded(value, output);
        }
    }

    void FlushLinePrefix(List<byte> output)
    {
        int index = 0;
        while (index < linePrefix.Count)
        {
            ProcessRegularByte(linePrefix[index], output);
            index++;
        }

        linePrefix.Clear();
    }

    void FlushPendingWhitespace(bool encode, List<byte> output)
    {
        int index = 0;
        while (index < pendingWhitespace.Count)
        {
            if (encode)
            {
                WriteEncoded(pendingWhitespace[index], output);
            }
            else
            {
                WriteLiteral(pendingWhitespace[index], output);
            }

            index++;
        }

        pendingWhitespace.Clear();
    }

    void WriteLiteral(byte value, List<byte> output)
    {
        bool wrapped = PrepareToken(1, output);
        if (wrapped && protectFromLines && value == 70)
        {
            AddEncodedValue(value, output);
            return;
        }

        output.Add(value);
        lineColumn++;
    }

    void WriteEncoded(byte value, List<byte> output)
    {
        PrepareToken(3, output);
        AddEncodedValue(value, output);
    }

    void AddEncodedValue(byte value, List<byte> output)
    {
        output.Add(61);
        output.Add(EncodeHex(value >> 4));
        output.Add(EncodeHex(value & 15));
        lineColumn += 3;
    }

    bool PrepareToken(int tokenLength, List<byte> output)
    {
        if (lineColumn > 0 && lineColumn + tokenLength > lineLength - 1)
        {
            output.Add(61);
            output.Add(13);
            output.Add(10);
            lineColumn = 0;
            return true;
        }

        return false;
    }

    static bool IsLiteral(byte value)
    {
        return value >= 33 && value <= 60 || value >= 62 && value <= 126;
    }

    static int GetFromByte(int index)
    {
        if (index == 0)
        {
            return 70;
        }

        if (index == 1)
        {
            return 114;
        }

        if (index == 2)
        {
            return 111;
        }

        if (index == 3)
        {
            return 109;
        }

        return 32;
    }

    static byte EncodeHex(int value)
    {
        if (value < 10)
        {
            return (byte)(48 + value);
        }

        return (byte)(65 + value - 10);
    }
}
