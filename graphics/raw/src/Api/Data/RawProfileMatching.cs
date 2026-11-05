namespace raw;

/// <summary>Contains normalized camera identity and observed structural evidence for profile matching.</summary>
public class RawProfileMatchInput
{
    /// <summary>The required container family.</summary>
    public RawFormat Format;
    /// <summary>The observed manufacturer text.</summary>
    public string Make;
    /// <summary>The observed camera model text.</summary>
    public string Model;
    /// <summary>The observed camera identifier, or an empty string.</summary>
    public string CameraId;
    /// <summary>The observed firmware text, or an empty string.</summary>
    public string Firmware;
    /// <summary>Whether byte-order evidence is available.</summary>
    public bool HasByteOrder;
    /// <summary>The observed byte order when available.</summary>
    public RawByteOrder ByteOrder;
    /// <summary>Whether classic-versus-BigTIFF evidence is available.</summary>
    public bool HasBigTiff;
    /// <summary>Whether the observed container is BigTIFF.</summary>
    public bool BigTiff;
    /// <summary>The observed stored width, or zero.</summary>
    public int Width;
    /// <summary>The observed stored height, or zero.</summary>
    public int Height;
    /// <summary>The observed meaningful bit depth, or zero.</summary>
    public int BitDepth;
    /// <summary>The observed compression identifier, or zero.</summary>
    public int Compression;
    /// <summary>The observed samples per pixel, or zero.</summary>
    public int SamplesPerPixel;
    /// <summary>Observed stable IFD paths.</summary>
    public string[] IfdPaths;
    /// <summary>Observed tags expressed as <c>path/tag</c> strings.</summary>
    public string[] Tags;
    /// <summary>The observed maker-note signature, or an empty string.</summary>
    public string? MakerNoteSignature;
    /// <summary>The observed structural digest, or an empty string.</summary>
    public string StructuralHash;
    /// <summary>Whether experimental profiles may participate in selection.</summary>
    public bool AllowExperimental;

    /// <summary>Creates an empty match input.</summary>
    public RawProfileMatchInput()
    {
        Format = RawFormat.Unknown;
        Make = string.Empty;
        Model = string.Empty;
        CameraId = string.Empty;
        Firmware = string.Empty;
        HasByteOrder = false;
        ByteOrder = RawByteOrder.LittleEndian;
        HasBigTiff = false;
        BigTiff = false;
        Width = 0;
        Height = 0;
        BitDepth = 0;
        Compression = 0;
        SamplesPerPixel = 0;
        IfdPaths = System.Array.Empty<string>();
        Tags = System.Array.Empty<string>();
        MakerNoteSignature = string.Empty;
        StructuralHash = string.Empty;
        AllowExperimental = false;
    }
}

/// <summary>Reports one ranked read-profile or write-profile match candidate.</summary>
public class RawProfileMatchCandidate
{
    /// <summary>The stable candidate identifier.</summary>
    public string ProfileId;
    /// <summary>The total deterministic score.</summary>
    public int Score;
    /// <summary>The identity portion of the score.</summary>
    public int IdentityScore;
    /// <summary>The structural fingerprint portion of the score.</summary>
    public int FingerprintScore;
    /// <summary>Whether the candidate is experimental.</summary>
    public bool Experimental;
    /// <summary>Whether the candidate is eligible under the request's experimental policy.</summary>
    public bool Eligible;
    /// <summary>The read profile, or <c>null</c> for a write-profile candidate.</summary>
    public RawReadProfile? ReadProfile;
    /// <summary>The write profile, or <c>null</c> for a read-profile candidate.</summary>
    public RawWriteProfile? WriteProfile;

    /// <summary>Creates a ranked match candidate.</summary>
    /// <param name="profileId">The stable identifier.</param>
    /// <param name="score">The total score.</param>
    /// <param name="identityScore">The identity score.</param>
    /// <param name="fingerprintScore">The fingerprint score.</param>
    /// <param name="experimental">Whether the profile is experimental.</param>
    /// <param name="eligible">Whether the profile is eligible.</param>
    /// <param name="readProfile">The read profile, or <c>null</c>.</param>
    /// <param name="writeProfile">The write profile, or <c>null</c>.</param>
    public RawProfileMatchCandidate(string profileId, int score, int identityScore, int fingerprintScore, bool experimental, bool eligible, RawReadProfile? readProfile, RawWriteProfile? writeProfile)
    {
        ProfileId = profileId == null ? string.Empty : profileId;
        Score = score;
        IdentityScore = identityScore;
        FingerprintScore = fingerprintScore;
        Experimental = experimental;
        Eligible = eligible;
        ReadProfile = readProfile;
        WriteProfile = writeProfile;
    }
}

/// <summary>Reports deterministic profile selection, ranked candidates, ambiguity, and policy diagnostics.</summary>
public class RawProfileMatchResult
{
    /// <summary>Whether exactly one eligible best profile was selected.</summary>
    public bool Matched;
    /// <summary>Whether multiple eligible candidates tied for the highest score.</summary>
    public bool Ambiguous;
    /// <summary>The selected read profile, or <c>null</c>.</summary>
    public RawReadProfile? ReadProfile;
    /// <summary>The selected write profile, or <c>null</c>.</summary>
    public RawWriteProfile? WriteProfile;
    /// <summary>All structurally compatible candidates in deterministic score order.</summary>
    public RawProfileMatchCandidate[] Candidates;
    /// <summary>Selection, ambiguity, and experimental-policy diagnostics.</summary>
    public RawDiagnostic[] Diagnostics;

    /// <summary>Creates a profile match result.</summary>
    /// <param name="matched">Whether one profile was selected.</param>
    /// <param name="ambiguous">Whether the best score was tied.</param>
    /// <param name="readProfile">The selected read profile.</param>
    /// <param name="writeProfile">The selected write profile.</param>
    /// <param name="candidates">Ranked candidates.</param>
    /// <param name="diagnostics">Match diagnostics.</param>
    public RawProfileMatchResult(bool matched, bool ambiguous, RawReadProfile? readProfile, RawWriteProfile? writeProfile, RawProfileMatchCandidate[] candidates, RawDiagnostic[] diagnostics)
    {
        Matched = matched;
        Ambiguous = ambiguous;
        ReadProfile = readProfile;
        WriteProfile = writeProfile;
        Candidates = candidates == null ? System.Array.Empty<RawProfileMatchCandidate>() : candidates;
        Diagnostics = diagnostics == null ? System.Array.Empty<RawDiagnostic>() : diagnostics;
    }
}
