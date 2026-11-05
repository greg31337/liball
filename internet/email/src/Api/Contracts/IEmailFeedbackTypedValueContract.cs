using System;
using System.Collections.Generic;
using System.Net;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFeedbackTypedValue"/>.
/// </summary>
public interface IEmailFeedbackTypedValueContract
{
    /// <summary>
    /// Gets the type name.
    /// </summary>
    /// <returns>The current type name.</returns>
    string GetTypeName();
    /// <summary>
    /// Gets the value produced by the operation, or null when no value is available.
    /// </summary>
    /// <returns>The current value.</returns>
    string GetValue();
}
