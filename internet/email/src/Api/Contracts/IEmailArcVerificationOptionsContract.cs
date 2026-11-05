using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcVerificationOptions"/>.
/// </summary>
public interface IEmailArcVerificationOptionsContract
{
    /// <summary>
    /// Gets the collection options.
    /// </summary>
    /// <returns>The current collection options.</returns>
    EmailArcSetCollectionOptions GetCollectionOptions();
    /// <summary>
    /// Sets the collection options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetCollectionOptions(EmailArcSetCollectionOptions value);
    /// <summary>
    /// Gets the verify prior message signatures.
    /// </summary>
    /// <returns><c>true</c> when the verify prior message signatures setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetVerifyPriorMessageSignatures();
    /// <summary>
    /// Sets the verify prior message signatures used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the verify prior message signatures behavior.</param>
    void SetVerifyPriorMessageSignatures(bool value);
    /// <summary>
    /// Gets the allow legacy RSA sha 1.
    /// </summary>
    /// <returns><c>true</c> when the allow legacy RSA sha 1 setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowLegacyRsaSha1();
    /// <summary>
    /// Sets the allow legacy RSA sha 1 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow legacy RSA sha 1 behavior.</param>
    void SetAllowLegacyRsaSha1(bool value);
    /// <summary>
    /// Determines whether the verification time is present or available.
    /// </summary>
    /// <returns><c>true</c> when the verification time is available; otherwise, <c>false</c>.</returns>
    bool HasVerificationTime();
    /// <summary>
    /// Gets the verification time.
    /// </summary>
    /// <returns>The current verification time.</returns>
    long GetVerificationTime();
    /// <summary>
    /// Sets the verification time used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetVerificationTime(long value);
    /// <summary>
    /// Clears the stored verification time value or values.
    /// </summary>
    void ClearVerificationTime();
    /// <summary>
    /// Gets the allowed clock skew seconds.
    /// </summary>
    /// <returns>The current allowed clock skew seconds.</returns>
    long GetAllowedClockSkewSeconds();
    /// <summary>
    /// Sets the allowed clock skew seconds used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetAllowedClockSkewSeconds(long value);
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
    /// Gets the maximum signature tags.
    /// </summary>
    /// <returns>The current maximum signature tags.</returns>
    int GetMaximumSignatureTags();
    /// <summary>
    /// Sets the maximum signature tags used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumSignatureTags(int value);
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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailArcVerificationOptions Clone();
}
