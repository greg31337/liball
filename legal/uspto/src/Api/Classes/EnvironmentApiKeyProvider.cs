namespace uspto;

/// <summary>
/// Reads service-specific API credentials from the process environment at request time.
/// </summary>
public class EnvironmentApiKeyProvider : IApiKeyProvider
{
    /// <summary>Initializes an environment-backed API key provider.</summary>
    public EnvironmentApiKeyProvider()
    {
    }

    /// <summary>Gets the credential currently available for a service.</summary>
    /// <param name="request">The credential request.</param>
    /// <returns>The selected credential and its non-secret environment-variable source.</returns>
    /// <exception cref="ValidationException">The request is missing or its service is unsupported.</exception>
    /// <exception cref="AuthenticationException">The mapped environment value is missing or blank and anonymous access is not allowed.</exception>
    public ApiCredentialResult GetApiKey(ApiCredentialRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("A credential request is required.", "request", "REQUIRED", null);
        }

        string environmentVariable;
        switch (request.Service)
        {
            case ApiService.Odp:
            case ApiService.OfficeAction:
                environmentVariable = UsptoEnvironmentVariables.OdpApiKey;
                break;
            case ApiService.Tsdr:
            case ApiService.TsdrPdfAndZip:
                environmentVariable = UsptoEnvironmentVariables.TsdrApiKey;
                break;
            case ApiService.PatentsView:
                environmentVariable = UsptoEnvironmentVariables.PatentsViewApiKey;
                break;
            default:
                throw new ValidationException("The credential request specifies an unsupported service.", "Service", "UNSUPPORTED_SERVICE", null);
        }

        string? apiKey = global::System.Environment.GetEnvironmentVariable(environmentVariable);
        if (global::System.String.IsNullOrWhiteSpace(apiKey))
        {
            if (request.AllowAnonymous)
            {
                return new ApiCredentialResult
                {
                    Request = request,
                    ApiKey = null,
                    Source = environmentVariable,
                    IsAvailable = false
                };
            }

            throw new AuthenticationException("No API credential is available for the requested service.", request.Service, 0, false, null, null);
        }

        return new ApiCredentialResult
        {
            Request = request,
            ApiKey = apiKey,
            Source = environmentVariable,
            IsAvailable = true
        };
    }
}
