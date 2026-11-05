namespace whois;

internal static class InputGuard
{
    internal static void RequireReference(object? value, string parameterName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(parameterName);
        }
    }

    internal static void ValidateQueryText(string? queryText, WhoisQueryKind queryKind)
    {
        if (string.IsNullOrWhiteSpace(queryText))
        {
            throw CreateQueryError("Query text must not be empty or whitespace.", "QueryText", string.Empty, queryKind, null);
        }

        if (ContainsControlCharacters(queryText))
        {
            throw CreateQueryError("Query text must not contain control characters.", "QueryText", queryText, queryKind, null);
        }
    }

    internal static void ValidateEndpoint(WhoisEndpoint? endpoint, string queryText, WhoisQueryKind queryKind)
    {
        if (endpoint == null)
        {
            throw CreateQueryError("A WHOIS endpoint is required.", "Endpoint", queryText, queryKind, null);
        }

        if (string.IsNullOrWhiteSpace(endpoint.Host))
        {
            throw CreateQueryError("The endpoint host must not be empty or whitespace.", "Endpoint.Host", queryText, queryKind, endpoint);
        }

        if (endpoint.Host != endpoint.Host.Trim())
        {
            throw CreateQueryError("The endpoint host must not have surrounding whitespace.", "Endpoint.Host", queryText, queryKind, endpoint);
        }

        if (ContainsControlCharacters(endpoint.Host))
        {
            throw CreateQueryError("The endpoint host must not contain control characters.", "Endpoint.Host", queryText, queryKind, endpoint);
        }

        if (endpoint.Host.IndexOf('/') >= 0 || endpoint.Host.IndexOf('\\') >= 0 || endpoint.Host.IndexOf('@') >= 0 || endpoint.Host.IndexOf('?') >= 0 || endpoint.Host.IndexOf('#') >= 0)
        {
            throw CreateQueryError("The endpoint host must not contain URI components.", "Endpoint.Host", queryText, queryKind, endpoint);
        }

        if (System.Uri.CheckHostName(endpoint.Host) == System.UriHostNameType.Unknown)
        {
            throw CreateQueryError("The endpoint host is not a valid DNS name or IP literal.", "Endpoint.Host", queryText, queryKind, endpoint);
        }

        if (endpoint.Port < 1 || endpoint.Port > 65535)
        {
            throw CreateQueryError("The endpoint port must be between 1 and 65535.", "Endpoint.Port", queryText, queryKind, endpoint);
        }
    }

    internal static void ValidateLimits(WhoisLimits? limits, string queryText, WhoisQueryKind queryKind)
    {
        if (limits == null)
        {
            throw CreateQueryError("Resource limits are required.", "Limits", queryText, queryKind, null);
        }

        if (limits.MaximumEncodedQueryBytes <= 0)
        {
            throw CreateQueryError("The encoded query byte limit must be positive.", "Limits.MaximumEncodedQueryBytes", queryText, queryKind, null);
        }

        if (limits.MaximumResponseBytes <= 0)
        {
            throw CreateQueryError("The response byte limit must be positive.", "Limits.MaximumResponseBytes", queryText, queryKind, null);
        }

        if (limits.MaximumReferralHops <= 0)
        {
            throw CreateQueryError("The referral hop limit must be positive.", "Limits.MaximumReferralHops", queryText, queryKind, null);
        }

        if (limits.MaximumLines <= 0)
        {
            throw CreateQueryError("The response line limit must be positive.", "Limits.MaximumLines", queryText, queryKind, null);
        }

        if (limits.MaximumFields <= 0)
        {
            throw CreateQueryError("The response field limit must be positive.", "Limits.MaximumFields", queryText, queryKind, null);
        }
    }

    internal static void ValidateTimeouts(WhoisTimeoutOptions? timeouts, string queryText, WhoisQueryKind queryKind)
    {
        if (timeouts == null)
        {
            throw CreateQueryError("Timeout options are required.", "Timeouts", queryText, queryKind, null);
        }

        ValidateStageTimeout(timeouts.Resolve, "Timeouts.Resolve", queryText, queryKind);
        ValidateStageTimeout(timeouts.Connect, "Timeouts.Connect", queryText, queryKind);
        ValidateStageTimeout(timeouts.Write, "Timeouts.Write", queryText, queryKind);
        ValidateStageTimeout(timeouts.ReadIdle, "Timeouts.ReadIdle", queryText, queryKind);

        if (timeouts.Overall != System.Threading.Timeout.InfiniteTimeSpan)
        {
            ValidateStageTimeout(timeouts.Overall, "Timeouts.Overall", queryText, queryKind);
            if (timeouts.Overall < timeouts.Resolve || timeouts.Overall < timeouts.Connect || timeouts.Overall < timeouts.Write || timeouts.Overall < timeouts.ReadIdle)
            {
                throw CreateQueryError("The overall timeout must not be shorter than a stage timeout.", "Timeouts.Overall", queryText, queryKind, null);
            }
        }
    }

    internal static bool ContainsControlCharacters(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (char.IsControl(value[index]))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static void ValidateStageTimeout(System.TimeSpan timeout, string parameterName, string queryText, WhoisQueryKind queryKind)
    {
        if (timeout <= System.TimeSpan.Zero)
        {
            throw CreateQueryError("A stage timeout must be positive.", parameterName, queryText, queryKind, null);
        }

        if (timeout > System.TimeSpan.FromMilliseconds(2147483647))
        {
            throw CreateQueryError("A timeout exceeds the supported millisecond range.", parameterName, queryText, queryKind, null);
        }
    }

    internal static WhoisQueryException CreateQueryError(string message, string invalidParameter, string queryText, WhoisQueryKind queryKind, WhoisEndpoint? endpoint)
    {
        return new WhoisQueryException(message, invalidParameter, queryText, queryKind, endpoint, null);
    }
}
