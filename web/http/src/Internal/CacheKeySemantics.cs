using System.Globalization;
using System.Text;

namespace http;

internal static class CacheKeySemantics
{
    internal static HttpCacheDecision Build(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        ValidateContexts(requestContext, responseContext);
        HttpRequest request = GetRequest(requestContext);
        HttpResponse response = GetResponse(responseContext);
        HttpMethodMetadata method = MethodSemantics.Describe(request.Method);
        if (!method.IsCacheable)
        {
            return CreateUnusable(
                requestContext,
                responseContext,
                "The request method has no compatible cache key semantics.");
        }

        string targetUri = NormalizeTargetUri(
            RequestTargetSemantics.BuildEffectiveUri(request));
        string primaryKey = request.Method + " " + targetUri;
        HttpHeaders varyFields = VarySemantics.Parse(response.Headers);
        string secondaryKey = VarySemantics.BuildKey(request, varyFields);
        if (secondaryKey == "V1;*")
        {
            HttpCacheDecision unusable = CreateUnusable(
                requestContext,
                responseContext,
                "A Vary wildcard prevents selection of this stored response.");
            unusable.PrimaryKey = primaryKey;
            unusable.SecondaryKey = secondaryKey;
            return unusable;
        }

        HttpCacheDecision result = new HttpCacheDecision();
        result.Outcome = HttpCacheOutcome.Store;
        result.RequestContext = requestContext;
        result.ResponseContext = responseContext;
        result.Response = response;
        result.PrimaryKey = primaryKey;
        result.SecondaryKey = secondaryKey;
        result.Reason = "The primary and Vary-derived cache keys were constructed.";
        return result;
    }

    internal static bool AreMethodsCompatible(
        string storedMethod,
        string presentedMethod)
    {
        HttpMethodMetadata stored = MethodSemantics.Describe(storedMethod);
        HttpMethodMetadata presented = MethodSemantics.Describe(presentedMethod);
        if (!stored.IsCacheable || !presented.IsCacheable)
        {
            return false;
        }

        if (presentedMethod == HttpMethods.Post)
        {
            return false;
        }

        if (presentedMethod == HttpMethods.Get)
        {
            return storedMethod == HttpMethods.Get ||
                storedMethod == HttpMethods.Post;
        }

        return presentedMethod == HttpMethods.Head;
    }

    internal static bool MatchesVariant(
        HttpCacheDecision storedKey,
        HttpCacheDecision presentedKey)
    {
        ValidateKeyDecision(storedKey);
        ValidateKeyDecision(presentedKey);
        if (storedKey.Outcome == HttpCacheOutcome.Unusable ||
            presentedKey.Outcome == HttpCacheOutcome.Unusable)
        {
            return false;
        }

        HttpRequest storedRequest = GetRequest(storedKey.RequestContext);
        HttpRequest presentedRequest = GetRequest(presentedKey.RequestContext);
        if (!AreMethodsCompatible(storedRequest.Method, presentedRequest.Method) ||
            ReadTargetKey(storedKey.PrimaryKey) !=
            ReadTargetKey(presentedKey.PrimaryKey))
        {
            return false;
        }

        return VarySemantics.CompareKeys(
            GetPrimaryOrSecondary(storedKey.SecondaryKey),
            GetPrimaryOrSecondary(presentedKey.SecondaryKey));
    }

    static void ValidateContexts(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext)
    {
        if (requestContext == null || responseContext == null ||
            requestContext.Request == null || responseContext.Response == null ||
            responseContext.Response.Headers == null ||
            responseContext.Response.Headers.Fields == null)
        {
            throw new HttpProtocolException(
                "The cache key context is incomplete.",
                "invalid-cache-key-context");
        }
    }

    static void ValidateKeyDecision(HttpCacheDecision decision)
    {
        if (decision == null || decision.RequestContext == null ||
            decision.RequestContext.Request == null || decision.PrimaryKey == null ||
            decision.SecondaryKey == null)
        {
            throw new HttpProtocolException(
                "A cache key decision is incomplete.",
                "invalid-cache-key-decision");
        }
    }

    static HttpRequest GetRequest(HttpCacheRequestContext? context)
    {
        if (context == null)
        {
            throw new HttpProtocolException(
                "The cache key request context is missing.",
                "invalid-cache-key-context");
        }

        HttpRequest? request = context.Request;
        if (request == null)
        {
            throw new HttpProtocolException(
                "The cache key request is missing.",
                "invalid-cache-key-context");
        }

        return request;
    }

    static HttpResponse GetResponse(HttpCacheResponseContext context)
    {
        HttpResponse? response = context.Response;
        if (response == null)
        {
            throw new HttpProtocolException(
                "The cache key response is missing.",
                "invalid-cache-key-context");
        }

        return response;
    }

    static string GetPrimaryOrSecondary(string? key)
    {
        if (key == null)
        {
            throw new HttpProtocolException(
                "A normalized cache key is missing.",
                "invalid-cache-key-decision");
        }

        return key;
    }

    static string ReadTargetKey(string? primaryKey)
    {
        if (primaryKey == null)
        {
            throw new HttpProtocolException(
                "A normalized primary cache key is missing.",
                "invalid-cache-key-decision");
        }

        int separator = primaryKey.IndexOf(' ');
        if (separator <= 0 || separator == primaryKey.Length - 1)
        {
            throw new HttpProtocolException(
                "A normalized primary cache key is malformed.",
                "invalid-cache-key-decision");
        }

        return primaryKey.Substring(separator + 1);
    }

