namespace uspto;

/// <summary>
/// Provides capability-detected read-only access to PatentsView PatentSearch data.
/// </summary>
public interface IPatentsViewClient
{
    /// <summary>Probes whether the configured PatentSearch service is available.</summary>
    /// <param name="context">The request context.</param>
    /// <returns>A credential-free capability description.</returns>
    PatentsViewCapabilityResult ProbePatentsViewCapability(RequestContext context);

    /// <summary>Queries PatentsView patent records.</summary>
    /// <param name="request">The cursor-capable typed query request.</param>
    /// <returns>The typed patent records and response metadata.</returns>
    PatentsViewPatentResponse QueryPatentsViewPatents(PatentsViewQueryRequest request);

    /// <summary>Gets one PatentsView patent record.</summary>
    /// <param name="patentIdentifier">The patent identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed research-derived patent record.</returns>
    PatentsViewPatent GetPatentsViewPatent(string patentIdentifier, RequestContext context);

    /// <summary>Queries PatentsView pre-grant publication records.</summary>
    /// <param name="request">The cursor-capable typed query request.</param>
    /// <returns>The typed publication records and response metadata.</returns>
    PatentsViewPublicationResponse QueryPatentsViewPublications(PatentsViewQueryRequest request);

    /// <summary>Gets one PatentsView pre-grant publication record.</summary>
    /// <param name="publicationIdentifier">The publication identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed research-derived publication record.</returns>
    PatentsViewPublication GetPatentsViewPublication(string publicationIdentifier, RequestContext context);

    /// <summary>Queries PatentsView inventor records.</summary>
    /// <param name="request">The cursor-capable typed query request.</param>
    /// <returns>The typed inventor records and response metadata.</returns>
    PatentsViewInventorResponse QueryPatentsViewInventors(PatentsViewQueryRequest request);

    /// <summary>Gets one PatentsView inventor record.</summary>
    /// <param name="inventorIdentifier">The PatentsView inventor identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed disambiguated inventor record.</returns>
    PatentsViewInventor GetPatentsViewInventor(string inventorIdentifier, RequestContext context);

    /// <summary>Queries PatentsView assignee records.</summary>
    /// <param name="request">The cursor-capable typed query request.</param>
    /// <returns>The typed assignee records and response metadata.</returns>
    PatentsViewAssigneeResponse QueryPatentsViewAssignees(PatentsViewQueryRequest request);

    /// <summary>Gets one PatentsView assignee record.</summary>
    /// <param name="assigneeIdentifier">The PatentsView assignee identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed disambiguated assignee record.</returns>
    PatentsViewAssignee GetPatentsViewAssignee(string assigneeIdentifier, RequestContext context);

    /// <summary>Queries PatentsView location records.</summary>
    /// <param name="request">The cursor-capable typed query request.</param>
    /// <returns>The typed location records and response metadata.</returns>
    PatentsViewLocationResponse QueryPatentsViewLocations(PatentsViewQueryRequest request);

    /// <summary>Gets one PatentsView location record.</summary>
    /// <param name="locationIdentifier">The PatentsView location identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed disambiguated location record.</returns>
    PatentsViewLocation GetPatentsViewLocation(string locationIdentifier, RequestContext context);

    /// <summary>Queries PatentsView patent attorney records.</summary>
    /// <param name="request">The cursor-capable typed query request.</param>
    /// <returns>The typed patent attorney records and response metadata.</returns>
    PatentsViewAttorneyResponse QueryPatentsViewAttorneys(PatentsViewQueryRequest request);

    /// <summary>Gets one PatentsView patent attorney record.</summary>
    /// <param name="attorneyIdentifier">The PatentsView attorney identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed disambiguated patent attorney record.</returns>
    PatentsViewAttorney GetPatentsViewAttorney(string attorneyIdentifier, RequestContext context);

    /// <summary>Queries PatentsView CPC classification records.</summary>
    /// <param name="request">The cursor-capable typed query request.</param>
    /// <returns>The typed CPC records and response metadata.</returns>
    PatentsViewCpcClassificationResponse QueryPatentsViewCpcClassifications(PatentsViewQueryRequest request);

    /// <summary>Gets one PatentsView CPC classification record.</summary>
    /// <param name="classificationIdentifier">The CPC classification identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed CPC classification record.</returns>
    PatentsViewCpcClassification GetPatentsViewCpcClassification(string classificationIdentifier, RequestContext context);

    /// <summary>Queries PatentsView IPC classification records.</summary>
    /// <param name="request">The cursor-capable typed query request.</param>
    /// <returns>The typed IPC records and response metadata.</returns>
    PatentsViewIpcClassificationResponse QueryPatentsViewIpcClassifications(PatentsViewQueryRequest request);

