using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailCalendarParameter"/>.
/// </summary>
public class EmailCalendarParameterOperations : IEmailCalendarParameterOperationsContract
{
    /// <summary>Creates a single-valued calendar parameter.</summary>
    /// <param name = "name">The parameter name.</param>
    /// <param name = "value">The parameter value.</param>
    /// <returns>The configured calendar parameter.</returns>
    public EmailCalendarParameter CreateWithNameAndValue(string name, string value)
    {
        return EmailCalendarParameter.CreateImplementationWithNameAndValue(name, value);
    }

    /// <summary>
    /// Creates a new calendar parameter.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "values">The values used by the operation.</param>
    /// <returns>The configured calendar parameter.</returns>
    public EmailCalendarParameter CreateWithNameAndValues(string name, List<string> values)
    {
        return EmailCalendarParameter.CreateImplementationWithNameAndValues(name, values);
    }
}
