using System;

namespace email;
/// <summary>
/// Represents a boundary delimiter.
/// </summary>
public class EmailBoundaryDelimiter : IEmailBoundaryDelimiterContract
{
    EmailRange sourceRange;
    bool closing;
    EmailLineEnding lineEnding;
    int precedingLineEndingByteCount;
    int transportPaddingByteCount;
    bool recovered;
    /// <summary>
    /// Creates a new boundary delimiter.
    /// </summary>
    /// <param name = "sourceRange">The source range used by the operation.</param>
    /// <param name = "closing">Whether to enable the closing behavior.</param>
    /// <param name = "lineEnding">The line ending used by the operation.</param>
    /// <param name = "precedingLineEndingByteCount">The number of items or bytes to process.</param>
    /// <param name = "transportPaddingByteCount">The number of items or bytes to process.</param>
    /// <param name = "recovered">Whether to enable the recovered behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailBoundaryDelimiter(EmailRange sourceRange, bool closing, EmailLineEnding lineEnding, int precedingLineEndingByteCount, int transportPaddingByteCount, bool recovered)
    {
        if (sourceRange == null)
        {
            throw new ArgumentNullException("sourceRange");
        }

        if (lineEnding < EmailLineEnding.None || lineEnding > EmailLineEnding.Cr)
        {
            throw new ArgumentOutOfRangeException("lineEnding");
        }

        if (precedingLineEndingByteCount < 0 || precedingLineEndingByteCount > 2)
        {
            throw new ArgumentOutOfRangeException("precedingLineEndingByteCount");
        }

        if (transportPaddingByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("transportPaddingByteCount");
        }

        this.sourceRange = sourceRange;
        this.closing = closing;
        this.lineEnding = lineEnding;
        this.precedingLineEndingByteCount = precedingLineEndingByteCount;
        this.transportPaddingByteCount = transportPaddingByteCount;
        this.recovered = recovered;
    }

    /// <summary>
    /// Gets the source range.
    /// </summary>
    /// <returns>The current source range.</returns>
    public EmailRange GetSourceRange()
    {
        return sourceRange;
    }

    /// <summary>
    /// Determines whether the current value or state is closing.
    /// </summary>
    /// <returns><c>true</c> when the value or state is closing; otherwise, <c>false</c>.</returns>
    public bool IsClosing()
    {
        return closing;
    }

    /// <summary>
    /// Gets the line ending.
    /// </summary>
    /// <returns>The current line ending.</returns>
    public EmailLineEnding GetLineEnding()
    {
        return lineEnding;
    }

    /// <summary>
    /// Gets the preceding line ending byte count.
    /// </summary>
    /// <returns>The current preceding line ending byte count.</returns>
    public int GetPrecedingLineEndingByteCount()
    {
        return precedingLineEndingByteCount;
    }

    /// <summary>
    /// Gets the transport padding byte count.
    /// </summary>
    /// <returns>The current transport padding byte count.</returns>
    public int GetTransportPaddingByteCount()
    {
        return transportPaddingByteCount;
    }

    /// <summary>
    /// Determines whether the operation or source value was recovered.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasRecovered()
    {
        return recovered;
    }
}
