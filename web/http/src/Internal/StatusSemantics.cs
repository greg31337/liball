namespace http;

internal static class StatusSemantics
{
    internal static HttpStatusClass Classify(int statusCode)
    {
        if (statusCode < 100 || statusCode > 999)
        {
            return HttpStatusClass.Invalid;
        }

        if (statusCode < 200)
        {
            return HttpStatusClass.Informational;
        }

        if (statusCode < 300)
        {
            return HttpStatusClass.Successful;
        }

        if (statusCode < 400)
        {
            return HttpStatusClass.Redirection;
        }

        if (statusCode < 500)
        {
            return HttpStatusClass.ClientError;
        }

        if (statusCode < 600)
        {
            return HttpStatusClass.ServerError;
        }

        return HttpStatusClass.Extension;
    }

    internal static HttpStatusMetadata Describe(int statusCode)
    {
        HttpStatusClass statusClass = Classify(statusCode);
        if (statusClass == HttpStatusClass.Invalid)
        {
            throw new HttpProtocolException(
                "The HTTP status code is not a supported three-digit status.",
                "invalid-status-code");
        }

        HttpStatusMetadata result = new HttpStatusMetadata();
        result.StatusCode = statusCode;
        result.StatusClass = statusClass;
        result.ReasonPhrase = FindReasonPhrase(statusCode);
        result.IsRegistered = result.ReasonPhrase != null;
        result.AllowsContent = AllowsStatusContent(statusCode);
        result.IsCacheableByDefault = IsDefaultCacheable(statusCode);
        result.IsRedirect = IsRedirectStatus(statusCode);
        result.RedirectsPostToGet = RedirectChangesPost(statusCode);
        result.IsRetryable = IsRetryableStatus(statusCode);
        return result;
    }

    static bool AllowsStatusContent(int statusCode)
    {
        return statusCode >= 200 && statusCode != HttpStatusCodes.NoContent &&
            statusCode != HttpStatusCodes.ResetContent &&
            statusCode != HttpStatusCodes.NotModified;
    }

    static bool IsDefaultCacheable(int statusCode)
    {
        switch (statusCode)
        {
            case HttpStatusCodes.Ok:
            case HttpStatusCodes.NonAuthoritativeInformation:
            case HttpStatusCodes.NoContent:
            case HttpStatusCodes.PartialContent:
            case HttpStatusCodes.MultipleChoices:
            case HttpStatusCodes.MovedPermanently:
            case HttpStatusCodes.PermanentRedirect:
            case HttpStatusCodes.NotFound:
            case HttpStatusCodes.MethodNotAllowed:
            case HttpStatusCodes.Gone:
            case HttpStatusCodes.UriTooLong:
            case HttpStatusCodes.NotImplemented:
                return true;
            default:
                return false;
        }
    }

    static bool IsRedirectStatus(int statusCode)
    {
        switch (statusCode)
        {
            case HttpStatusCodes.MultipleChoices:
            case HttpStatusCodes.MovedPermanently:
            case HttpStatusCodes.Found:
            case HttpStatusCodes.SeeOther:
            case HttpStatusCodes.UseProxy:
            case HttpStatusCodes.TemporaryRedirect:
            case HttpStatusCodes.PermanentRedirect:
                return true;
            default:
                return false;
        }
    }

    static bool RedirectChangesPost(int statusCode)
    {
        return statusCode == HttpStatusCodes.MovedPermanently ||
            statusCode == HttpStatusCodes.Found ||
            statusCode == HttpStatusCodes.SeeOther;
    }

    static bool IsRetryableStatus(int statusCode)
    {
        switch (statusCode)
        {
            case HttpStatusCodes.RequestTimeout:
            case HttpStatusCodes.ContentTooLarge:
            case HttpStatusCodes.MisdirectedRequest:
            case HttpStatusCodes.TooEarly:
            case HttpStatusCodes.TooManyRequests:
            case HttpStatusCodes.InternalServerError:
            case HttpStatusCodes.BadGateway:
            case HttpStatusCodes.ServiceUnavailable:
            case HttpStatusCodes.GatewayTimeout:
                return true;
            default:
                return false;
        }
    }

