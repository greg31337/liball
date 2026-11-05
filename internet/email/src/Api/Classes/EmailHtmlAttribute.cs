using System;

namespace email;
/// <summary>
/// Represents a HTML attribute.
/// </summary>
public class EmailHtmlAttribute : IEmailHtmlAttributeContract
{
    string name;
    string rawValue;
    string value;
    bool hasValue;
    int sourceOffset;
    int sourceLength;
    /// <summary>
    /// Creates a new HTML attribute.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "rawValue">The raw value used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "hasValue">Whether to enable the has value behavior.</param>
    /// <param name = "sourceOffset">The zero-based source offset.</param>
    /// <param name = "sourceLength">The source length used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHtmlAttribute(string name, string rawValue, string value, bool hasValue, int sourceOffset, int sourceLength)
    {
        if (name == null || name.Length == 0)
            throw new ArgumentException("An HTML attribute name is required.", "name");
        if (rawValue == null)
            throw new ArgumentNullException("rawValue");
        if (value == null)
            throw new ArgumentNullException("value");
        if (sourceOffset < 0)
            throw new ArgumentOutOfRangeException("sourceOffset");
        if (sourceLength < 1)
            throw new ArgumentOutOfRangeException("sourceLength");
        this.name = name;
        this.rawValue = rawValue;
        this.value = value;
        this.hasValue = hasValue;
        this.sourceOffset = sourceOffset;
        this.sourceLength = sourceLength;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    public string GetName()
    {
        return name;
    }

    /// <summary>
    /// Gets the raw value.
    /// </summary>
    /// <returns>The current raw value.</returns>
    public string GetRawValue()
    {
        return rawValue;
    }

    /// <summary>
    /// Gets the value produced by the operation, or null when no value is available.
    /// </summary>
    /// <returns>The current value.</returns>
    public string GetValue()
    {
        return value;
    }

    /// <summary>
    /// Determines whether the value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the value is available; otherwise, <c>false</c>.</returns>
    public bool HasValue()
    {
        return hasValue;
    }

    /// <summary>
    /// Gets the source offset.
    /// </summary>
    /// <returns>The current source offset.</returns>
    public int GetSourceOffset()
    {
        return sourceOffset;
    }

    /// <summary>
    /// Gets the source length.
    /// </summary>
    /// <returns>The current source length.</returns>
    public int GetSourceLength()
    {
        return sourceLength;
    }
}
