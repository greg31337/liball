namespace iso20022;

/// <summary>Identifies the policy used to select a message version.</summary>
public enum VersionSelectionKind
{
    /// <summary>Select one exact message identifier.</summary>
    Exact = 0,
    /// <summary>Select the version marked current by the chosen artifact.</summary>
    Current = 1,
    /// <summary>Select the latest compatible version.</summary>
    LatestCompatible = 2,
    /// <summary>Select a version within an inclusive range.</summary>
    Range = 3
}