    static string? FindReasonPhrase(int statusCode)
    {
        switch (statusCode)
        {
            case HttpStatusCodes.Continue:
                return HttpReasonPhrases.Continue;
            case HttpStatusCodes.SwitchingProtocols:
                return HttpReasonPhrases.SwitchingProtocols;
            case HttpStatusCodes.Processing:
                return HttpReasonPhrases.Processing;
            case HttpStatusCodes.EarlyHints:
                return HttpReasonPhrases.EarlyHints;
            case HttpStatusCodes.UploadResumptionSupported:
                return HttpReasonPhrases.UploadResumptionSupported;
            case HttpStatusCodes.Ok:
                return HttpReasonPhrases.Ok;
            case HttpStatusCodes.Created:
                return HttpReasonPhrases.Created;
            case HttpStatusCodes.Accepted:
                return HttpReasonPhrases.Accepted;
            case HttpStatusCodes.NonAuthoritativeInformation:
                return HttpReasonPhrases.NonAuthoritativeInformation;
            case HttpStatusCodes.NoContent:
                return HttpReasonPhrases.NoContent;
            case HttpStatusCodes.ResetContent:
                return HttpReasonPhrases.ResetContent;
            case HttpStatusCodes.PartialContent:
                return HttpReasonPhrases.PartialContent;
            case HttpStatusCodes.MultiStatus:
                return HttpReasonPhrases.MultiStatus;
            case HttpStatusCodes.AlreadyReported:
                return HttpReasonPhrases.AlreadyReported;
            case HttpStatusCodes.ImUsed:
                return HttpReasonPhrases.ImUsed;
            case HttpStatusCodes.MultipleChoices:
                return HttpReasonPhrases.MultipleChoices;
            case HttpStatusCodes.MovedPermanently:
                return HttpReasonPhrases.MovedPermanently;
            case HttpStatusCodes.Found:
                return HttpReasonPhrases.Found;
            case HttpStatusCodes.SeeOther:
                return HttpReasonPhrases.SeeOther;
            case HttpStatusCodes.NotModified:
                return HttpReasonPhrases.NotModified;
            case HttpStatusCodes.UseProxy:
                return HttpReasonPhrases.UseProxy;
            case HttpStatusCodes.Unused306:
                return HttpReasonPhrases.Unused306;
            case HttpStatusCodes.TemporaryRedirect:
                return HttpReasonPhrases.TemporaryRedirect;
            case HttpStatusCodes.PermanentRedirect:
                return HttpReasonPhrases.PermanentRedirect;
            case HttpStatusCodes.BadRequest:
                return HttpReasonPhrases.BadRequest;
            case HttpStatusCodes.Unauthorized:
                return HttpReasonPhrases.Unauthorized;
            case HttpStatusCodes.PaymentRequired:
                return HttpReasonPhrases.PaymentRequired;
            case HttpStatusCodes.Forbidden:
                return HttpReasonPhrases.Forbidden;
            case HttpStatusCodes.NotFound:
                return HttpReasonPhrases.NotFound;
            case HttpStatusCodes.MethodNotAllowed:
                return HttpReasonPhrases.MethodNotAllowed;
            case HttpStatusCodes.NotAcceptable:
                return HttpReasonPhrases.NotAcceptable;
            case HttpStatusCodes.ProxyAuthenticationRequired:
                return HttpReasonPhrases.ProxyAuthenticationRequired;
            case HttpStatusCodes.RequestTimeout:
                return HttpReasonPhrases.RequestTimeout;
            case HttpStatusCodes.Conflict:
                return HttpReasonPhrases.Conflict;
            case HttpStatusCodes.Gone:
                return HttpReasonPhrases.Gone;
            case HttpStatusCodes.LengthRequired:
                return HttpReasonPhrases.LengthRequired;
            case HttpStatusCodes.PreconditionFailed:
                return HttpReasonPhrases.PreconditionFailed;
            case HttpStatusCodes.ContentTooLarge:
                return HttpReasonPhrases.ContentTooLarge;
            case HttpStatusCodes.UriTooLong:
                return HttpReasonPhrases.UriTooLong;
            case HttpStatusCodes.UnsupportedMediaType:
                return HttpReasonPhrases.UnsupportedMediaType;
            case HttpStatusCodes.RangeNotSatisfiable:
                return HttpReasonPhrases.RangeNotSatisfiable;
            case HttpStatusCodes.ExpectationFailed:
                return HttpReasonPhrases.ExpectationFailed;
            case HttpStatusCodes.Unused418:
                return HttpReasonPhrases.Unused418;
            case HttpStatusCodes.MisdirectedRequest:
                return HttpReasonPhrases.MisdirectedRequest;
            case HttpStatusCodes.UnprocessableContent:
                return HttpReasonPhrases.UnprocessableContent;
            case HttpStatusCodes.Locked:
                return HttpReasonPhrases.Locked;
            case HttpStatusCodes.FailedDependency:
                return HttpReasonPhrases.FailedDependency;
            case HttpStatusCodes.TooEarly:
                return HttpReasonPhrases.TooEarly;
            case HttpStatusCodes.UpgradeRequired:
                return HttpReasonPhrases.UpgradeRequired;
            case HttpStatusCodes.PreconditionRequired:
                return HttpReasonPhrases.PreconditionRequired;
            case HttpStatusCodes.TooManyRequests:
                return HttpReasonPhrases.TooManyRequests;
            case HttpStatusCodes.RequestHeaderFieldsTooLarge:
                return HttpReasonPhrases.RequestHeaderFieldsTooLarge;
            case HttpStatusCodes.UnavailableForLegalReasons:
                return HttpReasonPhrases.UnavailableForLegalReasons;
            case HttpStatusCodes.InternalServerError:
                return HttpReasonPhrases.InternalServerError;
            case HttpStatusCodes.NotImplemented:
                return HttpReasonPhrases.NotImplemented;
            case HttpStatusCodes.BadGateway:
                return HttpReasonPhrases.BadGateway;
            case HttpStatusCodes.ServiceUnavailable:
                return HttpReasonPhrases.ServiceUnavailable;
            case HttpStatusCodes.GatewayTimeout:
                return HttpReasonPhrases.GatewayTimeout;
            case HttpStatusCodes.HttpVersionNotSupported:
                return HttpReasonPhrases.HttpVersionNotSupported;
            case HttpStatusCodes.VariantAlsoNegotiates:
                return HttpReasonPhrases.VariantAlsoNegotiates;
            case HttpStatusCodes.InsufficientStorage:
                return HttpReasonPhrases.InsufficientStorage;
            case HttpStatusCodes.LoopDetected:
                return HttpReasonPhrases.LoopDetected;
            case HttpStatusCodes.NotExtended:
                return HttpReasonPhrases.NotExtended;
            case HttpStatusCodes.NetworkAuthenticationRequired:
                return HttpReasonPhrases.NetworkAuthenticationRequired;
            default:
                return null;
        }
    }
}
