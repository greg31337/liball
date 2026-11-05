using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a generation operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailGenerationResult : IEmailGenerationResultContract
{
    EmailMessage? message;
    MimeEntity? entity;
    List<EmailGeneratedValue> generatedValues;
    /// <summary>
    /// Creates a new generation result.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "generatedValues">The generated values used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailGenerationResult(EmailMessage? message, MimeEntity? entity, List<EmailGeneratedValue> generatedValues)
    {
        if ((message == null) == (entity == null))
        {
            throw new ArgumentException("A generation result requires exactly one generated model.", "message");
        }

        if (generatedValues == null)
        {
            throw new ArgumentNullException("generatedValues");
        }

        this.message = message;
        this.entity = entity;
        this.generatedValues = new List<EmailGeneratedValue>(generatedValues);
    }

    /// <summary>
    /// Determines whether the message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message is available; otherwise, <c>false</c>.</returns>
    public bool HasMessage()
    {
        return message != null;
    }

    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <returns>The current message.</returns>
    public EmailMessage? GetMessage()
    {
        return message;
    }

    /// <summary>
    /// Determines whether the entity is present or available.
    /// </summary>
    /// <returns><c>true</c> when the entity is available; otherwise, <c>false</c>.</returns>
    public bool HasEntity()
    {
        return entity != null;
    }

    /// <summary>
    /// Gets the entity.
    /// </summary>
    /// <returns>The current entity.</returns>
    public MimeEntity? GetEntity()
    {
        return entity;
    }

    /// <summary>
    /// Determines whether the generated values is present or available.
    /// </summary>
    /// <returns><c>true</c> when the generated values is available; otherwise, <c>false</c>.</returns>
    public bool HasGeneratedValues()
    {
        return generatedValues.Count > 0;
    }

    /// <summary>
    /// Gets the generated value count.
    /// </summary>
    /// <returns>The current generated value count.</returns>
    public int GetGeneratedValueCount()
    {
        return generatedValues.Count;
    }

    /// <summary>
    /// Gets the generated value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested generated value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailGeneratedValue GetGeneratedValueAt(int index)
    {
        if (index < 0 || index >= generatedValues.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return generatedValues[index];
    }

    /// <summary>
    /// Gets the generated values.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current generated values.</returns>
    public List<EmailGeneratedValue> GetGeneratedValues()
    {
        return new List<EmailGeneratedValue>(generatedValues);
    }
}
