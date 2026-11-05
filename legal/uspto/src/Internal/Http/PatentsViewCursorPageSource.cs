namespace uspto;

internal class PatentsViewTypedPage
{
    internal object[] Records = new object[0];
    internal PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}

internal class PatentsViewCursorPageSource : ICursorPageSource
{
    internal PatentsViewAllPageOperation Operation;
    internal PatentsViewQueryRequest Request;
    internal PatentsViewQueryEndpoint Endpoint;
    internal bool UsePost;
    internal string[] IdentityFields;
    internal PatentsViewResponseMetadata? FirstMetadata = null;

    internal PatentsViewCursorPageSource(PatentsViewAllPageOperation operation, PatentsViewQueryRequest request, PatentsViewQueryEndpoint endpoint, bool usePost, string[] identityFields)
    {
        Operation = operation;
        Request = request;
        Endpoint = endpoint;
        UsePost = usePost;
        IdentityFields = identityFields;
    }

    public CursorPageResult FetchCursorPage(string[] afterValues, int size, global::System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PatentsViewQueryRequest pageRequest = CreatePageRequest(Request, afterValues, size, cancellationToken);
        PatentsViewTypedPage typedPage = FetchTypedPage(pageRequest);
        if (FirstMetadata == null)
        {
            FirstMetadata = typedPage.Metadata;
        }

        if (!typedPage.Metadata.IsComplete && !typedPage.Metadata.HasNextCursorPage)
        {
            throw new ValidationException("A non-final PatentsView page did not contain all stable cursor sort values.", "FieldSelection.Fields", "MISSING_CURSOR_FIELDS", null);
        }

        CursorPageResult page = new CursorPageResult();
        page.Records = typedPage.Records;
        page.RecordIdentifiers = CreateRecordIdentifiers(typedPage.Records);
        page.Metadata.Request = pageRequest.Search;
        page.Metadata.ReturnedCount = typedPage.Metadata.Count;
        page.Metadata.Total = typedPage.Metadata.TotalHits;
        page.Metadata.HasTotal = typedPage.Metadata.HasTotalHits;
        page.Metadata.HasNextCursorPage = typedPage.Metadata.HasNextCursorPage;
        page.Metadata.NextCursorPage.AfterValues = PaginationEngine.CopyValues(typedPage.Metadata.NextCursorPage.AfterValues);
        page.Metadata.NextCursorPage.Size = typedPage.Metadata.NextCursorPage.Size;
        page.Metadata.RequestIdentifier = typedPage.Metadata.RequestIdentifier;
        page.Metadata.Provenance = typedPage.Metadata.Provenance.Source;
        page.Metadata.FromCache = typedPage.Metadata.Provenance.FromCache;
        page.Metadata.FieldSources = ProvenanceLabeler.CopyFieldSources(typedPage.Metadata.Provenance.FieldSources);
        return page;
    }

