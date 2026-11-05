namespace uspto;

/// <summary>
/// Identifies a supported Patent Trial and Appeal Board collection.
/// </summary>
public enum PtabCollection
{
    /// <summary>PTAB trial proceeding records.</summary>
    TrialProceedings = 0,

    /// <summary>Decisions filed in PTAB trials.</summary>
    TrialDecisions = 1,

    /// <summary>Documents filed in PTAB trials.</summary>
    TrialDocuments = 2,

    /// <summary>PTAB appeal decisions.</summary>
    AppealDecisions = 3,

    /// <summary>PTAB interference decisions.</summary>
    InterferenceDecisions = 4
}
