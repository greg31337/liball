using System;
using System.Text;

namespace token;

/// <summary>Represents one signature entry in a general JWS JSON serialization.</summary>
public class JwsGeneralSignature
{
    /// <summary>Contains the integrity-protected JOSE header.</summary>
    public JoseHeader? ProtectedHeader;

    /// <summary>Contains the unprotected JOSE header, or <c>null</c> when absent.</summary>
    public JoseHeader? UnprotectedHeader;

    /// <summary>Contains the protected JWS algorithm identifier.</summary>
    public string? Algorithm;

    /// <summary>Contains registry metadata for <see cref="Algorithm"/>.</summary>
    public AlgorithmMetadata? AlgorithmMetadata;

    /// <summary>Indicates whether the algorithm has a usable built-in implementation.</summary>
    public bool AlgorithmAvailable;

    /// <summary>Indicates whether this entry uses the unsecured <c>none</c> algorithm.</summary>
    public bool Unsecured;

    /// <summary>Contains the key identifier from either header, or <c>null</c> when absent.</summary>
    public string? KeyId;

    /// <summary>Contains the decoded signature or MAC bytes.</summary>
    public byte[]? Signature;

    /// <summary>Indicates whether this signature has been verified.</summary>
    public bool SignatureVerified;

    internal string? FlattenedJson;

    /// <summary>Initializes an empty, unverified signature entry.</summary>
    public JwsGeneralSignature()
    {
        ProtectedHeader = null;
        UnprotectedHeader = null;
        Algorithm = null;
        AlgorithmMetadata = null;
        AlgorithmAvailable = false;
        Unsecured = false;
        KeyId = null;
        Signature = null;
        SignatureVerified = false;
        FlattenedJson = null;
    }
}
