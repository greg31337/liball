using System.Collections.Generic;

namespace csv;

/// <summary>
/// Provides field-level access and lifecycle control for a CSV reader.
/// </summary>
public interface ICsvReaderControl
{
    /// <summary>
    /// Returns a field from the current row by zero-based position.
    /// </summary>
    /// <param name="index">The zero-based field position.</param>
    /// <returns>The field value, or empty text when the position is unavailable.</returns>
    string? GetField(int index);

    /// <summary>
    /// Returns the number of fields in the current row.
    /// </summary>
    /// <returns>The field count.</returns>
    int CountFields();

    /// <summary>
    /// Determines whether the current row contains the requested position.
    /// </summary>
    /// <param name="index">The zero-based field position.</param>
    /// <returns><c>true</c> when the field exists; otherwise <c>false</c>.</returns>
    bool HasField(int index);

    /// <summary>
    /// Returns a field from the current row by header name.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <returns>The field value, or empty text when the name is unavailable.</returns>
    string? GetFieldByName(string name);

    /// <summary>
    /// Determines whether the current row contains a named field.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <returns><c>true</c> when the field exists; otherwise <c>false</c>.</returns>
    bool HasFieldByName(string name);

    /// <summary>
    /// Replaces a field in the current row by position.
    /// </summary>
    /// <param name="index">The zero-based field position.</param>
    /// <param name="value">The replacement value.</param>
    void SetField(int index, string? value);

    /// <summary>
    /// Replaces a field in the current row by header name.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The replacement value.</param>
    void SetFieldByName(string name, string? value);

    /// <summary>
    /// Returns the zero-based position of a header name.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <returns>The position, or -1 when no matching header exists.</returns>
    int GetHeaderIndex(string name);

    /// <summary>
    /// Returns rows collected by reject-mode parsing.
    /// </summary>
    /// <returns>The rejected rows.</returns>
    List<CsvRow> GetRejects();

    /// <summary>
    /// Closes the reader and, unless configured otherwise, its source.
    /// </summary>
    void Close();
}
