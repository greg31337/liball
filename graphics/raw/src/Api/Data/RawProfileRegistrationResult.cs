namespace raw;

/// <summary>Reports one atomic profile-catalogue registration attempt.</summary>
public class RawProfileRegistrationResult
{
    /// <summary>Whether the complete registration was accepted.</summary>
    public bool Accepted;
    /// <summary>Read profiles added under new identifiers.</summary>
    public RawReadProfile[] AddedReadProfiles;
    /// <summary>Read profiles that replaced lower-precedence entries.</summary>
    public RawReadProfile[] ReplacedReadProfiles;
    /// <summary>Write profiles added under new identifiers.</summary>
    public RawWriteProfile[] AddedWriteProfiles;
    /// <summary>Write profiles that replaced lower-precedence entries.</summary>
    public RawWriteProfile[] ReplacedWriteProfiles;
    /// <summary>Conflict, precedence, and ambiguity diagnostics produced by this attempt.</summary>
    public RawDiagnostic[] Diagnostics;

    /// <summary>Creates a profile registration result.</summary>
    /// <param name="accepted">Whether the registration was accepted.</param>
    /// <param name="addedReadProfiles">New read profiles.</param>
    /// <param name="replacedReadProfiles">Replaced read profiles.</param>
    /// <param name="addedWriteProfiles">New write profiles.</param>
    /// <param name="replacedWriteProfiles">Replaced write profiles.</param>
    /// <param name="diagnostics">Registration diagnostics.</param>
    public RawProfileRegistrationResult(bool accepted, RawReadProfile[]? addedReadProfiles, RawReadProfile[]? replacedReadProfiles, RawWriteProfile[]? addedWriteProfiles, RawWriteProfile[]? replacedWriteProfiles, RawDiagnostic[] diagnostics)
    {
        Accepted = accepted;
        AddedReadProfiles = addedReadProfiles == null ? System.Array.Empty<RawReadProfile>() : addedReadProfiles;
        ReplacedReadProfiles = replacedReadProfiles == null ? System.Array.Empty<RawReadProfile>() : replacedReadProfiles;
        AddedWriteProfiles = addedWriteProfiles == null ? System.Array.Empty<RawWriteProfile>() : addedWriteProfiles;
        ReplacedWriteProfiles = replacedWriteProfiles == null ? System.Array.Empty<RawWriteProfile>() : replacedWriteProfiles;
        Diagnostics = diagnostics == null ? System.Array.Empty<RawDiagnostic>() : diagnostics;
    }
}
