namespace uspto;

internal static class TrademarkDecisionCapabilityProvider
{
    internal static TrademarkDecisionCapabilityResult GetCapability(RequestContext sourceContext)
    {
        if (sourceContext == null)
        {
            throw new ValidationException("A trademark decision capability context is required.", "context", "REQUIRED", null);
        }

        sourceContext.CancellationToken.ThrowIfCancellationRequested();
        TrademarkDecisionCapabilityResult result = new TrademarkDecisionCapabilityResult();
        result.CapabilityName = "Trademark Decisions and Proceedings";
        result.IsPublishedApiAvailable = false;
        result.UnavailabilityCode = "NO_PUBLISHED_API";
        result.UnavailabilityReason = "The USPTO has not published a stable API for Trademark Decisions and Proceedings, so the browser portal is not an automated data source.";
        result.IsPortalAutomationSupported = false;
        result.IsBulkDiscoveryAvailable = true;
        result.BulkDatasetFamilies = new DatasetFamily[] { DatasetFamily.Ttab };
        result.BulkProductSearch.Query = "Trademark Trial and Appeal Board";
        result.BulkProductSearch.Context = CreateBulkSearchContext(sourceContext);
        result.Provenance = DataProvenance.ClientDerived;
        return result;
    }

    internal static RequestContext CreateBulkSearchContext(RequestContext source)
    {
        RequestContext context = new RequestContext();
        context.Service = ApiService.Odp;
        context.CancellationToken = source.CancellationToken;
        context.RetainRawResponse = source.RetainRawResponse;
        context.MaximumRawResponseBytes = source.MaximumRawResponseBytes;
        context.Deadline = source.Deadline;
        context.HasDeadline = source.HasDeadline;
        context.CorrelationIdentifier = source.CorrelationIdentifier;
        context.Fallback = source.Fallback;
        return context;
    }
}
