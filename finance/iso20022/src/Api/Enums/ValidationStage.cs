namespace iso20022;

/// <summary>Identifies one deterministic stage of ISO 20022 message validation.</summary>
public enum ValidationStage
{
    /// <summary>The validation stage is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>Secure XML parsing and well-formedness validation.</summary>
    SecureXml = 1,
    /// <summary>Envelope, namespace, header, and body coherence validation.</summary>
    EnvelopeAndHeader = 2,
    /// <summary>Exact local XSD validation.</summary>
    Xsd = 3,
    /// <summary>Generated model structure, order, multiplicity, and choice validation.</summary>
    Structural = 4,
    /// <summary>Primitive lexical and value-facet validation.</summary>
    PrimitiveFacet = 5,
    /// <summary>Closed repository code-set validation.</summary>
    InternalCode = 6,
    /// <summary>Independently versioned external code-set validation.</summary>
    ExternalCode = 7,
    /// <summary>Repository constraint validation.</summary>
    RepositoryConstraint = 8,
    /// <summary>Market-practice or caller profile validation.</summary>
    Profile = 9,
    /// <summary>Supplementary-data location and schema validation.</summary>
    SupplementaryData = 10,
    /// <summary>XML signature validation.</summary>
    Signature = 11,
    /// <summary>Built-in and caller-supplied semantic rule validation.</summary>
    Semantic = 12
}
