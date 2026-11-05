namespace uri;

/// <summary>
/// Identifies the minimum RFC 6570 feature level required by a URI Template.
/// </summary>
public enum UriTemplateLevel
{
    /// <summary>Simple single-variable string expansion.</summary>
    Level1 = 1,

    /// <summary>Reserved and fragment expansion.</summary>
    Level2 = 2,

    /// <summary>Multiple variables and delimiter-oriented operators.</summary>
    Level3 = 3,

    /// <summary>Prefix, explode, list, and associative value processing.</summary>
    Level4 = 4
}
