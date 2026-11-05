using System.Text;

namespace http;

internal class Http2PushRequestResult
{
    internal HttpOperationStatus Status;
    internal HttpRequest? Request;
    internal string? ErrorCodeName;
    internal string? ErrorMessage;
}

internal static class Http2PushSemantics
{
    internal static Http2PushRequestResult ValidatePushRequest(
        HttpHeaders headers)
    {
        Http2PushRequestResult result = new Http2PushRequestResult();
        result.Status = HttpOperationStatus.Done;
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http2;
        bool regularSeen = false;
        bool methodSeen = false;
        bool schemeSeen = false;
        bool authoritySeen = false;
        bool pathSeen = false;
        string method = "";
        string scheme = "";
        string authority = "";
        string path = "";
        int index = 0;
        while (index < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[index];
            if (field == null || field.Name == null || field.Value == null)
            {
                return RejectPushRequest(
                    result,
                    "http2-invalid-push-request-field",
                    "A pushed request contains an invalid field.");
            }

            bool pseudo = field.Name.Length != 0 && field.Name[0] == ':';
            if (pseudo && regularSeen)
            {
                return RejectPushRequest(
                    result,
                    "http2-push-pseudo-header-order",
                    "Pushed request pseudo-header fields must precede regular fields.");
            }

            if (!pseudo)
            {
                regularSeen = true;
                request.Headers.Fields.Add(field);
                index = index + 1;
                continue;
            }

            if (!Ascii.IsValid(field.Value))
            {
                return RejectPushRequest(
                    result,
                    "http2-invalid-push-pseudo-header-value",
                    "A pushed request pseudo-header value must be ASCII.");
            }

            string value = Encoding.ASCII.GetString(field.Value);
            if (field.Name == ":method")
            {
                if (methodSeen)
                {
                    return RejectDuplicatePushPseudoHeader(result);
                }

                methodSeen = true;
                method = value;
            }
            else if (field.Name == ":scheme")
            {
                if (schemeSeen)
                {
                    return RejectDuplicatePushPseudoHeader(result);
                }

                schemeSeen = true;
                scheme = value;
            }
            else if (field.Name == ":authority")
            {
                if (authoritySeen)
                {
                    return RejectDuplicatePushPseudoHeader(result);
                }

                authoritySeen = true;
                authority = value;
            }
            else if (field.Name == ":path")
            {
                if (pathSeen)
                {
                    return RejectDuplicatePushPseudoHeader(result);
                }

                pathSeen = true;
                path = value;
            }
            else
            {
                return RejectPushRequest(
                    result,
                    "http2-invalid-push-pseudo-header",
                    "A pushed request contains a response or undefined pseudo-header field.");
            }

            index = index + 1;
        }

        if (!methodSeen || !schemeSeen || !pathSeen || method.Length == 0 ||
            scheme.Length == 0 || path.Length == 0)
        {
            return RejectPushRequest(
                result,
                "http2-incomplete-push-request",
                "A pushed request requires non-empty :method, :scheme, and :path fields.");
        }

        if (!IsValidMethodToken(method))
        {
            return RejectPushRequest(
                result,
                "http2-invalid-push-method",
                "The pushed request method is not a valid token.");
        }

        HttpMethodMetadata metadata = MethodSemantics.Describe(method);
        if (!metadata.IsSafe)
        {
            return RejectPushRequest(
                result,
                "http2-unsafe-push-method",
                "A pushed request method must be safe.");
        }

        if (!metadata.IsCacheable)
        {
            return RejectPushRequest(
                result,
                "http2-uncacheable-push-method",
                "A pushed request method must be cacheable.");
        }

        request.Method = method;
        request.MethodMetadata = metadata;
        request.Scheme = scheme;
        request.Authority = authoritySeen ? authority : null;
        request.Target = path;
        request.TargetForm = path == "*" ?
            HttpRequestTargetForm.Asterisk : HttpRequestTargetForm.Origin;
        int query = path.IndexOf('?');
        if (query >= 0)
        {
            request.Path = path.Substring(0, query);
            request.HasQuery = true;
            request.Query = path.Substring(query + 1);
        }
        else
        {
            request.Path = path;
        }

        request.IsComplete = true;
        result.Request = request;
        return result;
    }

    static bool IsValidMethodToken(string method)
    {
        int index = 0;
        while (index < method.Length)
        {
            char character = method[index];
            if (character > 0x7f ||
                !HttpGrammar.IsTokenCharacter((byte)character))
            {
                return false;
            }

            index = index + 1;
        }

        return method.Length != 0;
    }

    static Http2PushRequestResult RejectDuplicatePushPseudoHeader(
        Http2PushRequestResult result)
    {
        return RejectPushRequest(
            result,
            "http2-duplicate-push-pseudo-header",
            "A pushed request cannot repeat a pseudo-header field.");
    }

    static Http2PushRequestResult RejectPushRequest(
        Http2PushRequestResult result,
        string errorCodeName,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorCodeName = errorCodeName;
        result.ErrorMessage = errorMessage;
        return result;
    }
}
