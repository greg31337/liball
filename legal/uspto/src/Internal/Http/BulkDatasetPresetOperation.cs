namespace uspto;

internal class BulkDatasetPresetOperation : IBulkDatasetPresetSource
{
    internal BulkCatalogSearchOperation CatalogOperation;

    internal BulkDatasetPresetOperation(BulkCatalogSearchOperation catalogOperation)
    {
        if (catalogOperation == null)
        {
            throw new ValidationException("A bulk catalog search operation is required.", "catalogOperation", "REQUIRED", null);
        }

        CatalogOperation = catalogOperation;
    }

    internal BulkDatasetPreset[] GetPresets(DatasetFamily[] families, RequestContext context)
    {
        BulkDatasetPresetResolver.ValidatePresetFamilies(families);
        if (context == null)
        {
            throw new ValidationException("A dataset preset request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        if (families.Length == 0)
        {
            return new BulkDatasetPreset[0];
        }

        SearchRequest request = new SearchRequest();
        request.Sorts = new SearchSort[]
        {
            new SearchSort
            {
                Field = "productIdentifier",
                Direction = SortDirection.Ascending
            }
        };
        request.OffsetPage.Limit = UsptoDefaults.PageSize;
        request.MaximumPages = CatalogOperation.Options.Limits.MaximumPages;
        request.MaximumResults = CatalogOperation.Options.Limits.MaximumResults;
        request.Context = PatentApplicationSearchOperation.CreateRequestContext(context);
        BulkProductResponse catalog = CatalogOperation.SearchAll(request);
        return BulkDatasetPresetResolver.ResolvePresets(families, catalog.Products, catalog.Provenance);
    }

    public BulkDatasetPreset[] GetBulkDatasetPresets(DatasetFamily[] families, RequestContext context)
    {
        return GetPresets(families, context);
    }
}
