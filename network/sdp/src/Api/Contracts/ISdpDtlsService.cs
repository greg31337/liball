using System.Collections.Generic;

namespace sdp;

/// <summary>Resolves and edits effective TLS and DTLS signaling for SDP media.</summary>
public interface ISdpDtlsService
{
    /// <summary>Resolves session defaults and media-level TLS or DTLS signaling.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The effective TLS or DTLS signaling for the selected media section.</returns>
    SdpDtlsDescription ResolveMediaDtls(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves effective TLS or DTLS signaling using the BUNDLE-tagged media section when applicable.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The effective TLS or DTLS signaling for the selected media transport.</returns>
    SdpDtlsDescription ResolveEffectiveDtls(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves the media section that owns TLS or DTLS transport signaling for a selected media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>The zero-based media-section index that owns the TLS or DTLS transport.</returns>
    int ResolveDtlsTransportMediaIndex(SdpSessionDescription description, int mediaIndex);

    /// <summary>Selects the first offered fingerprint using caller-supplied hash-function preference order.</summary>
    /// <param name="description">The resolved TLS or DTLS signaling.</param>
    /// <param name="preferredAlgorithms">Hash-function tokens ordered from most to least preferred.</param>
    /// <returns>The preferred fingerprint, or <c>null</c> when no preferred algorithm was offered.</returns>
    SdpFingerprintAttribute? SelectPreferredFingerprint(SdpDtlsDescription description, List<string> preferredAlgorithms);

    /// <summary>Selects a compatible DTLS answer setup role.</summary>
    /// <param name="offeredRole">The effective setup role from the offer.</param>
    /// <param name="preferredAnswerRole">The preferred active or passive answer role.</param>
    /// <returns>The compatible answer setup role.</returns>
    SdpDtlsSetupRole SelectAnswerSetupRole(SdpDtlsSetupRole offeredRole, SdpDtlsSetupRole preferredAnswerRole);

    /// <summary>Replaces media-level fingerprint attributes in caller-supplied order.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="fingerprints">The fingerprints to set, or an empty list to clear media-level fingerprints.</param>
    void ReplaceFingerprints(SdpSessionDescription description, int mediaIndex, List<SdpFingerprintAttribute> fingerprints);

    /// <summary>Replaces the media-level TLS connection or DTLS association identifier.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="tlsId">The TLS identifier to set.</param>
    void ReplaceTlsId(SdpSessionDescription description, int mediaIndex, SdpTlsIdAttribute tlsId);
}
