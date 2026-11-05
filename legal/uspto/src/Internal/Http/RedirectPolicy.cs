namespace uspto;

internal class RedirectState
{
    internal ApiService Service = ApiService.Unknown;
    internal int MaximumHopCount = 0;
    internal int HopCount = 0;
    internal global::System.Uri CurrentUri = new global::System.Uri("https://invalid.example/");
    internal global::System.Collections.Generic.HashSet<string> VisitedUris = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);

    internal RedirectState(ApiService service, global::System.Uri initialUri, int maximumHopCount)
    {
        if (initialUri == null)
        {
            throw new ValidationException("An initial redirect URI is required.", "initialUri", "REQUIRED", null);
        }

        if (!initialUri.IsAbsoluteUri)
        {
            throw new ValidationException("The initial redirect URI must be absolute.", "initialUri", "ABSOLUTE_URI_REQUIRED", null);
        }

        if (!global::System.String.Equals(initialUri.Scheme, global::System.Uri.UriSchemeHttps, global::System.StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException("Redirect processing requires an HTTPS initial URI.", service, 0, false, "REDIRECT_HTTPS_REQUIRED", null, null, false, null);
        }

        if (maximumHopCount < 0)
        {
            throw new ValidationException("The maximum redirect hop count cannot be negative.", "maximumHopCount", "INVALID_REDIRECT_LIMIT", null);
        }

        Service = service;
        MaximumHopCount = maximumHopCount;
        CurrentUri = initialUri;
        VisitedUris.Add(RedirectPolicy.GetRedirectIdentity(initialUri));
    }
}

internal static class RedirectPolicy
{
    internal static void DisableAutomaticRedirects(global::System.Net.Http.SocketsHttpHandler handler)
    {
        if (handler == null)
        {
            throw new ValidationException("A sockets HTTP handler is required.", "handler", "REQUIRED", null);
        }

        handler.AllowAutoRedirect = false;
    }

    internal static bool IsRedirectStatus(int statusCode)
    {
        return statusCode == 301 || statusCode == 302 || statusCode == 303 || statusCode == 307 || statusCode == 308;
    }