    internal PatentsViewTypedPage FetchTypedPage(PatentsViewQueryRequest request)
    {
        PatentsViewTypedPage page = new PatentsViewTypedPage();
        switch (Endpoint)
        {
            case PatentsViewQueryEndpoint.Patent:
                PatentsViewPatentResponse patents = UsePost ? Operation.PatentOperation.QueryPatentsPost(request) : Operation.PatentOperation.QueryPatentsGet(request);
                page.Records = patents.Patents;
                page.Metadata = patents.Metadata;
                break;
            case PatentsViewQueryEndpoint.Publication:
                PatentsViewPublicationResponse publications = UsePost ? Operation.PatentOperation.QueryPublicationsPost(request) : Operation.PatentOperation.QueryPublicationsGet(request);
                page.Records = publications.Publications;
                page.Metadata = publications.Metadata;
                break;
            case PatentsViewQueryEndpoint.Inventor:
                PatentsViewInventorResponse inventors = UsePost ? Operation.EntityOperation.QueryInventorsPost(request) : Operation.EntityOperation.QueryInventorsGet(request);
                page.Records = inventors.Inventors;
                page.Metadata = inventors.Metadata;
                break;
            case PatentsViewQueryEndpoint.Assignee:
                PatentsViewAssigneeResponse assignees = UsePost ? Operation.EntityOperation.QueryAssigneesPost(request) : Operation.EntityOperation.QueryAssigneesGet(request);
                page.Records = assignees.Assignees;
                page.Metadata = assignees.Metadata;
                break;
            case PatentsViewQueryEndpoint.Location:
                PatentsViewLocationResponse locations = UsePost ? Operation.EntityOperation.QueryLocationsPost(request) : Operation.EntityOperation.QueryLocationsGet(request);
                page.Records = locations.Locations;
                page.Metadata = locations.Metadata;
                break;
            case PatentsViewQueryEndpoint.Attorney:
                PatentsViewAttorneyResponse attorneys = UsePost ? Operation.EntityOperation.QueryAttorneysPost(request) : Operation.EntityOperation.QueryAttorneysGet(request);
                page.Records = attorneys.Attorneys;
                page.Metadata = attorneys.Metadata;
                break;
            case PatentsViewQueryEndpoint.CpcClass:
                PatentsViewCpcClassificationResponse cpcClasses = UsePost ? Operation.ClassificationOperation.QueryCpcClassesPost(request) : Operation.ClassificationOperation.QueryCpcClassesGet(request);
                page.Records = cpcClasses.Classifications;
                page.Metadata = cpcClasses.Metadata;
                break;
            case PatentsViewQueryEndpoint.CpcSubclass:
                PatentsViewCpcClassificationResponse cpcSubclasses = UsePost ? Operation.ClassificationOperation.QueryCpcSubclassesPost(request) : Operation.ClassificationOperation.QueryCpcSubclassesGet(request);
                page.Records = cpcSubclasses.Classifications;
                page.Metadata = cpcSubclasses.Metadata;
                break;
            case PatentsViewQueryEndpoint.CpcGroup:
                PatentsViewCpcClassificationResponse cpcGroups = UsePost ? Operation.ClassificationOperation.QueryCpcGroupsPost(request) : Operation.ClassificationOperation.QueryCpcGroupsGet(request);
                page.Records = cpcGroups.Classifications;
                page.Metadata = cpcGroups.Metadata;
                break;
            case PatentsViewQueryEndpoint.Ipc:
                PatentsViewIpcClassificationResponse ipc = UsePost ? Operation.ClassificationOperation.QueryIpcPost(request) : Operation.ClassificationOperation.QueryIpcGet(request);
                page.Records = ipc.Classifications;
                page.Metadata = ipc.Metadata;
                break;
            case PatentsViewQueryEndpoint.UspcMainClass:
                PatentsViewUspcClassificationResponse uspcMain = UsePost ? Operation.ClassificationOperation.QueryUspcMainClassesPost(request) : Operation.ClassificationOperation.QueryUspcMainClassesGet(request);
                page.Records = uspcMain.Classifications;
                page.Metadata = uspcMain.Metadata;
                break;
            case PatentsViewQueryEndpoint.UspcSubclass:
                PatentsViewUspcClassificationResponse uspcSubclass = UsePost ? Operation.ClassificationOperation.QueryUspcSubclassesPost(request) : Operation.ClassificationOperation.QueryUspcSubclassesGet(request);
                page.Records = uspcSubclass.Classifications;
                page.Metadata = uspcSubclass.Metadata;
                break;
            case PatentsViewQueryEndpoint.Wipo:
                PatentsViewWipoClassificationResponse wipo = UsePost ? Operation.ClassificationOperation.QueryWipoPost(request) : Operation.ClassificationOperation.QueryWipoGet(request);
                page.Records = wipo.Classifications;
                page.Metadata = wipo.Metadata;
                break;
            case PatentsViewQueryEndpoint.UsPatentCitation:
                PatentsViewCitationResponse patentCitations = UsePost ? Operation.CitationOperation.QueryUsPatentCitationsPost(request) : Operation.CitationOperation.QueryUsPatentCitationsGet(request);
                page.Records = patentCitations.Citations;
                page.Metadata = patentCitations.Metadata;
                break;
            case PatentsViewQueryEndpoint.UsApplicationCitation:
                PatentsViewCitationResponse applicationCitations = UsePost ? Operation.CitationOperation.QueryUsApplicationCitationsPost(request) : Operation.CitationOperation.QueryUsApplicationCitationsGet(request);
                page.Records = applicationCitations.Citations;
                page.Metadata = applicationCitations.Metadata;
                break;
            case PatentsViewQueryEndpoint.ForeignCitation:
                PatentsViewCitationResponse foreignCitations = UsePost ? Operation.CitationOperation.QueryForeignCitationsPost(request) : Operation.CitationOperation.QueryForeignCitationsGet(request);
                page.Records = foreignCitations.Citations;
                page.Metadata = foreignCitations.Metadata;
                break;
            case PatentsViewQueryEndpoint.OtherReference:
                PatentsViewCitationResponse otherReferences = UsePost ? Operation.CitationOperation.QueryOtherReferencesPost(request) : Operation.CitationOperation.QueryOtherReferencesGet(request);
                page.Records = otherReferences.Citations;
                page.Metadata = otherReferences.Metadata;
                break;
            case PatentsViewQueryEndpoint.PatentRelatedApplication:
                PatentsViewRelatedApplicationResponse patentRelated = UsePost ? Operation.CitationOperation.QueryPatentRelatedApplicationsPost(request) : Operation.CitationOperation.QueryPatentRelatedApplicationsGet(request);
                page.Records = patentRelated.RelatedApplications;
                page.Metadata = patentRelated.Metadata;
                break;
            case PatentsViewQueryEndpoint.PublicationRelatedApplication:
                PatentsViewRelatedApplicationResponse publicationRelated = UsePost ? Operation.CitationOperation.QueryPublicationRelatedApplicationsPost(request) : Operation.CitationOperation.QueryPublicationRelatedApplicationsGet(request);
                page.Records = publicationRelated.RelatedApplications;
                page.Metadata = publicationRelated.Metadata;
                break;
            case PatentsViewQueryEndpoint.GrantSummary:
                PatentsViewSummaryResponse grantSummaries = UsePost ? Operation.TextOperation.QueryGrantSummariesPost(request) : Operation.TextOperation.QueryGrantSummariesGet(request);
                page.Records = grantSummaries.Summaries;
                page.Metadata = grantSummaries.Metadata;
                break;
            case PatentsViewQueryEndpoint.PublicationSummary:
                PatentsViewSummaryResponse publicationSummaries = UsePost ? Operation.TextOperation.QueryPublicationSummariesPost(request) : Operation.TextOperation.QueryPublicationSummariesGet(request);
                page.Records = publicationSummaries.Summaries;
                page.Metadata = publicationSummaries.Metadata;
                break;
            case PatentsViewQueryEndpoint.GrantClaim:
                PatentsViewClaimResponse grantClaims = UsePost ? Operation.TextOperation.QueryGrantClaimsPost(request) : Operation.TextOperation.QueryGrantClaimsGet(request);
                page.Records = grantClaims.Claims;
                page.Metadata = grantClaims.Metadata;
                break;
            case PatentsViewQueryEndpoint.PublicationClaim:
                PatentsViewClaimResponse publicationClaims = UsePost ? Operation.TextOperation.QueryPublicationClaimsPost(request) : Operation.TextOperation.QueryPublicationClaimsGet(request);
                page.Records = publicationClaims.Claims;
                page.Metadata = publicationClaims.Metadata;
                break;
            case PatentsViewQueryEndpoint.GrantDescription:
                PatentsViewDescriptionResponse grantDescriptions = UsePost ? Operation.TextOperation.QueryGrantDescriptionsPost(request) : Operation.TextOperation.QueryGrantDescriptionsGet(request);
                page.Records = grantDescriptions.Descriptions;
                page.Metadata = grantDescriptions.Metadata;
                break;
            case PatentsViewQueryEndpoint.PublicationDescription:
                PatentsViewDescriptionResponse publicationDescriptions = UsePost ? Operation.TextOperation.QueryPublicationDescriptionsPost(request) : Operation.TextOperation.QueryPublicationDescriptionsGet(request);
                page.Records = publicationDescriptions.Descriptions;
                page.Metadata = publicationDescriptions.Metadata;
                break;
            case PatentsViewQueryEndpoint.GrantDrawingDescription:
                PatentsViewDrawingDescriptionResponse grantDrawings = UsePost ? Operation.TextOperation.QueryGrantDrawingDescriptionsPost(request) : Operation.TextOperation.QueryGrantDrawingDescriptionsGet(request);
                page.Records = grantDrawings.DrawingDescriptions;
                page.Metadata = grantDrawings.Metadata;
                break;
            case PatentsViewQueryEndpoint.PublicationDrawingDescription:
                PatentsViewDrawingDescriptionResponse publicationDrawings = UsePost ? Operation.TextOperation.QueryPublicationDrawingDescriptionsPost(request) : Operation.TextOperation.QueryPublicationDrawingDescriptionsGet(request);
                page.Records = publicationDrawings.DrawingDescriptions;
                page.Metadata = publicationDrawings.Metadata;
                break;
            default:
                throw new ValidationException("The PatentsView query endpoint is unsupported.", "endpoint", "UNSUPPORTED_ENDPOINT", null);
        }

        return page;
    }

