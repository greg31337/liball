namespace uspto;

internal static class RequestHeaderPolicy
{
    internal static void Apply(global::System.Net.Http.HttpRequestMessage request, ApiService service, UsptoClientOptions options, bool allowAnonymous, DocumentFormat responseFormat)
    {
        if (request == null)
        {
            throw new ValidationException("An HTTP request is required.", "request", "REQUIRED", null);
        }

        if (options == null)
        {
            throw new ValidationException("Client options are required.", "options", "REQUIRED", null);
        }

        string credentialHeader;
        string? configuredApiKey;
        switch (service)
        {
            case ApiService.Odp:
            case ApiService.OfficeAction:
                credentialHeader = UsptoHeaders.OdpApiKey;
                configuredApiKey = options.OdpApiKey;
                break;
            case ApiService.Tsdr:
            case ApiService.TsdrPdfAndZip:
                credentialHeader = UsptoHeaders.TsdrApiKey;
                configuredApiKey = options.TsdrApiKey;
                break;
            case ApiService.PatentsView:
                credentialHeader = UsptoHeaders.PatentsViewApiKey;
                configuredApiKey = options.PatentsViewApiKey;
                break;
            default:
                throw new ValidationException("The requested service does not have an HTTP header policy.", "service", "UNSUPPORTED_SERVICE", null);
        }

        string mediaType;
        switch (service)
        {
            case ApiService.Odp:
                if (responseFormat == DocumentFormat.Csv)
                {
                    mediaType = UsptoMediaTypes.Csv;
                }
                else if (responseFormat == DocumentFormat.Unknown || responseFormat == DocumentFormat.Json)
                {
                    mediaType = UsptoMediaTypes.Json;
                }
                else
                {
                    throw new ValidationException("The requested response format is not supported by the service.", "responseFormat", "UNSUPPORTED_FORMAT", null);
                }

                break;
            case ApiService.OfficeAction:
            case ApiService.PatentsView:
                if (responseFormat != DocumentFormat.Unknown && responseFormat != DocumentFormat.Json)
                {
                    throw new ValidationException("The requested response format is not supported by the service.", "responseFormat", "UNSUPPORTED_FORMAT", null);
                }

                mediaType = UsptoMediaTypes.Json;
                break;
            case ApiService.Tsdr:
                switch (responseFormat)
                {
                    case DocumentFormat.Unknown:
                    case DocumentFormat.Json:
                        mediaType = UsptoMediaTypes.Json;
                        break;
                    case DocumentFormat.Xml:
                        mediaType = UsptoMediaTypes.Xml;
                        break;
                    case DocumentFormat.Html:
                        mediaType = UsptoMediaTypes.Html;
                        break;
                    default:
                        throw new ValidationException("The requested response format is not supported by the TSDR general service.", "responseFormat", "UNSUPPORTED_FORMAT", null);
                }

                break;
            case ApiService.TsdrPdfAndZip:
                switch (responseFormat)
                {
                    case DocumentFormat.Pdf:
                        mediaType = UsptoMediaTypes.Pdf;
                        break;
                    case DocumentFormat.Zip:
                        mediaType = UsptoMediaTypes.Zip;
                        break;
                    default:
                        throw new ValidationException("The TSDR document service requires PDF or ZIP content negotiation.", "responseFormat", "UNSUPPORTED_FORMAT", null);
                }

                break;
            default:
                throw new ValidationException("The requested service does not have a content-negotiation policy.", "service", "UNSUPPORTED_SERVICE", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(options.UserAgent) || options.UserAgent.IndexOf('\r') >= 0 || options.UserAgent.IndexOf('\n') >= 0)
        {
            throw new ValidationException("A valid user agent is required.", "UserAgent", "INVALID_HEADER_VALUE", null);
        }

        string? apiKey = configuredApiKey;
        if (configuredApiKey != null)
        {
            if (global::System.String.IsNullOrWhiteSpace(configuredApiKey) || configuredApiKey.IndexOf('\r') >= 0 || configuredApiKey.IndexOf('\n') >= 0)
            {
                throw new AuthenticationException("The configured API credential is invalid.", service, 0, false, null, null);
            }
        }
        else if (options.ApiKeyProvider != null)
        {
            ApiCredentialRequest credentialRequest = new ApiCredentialRequest
            {
                Service = service,
                AllowAnonymous = allowAnonymous
            };
            ApiCredentialResult credentialResult = options.ApiKeyProvider.GetApiKey(credentialRequest);
            if (credentialResult == null)
            {
                throw new AuthenticationException("The API credential provider returned an invalid result.", service, 0, false, null, null);
            }

            if (credentialResult.IsAvailable)
            {
                if (global::System.String.IsNullOrWhiteSpace(credentialResult.ApiKey) || credentialResult.ApiKey.IndexOf('\r') >= 0 || credentialResult.ApiKey.IndexOf('\n') >= 0)
                {
                    throw new AuthenticationException("The API credential provider returned an invalid result.", service, 0, false, null, null);
                }

                if (credentialResult.HasExpiresAt && credentialResult.ExpiresAt <= global::System.DateTimeOffset.UtcNow)
                {
                    if (!allowAnonymous)
                    {
                        throw new AuthenticationException("No current API credential is available for the requested service.", service, 0, false, null, null);
                    }

                    apiKey = null;
                }
                else
                {
                    apiKey = credentialResult.ApiKey;
                }
            }
            else
            {
                apiKey = null;
                if (!allowAnonymous)
                {
                    throw new AuthenticationException("No API credential is available for the requested service.", service, 0, false, null, null);
                }
            }
        }
        else if (!allowAnonymous)
        {
            throw new AuthenticationException("No API credential is available for the requested service.", service, 0, false, null, null);
        }

        request.Headers.Remove(UsptoHeaders.OdpApiKey);
        request.Headers.Remove(UsptoHeaders.TsdrApiKey);
        request.Headers.Remove(UsptoHeaders.PatentsViewApiKey);
        request.Headers.Remove(UsptoHeaders.UserAgent);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new global::System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(mediaType));
        request.Headers.TryAddWithoutValidation(UsptoHeaders.UserAgent, options.UserAgent);
        RequestCredentialStore.Remember(request, apiKey);
        if (apiKey != null)
        {
            request.Headers.TryAddWithoutValidation(credentialHeader, apiKey);
        }
    }
}
