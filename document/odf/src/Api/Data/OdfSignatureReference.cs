using System.Xml;

namespace odf;

/// <summary>
/// Describes one digest reference in XML Signature metadata.
/// </summary>
public class OdfSignatureReference
{
    /// <summary>
    /// The optional reference identifier.
    /// </summary>
    public string Id = "";
    /// <summary>
    /// The reference URI lexical value.
    /// </summary>
    public string Uri = "";
    /// <summary>
    /// Whether the URI attribute was present.
    /// </summary>
    public bool HasUri;
    /// <summary>
    /// The optional reference type IRI.
    /// </summary>
    public string Type = "";
    /// <summary>
    /// Whether the reference identifies content in the signature XML document.
    /// </summary>
    public bool IsSameDocument;
    /// <summary>
    /// Whether the reference is a valid package-relative reference.
    /// </summary>
    public bool IsPackageReference;
    /// <summary>
    /// Transforms in declared order.
    /// </summary>
    public OdfSignatureTransform[] Transforms = [];
    /// <summary>
    /// The digest algorithm IRI.
    /// </summary>
    public string DigestMethod = "";
    /// <summary>
    /// The decoded digest value.
    /// </summary>
    public byte[] DigestValue = [];
    /// <summary>
    /// The digest value base64 lexical text.
    /// </summary>
    public string DigestValueLexical = "";
    /// <summary>
    /// Whether the digest value is valid non-empty base64.
    /// </summary>
    public bool DigestValueIsValid;
    /// <summary>
    /// The preserved Reference subtree.
    /// </summary>
    public string OriginalXml = "";
    /// <summary>
    /// Whether required reference metadata is present and valid.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// The canonical package path resolved during verification, or an empty string.
    /// </summary>
    public string ResolvedPackagePath = "";
    /// <summary>
    /// Whether an encrypted entry was verified from its stored ciphertext bytes.
    /// </summary>
    public bool UsesEncryptedPackageBytes;
    /// <summary>
    /// Whether reference verification reached a digest comparison.
    /// </summary>
    public bool IsVerified;
    /// <summary>
    /// Whether the calculated digest matched the declared digest.
    /// </summary>
    public bool DigestMatches;
    /// <summary>
    /// The reference verification status.
    /// </summary>
    public OdfSignatureStatus Status;
    internal XmlElement? ReferenceElement;
}
