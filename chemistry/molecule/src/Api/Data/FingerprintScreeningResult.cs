namespace molecule;


/// <summary>
/// Holds fingerprints, screening disposition, and the optional exact substructure match.
/// </summary>
public class FingerprintScreeningResult
{
    /// <summary>
    /// The target fingerprint used by the screen.
    /// </summary>
    public BitFingerprint TargetFingerprint;
    /// <summary>
    /// The query fingerprint used by the screen.
    /// </summary>
    public BitFingerprint QueryFingerprint;
    /// <summary>
    /// The exact match, or an unsuccessful match when screening rejected the query.
    /// </summary>
    public SubgraphMatch Match;
    /// <summary>
    /// Diagnostics produced while screening and matching.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// Whether all query fingerprint bits occurred in the target fingerprint.
    /// </summary>
    public bool ScreenPassed;
    /// <summary>
    /// Whether exact substructure matching was attempted after screening.
    /// </summary>
    public bool ExactMatchAttempted;
    /// <summary>
    /// Whether an exact substructure match was found.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes an empty fingerprint-screening result.
    /// </summary>
    public FingerprintScreeningResult()
    {
        TargetFingerprint = new BitFingerprint(1024);
        QueryFingerprint = new BitFingerprint(1024);
        Match = new SubgraphMatch();
        Report = new MoleculeReport();
        ScreenPassed = false;
        ExactMatchAttempted = false;
        Success = false;
    }
}
