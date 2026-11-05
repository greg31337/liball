using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a security multipart.
/// </summary>
public class EmailSecurityMultipart : IEmailSecurityMultipartContract
{
    MimeEntity root;
    string? protocol;
    List<string> micAlgorithms;
    MimeEntity? contentPart;
    MimeEntity? securityPart;
    EmailContent? exactSignedOctets;
    bool invariantsSatisfied;
    /// <summary>
    /// Creates a new security multipart.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "protocol">The protocol used by the operation.</param>
    /// <param name = "micAlgorithms">The mic algorithms used by the operation.</param>
    /// <param name = "contentPart">The content part used by the operation.</param>
    /// <param name = "securityPart">The security part used by the operation.</param>
    /// <param name = "exactSignedOctets">The exact signed octets used by the operation.</param>
    /// <param name = "invariantsSatisfied">Whether to enable the invariants satisfied behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSecurityMultipart(MimeEntity root, string? protocol, List<string> micAlgorithms, MimeEntity? contentPart, MimeEntity? securityPart, EmailContent? exactSignedOctets, bool invariantsSatisfied)
    {
        if (root == null)
            throw new ArgumentNullException("root");
        if (root.GetKind() != MimeEntityKind.Signed && root.GetKind() != MimeEntityKind.Encrypted)
            throw new ArgumentException("A security multipart root must be signed or encrypted.", "root");
        if (protocol != null && protocol.Length == 0)
            throw new ArgumentException("A security protocol cannot be empty when supplied.", "protocol");
        if (micAlgorithms == null)
            throw new ArgumentNullException("micAlgorithms");
        int index = 0;
        while (index < micAlgorithms.Count)
        {
            if (micAlgorithms[index] == null || micAlgorithms[index].Length == 0)
                throw new ArgumentException("MIC algorithm names cannot be null or empty.", "micAlgorithms");
            index++;
        }

        if (exactSignedOctets != null && root.GetKind() != MimeEntityKind.Signed)
            throw new ArgumentException("Exact signed octets require a multipart/signed root.", "exactSignedOctets");
        this.root = root;
        this.protocol = protocol;
        this.micAlgorithms = new List<string>(micAlgorithms);
        this.contentPart = contentPart;
        this.securityPart = securityPart;
        this.exactSignedOctets = exactSignedOctets;
        this.invariantsSatisfied = invariantsSatisfied;
    }

    /// <summary>
    /// Gets the root.
    /// </summary>
    /// <returns>The current root.</returns>
    public MimeEntity GetRoot()
    {
        return root;
    }

    /// <summary>
    /// Determines whether the current value or state is signed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is signed; otherwise, <c>false</c>.</returns>
    public bool IsSigned()
    {
        return root.GetKind() == MimeEntityKind.Signed;
    }

    /// <summary>
    /// Determines whether the current value or state is encrypted.
    /// </summary>
    /// <returns><c>true</c> when the value or state is encrypted; otherwise, <c>false</c>.</returns>
    public bool IsEncrypted()
    {
        return root.GetKind() == MimeEntityKind.Encrypted;
    }

    /// <summary>
    /// Determines whether the protocol is present or available.
    /// </summary>
    /// <returns><c>true</c> when the protocol is available; otherwise, <c>false</c>.</returns>
    public bool HasProtocol()
    {
        return protocol != null;
    }

    /// <summary>
    /// Gets the protocol.
    /// </summary>
    /// <returns>The current protocol.</returns>
    public string? GetProtocol()
    {
        return protocol;
    }

    /// <summary>
    /// Gets the mic algorithm count.
    /// </summary>
    /// <returns>The current mic algorithm count.</returns>
    public int GetMicAlgorithmCount()
    {
        return micAlgorithms.Count;
    }

    /// <summary>
    /// Gets the mic algorithm at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested mic algorithm.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public string GetMicAlgorithmAt(int index)
    {
        if (index < 0 || index >= micAlgorithms.Count)
            throw new ArgumentOutOfRangeException("index");
        return micAlgorithms[index];
    }

    /// <summary>
    /// Gets the mic algorithms.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current mic algorithms.</returns>
    public List<string> GetMicAlgorithms()
    {
        return new List<string>(micAlgorithms);
    }

    /// <summary>
    /// Determines whether the content part is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content part is available; otherwise, <c>false</c>.</returns>
    public bool HasContentPart()
    {
        return contentPart != null;
    }

    /// <summary>
    /// Gets the content part.
    /// </summary>
    /// <returns>The current content part.</returns>
    public MimeEntity? GetContentPart()
    {
        return contentPart;
    }

    /// <summary>
    /// Determines whether the security part is present or available.
    /// </summary>
    /// <returns><c>true</c> when the security part is available; otherwise, <c>false</c>.</returns>
    public bool HasSecurityPart()
    {
        return securityPart != null;
    }

    /// <summary>
    /// Gets the security part.
    /// </summary>
    /// <returns>The current security part.</returns>
    public MimeEntity? GetSecurityPart()
    {
        return securityPart;
    }

    /// <summary>
    /// Gets the signed content part.
    /// </summary>
    /// <returns>The current signed content part.</returns>
    public MimeEntity? GetSignedContentPart()
    {
        return IsSigned() ? contentPart : null;
    }

    /// <summary>
    /// Gets the signature part.
    /// </summary>
    /// <returns>The current signature part.</returns>
    public MimeEntity? GetSignaturePart()
    {
        return IsSigned() ? securityPart : null;
    }

    /// <summary>
    /// Gets the encryption control part.
    /// </summary>
    /// <returns>The current encryption control part.</returns>
    public MimeEntity? GetEncryptionControlPart()
    {
        return IsEncrypted() ? securityPart : null;
    }

    /// <summary>
    /// Gets the encrypted content part.
    /// </summary>
    /// <returns>The current encrypted content part.</returns>
    public MimeEntity? GetEncryptedContentPart()
    {
        return IsEncrypted() ? contentPart : null;
    }

    /// <summary>
    /// Determines whether the exact signed octets is present or available.
    /// </summary>
    /// <returns><c>true</c> when the exact signed octets is available; otherwise, <c>false</c>.</returns>
    public bool HasExactSignedOctets()
    {
        return exactSignedOctets != null;
    }

    /// <summary>
    /// Gets the exact signed octets.
    /// </summary>
    /// <returns>The current exact signed octets.</returns>
    public EmailContent? GetExactSignedOctets()
    {
        return exactSignedOctets;
    }

    /// <summary>
    /// Materializes the exact signed octets.
    /// </summary>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public byte[] MaterializeExactSignedOctets(long maximumBytes)
    {
        if (exactSignedOctets == null)
            throw new InvalidOperationException("Exact signed octets are not available.");
        return exactSignedOctets.Materialize(maximumBytes);
    }

    /// <summary>
    /// Determines whether the compared values are invariants satisfied.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool AreInvariantsSatisfied()
    {
        return invariantsSatisfied;
    }
}
