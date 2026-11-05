namespace uspto;

internal class OdpIdentifierResolutionLookup : IIdentifierResolutionLookup
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;

    internal OdpIdentifierResolutionLookup(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        if (client == null)
        {
            throw new ValidationException("An HTTP client is required.", "client", "REQUIRED", null);
        }

        if (options == null)
        {
            throw new ValidationException("Client options are required.", "options", "REQUIRED", null);
        }

        Client = client;
        Options = options;
    }

    public IdentifierResolutionLookupResult LookupExact(string fieldName, string fieldValue, RequestContext context)
    {
        ValidateLookupInput(fieldName, fieldValue, context);
        RequestContext requestContext = CreateRequestContext(context);
        global::System.Collections.Generic.Dictionary<string, string?> query = new global::System.Collections.Generic.Dictionary<string, string?>();
        query.Add("fields", "applicationNumberText");
        query.Add("limit", "2");
        query.Add("offset", "0");
        query.Add("q", OdpQueryWriter.WriteEquality(fieldName, fieldValue));

        using (global::System.Net.Http.HttpRequestMessage request = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "applications", "search" }, query))
        {
            RequestHeaderPolicy.Apply(request, ApiService.Odp, Options, false, DocumentFormat.Json);
            HttpTimeoutScope timeout = new HttpTimeoutScope(Options, requestContext, false);
            using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, request, timeout))
            {
                int statusCode = (int)response.StatusCode;
                if (statusCode == 404)
                {
                    return new IdentifierResolutionLookupResult();
                }

                string[] secrets = CredentialSecrets();
                if (statusCode < 200 || statusCode >= 300)
                {
                    throw ErrorMapper.MapResponse(response, ApiService.Odp, fieldValue, Options.Limits, secrets, requestContext.CancellationToken);
                }

                BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, requestContext, Options.Limits, secrets, timeout);
                return ParseResponse(buffered.Text);
            }
        }
    }

    internal static void ValidateLookupInput(string fieldName, string fieldValue, RequestContext context)
    {
        if (global::System.String.IsNullOrWhiteSpace(fieldName))
        {
            throw new ValidationException("An exact ODP field name is required.", "fieldName", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(fieldValue))
        {
            throw new ValidationException("An exact ODP field value is required.", "fieldValue", "REQUIRED", null);
        }

        if (fieldName.IndexOf('"') >= 0 || fieldValue.IndexOf('"') >= 0 || fieldName.IndexOf('\\') >= 0 || fieldValue.IndexOf('\\') >= 0)
        {
            throw new ValidationException("The exact ODP lookup contains unsupported query characters.", "fieldValue", "INVALID_QUERY_LITERAL", null);
        }

        if (context == null)
        {
            throw new ValidationException("A request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
    }

    internal static RequestContext CreateRequestContext(RequestContext source)
    {
        RequestContext result = new RequestContext();
        result.Service = ApiService.Odp;
        result.CancellationToken = source.CancellationToken;
        result.RetainRawResponse = source.RetainRawResponse;
        result.MaximumRawResponseBytes = source.MaximumRawResponseBytes;
        result.Deadline = source.Deadline;
        result.HasDeadline = source.HasDeadline;
        result.CorrelationIdentifier = source.CorrelationIdentifier;
        result.Fallback = source.Fallback;
        return result;
    }

    internal string[] CredentialSecrets()
    {
        if (Options.OdpApiKey == null)
        {
            return new string[0];
        }

        return new string[] { Options.OdpApiKey };
    }

    internal static IdentifierResolutionLookupResult ParseResponse(string json)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = new JsonFieldReader(document.RootElement, "$");
            JsonFieldValue count = root.ReadInt32("count");
            JsonFieldValue records = root.ReadArray("patentFileWrapperDataBag");
            if (!count.IsPresent || !count.IsValid || count.IsNull || count.Int32Value < 0)
            {
                throw new ParseException("The ODP identifier response has an invalid count.", DocumentFormat.Json, "ODP patent application search", "$.count", 0L, false, 0, false, null);
            }

            if (!records.IsValid || records.IsNull)
            {
                throw new ParseException("The ODP identifier response has an invalid record collection.", DocumentFormat.Json, "ODP patent application search", "$.patentFileWrapperDataBag", 0L, false, 0, false, null);
            }

            if (count.Int32Value == 0 && records.IsPresent && records.ArrayItems.Length != 0)
            {
                throw new ParseException("The ODP identifier response count does not match its records.", DocumentFormat.Json, "ODP patent application search", "$.patentFileWrapperDataBag", 0L, false, 0, false, null);
            }

            if (count.Int32Value == 1 && (!records.IsPresent || records.ArrayItems.Length != 1))
            {
                throw new ParseException("The ODP identifier response count does not match its records.", DocumentFormat.Json, "ODP patent application search", "$.patentFileWrapperDataBag", 0L, false, 0, false, null);
            }

            IdentifierResolutionLookupResult result = new IdentifierResolutionLookupResult();
            result.MatchCount = count.Int32Value;
            result.ApplicationNumbers = new string[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                JsonFieldReader record = new JsonFieldReader(records.ArrayItems[index], records.ArrayItemPaths[index]);
                JsonFieldValue applicationNumber = record.ReadString("applicationNumberText");
                if (!applicationNumber.IsPresent || !applicationNumber.IsValid || applicationNumber.IsNull || global::System.String.IsNullOrWhiteSpace(applicationNumber.StringValue))
                {
                    throw new ParseException("The ODP identifier response has an invalid application number.", DocumentFormat.Json, "ODP patent application search", applicationNumber.Path, 0L, false, 0, false, null);
                }

                result.ApplicationNumbers[index] = applicationNumber.StringValue;
            }

            return result;
        }
    }
}
