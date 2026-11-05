using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimSigningOptions"/>.
/// </summary>
public interface IEmailDkimSigningOptionsContract
{
    /// <summary>
    /// Gets the domain.
    /// </summary>
    /// <returns>The current domain.</returns>
    string GetDomain();
    /// <summary>
    /// Sets the domain used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetDomain(string value);
    /// <summary>
    /// Gets the selector.
    /// </summary>
    /// <returns>The current selector.</returns>
    string GetSelector();
    /// <summary>
    /// Sets the selector used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetSelector(string value);
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
    /// Adds a signed header to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddSignedHeader(string value);
    /// <summary>
    /// Clears the stored signed headers value or values.
    /// </summary>
    void ClearSignedHeaders();
    /// <summary>
    /// Gets the header canonicalization kind.
    /// </summary>
    /// <returns>The current header canonicalization kind.</returns>
    EmailDkimCanonicalizationKind GetHeaderCanonicalizationKind();
    /// <summary>
    /// Sets the header canonicalization kind used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetHeaderCanonicalizationKind(EmailDkimCanonicalizationKind value);
    /// <summary>
    /// Gets the body canonicalization kind.
    /// </summary>
    /// <returns>The current body canonicalization kind.</returns>
    EmailDkimCanonicalizationKind GetBodyCanonicalizationKind();
    /// <summary>
    /// Sets the body canonicalization kind used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetBodyCanonicalizationKind(EmailDkimCanonicalizationKind value);
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
    /// Sets the timestamp used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetTimestamp(long value);
    /// <summary>
    /// Clears the stored timestamp value or values.
    /// </summary>
    void ClearTimestamp();
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
    /// Sets the expiration used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetExpiration(long value);
    /// <summary>
    /// Clears the stored expiration value or values.
    /// </summary>
    void ClearExpiration();
    /// <summary>
    /// Determines whether the body length limit is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body length limit is available; otherwise, <c>false</c>.</returns>
    bool HasBodyLengthLimit();
    /// <summary>
    /// Gets the body length limit.
    /// </summary>
    /// <returns>The current body length limit.</returns>
    long GetBodyLengthLimit();
    /// <summary>
    /// Sets the body length limit used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetBodyLengthLimit(long value);
    /// <summary>
    /// Clears the stored body length limit value or values.
    /// </summary>
    void ClearBodyLengthLimit();
    /// <summary>
    /// Gets the maximum body input bytes.
    /// </summary>
    /// <returns>The current maximum body input bytes.</returns>
    long GetMaximumBodyInputBytes();
    /// <summary>
    /// Sets the maximum body input bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumBodyInputBytes(long value);
    /// <summary>
    /// Gets the maximum canonicalized body bytes.
    /// </summary>
    /// <returns>The current maximum canonicalized body bytes.</returns>
    long GetMaximumCanonicalizedBodyBytes();
    /// <summary>
    /// Sets the maximum canonicalized body bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumCanonicalizedBodyBytes(long value);
    /// <summary>
    /// Gets the maximum canonicalized header bytes.
    /// </summary>
    /// <returns>The current maximum canonicalized header bytes.</returns>
    long GetMaximumCanonicalizedHeaderBytes();
    /// <summary>
    /// Sets the maximum canonicalized header bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumCanonicalizedHeaderBytes(long value);
    /// <summary>
    /// Gets the maximum signature header bytes.
    /// </summary>
    /// <returns>The current maximum signature header bytes.</returns>
    int GetMaximumSignatureHeaderBytes();
    /// <summary>
    /// Sets the maximum signature header bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumSignatureHeaderBytes(int value);
    /// <summary>
    /// Gets the maximum signed header count.
    /// </summary>
    /// <returns>The current maximum signed header count.</returns>
    int GetMaximumSignedHeaderCount();
    /// <summary>
    /// Sets the maximum signed header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumSignedHeaderCount(int value);
    /// <summary>
    /// Gets the minimum RSA key bits.
    /// </summary>
    /// <returns>The current minimum RSA key bits.</returns>
    int GetMinimumRsaKeyBits();
    /// <summary>
    /// Sets the minimum RSA key bits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMinimumRsaKeyBits(int value);
    /// <summary>
    /// Gets the maximum RSA key bits.
    /// </summary>
    /// <returns>The current maximum RSA key bits.</returns>
    int GetMaximumRsaKeyBits();
    /// <summary>
    /// Sets the maximum RSA key bits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumRsaKeyBits(int value);
    /// <summary>
    /// Gets the preferred line length.
    /// </summary>
    /// <returns>The current preferred line length.</returns>
    int GetPreferredLineLength();
    /// <summary>
    /// Sets the preferred line length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetPreferredLineLength(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailDkimSigningOptions Clone();
}
