namespace cookies;

internal static class CookiePathMatcher
{
    internal static CookiePathResult ExtractRequestPath(global::System.Uri uri)
    {
        CookieUriHostResult host = CookieUriHostExtractor.Extract(uri);
        if (!host.Success)
        {
            return CreateFailure(host.ErrorCode);
        }

        string path = uri.AbsolutePath;
        if (path.Length == 0 || path[0] != '/')
        {
            path = "/";
        }

        return CreateSuccess(path, false);
    }

    internal static CookiePathResult ComputeDefaultPath(global::System.Uri uri)
    {
        CookiePathResult requestPath = ExtractRequestPath(uri);
        if (!requestPath.Success)
        {
            return requestPath;
        }

        string defaultPath = ComputeDefaultFromEscapedPath(requestPath.Path);
        return CreateSuccess(defaultPath, true);
    }

    internal static CookiePathResult NormalizeSetPath(SetCookie cookie, global::System.Uri origin)
    {
        CookiePathResult defaultPath = ComputeDefaultPath(origin);
        if (!defaultPath.Success)
        {
            return defaultPath;
        }

        if (cookie == null || cookie.Path == null)
        {
            return CreateFailure(CookieErrorCode.InvalidPath);
        }

        if (!cookie.HasPath || cookie.Path.Length == 0 || cookie.Path[0] != '/')
        {
            return defaultPath;
        }

        int index = 0;
        while (index < cookie.Path.Length)
        {
            char character = cookie.Path[index];
            if (CookieCharacterRules.IsHeaderInjection(character))
            {
                return CreateFailure(CookieErrorCode.HeaderInjection);
            }

            if (!CookieCharacterRules.IsAttributeValueOctet(character))
            {
                return CreateFailure(CookieErrorCode.InvalidPath);
            }

            index = index + 1;
        }

        return CreateSuccess(cookie.Path, false);
    }

    internal static string ComputeDefaultFromEscapedPath(string requestPath)
    {
        if (requestPath == null || requestPath.Length == 0 || requestPath[0] != '/')
        {
            return "/";
        }

        int finalSlash = requestPath.Length - 1;
        while (finalSlash > 0 && requestPath[finalSlash] != '/')
        {
            finalSlash = finalSlash - 1;
        }

        if (finalSlash == 0)
        {
            return "/";
        }

        return requestPath.Substring(0, finalSlash);
    }

    internal static bool PathMatches(string requestPath, string cookiePath)
    {
        if (cookiePath == null || cookiePath.Length == 0 || cookiePath[0] != '/')
        {
            return false;
        }

        if (requestPath == null || requestPath.Length == 0)
        {
            requestPath = "/";
        }

        if (requestPath[0] != '/' || requestPath.Length < cookiePath.Length)
        {
            return false;
        }

        int index = 0;
        while (index < cookiePath.Length)
        {
            if (requestPath[index] != cookiePath[index])
            {
                return false;
            }

            index = index + 1;
        }

        if (requestPath.Length == cookiePath.Length)
        {
            return true;
        }

        return cookiePath[cookiePath.Length - 1] == '/' ||
            requestPath[cookiePath.Length] == '/';
    }

    static CookiePathResult CreateSuccess(string path, bool usedDefault)
    {
        CookiePathResult result = new CookiePathResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.None;
        result.Path = path;
        result.UsedDefault = usedDefault;
        return result;
    }

    static CookiePathResult CreateFailure(CookieErrorCode errorCode)
    {
        CookiePathResult result = new CookiePathResult();
        result.ErrorCode = errorCode;
        return result;
    }
}
