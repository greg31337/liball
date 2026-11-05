using System.Collections.Generic;

namespace sdp;

/// <summary>Projects RTP source, stream, simulcast, and image signaling.</summary>
public interface ISdpStreamService
{
    /// <summary>Resolves valid synchronization-source attributes in media attribute order.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>Independent source-attribute projections retaining their backing attributes.</returns>
    List<SdpSourceAttribute> ResolveSourceAttributes(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves valid synchronization-source groups in media attribute order.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>Independent source-group projections retaining their backing attributes.</returns>
    List<SdpSourceGroupAttribute> ResolveSourceGroups(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves valid media- and source-level MSID associations in media attribute order.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>Independent MSID projections retaining their backing attributes.</returns>
    List<SdpMediaStreamId> ResolveMediaStreamIds(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves valid session-level MSID semantic declarations in source order.</summary>
    /// <param name="description">The session description containing the semantic declarations.</param>
    /// <returns>Independent MSID semantic projections retaining their backing attributes.</returns>
    List<SdpMediaStreamSemanticAttribute> ResolveMediaStreamSemantics(SdpSessionDescription description);

    /// <summary>Resolves valid RID attributes in media attribute order.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>Independent RID projections retaining their backing attributes.</returns>
    List<SdpRidAttribute> ResolveRids(SdpSessionDescription description, int mediaIndex);

    /// <summary>Resolves the first valid simulcast attribute of a media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>An independent simulcast projection, or <c>null</c> when none is valid.</returns>
    SdpSimulcastAttribute? ResolveSimulcast(SdpSessionDescription description, int mediaIndex);

    /// <summary>Atomically renames one RID and every dependency and simulcast reference to it.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="currentId">The exact existing RID identifier.</param>
    /// <param name="replacementId">The unique replacement RID identifier.</param>
    /// <returns>The renamed RID projection linked to its updated source attribute.</returns>
    SdpRidAttribute RenameRid(SdpSessionDescription description, int mediaIndex, string currentId, string replacementId);

    /// <summary>Atomically removes one RID and its dependency and simulcast references.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="ridId">The exact RID identifier to remove.</param>
    /// <returns><c>true</c> when the RID existed and was removed; otherwise, <c>false</c>.</returns>
    bool RemoveRid(SdpSessionDescription description, int mediaIndex, string ridId);

    /// <summary>Atomically retains selected simulcast layers and removes unselected RID references.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="retainedRidIds">The distinct simulcast RID identifiers to retain.</param>
    /// <returns>The filtered simulcast projection, or <c>null</c> when no simulcast layer remains.</returns>
    SdpSimulcastAttribute? FilterSimulcastLayers(SdpSessionDescription description, int mediaIndex, List<string> retainedRidIds);

    /// <summary>Resolves valid image-constraint attributes in media attribute order.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>Independent image-constraint projections retaining their backing attributes.</returns>
    List<SdpImageAttribute> ResolveImageAttributes(SdpSessionDescription description, int mediaIndex);
}
