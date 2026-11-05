namespace ttf;

/// <summary>Deduplicates complete finalized PDF font resources by stable key.</summary>
/// <remarks>
/// The collection is document-local and preserves first-registration order.
/// Resources from one face remain distinct when policies, writing mode,
/// allocation, mappings, or generated output differ.
/// </remarks>
public class PdfFontResourceCollection : IPdfFontResourceCollection
{
    System.Collections.Generic.Dictionary<string, PdfFontResource> byKey;
    System.Collections.Generic.List<PdfFontResource> resources;
    object collectionSync;

    /// <summary>Creates an empty document-local finalized resource collection.</summary>
    public PdfFontResourceCollection()
    {
        byKey =
            new System.Collections.Generic.Dictionary<string, PdfFontResource>(
                System.StringComparer.Ordinal);
        resources =
            new System.Collections.Generic.List<PdfFontResource>();
        collectionSync = new object();
    }

    /// <summary>Adds a resource or returns the equal resource already registered.</summary>
    /// <param name="resource">A successfully finalized font resource.</param>
    /// <returns>The first resource registered for the deterministic key.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>resource</c> is null.
    /// </exception>
    public PdfFontResource GetOrAdd(PdfFontResource resource)
    {
        if (resource == null)
        {
            throw new System.ArgumentNullException("resource");
        }

        lock (collectionSync)
        {
            string key = resource.GetDeduplicationKey();

            if (byKey.ContainsKey(key))
            {
                return byKey[key];
            }

            byKey.Add(key, resource);
            resources.Add(resource);
            return resource;
        }
    }

    /// <summary>Gets the number of distinct finalized resources.</summary>
    /// <returns>The number of unique resource keys.</returns>
    public int GetCount()
    {
        lock (collectionSync)
        {
            return resources.Count;
        }
    }

    /// <summary>Gets one distinct resource in first-registration order.</summary>
    /// <param name="index">The zero-based resource index.</param>
    /// <returns>The registered finalized resource.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the collection.
    /// </exception>
    public PdfFontResource GetResource(int index)
    {
        lock (collectionSync)
        {
            if (index < 0 || index >= resources.Count)
            {
                throw new System.ArgumentOutOfRangeException("index");
            }

            return resources[index];
        }
    }

    /// <summary>Finds a registered resource by deterministic key.</summary>
    /// <param name="deduplicationKey">The complete versioned resource key.</param>
    /// <returns>The registered resource, or null when it is absent.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>deduplicationKey</c> is null.
    /// </exception>
    public PdfFontResource? Find(string deduplicationKey)
    {
        if (deduplicationKey == null)
        {
            throw new System.ArgumentNullException("deduplicationKey");
        }

        lock (collectionSync)
        {
            if (byKey.ContainsKey(deduplicationKey))
            {
                return byKey[deduplicationKey];
            }

            return null;
        }
    }
}
