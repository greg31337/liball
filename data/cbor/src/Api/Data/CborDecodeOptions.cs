using System.Collections.Generic;

namespace cbor;

/// <summary>
/// Holds policies applied while decoding CBOR data.
/// </summary>
public class CborDecodeOptions
{
    /// <summary>Indicates whether bytes may remain after the requested item.</summary>
    public bool AllowTrailingData;
    /// <summary>The policy for equivalent map keys.</summary>
    public CborDuplicateKeyPolicy DuplicateKeyPolicy = CborDuplicateKeyPolicy.Reject;
    /// <summary>The policy for semantic tags.</summary>
    public CborTagPolicy TagPolicy = CborTagPolicy.ValidateKnown;
    /// <summary>The policy for string references and shared values.</summary>
    public CborSharedValuePolicy SharedValuePolicy = CborSharedValuePolicy.Disabled;
    /// <summary>The highest validation layer applied during decoding.</summary>
    public CborValidationLevel ValidationLevel = CborValidationLevel.Basic;
    /// <summary>The deterministic encoding profile checked by deterministic and application validation.</summary>
    public CborEncodingProfile DeterministicEncodingProfile = CborEncodingProfile.CoreDeterministic;
    /// <summary>Indicates whether indefinite-length strings and containers are accepted.</summary>
    public bool AllowIndefiniteLength = true;
    /// <summary>Indicates whether unknown, non-reserved simple values are accepted.</summary>
    public bool AllowUnknownSimpleValues = true;
    /// <summary>Indicates whether malformed UTF-8 text strings are rejected.</summary>
    public bool RejectInvalidUtf8 = true;
    /// <summary>Indicates whether original representation metadata is retained.</summary>
    public bool RetainRepresentation;
    /// <summary>Indicates whether encoded source ranges are retained.</summary>
    public bool RetainSourceRanges;
    /// <summary>The maximum nested array, map, tag, and indefinite-string depth.</summary>
    public int MaxNestingDepth = 64;
    /// <summary>The maximum total number of decoded items.</summary>
    public int MaxTotalItems = 1000000;
    /// <summary>The maximum number of items in one array.</summary>
    public int MaxArrayItems = 100000;
    /// <summary>The maximum number of pairs in one map.</summary>
    public int MaxMapPairs = 100000;
    /// <summary>The maximum encoded bytes in one byte or text string.</summary>
    public int MaxStringBytes = 16777216;
    /// <summary>The maximum encoded bytes consumed by one operation.</summary>
    public long MaxTotalBytes = 67108864;
    /// <summary>The maximum source bytes retained for representation preservation.</summary>
    public int MaxRetainedSourceBytes = 16777216;
    /// <summary>The maximum number of top-level items in one CBOR Sequence operation.</summary>
    public int MaxSequenceItems = 1000000;
    /// <summary>The maximum encoded bytes in one CBOR Sequence operation.</summary>
    public long MaxSequenceBytes = 67108864;
    /// <summary>The maximum recursion depth for embedded CBOR processing.</summary>
    public int MaxEmbeddedDepth = 8;
    /// <summary>The maximum number of validation issues collected by one operation.</summary>
    public int MaxValidationIssues = 100;
    /// <summary>Indicates whether validation collects multiple issues instead of stopping after the first.</summary>
    public bool CollectValidationIssues = true;
    /// <summary>The tag handlers available to this operation.</summary>
    public List<CborTagRegistration> TagRegistrations = new List<CborTagRegistration>();
}
