using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimSignature"/>.
/// </summary>
public interface IEmailDkimSignatureContract
{
    /// <summary>
    /// Gets the header kind.
    /// </summary>
    /// <returns>The current header kind.</returns>
    EmailSignatureHeaderKind GetHeaderKind();
    /// <summary>
    /// Determines whether the current value or state is ARC message signature.
    /// </summary>
    /// <returns><c>true</c> when the value or state is ARC message signature; otherwise, <c>false</c>.</returns>
    bool IsArcMessageSignature();
    /// <summary>
    /// Gets the tags.
    /// </summary>
    /// <returns>The current tags.</returns>
    EmailSignatureTagList GetTags();
    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    int GetVersion();
    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    int GetInstance();
    /// <summary>
    /// Gets the algorithm.
    /// </summary>
    /// <returns>The current algorithm.</returns>
    string GetAlgorithm();
    /// <summary>
    /// Gets the algorithm kind.
    /// </summary>
    /// <returns>The current algorithm kind.</returns>
    EmailDkimAlgorithmKind GetAlgorithmKind();
    /// <summary>
    /// Gets the signature data.
    /// </summary>
    /// <returns>The current signature data.</returns>
    string GetSignatureData();
    /// <summary>
    /// Gets the body hash.
    /// </summary>
    /// <returns>The current body hash.</returns>
    string GetBodyHash();
    /// <summary>
    /// Gets the signing domain.
    /// </summary>
    /// <returns>The current signing domain.</returns>
    string GetSigningDomain();
    /// <summary>
    /// Gets the selector.
    /// </summary>
    /// <returns>The current selector.</returns>
    string GetSelector();
    /// <summary>
    /// Gets the identity.
    /// </summary>
    /// <returns>The current identity.</returns>
    string GetIdentity();
    /// <summary>
    /// Determines whether the body length is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body length is available; otherwise, <c>false</c>.</returns>
    bool HasBodyLength();
    /// <summary>
    /// Gets the body length.
    /// </summary>
    /// <returns>The current body length.</returns>
    long GetBodyLength();
    /// <summary>
    /// Determines whether the timestamp is present or available.
    /// </summary>
    /// <returns><c>true</c> when the timestamp is available; otherwise, <c>false</c>.</returns>
    bool HasTimestamp();
    /// <summary>
    /// Gets the timestamp.
    /// </summary>
    /// <returns>The current timestamp.</returns>
    long GetTimestamp();
    /// <summary>
    /// Determines whether the expiration is present or available.
    /// </summary>
    /// <returns><c>true</c> when the expiration is available; otherwise, <c>false</c>.</returns>
    bool HasExpiration();
    /// <summary>
    /// Gets the expiration.
    /// </summary>
    /// <returns>The current expiration.</returns>
    long GetExpiration();
    /// <summary>
    /// Gets the header canonicalization kind.
    /// </summary>
    /// <returns>The current header canonicalization kind.</returns>
    EmailDkimCanonicalizationKind GetHeaderCanonicalizationKind();
    /// <summary>
    /// Gets the body canonicalization kind.
    /// </summary>
    /// <returns>The current body canonicalization kind.</returns>
    EmailDkimCanonicalizationKind GetBodyCanonicalizationKind();
    /// <summary>
    /// Gets the signed header count.
    /// </summary>
    /// <returns>The current signed header count.</returns>
    int GetSignedHeaderCount();
    /// <summary>
    /// Gets the signed header.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current signed header.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    string GetSignedHeader(int index);
    /// <summary>
    /// Gets the signed headers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current signed headers.</returns>
    List<string> GetSignedHeaders();
    /// <summary>
    /// Gets the query methods.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current query methods.</returns>
    List<string> GetQueryMethods();
    /// <summary>
    /// Gets the copied headers.
    /// </summary>
    /// <returns>The current copied headers.</returns>
    string GetCopiedHeaders();
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition GetSourcePosition();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <returns>The signature format result produced by the operation.</returns>
    EmailSignatureFormatResult Format();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    EmailSignatureFormatResult FormatWithOptions(EmailSignatureFormatOptions options);
}
