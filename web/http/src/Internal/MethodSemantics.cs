namespace http;

internal static class MethodSemantics
{
    internal static HttpMethodMetadata Describe(string method)
    {
        if (!IsValidMethod(method))
        {
            throw new HttpProtocolException(
                "The HTTP method is not a valid token.",
                "invalid-method");
        }

        HttpMethodMetadata result = new HttpMethodMetadata();
        result.Method = method;
        result.IsRegistered = IsRegistered(method);
        if (!result.IsRegistered)
        {
            return result;
        }

        result.IsSafe = IsSafe(method);
        result.IsIdempotent = IsIdempotent(method);
        ApplyCoreSemantics(result);
        return result;
    }

    static bool IsValidMethod(string method)
    {
        if (method == null || method.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < method.Length)
        {
            char character = method[index];
            if (character > 0x7f || !HttpGrammar.IsTokenCharacter((byte)character))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsRegistered(string method)
    {
        switch (method)
        {
            case HttpMethods.Acl:
            case HttpMethods.BaselineControl:
            case HttpMethods.Bind:
            case HttpMethods.CheckIn:
            case HttpMethods.CheckOut:
            case HttpMethods.Connect:
            case HttpMethods.Copy:
            case HttpMethods.Delete:
            case HttpMethods.Get:
            case HttpMethods.Head:
            case HttpMethods.Label:
            case HttpMethods.Link:
            case HttpMethods.Lock:
            case HttpMethods.Merge:
            case HttpMethods.MkActivity:
            case HttpMethods.MkCalendar:
            case HttpMethods.MkCol:
            case HttpMethods.MkRedirectRef:
            case HttpMethods.MkWorkspace:
            case HttpMethods.Move:
            case HttpMethods.Options:
            case HttpMethods.OrderPatch:
            case HttpMethods.Patch:
            case HttpMethods.Post:
            case HttpMethods.Pri:
            case HttpMethods.PropFind:
            case HttpMethods.PropPatch:
            case HttpMethods.Put:
            case HttpMethods.Query:
            case HttpMethods.Rebind:
            case HttpMethods.Report:
            case HttpMethods.Search:
            case HttpMethods.Trace:
            case HttpMethods.Unbind:
            case HttpMethods.Uncheckout:
            case HttpMethods.Unlink:
            case HttpMethods.Unlock:
            case HttpMethods.Update:
            case HttpMethods.UpdateRedirectRef:
            case HttpMethods.VersionControl:
            case HttpMethods.Wildcard:
                return true;
            default:
                return false;
        }
    }

    static bool IsSafe(string method)
    {
        switch (method)
        {
            case HttpMethods.Get:
            case HttpMethods.Head:
            case HttpMethods.Options:
            case HttpMethods.Pri:
            case HttpMethods.PropFind:
            case HttpMethods.Query:
            case HttpMethods.Report:
            case HttpMethods.Search:
            case HttpMethods.Trace:
                return true;
            default:
                return false;
        }
    }

    static bool IsIdempotent(string method)
    {
        return method != HttpMethods.Connect && method != HttpMethods.Lock &&
            method != HttpMethods.Patch && method != HttpMethods.Post &&
            method != HttpMethods.Wildcard;
    }

    static void ApplyCoreSemantics(HttpMethodMetadata result)
    {
        string method = result.Method;
        if (method == HttpMethods.Get || method == HttpMethods.Head)
        {
            result.IsCore = true;
            result.IsCacheable = true;
            return;
        }

        if (method == HttpMethods.Post)
        {
            result.IsCore = true;
            result.IsCacheable = true;
            result.HasDefinedRequestContentSemantics = true;
            return;
        }

        if (method == HttpMethods.Put)
        {
            result.IsCore = true;
            result.HasDefinedRequestContentSemantics = true;
            return;
        }

        if (method == HttpMethods.Delete || method == HttpMethods.Connect ||
            method == HttpMethods.Options || method == HttpMethods.Trace)
        {
            result.IsCore = true;
        }
    }
}
