namespace uspto;

internal class PatentApplicationRetrievalOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;
    internal IdentifierResolver Resolver;

    internal PatentApplicationRetrievalOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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
        Resolver = new IdentifierResolver(new OdpIdentifierResolutionLookup(client, options));
    }

    internal string ResolveIdentifier(string identifier, RequestContext context)
    {
        return Resolver.Resolve(identifier, context);
    }

    internal PatentFileWrapper GetApplication(string identifier, RequestContext context)
    {
        string applicationNumber = ResolveIdentifier(identifier, context);
        return GetNormalizedApplication(applicationNumber, context);
    }

    internal PatentFileWrapper GetNormalizedApplication(string applicationNumber, RequestContext context)
    {
        if (context == null)
        {
            throw new ValidationException("A patent application request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(applicationNumber);
        if (!parsed.IsValid)
        {
            throw new ValidationException("The patent application identifier is invalid.", "applicationNumber", parsed.FailureCode, null);
        }

        RequestContext requestContext = PatentApplicationSearchOperation.CreateRequestContext(context);
        using (global::System.Net.Http.HttpRequestMessage request = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "applications", parsed.CanonicalNumber }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            RequestHeaderPolicy.Apply(request, ApiService.Odp, Options, false, DocumentFormat.Json);
            HttpTimeoutScope timeout = new HttpTimeoutScope(Options, requestContext, false);
            using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, request, timeout))
            {
                string[] secrets = CredentialSecrets();
                int statusCode = (int)response.StatusCode;
                if (statusCode < 200 || statusCode >= 300)
                {
                    throw ErrorMapper.MapResponse(response, ApiService.Odp, parsed.CanonicalNumber, Options.Limits, secrets, requestContext.CancellationToken);
                }

                BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, requestContext, Options.Limits, secrets, timeout);
                return OdpJsonParser.ParsePatentApplication(buffered.Text, parsed.CanonicalNumber, buffered.Metadata);
            }
        }
    }

    internal PatentTransactionHistory GetTransactions(string identifier, RequestContext context)
    {
        string applicationNumber = ResolveIdentifier(identifier, context);
        return GetNormalizedTransactions(applicationNumber, context);
    }

    internal PatentTransactionHistory GetNormalizedTransactions(string applicationNumber, RequestContext context)
    {
        if (context == null)
        {
            throw new ValidationException("A patent transaction request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(applicationNumber);
        if (!parsed.IsValid)
        {
            throw new ValidationException("The patent application identifier is invalid.", "applicationNumber", parsed.FailureCode, null);
        }

        RequestContext requestContext = PatentApplicationSearchOperation.CreateRequestContext(context);
        using (global::System.Net.Http.HttpRequestMessage request = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "applications", parsed.CanonicalNumber, "transactions" }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            RequestHeaderPolicy.Apply(request, ApiService.Odp, Options, false, DocumentFormat.Json);
            HttpTimeoutScope timeout = new HttpTimeoutScope(Options, requestContext, false);
            using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, request, timeout))
            {
                string[] secrets = CredentialSecrets();
                int statusCode = (int)response.StatusCode;
                if (statusCode < 200 || statusCode >= 300)
                {
                    throw ErrorMapper.MapResponse(response, ApiService.Odp, parsed.CanonicalNumber, Options.Limits, secrets, requestContext.CancellationToken);
                }

                BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, requestContext, Options.Limits, secrets, timeout);
                return OdpPatentTransactionParser.Parse(buffered.Text, parsed.CanonicalNumber, buffered.Metadata);
            }
        }
    }

    internal PatentDocumentList GetDocuments(string identifier, RequestContext context)
    {
        string applicationNumber = ResolveIdentifier(identifier, context);
        return GetNormalizedDocuments(applicationNumber, context);
    }

    internal PatentDocumentList GetNormalizedDocuments(string applicationNumber, RequestContext context)
    {
        if (context == null)
        {
            throw new ValidationException("A patent document request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(applicationNumber);
        if (!parsed.IsValid)
        {
            throw new ValidationException("The patent application identifier is invalid.", "applicationNumber", parsed.FailureCode, null);
        }

        RequestContext requestContext = PatentApplicationSearchOperation.CreateRequestContext(context);
        using (global::System.Net.Http.HttpRequestMessage request = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "applications", parsed.CanonicalNumber, "documents" }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            RequestHeaderPolicy.Apply(request, ApiService.Odp, Options, false, DocumentFormat.Json);
            HttpTimeoutScope timeout = new HttpTimeoutScope(Options, requestContext, false);
            using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, request, timeout))
            {
                string[] secrets = CredentialSecrets();
                int statusCode = (int)response.StatusCode;
                if (statusCode < 200 || statusCode >= 300)
                {
                    throw ErrorMapper.MapResponse(response, ApiService.Odp, parsed.CanonicalNumber, Options.Limits, secrets, requestContext.CancellationToken);
                }

                BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, requestContext, Options.Limits, secrets, timeout);
                return OdpPatentDocumentParser.Parse(buffered.Text, parsed.CanonicalNumber, buffered.Metadata);
            }
        }
    }

    internal PatentAssociatedDocumentList GetAssociatedDocuments(string identifier, RequestContext context)
    {
        string applicationNumber = ResolveIdentifier(identifier, context);
        return GetNormalizedAssociatedDocuments(applicationNumber, context);
    }

    internal PatentAssociatedDocumentList GetNormalizedAssociatedDocuments(string applicationNumber, RequestContext context)
    {
        if (context == null)
        {
            throw new ValidationException("An associated-document request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(applicationNumber);
        if (!parsed.IsValid)
        {
            throw new ValidationException("The patent application identifier is invalid.", "applicationNumber", parsed.FailureCode, null);
        }

        RequestContext requestContext = PatentApplicationSearchOperation.CreateRequestContext(context);
        using (global::System.Net.Http.HttpRequestMessage request = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "applications", parsed.CanonicalNumber, "associated-documents" }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            RequestHeaderPolicy.Apply(request, ApiService.Odp, Options, false, DocumentFormat.Json);
            HttpTimeoutScope timeout = new HttpTimeoutScope(Options, requestContext, false);
            using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, request, timeout))
            {
                string[] secrets = CredentialSecrets();
                int statusCode = (int)response.StatusCode;
                if (statusCode < 200 || statusCode >= 300)
                {
                    throw ErrorMapper.MapResponse(response, ApiService.Odp, parsed.CanonicalNumber, Options.Limits, secrets, requestContext.CancellationToken);
                }

                BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, requestContext, Options.Limits, secrets, timeout);
                return OdpPatentAssociatedDocumentParser.Parse(buffered.Text, parsed.CanonicalNumber, buffered.Metadata);
            }
        }
    }

    internal PatentContinuity GetContinuity(string identifier, RequestContext context)
    {
        string applicationNumber = ResolveIdentifier(identifier, context);
        return GetNormalizedContinuity(applicationNumber, context);
    }

    internal PatentContinuity GetNormalizedContinuity(string applicationNumber, RequestContext context)
    {
        if (context == null)
        {
            throw new ValidationException("A patent continuity request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(applicationNumber);
        if (!parsed.IsValid)
        {
            throw new ValidationException("The patent application identifier is invalid.", "applicationNumber", parsed.FailureCode, null);
        }

        RequestContext requestContext = PatentApplicationSearchOperation.CreateRequestContext(context);
        using (global::System.Net.Http.HttpRequestMessage request = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "applications", parsed.CanonicalNumber, "continuity" }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            RequestHeaderPolicy.Apply(request, ApiService.Odp, Options, false, DocumentFormat.Json);
            HttpTimeoutScope timeout = new HttpTimeoutScope(Options, requestContext, false);
            using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, request, timeout))
            {
                string[] secrets = CredentialSecrets();
                int statusCode = (int)response.StatusCode;
                if (statusCode < 200 || statusCode >= 300)
                {
                    throw ErrorMapper.MapResponse(response, ApiService.Odp, parsed.CanonicalNumber, Options.Limits, secrets, requestContext.CancellationToken);
                }

                BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, requestContext, Options.Limits, secrets, timeout);
                return OdpPatentContinuityPriorityParser.ParseContinuity(buffered.Text, parsed.CanonicalNumber, buffered.Metadata);
            }
        }
    }

    internal PatentForeignPriorityList GetForeignPriorities(string identifier, RequestContext context)
    {
        string applicationNumber = ResolveIdentifier(identifier, context);
        return GetNormalizedForeignPriorities(applicationNumber, context);
    }

    internal PatentForeignPriorityList GetNormalizedForeignPriorities(string applicationNumber, RequestContext context)
    {
        if (context == null)
        {
            throw new ValidationException("A foreign-priority request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(applicationNumber);
        if (!parsed.IsValid)
        {
            throw new ValidationException("The patent application identifier is invalid.", "applicationNumber", parsed.FailureCode, null);
        }

        RequestContext requestContext = PatentApplicationSearchOperation.CreateRequestContext(context);
        using (global::System.Net.Http.HttpRequestMessage request = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "applications", parsed.CanonicalNumber, "foreign-priority" }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            RequestHeaderPolicy.Apply(request, ApiService.Odp, Options, false, DocumentFormat.Json);
            HttpTimeoutScope timeout = new HttpTimeoutScope(Options, requestContext, false);
            using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, request, timeout))
            {
                string[] secrets = CredentialSecrets();
                int statusCode = (int)response.StatusCode;
                if (statusCode < 200 || statusCode >= 300)
                {
                    throw ErrorMapper.MapResponse(response, ApiService.Odp, parsed.CanonicalNumber, Options.Limits, secrets, requestContext.CancellationToken);
                }

                BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, requestContext, Options.Limits, secrets, timeout);
                return OdpPatentContinuityPriorityParser.ParseForeignPriorities(buffered.Text, parsed.CanonicalNumber, buffered.Metadata);
            }
        }
    }

    internal PatentAssignmentHistory GetAssignments(string identifier, RequestContext context)
    {
        string applicationNumber = ResolveIdentifier(identifier, context);
        return GetNormalizedAssignments(applicationNumber, context);
    }

    internal PatentAssignmentHistory GetNormalizedAssignments(string applicationNumber, RequestContext context)
    {
        if (context == null)
        {
            throw new ValidationException("A patent assignment request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(applicationNumber);
        if (!parsed.IsValid)
        {
            throw new ValidationException("The patent application identifier is invalid.", "applicationNumber", parsed.FailureCode, null);
        }

        RequestContext requestContext = PatentApplicationSearchOperation.CreateRequestContext(context);
        using (global::System.Net.Http.HttpRequestMessage request = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "applications", parsed.CanonicalNumber, "assignment" }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            RequestHeaderPolicy.Apply(request, ApiService.Odp, Options, false, DocumentFormat.Json);
            HttpTimeoutScope timeout = new HttpTimeoutScope(Options, requestContext, false);
            using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, request, timeout))
            {
                string[] secrets = CredentialSecrets();
                int statusCode = (int)response.StatusCode;
                if (statusCode < 200 || statusCode >= 300)
                {
                    throw ErrorMapper.MapResponse(response, ApiService.Odp, parsed.CanonicalNumber, Options.Limits, secrets, requestContext.CancellationToken);
                }

                BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, requestContext, Options.Limits, secrets, timeout);
                return OdpPatentAssignmentParser.Parse(buffered.Text, parsed.CanonicalNumber, buffered.Metadata);
            }
        }
    }

    internal PatentAttorneyRecord GetAttorneys(string identifier, RequestContext context)
    {
        string applicationNumber = ResolveIdentifier(identifier, context);
        return GetNormalizedAttorneys(applicationNumber, context);
    }

    internal PatentAttorneyRecord GetNormalizedAttorneys(string applicationNumber, RequestContext context)
    {
        if (context == null)
        {
            throw new ValidationException("A patent attorney request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(applicationNumber);
        if (!parsed.IsValid)
        {
            throw new ValidationException("The patent application identifier is invalid.", "applicationNumber", parsed.FailureCode, null);
        }

        RequestContext requestContext = PatentApplicationSearchOperation.CreateRequestContext(context);
        using (global::System.Net.Http.HttpRequestMessage request = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "applications", parsed.CanonicalNumber, "attorney" }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            RequestHeaderPolicy.Apply(request, ApiService.Odp, Options, false, DocumentFormat.Json);
            HttpTimeoutScope timeout = new HttpTimeoutScope(Options, requestContext, false);
            using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, request, timeout))
            {
                string[] secrets = CredentialSecrets();
                int statusCode = (int)response.StatusCode;
                if (statusCode < 200 || statusCode >= 300)
                {
                    throw ErrorMapper.MapResponse(response, ApiService.Odp, parsed.CanonicalNumber, Options.Limits, secrets, requestContext.CancellationToken);
                }

                BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, requestContext, Options.Limits, secrets, timeout);
                return OdpPatentAttorneyParser.Parse(buffered.Text, parsed.CanonicalNumber, buffered.Metadata);
            }
        }
    }

    internal PatentTermAdjustment GetTermAdjustment(string identifier, RequestContext context)
    {
        string applicationNumber = ResolveIdentifier(identifier, context);
        return GetNormalizedTermAdjustment(applicationNumber, context);
    }

    internal PatentTermAdjustment GetNormalizedTermAdjustment(string applicationNumber, RequestContext context)
    {
        if (context == null)
        {
            throw new ValidationException("A patent-term-adjustment request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(applicationNumber);
        if (!parsed.IsValid)
        {
            throw new ValidationException("The patent application identifier is invalid.", "applicationNumber", parsed.FailureCode, null);
        }

        RequestContext requestContext = PatentApplicationSearchOperation.CreateRequestContext(context);
        using (global::System.Net.Http.HttpRequestMessage request = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "applications", parsed.CanonicalNumber, "adjustment" }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            RequestHeaderPolicy.Apply(request, ApiService.Odp, Options, false, DocumentFormat.Json);
            HttpTimeoutScope timeout = new HttpTimeoutScope(Options, requestContext, false);
            using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, request, timeout))
            {
                string[] secrets = CredentialSecrets();
                int statusCode = (int)response.StatusCode;
                if (statusCode < 200 || statusCode >= 300)
                {
                    throw ErrorMapper.MapResponse(response, ApiService.Odp, parsed.CanonicalNumber, Options.Limits, secrets, requestContext.CancellationToken);
                }

                BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, requestContext, Options.Limits, secrets, timeout);
                return OdpPatentTermAdjustmentParser.Parse(buffered.Text, parsed.CanonicalNumber, buffered.Metadata);
            }
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
