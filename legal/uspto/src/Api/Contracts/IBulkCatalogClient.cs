namespace uspto;

/// <summary>
/// Provides read-only discovery of USPTO bulk products and files.
/// </summary>
public interface IBulkCatalogClient
{
    /// <summary>Searches one page of bulk products.</summary>
    /// <param name="request">The search request.</param>
    /// <returns>The typed product page and catalog facets.</returns>
    BulkProductResponse SearchBulkProducts(SearchRequest request);

    /// <summary>Searches bounded bulk product pages.</summary>
    /// <param name="request">The bounded search request.</param>
    /// <returns>The typed bounded product aggregate and catalog facets.</returns>
    BulkProductResponse GetAllBulkProducts(SearchRequest request);

    /// <summary>Gets one exact bulk product.</summary>
    /// <param name="productIdentifier">The catalog product identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed catalog product and included file records.</returns>
    BulkProduct GetBulkProduct(string productIdentifier, RequestContext context);

    /// <summary>Gets exact metadata for one bulk product file.</summary>
    /// <param name="productIdentifier">The catalog product identifier.</param>
    /// <param name="fileName">The exact catalog filename.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed file metadata and credential-free download location.</returns>
    BulkProductFile GetBulkFileMetadata(string productIdentifier, string fileName, RequestContext context);

    /// <summary>Gets dynamic product presets for selected dataset families.</summary>
    /// <param name="families">The selected dataset families.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The currently available products grouped by requested family.</returns>
    BulkDatasetPreset[] GetDatasetPresets(DatasetFamily[] families, RequestContext context);
}
