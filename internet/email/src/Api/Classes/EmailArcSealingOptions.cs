using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Controls how ARC sealing operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailArcSealingOptions : IEmailArcSealingOptionsContract
{
    /// <summary>
    /// The default minimum RSA key bits used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMinimumRsaKeyBits = EmailDkimSigningOptionsConstants.DefaultMinimumRsaKeyBits;
    /// <summary>
    /// The default maximum RSA key bits used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumRsaKeyBits = EmailDkimSigningOptionsConstants.DefaultMaximumRsaKeyBits;
    /// <summary>
    /// The default maximum signature header bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumSignatureHeaderBytes = EmailDkimSigningOptionsConstants.DefaultMaximumSignatureHeaderBytes;
    /// <summary>
    /// The default maximum signed header count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumSignedHeaderCount = EmailDkimSigningOptionsConstants.DefaultMaximumSignedHeaderCount;
    string messageSignatureDomain;
    string messageSignatureSelector;
    string sealDomain;
    string sealSelector;
    List<string> signedHeaderNames;
    bool includeExistingDkimSignatures;
    EmailDkimCanonicalizationKind headerCanonicalizationKind;
    EmailDkimCanonicalizationKind bodyCanonicalizationKind;
    bool hasTimestamp;
    long timestamp;
    bool hasBodyLengthLimit;
    long bodyLengthLimit;
    long maximumBodyInputBytes;
    long maximumCanonicalizedBodyBytes;
    long maximumCanonicalizedHeaderBytes;
    int maximumSignatureHeaderBytes;
    int maximumSignedHeaderCount;
    int minimumRsaKeyBits;
    int maximumRsaKeyBits;
    int preferredLineLength;
    EmailArcSetCollectionOptions collectionOptions;
    /// <summary>
    /// Creates a new ARC sealing options object.
    /// </summary>
    public EmailArcSealingOptions()
    {
        messageSignatureDomain = "";
        messageSignatureSelector = "";
        sealDomain = "";
        sealSelector = "";
        signedHeaderNames = new List<string>();
        signedHeaderNames.Add("From");
        includeExistingDkimSignatures = true;
        headerCanonicalizationKind = EmailDkimCanonicalizationKind.Relaxed;
        bodyCanonicalizationKind = EmailDkimCanonicalizationKind.Relaxed;
        hasTimestamp = false;
        timestamp = 0;
        hasBodyLengthLimit = false;
        bodyLengthLimit = 0;
        maximumBodyInputBytes = EmailDkimBodyCanonicalizationOptionsConstants.DefaultMaximumInputBytes;
        maximumCanonicalizedBodyBytes = EmailDkimBodyCanonicalizationOptionsConstants.DefaultMaximumCanonicalizedBytes;
        maximumCanonicalizedHeaderBytes = EmailDkimHeaderCanonicalizationOptionsConstants.DefaultMaximumCanonicalizedBytes;
        maximumSignatureHeaderBytes = DefaultMaximumSignatureHeaderBytes;
        maximumSignedHeaderCount = DefaultMaximumSignedHeaderCount;
        minimumRsaKeyBits = DefaultMinimumRsaKeyBits;
        maximumRsaKeyBits = DefaultMaximumRsaKeyBits;
        preferredLineLength = 78;
        collectionOptions = new EmailArcSetCollectionOptions();
    }

    /// <summary>
    /// Gets the message signature domain.
    /// </summary>
    /// <returns>The current message signature domain.</returns>
    public string GetMessageSignatureDomain()
    {
        return messageSignatureDomain;
    }

    /// <summary>
    /// Sets the message signature domain used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMessageSignatureDomain(string value)
    {
        messageSignatureDomain = EmailDkimSigningValidation.RequireDomain(value, "value");
    }

    /// <summary>
    /// Gets the message signature selector.
    /// </summary>
    /// <returns>The current message signature selector.</returns>
    public string GetMessageSignatureSelector()
    {
        return messageSignatureSelector;
    }

    /// <summary>
    /// Sets the message signature selector used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMessageSignatureSelector(string value)
    {
        messageSignatureSelector = EmailDkimSigningValidation.RequireDomain(value, "value");
    }

    /// <summary>
    /// Gets the seal domain.
    /// </summary>
    /// <returns>The current seal domain.</returns>
    public string GetSealDomain()
    {
        return sealDomain;
    }

    /// <summary>
    /// Sets the seal domain used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetSealDomain(string value)
    {
        sealDomain = EmailDkimSigningValidation.RequireDomain(value, "value");
    }

    /// <summary>
    /// Gets the seal selector.
    /// </summary>
    /// <returns>The current seal selector.</returns>
    public string GetSealSelector()
    {
        return sealSelector;
    }

    /// <summary>
    /// Sets the seal selector used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetSealSelector(string value)
    {
        sealSelector = EmailDkimSigningValidation.RequireDomain(value, "value");
    }

    /// <summary>
    /// Gets the signed header count.
    /// </summary>
    /// <returns>The current signed header count.</returns>
    public int GetSignedHeaderCount()
    {
        return signedHeaderNames.Count;
    }

    /// <summary>
    /// Gets the signed header.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current signed header.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public string GetSignedHeader(int index)
    {
        if (index < 0 || index >= signedHeaderNames.Count)
            throw new ArgumentOutOfRangeException("index");
        return signedHeaderNames[index];
    }

    /// <summary>
    /// Gets the signed headers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current signed headers.</returns>
    public List<string> GetSignedHeaders()
    {
        return new List<string>(signedHeaderNames);
    }

    /// <summary>
    /// Adds a signed header to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void AddSignedHeader(string value)
    {
        signedHeaderNames.Add(EmailDkimSigningValidation.RequireHeaderName(value, "value"));
    }

    /// <summary>
    /// Clears the stored signed headers value or values.
    /// </summary>
    public void ClearSignedHeaders()
    {
        signedHeaderNames.Clear();
    }

    /// <summary>
    /// Gets the include existing DKIM signatures.
    /// </summary>
    /// <returns><c>true</c> when the include existing DKIM signatures setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeExistingDkimSignatures()
    {
        return includeExistingDkimSignatures;
    }

    /// <summary>
    /// Sets the include existing DKIM signatures used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include existing DKIM signatures behavior.</param>
    public void SetIncludeExistingDkimSignatures(bool value)
    {
        includeExistingDkimSignatures = value;
    }

    /// <summary>
    /// Gets the header canonicalization kind.
    /// </summary>
    /// <returns>The current header canonicalization kind.</returns>
    public EmailDkimCanonicalizationKind GetHeaderCanonicalizationKind()
    {
        return headerCanonicalizationKind;
    }

    /// <summary>
    /// Sets the header canonicalization kind used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetHeaderCanonicalizationKind(EmailDkimCanonicalizationKind value)
    {
        EmailDkimSigningValidation.RequireCanonicalization(value, "value");
        headerCanonicalizationKind = value;
    }

    /// <summary>
    /// Gets the body canonicalization kind.
    /// </summary>
    /// <returns>The current body canonicalization kind.</returns>
    public EmailDkimCanonicalizationKind GetBodyCanonicalizationKind()
    {
        return bodyCanonicalizationKind;
    }

    /// <summary>
    /// Sets the body canonicalization kind used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetBodyCanonicalizationKind(EmailDkimCanonicalizationKind value)
    {
        EmailDkimSigningValidation.RequireCanonicalization(value, "value");
        bodyCanonicalizationKind = value;
    }

    /// <summary>
    /// Determines whether the timestamp is present or available.
    /// </summary>
    /// <returns><c>true</c> when the timestamp is available; otherwise, <c>false</c>.</returns>
    public bool HasTimestamp()
    {
        return hasTimestamp;
    }

    /// <summary>
    /// Gets the timestamp.
    /// </summary>
    /// <returns>The current timestamp.</returns>
    public long GetTimestamp()
    {
        return timestamp;
    }

    /// <summary>
    /// Sets the timestamp used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetTimestamp(long value)
    {
        timestamp = EmailDkimSigningValidation.RequireTime(value, "value");
        hasTimestamp = true;
    }

    /// <summary>
    /// Clears the stored timestamp value or values.
    /// </summary>
    public void ClearTimestamp()
    {
        hasTimestamp = false;
        timestamp = 0;
    }

    /// <summary>
    /// Determines whether the body length limit is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body length limit is available; otherwise, <c>false</c>.</returns>
    public bool HasBodyLengthLimit()
    {
        return hasBodyLengthLimit;
    }

    /// <summary>
    /// Gets the body length limit.
    /// </summary>
    /// <returns>The current body length limit.</returns>
    public long GetBodyLengthLimit()
    {
        return bodyLengthLimit;
    }

    /// <summary>
    /// Sets the body length limit used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetBodyLengthLimit(long value)
    {
        bodyLengthLimit = EmailLimitValidation.RequireNonNegative(value, "value");
        hasBodyLengthLimit = true;
    }

    /// <summary>
    /// Clears the stored body length limit value or values.
    /// </summary>
    public void ClearBodyLengthLimit()
    {
        hasBodyLengthLimit = false;
        bodyLengthLimit = 0;
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
    /// Gets the minimum RSA key bits.
    /// </summary>
    /// <returns>The current minimum RSA key bits.</returns>
    public int GetMinimumRsaKeyBits()
    {
        return minimumRsaKeyBits;
    }

    /// <summary>
    /// Sets the minimum RSA key bits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMinimumRsaKeyBits(int value)
    {
        if (value < DefaultMinimumRsaKeyBits || value > maximumRsaKeyBits)
            throw new ArgumentOutOfRangeException("value");
        minimumRsaKeyBits = value;
    }

    /// <summary>
    /// Gets the maximum RSA key bits.
    /// </summary>
    /// <returns>The current maximum RSA key bits.</returns>
    public int GetMaximumRsaKeyBits()
    {
        return maximumRsaKeyBits;
    }

    /// <summary>
    /// Sets the maximum RSA key bits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumRsaKeyBits(int value)
    {
        if (value < DefaultMaximumRsaKeyBits || value < minimumRsaKeyBits)
            throw new ArgumentOutOfRangeException("value");
        maximumRsaKeyBits = value;
    }

    /// <summary>
    /// Gets the preferred line length.
    /// </summary>
    /// <returns>The current preferred line length.</returns>
    public int GetPreferredLineLength()
    {
        return preferredLineLength;
    }

    /// <summary>
    /// Sets the preferred line length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetPreferredLineLength(int value)
    {
        if (value < 20 || value > 998)
            throw new ArgumentOutOfRangeException("value");
        preferredLineLength = value;
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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailArcSealingOptions Clone()
    {
        EmailArcSealingOptions copy = new EmailArcSealingOptions();
        if (messageSignatureDomain.Length > 0)
            copy.SetMessageSignatureDomain(messageSignatureDomain);
        if (messageSignatureSelector.Length > 0)
            copy.SetMessageSignatureSelector(messageSignatureSelector);
        if (sealDomain.Length > 0)
            copy.SetSealDomain(sealDomain);
        if (sealSelector.Length > 0)
            copy.SetSealSelector(sealSelector);
        copy.ClearSignedHeaders();
        int index = 0;
        while (index < signedHeaderNames.Count)
        {
            copy.AddSignedHeader(signedHeaderNames[index]);
            index++;
        }

        copy.SetIncludeExistingDkimSignatures(includeExistingDkimSignatures);
        copy.SetHeaderCanonicalizationKind(headerCanonicalizationKind);
        copy.SetBodyCanonicalizationKind(bodyCanonicalizationKind);
        if (hasTimestamp)
            copy.SetTimestamp(timestamp);
        if (hasBodyLengthLimit)
            copy.SetBodyLengthLimit(bodyLengthLimit);
        copy.SetMaximumBodyInputBytes(maximumBodyInputBytes);
        copy.SetMaximumCanonicalizedBodyBytes(maximumCanonicalizedBodyBytes);
        copy.SetMaximumCanonicalizedHeaderBytes(maximumCanonicalizedHeaderBytes);
        copy.SetMaximumSignatureHeaderBytes(maximumSignatureHeaderBytes);
        copy.SetMaximumSignedHeaderCount(maximumSignedHeaderCount);
        copy.SetMaximumRsaKeyBits(maximumRsaKeyBits);
        copy.SetMinimumRsaKeyBits(minimumRsaKeyBits);
        copy.SetPreferredLineLength(preferredLineLength);
        copy.SetCollectionOptions(collectionOptions);
        return copy;
    }
}
