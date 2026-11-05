using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMediaType"/>.
/// </summary>
public interface IEmailMediaTypeContract
{
    /// <summary>
    /// Gets the top level type.
    /// </summary>
    /// <returns>The current top level type.</returns>
    string GetTopLevelType();
    /// <summary>
    /// Gets the subtype.
    /// </summary>
    /// <returns>The current subtype.</returns>
    string GetSubtype();
    /// <summary>
    /// Gets the normalized top level type.
    /// </summary>
    /// <returns>The current normalized top level type.</returns>
    string GetNormalizedTopLevelType();
    /// <summary>
    /// Gets the normalized subtype.
    /// </summary>
    /// <returns>The current normalized subtype.</returns>
    string GetNormalizedSubtype();
    /// <summary>
    /// Determines whether the current value or state is type.
    /// </summary>
    /// <param name = "expectedType">The expected type used by the operation.</param>
    /// <param name = "expectedSubtype">The expected subtype used by the operation.</param>
    /// <returns><c>true</c> when the value or state is type; otherwise, <c>false</c>.</returns>
    bool IsType(string? expectedType, string? expectedSubtype);
    /// <summary>
    /// Determines whether the current value or state is extension type.
    /// </summary>
    /// <returns><c>true</c> when the value or state is extension type; otherwise, <c>false</c>.</returns>
    bool IsExtensionType();
    /// <summary>
    /// Determines whether the current value or state is private type.
    /// </summary>
    /// <returns><c>true</c> when the value or state is private type; otherwise, <c>false</c>.</returns>
    bool IsPrivateType();
    /// <summary>
    /// Determines whether the current value or state is private subtype.
    /// </summary>
    /// <returns><c>true</c> when the value or state is private subtype; otherwise, <c>false</c>.</returns>
    bool IsPrivateSubtype();
    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <returns>The current parameters.</returns>
    EmailParameterList GetParameters();
    /// <summary>
    /// Determines whether the operation or source value was defaulted.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasDefaulted();
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition GetSourcePosition();
}
