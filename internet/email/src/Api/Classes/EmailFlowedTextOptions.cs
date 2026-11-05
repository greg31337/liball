using System;

namespace email;
/// <summary>
/// Controls how flowed text operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailFlowedTextOptions : IEmailFlowedTextOptionsContract
{
    bool deleteSpace;
    int maximumLineLength;
    int maximumCharacters;
    /// <summary>
    /// Creates a new flowed text options object.
    /// </summary>
    public EmailFlowedTextOptions()
    {
        deleteSpace = true;
        maximumLineLength = 78;
        maximumCharacters = 16 * 1024 * 1024;
    }

    /// <summary>
    /// Gets the delete space.
    /// </summary>
    /// <returns><c>true</c> when the delete space setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetDeleteSpace()
    {
        return deleteSpace;
    }

    /// <summary>
    /// Sets the delete space used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the delete space behavior.</param>
    public void SetDeleteSpace(bool value)
    {
        deleteSpace = value;
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
        if (value < 4 || value > 998)
            throw new ArgumentOutOfRangeException("value");
        maximumLineLength = value;
    }

    /// <summary>
    /// Gets the maximum characters.
    /// </summary>
    /// <returns>The current maximum characters.</returns>
    public int GetMaximumCharacters()
    {
        return maximumCharacters;
    }

    /// <summary>
    /// Sets the maximum characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumCharacters(int value)
    {
        if (value < 1)
            throw new ArgumentOutOfRangeException("value");
        maximumCharacters = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailFlowedTextOptions Clone()
    {
        EmailFlowedTextOptions copy = new EmailFlowedTextOptions();
        copy.SetDeleteSpace(deleteSpace);
        copy.SetMaximumLineLength(maximumLineLength);
        copy.SetMaximumCharacters(maximumCharacters);
        return copy;
    }
}
