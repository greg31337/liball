namespace uspto;

internal class ConfiguredPatentsViewClient : IPatentsViewClient
{
    internal ConfiguredClientComponents Components;
    internal PatentsViewPatentOperation Patents;
    internal PatentsViewEntityOperation Entities;
    internal PatentsViewClassificationOperation Classifications;
    internal PatentsViewCitationOperation Citations;
    internal PatentsViewTextOperation Text;
    internal PatentsViewAllPageOperation AllPages;
    internal PatentsViewCapabilityOperation Capability;

    internal ConfiguredPatentsViewClient(ConfiguredClientComponents components)
    {
        Components = components;
        Patents = components.CreateSharedPatentsViewTransport();
        Entities = new PatentsViewEntityOperation(components.Client, components.Options);
        Entities.Transport = Patents;
        Classifications = new PatentsViewClassificationOperation(components.Client, components.Options);
        Classifications.Transport = Patents;
        Citations = new PatentsViewCitationOperation(components.Client, components.Options);
        Citations.Transport = Patents;
        Text = new PatentsViewTextOperation(components.Client, components.Options);
        Text.Transport = Patents;
        AllPages = new PatentsViewAllPageOperation(components.Client, components.Options);
        AllPages.PatentOperation = Patents;
        AllPages.EntityOperation = Entities;
        AllPages.ClassificationOperation = Classifications;
        AllPages.CitationOperation = Citations;
        AllPages.TextOperation = Text;
        Capability = components.CreateSharedPatentsViewCapability();
    }

    public PatentsViewCapabilityResult ProbePatentsViewCapability(RequestContext context)
    {
        return Capability.Probe(context);
    }

    public PatentsViewPatentResponse QueryPatentsViewPatents(PatentsViewQueryRequest request)
    {
        return AllPages.QueryPatentsAll(request, true);
    }

    public PatentsViewPatent GetPatentsViewPatent(string patentIdentifier, RequestContext context)
    {
        return Patents.GetPatent(patentIdentifier, context);
    }

    public PatentsViewPublicationResponse QueryPatentsViewPublications(PatentsViewQueryRequest request)
    {
        return AllPages.QueryPublicationsAll(request, true);
    }

    public PatentsViewPublication GetPatentsViewPublication(string publicationIdentifier, RequestContext context)
    {
        return Patents.GetPublication(publicationIdentifier, context);
    }

    public PatentsViewInventorResponse QueryPatentsViewInventors(PatentsViewQueryRequest request)
    {
        return AllPages.QueryInventorsAll(request, true);
    }

    public PatentsViewInventor GetPatentsViewInventor(string inventorIdentifier, RequestContext context)
    {
        return Entities.GetInventor(inventorIdentifier, context);
    }

    public PatentsViewAssigneeResponse QueryPatentsViewAssignees(PatentsViewQueryRequest request)
    {
        return AllPages.QueryAssigneesAll(request, true);
    }

    public PatentsViewAssignee GetPatentsViewAssignee(string assigneeIdentifier, RequestContext context)
    {
        return Entities.GetAssignee(assigneeIdentifier, context);
    }

    public PatentsViewLocationResponse QueryPatentsViewLocations(PatentsViewQueryRequest request)
    {
        return AllPages.QueryLocationsAll(request, true);
    }

    public PatentsViewLocation GetPatentsViewLocation(string locationIdentifier, RequestContext context)
    {
        return Entities.GetLocation(locationIdentifier, context);
    }

    public PatentsViewAttorneyResponse QueryPatentsViewAttorneys(PatentsViewQueryRequest request)
    {
        return AllPages.QueryAttorneysAll(request, true);
    }

    public PatentsViewAttorney GetPatentsViewAttorney(string attorneyIdentifier, RequestContext context)
    {
        return Entities.GetAttorney(attorneyIdentifier, context);
    }

    public PatentsViewCpcClassificationResponse QueryPatentsViewCpcClassifications(PatentsViewQueryRequest request)
    {
        return AllPages.QueryCpcClassesAll(request, true);
    }

    public PatentsViewCpcClassification GetPatentsViewCpcClassification(string classificationIdentifier, RequestContext context)
    {
        return Classifications.GetCpcClass(classificationIdentifier, context);
    }

    public PatentsViewIpcClassificationResponse QueryPatentsViewIpcClassifications(PatentsViewQueryRequest request)
    {
        return AllPages.QueryIpcAll(request, true);
    }

    public PatentsViewIpcClassification GetPatentsViewIpcClassification(string classificationIdentifier, RequestContext context)
    {
        return Classifications.GetIpc(classificationIdentifier, context);
    }

    public PatentsViewUspcClassificationResponse QueryPatentsViewUspcClassifications(PatentsViewQueryRequest request)
    {
        return AllPages.QueryUspcMainClassesAll(request, true);
    }

    public PatentsViewUspcClassification GetPatentsViewUspcClassification(string classificationIdentifier, RequestContext context)
    {
        return Classifications.GetUspcMainClass(classificationIdentifier, context);
    }

    public PatentsViewWipoClassificationResponse QueryPatentsViewWipoClassifications(PatentsViewQueryRequest request)
    {
        return AllPages.QueryWipoAll(request, true);
    }

    public PatentsViewWipoClassification GetPatentsViewWipoClassification(string classificationIdentifier, RequestContext context)
    {
        return Classifications.GetWipo(classificationIdentifier, context);
    }

    public PatentsViewCitationResponse QueryPatentsViewCitations(PatentsViewQueryRequest request)
    {
        return AllPages.QueryUsPatentCitationsAll(request, true);
    }

    public PatentsViewCitationResponse GetPatentsViewCitations(string identifier, RequestContext context)
    {
        return Citations.GetUsPatentCitations(identifier, context);
    }

    public PatentsViewRelatedApplicationResponse QueryPatentsViewRelatedApplications(PatentsViewQueryRequest request)
    {
        return AllPages.QueryPatentRelatedApplicationsAll(request, true);
    }

    public PatentsViewRelatedApplicationResponse GetPatentsViewRelatedApplications(string identifier, RequestContext context)
    {
        return Citations.GetPatentRelatedApplications(identifier, context);
    }

    public PatentsViewClaimResponse QueryPatentsViewClaims(PatentsViewQueryRequest request)
    {
        return AllPages.QueryGrantClaimsAll(request, true);
    }

    public PatentsViewClaimResponse GetPatentsViewClaims(string identifier, RequestContext context)
    {
        return Text.GetGrantClaims(identifier, context);
    }

    public PatentsViewSummaryResponse QueryPatentsViewSummaries(PatentsViewQueryRequest request)
    {
        return AllPages.QueryGrantSummariesAll(request, true);
    }

    public PatentsViewSummary GetPatentsViewSummary(string identifier, RequestContext context)
    {
        return Text.GetGrantSummary(identifier, context);
    }

    public PatentsViewDescriptionResponse QueryPatentsViewDescriptions(PatentsViewQueryRequest request)
    {
        return AllPages.QueryGrantDescriptionsAll(request, true);
    }

    public PatentsViewDescription GetPatentsViewDescription(string identifier, RequestContext context)
    {
        return Text.GetGrantDescription(identifier, context);
    }

    public PatentsViewDrawingDescriptionResponse QueryPatentsViewDrawingDescriptions(PatentsViewQueryRequest request)
    {
        return AllPages.QueryGrantDrawingDescriptionsAll(request, true);
    }

    public PatentsViewDrawingDescriptionResponse GetPatentsViewDrawingDescription(string identifier, RequestContext context)
    {
        return Text.GetGrantDrawingDescriptions(identifier, context);
    }
}
