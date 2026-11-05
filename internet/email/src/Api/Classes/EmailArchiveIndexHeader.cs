using System;

namespace email;
/// <summary>
/// Represents an archive index header.
/// </summary>
public class EmailArchiveIndexHeader : IEmailArchiveIndexHeaderContract
{
    string name;
    string value;
    int occurrenceIndex;
    /// <summary>
    /// Creates a new archive index header.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "occurrenceIndex">The zero-based occurrence index.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArchiveIndexHeader(string name, string value, int occurrenceIndex)
    {
        if (name == null || value == null)
            throw new ArgumentNullException("name");
        if (name.Length == 0)
            throw new ArgumentException("An indexed header name cannot be empty.", "name");
        if (occurrenceIndex < 0)
            throw new ArgumentOutOfRangeException("occurrenceIndex");
        this.name = name;
        this.value = value;
        this.occurrenceIndex = occurrenceIndex;
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
    /// Gets the value produced by the operation, or null when no value is available.
    /// </summary>
    /// <returns>The current value.</returns>
    public string GetValue()
    {
        return value;
    }

    /// <summary>
    /// Gets the occurrence index.
    /// </summary>
    /// <returns>The current occurrence index.</returns>
    public int GetOccurrenceIndex()
    {
        return occurrenceIndex;
    }
}