    internal string[] CreateRecordIdentifiers(object[] records)
    {
        string[] identifiers = new string[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            string? first = null;
            int sequence = 0;
            bool hasSequence = false;
            switch (Endpoint)
            {
                case PatentsViewQueryEndpoint.Patent:
                    first = ((PatentsViewPatent)records[index]).PatentIdentifier;
                    break;
                case PatentsViewQueryEndpoint.Publication:
                    first = ((PatentsViewPublication)records[index]).DocumentNumber;
                    break;
                case PatentsViewQueryEndpoint.Inventor:
                    first = ((PatentsViewInventor)records[index]).InventorIdentifier;
                    break;
                case PatentsViewQueryEndpoint.Assignee:
                    first = ((PatentsViewAssignee)records[index]).AssigneeIdentifier;
                    break;
                case PatentsViewQueryEndpoint.Location:
                    first = ((PatentsViewLocation)records[index]).LocationIdentifier;
                    break;
                case PatentsViewQueryEndpoint.Attorney:
                    first = ((PatentsViewAttorney)records[index]).AttorneyIdentifier;
                    break;
                case PatentsViewQueryEndpoint.CpcClass:
                case PatentsViewQueryEndpoint.CpcSubclass:
                case PatentsViewQueryEndpoint.CpcGroup:
                    first = ((PatentsViewCpcClassification)records[index]).ClassificationIdentifier;
                    break;
                case PatentsViewQueryEndpoint.Ipc:
                    first = ((PatentsViewIpcClassification)records[index]).ClassificationIdentifier;
                    break;
                case PatentsViewQueryEndpoint.UspcMainClass:
                case PatentsViewQueryEndpoint.UspcSubclass:
                    first = ((PatentsViewUspcClassification)records[index]).ClassificationIdentifier;
                    break;
                case PatentsViewQueryEndpoint.Wipo:
                    first = ((PatentsViewWipoClassification)records[index]).ClassificationIdentifier;
                    break;
                case PatentsViewQueryEndpoint.UsPatentCitation:
                case PatentsViewQueryEndpoint.UsApplicationCitation:
                case PatentsViewQueryEndpoint.ForeignCitation:
                case PatentsViewQueryEndpoint.OtherReference:
                    PatentsViewCitation citation = (PatentsViewCitation)records[index];
                    first = citation.PatentIdentifier;
                    sequence = citation.Sequence;
                    hasSequence = citation.HasSequence;
                    break;
                case PatentsViewQueryEndpoint.PatentRelatedApplication:
                    first = ((PatentsViewRelatedApplication)records[index]).PatentIdentifier;
                    break;
                case PatentsViewQueryEndpoint.PublicationRelatedApplication:
                    first = ((PatentsViewRelatedApplication)records[index]).DocumentNumber;
                    break;
                case PatentsViewQueryEndpoint.GrantSummary:
                    first = ((PatentsViewSummary)records[index]).PatentIdentifier;
                    break;
                case PatentsViewQueryEndpoint.PublicationSummary:
                    first = ((PatentsViewSummary)records[index]).DocumentNumber;
                    break;
                case PatentsViewQueryEndpoint.GrantClaim:
                case PatentsViewQueryEndpoint.PublicationClaim:
                    PatentsViewClaim claim = (PatentsViewClaim)records[index];
                    first = Endpoint == PatentsViewQueryEndpoint.GrantClaim ? claim.PatentIdentifier : claim.DocumentNumber;
                    sequence = claim.Sequence;
                    hasSequence = claim.HasSequence;
                    break;
                case PatentsViewQueryEndpoint.GrantDescription:
                    first = ((PatentsViewDescription)records[index]).PatentIdentifier;
                    break;
                case PatentsViewQueryEndpoint.PublicationDescription:
                    first = ((PatentsViewDescription)records[index]).DocumentNumber;
                    break;
                case PatentsViewQueryEndpoint.GrantDrawingDescription:
                case PatentsViewQueryEndpoint.PublicationDrawingDescription:
                    PatentsViewDrawingDescription drawing = (PatentsViewDrawingDescription)records[index];
                    first = Endpoint == PatentsViewQueryEndpoint.GrantDrawingDescription ? drawing.PatentIdentifier : drawing.DocumentNumber;
                    sequence = drawing.Sequence;
                    hasSequence = drawing.HasSequence;
                    break;
            }

            identifiers[index] = CreateIdentity(first, sequence, IdentityFields.Length > 1, hasSequence);
        }

        return identifiers;
    }

