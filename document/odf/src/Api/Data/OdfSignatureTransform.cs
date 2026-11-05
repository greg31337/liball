using System.Xml;

namespace odf;

/// <summary>
/// Describes one transform declared by an XML Signature reference.
/// </summary>
public class OdfSignatureTransform
{
    /// <summary>
    /// The transform algorithm IRI.
    /// </summary>
    public string Algorithm = "";
    /// <summary>
    /// The exclusive-canonicalization inclusive-prefix lexical value.
    /// </summary>
    public string InclusiveNamespacePrefixes = "";
    /// <summary>
    /// Inclusive namespace prefixes after XML-whitespace tokenization.
    /// </summary>
    public string[] InclusivePrefixes = [];
    /// <summary>
    /// The preserved Transform subtree.
    /// </summary>
    public string OriginalXml = "";
    /// <summary>
    /// Whether the required transform metadata is present.
    /// </summary>
    public bool IsValid;
    internal XmlElement? TransformElement;
}
