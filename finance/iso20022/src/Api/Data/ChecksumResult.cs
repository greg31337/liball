using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains a deterministic message checksum, expected-value status, and duplicate-key findings.</summary>
public class ChecksumResult
{
    /// <summary>Whether checksum processing completed under the selected configuration and limits.</summary>
    public bool Success;
    /// <summary>The checksum calculation or verification outcome.</summary>
    public ChecksumStatus Status;
    /// <summary>The calculated checksum and its lexical encoding.</summary>
    public IsoBinaryValue Checksum = new IsoBinaryValue();
    /// <summary>Whether the request supplied an expected checksum.</summary>
    public bool HasExpectedChecksum;
    /// <summary>Whether the calculated checksum matches the supplied expected checksum.</summary>
    public bool MatchesExpected;
    /// <summary>The scope used for checksum calculation.</summary>
    public ChecksumScope Scope;
    /// <summary>The representation used for checksum calculation.</summary>
    public MessageEqualityMode EqualityMode;
    /// <summary>The applied hash algorithm name.</summary>
    public string Algorithm = string.Empty;
    /// <summary>The located semantic key paths that were duplicated.</summary>
    public List<XmlPathData> DuplicateKeyPaths = new List<XmlPathData>();
    /// <summary>The duplicate key values when reporting is permitted.</summary>
    public List<string> DuplicateKeys = new List<string>();
    /// <summary>Whether duplicate key values were intentionally redacted.</summary>
    public bool DuplicateKeysRedacted;
    /// <summary>The ordered located checksum and duplicate-key diagnostics.</summary>
    public List<ValidationDiagnostic> Diagnostics = new List<ValidationDiagnostic>();
    /// <summary>A stable operation error code when checksum processing could not complete.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable operation error message when checksum processing could not complete.</summary>
    public string ErrorMessage = string.Empty;
}
