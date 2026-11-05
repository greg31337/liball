using System;

namespace email;
/// <summary>
/// Controls how authentication format operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailAuthenticationFormatOptions : IEmailAuthenticationFormatOptionsContract
{
    int firstLineColumn;
    int maximumLineLength;
    int maximumOutputBytes;
    bool includeComments;
    /// <summary>
    /// Creates a new authentication format options object.
    /// </summary>
    public EmailAuthenticationFormatOptions()
    {
        firstLineColumn = 0;
        maximumLineLength = 78;
        maximumOutputBytes = EmailParsingLimitsConstants.DefaultMaxLineBytes;
        includeComments = true;
    }

    /// <summary>
    /// Gets the first line column.
    /// </summary>
    /// <returns>The current first line column.</returns>
    public int GetFirstLineColumn()
    {
        return firstLineColumn;
    }

    /// <summary>
    /// Sets the first line column used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetFirstLineColumn(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        firstLineColumn = value;
    }

    /// <summary>
    /// Gets the maximum line length.
    /// </summary>
    /// <returns>The current maximum line length.</returns>
    public int GetMaximumLineLength()
    {
        return maximumLineLength;
    }

    /// <summary>
    /// Sets the maximum line length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumLineLength(int value)
    {
        if (value < 20 || value > 998)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumLineLength = value;
    }

    /// <summary>
    /// Gets the maximum output bytes.
    /// </summary>
    /// <returns>The current maximum output bytes.</returns>
    public int GetMaximumOutputBytes()
    {
        return maximumOutputBytes;
    }

    /// <summary>
    /// Sets the maximum output bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumOutputBytes(int value)
    {
        maximumOutputBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the include comments.
    /// </summary>
    /// <returns><c>true</c> when the include comments setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeComments()
    {
        return includeComments;
    }

    /// <summary>
    /// Sets the include comments used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include comments behavior.</param>
    public void SetIncludeComments(bool value)
    {
        includeComments = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailAuthenticationFormatOptions Clone()
    {
        EmailAuthenticationFormatOptions copy = new EmailAuthenticationFormatOptions();
        copy.SetFirstLineColumn(firstLineColumn);
        copy.SetMaximumLineLength(maximumLineLength);
        copy.SetMaximumOutputBytes(maximumOutputBytes);
        copy.SetIncludeComments(includeComments);
        return copy;
    }
}
