using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDispositionNotificationTypedValue"/>.
/// </summary>
public interface IEmailDispositionNotificationTypedValueContract
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