    internal static string CreateIdentity(string? first, int sequence, bool requiresSequence, bool hasSequence)
    {
        if (global::System.String.IsNullOrWhiteSpace(first))
        {
            return string.Empty;
        }

        if (!requiresSequence)
        {
            return first;
        }

        if (!hasSequence)
        {
            return string.Empty;
        }

        string second = sequence.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        return first.Length.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + ":" + first + second.Length.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + ":" + second;
    }

    internal static PatentsViewQueryRequest CreatePageRequest(PatentsViewQueryRequest source, string[] afterValues, int size, global::System.Threading.CancellationToken cancellationToken)
    {
        PatentsViewQueryRequest request = new PatentsViewQueryRequest();
        request.PadPatentIdentifiers = source.PadPatentIdentifiers;
        request.ExcludeWithdrawnPatents = source.ExcludeWithdrawnPatents;
        request.Search.Query = source.Search.Query;
        request.Search.Filters = source.Search.Filters;
        request.Search.Ranges = source.Search.Ranges;
        request.Search.Sorts = CopySorts(source.Search.Sorts);
        request.Search.FieldSelection = CreateFieldSelection(source.Search.FieldSelection, request.Search.Sorts);
        request.Search.Facets = source.Search.Facets;
        request.Search.OffsetPage = source.Search.OffsetPage;
        request.Search.UseCursorPage = true;
        request.Search.CursorPage.AfterValues = PaginationEngine.CopyValues(afterValues);
        request.Search.CursorPage.Size = size;
        request.Search.MaximumPages = source.Search.MaximumPages;
        request.Search.MaximumResults = source.Search.MaximumResults;
        request.Search.Context = CreateContext(source.Search.Context, cancellationToken);
        return request;
    }

