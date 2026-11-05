using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Configures creation of a detached OPC package signature.
/// </summary>
public class XlsxSignatureAuthoringOptions
{
    /// <summary>
    /// Stores the certificate containing the private key used to sign the package.
    /// </summary>
    public X509Certificate2? Certificate;
    /// <summary>
    /// Stores an optional RSA private key when the certificate does not directly carry one.
    /// </summary>
    public RSA? PrivateKey;
    /// <summary>
    /// Stores the supported RSA and SHA-2 signature algorithm.
    /// </summary>
    public XlsxSignatureAlgorithm Algorithm;
    /// <summary>
    /// Stores the XML signature identifier.
    /// </summary>
    public string SignatureId;
    /// <summary>
    /// Stores the normalized signature part path.
    /// </summary>
    public string SignaturePartPath;
    /// <summary>
    /// Contains package part paths to sign. An empty collection selects all non-relationship content parts.
    /// </summary>
    public List<string> PartPaths;
    /// <summary>
    /// Initializes new signature authoring options with safe defaults.
    /// </summary>
    public XlsxSignatureAuthoringOptions()
    {
        Certificate = null;
        PrivateKey = null;
        Algorithm = XlsxSignatureAlgorithm.RsaSha256;
        SignatureId = "idPackageSignature";
        SignaturePartPath = "/_xmlsignatures/sig1.xml";
        PartPaths = new List<string>();
    }
}