    internal static global::System.Net.Http.HttpRequestMessage CreateRedirectRequest(global::System.Net.Http.HttpRequestMessage currentRequest, global::System.Net.Http.HttpResponseMessage response, RedirectState state, global::System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        global::System.Uri targetUri = ResolveRedirectLocation(currentRequest, response, state);
        bool sameOrigin = IsSameOrigin(currentRequest.RequestUri, targetUri);
        global::System.Net.Http.HttpMethod method = SelectRedirectMethod(currentRequest.Method, (int)response.StatusCode);
        global::System.Net.Http.HttpRequestMessage nextRequest = new global::System.Net.Http.HttpRequestMessage(method, targetUri);
        nextRequest.Version = currentRequest.Version;
        nextRequest.VersionPolicy = currentRequest.VersionPolicy;
        CopyRequestHeaders(currentRequest, nextRequest);
        nextRequest.Headers.Remove("Host");

        bool preserveContent = method == currentRequest.Method;
        if (preserveContent && currentRequest.Content != null)
        {
            nextRequest.Content = CloneRequestContent(currentRequest.Content, cancellationToken);
        }

        if (!sameOrigin)
        {
            RemoveRequestCredentialHeaders(nextRequest.Headers);
            if (nextRequest.Content != null)
            {
                RemoveContentCredentialHeaders(nextRequest.Content.Headers);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        CommitRedirect(state, targetUri);
        return nextRequest;
    }

    internal static global::System.Uri ResolveRedirectLocation(global::System.Net.Http.HttpRequestMessage currentRequest, global::System.Net.Http.HttpResponseMessage response, RedirectState state)
    {
        if (currentRequest == null)
        {
            throw new ValidationException("A current HTTP request is required.", "currentRequest", "REQUIRED", null);
        }

        if (response == null)
        {
            throw new ValidationException("An HTTP redirect response is required.", "response", "REQUIRED", null);
        }

        if (state == null)
        {
            throw new ValidationException("Redirect state is required.", "state", "REQUIRED", null);
        }

        if (currentRequest.RequestUri == null || !currentRequest.RequestUri.IsAbsoluteUri)
        {
            throw new ValidationException("The current request URI must be absolute.", "currentRequest", "ABSOLUTE_URI_REQUIRED", null);
        }

        if (GetRedirectIdentity(currentRequest.RequestUri) != GetRedirectIdentity(state.CurrentUri))
        {
            throw new ValidationException("The current request does not match the active redirect chain.", "currentRequest", "REDIRECT_STATE_MISMATCH", null);
        }

        int statusCode = (int)response.StatusCode;
        if (!IsRedirectStatus(statusCode))
        {
            throw new ValidationException("The HTTP response is not a supported redirect.", "response", "UNSUPPORTED_REDIRECT_STATUS", null);
        }

        global::System.Uri? location = response.Headers.Location;
        if (location == null)
        {
            throw new ApiException("The redirect response did not provide a target location.", state.Service, statusCode, true, "REDIRECT_LOCATION_MISSING", null, null, false, null);
        }

        global::System.Uri targetUri = new global::System.Uri(currentRequest.RequestUri, location);
        if (!global::System.String.Equals(targetUri.Scheme, global::System.Uri.UriSchemeHttps, global::System.StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException("The redirect target must use HTTPS.", state.Service, statusCode, true, "REDIRECT_HTTPS_REQUIRED", null, null, false, null);
        }

        string identity = GetRedirectIdentity(targetUri);
        if (state.VisitedUris.Contains(identity))
        {
            throw new ApiException("The redirect chain contains a loop.", state.Service, statusCode, true, "REDIRECT_LOOP", null, null, false, null);
        }

        if (state.HopCount >= state.MaximumHopCount)
        {
            throw new ApiException("The redirect chain exceeds the configured hop limit.", state.Service, statusCode, true, "REDIRECT_LIMIT_EXCEEDED", null, null, false, null);
        }

        return targetUri;
    }

    internal static void CommitRedirect(RedirectState state, global::System.Uri targetUri)
    {
        state.VisitedUris.Add(GetRedirectIdentity(targetUri));
        state.HopCount += 1;
        state.CurrentUri = targetUri;
    }

    internal static global::System.Net.Http.HttpMethod SelectRedirectMethod(global::System.Net.Http.HttpMethod currentMethod, int statusCode)
    {
        if (statusCode == 303 && currentMethod != global::System.Net.Http.HttpMethod.Head)
        {
            return global::System.Net.Http.HttpMethod.Get;
        }

        if ((statusCode == 301 || statusCode == 302) && currentMethod == global::System.Net.Http.HttpMethod.Post)
        {
            return global::System.Net.Http.HttpMethod.Get;
        }

        return currentMethod;
    }

    internal static global::System.Net.Http.HttpContent CloneRequestContent(global::System.Net.Http.HttpContent content, global::System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byte[] body = content.ReadAsByteArrayAsync(cancellationToken).GetAwaiter().GetResult();
        global::System.Net.Http.ByteArrayContent clone = new global::System.Net.Http.ByteArrayContent(body);
        foreach (global::System.Collections.Generic.KeyValuePair<string, global::System.Collections.Generic.IEnumerable<string>> header in content.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    internal static void CopyRequestHeaders(global::System.Net.Http.HttpRequestMessage source, global::System.Net.Http.HttpRequestMessage destination)
    {
        foreach (global::System.Collections.Generic.KeyValuePair<string, global::System.Collections.Generic.IEnumerable<string>> header in source.Headers)
        {
            destination.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    internal static void RemoveRequestCredentialHeaders(global::System.Net.Http.Headers.HttpRequestHeaders headers)
    {
        headers.Remove("Authorization");
        headers.Remove("Proxy-Authorization");
        headers.Remove("Cookie");
        headers.Remove(UsptoHeaders.OdpApiKey);
        headers.Remove(UsptoHeaders.TsdrApiKey);
        headers.Remove(UsptoHeaders.PatentsViewApiKey);
    }

    internal static void RemoveContentCredentialHeaders(global::System.Net.Http.Headers.HttpContentHeaders headers)
    {
        headers.Remove(UsptoHeaders.OdpApiKey);
        headers.Remove(UsptoHeaders.TsdrApiKey);
        headers.Remove(UsptoHeaders.PatentsViewApiKey);
    }

    internal static bool IsSameOrigin(global::System.Uri? first, global::System.Uri second)
    {
        if (first == null)
        {
            return false;
        }

        return global::System.String.Equals(first.Scheme, second.Scheme, global::System.StringComparison.OrdinalIgnoreCase)
            && global::System.String.Equals(first.IdnHost, second.IdnHost, global::System.StringComparison.OrdinalIgnoreCase)
            && first.Port == second.Port;
    }

    internal static string GetRedirectIdentity(global::System.Uri uri)
    {
        global::System.Uri comparableUri = new global::System.Uri(uri.OriginalString, global::System.UriKind.Absolute);
        return comparableUri.GetComponents(global::System.UriComponents.SchemeAndServer | global::System.UriComponents.PathAndQuery, global::System.UriFormat.UriEscaped);
    }
}
