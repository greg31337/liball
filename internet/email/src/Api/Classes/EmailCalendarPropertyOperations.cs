using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailCalendarProperty"/>.
/// </summary>
public class EmailCalendarPropertyOperations : IEmailCalendarPropertyOperationsContract
{
    /// <summary>Creates a calendar property without parameters.</summary>
    /// <param name = "name">The property name.</param>
    /// <param name = "rawValue">The raw property value.</param>
    /// <returns>The configured calendar property.</returns>
    public EmailCalendarProperty CreateWithNameAndRawValue(string name, string rawValue)
    {
        return EmailCalendarProperty.CreateImplementationWithNameAndRawValue(name, rawValue);
    }

    /// <summary>
    /// Creates a new calendar property.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "parameters">The parameters used by the operation.</param>
    /// <param name = "rawValue">The raw value used by the operation.</param>
    /// <returns>The configured calendar property.</returns>
    public EmailCalendarProperty CreateWithNameAndParametersAndRawValue(string name, List<EmailCalendarParameter> parameters, string rawValue)
    {
        return EmailCalendarProperty.CreateImplementationWithNameAndParametersAndRawValue(name, parameters, rawValue);
    }

    /// <summary>
    /// Creates a calendar property with parameters and a source position.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "parameters">The parameters used by the operation.</param>
    /// <param name = "rawValue">The raw value used by the operation.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <returns>The configured calendar property.</returns>
    public EmailCalendarProperty CreateWithNameAndParametersAndRawValueAndSourcePosition(string name, List<EmailCalendarParameter> parameters, string rawValue, EmailSourcePosition sourcePosition)
    {
        return EmailCalendarProperty.CreateImplementationWithNameAndParametersAndRawValueAndSourcePosition(name, parameters, rawValue, sourcePosition);
    }

    /// <summary>
    /// Creates a text configured for the requested behavior.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The calendar property produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCalendarProperty CreateText(string name, string value)
    {
        return EmailCalendarProperty.CreateTextImplementation(name, value);
    }
}
