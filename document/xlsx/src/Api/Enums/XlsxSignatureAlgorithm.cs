using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Identifies a supported package signature algorithm.
/// </summary>
public enum XlsxSignatureAlgorithm
{
    /// <summary>
    /// Indicates that no supported signature algorithm was selected.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Indicates RSA PKCS number one version one point five with SHA-256.
    /// </summary>
    RsaSha256 = 1,
    /// <summary>
    /// Indicates RSA PKCS number one version one point five with SHA-384.
    /// </summary>
    RsaSha384 = 2,
    /// <summary>
    /// Indicates RSA PKCS number one version one point five with SHA-512.
    /// </summary>
    RsaSha512 = 3
}
