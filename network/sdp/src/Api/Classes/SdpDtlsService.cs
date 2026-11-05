using System;
using System.Collections.Generic;

namespace sdp;

/// <summary>Resolves and edits effective TLS and DTLS signaling for SDP media.</summary>
public class SdpDtlsService : ISdpDtlsService
{
    /// <summary>Initializes an SDP TLS and DTLS service.</summary>
    public SdpDtlsService()
    {
    }

    /// <summary>Resolves session defaults and media-level TLS or DTLS signaling.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The effective TLS or DTLS signaling for the selected media section.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public SdpDtlsDescription ResolveMediaDtls(SdpSessionDescription description, int mediaIndex)
    {
        ValidateResolutionArguments(description, mediaIndex);
        return SdpDtlsServiceCore.ResolveMediaDtls(description, mediaIndex);
    }

    /// <summary>Resolves effective TLS or DTLS signaling using the BUNDLE-tagged media section when applicable.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The effective TLS or DTLS signaling for the selected media transport.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public SdpDtlsDescription ResolveEffectiveDtls(SdpSessionDescription description, int mediaIndex)
    {
        ValidateResolutionArguments(description, mediaIndex);
        int transportMediaIndex = SdpDtlsServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
        return SdpDtlsServiceCore.ResolveMediaDtls(description, transportMediaIndex);
    }

    /// <summary>Resolves the media section that owns TLS or DTLS transport signaling for a selected media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The zero-based media-section index that owns the TLS or DTLS transport.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public int ResolveDtlsTransportMediaIndex(SdpSessionDescription description, int mediaIndex)
    {
        ValidateResolutionArguments(description, mediaIndex);
        return SdpDtlsServiceCore.ResolveTransportMediaIndex(description, mediaIndex);
    }

    /// <summary>Selects the first offered fingerprint using caller-supplied hash-function preference order.</summary>
    /// <param name="description">The resolved TLS or DTLS signaling.</param>
    /// <param name="preferredAlgorithms">Hash-function tokens ordered from most to least preferred.</param>
    /// <returns>The preferred fingerprint, or <c>null</c> when no preferred algorithm was offered.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or preferred-algorithms argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a preferred hash-function token is invalid.</exception>
    public SdpFingerprintAttribute? SelectPreferredFingerprint(SdpDtlsDescription description, List<string> preferredAlgorithms)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        if (preferredAlgorithms == null)
        {
            throw new ArgumentNullException("preferredAlgorithms");
        }
        return SdpDtlsServiceCore.SelectPreferredFingerprint(description, preferredAlgorithms);
    }

    /// <summary>Selects a compatible DTLS answer setup role.</summary>
    /// <param name="offeredRole">The effective setup role from the offer.</param>
    /// <param name="preferredAnswerRole">The preferred active or passive answer role.</param>
    /// <returns>The compatible answer setup role.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when either role is not a defined setup-role value.</exception>
    /// <exception cref="ArgumentException">Thrown when either role cannot participate in DTLS answer selection.</exception>
    public SdpDtlsSetupRole SelectAnswerSetupRole(SdpDtlsSetupRole offeredRole, SdpDtlsSetupRole preferredAnswerRole)
    {
        return SdpDtlsServiceCore.SelectAnswerSetupRole(offeredRole, preferredAnswerRole);
    }

    /// <summary>Replaces media-level fingerprint attributes in caller-supplied order.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="fingerprints">The fingerprints to set, or an empty list to clear media-level fingerprints.</param>
    /// <exception cref="ArgumentNullException">Thrown when the description or fingerprints argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    /// <exception cref="ArgumentException">Thrown when a fingerprint cannot be represented as an SDP attribute.</exception>
    public void ReplaceFingerprints(SdpSessionDescription description, int mediaIndex, List<SdpFingerprintAttribute> fingerprints)
    {
        ValidateResolutionArguments(description, mediaIndex);
        if (fingerprints == null)
        {
            throw new ArgumentNullException("fingerprints");
        }
        SdpDtlsServiceCore.ReplaceFingerprints(description.MediaDescriptions[mediaIndex], fingerprints);
    }

    /// <summary>Replaces the media-level TLS connection or DTLS association identifier.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="tlsId">The TLS identifier to set.</param>
    /// <exception cref="ArgumentNullException">Thrown when the description or TLS-identifier argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    /// <exception cref="ArgumentException">Thrown when the TLS identifier cannot be represented as an SDP attribute.</exception>
    public void ReplaceTlsId(SdpSessionDescription description, int mediaIndex, SdpTlsIdAttribute tlsId)
    {
        ValidateResolutionArguments(description, mediaIndex);
        if (tlsId == null)
        {
            throw new ArgumentNullException("tlsId");
        }
        SdpDtlsServiceCore.ReplaceTlsId(description.MediaDescriptions[mediaIndex], tlsId);
    }

    internal void ValidateResolutionArguments(SdpSessionDescription description, int mediaIndex)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        if (mediaIndex < 0 || mediaIndex >= description.MediaDescriptions.Count)
        {
            throw new ArgumentOutOfRangeException("mediaIndex");
        }
    }
}
