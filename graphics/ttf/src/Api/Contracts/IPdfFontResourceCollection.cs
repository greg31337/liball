namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfFontResourceCollection"/>.</summary>
public interface IPdfFontResourceCollection
{
    /// <summary>Adds a resource or returns the equal resource already registered.</summary>
    /// <param name="resource">A successfully finalized font resource.</param>
    /// <returns>The first resource registered for the deterministic key.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>resource</c> is null.
    /// </exception>
    PdfFontResource GetOrAdd(PdfFontResource resource);

    /// <summary>Gets the number of distinct finalized resources.</summary>
    /// <returns>The number of unique resource keys.</returns>
    int GetCount();

    /// <summary>Gets one distinct resource in first-registration order.</summary>
    /// <param name="index">The zero-based resource index.</param>
    /// <returns>The registered finalized resource.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the collection.
    /// </exception>
    PdfFontResource GetResource(int index);

    /// <summary>Finds a registered resource by deterministic key.</summary>
    /// <param name="deduplicationKey">The complete versioned resource key.</param>
    /// <returns>The registered resource, or null when it is absent.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>deduplicationKey</c> is null.
    /// </exception>
    PdfFontResource? Find(string deduplicationKey);
}
