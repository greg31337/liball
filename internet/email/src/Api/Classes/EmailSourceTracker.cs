using System;

namespace email;
/// <summary>
/// Represents a source tracker.
/// </summary>
public class EmailSourceTracker : IEmailSourceTrackerContract
{
    long byteOffset;
    long lineNumber;
    long columnNumber;
    bool previousWasCarriageReturn;
    /// <summary>
    /// Creates a source tracker at the supplied position.
    /// </summary>
    public EmailSourceTracker()
    {
        byteOffset = 0;
        lineNumber = 1;
        columnNumber = 1;
        previousWasCarriageReturn = false;
    }

    internal static EmailSourceTracker CreateImplementationWith()
    {
        return new EmailSourceTracker();
    }

    internal static EmailSourceTracker CreateImplementationWithInitialPosition(EmailSourcePosition? initialPosition)
    {
        if (initialPosition == null)
        {
            throw new ArgumentNullException("initialPosition");
        }

        EmailSourceTracker value = new EmailSourceTracker();
        value.byteOffset = initialPosition.GetByteOffset();
        value.lineNumber = initialPosition.GetLineNumber();
        value.columnNumber = initialPosition.GetColumnNumber();
        return value;
    }

    /// <summary>
    /// Gets the position.
    /// </summary>
    /// <returns>The current position.</returns>
    public EmailSourcePosition GetPosition()
    {
        return new EmailSourcePosition(byteOffset, lineNumber, columnNumber);
    }

    /// <summary>
    /// Gets the byte offset.
    /// </summary>
    /// <returns>The current byte offset.</returns>
    public long GetByteOffset()
    {
        return byteOffset;
    }

    /// <summary>
    /// Gets the line number.
    /// </summary>
    /// <returns>The current line number.</returns>
    public long GetLineNumber()
    {
        return lineNumber;
    }

    /// <summary>
    /// Gets the column number.
    /// </summary>
    /// <returns>The current column number.</returns>
    public long GetColumnNumber()
    {
        return columnNumber;
    }

    /// <summary>
    /// Advances the supplied value.
    /// </summary>
    /// <param name = "bytes">The source bytes or data to process.</param>
    /// <exception cref = "OverflowException">Thrown when the operation encounters an overflow.</exception>
    public void Advance(ReadOnlySpan<byte> bytes)
    {
        long nextByteOffset = byteOffset;
        long nextLineNumber = lineNumber;
        long nextColumnNumber = columnNumber;
        bool nextPreviousWasCarriageReturn = previousWasCarriageReturn;
        int index = 0;
        while (index < bytes.Length)
        {
            if (nextByteOffset == long.MaxValue)
            {
                throw new OverflowException("The source byte offset exceeds Int64.MaxValue.");
            }

            nextByteOffset++;
            byte value = bytes[index];
            if (value == 13)
            {
                if (nextLineNumber == long.MaxValue)
                {
                    throw new OverflowException("The source line number exceeds Int64.MaxValue.");
                }

                nextLineNumber++;
                nextColumnNumber = 1;
                nextPreviousWasCarriageReturn = true;
            }
            else if (value == 10)
            {
                if (!nextPreviousWasCarriageReturn)
                {
                    if (nextLineNumber == long.MaxValue)
                    {
                        throw new OverflowException("The source line number exceeds Int64.MaxValue.");
                    }

                    nextLineNumber++;
                    nextColumnNumber = 1;
                }

                nextPreviousWasCarriageReturn = false;
            }
            else
            {
                if (nextColumnNumber == long.MaxValue)
                {
                    throw new OverflowException("The source column number exceeds Int64.MaxValue.");
                }

                nextColumnNumber++;
                nextPreviousWasCarriageReturn = false;
            }

            index++;
        }

        byteOffset = nextByteOffset;
        lineNumber = nextLineNumber;
        columnNumber = nextColumnNumber;
        previousWasCarriageReturn = nextPreviousWasCarriageReturn;
    }

    /// <summary>
    /// Resets the supplied value.
    /// </summary>
    public void Reset()
    {
        byteOffset = 0;
        lineNumber = 1;
        columnNumber = 1;
        previousWasCarriageReturn = false;
    }
}
