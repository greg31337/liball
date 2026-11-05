using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents a related index.
/// </summary>
public class EmailRelatedIndex : IEmailRelatedIndexContract
{
    Dictionary<string, List<MimeEntity>> contentIds;
    Dictionary<string, List<MimeEntity>> locations;
    List<EmailRelatedDuplicate> duplicates;
    List<EmailRelatedReference> references;
    List<EmailRelatedReference> missingReferences;
    internal EmailRelatedIndex(Dictionary<string, List<MimeEntity>> contentIds, Dictionary<string, List<MimeEntity>> locations, List<EmailRelatedDuplicate> duplicates, List<EmailRelatedReference> references, List<EmailRelatedReference> missingReferences)
    {
        this.contentIds = contentIds;
        this.locations = locations;
        this.duplicates = duplicates;
        this.references = references;
        this.missingReferences = missingReferences;
    }

    internal static EmailRelatedIndex BuildImplementationWithRoot(MimeEntity? root)
    {
        return BuildImplementationWithRootAndOptions(root, new EmailRelatedIndexOptions());
    }

    internal static EmailRelatedIndex BuildImplementationWithRootAndOptions(MimeEntity? root, EmailRelatedIndexOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailRelatedIndexWorker worker = new EmailRelatedIndexWorker(options.Clone());
        return worker.Build(root);
    }

    internal static EmailRelatedIndex BuildImplementationWithMessage(EmailMessage message)
    {
        return BuildImplementationWithRoot(GetMessageRoot(message));
    }

    internal static EmailRelatedIndex BuildImplementationWithMessageAndOptions(EmailMessage message, EmailRelatedIndexOptions options)
    {
        return BuildImplementationWithRootAndOptions(GetMessageRoot(message), options);
    }

    internal static List<string> FindHtmlReferencesImplementation(string html)
    {
        EmailHtmlReferenceScanner scanner = new EmailHtmlReferenceScanner(EmailRelatedIndexOptionsConstants.DefaultMaximumReferences, EmailHtmlLimitsConstants.DefaultMaxUrlCharacters);
        return scanner.Find(html);
    }

    /// <summary>
    /// Resolves the content ID.
    /// </summary>
    /// <param name = "contentId">The content ID used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity? ResolveContentId(string contentId)
    {
        string key = new EmailRelatedUri().NormalizeContentIdWithValue(contentId);
        return GetFirst(contentIds, key);
    }

    /// <summary>
    /// Gets the by content ID.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "contentId">The content ID used by the operation.</param>
    /// <returns>The current by content ID.</returns>
    public List<MimeEntity> GetByContentId(string contentId)
    {
        string key = new EmailRelatedUri().NormalizeContentIdWithValue(contentId);
        return GetAll(contentIds, key);
    }

    /// <summary>
    /// Resolves the location.
    /// </summary>
    /// <param name = "location">The location used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity? ResolveLocation(string location)
    {
        string key = new EmailRelatedUri().NormalizeLocation(location);
        return GetFirst(locations, key);
    }

    /// <summary>
    /// Gets the by location.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "location">The location used by the operation.</param>
    /// <returns>The current by location.</returns>
    public List<MimeEntity> GetByLocation(string location)
    {
        string key = new EmailRelatedUri().NormalizeLocation(location);
        return GetAll(locations, key);
    }

    /// <summary>
    /// Resolves the location.
    /// </summary>
    /// <param name = "location">The location used by the operation.</param>
    /// <param name = "baseLocation">The base location used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity? ResolveLocationAgainstBase(string location, string baseLocation)
    {
        string key = new EmailRelatedUri().ResolveLocation(location, baseLocation);
        return GetFirst(locations, key);
    }

    /// <summary>
    /// Gets the duplicate count.
    /// </summary>
    /// <returns>The current duplicate count.</returns>
    public int GetDuplicateCount()
    {
        return duplicates.Count;
    }

    /// <summary>
    /// Gets the duplicate at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested duplicate.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailRelatedDuplicate GetDuplicateAt(int index)
    {
        if (index < 0 || index >= duplicates.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return duplicates[index];
    }

    /// <summary>
    /// Gets the duplicates.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current duplicates.</returns>
    public List<EmailRelatedDuplicate> GetDuplicates()
    {
        return new List<EmailRelatedDuplicate>(duplicates);
    }

    /// <summary>
    /// Gets the reference count.
    /// </summary>
    /// <returns>The current reference count.</returns>
    public int GetReferenceCount()
    {
        return references.Count;
    }

    /// <summary>
    /// Gets the reference at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested reference.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailRelatedReference GetReferenceAt(int index)
    {
        if (index < 0 || index >= references.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return references[index];
    }

    /// <summary>
    /// Gets the references.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current references.</returns>
    public List<EmailRelatedReference> GetReferences()
    {
        return new List<EmailRelatedReference>(references);
    }

    /// <summary>
    /// Gets the missing references.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current missing references.</returns>
    public List<EmailRelatedReference> GetMissingReferences()
    {
        return new List<EmailRelatedReference>(missingReferences);
    }

    static MimeEntity? GetFirst(Dictionary<string, List<MimeEntity>> values, string key)
    {
        return values.ContainsKey(key) ? values[key][0] : null;
    }

    static List<MimeEntity> GetAll(Dictionary<string, List<MimeEntity>> values, string key)
    {
        return values.ContainsKey(key) ? new List<MimeEntity>(values[key]) : new List<MimeEntity>();
    }

    static MimeEntity? GetMessageRoot(EmailMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (!message.HasBody())
        {
            throw new ArgumentException("A message without a MIME body has no related-resource index.", "message");
        }

        return message.GetBody();
    }
}
