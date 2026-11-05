namespace uspto;

internal enum PatentsViewQueryEndpoint
{
    Patent = 0,
    Publication = 1,
    Inventor = 2,
    Assignee = 3,
    Location = 4,
    Attorney = 5,
    CpcClass = 6,
    CpcSubclass = 7,
    CpcGroup = 8,
    Ipc = 9,
    UspcMainClass = 10,
    UspcSubclass = 11,
    Wipo = 12,
    UsPatentCitation = 13,
    UsApplicationCitation = 14,
    ForeignCitation = 15,
    OtherReference = 16,
    PatentRelatedApplication = 17,
    PublicationRelatedApplication = 18,
    GrantSummary = 19,
    PublicationSummary = 20,
    GrantClaim = 21,
    PublicationClaim = 22,
    GrantDescription = 23,
    PublicationDescription = 24,
    GrantDrawingDescription = 25,
    PublicationDrawingDescription = 26
}

internal class PatentsViewAllPageOperation
{
    internal PatentsViewPatentOperation PatentOperation;
    internal PatentsViewEntityOperation EntityOperation;
    internal PatentsViewClassificationOperation ClassificationOperation;
    internal PatentsViewCitationOperation CitationOperation;
    internal PatentsViewTextOperation TextOperation;
    internal UsptoClientOptions Options;

    internal PatentsViewAllPageOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        if (options == null)
        {
            throw new ValidationException("Client options are required.", "options", "REQUIRED", null);
        }

