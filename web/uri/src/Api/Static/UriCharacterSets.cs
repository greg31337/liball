namespace uri;

/// <summary>
/// Provides the ASCII character sets defined by the RFC 3986 generic URI syntax.
/// </summary>
public static class UriCharacterSets
{
    /// <summary>The characters reserved as generic or component-specific delimiters.</summary>
    public const string Reserved = ":/?#[]@!$&'()*+,;=";

    /// <summary>The generic delimiter characters.</summary>
    public const string GeneralDelimiters = ":/?#[]@";

    /// <summary>The subcomponent delimiter characters.</summary>
    public const string SubDelimiters = "!$&'()*+,;=";

    /// <summary>The uppercase letters, lowercase letters, digits, hyphen, period, underscore, and tilde.</summary>
    public const string Unreserved = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
}
