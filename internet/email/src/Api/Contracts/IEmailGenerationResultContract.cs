using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailGenerationResult"/>.
/// </summary>
public interface IEmailGenerationResultContract
{
    /// <summary>
    /// Determines whether the message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message is available; otherwise, <c>false</c>.</returns>
    bool HasMessage();
    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <returns>The current message.</returns>
    EmailMessage? GetMessage();
    /// <summary>
    /// Determines whether the entity is present or available.
    /// </summary>
    /// <returns><c>true</c> when the entity is available; otherwise, <c>false</c>.</returns>
    bool HasEntity();
    /// <summary>
    /// Gets the entity.
    /// </summary>
    /// <returns>The current entity.</returns>
    MimeEntity? GetEntity();
    /// <summary>
    /// Determines whether the generated values is present or available.
    /// </summary>
    /// <returns><c>true</c> when the generated values is available; otherwise, <c>false</c>.</returns>
    bool HasGeneratedValues();
    /// <summary>
    /// Gets the generated value count.
    /// </summary>
    /// <returns>The current generated value count.</returns>
    int GetGeneratedValueCount();
    /// <summary>
    /// Gets the generated value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested generated value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailGeneratedValue GetGeneratedValueAt(int index);
    /// <summary>
    /// Gets the generated values.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current generated values.</returns>
    List<EmailGeneratedValue> GetGeneratedValues();
}
