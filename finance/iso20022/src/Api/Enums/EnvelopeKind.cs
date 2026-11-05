namespace iso20022;

/// <summary>Identifies the physical layout of an ISO 20022 message document.</summary>
public enum EnvelopeKind
{
    /// <summary>The envelope layout is not known.</summary>
    Unknown = 0,
    /// <summary>A standalone <c>Document</c> root.</summary>
    StandaloneDocument = 1,
    /// <summary>A standalone <c>AppHdr</c> root.</summary>
    StandaloneHeader = 2,
    /// <summary>Sibling <c>AppHdr</c> and <c>Document</c> roots.</summary>
    SiblingHeaderAndDocument = 3,
    /// <summary>A business-message envelope.</summary>
    BusinessMessageEnvelope = 4,
    /// <summary>A business-file envelope containing multiple messages.</summary>
    BusinessFileEnvelope = 5,
    /// <summary>A caller-configured external wrapper.</summary>
    CustomWrapper = 6
}
