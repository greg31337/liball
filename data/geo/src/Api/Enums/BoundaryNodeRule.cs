namespace geo;

/// <summary>Identifies the rule used to classify line endpoints as boundary.</summary>
public enum BoundaryNodeRule
{
    /// <summary>Endpoints with odd valence are boundary, as required by OGC SFA.</summary>
    Mod2 = 0,

    /// <summary>Every endpoint is boundary.</summary>
    Endpoint = 1,

    /// <summary>Only endpoints with valence greater than one are boundary.</summary>
    MultivalentEndpoint = 2,

    /// <summary>Only endpoints with valence one are boundary.</summary>
    MonovalentEndpoint = 3
}
