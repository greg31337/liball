using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCalendarParameter"/>.
/// </summary>
public interface IEmailCalendarParameterOperationsContract
{
    /// <summary>Creates a single-valued calendar parameter.</summary>
    /// <param name = "name">The parameter name.</param>
    /// <param name = "value">The parameter value.</param>
    /// <returns>The configured calendar parameter.</returns>
    EmailCalendarParameter CreateWithNameAndValue(string name, string value);
    /// <summary>
    /// Creates a new calendar parameter.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "values">The values used by the operation.</param>
    /// <returns>The configured calendar parameter.</returns>
    EmailCalendarParameter CreateWithNameAndValues(string name, List<string> values);
}
