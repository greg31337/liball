namespace uspto;

internal class TrademarkMultiCaseBundleOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;
    internal DownloadHostPolicy HostPolicy;

    internal TrademarkMultiCaseBundleOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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
        HostPolicy = DownloadHostPolicy.CreateDefault();
    }

    internal DownloadResult Download(string[] identifiers, DocumentFormat format, DownloadRequest request)
    {
        ValidateFormat(format);
        if (request == null)
        {
            throw new ValidationException("A multi-case trademark bundle request is required.", "request", "REQUIRED", null);
        }

        if (request.Context == null)
        {
            throw new ValidationException("A multi-case trademark bundle context is required.", "Context", "REQUIRED", null);
        }

        request.Context.CancellationToken.ThrowIfCancellationRequested();
        global::System.Collections.Generic.Dictionary<string, string?> query = BuildQuery(identifiers, Options.Limits);
        string extension = TrademarkCaseDocumentTransferOperation.FormatExtension(format);
        global::System.Uri sourceUri;
        using (global::System.Net.Http.HttpRequestMessage route = RequestBuilder.CreateGetRequest(Options.TsdrBaseUri, new string[] { "ts", "cd", "casedocs", "bundle." + extension }, query))
        {
            global::System.Uri builtUri = route.RequestUri ?? new global::System.Uri("https://invalid.example/");
            ValidateUriSize(builtUri, Options.Limits);
            sourceUri = new global::System.Uri(builtUri.OriginalString, global::System.UriKind.Absolute);
        }

        DownloadRequest resolvedRequest = TrademarkStatusContentOperation.CopyRequest(request, sourceUri);
        PatentDocumentDownloadOperation operation = new PatentDocumentDownloadOperation(Client, Options);
        operation.HostPolicy = HostPolicy;
        operation.Service = TsdrPolicy.ClassifyRateService(format);
        operation.RequestedFormat = format;
        operation.StrictMediaType = true;
        operation.IsBundleRequest = true;
        DownloadResult result = operation.Download("trademark-multi-case-bundle." + extension, resolvedRequest);
        result.Request = request;
        return result;
    }

    internal static global::System.Collections.Generic.Dictionary<string, string?> BuildQuery(string[] identifiers, UsptoLimits limits)
    {
        if (identifiers == null)
        {
            throw new ValidationException("Trademark bundle identifiers are required.", "identifiers", "REQUIRED", null);
        }

        if (limits == null)
        {
            throw new ValidationException("Trademark bundle limits are required.", "limits", "REQUIRED", null);
        }

        if (limits.MaximumResults <= 0)
        {
            throw new ValidationException("The maximum trademark bundle identifier count must be positive.", "MaximumResults", "INVALID_LIMIT", null);
        }

        if (identifiers.Length == 0)
        {
            throw new ValidationException("At least one trademark bundle identifier is required.", "identifiers", "EMPTY_IDENTIFIERS", null);
        }

        if (identifiers.Length > limits.MaximumResults)
        {
            throw new ValidationException("The trademark bundle identifier count exceeds the configured limit.", "identifiers", "TOO_MANY_IDENTIFIERS", null);
        }

        global::System.Collections.Generic.List<string> serialNumbers = new global::System.Collections.Generic.List<string>();
        global::System.Collections.Generic.List<string> registrationNumbers = new global::System.Collections.Generic.List<string>();
        global::System.Collections.Generic.List<string> referenceNumbers = new global::System.Collections.Generic.List<string>();
        global::System.Collections.Generic.HashSet<string> canonicalIdentifiers = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.OrdinalIgnoreCase);
        int index;
        for (index = 0; index < identifiers.Length; index += 1)
        {
            TrademarkIdentifierResult parsed = TrademarkIdentifierParser.ParseTrademarkIdentifier(identifiers[index]);
            if (!parsed.IsValid)
            {
                throw new ValidationException("A trademark bundle identifier is invalid.", "identifiers", parsed.FailureCode, null);
            }

            if (!canonicalIdentifiers.Add(parsed.CanonicalNumber))
            {
                throw new ValidationException("Trademark bundle identifiers must be unique.", "identifiers", "DUPLICATE_IDENTIFIER", null);
            }

            switch (parsed.Kind)
            {
                case TrademarkIdentifierKind.SerialNumber:
                    serialNumbers.Add(parsed.IdentifierValue);
                    break;
                case TrademarkIdentifierKind.RegistrationNumber:
                    registrationNumbers.Add(parsed.IdentifierValue);
                    break;
                case TrademarkIdentifierKind.ReferenceNumber:
                    referenceNumbers.Add(parsed.IdentifierValue);
                    break;
                default:
                    throw new ValidationException("The trademark bundle identifier kind is unsupported.", "identifiers", "UNSUPPORTED_IDENTIFIER_KIND", null);
            }
        }

        global::System.Collections.Generic.Dictionary<string, string?> query = new global::System.Collections.Generic.Dictionary<string, string?>();
        if (serialNumbers.Count > 0)
        {
            query.Add("sn", global::System.String.Join(",", serialNumbers));
        }

        if (registrationNumbers.Count > 0)
        {
            query.Add("rn", global::System.String.Join(",", registrationNumbers));
        }

        if (referenceNumbers.Count > 0)
        {
            query.Add("ref", global::System.String.Join(",", referenceNumbers));
        }

        return query;
    }

    internal static void ValidateUriSize(global::System.Uri uri, UsptoLimits limits)
    {
        if (limits.MaximumUriCharacters <= 0)
        {
            throw new ValidationException("The maximum trademark bundle URI length must be positive.", "MaximumUriCharacters", "INVALID_LIMIT", null);
        }

        if (uri.OriginalString.Length > limits.MaximumUriCharacters)
        {
            throw new ValidationException("The encoded trademark bundle URI exceeds the configured size limit.", "MaximumUriCharacters", "REQUEST_URI_TOO_LARGE", null);
        }
    }

    internal static void ValidateFormat(DocumentFormat format)
    {
        if (format != DocumentFormat.Pdf && format != DocumentFormat.Xml && format != DocumentFormat.Zip)
        {
            throw new ValidationException("Multi-case trademark bundles require PDF, XML, or ZIP.", "format", "UNSUPPORTED_MULTI_CASE_BUNDLE_FORMAT", null);
        }
    }
}
