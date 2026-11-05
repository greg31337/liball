namespace uspto;

internal class PatentsViewClassificationOperation
{
    internal PatentsViewPatentOperation Transport;

    internal PatentsViewClassificationOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        Transport = new PatentsViewPatentOperation(client, options);
    }

    internal PatentsViewCpcClassificationResponse QueryCpcClassesGet(PatentsViewQueryRequest request)
    {
        PatentsViewBufferedResult buffered = ExecuteClassificationQuery("cpc_class", request, false);
        return ParseCpcBuffered(buffered, request, "cpc_classes", "class");
    }

    internal PatentsViewCpcClassificationResponse QueryCpcClassesPost(PatentsViewQueryRequest request)
    {
        PatentsViewBufferedResult buffered = ExecuteClassificationQuery("cpc_class", request, true);
        return ParseCpcBuffered(buffered, request, "cpc_classes", "class");
    }

    internal PatentsViewCpcClassificationResponse QueryCpcSubclassesGet(PatentsViewQueryRequest request)
    {
        PatentsViewBufferedResult buffered = ExecuteClassificationQuery("cpc_subclass", request, false);
        return ParseCpcBuffered(buffered, request, "cpc_subclasses", "subclass");
    }

    internal PatentsViewCpcClassificationResponse QueryCpcSubclassesPost(PatentsViewQueryRequest request)
    {
        PatentsViewBufferedResult buffered = ExecuteClassificationQuery("cpc_subclass", request, true);
        return ParseCpcBuffered(buffered, request, "cpc_subclasses", "subclass");
    }

    internal PatentsViewCpcClassificationResponse QueryCpcGroupsGet(PatentsViewQueryRequest request)
    {
        PatentsViewBufferedResult buffered = ExecuteClassificationQuery("cpc_group", request, false);
        return ParseCpcBuffered(buffered, request, "cpc_groups", "group");
    }

    internal PatentsViewCpcClassificationResponse QueryCpcGroupsPost(PatentsViewQueryRequest request)
    {
        PatentsViewBufferedResult buffered = ExecuteClassificationQuery("cpc_group", request, true);
        return ParseCpcBuffered(buffered, request, "cpc_groups", "group");
    }

    internal PatentsViewIpcClassificationResponse QueryIpcGet(PatentsViewQueryRequest request)
    {
        PatentsViewBufferedResult buffered = ExecuteClassificationQuery("ipc", request, false);
        return ParseIpcBuffered(buffered, request);
    }

    internal PatentsViewIpcClassificationResponse QueryIpcPost(PatentsViewQueryRequest request)
    {
        PatentsViewBufferedResult buffered = ExecuteClassificationQuery("ipc", request, true);
        return ParseIpcBuffered(buffered, request);
    }

    internal PatentsViewUspcClassificationResponse QueryUspcMainClassesGet(PatentsViewQueryRequest request)
    {
        PatentsViewBufferedResult buffered = ExecuteClassificationQuery("uspc_mainclass", request, false);
        return ParseUspcBuffered(buffered, request, "uspc_mainclasses", "mainclass");
    }

    internal PatentsViewUspcClassificationResponse QueryUspcMainClassesPost(PatentsViewQueryRequest request)
    {
        PatentsViewBufferedResult buffered = ExecuteClassificationQuery("uspc_mainclass", request, true);
        return ParseUspcBuffered(buffered, request, "uspc_mainclasses", "mainclass");
    }

    internal PatentsViewUspcClassificationResponse QueryUspcSubclassesGet(PatentsViewQueryRequest request)
    {
        PatentsViewBufferedResult buffered = ExecuteClassificationQuery("uspc_subclass", request, false);
        return ParseUspcBuffered(buffered, request, "uspc_subclasses", "subclass");
    }

    internal PatentsViewUspcClassificationResponse QueryUspcSubclassesPost(PatentsViewQueryRequest request)
    {
        PatentsViewBufferedResult buffered = ExecuteClassificationQuery("uspc_subclass", request, true);
        return ParseUspcBuffered(buffered, request, "uspc_subclasses", "subclass");
    }

    internal PatentsViewWipoClassificationResponse QueryWipoGet(PatentsViewQueryRequest request)
    {
        PatentsViewBufferedResult buffered = ExecuteClassificationQuery("wipo", request, false);
        return ParseWipoBuffered(buffered, request);
    }

    internal PatentsViewWipoClassificationResponse QueryWipoPost(PatentsViewQueryRequest request)
    {
        PatentsViewBufferedResult buffered = ExecuteClassificationQuery("wipo", request, true);
        return ParseWipoBuffered(buffered, request);
    }

    internal PatentsViewCpcClassification GetCpcClass(string identifier, RequestContext context)
    {
        PatentsViewCpcClassificationResponse response = RetrieveCpc("cpc_class", identifier, identifier, context, "cpc_classes", "class");
        return RequireSingleCpc(response, identifier, "cpc class");
    }

    internal PatentsViewCpcClassification GetCpcSubclass(string identifier, RequestContext context)
    {
        PatentsViewCpcClassificationResponse response = RetrieveCpc("cpc_subclass", identifier, identifier, context, "cpc_subclasses", "subclass");
        return RequireSingleCpc(response, identifier, "cpc subclass");
    }

    internal PatentsViewCpcClassification GetCpcGroup(string identifier, RequestContext context)
    {
        string routeIdentifier = ReplaceClassificationSeparator(identifier);
        PatentsViewCpcClassificationResponse response = RetrieveCpc("cpc_group", routeIdentifier, identifier, context, "cpc_groups", "group");
        return RequireSingleCpc(response, identifier, "cpc group");
    }

    internal PatentsViewIpcClassification GetIpc(string identifier, RequestContext context)
    {
        PatentsViewPatentOperation.ValidateRetrievalRequest(identifier, context, "classificationIdentifier");
        PatentsViewQueryRequest request = CreateIdentifierQuery(identifier, "ipc_id", context);
        PatentsViewIpcClassificationResponse response = QueryIpcGet(request);
        if (response.Classifications.Length == 0)
        {
            throw CreateClassificationNotFound("IPC classification", identifier, response.Metadata.RequestIdentifier);
        }

        if (response.Classifications.Length != 1 || !global::System.String.Equals(response.Classifications[0].ClassificationIdentifier, identifier, global::System.StringComparison.Ordinal))
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView IPC response does not match the requested identifier.", "$.ipcr");
        }

        return response.Classifications[0];
    }

    internal PatentsViewUspcClassification GetUspcMainClass(string identifier, RequestContext context)
    {
        PatentsViewUspcClassificationResponse response = RetrieveUspc("uspc_mainclass", identifier, identifier, context, "uspc_mainclasses", "mainclass");
        return RequireSingleUspc(response, identifier, "USPC main class");
    }

    internal PatentsViewUspcClassification GetUspcSubclass(string identifier, RequestContext context)
    {
        string routeIdentifier = ReplaceClassificationSeparator(identifier);
        PatentsViewUspcClassificationResponse response = RetrieveUspc("uspc_subclass", routeIdentifier, identifier, context, "uspc_subclasses", "subclass");
        return RequireSingleUspc(response, identifier, "USPC subclass");
    }

    internal PatentsViewWipoClassification GetWipo(string identifier, RequestContext context)
    {
        PatentsViewPatentOperation.ValidateRetrievalRequest(identifier, context, "classificationIdentifier");
        PatentsViewBufferedResult buffered = Transport.ExecuteRetrieval("wipo", identifier, context);
        PatentsViewQueryRequest request = PatentsViewPatentOperation.CreateRetrievalQuery(context);
        PatentsViewWipoClassificationResponse response = ParseWipoBuffered(buffered, request);
        if (response.Classifications.Length == 0)
        {
            throw CreateClassificationNotFound("WIPO classification", identifier, response.Metadata.RequestIdentifier);
        }

        if (response.Classifications.Length != 1 || !global::System.String.Equals(response.Classifications[0].ClassificationIdentifier, identifier, global::System.StringComparison.Ordinal))
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView WIPO response does not match the requested identifier.", "$.wipo");
        }

        return response.Classifications[0];
    }

    internal PatentsViewBufferedResult ExecuteClassificationQuery(string resource, PatentsViewQueryRequest request, bool usePost)
    {
        PatentsViewPatentOperation.ValidateQueryRequest(request);
        return Transport.ExecuteQuery(resource, request, usePost);
    }

    internal PatentsViewCpcClassificationResponse RetrieveCpc(string resource, string routeIdentifier, string identifier, RequestContext context, string collectionName, string level)
    {
        PatentsViewPatentOperation.ValidateRetrievalRequest(identifier, context, "classificationIdentifier");
        PatentsViewBufferedResult buffered = Transport.ExecuteRetrieval(resource, routeIdentifier, context);
        PatentsViewQueryRequest request = PatentsViewPatentOperation.CreateRetrievalQuery(context);
        return ParseCpcBuffered(buffered, request, collectionName, level);
    }

    internal PatentsViewUspcClassificationResponse RetrieveUspc(string resource, string routeIdentifier, string identifier, RequestContext context, string collectionName, string level)
    {
        PatentsViewPatentOperation.ValidateRetrievalRequest(identifier, context, "classificationIdentifier");
        PatentsViewBufferedResult buffered = Transport.ExecuteRetrieval(resource, routeIdentifier, context);
        PatentsViewQueryRequest request = PatentsViewPatentOperation.CreateRetrievalQuery(context);
        return ParseUspcBuffered(buffered, request, collectionName, level);
    }

    internal static PatentsViewCpcClassificationResponse ParseCpcBuffered(PatentsViewBufferedResult buffered, PatentsViewQueryRequest request, string collectionName, string level)
    {
        return PatentsViewClassificationParser.ParseCpcResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache, collectionName, level);
    }

    internal static PatentsViewIpcClassificationResponse ParseIpcBuffered(PatentsViewBufferedResult buffered, PatentsViewQueryRequest request)
    {
        return PatentsViewClassificationParser.ParseIpcResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache);
    }

    internal static PatentsViewUspcClassificationResponse ParseUspcBuffered(PatentsViewBufferedResult buffered, PatentsViewQueryRequest request, string collectionName, string level)
    {
        return PatentsViewClassificationParser.ParseUspcResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache, collectionName, level);
    }

    internal static PatentsViewWipoClassificationResponse ParseWipoBuffered(PatentsViewBufferedResult buffered, PatentsViewQueryRequest request)
    {
        return PatentsViewClassificationParser.ParseWipoResponse(buffered.Response.Text, request, buffered.Response.Metadata, PatentsViewPatentOperation.GetRawResponse(buffered.Response), buffered.FromCache);
    }

    internal static PatentsViewCpcClassification RequireSingleCpc(PatentsViewCpcClassificationResponse response, string identifier, string classificationName)
    {
        if (response.Classifications.Length == 0)
        {
            throw CreateClassificationNotFound(classificationName, identifier, response.Metadata.RequestIdentifier);
        }

        if (response.Classifications.Length != 1 || !global::System.String.Equals(response.Classifications[0].ClassificationIdentifier, identifier, global::System.StringComparison.Ordinal))
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView " + classificationName + " response does not match the requested identifier.", "$.classifications");
        }

        return response.Classifications[0];
    }

    internal static PatentsViewUspcClassification RequireSingleUspc(PatentsViewUspcClassificationResponse response, string identifier, string classificationName)
    {
        if (response.Classifications.Length == 0)
        {
            throw CreateClassificationNotFound(classificationName, identifier, response.Metadata.RequestIdentifier);
        }

        if (response.Classifications.Length != 1 || !global::System.String.Equals(response.Classifications[0].ClassificationIdentifier, identifier, global::System.StringComparison.Ordinal))
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView " + classificationName + " response does not match the requested identifier.", "$.classifications");
        }

        return response.Classifications[0];
    }

    internal static PatentsViewQueryRequest CreateIdentifierQuery(string identifier, string fieldName, RequestContext context)
    {
        PatentsViewQueryRequest request = new PatentsViewQueryRequest();
        request.Search.Filters = new SearchFilter[]
        {
            new SearchFilter { Field = fieldName, Operator = SearchOperator.Equal, Value = identifier }
        };
        request.Search.FieldSelection.Fields = new string[0];
        request.Search.Sorts = new SearchSort[]
        {
            new SearchSort { Field = fieldName, Direction = SortDirection.Ascending }
        };
        request.Search.CursorPage.Size = 2;
        request.Search.Context = context;
        return request;
    }

    internal static string ReplaceClassificationSeparator(string identifier)
    {
        if (identifier == null)
        {
            return string.Empty;
        }

        return identifier.Replace('/', ':');
    }

    internal static NotFoundException CreateClassificationNotFound(string classificationName, string identifier, string? requestIdentifier)
    {
        return new NotFoundException("The requested PatentsView " + classificationName + " was not found.", ApiService.PatentsView, identifier, 200, true, requestIdentifier, null);
    }
}
