namespace uspto;

internal class BulkCatalogRetrievalOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;
    internal DownloadHostPolicy HostPolicy;

    internal BulkCatalogRetrievalOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options, DownloadHostPolicy hostPolicy)
    {
        if (client == null)
        {
            throw new ValidationException("An HTTP client is required.", "client", "REQUIRED", null);
        }

        if (options == null)
        {
            throw new ValidationException("Client options are required.", "options", "REQUIRED", null);
        }

        if (hostPolicy == null)
        {
            throw new ValidationException("A download host policy is required.", "hostPolicy", "REQUIRED", null);
        }

        Client = client;
        Options = options;
        HostPolicy = hostPolicy;
    }

    internal BulkProduct GetProduct(string productIdentifier, RequestContext context)
    {
        ValidateProductIdentifier(productIdentifier);
        ValidateContext(context);
        ValidateCatalogLimits();
        string normalizedIdentifier = productIdentifier.Trim();
        RequestContext requestContext = PatentApplicationSearchOperation.CreateRequestContext(context);
        global::System.Collections.Generic.List<BulkProductFile> files = new global::System.Collections.Generic.List<BulkProductFile>();
        global::System.Collections.Generic.HashSet<string> fileNames = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        BulkProduct? result = null;
        long offset = 0L;
        int pagesRead = 0;
        while (pagesRead < Options.Limits.MaximumPages && files.Count < Options.Limits.MaximumResults)
        {
            requestContext.CancellationToken.ThrowIfCancellationRequested();
            global::System.Collections.Generic.Dictionary<string, string?> query = new global::System.Collections.Generic.Dictionary<string, string?>();
            query.Add("includeFiles", "true");
            query.Add("limit", UsptoDefaults.PageSize.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            query.Add("offset", offset.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            BulkProduct page;
            using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "datasets", "products", normalizedIdentifier }, query))
            {
                page = SendProduct(message, normalizedIdentifier, requestContext);
            }

            pagesRead += 1;
            if (result == null)
            {
                result = page;
                if (result.HasFileCount && result.FileCount > Options.Limits.MaximumResults)
                {
                    throw new ValidationException("The bulk product file count exceeds the configured result limit.", "MaximumResults", "RESULT_LIMIT_EXCEEDED", null);
                }
            }
            else
            {
                ValidateProductPage(result, page);
            }

            int pageIndex;
            for (pageIndex = 0; pageIndex < page.Files.Length; pageIndex += 1)
            {
                BulkProductFile file = page.Files[pageIndex];
                string identity = file.FileName ?? string.Empty;
                if (!fileNames.Add(identity))
                {
                    throw new ValidationException("Bulk product pagination returned a duplicate file.", "FileName", "DUPLICATE_PAGE_RECORD", null);
                }

                if (files.Count >= Options.Limits.MaximumResults)
                {
                    throw new ValidationException("The bulk product files exceed the configured result limit.", "MaximumResults", "RESULT_LIMIT_EXCEEDED", null);
                }

                files.Add(file);
            }

            offset += page.Files.LongLength;
            if (result.HasFileCount && offset >= result.FileCount)
            {
                break;
            }

            if (page.Files.Length == 0)
            {
                if (result.HasFileCount && offset < result.FileCount)
                {
                    throw new ApiException("Bulk product pagination ended before all file metadata was returned.", ApiService.Odp, 200, true, "BULK_FILE_PAGE_INCOMPLETE", null, page.Provenance.RequestIdentifier, false, null);
                }

                break;
            }

            if (!result.HasFileCount && page.Files.Length < UsptoDefaults.PageSize)
            {
                break;
            }
        }

        if (result == null)
        {
            throw new ApiException("Bulk product retrieval did not return a product.", ApiService.Odp, 0, false, "BULK_PRODUCT_MISSING", null, null, false, null);
        }

        if (result.HasFileCount && files.Count < result.FileCount)
        {
            throw new ValidationException("The bulk product files exceed the configured page limit.", "MaximumPages", "PAGE_LIMIT_EXCEEDED", null);
        }

        result.Files = files.ToArray();
        result.ReturnedFileCount = result.Files.LongLength;
        result.HasReturnedFileCount = true;
        return result;
    }

    internal BulkProductFile GetFile(string productIdentifier, string fileName, RequestContext context)
    {
        ValidateProductIdentifier(productIdentifier);
        ValidateFileName(fileName);
        ValidateContext(context);
        string normalizedIdentifier = productIdentifier.Trim();
        BulkProduct product = GetProduct(normalizedIdentifier, context);
        BulkProductFile? catalogFile = FindCatalogFile(product, fileName);
        if (catalogFile == null)
        {
            throw new NotFoundException("The requested bulk product file was not found in the product catalog.", ApiService.Odp, normalizedIdentifier + "/" + fileName, 0, false, product.Provenance.RequestIdentifier, null);
        }

        using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "datasets", "products", "files", normalizedIdentifier, fileName }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            RequestContext requestContext = PatentApplicationSearchOperation.CreateRequestContext(context);
            RequestHeaderPolicy.Apply(message, ApiService.Odp, Options, false, DocumentFormat.Json);
            HttpTimeoutScope timeout = new HttpTimeoutScope(Options, requestContext, false);
            using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, message, timeout))
            {
                ValidateResponseIdentity(message, response);
                return ResolveFileResponse(message, response, catalogFile, normalizedIdentifier, fileName, requestContext, timeout);
            }
        }
    }

    internal BulkProduct SendProduct(global::System.Net.Http.HttpRequestMessage message, string productIdentifier, RequestContext context)
    {
        RequestHeaderPolicy.Apply(message, ApiService.Odp, Options, false, DocumentFormat.Json);
        HttpTimeoutScope timeout = new HttpTimeoutScope(Options, context, false);
        using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, message, timeout))
        {
            ValidateResponseIdentity(message, response);
            string[] secrets = CredentialSecrets();
            int statusCode = (int)response.StatusCode;
            if (statusCode < 200 || statusCode >= 300)
            {
                throw ErrorMapper.MapResponse(response, ApiService.Odp, "bulk product retrieval", Options.Limits, secrets, context.CancellationToken);
            }

            BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, context, Options.Limits, secrets, timeout);
            string? rawResponse = null;
            if (buffered.HasRawText)
            {
                rawResponse = buffered.RawText;
            }

            return OdpBulkCatalogParser.ParseExactProduct(buffered.Text, productIdentifier, buffered.Metadata, rawResponse);
        }
    }

    internal BulkProductFile ResolveFileResponse(global::System.Net.Http.HttpRequestMessage message, global::System.Net.Http.HttpResponseMessage response, BulkProductFile catalogFile, string productIdentifier, string fileName, RequestContext context, HttpTimeoutScope timeout)
    {
        string[] secrets = CredentialSecrets();
        int statusCode = (int)response.StatusCode;
        if (RedirectPolicy.IsRedirectStatus(statusCode))
        {
            if (message.RequestUri == null)
            {
                throw new ValidationException("The bulk file catalog URI is required.", "RequestUri", "REQUIRED", null);
            }

            RedirectState redirectState = new RedirectState(ApiService.Odp, message.RequestUri, Options.Download.RedirectHopCount);
            global::System.Uri target = RedirectPolicy.ResolveRedirectLocation(message, response, redirectState);
            HostPolicy.Validate(target, Options, context.CancellationToken);
            HttpResponseMetadata metadata = ResponseReader.CaptureMetadata(response, secrets);
            catalogFile.CatalogUri = SecretRedactor.RedactUri(message.RequestUri, secrets);
            catalogFile.DownloadUri = SecretRedactor.RedactUri(target, secrets);
            catalogFile.ResponseStatusCode = statusCode;
            catalogFile.HasResponseStatusCode = true;
            catalogFile.Provenance = OdpJsonParser.CreateProvenance(metadata, metadata.RequestIdentifier);
            catalogFile.Provenance.DatasetIdentifier = productIdentifier;
            return catalogFile;
        }

        if (statusCode < 200 || statusCode >= 300)
        {
            throw ErrorMapper.MapResponse(response, ApiService.Odp, "bulk product file retrieval", Options.Limits, secrets, context.CancellationToken);
        }

        BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, context, Options.Limits, secrets, timeout);
        string? rawResponse = null;
        if (buffered.HasRawText)
        {
            rawResponse = buffered.RawText;
        }

        BulkProductFile result = OdpBulkCatalogParser.ParseExactFile(buffered.Text, productIdentifier, fileName, buffered.Metadata, rawResponse);
        if (result.DownloadUri == null)
        {
            throw new ApiException("The exact bulk file response did not provide a download location.", ApiService.Odp, statusCode, true, "BULK_FILE_LOCATION_MISSING", null, buffered.Metadata.RequestIdentifier, false, null);
        }

        HostPolicy.Validate(result.DownloadUri, Options, context.CancellationToken);
        if (message.RequestUri != null)
        {
            result.CatalogUri = SecretRedactor.RedactUri(message.RequestUri, secrets);
        }

        result.ResponseStatusCode = statusCode;
        result.HasResponseStatusCode = true;
        return result;
    }

    internal static BulkProductFile? FindCatalogFile(BulkProduct product, string fileName)
    {
        int index;
        for (index = 0; index < product.Files.Length; index += 1)
        {
            BulkProductFile candidate = product.Files[index];
            if (global::System.String.Equals(candidate.FileName, fileName, global::System.StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
    }

    internal static void ValidateProductIdentifier(string productIdentifier)
    {
        if (global::System.String.IsNullOrWhiteSpace(productIdentifier))
        {
            throw new ValidationException("A bulk product identifier is required.", "productIdentifier", "REQUIRED", null);
        }
    }

    internal static void ValidateFileName(string fileName)
    {
        if (global::System.String.IsNullOrWhiteSpace(fileName))
        {
            throw new ValidationException("An exact bulk product filename is required.", "fileName", "REQUIRED", null);
        }
    }

    internal static void ValidateContext(RequestContext context)
    {
        if (context == null)
        {
            throw new ValidationException("A bulk catalog request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
    }

    internal void ValidateCatalogLimits()
    {
        if (Options.Limits.MaximumPages <= 0)
        {
            throw new ValidationException("The bulk product page limit must be positive.", "MaximumPages", "PAGE_LIMIT_OUT_OF_RANGE", null);
        }

        if (Options.Limits.MaximumResults <= 0)
        {
            throw new ValidationException("The bulk product result limit must be positive.", "MaximumResults", "RESULT_LIMIT_OUT_OF_RANGE", null);
        }
    }

    internal static void ValidateProductPage(BulkProduct firstPage, BulkProduct nextPage)
    {
        if (!global::System.String.Equals(firstPage.ProductIdentifier, nextPage.ProductIdentifier, global::System.StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("The bulk product identifier changed between file pages.", "ProductIdentifier", "PAGE_IDENTITY_MISMATCH", null);
        }

        if (firstPage.HasFileCount != nextPage.HasFileCount || firstPage.HasFileCount && firstPage.FileCount != nextPage.FileCount)
        {
            throw new ValidationException("The bulk product file count changed between pages.", "FileCount", "TOTAL_MISMATCH", null);
        }
    }

    internal static void ValidateResponseIdentity(global::System.Net.Http.HttpRequestMessage message, global::System.Net.Http.HttpResponseMessage response)
    {
        if (message.RequestUri == null || response.RequestMessage == null || response.RequestMessage.RequestUri == null)
        {
            return;
        }

        string sentIdentity = RedirectPolicy.GetRedirectIdentity(message.RequestUri);
        string responseIdentity = RedirectPolicy.GetRedirectIdentity(response.RequestMessage.RequestUri);
        if (!global::System.String.Equals(sentIdentity, responseIdentity, global::System.StringComparison.Ordinal))
        {
            throw new ApiException("The HTTP client followed a redirect outside the validated redirect policy.", ApiService.Odp, (int)response.StatusCode, true, "AUTOMATIC_REDIRECT_REJECTED", null, null, false, null);
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
