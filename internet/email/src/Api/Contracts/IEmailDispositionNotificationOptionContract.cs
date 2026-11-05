using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDispositionNotificationOption"/>.
/// </summary>
public interface IEmailDispositionNotificationOptionContract
{
    /// <summary>
    /// Gets the attribute.
    /// </summary>
    /// <returns>The current attribute.</returns>
    string GetAttribute();
    /// <summary>
    /// Gets the importance.
    /// </summary>
    /// <returns>The current importance.</returns>
    EmailDispositionNotificationOptionImportance GetImportance();
    /// <summary>
    /// Gets the value count.
    /// </summary>
    /// <returns>The current value count.</returns>
    int GetValueCount();
    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    string GetValueAt(int index);
    /// <summary>
    /// Gets the values.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current values.</returns>
    List<string> GetValues();
}
