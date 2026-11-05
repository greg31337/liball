namespace uspto;

internal class PatentsViewEntityOperation
{
    internal PatentsViewPatentOperation Transport;

    internal PatentsViewEntityOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        Transport = new PatentsViewPatentOperation(client, options);
    }

    internal PatentsViewInventorResponse QueryInventorsGet(PatentsViewQueryRequest request)
    {
        PatentsViewPatentOperation.ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = Transport.ExecuteQuery("inventor", request, false);
        return PatentsViewEntityParser.ParseInventorResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache);
    }

    internal PatentsViewInventorResponse QueryInventorsPost(PatentsViewQueryRequest request)
    {
        PatentsViewPatentOperation.ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = Transport.ExecuteQuery("inventor", request, true);
        return PatentsViewEntityParser.ParseInventorResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache);
    }

    internal PatentsViewAssigneeResponse QueryAssigneesGet(PatentsViewQueryRequest request)
    {
        PatentsViewPatentOperation.ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = Transport.ExecuteQuery("assignee", request, false);
        return PatentsViewEntityParser.ParseAssigneeResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache);
    }

    internal PatentsViewAssigneeResponse QueryAssigneesPost(PatentsViewQueryRequest request)
    {
        PatentsViewPatentOperation.ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = Transport.ExecuteQuery("assignee", request, true);
        return PatentsViewEntityParser.ParseAssigneeResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache);
    }

    internal PatentsViewLocationResponse QueryLocationsGet(PatentsViewQueryRequest request)
    {
        PatentsViewPatentOperation.ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = Transport.ExecuteQuery("location", request, false);
        return PatentsViewEntityParser.ParseLocationResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache);
    }

    internal PatentsViewLocationResponse QueryLocationsPost(PatentsViewQueryRequest request)
    {
        PatentsViewPatentOperation.ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = Transport.ExecuteQuery("location", request, true);
        return PatentsViewEntityParser.ParseLocationResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache);
    }

    internal PatentsViewAttorneyResponse QueryAttorneysGet(PatentsViewQueryRequest request)
    {
        PatentsViewPatentOperation.ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = Transport.ExecuteQuery("patent/attorney", request, false);
        return PatentsViewEntityParser.ParseAttorneyResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache);
    }

    internal PatentsViewAttorneyResponse QueryAttorneysPost(PatentsViewQueryRequest request)
    {
        PatentsViewPatentOperation.ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = Transport.ExecuteQuery("patent/attorney", request, true);
        return PatentsViewEntityParser.ParseAttorneyResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache);
    }

    internal PatentsViewInventor GetInventor(string identifier, RequestContext context)
    {
        PatentsViewPatentOperation.ValidateRetrievalRequest(identifier, context, "inventorIdentifier");
        PatentsViewBufferedResult buffered = Transport.ExecuteRetrieval("inventor", identifier, context);
        PatentsViewQueryRequest request = PatentsViewPatentOperation.CreateRetrievalQuery(context);
        PatentsViewInventorResponse response = PatentsViewEntityParser.ParseInventorResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache);
        if (response.Inventors.Length == 0)
        {
            throw CreateEntityNotFound("inventor", identifier, buffered.Response.Metadata);
        }

        if (response.Inventors.Length != 1 || !global::System.String.Equals(response.Inventors[0].InventorIdentifier, identifier, global::System.StringComparison.Ordinal))
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView inventor response does not match the requested identifier.", "$.inventors");
        }

        return response.Inventors[0];
    }

    internal PatentsViewAssignee GetAssignee(string identifier, RequestContext context)
    {
        PatentsViewPatentOperation.ValidateRetrievalRequest(identifier, context, "assigneeIdentifier");
        PatentsViewBufferedResult buffered = Transport.ExecuteRetrieval("assignee", identifier, context);
        PatentsViewQueryRequest request = PatentsViewPatentOperation.CreateRetrievalQuery(context);
        PatentsViewAssigneeResponse response = PatentsViewEntityParser.ParseAssigneeResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache);
        if (response.Assignees.Length == 0)
        {
            throw CreateEntityNotFound("assignee", identifier, buffered.Response.Metadata);
        }

        if (response.Assignees.Length != 1 || !global::System.String.Equals(response.Assignees[0].AssigneeIdentifier, identifier, global::System.StringComparison.Ordinal))
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView assignee response does not match the requested identifier.", "$.assignees");
        }

        return response.Assignees[0];
    }

    internal PatentsViewLocation GetLocation(string identifier, RequestContext context)
    {
        PatentsViewPatentOperation.ValidateRetrievalRequest(identifier, context, "locationIdentifier");
        PatentsViewBufferedResult buffered = Transport.ExecuteRetrieval("location", identifier, context);
        PatentsViewQueryRequest request = PatentsViewPatentOperation.CreateRetrievalQuery(context);
        PatentsViewLocationResponse response = PatentsViewEntityParser.ParseLocationResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache);
        if (response.Locations.Length == 0)
        {
            throw CreateEntityNotFound("location", identifier, buffered.Response.Metadata);
        }

        if (response.Locations.Length != 1 || !global::System.String.Equals(response.Locations[0].LocationIdentifier, identifier, global::System.StringComparison.Ordinal))
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView location response does not match the requested identifier.", "$.locations");
        }

        return response.Locations[0];
    }

    internal PatentsViewAttorney GetAttorney(string identifier, RequestContext context)
    {
        PatentsViewPatentOperation.ValidateRetrievalRequest(identifier, context, "attorneyIdentifier");
        PatentsViewBufferedResult buffered = Transport.ExecuteRetrieval("patent/attorney", identifier, context);
        PatentsViewQueryRequest request = PatentsViewPatentOperation.CreateRetrievalQuery(context);
        PatentsViewAttorneyResponse response = PatentsViewEntityParser.ParseAttorneyResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache);
        if (response.Attorneys.Length == 0)
        {
            throw CreateEntityNotFound("attorney", identifier, buffered.Response.Metadata);
        }

        if (response.Attorneys.Length != 1 || !global::System.String.Equals(response.Attorneys[0].AttorneyIdentifier, identifier, global::System.StringComparison.Ordinal))
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView attorney response does not match the requested identifier.", "$.attorneys");
        }

        return response.Attorneys[0];
    }

    internal static NotFoundException CreateEntityNotFound(string entityName, string identifier, HttpResponseMetadata metadata)
    {
        return new NotFoundException("The requested PatentsView " + entityName + " was not found.", ApiService.PatentsView, identifier, metadata.StatusCode, true, metadata.RequestIdentifier, null);
    }
}
