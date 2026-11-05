namespace qrcode;

/// <summary>Contains logical conformance failures, damage locations, and recoverability indicators.</summary>
public class QrValidationResult
{
    /// <summary>Whether every enabled logical conformance check passed.</summary>
    public bool IsConformant;

    /// <summary>The machine-readable failures found during validation.</summary>
    public QrFailureReason[] FailureReasons = new QrFailureReason[0];

    /// <summary>Human-readable validation failures in check order.</summary>
    public string[] Failures = new string[0];

    /// <summary>The row-major indices of modules known to be damaged or inconsistent.</summary>
    public int[] DamagedModuleIndices = new int[0];

    /// <summary>The structural roles represented by damaged modules.</summary>
    public QrModuleRole[] DamagedRoles = new QrModuleRole[0];

    /// <summary>The number of fixed-pattern mismatches.</summary>
    public int FixedPatternDamageCount;

    /// <summary>The number of format and version modules differing from reconstructed metadata, including unknown modules.</summary>
    /// <remarks>This count describes module damage independently of metadata correction success.</remarks>
    public int CriticalMetadataDamageCount;

    /// <summary>The number of data modules differing from the reconstructed payload, including unknown modules.</summary>
    public int PayloadDamageCount;

    /// <summary>The number of error-correction modules differing from reconstructed codewords, including unknown modules.</summary>
    public int ErrorCorrectionDamageCount;

    /// <summary>The number of unknown or unreadable modules.</summary>
    public int UnknownModuleCount;

    /// <summary>Whether redundant format information is mutually consistent.</summary>
    public bool FormatInformationConsistent;

    /// <summary>Whether dimension and version information are mutually consistent.</summary>
    public bool VersionInformationConsistent;

    /// <summary>Whether the observed quiet zone meets the selected profile requirement.</summary>
    public bool QuietZoneValid;

    /// <summary>Whether the symbol appears recoverable within its error-correction allowance.</summary>
    public bool IsRecoverable;
}
