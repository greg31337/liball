using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRelatedIndex"/>.
/// </summary>
public interface IEmailRelatedIndexContract
{
    /// <summary>
    /// Resolves the content ID.
    /// </summary>
    /// <param name = "contentId">The content ID used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity? ResolveContentId(string contentId);
    /// <summary>
    /// Gets the by content ID.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "contentId">The content ID used by the operation.</param>
    /// <returns>The current by content ID.</returns>
    List<MimeEntity> GetByContentId(string contentId);
    /// <summary>
    /// Resolves the location.
    /// </summary>
    /// <param name = "location">The location used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity? ResolveLocation(string location);
    /// <summary>
    /// Gets the by location.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "location">The location used by the operation.</param>
    /// <returns>The current by location.</returns>
    List<MimeEntity> GetByLocation(string location);
    /// <summary>
    /// Resolves the location.
    /// </summary>
    /// <param name = "location">The location used by the operation.</param>
    /// <param name = "baseLocation">The base location used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity? ResolveLocationAgainstBase(string location, string baseLocation);
    /// <summary>
    /// Gets the duplicate count.
    /// </summary>
    /// <returns>The current duplicate count.</returns>
    int GetDuplicateCount();
    /// <summary>
    /// Gets the duplicate at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested duplicate.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailRelatedDuplicate GetDuplicateAt(int index);
    /// <summary>
    /// Gets the duplicates.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current duplicates.</returns>
    List<EmailRelatedDuplicate> GetDuplicates();
    /// <summary>
    /// Gets the reference count.
    /// </summary>
    /// <returns>The current reference count.</returns>
    int GetReferenceCount();
    /// <summary>
    /// Gets the reference at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested reference.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailRelatedReference GetReferenceAt(int index);
    /// <summary>
    /// Gets the references.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current references.</returns>
    List<EmailRelatedReference> GetReferences();
    /// <summary>
    /// Gets the missing references.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current missing references.</returns>
    List<EmailRelatedReference> GetMissingReferences();
}
