namespace iso20022;

/// <summary>Reports containment, transform, and digest validation for one XML signature reference.</summary>
public class SignatureReferenceResult
{
    /// <summary>The exact reference URI from the signature.</summary>
    public string Uri = string.Empty;
    /// <summary>The resolved target path when the reference is contained and unambiguous.</summary>
    public XmlPathData TargetPath = new XmlPathData();
    /// <summary>The digest algorithm URI.</summary>
    public string DigestAlgorithm = string.Empty;
    /// <summary>The signed digest value.</summary>
    public IsoBinaryValue ExpectedDigest = new IsoBinaryValue();
    /// <summary>The digest calculated from the resolved target.</summary>
    public IsoBinaryValue CalculatedDigest = new IsoBinaryValue();
    /// <summary>The reference validation outcome.</summary>
    public SignatureReferenceStatus Status;
    /// <summary>Whether the target is contained within the permitted message boundary.</summary>
    public bool ContainedInMessage;
}