    /// <summary>Gets one PatentsView IPC classification record.</summary>
    /// <param name="classificationIdentifier">The IPC classification identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed IPC classification record.</returns>
    PatentsViewIpcClassification GetPatentsViewIpcClassification(string classificationIdentifier, RequestContext context);

    /// <summary>Queries PatentsView USPC classification records.</summary>
    /// <param name="request">The cursor-capable typed query request.</param>
    /// <returns>The typed USPC records and response metadata.</returns>
    PatentsViewUspcClassificationResponse QueryPatentsViewUspcClassifications(PatentsViewQueryRequest request);

    /// <summary>Gets one PatentsView USPC classification record.</summary>
    /// <param name="classificationIdentifier">The USPC classification identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed USPC classification record.</returns>
    PatentsViewUspcClassification GetPatentsViewUspcClassification(string classificationIdentifier, RequestContext context);

    /// <summary>Queries PatentsView WIPO classification records.</summary>
    /// <param name="request">The cursor-capable typed query request.</param>
    /// <returns>The typed WIPO records and response metadata.</returns>
    PatentsViewWipoClassificationResponse QueryPatentsViewWipoClassifications(PatentsViewQueryRequest request);

    /// <summary>Gets one PatentsView WIPO classification record.</summary>
    /// <param name="classificationIdentifier">The WIPO classification identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed WIPO classification record.</returns>
    PatentsViewWipoClassification GetPatentsViewWipoClassification(string classificationIdentifier, RequestContext context);

    /// <summary>Queries PatentsView citation records.</summary>
    /// <param name="request">The cursor-capable typed query request.</param>
    /// <returns>The typed citations and response metadata.</returns>
    PatentsViewCitationResponse QueryPatentsViewCitations(PatentsViewQueryRequest request);

    /// <summary>Gets PatentsView citations associated with one patent or publication.</summary>
    /// <param name="identifier">The patent or publication identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed citations and response metadata.</returns>
    PatentsViewCitationResponse GetPatentsViewCitations(string identifier, RequestContext context);

    /// <summary>Queries PatentsView related-application records.</summary>
    /// <param name="request">The cursor-capable typed query request.</param>
    /// <returns>The typed related-application records and response metadata.</returns>
    PatentsViewRelatedApplicationResponse QueryPatentsViewRelatedApplications(PatentsViewQueryRequest request);

    /// <summary>Gets PatentsView related applications for one patent or publication.</summary>
    /// <param name="identifier">The patent or publication identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed related-application records and response metadata.</returns>
    PatentsViewRelatedApplicationResponse GetPatentsViewRelatedApplications(string identifier, RequestContext context);

    /// <summary>Queries PatentsView claim records.</summary>
    /// <param name="request">The cursor-capable typed query request.</param>
    /// <returns>The typed claims and response metadata.</returns>
    PatentsViewClaimResponse QueryPatentsViewClaims(PatentsViewQueryRequest request);

    /// <summary>Gets PatentsView claims for one patent or publication.</summary>
    /// <param name="identifier">The patent or publication identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed claims and response metadata.</returns>
    PatentsViewClaimResponse GetPatentsViewClaims(string identifier, RequestContext context);

    /// <summary>Queries PatentsView brief summary records.</summary>
    /// <param name="request">The cursor-capable typed query request.</param>
    /// <returns>The typed summaries and response metadata.</returns>
    PatentsViewSummaryResponse QueryPatentsViewSummaries(PatentsViewQueryRequest request);

    /// <summary>Gets the PatentsView brief summary for one patent or publication.</summary>
    /// <param name="identifier">The patent or publication identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed brief-summary record.</returns>
    PatentsViewSummary GetPatentsViewSummary(string identifier, RequestContext context);

    /// <summary>Queries PatentsView detailed description records.</summary>
    /// <param name="request">The cursor-capable typed query request.</param>
    /// <returns>The typed detailed descriptions and response metadata.</returns>
    PatentsViewDescriptionResponse QueryPatentsViewDescriptions(PatentsViewQueryRequest request);

    /// <summary>Gets the PatentsView detailed description for one patent or publication.</summary>
    /// <param name="identifier">The patent or publication identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed detailed-description record.</returns>
    PatentsViewDescription GetPatentsViewDescription(string identifier, RequestContext context);

    /// <summary>Queries PatentsView drawing description records.</summary>
    /// <param name="request">The cursor-capable typed query request.</param>
    /// <returns>The typed drawing descriptions and response metadata.</returns>
    PatentsViewDrawingDescriptionResponse QueryPatentsViewDrawingDescriptions(PatentsViewQueryRequest request);

    /// <summary>Gets the PatentsView drawing description for one patent or publication.</summary>
    /// <param name="identifier">The patent or publication identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed drawing descriptions and response metadata.</returns>
    PatentsViewDrawingDescriptionResponse GetPatentsViewDrawingDescription(string identifier, RequestContext context);
}
