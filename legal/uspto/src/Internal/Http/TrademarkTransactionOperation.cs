namespace uspto;

internal class TrademarkTransactionOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;

    internal TrademarkTransactionOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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

    internal TrademarkLastUpdateResult GetLastUpdate(string identifier, DocumentFormat format, RequestContext sourceContext)
    {
        if (sourceContext == null)
        {
            throw new ValidationException("A trademark last-update context is required.", "context", "REQUIRED", null);
        }

        sourceContext.CancellationToken.ThrowIfCancellationRequested();
        if (format != DocumentFormat.Json && format != DocumentFormat.Xml)
        {
            throw new ValidationException("Trademark last-update information requires JSON or XML.", "format", "UNSUPPORTED_FORMAT", null);
        }

        TrademarkIdentifierResult parsed = TrademarkIdentifierParser.ParseTrademarkIdentifier(identifier);
        if (!parsed.IsValid)
        {
            throw new ValidationException("The trademark identifier is invalid.", "identifier", parsed.FailureCode, null);
        }

        if (parsed.Kind != TrademarkIdentifierKind.SerialNumber)
        {
            throw new ValidationException("Trademark last-update information requires a serial number.", "identifier", "LAST_UPDATE_REQUIRES_SERIAL_NUMBER", null);
        }

        string extension = format == DocumentFormat.Json ? "json" : "xml";
        global::System.Collections.Generic.Dictionary<string, string?> query = new global::System.Collections.Generic.Dictionary<string, string?>();
        query.Add("sn", parsed.IdentifierValue);
        BufferedHttpResponse buffered = ReadResponse(new string[] { "last-update", "info." + extension }, query, format, sourceContext, parsed.CanonicalNumber);
        string? rawResponse = TrademarkDocumentOperation.RawResponse(buffered);
        if (format == DocumentFormat.Json)
        {
            return TsdrTransactionParser.ParseLastUpdateJson(buffered.Text, identifier, parsed, buffered.Metadata, rawResponse);
        }

        return TsdrTransactionParser.ParseLastUpdateXml(buffered.Text, identifier, parsed, buffered.Metadata, rawResponse, Options.Limits);
    }

    internal TrademarkMultiStatusResult GetMultiStatus(string[] identifiers, RequestContext sourceContext)
    {
        if (sourceContext == null)
        {
            throw new ValidationException("A trademark multi-status context is required.", "context", "REQUIRED", null);
        }

        sourceContext.CancellationToken.ThrowIfCancellationRequested();
        TrademarkIdentifierResult[] parsed = ValidateMultiIdentifiers(identifiers, Options.Limits);
        global::System.Collections.Generic.List<string> values = new global::System.Collections.Generic.List<string>();
        int index;
        for (index = 0; index < parsed.Length; index += 1)
        {
            values.Add(parsed[index].IdentifierValue);
        }

        global::System.Collections.Generic.Dictionary<string, string?> query = new global::System.Collections.Generic.Dictionary<string, string?>();
        query.Add("ids", global::System.String.Join(",", values));
        BufferedHttpResponse buffered = ReadResponse(new string[] { "ts", "cd", "caseMultiStatus", parsed[0].ApiPrefix }, query, DocumentFormat.Json, sourceContext, parsed[0].CanonicalNumber);
        return TsdrTransactionParser.ParseMultiStatusJson(buffered.Text, identifiers, parsed, buffered.Metadata, TrademarkDocumentOperation.RawResponse(buffered));
    }

    internal BufferedHttpResponse ReadResponse(string[] path, global::System.Collections.Generic.Dictionary<string, string?> query, DocumentFormat format, RequestContext sourceContext, string errorIdentifier)
    {
        RequestContext context = TrademarkStatusOperation.CreateRequestContext(sourceContext);
        using (global::System.Net.Http.HttpRequestMessage request = RequestBuilder.CreateGetRequest(Options.TsdrBaseUri, path, query))
        {
            global::System.Uri requestUri = request.RequestUri ?? new global::System.Uri("https://invalid.example/");
            TrademarkMultiCaseBundleOperation.ValidateUriSize(requestUri, Options.Limits);
            RequestHeaderPolicy.Apply(request, ApiService.Tsdr, Options, false, format);
            HttpTimeoutScope timeout = new HttpTimeoutScope(Options, context, false);
            using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, request, timeout))
            {
                int statusCode = (int)response.StatusCode;
                if (statusCode < 200 || statusCode >= 300)
                {
                    throw ErrorMapper.MapResponse(response, ApiService.Tsdr, errorIdentifier, Options.Limits, CredentialSecrets(), context.CancellationToken);
                }

                return TimeoutController.ReadTextBodyPhase(response, format, context, Options.Limits, CredentialSecrets(), timeout);
            }
        }
    }

    internal static TrademarkIdentifierResult[] ValidateMultiIdentifiers(string[] identifiers, UsptoLimits limits)
    {
        if (identifiers == null)
        {
            throw new ValidationException("Trademark multi-status identifiers are required.", "identifiers", "REQUIRED", null);
        }

        if (limits == null)
        {
            throw new ValidationException("Trademark multi-status limits are required.", "limits", "REQUIRED", null);
        }

        if (limits.MaximumResults <= 0)
        {
            throw new ValidationException("The maximum multi-status identifier count must be positive.", "MaximumResults", "INVALID_LIMIT", null);
        }

        if (identifiers.Length == 0)
        {
            throw new ValidationException("At least one multi-status identifier is required.", "identifiers", "EMPTY_IDENTIFIERS", null);
        }

        if (identifiers.Length > limits.MaximumResults)
        {
            throw new ValidationException("The multi-status identifier count exceeds the configured limit.", "identifiers", "TOO_MANY_IDENTIFIERS", null);
        }

        TrademarkIdentifierResult[] results = new TrademarkIdentifierResult[identifiers.Length];
        global::System.Collections.Generic.HashSet<string> canonical = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.OrdinalIgnoreCase);
        TrademarkIdentifierKind kind = TrademarkIdentifierKind.Unknown;
        int index;
        for (index = 0; index < identifiers.Length; index += 1)
        {
            TrademarkIdentifierResult parsed = TrademarkIdentifierParser.ParseTrademarkIdentifier(identifiers[index]);
            if (!parsed.IsValid)
            {
                throw new ValidationException("A trademark multi-status identifier is invalid.", "identifiers", parsed.FailureCode, null);
            }

            if (kind == TrademarkIdentifierKind.Unknown)
            {
                kind = parsed.Kind;
            }
            else if (kind != parsed.Kind)
            {
                throw new ValidationException("Trademark multi-status identifiers must use one identifier kind.", "identifiers", "MIXED_IDENTIFIER_KINDS", null);
            }

            if (!canonical.Add(parsed.CanonicalNumber))
            {
                throw new ValidationException("Trademark multi-status identifiers must be unique.", "identifiers", "DUPLICATE_IDENTIFIER", null);
            }

            results[index] = parsed;
        }

        return results;
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
