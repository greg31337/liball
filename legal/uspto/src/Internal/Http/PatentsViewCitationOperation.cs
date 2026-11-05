namespace uspto;

internal class PatentsViewCitationOperation
{
    internal PatentsViewPatentOperation Transport;

    internal PatentsViewCitationOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        Transport = new PatentsViewPatentOperation(client, options);
    }

    internal PatentsViewCitationResponse QueryUsPatentCitationsGet(PatentsViewQueryRequest request)
    {
        return QueryCitations("patent/us_patent_citation", request, false, "us_patent_citations", "us_patent");
    }

    internal PatentsViewCitationResponse QueryUsPatentCitationsPost(PatentsViewQueryRequest request)
    {
        return QueryCitations("patent/us_patent_citation", request, true, "us_patent_citations", "us_patent");
    }

    internal PatentsViewCitationResponse QueryUsApplicationCitationsGet(PatentsViewQueryRequest request)
    {
        return QueryCitations("patent/us_application_citation", request, false, "us_application_citations", "us_application");
    }

    internal PatentsViewCitationResponse QueryUsApplicationCitationsPost(PatentsViewQueryRequest request)
    {
        return QueryCitations("patent/us_application_citation", request, true, "us_application_citations", "us_application");
    }

    internal PatentsViewCitationResponse QueryForeignCitationsGet(PatentsViewQueryRequest request)
    {
        return QueryCitations("patent/foreign_citation", request, false, "foreign_citations", "foreign");
    }

    internal PatentsViewCitationResponse QueryForeignCitationsPost(PatentsViewQueryRequest request)
    {
        return QueryCitations("patent/foreign_citation", request, true, "foreign_citations", "foreign");
    }

    internal PatentsViewCitationResponse QueryOtherReferencesGet(PatentsViewQueryRequest request)
    {
        return QueryCitations("patent/other_reference", request, false, "other_references", "other");
    }

    internal PatentsViewCitationResponse QueryOtherReferencesPost(PatentsViewQueryRequest request)
    {
        return QueryCitations("patent/other_reference", request, true, "other_references", "other");
    }

    internal PatentsViewRelatedApplicationResponse QueryPatentRelatedApplicationsGet(PatentsViewQueryRequest request)
    {
        return QueryRelatedApplications("patent/rel_app_text", request, false, false);
    }

    internal PatentsViewRelatedApplicationResponse QueryPatentRelatedApplicationsPost(PatentsViewQueryRequest request)
    {
        return QueryRelatedApplications("patent/rel_app_text", request, true, false);
    }

    internal PatentsViewRelatedApplicationResponse QueryPublicationRelatedApplicationsGet(PatentsViewQueryRequest request)
    {
        return QueryRelatedApplications("publication/rel_app_text", request, false, true);
    }

    internal PatentsViewRelatedApplicationResponse QueryPublicationRelatedApplicationsPost(PatentsViewQueryRequest request)
    {
        return QueryRelatedApplications("publication/rel_app_text", request, true, true);
    }

    internal PatentsViewCitationResponse GetUsPatentCitations(string patentIdentifier, RequestContext context)
    {
        PatentsViewPatentOperation.ValidateRetrievalRequest(patentIdentifier, context, "identifier");
        PatentsViewBufferedResult buffered = Transport.ExecuteRetrieval("patent/us_patent_citation", patentIdentifier, context);
        PatentsViewQueryRequest request = PatentsViewPatentOperation.CreateRetrievalQuery(context);
        PatentsViewCitationResponse response = ParseCitationBuffered(buffered, request, "us_patent_citations", "us_patent");
        ValidateCitationOwner(response, patentIdentifier, "$.us_patent_citations");
        return response;
    }

    internal PatentsViewCitationResponse GetUsApplicationCitations(string patentIdentifier, RequestContext context)
    {
        PatentsViewPatentOperation.ValidateRetrievalRequest(patentIdentifier, context, "identifier");
        PatentsViewBufferedResult buffered = Transport.ExecuteRetrieval("patent/us_application_citation", patentIdentifier, context);
        PatentsViewQueryRequest request = PatentsViewPatentOperation.CreateRetrievalQuery(context);
        PatentsViewCitationResponse response = ParseCitationBuffered(buffered, request, "us_application_citations", "us_application");
        ValidateCitationOwner(response, patentIdentifier, "$.us_application_citations");
        return response;
    }

    internal PatentsViewCitationResponse GetForeignCitations(string patentIdentifier, RequestContext context)
    {
        PatentsViewQueryRequest request = CreateAssociatedQuery(patentIdentifier, "patent_id", "citation_sequence", context);
        PatentsViewCitationResponse response = QueryForeignCitationsGet(request);
        ValidateCitationOwner(response, patentIdentifier, "$.foreign_citations");
        return response;
    }

    internal PatentsViewCitationResponse GetOtherReferences(string patentIdentifier, RequestContext context)
    {
        PatentsViewQueryRequest request = CreateAssociatedQuery(patentIdentifier, "patent_id", "reference_sequence", context);
        PatentsViewCitationResponse response = QueryOtherReferencesGet(request);
        ValidateCitationOwner(response, patentIdentifier, "$.other_references");
        return response;
    }

    internal PatentsViewRelatedApplicationResponse GetPatentRelatedApplications(string patentIdentifier, RequestContext context)
    {
        PatentsViewQueryRequest request = CreateAssociatedQuery(patentIdentifier, "patent_id", null, context);
        PatentsViewRelatedApplicationResponse response = QueryPatentRelatedApplicationsGet(request);
        ValidateRelatedOwner(response, patentIdentifier, false);
        return response;
    }

    internal PatentsViewRelatedApplicationResponse GetPublicationRelatedApplications(string documentNumber, RequestContext context)
    {
        PatentsViewQueryRequest request = CreateAssociatedQuery(documentNumber, "document_number", null, context);
        PatentsViewRelatedApplicationResponse response = QueryPublicationRelatedApplicationsGet(request);
        ValidateRelatedOwner(response, documentNumber, true);
        return response;
    }

    internal PatentsViewCitationResponse QueryCitations(string resource, PatentsViewQueryRequest request, bool usePost, string collectionName, string citationType)
    {
        PatentsViewPatentOperation.ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = Transport.ExecuteQuery(resource, request, usePost);
        return ParseCitationBuffered(buffered, request, collectionName, citationType);
    }

    internal PatentsViewRelatedApplicationResponse QueryRelatedApplications(string resource, PatentsViewQueryRequest request, bool usePost, bool isPublication)
    {
        PatentsViewPatentOperation.ValidateQueryRequest(request);
        PatentsViewBufferedResult buffered = Transport.ExecuteQuery(resource, request, usePost);
        return PatentsViewCitationParser.ParseRelatedApplicationResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache, isPublication);
    }

    internal static PatentsViewCitationResponse ParseCitationBuffered(PatentsViewBufferedResult buffered, PatentsViewQueryRequest request, string collectionName, string citationType)
    {
        return PatentsViewCitationParser.ParseCitationResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache, collectionName, citationType);
    }

    internal static PatentsViewQueryRequest CreateAssociatedQuery(string identifier, string identifierField, string? sequenceField, RequestContext context)
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

        request.Search.CursorPage.Size = UsptoDefaults.PatentsViewMaximumPageSize;
        request.Search.Context = context;
        return request;
    }

    internal static void ValidateCitationOwner(PatentsViewCitationResponse response, string identifier, string path)
    {
        int index;
        for (index = 0; index < response.Citations.Length; index += 1)
        {
            if (!global::System.String.Equals(response.Citations[index].PatentIdentifier, identifier, global::System.StringComparison.Ordinal))
            {
                throw PatentsViewPatentParser.ParsePatentSearchFailure("A PatentsView citation does not match the requested patent identifier.", path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "].patent_id");
            }
        }
    }

    internal static void ValidateRelatedOwner(PatentsViewRelatedApplicationResponse response, string identifier, bool isPublication)
    {
        int index;
        for (index = 0; index < response.RelatedApplications.Length; index += 1)
        {
            string? actual = response.RelatedApplications[index].PatentIdentifier;
            string fieldName = "patent_id";
            if (isPublication)
            {
                actual = response.RelatedApplications[index].DocumentNumber;
                fieldName = "document_number";
            }

            if (!global::System.String.Equals(actual, identifier, global::System.StringComparison.Ordinal))
            {
                throw PatentsViewPatentParser.ParsePatentSearchFailure("A PatentsView related-application record does not match the requested identifier.", "$.rel_app_texts[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]." + fieldName);
            }
        }
    }
}
