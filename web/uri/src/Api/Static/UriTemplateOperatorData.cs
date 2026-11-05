namespace uri;

/// <summary>
/// Provides the operator characters defined or reserved by RFC 6570.
/// </summary>
public static class UriTemplateOperatorData
{
    /// <summary>The reserved expansion operator.</summary>
    public const char ReservedExpansion = '+';

    /// <summary>The fragment expansion operator.</summary>
    public const char FragmentExpansion = '#';

    /// <summary>The label expansion operator.</summary>
    public const char LabelExpansion = '.';

    /// <summary>The path segment expansion operator.</summary>
    public const char PathSegmentExpansion = '/';

    /// <summary>The path parameter expansion operator.</summary>
    public const char PathParameterExpansion = ';';

    /// <summary>The query expansion operator.</summary>
    public const char QueryExpansion = '?';

    /// <summary>The query continuation expansion operator.</summary>
    public const char QueryContinuationExpansion = '&';

    /// <summary>The operator characters reserved for future template extensions.</summary>
    public const string ReservedFutureOperators = "=,!@|";
}
