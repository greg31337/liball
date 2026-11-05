namespace ttf;

/// <summary>Identifies the configured source of one resolved fallback match.</summary>
public enum FontFallbackMatchKind
{
    /// <summary>The caller-requested concrete family supplied the match.</summary>
    RequestedFamily = 0,

    /// <summary>The concrete family configured for a generic alias supplied the match.</summary>
    GenericFamily = 1,

    /// <summary>A script-specific fallback family supplied the match.</summary>
    ScriptFallback = 2,

    /// <summary>A set-wide fallback family supplied the match.</summary>
    FallbackFamily = 3
}
