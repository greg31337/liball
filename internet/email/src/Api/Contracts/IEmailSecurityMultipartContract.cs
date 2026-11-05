using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSecurityMultipart"/>.
/// </summary>
public interface IEmailSecurityMultipartContract
{
    /// <summary>
    /// Gets the root.
    /// </summary>
    /// <returns>The current root.</returns>
    MimeEntity GetRoot();
    /// <summary>
    /// Determines whether the current value or state is signed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is signed; otherwise, <c>false</c>.</returns>
    bool IsSigned();
    /// <summary>
    /// Determines whether the current value or state is encrypted.
    /// </summary>
    /// <returns><c>true</c> when the value or state is encrypted; otherwise, <c>false</c>.</returns>
    bool IsEncrypted();
    /// <summary>
    /// Determines whether the protocol is present or available.
    /// </summary>
    /// <returns><c>true</c> when the protocol is available; otherwise, <c>false</c>.</returns>
    bool HasProtocol();
    /// <summary>
    /// Gets the protocol.
    /// </summary>
    /// <returns>The current protocol.</returns>
    string? GetProtocol();
    /// <summary>
    /// Gets the mic algorithm count.
    /// </summary>
    /// <returns>The current mic algorithm count.</returns>
    int GetMicAlgorithmCount();
    /// <summary>
    /// Gets the mic algorithm at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested mic algorithm.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    string GetMicAlgorithmAt(int index);
    /// <summary>
    /// Gets the mic algorithms.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current mic algorithms.</returns>
    List<string> GetMicAlgorithms();
    /// <summary>
    /// Determines whether the content part is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content part is available; otherwise, <c>false</c>.</returns>
    bool HasContentPart();
    /// <summary>
    /// Gets the content part.
    /// </summary>
    /// <returns>The current content part.</returns>
    MimeEntity? GetContentPart();
    /// <summary>
    /// Determines whether the security part is present or available.
    /// </summary>
    /// <returns><c>true</c> when the security part is available; otherwise, <c>false</c>.</returns>
    bool HasSecurityPart();
    /// <summary>
    /// Gets the security part.
    /// </summary>
    /// <returns>The current security part.</returns>
    MimeEntity? GetSecurityPart();
    /// <summary>
    /// Gets the signed content part.
    /// </summary>
    /// <returns>The current signed content part.</returns>
    MimeEntity? GetSignedContentPart();
    /// <summary>
    /// Gets the signature part.
    /// </summary>
    /// <returns>The current signature part.</returns>
    MimeEntity? GetSignaturePart();
    /// <summary>
    /// Gets the encryption control part.
    /// </summary>
    /// <returns>The current encryption control part.</returns>
    MimeEntity? GetEncryptionControlPart();
    /// <summary>
    /// Gets the encrypted content part.
    /// </summary>
    /// <returns>The current encrypted content part.</returns>
    MimeEntity? GetEncryptedContentPart();
    /// <summary>
    /// Determines whether the exact signed octets is present or available.
    /// </summary>
    /// <returns><c>true</c> when the exact signed octets is available; otherwise, <c>false</c>.</returns>
    bool HasExactSignedOctets();
    /// <summary>
    /// Gets the exact signed octets.
    /// </summary>
    /// <returns>The current exact signed octets.</returns>
    EmailContent? GetExactSignedOctets();
    /// <summary>
    /// Materializes the exact signed octets.
    /// </summary>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    byte[] MaterializeExactSignedOctets(long maximumBytes);
    /// <summary>
    /// Determines whether the compared values are invariants satisfied.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool AreInvariantsSatisfied();
}
