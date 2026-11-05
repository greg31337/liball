using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHtmlDataUriOptions"/>.
/// </summary>
public interface IEmailHtmlDataUriOptionsContract
{
    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    EmailHtmlLimits GetLimits();
    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetLimits(EmailHtmlLimits value);
    /// <summary>
    /// Gets the allowed media types.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current allowed media types.</returns>
    List<string> GetAllowedMediaTypes();
    /// <summary>
    /// Sets the allowed media types used by this instance.
    /// </summary>
    /// <param name = "values">The values used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetAllowedMediaTypes(List<string> values);
    /// <summary>
    /// Adds an allowed media type to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddAllowedMediaType(string value);
    /// <summary>
    /// Gets the maximum media type characters.
    /// </summary>
    /// <returns>The current maximum media type characters.</returns>
    int GetMaximumMediaTypeCharacters();
    /// <summary>
    /// Sets the maximum media type characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumMediaTypeCharacters(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailHtmlDataUriOptions Clone();
}
