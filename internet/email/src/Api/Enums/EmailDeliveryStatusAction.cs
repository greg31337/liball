using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Specifies the supported delivery status values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailDeliveryStatusAction
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the failed choice or state.
    /// </summary>
    Failed = 1,
    /// <summary>
    /// Represents the delayed choice or state.
    /// </summary>
    Delayed = 2,
    /// <summary>
    /// Represents the delivered choice or state.
    /// </summary>
    Delivered = 3,
    /// <summary>
    /// Represents the relayed choice or state.
    /// </summary>
    Relayed = 4,
    /// <summary>
    /// Represents the expanded choice or state.
    /// </summary>
    Expanded = 5
}
