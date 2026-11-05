using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCalendarProperty"/>.
/// </summary>
public interface IEmailCalendarPropertyOperationsContract
{
    /// <summary>Creates a calendar property without parameters.</summary>
    /// <param name = "name">The property name.</param>
    /// <param name = "rawValue">The raw property value.</param>
    /// <returns>The configured calendar property.</returns>
    EmailCalendarProperty CreateWithNameAndRawValue(string name, string rawValue);
    /// <summary>
    /// Creates a new calendar property.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "parameters">The parameters used by the operation.</param>
    /// <param name = "rawValue">The raw value used by the operation.</param>
    /// <returns>The configured calendar property.</returns>
    EmailCalendarProperty CreateWithNameAndParametersAndRawValue(string name, List<EmailCalendarParameter> parameters, string rawValue);
    /// <summary>
    /// Creates a calendar property with parameters and a source position.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "parameters">The parameters used by the operation.</param>
    /// <param name = "rawValue">The raw value used by the operation.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <returns>The configured calendar property.</returns>
    EmailCalendarProperty CreateWithNameAndParametersAndRawValueAndSourcePosition(string name, List<EmailCalendarParameter> parameters, string rawValue, EmailSourcePosition sourcePosition);
    /// <summary>
    /// Creates a text configured for the requested behavior.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The calendar property produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailCalendarProperty CreateText(string name, string value);
}
