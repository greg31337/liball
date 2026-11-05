using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Describes one package part reference contained in a digital signature manifest.
/// </summary>
public class XlsxSignatureReference
{
    /// <summary>
    /// Stores the reference URI exactly as represented in signature XML.
    /// </summary>
    public string Uri;
    /// <summary>
    /// Stores the normalized package part path referenced by the signature.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores the content type encoded in the reference URI.
    /// </summary>
    public string? ContentType;
    /// <summary>
    /// Stores the digest method URI.
    /// </summary>
    public string DigestMethod;
    /// <summary>
    /// Stores the transform method URI when a transform is present.
    /// </summary>
    public string TransformMethod;
    /// <summary>
    /// Contains the digest bytes represented by the signature.
    /// </summary>
    public byte[] DigestValue;
    /// <summary>
    /// Indicates whether the referenced package part exists.
    /// </summary>
    public bool PartExists;
    /// <summary>
    /// Indicates whether the reference uses only supported transforms and digest methods.
    /// </summary>
    public bool IsSupported;
    /// <summary>
    /// Indicates whether the recomputed digest matches the stored digest.
    /// </summary>
    public bool DigestValid;
    /// <summary>
    /// Initializes a new signature reference with its default state.
    /// </summary>
    public XlsxSignatureReference()
    {
        Uri = "";
        PartPath = "";
        ContentType = "";
        DigestMethod = "";
        TransformMethod = "";
        DigestValue = new byte[0];
        PartExists = false;
        IsSupported = true;
        DigestValid = false;
    }
}