    internal static SearchSort[] CopySorts(SearchSort[] sorts)
    {
        SearchSort[] result = new SearchSort[sorts.Length];
        int index;
        for (index = 0; index < sorts.Length; index += 1)
        {
            result[index] = new SearchSort { Field = sorts[index].Field, Direction = sorts[index].Direction };
        }

        return result;
    }

    internal static SearchFieldSelection CreateFieldSelection(SearchFieldSelection selection, SearchSort[] sorts)
    {
        if (selection == null || selection.Fields == null)
        {
            throw new ValidationException("PatentsView selected fields are required.", "FieldSelection.Fields", "REQUIRED", null);
        }

        SearchFieldSelection result = new SearchFieldSelection();
        if (selection.Fields.Length == 0)
        {
            return result;
        }

        int missing = 0;
        int sortIndex;
        for (sortIndex = 0; sortIndex < sorts.Length; sortIndex += 1)
        {
            if (!ContainsField(selection.Fields, sorts[sortIndex].Field))
            {
                missing += 1;
            }
        }

        result.Fields = new string[selection.Fields.Length + missing];
        global::System.Array.Copy(selection.Fields, result.Fields, selection.Fields.Length);
        int destination = selection.Fields.Length;
        for (sortIndex = 0; sortIndex < sorts.Length; sortIndex += 1)
        {
            if (!ContainsField(selection.Fields, sorts[sortIndex].Field))
            {
                result.Fields[destination] = sorts[sortIndex].Field ?? string.Empty;
                destination += 1;
            }
        }

        return result;
    }

    internal static bool ContainsField(string[] fields, string? expected)
    {
        int index;
        for (index = 0; index < fields.Length; index += 1)
        {
            if (global::System.String.Equals(fields[index], expected, global::System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static RequestContext CreateContext(RequestContext source, global::System.Threading.CancellationToken cancellationToken)
    {
        if (source == null)
        {
            throw new ValidationException("A PatentsView request context is required.", "Context", "REQUIRED", null);
        }

        RequestContext context = new RequestContext();
        context.Service = source.Service;
        context.CancellationToken = cancellationToken;
        context.RetainRawResponse = source.RetainRawResponse;
        context.MaximumRawResponseBytes = source.MaximumRawResponseBytes;
        context.Deadline = source.Deadline;
        context.HasDeadline = source.HasDeadline;
        context.CorrelationIdentifier = source.CorrelationIdentifier;
        context.Fallback = source.Fallback;
        return context;
    }
}
