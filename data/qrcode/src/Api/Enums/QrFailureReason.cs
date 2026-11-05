namespace qrcode;

/// <summary>Identifies a machine-readable reason that a QR operation did not succeed.</summary>
public enum QrFailureReason
{
    /// <summary>The operation succeeded.</summary>
    None = 0,

    /// <summary>No symbol was found.</summary>
    NotFound = 1,

    /// <summary>Caller input or configuration was invalid.</summary>
    InvalidInput = 2,

    /// <summary>Symbol or image geometry was invalid.</summary>
    InvalidGeometry = 3,

    /// <summary>Format information was invalid or uncorrectable.</summary>
    InvalidFormat = 4,

    /// <summary>Version or size information was invalid.</summary>
    InvalidVersion = 5,

    /// <summary>An unsupported or illegal mode indicator was encountered.</summary>
    InvalidMode = 6,

    /// <summary>Encoded data was malformed.</summary>
    InvalidData = 7,

    /// <summary>The requested text conversion was not supported.</summary>
    UnsupportedTextEncoding = 8,

    /// <summary>Error correction could not verify or repair the symbol.</summary>
    ChecksumFailure = 9,

    /// <summary>The requested payload exceeds the permitted symbol capacity.</summary>
    CapacityExceeded = 10,

    /// <summary>A structured-append sequence is missing one or more parts.</summary>
    IncompleteSequence = 11,

    /// <summary>A structured-append sequence contains conflicting parts.</summary>
    ConflictingSequence = 12,

    /// <summary>A configured processing or allocation limit was reached.</summary>
    ResourceLimit = 13
}
