using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCalendarProperty"/>.
/// </summary>
public interface IEmailCalendarPropertyContract
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    string? GetName();
    /// <summary>
    /// Gets the raw value.
    /// </summary>
    /// <returns>The current raw value.</returns>
    string? GetRawValue();
    /// <summary>
    /// Gets the text value.
    /// </summary>
    /// <returns>The current text value.</returns>
    string GetTextValue();
    /// <summary>
    /// Determines whether the source position is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source position is available; otherwise, <c>false</c>.</returns>
    bool HasSourcePosition();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition? GetSourcePosition();
    /// <summary>
    /// Gets the parameter count.
    /// </summary>
    /// <returns>The current parameter count.</returns>
    int GetParameterCount();
    /// <summary>
    /// Gets the parameter at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested parameter.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailCalendarParameter GetParameterAt(int index);
    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current parameters.</returns>
    List<EmailCalendarParameter> GetParameters();
    /// <summary>
    /// Gets the first parameter.
    /// </summary>
    /// <param name = "parameterName">The parameter name used by the operation.</param>
    /// <returns>The current first parameter.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailCalendarParameter? GetFirstParameter(string parameterName);
    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "parameterName">The parameter name used by the operation.</param>
    /// <returns>The current parameters.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    List<EmailCalendarParameter> GetParametersNamed(string parameterName);
}
