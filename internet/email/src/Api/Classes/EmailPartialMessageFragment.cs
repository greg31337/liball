using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a partial message fragment.
/// </summary>
public class EmailPartialMessageFragment : IEmailPartialMessageFragmentContract
{
    MimeEntity entity;
    EmailHeaders outerHeaders;
    EmailEnvelope envelope;
    int insertionIndex;
    /// <summary>
    /// Creates a new partial message fragment.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "outerHeaders">The outer headers used by the operation.</param>
    /// <param name = "envelope">The envelope used by the operation.</param>
    /// <param name = "insertionIndex">The zero-based insertion index.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailPartialMessageFragment(MimeEntity entity, EmailHeaders outerHeaders, EmailEnvelope? envelope, int insertionIndex)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");
        if (outerHeaders == null)
            throw new ArgumentNullException("outerHeaders");
        this.entity = entity;
        this.outerHeaders = outerHeaders.Clone();
        this.envelope = envelope == null ? new EmailEnvelopeOperations().CreateWith() : envelope.Clone();
        this.insertionIndex = insertionIndex;
    }

    /// <summary>
    /// Gets the entity.
    /// </summary>
    /// <returns>The current entity.</returns>
    public MimeEntity GetEntity()
    {
        return entity;
    }

    /// <summary>
    /// Gets the outer headers.
    /// </summary>
    /// <returns>The current outer headers.</returns>
    public EmailHeaders GetOuterHeaders()
    {
        return outerHeaders.Clone();
    }

    /// <summary>
    /// Gets the envelope.
    /// </summary>
    /// <returns>The current envelope.</returns>
    public EmailEnvelope GetEnvelope()
    {
        return envelope.Clone();
    }

    /// <summary>
    /// Gets the metadata.
    /// </summary>
    /// <returns>The current metadata.</returns>
    public EmailPartialMessageMetadata? GetMetadata()
    {
        return entity.GetPartialMessageMetadata();
    }

    /// <summary>
    /// Gets the fragment ID.
    /// </summary>
    /// <returns>The current fragment ID.</returns>
    public string? GetFragmentId()
    {
        return (GetMetadata() ?? throw new InvalidOperationException()).GetFragmentId();
    }

    /// <summary>
    /// Gets the fragment number.
    /// </summary>
    /// <returns>The current fragment number.</returns>
    public long GetFragmentNumber()
    {
        return (GetMetadata() ?? throw new InvalidOperationException()).GetFragmentNumber();
    }

    /// <summary>
    /// Determines whether the total fragments is present or available.
    /// </summary>
    /// <returns><c>true</c> when the total fragments is available; otherwise, <c>false</c>.</returns>
    public bool HasTotalFragments()
    {
        return (GetMetadata() ?? throw new InvalidOperationException()).HasTotalFragments();
    }

    /// <summary>
    /// Gets the total fragments.
    /// </summary>
    /// <returns>The current total fragments.</returns>
    public long GetTotalFragments()
    {
        return (GetMetadata() ?? throw new InvalidOperationException()).GetTotalFragments();
    }

    /// <summary>
    /// Gets the insertion index.
    /// </summary>
    /// <returns>The current insertion index.</returns>
    public int GetInsertionIndex()
    {
        return insertionIndex;
    }
}
