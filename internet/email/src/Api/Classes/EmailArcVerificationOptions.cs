using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Controls how ARC verification operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailArcVerificationOptions : IEmailArcVerificationOptionsContract
{
    /// <summary>
    /// The default maximum signature header bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumSignatureHeaderBytes = 65536;
    /// <summary>
    /// The default maximum signed header count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumSignedHeaderCount = EmailDkimHeaderCanonicalizationOptionsConstants.DefaultMaximumSelectedHeaderCount;
    EmailArcSetCollectionOptions collectionOptions;
    bool verifyPriorMessageSignatures;
    bool allowLegacyRsaSha1;
    bool hasVerificationTime;
    long verificationTime;
    long allowedClockSkewSeconds;
    int maximumSignatureHeaderBytes;
    int maximumSignatureTags;
    int maximumSignedHeaderCount;
    long maximumBodyInputBytes;
    long maximumCanonicalizedBodyBytes;
    long maximumCanonicalizedHeaderBytes;
    /// <summary>
    /// Creates a new ARC verification options object.
    /// </summary>
    public EmailArcVerificationOptions()
    {
        collectionOptions = new EmailArcSetCollectionOptions();
        verifyPriorMessageSignatures = true;
        allowLegacyRsaSha1 = false;
        hasVerificationTime = false;
        verificationTime = 0;
        allowedClockSkewSeconds = 300;
        maximumSignatureHeaderBytes = DefaultMaximumSignatureHeaderBytes;
        maximumSignatureTags = EmailSignatureParserOptionsConstants.DefaultMaximumTags;
        maximumSignedHeaderCount = DefaultMaximumSignedHeaderCount;
        maximumBodyInputBytes = EmailDkimBodyCanonicalizationOptionsConstants.DefaultMaximumInputBytes;
        maximumCanonicalizedBodyBytes = EmailDkimBodyCanonicalizationOptionsConstants.DefaultMaximumCanonicalizedBytes;
        maximumCanonicalizedHeaderBytes = EmailDkimHeaderCanonicalizationOptionsConstants.DefaultMaximumCanonicalizedBytes;
    }

    /// <summary>
    /// Gets the collection options.
    /// </summary>
    /// <returns>The current collection options.</returns>
    public EmailArcSetCollectionOptions GetCollectionOptions()
    {
        return collectionOptions.Clone();
    }

    /// <summary>
    /// Sets the collection options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetCollectionOptions(EmailArcSetCollectionOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        collectionOptions = value.Clone();
    }

    /// <summary>
    /// Gets the verify prior message signatures.
    /// </summary>
    /// <returns><c>true</c> when the verify prior message signatures setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetVerifyPriorMessageSignatures()
    {
        return verifyPriorMessageSignatures;
    }

    /// <summary>
    /// Sets the verify prior message signatures used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the verify prior message signatures behavior.</param>
    public void SetVerifyPriorMessageSignatures(bool value)
    {
        verifyPriorMessageSignatures = value;
    }

    /// <summary>
    /// Gets the allow legacy RSA sha 1.
    /// </summary>
    /// <returns><c>true</c> when the allow legacy RSA sha 1 setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowLegacyRsaSha1()
    {
        return allowLegacyRsaSha1;
    }

    /// <summary>
    /// Sets the allow legacy RSA sha 1 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow legacy RSA sha 1 behavior.</param>
    public void SetAllowLegacyRsaSha1(bool value)
    {
        allowLegacyRsaSha1 = value;
    }

    /// <summary>
    /// Determines whether the verification time is present or available.
    /// </summary>
    /// <returns><c>true</c> when the verification time is available; otherwise, <c>false</c>.</returns>
    public bool HasVerificationTime()
    {
        return hasVerificationTime;
    }

    /// <summary>
    /// Gets the verification time.
    /// </summary>
    /// <returns>The current verification time.</returns>
    public long GetVerificationTime()
    {
        return verificationTime;
    }

    /// <summary>
    /// Sets the verification time used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetVerificationTime(long value)
    {
        verificationTime = EmailDkimSigningValidation.RequireTime(value, "value");
        hasVerificationTime = true;
    }

    /// <summary>
    /// Clears the stored verification time value or values.
    /// </summary>
    public void ClearVerificationTime()
    {
        hasVerificationTime = false;
        verificationTime = 0;
    }

    /// <summary>
    /// Gets the allowed clock skew seconds.
    /// </summary>
    /// <returns>The current allowed clock skew seconds.</returns>
    public long GetAllowedClockSkewSeconds()
    {
        return allowedClockSkewSeconds;
    }

    /// <summary>
    /// Sets the allowed clock skew seconds used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetAllowedClockSkewSeconds(long value)
    {
        allowedClockSkewSeconds = EmailLimitValidation.RequireNonNegative(value, "value");
    }

    /// <summary>
    /// Gets the maximum signature header bytes.
    /// </summary>
    /// <returns>The current maximum signature header bytes.</returns>
    public int GetMaximumSignatureHeaderBytes()
    {
        return maximumSignatureHeaderBytes;
    }

    /// <summary>
    /// Sets the maximum signature header bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumSignatureHeaderBytes(int value)
    {
        maximumSignatureHeaderBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum signature tags.
    /// </summary>
    /// <returns>The current maximum signature tags.</returns>
    public int GetMaximumSignatureTags()
    {
        return maximumSignatureTags;
    }

    /// <summary>
    /// Sets the maximum signature tags used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumSignatureTags(int value)
    {
        maximumSignatureTags = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum signed header count.
    /// </summary>
    /// <returns>The current maximum signed header count.</returns>
    public int GetMaximumSignedHeaderCount()
    {
        return maximumSignedHeaderCount;
    }

    /// <summary>
    /// Sets the maximum signed header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumSignedHeaderCount(int value)
    {
        maximumSignedHeaderCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum body input bytes.
    /// </summary>
    /// <returns>The current maximum body input bytes.</returns>
    public long GetMaximumBodyInputBytes()
    {
        return maximumBodyInputBytes;
    }

    /// <summary>
    /// Sets the maximum body input bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumBodyInputBytes(long value)
    {
        maximumBodyInputBytes = EmailLimitValidation.RequireNonNegative(value, "value");
    }

    /// <summary>
    /// Gets the maximum canonicalized body bytes.
    /// </summary>
    /// <returns>The current maximum canonicalized body bytes.</returns>
    public long GetMaximumCanonicalizedBodyBytes()
    {
        return maximumCanonicalizedBodyBytes;
    }

    /// <summary>
    /// Sets the maximum canonicalized body bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumCanonicalizedBodyBytes(long value)
    {
        maximumCanonicalizedBodyBytes = EmailLimitValidation.RequireNonNegative(value, "value");
    }

    /// <summary>
    /// Gets the maximum canonicalized header bytes.
    /// </summary>
    /// <returns>The current maximum canonicalized header bytes.</returns>
    public long GetMaximumCanonicalizedHeaderBytes()
    {
        return maximumCanonicalizedHeaderBytes;
    }

    /// <summary>
    /// Sets the maximum canonicalized header bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumCanonicalizedHeaderBytes(long value)
    {
        maximumCanonicalizedHeaderBytes = EmailLimitValidation.RequireNonNegative(value, "value");
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailArcVerificationOptions Clone()
    {
        EmailArcVerificationOptions copy = new EmailArcVerificationOptions();
        copy.SetCollectionOptions(collectionOptions);
        copy.SetVerifyPriorMessageSignatures(verifyPriorMessageSignatures);
        copy.SetAllowLegacyRsaSha1(allowLegacyRsaSha1);
        if (hasVerificationTime)
            copy.SetVerificationTime(verificationTime);
        copy.SetAllowedClockSkewSeconds(allowedClockSkewSeconds);
        copy.SetMaximumSignatureHeaderBytes(maximumSignatureHeaderBytes);
        copy.SetMaximumSignatureTags(maximumSignatureTags);
        copy.SetMaximumSignedHeaderCount(maximumSignedHeaderCount);
        copy.SetMaximumBodyInputBytes(maximumBodyInputBytes);
        copy.SetMaximumCanonicalizedBodyBytes(maximumCanonicalizedBodyBytes);
        copy.SetMaximumCanonicalizedHeaderBytes(maximumCanonicalizedHeaderBytes);
        return copy;
    }
}