    internal static string NormalizeTargetUri(string effectiveUri)
    {
        UriValidationResult parsed = UriCodec.ValidateUriReference(
            effectiveUri,
            int.MaxValue);
        if (parsed.Status != HttpOperationStatus.Done || !parsed.IsAbsolute ||
            parsed.HasFragment || parsed.HasUserInfo ||
            parsed.HasPort && parsed.HasPortValue && !parsed.IsPortInRange)
        {
            throw new HttpProtocolException(
                "The effective target URI cannot form a cache key.",
                "invalid-cache-key-target");
        }

        string scheme = ToLowercaseAscii(parsed.Scheme);
        StringBuilder result = new StringBuilder();
        result.Append(scheme);
        result.Append(':');
        if (parsed.HasAuthority)
        {
            result.Append("//");
            result.Append(NormalizeHost(parsed.Host));
            if (parsed.HasPortValue && !IsDefaultPort(scheme, parsed.Port))
            {
                result.Append(':');
                result.Append(parsed.Port.ToString(CultureInfo.InvariantCulture));
            }
        }

        string path = NormalizePercentEncoding(parsed.Path, false);
        path = RemoveDotSegments(path);
        if (parsed.HasAuthority && path.Length == 0)
        {
            path = "/";
        }

        result.Append(path);
        if (parsed.HasQuery)
        {
            result.Append('?');
            result.Append(NormalizePercentEncoding(parsed.Query, false));
        }

        return result.ToString();
    }

    static string NormalizeHost(string host)
    {
        return NormalizePercentEncoding(host, true);
    }

    static string NormalizePercentEncoding(string value, bool lowercase)
    {
        const string Hex = "0123456789ABCDEF";
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '%')
            {
                int decoded = DecodeHex(value[index + 1]) * 16 +
                    DecodeHex(value[index + 2]);
                if (IsUnreserved(decoded))
                {
                    char decodedCharacter = (char)decoded;
                    result.Append(lowercase ?
                        ToLowercaseCharacter(decodedCharacter) : decodedCharacter);
                }
                else
                {
                    result.Append('%');
                    result.Append(Hex[decoded >> 4]);
                    result.Append(Hex[decoded & 0x0f]);
                }

                index = index + 3;
            }
            else
            {
                result.Append(lowercase ?
                    ToLowercaseCharacter(character) : character);
                index = index + 1;
            }
        }

        return result.ToString();
    }

    static int DecodeHex(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return character - '0';
        }

        if (character >= 'A' && character <= 'F')
        {
            return character - 'A' + 10;
        }

        return character - 'a' + 10;
    }

    static bool IsUnreserved(int value)
    {
        return value >= 'A' && value <= 'Z' ||
            value >= 'a' && value <= 'z' ||
            value >= '0' && value <= '9' ||
            value == '-' || value == '.' || value == '_' || value == '~';
    }

    static char ToLowercaseCharacter(char character)
    {
        if (character >= 'A' && character <= 'Z')
        {
            return (char)(character + ('a' - 'A'));
        }

        return character;
    }

    static string ToLowercaseAscii(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            result.Append(ToLowercaseCharacter(value[index]));
            index = index + 1;
        }

        return result.ToString();
    }

    static bool IsDefaultPort(string scheme, int port)
    {
        return scheme == "http" && port == 80 ||
            scheme == "https" && port == 443;
    }

    static string RemoveDotSegments(string path)
    {
        string input = path;
        StringBuilder output = new StringBuilder();
        while (input.Length != 0)
        {
            if (input.StartsWith("../", System.StringComparison.Ordinal))
            {
                input = input.Substring(3);
            }
            else if (input.StartsWith("./", System.StringComparison.Ordinal))
            {
                input = input.Substring(2);
            }
            else if (input.StartsWith("/./", System.StringComparison.Ordinal))
            {
                input = input.Substring(2);
            }
            else if (input == "/.")
            {
                input = "/";
            }
            else if (input.StartsWith("/../", System.StringComparison.Ordinal))
            {
                input = input.Substring(3);
                RemoveLastPathSegment(output);
            }
            else if (input == "/..")
            {
                input = "/";
                RemoveLastPathSegment(output);
            }
            else if (input == "." || input == "..")
            {
                input = "";
            }
            else
            {
                int length = FirstPathSegmentLength(input);
                output.Append(input.Substring(0, length));
                input = input.Substring(length);
            }
        }

        return output.ToString();
    }

    static int FirstPathSegmentLength(string input)
    {
        int start = input[0] == '/' ? 1 : 0;
        int slash = input.IndexOf('/', start);
        return slash < 0 ? input.Length : slash;
    }

    static void RemoveLastPathSegment(StringBuilder output)
    {
        int index = output.Length - 1;
        while (index >= 0 && output[index] != '/')
        {
            index = index - 1;
        }

        output.Length = index < 0 ? 0 : index;
    }

    static HttpCacheDecision CreateUnusable(
        HttpCacheRequestContext requestContext,
        HttpCacheResponseContext responseContext,
        string reason)
    {
        HttpCacheDecision result = new HttpCacheDecision();
        result.Outcome = HttpCacheOutcome.Unusable;
        result.RequestContext = requestContext;
        result.ResponseContext = responseContext;
        result.Reason = reason;
        return result;
    }
}
