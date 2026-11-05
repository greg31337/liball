using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Writes SMTP data in its wire or storage format.
/// </summary>
public class SmtpDataWriter : ISmtpDataWriterContract
{
    /// <summary>
    /// The default buffer bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultBufferBytes = 81920;
    EmailOutputSink? output;
    List<byte>? pendingOutput;
    long sourceByteCount;
    long dotStuffedLineCount;
    long normalizedLineEndingCount;
    bool pendingCarriageReturn;
    bool atLineStart;
    bool completed;
    /// <summary>
    /// Creates an SMTP DATA writer backed by a writable stream.
    /// </summary>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public SmtpDataWriter(Stream output)
    {
        if (output == null)
        {
            throw new ArgumentNullException("output");
        }

        if (!output.CanWrite)
        {
            throw new ArgumentException("The SMTP DATA output stream must be writable.", "output");
        }

        Initialize(EmailOutputSink.CreateWithStream(output));
    }

    internal static SmtpDataWriter CreateImplementationWithOutputUsingStream(Stream output)
    {
        return new SmtpDataWriter(output);
    }

    internal static SmtpDataWriter CreateImplementationWithOutputUsingEmailCountingStream(EmailCountingStream output)
    {
        if (output == null)
        {
            throw new ArgumentNullException("output");
        }

        SmtpDataWriter value = new SmtpDataWriter(Stream.Null);
        value.Initialize(EmailOutputSink.CreateWithCounter(output));
        return value;
    }

    internal static SmtpDataWriter CreateImplementationWithOutputUsingEmailHashingStream(EmailHashingStream output)
    {
        if (output == null)
        {
            throw new ArgumentNullException("output");
        }

        SmtpDataWriter value = new SmtpDataWriter(Stream.Null);
        value.Initialize(EmailOutputSink.CreateWithHashing(output));
        return value;
    }

    void Initialize(EmailOutputSink suppliedOutput)
    {
        output = suppliedOutput;
        pendingOutput = new List<byte>();
        sourceByteCount = 0;
        dotStuffedLineCount = 0;
        normalizedLineEndingCount = 0;
        pendingCarriageReturn = false;
        atLineStart = true;
        completed = false;
    }

    /// <summary>
    /// Processes the next input segment while retaining state for later segments.
    /// </summary>
    /// <remarks>
    /// Call this method in input order, then call <c>Complete</c> once no more input remains.
    /// </remarks>
    /// <param name = "input">The input stream or data to process.</param>
    public void Feed(ReadOnlySpan<byte> input)
    {
        EnsureActive();
        int index = 0;
        while (index < input.Length)
        {
            byte value = input[index];
            sourceByteCount = new EmailSafeArithmetic().AddNonNegative(sourceByteCount, 1);
            if (pendingCarriageReturn)
            {
                pendingCarriageReturn = false;
                if (value == 10)
                {
                    WriteCrlf();
                    index++;
                    continue;
                }

                normalizedLineEndingCount = new EmailSafeArithmetic().AddNonNegative(normalizedLineEndingCount, 1);
                WriteCrlf();
            }

            if (value == 13)
            {
                pendingCarriageReturn = true;
            }
            else if (value == 10)
            {
                normalizedLineEndingCount = new EmailSafeArithmetic().AddNonNegative(normalizedLineEndingCount, 1);
                WriteCrlf();
            }
            else
            {
                WriteContentByte(value);
            }

            index++;
        }

        FlushOutput();
    }

    /// <summary>
    /// Finishes incremental processing and returns the final output or status.
    /// </summary>
    /// <remarks>
    /// Calling this method finalizes buffered state; use the instance's status method before attempting further input.
    /// </remarks>
    /// <returns>The SMTP data write result produced by the operation.</returns>
    public SmtpDataWriteResult Complete()
    {
        EnsureActive();
        if (pendingCarriageReturn)
        {
            pendingCarriageReturn = false;
            normalizedLineEndingCount = new EmailSafeArithmetic().AddNonNegative(normalizedLineEndingCount, 1);
            WriteCrlf();
        }

        if (!atLineStart)
        {
            WriteCrlf();
        }

        FlushOutput();
        long dataByteCount = (output ?? throw new InvalidOperationException()).GetByteCount();
        output.Write(new byte[] { 46, 13, 10 });
        completed = true;
        return new SmtpDataWriteResult(sourceByteCount, dataByteCount, output.GetByteCount(), dotStuffedLineCount, normalizedLineEndingCount);
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
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public long GetSourceByteCount()
    {
        return sourceByteCount;
    }

    /// <summary>
    /// Gets the output byte count.
    /// </summary>
    /// <returns>The current output byte count.</returns>
    public long GetOutputByteCount()
    {
        return (output ?? throw new InvalidOperationException()).GetByteCount();
    }

    internal static byte[] FormatImplementation(ReadOnlySpan<byte> input)
    {
        MemoryStream output = new MemoryStream();
        SmtpDataWriter writer = new SmtpDataWriter(output);
        writer.Feed(input);
        writer.Complete();
        return output.ToArray();
    }

    internal static SmtpDataWriteResult WriteImplementation(Stream input, Stream output)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }

        if (!input.CanRead)
        {
            throw new ArgumentException("The SMTP DATA input stream must be readable.", "input");
        }

        SmtpDataWriter writer = new SmtpDataWriter(output);
        byte[] buffer = new byte[DefaultBufferBytes];
        while (true)
        {
            int read = input.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            writer.Feed(new ReadOnlySpan<byte>(buffer, 0, read));
        }

        return writer.Complete();
    }

    void WriteContentByte(byte value)
    {
        if (atLineStart && value == 46)
        {
            AddOutputByte(46);
            dotStuffedLineCount = new EmailSafeArithmetic().AddNonNegative(dotStuffedLineCount, 1);
        }

        AddOutputByte(value);
        atLineStart = false;
    }

    void WriteCrlf()
    {
        AddOutputByte(13);
        AddOutputByte(10);
        atLineStart = true;
    }

    void AddOutputByte(byte value)
    {
        (pendingOutput ?? throw new InvalidOperationException()).Add(value);
        if (pendingOutput.Count >= DefaultBufferBytes)
        {
            FlushOutput();
        }
    }

    void FlushOutput()
    {
        if ((pendingOutput ?? throw new InvalidOperationException()).Count > 0)
        {
            (output ?? throw new InvalidOperationException()).Write(pendingOutput.ToArray());
            pendingOutput.Clear();
        }
    }

    void EnsureActive()
    {
        if (completed)
        {
            throw new InvalidOperationException("SMTP DATA formatting has already completed.");
        }
    }
}
