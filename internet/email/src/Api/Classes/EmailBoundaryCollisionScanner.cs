using System;
using System.Text;

namespace email;
/// <summary>
/// Represents a boundary collision scanner.
/// </summary>
public class EmailBoundaryCollisionScanner : IEmailBoundaryCollisionScannerContract
{
    byte[] boundary;
    int stage;
    int boundaryIndex;
    long sourceByteCount;
    bool lineHasBytes;
    bool pendingCarriageReturn;
    bool collision;
    bool completed;
    /// <summary>
    /// Creates a new boundary collision scanner.
    /// </summary>
    /// <param name = "boundary">The boundary used by the operation.</param>
    public EmailBoundaryCollisionScanner(string boundary)
    {
        this.boundary = ValidateBoundary(boundary);
        stage = 0;
        boundaryIndex = 0;
        sourceByteCount = 0;
        lineHasBytes = false;
        pendingCarriageReturn = false;
        collision = false;
        completed = false;
    }

    /// <summary>
    /// Processes the next input segment while retaining state for later segments.
    /// </summary>
    /// <remarks>
    /// Call this method in input order, then call <c>Complete</c> once no more input remains.
    /// </remarks>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool Feed(ReadOnlySpan<byte> input)
    {
        EnsureActive();
        int index = 0;
        while (index < input.Length)
        {
            byte value = input[index];
            sourceByteCount++;
            if (!collision)
            {
                ProcessByte(value);
            }

            index++;
        }

        return collision;
    }

    /// <summary>
    /// Finishes incremental processing and returns the final output or status.
    /// </summary>
    /// <remarks>
    /// Calling this method finalizes buffered state; use the instance's status method before attempting further input.
    /// </remarks>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool Complete()
    {
        EnsureActive();
        if (!collision && lineHasBytes)
        {
            FinishLine();
        }

        pendingCarriageReturn = false;
        completed = true;
        return collision;
    }

    /// <summary>
    /// Determines whether the collision is present or available.
    /// </summary>
    /// <returns><c>true</c> when the collision is available; otherwise, <c>false</c>.</returns>
    public bool HasCollision()
    {
        return collision;
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

    void ProcessByte(byte value)
    {
        if (pendingCarriageReturn)
        {
            pendingCarriageReturn = false;
            if (value == 10)
            {
                return;
            }
        }

        if (value == 13)
        {
            FinishLine();
            pendingCarriageReturn = true;
            return;
        }

        if (value == 10)
        {
            FinishLine();
            return;
        }

        lineHasBytes = true;
        ProcessLineByte(value);
    }

    void ProcessLineByte(byte value)
    {
        if (stage == 7)
        {
            return;
        }

        if (stage == 0)
        {
            stage = value == 45 ? 1 : 7;
            return;
        }

        if (stage == 1)
        {
            if (value == 45)
            {
                stage = 2;
                boundaryIndex = 0;
            }
            else
            {
                stage = 7;
            }

            return;
        }

        if (stage == 2)
        {
            if (boundaryIndex < boundary.Length && value == boundary[boundaryIndex])
            {
                boundaryIndex++;
                if (boundaryIndex == boundary.Length)
                {
                    stage = 3;
                }
            }
            else
            {
                stage = 7;
            }

            return;
        }

        if (stage == 3)
        {
            if (value == 45)
            {
                stage = 4;
            }
            else if (value == 32 || value == 9)
            {
                stage = 6;
            }
            else
            {
                stage = 7;
            }

            return;
        }

        if (stage == 4)
        {
            stage = value == 45 ? 5 : 7;
            return;
        }

        if (stage == 5 || stage == 6)
        {
            if (value == 32 || value == 9)
            {
                stage = 6;
            }
            else
            {
                stage = 7;
            }
        }
    }

    void FinishLine()
    {
        if (stage == 3 || stage == 5 || stage == 6)
        {
            collision = true;
        }

        stage = 0;
        boundaryIndex = 0;
        lineHasBytes = false;
    }

    void EnsureActive()
    {
        if (completed)
        {
            throw new InvalidOperationException("The boundary collision scanner is already complete.");
        }
    }

    static byte[] ValidateBoundary(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Length < 1 || value.Length > 70)
        {
            throw new ArgumentException("A MIME boundary must contain between 1 and 70 bytes.", "value");
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current > 127 || !IsBoundaryCharacter(current) || index + 1 == value.Length && current == ' ')
            {
                throw new ArgumentException("A MIME boundary contains an invalid character.", "value");
            }

            index++;
        }

        return Encoding.ASCII.GetBytes(value);
    }

    static bool IsBoundaryCharacter(char value)
    {
        if (value >= '0' && value <= '9' || value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z')
        {
            return true;
        }

        return value == '\'' || value == '(' || value == ')' || value == '+' || value == '_' || value == ',' || value == '-' || value == '.' || value == '/' || value == ':' || value == '=' || value == '?' || value == ' ';
    }
}
