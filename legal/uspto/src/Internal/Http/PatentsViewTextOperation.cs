namespace uspto;

internal class PatentsViewTextOperation
{
    internal PatentsViewPatentOperation Transport;

    internal PatentsViewTextOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        Transport = new PatentsViewPatentOperation(client, options);
    }

    internal PatentsViewSummaryResponse QueryGrantSummariesGet(PatentsViewQueryRequest request)
    {
        return QuerySummaries("g_brf_sum_text", request, false, "g_brf_sum_texts", false);
    }

    internal PatentsViewSummaryResponse QueryGrantSummariesPost(PatentsViewQueryRequest request)
    {
        return QuerySummaries("g_brf_sum_text", request, true, "g_brf_sum_texts", false);
    }

    internal PatentsViewSummaryResponse QueryPublicationSummariesGet(PatentsViewQueryRequest request)
    {
        return QuerySummaries("pg_brf_sum_text", request, false, "pg_brf_sum_texts", true);
    }

    internal PatentsViewSummaryResponse QueryPublicationSummariesPost(PatentsViewQueryRequest request)
    {
        return QuerySummaries("pg_brf_sum_text", request, true, "pg_brf_sum_texts", true);
    }

    internal PatentsViewClaimResponse QueryGrantClaimsGet(PatentsViewQueryRequest request)
    {
        return QueryClaims("g_claim", request, false, "g_claims", false);
    }

    internal PatentsViewClaimResponse QueryGrantClaimsPost(PatentsViewQueryRequest request)
    {
        return QueryClaims("g_claim", request, true, "g_claims", false);
    }

    internal PatentsViewClaimResponse QueryPublicationClaimsGet(PatentsViewQueryRequest request)
    {
        return QueryClaims("pg_claim", request, false, "pg_claims", true);
    }

    internal PatentsViewClaimResponse QueryPublicationClaimsPost(PatentsViewQueryRequest request)
    {
        return QueryClaims("pg_claim", request, true, "pg_claims", true);
    }

    internal PatentsViewDescriptionResponse QueryGrantDescriptionsGet(PatentsViewQueryRequest request)
    {
        return QueryDescriptions("g_detail_desc_text", request, false, "g_detail_desc_texts", false);
    }

    internal PatentsViewDescriptionResponse QueryGrantDescriptionsPost(PatentsViewQueryRequest request)
    {
        return QueryDescriptions("g_detail_desc_text", request, true, "g_detail_desc_texts", false);
    }

    internal PatentsViewDescriptionResponse QueryPublicationDescriptionsGet(PatentsViewQueryRequest request)
    {
        return QueryDescriptions("pg_detail_desc_text", request, false, "pg_detail_desc_texts", true);
    }

    internal PatentsViewDescriptionResponse QueryPublicationDescriptionsPost(PatentsViewQueryRequest request)
    {
        return QueryDescriptions("pg_detail_desc_text", request, true, "pg_detail_desc_texts", true);
    }

    internal PatentsViewDrawingDescriptionResponse QueryGrantDrawingDescriptionsGet(PatentsViewQueryRequest request)
    {
        return QueryDrawings("g_draw_desc_text", request, false, "g_draw_desc_texts", false);
    }

    internal PatentsViewDrawingDescriptionResponse QueryGrantDrawingDescriptionsPost(PatentsViewQueryRequest request)
    {
        return QueryDrawings("g_draw_desc_text", request, true, "g_draw_desc_texts", false);
    }

    internal PatentsViewDrawingDescriptionResponse QueryPublicationDrawingDescriptionsGet(PatentsViewQueryRequest request)
    {
        return QueryDrawings("pg_draw_desc_text", request, false, "pg_draw_desc_texts", true);
    }

    internal PatentsViewDrawingDescriptionResponse QueryPublicationDrawingDescriptionsPost(PatentsViewQueryRequest request)
    {
        return QueryDrawings("pg_draw_desc_text", request, true, "pg_draw_desc_texts", true);
    }

    internal PatentsViewSummary GetGrantSummary(string patentIdentifier, RequestContext context)
    {
        PatentsViewQueryRequest request = CreateTextQuery(patentIdentifier, "patent_id", null, context, 2);
        PatentsViewSummaryResponse response = QueryGrantSummariesGet(request);
        return RequireSummary(response, patentIdentifier, false, "$.g_brf_sum_texts");
    }

    internal PatentsViewSummary GetPublicationSummary(string documentNumber, RequestContext context)
    {
        PatentsViewQueryRequest request = CreateTextQuery(documentNumber, "document_number", null, context, 2);
        PatentsViewSummaryResponse response = QueryPublicationSummariesGet(request);
        return RequireSummary(response, documentNumber, true, "$.pg_brf_sum_texts");
    }

    internal PatentsViewClaimResponse GetGrantClaims(string patentIdentifier, RequestContext context)
    {
        PatentsViewQueryRequest request = CreateTextQuery(patentIdentifier, "patent_id", "claim_sequence", context, UsptoDefaults.PatentsViewMaximumPageSize);
        PatentsViewClaimResponse response = QueryGrantClaimsGet(request);
        ValidateClaimOwners(response, patentIdentifier, false, "$.g_claims");
        return response;
    }

    internal PatentsViewClaimResponse GetPublicationClaims(string documentNumber, RequestContext context)
    {
        PatentsViewQueryRequest request = CreateTextQuery(documentNumber, "document_number", "claim_sequence", context, UsptoDefaults.PatentsViewMaximumPageSize);
        PatentsViewClaimResponse response = QueryPublicationClaimsGet(request);
        ValidateClaimOwners(response, documentNumber, true, "$.pg_claims");
        return response;
    }

    internal PatentsViewDescription GetGrantDescription(string patentIdentifier, RequestContext context)
    {
        PatentsViewQueryRequest request = CreateTextQuery(patentIdentifier, "patent_id", null, context, 2);
        PatentsViewDescriptionResponse response = QueryGrantDescriptionsGet(request);
        return RequireDescription(response, patentIdentifier, false, "$.g_detail_desc_texts");
    }

    internal PatentsViewDescription GetPublicationDescription(string documentNumber, RequestContext context)
    {
        PatentsViewQueryRequest request = CreateTextQuery(documentNumber, "document_number", null, context, 2);
        PatentsViewDescriptionResponse response = QueryPublicationDescriptionsGet(request);
        return RequireDescription(response, documentNumber, true, "$.pg_detail_desc_texts");
    }

    internal PatentsViewDrawingDescriptionResponse GetGrantDrawingDescriptions(string patentIdentifier, RequestContext context)
    {
        PatentsViewQueryRequest request = CreateTextQuery(patentIdentifier, "patent_id", "draw_desc_sequence", context, UsptoDefaults.PatentsViewMaximumPageSize);
        PatentsViewDrawingDescriptionResponse response = QueryGrantDrawingDescriptionsGet(request);
        ValidateDrawingOwners(response, patentIdentifier, false, "$.g_draw_desc_texts");
        return response;
    }

    internal PatentsViewDrawingDescriptionResponse GetPublicationDrawingDescriptions(string documentNumber, RequestContext context)
    {
        PatentsViewQueryRequest request = CreateTextQuery(documentNumber, "document_number", "draw_desc_sequence", context, UsptoDefaults.PatentsViewMaximumPageSize);
        PatentsViewDrawingDescriptionResponse response = QueryPublicationDrawingDescriptionsGet(request);
        ValidateDrawingOwners(response, documentNumber, true, "$.pg_draw_desc_texts");
        return response;
    }

    internal PatentsViewSummaryResponse QuerySummaries(string resource, PatentsViewQueryRequest request, bool usePost, string collectionName, bool isPublication)
    {
        PatentsViewPatentOperation.ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = Transport.ExecuteQuery(resource, request, usePost);
        return PatentsViewTextParser.ParseSummaryResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache, collectionName, isPublication);
    }

    internal PatentsViewClaimResponse QueryClaims(string resource, PatentsViewQueryRequest request, bool usePost, string collectionName, bool isPublication)
    {
        PatentsViewPatentOperation.ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = Transport.ExecuteQuery(resource, request, usePost);
        return PatentsViewTextParser.ParseClaimResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache, collectionName, isPublication);
    }

    internal PatentsViewDescriptionResponse QueryDescriptions(string resource, PatentsViewQueryRequest request, bool usePost, string collectionName, bool isPublication)
    {
        PatentsViewPatentOperation.ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = Transport.ExecuteQuery(resource, request, usePost);
        return PatentsViewTextParser.ParseDescriptionResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache, collectionName, isPublication);
    }

    internal PatentsViewDrawingDescriptionResponse QueryDrawings(string resource, PatentsViewQueryRequest request, bool usePost, string collectionName, bool isPublication)
    {
        PatentsViewPatentOperation.ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = Transport.ExecuteQuery(resource, request, usePost);
        return PatentsViewTextParser.ParseDrawingResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache, collectionName, isPublication);
    }

    internal static PatentsViewQueryRequest CreateTextQuery(string identifier, string identifierField, string? sequenceField, RequestContext context, int size)
    {
        PatentsViewPatentOperation.ValidateRetrievalRequest(identifier, context, "identifier");
        PatentsViewQueryRequest request = new PatentsViewQueryRequest();
        request.Search.Filters = new SearchFilter[]
        {
            new SearchFilter { Field = identifierField, Operator = SearchOperator.Equal, Value = identifier }
        };
        request.Search.FieldSelection.Fields = new string[0];
        if (sequenceField == null)
        {
            request.Search.Sorts = new SearchSort[]
            {
                new SearchSort { Field = identifierField, Direction = SortDirection.Ascending }
            };
        }
        else
        {
            request.Search.Sorts = new SearchSort[]
            {
                new SearchSort { Field = identifierField, Direction = SortDirection.Ascending },
                new SearchSort { Field = sequenceField, Direction = SortDirection.Ascending }
            };
        }

        request.Search.CursorPage.Size = size;
        request.Search.Context = context;
        return request;
    }

    internal static PatentsViewSummary RequireSummary(PatentsViewSummaryResponse response, string identifier, bool isPublication, string path)
    {
        if (response.Summaries.Length == 0)
        {
            throw CreateTextNotFound("brief summary", identifier, response.Metadata.RequestIdentifier);
        }

        string? actual = response.Summaries[0].PatentIdentifier;
        if (isPublication)
        {
            actual = response.Summaries[0].DocumentNumber;
        }

        if (response.Summaries.Length != 1 || !global::System.String.Equals(actual, identifier, global::System.StringComparison.Ordinal))
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView brief-summary response does not match the requested identifier.", path);
        }

        return response.Summaries[0];
    }

    internal static PatentsViewDescription RequireDescription(PatentsViewDescriptionResponse response, string identifier, bool isPublication, string path)
    {
        if (response.Descriptions.Length == 0)
        {
            throw CreateTextNotFound("detailed description", identifier, response.Metadata.RequestIdentifier);
        }

        string? actual = response.Descriptions[0].PatentIdentifier;
        if (isPublication)
        {
            actual = response.Descriptions[0].DocumentNumber;
        }

        if (response.Descriptions.Length != 1 || !global::System.String.Equals(actual, identifier, global::System.StringComparison.Ordinal))
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView detailed-description response does not match the requested identifier.", path);
        }

        return response.Descriptions[0];
    }

    internal static void ValidateClaimOwners(PatentsViewClaimResponse response, string identifier, bool isPublication, string path)
    {
        int index;
        for (index = 0; index < response.Claims.Length; index += 1)
        {
            string? actual = response.Claims[index].PatentIdentifier;
            if (isPublication)
            {
                actual = response.Claims[index].DocumentNumber;
            }

            if (!global::System.String.Equals(actual, identifier, global::System.StringComparison.Ordinal))
            {
                throw PatentsViewPatentParser.ParsePatentSearchFailure("A PatentsView claim does not match the requested identifier.", path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]");
            }
        }
    }

    internal static void ValidateDrawingOwners(PatentsViewDrawingDescriptionResponse response, string identifier, bool isPublication, string path)
    {
        int index;
        for (index = 0; index < response.DrawingDescriptions.Length; index += 1)
        {
            string? actual = response.DrawingDescriptions[index].PatentIdentifier;
            if (isPublication)
            {
                actual = response.DrawingDescriptions[index].DocumentNumber;
            }

            if (!global::System.String.Equals(actual, identifier, global::System.StringComparison.Ordinal))
            {
                throw PatentsViewPatentParser.ParsePatentSearchFailure("A PatentsView drawing description does not match the requested identifier.", path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]");
            }
        }
    }

    internal static NotFoundException CreateTextNotFound(string textName, string identifier, string? requestIdentifier)
    {
        return new NotFoundException("The requested PatentsView " + textName + " was not returned; PatentSearch long-text coverage is limited to documented available years.", ApiService.PatentsView, identifier, 200, true, requestIdentifier, null);
    }
}
