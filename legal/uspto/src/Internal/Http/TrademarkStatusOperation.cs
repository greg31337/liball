namespace uspto;

internal class TrademarkStatusOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;

    internal TrademarkStatusOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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

    internal TrademarkCaseStatus GetStatus(string identifier, DocumentFormat format, RequestContext sourceContext)
    {
        if (sourceContext == null)
        {
            throw new ValidationException("A trademark status request context is required.", "context", "REQUIRED", null);
        }

        sourceContext.CancellationToken.ThrowIfCancellationRequested();
        if (format != DocumentFormat.Json && format != DocumentFormat.Xml)
        {
            throw new ValidationException("Parsed trademark status requires JSON or XML.", "format", "UNSUPPORTED_FORMAT", null);
        }

        TrademarkIdentifierResult parsed = TrademarkIdentifierParser.ParseTrademarkIdentifier(identifier);
        if (!parsed.IsValid)
        {
            throw new ValidationException("The trademark identifier is invalid.", "identifier", parsed.FailureCode, null);
        }

        RequestContext context = CreateRequestContext(sourceContext);
        using (global::System.Net.Http.HttpRequestMessage request = RequestBuilder.CreateGetRequest(Options.TsdrBaseUri, new string[] { "ts", "cd", "casestatus", parsed.CanonicalNumber, "info" }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            RequestHeaderPolicy.Apply(request, ApiService.Tsdr, Options, false, format);
            HttpTimeoutScope timeout = new HttpTimeoutScope(Options, context, false);
            using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, request, timeout))
            {
                string[] secrets = CredentialSecrets();
                int statusCode = (int)response.StatusCode;
                if (statusCode < 200 || statusCode >= 300)
                {
                    throw ErrorMapper.MapResponse(response, ApiService.Tsdr, parsed.CanonicalNumber, Options.Limits, secrets, context.CancellationToken);
                }

                BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, format, context, Options.Limits, secrets, timeout);
                string? rawResponse = null;
                if (buffered.HasRawText)
                {
                    rawResponse = buffered.RawText;
                }

                if (format == DocumentFormat.Json)
                {
                    return TsdrJsonParser.ParseStatus(buffered.Text, identifier, parsed, buffered.Metadata, rawResponse);
                }

                return TsdrXmlParser.ParseStatus(buffered.Text, identifier, parsed, buffered.Metadata, rawResponse, Options.Limits);
            }
        }
    }

    internal static RequestContext CreateRequestContext(RequestContext source)
    {
        RequestContext context = PatentApplicationSearchOperation.CreateRequestContext(source);
        context.Service = ApiService.Tsdr;
        return context;
    }

    internal string[] CredentialSecrets()
    {
        if (Options.TsdrApiKey == null)
        {
            return new string[0];
        }

        return new string[] { Options.TsdrApiKey };
    }
}