        Options = options;
        PatentOperation = new PatentsViewPatentOperation(client, options);
        EntityOperation = new PatentsViewEntityOperation(client, options);
        ClassificationOperation = new PatentsViewClassificationOperation(client, options);
        CitationOperation = new PatentsViewCitationOperation(client, options);
        TextOperation = new PatentsViewTextOperation(client, options);
    }

    internal PatentsViewPatentResponse QueryPatentsAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewPatentResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.Patent, new string[] { "patent_id" });
    }

    internal PatentsViewPublicationResponse QueryPublicationsAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewPublicationResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.Publication, new string[] { "document_number" });
    }

    internal PatentsViewInventorResponse QueryInventorsAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewInventorResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.Inventor, new string[] { "inventor_id" });
    }

    internal PatentsViewAssigneeResponse QueryAssigneesAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewAssigneeResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.Assignee, new string[] { "assignee_id" });
    }

    internal PatentsViewLocationResponse QueryLocationsAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewLocationResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.Location, new string[] { "location_id" });
    }

    internal PatentsViewAttorneyResponse QueryAttorneysAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewAttorneyResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.Attorney, new string[] { "attorney_id" });
    }

    internal PatentsViewCpcClassificationResponse QueryCpcClassesAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewCpcClassificationResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.CpcClass, new string[] { "cpc_class_id" });
    }

    internal PatentsViewCpcClassificationResponse QueryCpcSubclassesAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewCpcClassificationResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.CpcSubclass, new string[] { "cpc_subclass_id" });
    }

    internal PatentsViewCpcClassificationResponse QueryCpcGroupsAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewCpcClassificationResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.CpcGroup, new string[] { "cpc_group_id" });
    }

    internal PatentsViewIpcClassificationResponse QueryIpcAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewIpcClassificationResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.Ipc, new string[] { "ipc_id" });
    }

    internal PatentsViewUspcClassificationResponse QueryUspcMainClassesAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewUspcClassificationResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.UspcMainClass, new string[] { "uspc_mainclass_id" });
    }

    internal PatentsViewUspcClassificationResponse QueryUspcSubclassesAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewUspcClassificationResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.UspcSubclass, new string[] { "uspc_subclass_id" });
    }

    internal PatentsViewWipoClassificationResponse QueryWipoAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewWipoClassificationResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.Wipo, new string[] { "wipo_id" });
    }

    internal PatentsViewCitationResponse QueryUsPatentCitationsAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewCitationResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.UsPatentCitation, new string[] { "patent_id", "citation_sequence" });
    }

    internal PatentsViewCitationResponse QueryUsApplicationCitationsAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewCitationResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.UsApplicationCitation, new string[] { "patent_id", "citation_sequence" });
    }

    internal PatentsViewCitationResponse QueryForeignCitationsAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewCitationResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.ForeignCitation, new string[] { "patent_id", "citation_sequence" });
    }

    internal PatentsViewCitationResponse QueryOtherReferencesAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewCitationResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.OtherReference, new string[] { "patent_id", "reference_sequence" });
    }

    internal PatentsViewRelatedApplicationResponse QueryPatentRelatedApplicationsAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewRelatedApplicationResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.PatentRelatedApplication, new string[] { "patent_id" });
    }

    internal PatentsViewRelatedApplicationResponse QueryPublicationRelatedApplicationsAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewRelatedApplicationResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.PublicationRelatedApplication, new string[] { "document_number" });
    }

    internal PatentsViewSummaryResponse QueryGrantSummariesAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewSummaryResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.GrantSummary, new string[] { "patent_id" });
    }

    internal PatentsViewSummaryResponse QueryPublicationSummariesAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewSummaryResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.PublicationSummary, new string[] { "document_number" });
    }

    internal PatentsViewClaimResponse QueryGrantClaimsAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewClaimResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.GrantClaim, new string[] { "patent_id", "claim_sequence" });
    }

    internal PatentsViewClaimResponse QueryPublicationClaimsAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewClaimResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.PublicationClaim, new string[] { "document_number", "claim_sequence" });
    }

    internal PatentsViewDescriptionResponse QueryGrantDescriptionsAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewDescriptionResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.GrantDescription, new string[] { "patent_id" });
    }

    internal PatentsViewDescriptionResponse QueryPublicationDescriptionsAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewDescriptionResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.PublicationDescription, new string[] { "document_number" });
    }

    internal PatentsViewDrawingDescriptionResponse QueryGrantDrawingDescriptionsAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewDrawingDescriptionResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.GrantDrawingDescription, new string[] { "patent_id", "draw_desc_sequence" });
    }

    internal PatentsViewDrawingDescriptionResponse QueryPublicationDrawingDescriptionsAll(PatentsViewQueryRequest request, bool usePost)
    {
        return (PatentsViewDrawingDescriptionResponse)AggregateEndpoint(request, usePost, PatentsViewQueryEndpoint.PublicationDrawingDescription, new string[] { "document_number", "draw_desc_sequence" });
    }

    internal object AggregateEndpoint(PatentsViewQueryRequest request, bool usePost, PatentsViewQueryEndpoint endpoint, string[] identityFields)
    {
        PatentsViewPatentOperation.ValidateQueryRequest(request);
        ValidateIdentitySort(request.Search.Sorts, identityFields);
        PatentsViewCursorPageSource source = new PatentsViewCursorPageSource(this, request, endpoint, usePost, identityFields);
        CursorPaginationResult pagination = PaginationEngine.AggregateCursorPages(source, request.Search, Options.Limits, identityFields[identityFields.Length - 1]);
        PatentsViewResponseMetadata metadata = CreateAggregateMetadata(request, pagination, source.FirstMetadata);
        return CreateTypedResponse(endpoint, pagination.Records, metadata);
    }

    internal static void ValidateIdentitySort(SearchSort[] sorts, string[] identityFields)
    {
        if (sorts == null || identityFields == null || sorts.Length < identityFields.Length || identityFields.Length == 0)
        {
            throw new ValidationException("PatentsView all-page retrieval requires the endpoint's stable identity sort.", "Sorts", "DETERMINISTIC_SORT_REQUIRED", null);
        }

        int sortStart = sorts.Length - identityFields.Length;
        int index;
        for (index = 0; index < identityFields.Length; index += 1)
        {
            SearchSort sort = sorts[sortStart + index];
            if (sort == null || !global::System.String.Equals(sort.Field, identityFields[index], global::System.StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("The final PatentsView sorts must preserve the endpoint's stable identity order.", "Sorts", "DETERMINISTIC_SORT_REQUIRED", null);
            }
        }
    }

    internal static PatentsViewResponseMetadata CreateAggregateMetadata(PatentsViewQueryRequest request, CursorPaginationResult pagination, PatentsViewResponseMetadata? first)
    {
        PatentsViewResponseMetadata metadata = new PatentsViewResponseMetadata();
        metadata.Request = request;
        metadata.Count = pagination.Records.Length;
        metadata.TotalHits = pagination.Total;
        metadata.HasTotalHits = pagination.HasTotal;
        metadata.IsComplete = pagination.Completed;
        metadata.HasNextCursorPage = pagination.HasNextCursorPage;
        metadata.NextCursorPage.AfterValues = PaginationEngine.CopyValues(pagination.NextCursorPage.AfterValues);
        metadata.NextCursorPage.Size = pagination.NextCursorPage.Size;
        if (first != null)
        {
            metadata.IsError = first.IsError;
            metadata.RequestIdentifier = first.RequestIdentifier;
            metadata.Provenance = OdpJsonParser.CopyProvenance(first.Provenance);
            metadata.RawResponse = first.RawResponse;
            foreach (global::System.Collections.Generic.KeyValuePair<string, string> field in first.AdditionalFields)
            {
                metadata.AdditionalFields[field.Key] = field.Value;
            }
        }

        return metadata;
    }

    internal static object CreateTypedResponse(PatentsViewQueryEndpoint endpoint, object[] records, PatentsViewResponseMetadata metadata)
    {
        switch (endpoint)
        {
            case PatentsViewQueryEndpoint.Patent:
                PatentsViewPatentResponse patents = new PatentsViewPatentResponse();
                patents.Patents = CopyPatents(records);
                patents.Metadata = metadata;
                return patents;
            case PatentsViewQueryEndpoint.Publication:
                PatentsViewPublicationResponse publications = new PatentsViewPublicationResponse();
                publications.Publications = CopyPublications(records);
                publications.Metadata = metadata;
                return publications;
            case PatentsViewQueryEndpoint.Inventor:
                PatentsViewInventorResponse inventors = new PatentsViewInventorResponse();
                inventors.Inventors = CopyInventors(records);
                inventors.Metadata = metadata;
                return inventors;
            case PatentsViewQueryEndpoint.Assignee:
                PatentsViewAssigneeResponse assignees = new PatentsViewAssigneeResponse();
                assignees.Assignees = CopyAssignees(records);
                assignees.Metadata = metadata;
                return assignees;
            case PatentsViewQueryEndpoint.Location:
                PatentsViewLocationResponse locations = new PatentsViewLocationResponse();
                locations.Locations = CopyLocations(records);
                locations.Metadata = metadata;
                return locations;
            case PatentsViewQueryEndpoint.Attorney:
                PatentsViewAttorneyResponse attorneys = new PatentsViewAttorneyResponse();
                attorneys.Attorneys = CopyAttorneys(records);
                attorneys.Metadata = metadata;
                return attorneys;
            case PatentsViewQueryEndpoint.CpcClass:
            case PatentsViewQueryEndpoint.CpcSubclass:
            case PatentsViewQueryEndpoint.CpcGroup:
                PatentsViewCpcClassificationResponse cpc = new PatentsViewCpcClassificationResponse();
                cpc.Classifications = CopyCpc(records);
                cpc.Metadata = metadata;
                return cpc;
            case PatentsViewQueryEndpoint.Ipc:
                PatentsViewIpcClassificationResponse ipc = new PatentsViewIpcClassificationResponse();
                ipc.Classifications = CopyIpc(records);
                ipc.Metadata = metadata;
                return ipc;
            case PatentsViewQueryEndpoint.UspcMainClass:
            case PatentsViewQueryEndpoint.UspcSubclass:
                PatentsViewUspcClassificationResponse uspc = new PatentsViewUspcClassificationResponse();
                uspc.Classifications = CopyUspc(records);
                uspc.Metadata = metadata;
                return uspc;
            case PatentsViewQueryEndpoint.Wipo:
                PatentsViewWipoClassificationResponse wipo = new PatentsViewWipoClassificationResponse();
                wipo.Classifications = CopyWipo(records);
                wipo.Metadata = metadata;
                return wipo;
            case PatentsViewQueryEndpoint.UsPatentCitation:
            case PatentsViewQueryEndpoint.UsApplicationCitation:
            case PatentsViewQueryEndpoint.ForeignCitation:
            case PatentsViewQueryEndpoint.OtherReference:
                PatentsViewCitationResponse citations = new PatentsViewCitationResponse();
                citations.Citations = CopyCitations(records);
                citations.Metadata = metadata;
                return citations;
            case PatentsViewQueryEndpoint.PatentRelatedApplication:
            case PatentsViewQueryEndpoint.PublicationRelatedApplication:
                PatentsViewRelatedApplicationResponse related = new PatentsViewRelatedApplicationResponse();
                related.RelatedApplications = CopyRelated(records);
                related.Metadata = metadata;
                return related;
            case PatentsViewQueryEndpoint.GrantSummary:
            case PatentsViewQueryEndpoint.PublicationSummary:
                PatentsViewSummaryResponse summaries = new PatentsViewSummaryResponse();
                summaries.Summaries = CopySummaries(records);
                summaries.Metadata = metadata;
                return summaries;
            case PatentsViewQueryEndpoint.GrantClaim:
            case PatentsViewQueryEndpoint.PublicationClaim:
                PatentsViewClaimResponse claims = new PatentsViewClaimResponse();
                claims.Claims = CopyClaims(records);
                claims.Metadata = metadata;
                return claims;
            case PatentsViewQueryEndpoint.GrantDescription:
            case PatentsViewQueryEndpoint.PublicationDescription:
                PatentsViewDescriptionResponse descriptions = new PatentsViewDescriptionResponse();
                descriptions.Descriptions = CopyDescriptions(records);
                descriptions.Metadata = metadata;
                return descriptions;
            default:
                PatentsViewDrawingDescriptionResponse drawings = new PatentsViewDrawingDescriptionResponse();
                drawings.DrawingDescriptions = CopyDrawings(records);
                drawings.Metadata = metadata;
                return drawings;
        }
    }

    internal static PatentsViewPatent[] CopyPatents(object[] records)
    {
        PatentsViewPatent[] result = new PatentsViewPatent[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = (PatentsViewPatent)records[index];
        }

        return result;
    }

    internal static PatentsViewPublication[] CopyPublications(object[] records)
    {
        PatentsViewPublication[] result = new PatentsViewPublication[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = (PatentsViewPublication)records[index];
        }

        return result;
    }

    internal static PatentsViewInventor[] CopyInventors(object[] records)
    {
        PatentsViewInventor[] result = new PatentsViewInventor[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = (PatentsViewInventor)records[index];
        }

        return result;
    }

    internal static PatentsViewAssignee[] CopyAssignees(object[] records)
    {
        PatentsViewAssignee[] result = new PatentsViewAssignee[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = (PatentsViewAssignee)records[index];
        }

        return result;
    }

    internal static PatentsViewLocation[] CopyLocations(object[] records)
    {
        PatentsViewLocation[] result = new PatentsViewLocation[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = (PatentsViewLocation)records[index];
        }

        return result;
    }

    internal static PatentsViewAttorney[] CopyAttorneys(object[] records)
    {
        PatentsViewAttorney[] result = new PatentsViewAttorney[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = (PatentsViewAttorney)records[index];
        }

        return result;
    }

    internal static PatentsViewCpcClassification[] CopyCpc(object[] records)
    {
        PatentsViewCpcClassification[] result = new PatentsViewCpcClassification[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = (PatentsViewCpcClassification)records[index];
        }

        return result;
    }

    internal static PatentsViewIpcClassification[] CopyIpc(object[] records)
    {
        PatentsViewIpcClassification[] result = new PatentsViewIpcClassification[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = (PatentsViewIpcClassification)records[index];
        }

        return result;
    }

    internal static PatentsViewUspcClassification[] CopyUspc(object[] records)
    {
        PatentsViewUspcClassification[] result = new PatentsViewUspcClassification[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = (PatentsViewUspcClassification)records[index];
        }

        return result;
    }

    internal static PatentsViewWipoClassification[] CopyWipo(object[] records)
    {
        PatentsViewWipoClassification[] result = new PatentsViewWipoClassification[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = (PatentsViewWipoClassification)records[index];
        }

        return result;
    }

    internal static PatentsViewCitation[] CopyCitations(object[] records)
    {
        PatentsViewCitation[] result = new PatentsViewCitation[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = (PatentsViewCitation)records[index];
        }

        return result;
    }

    internal static PatentsViewRelatedApplication[] CopyRelated(object[] records)
    {
        PatentsViewRelatedApplication[] result = new PatentsViewRelatedApplication[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = (PatentsViewRelatedApplication)records[index];
        }

        return result;
    }

    internal static PatentsViewSummary[] CopySummaries(object[] records)
    {
        PatentsViewSummary[] result = new PatentsViewSummary[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = (PatentsViewSummary)records[index];
        }

        return result;
    }

    internal static PatentsViewClaim[] CopyClaims(object[] records)
    {
        PatentsViewClaim[] result = new PatentsViewClaim[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = (PatentsViewClaim)records[index];
        }

        return result;
    }

    internal static PatentsViewDescription[] CopyDescriptions(object[] records)
    {
        PatentsViewDescription[] result = new PatentsViewDescription[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = (PatentsViewDescription)records[index];
        }

        return result;
    }

    internal static PatentsViewDrawingDescription[] CopyDrawings(object[] records)
    {
        PatentsViewDrawingDescription[] result = new PatentsViewDrawingDescription[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = (PatentsViewDrawingDescription)records[index];
        }

        return result;
    }
}
