using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAuthenticationProperty"/>.
/// </summary>
public interface IEmailAuthenticationPropertyContract
{
    /// <summary>
    /// Gets the property type.
    /// </summary>
    /// <returns>The current property type.</returns>
    string GetPropertyType();
    /// <summary>
    /// Gets the normalized property type.
    /// </summary>
    /// <returns>The current normalized property type.</returns>
    string GetNormalizedPropertyType();
    /// <summary>
    /// Gets the property.
    /// </summary>
    /// <returns>The current property.</returns>
    string GetProperty();
    /// <summary>
    /// Gets the normalized property.
    /// </summary>
    /// <returns>The current normalized property.</returns>
    string GetNormalizedProperty();
    /// <summary>
    /// Gets the value produced by the operation, or null when no value is available.
    /// </summary>
    /// <returns>The current value.</returns>
    string GetValue();
    /// <summary>
    /// Determines whether the operation or source value was quoted.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasQuoted();
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Gets the raw value bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw value bytes.</returns>
    byte[] GetRawValueBytes();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition GetSourcePosition();
}
