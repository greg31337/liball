namespace uspto;

internal class OfficeActionFieldOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;

    internal OfficeActionFieldOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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

    internal OfficeActionFieldResponse Discover(string datasetName, RequestContext context)
    {
        if (global::System.String.IsNullOrWhiteSpace(datasetName))
        {
            throw new ValidationException("An Office Action dataset name is required.", "datasetName", "REQUIRED", null);
        }

        if (context == null)
        {
            throw new ValidationException("An Office Action request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        string version = ResolveVersion(datasetName);
        using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateGetRequest(Options.OfficeActionBaseUri, new string[] { "api", "v1", "patent", "oa", datasetName, version, "fields" }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            return Send(message, datasetName, version, context);
        }
    }

    internal OfficeActionFieldResponse Send(global::System.Net.Http.HttpRequestMessage message, string datasetName, string version, RequestContext sourceContext)
    {
        RequestContext context = PatentApplicationSearchOperation.CreateRequestContext(sourceContext);
        context.Service = ApiService.OfficeAction;
        RequestHeaderPolicy.Apply(message, ApiService.OfficeAction, Options, false, DocumentFormat.Json);
        HttpTimeoutScope timeout = new HttpTimeoutScope(Options, context, false);
        using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, message, timeout))
        {
            string[] secrets = CredentialSecrets();
            int statusCode = (int)response.StatusCode;
            if (statusCode < 200 || statusCode >= 300)
            {
                throw ErrorMapper.MapResponse(response, ApiService.OfficeAction, "Office Action field discovery", Options.Limits, secrets, context.CancellationToken);
            }

            BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, context, Options.Limits, secrets, timeout);
            string? rawResponse = null;
            if (buffered.HasRawText)
            {
                rawResponse = buffered.RawText;
            }

            return OfficeActionFieldParser.Parse(buffered.Text, datasetName, version, buffered.Metadata, rawResponse);
        }
    }

    internal static string ResolveVersion(string datasetName)
    {
        switch (datasetName)
        {
            case "oa_actions":
                return "v1";
            case "oa_citations":
            case "oa_rejections":
                return "v2";
            case "enriched_cited_reference_metadata":
                return "v3";
            default:
                throw new ValidationException("The Office Action dataset is not supported.", "datasetName", "UNSUPPORTED_OFFICE_ACTION_DATASET", null);
        }
    }

    internal string[] CredentialSecrets()
    {
        if (Options.OdpApiKey == null)
        {
            return new string[0];
        }

        return new string[] { Options.OdpApiKey };
    }
}
