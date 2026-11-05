using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a disposition notification option.
/// </summary>
public class EmailDispositionNotificationOption : IEmailDispositionNotificationOptionContract
{
    string attribute;
    EmailDispositionNotificationOptionImportance importance;
    List<string> values;
    /// <summary>
    /// Creates a new disposition notification option.
    /// </summary>
    /// <param name = "attribute">The attribute used by the operation.</param>
    /// <param name = "importance">The importance used by the operation.</param>
    /// <param name = "values">The values used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDispositionNotificationOption(string attribute, EmailDispositionNotificationOptionImportance importance, List<string> values)
    {
        if (!EmailDispositionNotificationRequestValueParser.IsAtom(attribute))
            throw new ArgumentException("An MDN option attribute must be an atom.", "attribute");
        if (importance != EmailDispositionNotificationOptionImportance.Required && importance != EmailDispositionNotificationOptionImportance.Optional)
            throw new ArgumentOutOfRangeException("importance");
        if (values == null)
            throw new ArgumentNullException("values");
        if (values.Count == 0)
            throw new ArgumentException("An MDN option requires at least one value.", "values");
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == null || values[index].Length == 0 || !EmailDispositionNotificationRequestValueParser.IsAsciiWordValue(values[index]))
                throw new ArgumentException("MDN option values must be nonempty printable ASCII words.", "values");
            index++;
        }

        this.attribute = attribute.ToLowerInvariant();
        this.importance = importance;
        this.values = new List<string>(values);
    }

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    /// <returns>The current attribute.</returns>
    public string GetAttribute()
    {
        return attribute;
    }

    /// <summary>
    /// Gets the importance.
    /// </summary>
    /// <returns>The current importance.</returns>
    public EmailDispositionNotificationOptionImportance GetImportance()
    {
        return importance;
    }

    /// <summary>
    /// Gets the value count.
    /// </summary>
    /// <returns>The current value count.</returns>
    public int GetValueCount()
    {
        return values.Count;
    }

    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public string GetValueAt(int index)
    {
        if (index < 0 || index >= values.Count)
            throw new ArgumentOutOfRangeException("index");
        return values[index];
    }

    /// <summary>
    /// Gets the values.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current values.</returns>
    public List<string> GetValues()
    {
        return new List<string>(values);
    }
}
