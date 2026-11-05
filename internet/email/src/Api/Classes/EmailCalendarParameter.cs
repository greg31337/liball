using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a calendar parameter.
/// </summary>
public class EmailCalendarParameter : IEmailCalendarParameterContract
{
    string? name;
    List<string>? values;
    /// <summary>
    /// Creates a single-valued calendar parameter.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCalendarParameter(string name, string value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        List<string> supplied = new List<string>();
        supplied.Add(value);
        Initialize(name, supplied);
    }

    internal static EmailCalendarParameter CreateImplementationWithNameAndValue(string name, string value)
    {
        return new EmailCalendarParameter(name, value);
    }

    internal static EmailCalendarParameter CreateImplementationWithNameAndValues(string name, List<string> values)
    {
        if (values == null)
            throw new ArgumentNullException("values");
        string first = values.Count == 0 ? "" : values[0];
        EmailCalendarParameter value = new EmailCalendarParameter(name, first);
        value.Initialize(name, values);
        return value;
    }

    void Initialize(string suppliedName, List<string> suppliedValues)
    {
        if (!EmailCalendarSyntax.IsName(suppliedName))
            throw new ArgumentException("A calendar parameter name must be an iCalendar name token.", "suppliedName");
        if (suppliedValues == null)
            throw new ArgumentNullException("suppliedValues");
        if (suppliedValues.Count == 0)
            throw new ArgumentException("A calendar parameter requires at least one value.", "suppliedValues");
        values = new List<string>();
        int index = 0;
        while (index < suppliedValues.Count)
        {
            string value = suppliedValues[index];
            if (value == null || value.Length == 0)
                throw new ArgumentException("Calendar parameter values must be nonempty.", "suppliedValues");
            if (value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0 || value.IndexOf('\0') >= 0 || value.IndexOf('"') >= 0)
                throw new ArgumentException("A calendar parameter value contains a prohibited character.", "suppliedValues");
            values.Add(value);
            index++;
        }

        name = suppliedName.ToUpperInvariant();
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    public string? GetName()
    {
        return name;
    }

    /// <summary>
    /// Gets the value count.
    /// </summary>
    /// <returns>The current value count.</returns>
    public int GetValueCount()
    {
        return (values ?? throw new InvalidOperationException()).Count;
    }

    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public string GetValueAt(int index)
    {
        if (index < 0 || index >= (values ?? throw new InvalidOperationException()).Count)
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
        return new List<string>((values ?? throw new InvalidOperationException()));
    }
}
