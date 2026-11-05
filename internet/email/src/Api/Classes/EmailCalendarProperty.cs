using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a calendar property.
/// </summary>
public class EmailCalendarProperty : IEmailCalendarPropertyContract
{
    string? name;
    List<EmailCalendarParameter>? parameters;
    string? rawValue;
    EmailSourcePosition? sourcePosition;
    /// <summary>
    /// Creates a calendar property without parameters.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "rawValue">The raw value used by the operation.</param>
    public EmailCalendarProperty(string name, string rawValue)
    {
        Initialize(name, new List<EmailCalendarParameter>(), rawValue, null);
    }

    internal static EmailCalendarProperty CreateImplementationWithNameAndRawValue(string name, string rawValue)
    {
        return new EmailCalendarProperty(name, rawValue);
    }

    internal static EmailCalendarProperty CreateImplementationWithNameAndParametersAndRawValue(string name, List<EmailCalendarParameter> parameters, string rawValue)
    {
        EmailCalendarProperty value = new EmailCalendarProperty(name, rawValue);
        value.Initialize(name, parameters, rawValue, null);
        return value;
    }

    internal static EmailCalendarProperty CreateImplementationWithNameAndParametersAndRawValueAndSourcePosition(string name, List<EmailCalendarParameter> parameters, string rawValue, EmailSourcePosition sourcePosition)
    {
        EmailCalendarProperty value = new EmailCalendarProperty(name, rawValue);
        value.Initialize(name, parameters, rawValue, sourcePosition);
        return value;
    }

    void Initialize(string suppliedName, List<EmailCalendarParameter> suppliedParameters, string suppliedRawValue, EmailSourcePosition? suppliedSourcePosition)
    {
        if (!EmailCalendarSyntax.IsName(suppliedName))
            throw new ArgumentException("A calendar property name must be an iCalendar name token.", "suppliedName");
        if (suppliedParameters == null)
            throw new ArgumentNullException("suppliedParameters");
        if (suppliedRawValue == null)
            throw new ArgumentNullException("suppliedRawValue");
        if (suppliedRawValue.IndexOf('\r') >= 0 || suppliedRawValue.IndexOf('\n') >= 0 || suppliedRawValue.IndexOf('\0') >= 0)
            throw new ArgumentException("A raw calendar property value cannot contain line breaks or NUL.", "suppliedRawValue");
        parameters = new List<EmailCalendarParameter>();
        int index = 0;
        while (index < suppliedParameters.Count)
        {
            if (suppliedParameters[index] == null)
                throw new ArgumentException("Calendar property parameters cannot contain null entries.", "suppliedParameters");
            parameters.Add(suppliedParameters[index]);
            index++;
        }

        name = suppliedName.ToUpperInvariant();
        rawValue = suppliedRawValue;
        sourcePosition = suppliedSourcePosition;
    }

    internal static EmailCalendarProperty CreateTextImplementation(string name, string value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        return new EmailCalendarProperty(name, EmailCalendarSyntax.EscapeText(value));
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
    /// Gets the raw value.
    /// </summary>
    /// <returns>The current raw value.</returns>
    public string? GetRawValue()
    {
        return rawValue;
    }

    /// <summary>
    /// Gets the text value.
    /// </summary>
    /// <returns>The current text value.</returns>
    public string GetTextValue()
    {
        return EmailCalendarSyntax.UnescapeText(rawValue);
    }

    /// <summary>
    /// Determines whether the source position is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source position is available; otherwise, <c>false</c>.</returns>
    public bool HasSourcePosition()
    {
        return sourcePosition != null;
    }

    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition? GetSourcePosition()
    {
        return sourcePosition;
    }

    /// <summary>
    /// Gets the parameter count.
    /// </summary>
    /// <returns>The current parameter count.</returns>
    public int GetParameterCount()
    {
        return (parameters ?? throw new InvalidOperationException()).Count;
    }

    /// <summary>
    /// Gets the parameter at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested parameter.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailCalendarParameter GetParameterAt(int index)
    {
        if (index < 0 || index >= (parameters ?? throw new InvalidOperationException()).Count)
            throw new ArgumentOutOfRangeException("index");
        return parameters[index];
    }

    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current parameters.</returns>
    public List<EmailCalendarParameter> GetParameters()
    {
        return new List<EmailCalendarParameter>((parameters ?? throw new InvalidOperationException()));
    }

    /// <summary>
    /// Gets the first parameter.
    /// </summary>
    /// <param name = "parameterName">The parameter name used by the operation.</param>
    /// <returns>The current first parameter.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCalendarParameter? GetFirstParameter(string parameterName)
    {
        if (parameterName == null)
            throw new ArgumentNullException("parameterName");
        int index = 0;
        while (index < (parameters ?? throw new InvalidOperationException()).Count)
        {
            if (string.Equals(parameters[index].GetName(), parameterName, StringComparison.OrdinalIgnoreCase))
                return parameters[index];
            index++;
        }

        return null;
    }

    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "parameterName">The parameter name used by the operation.</param>
    /// <returns>The current parameters.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public List<EmailCalendarParameter> GetParametersNamed(string parameterName)
    {
        if (parameterName == null)
            throw new ArgumentNullException("parameterName");
        List<EmailCalendarParameter> result = new List<EmailCalendarParameter>();
        int index = 0;
        while (index < (parameters ?? throw new InvalidOperationException()).Count)
        {
            if (string.Equals(parameters[index].GetName(), parameterName, StringComparison.OrdinalIgnoreCase))
                result.Add(parameters[index]);
            index++;
        }

        return result;
    }
}
