using System;
using System.Collections.Generic;

namespace sdp;

/// <summary>Projects RTP source, stream, simulcast, and image signaling.</summary>
public class SdpStreamService : ISdpStreamService
{
    /// <summary>Initializes an SDP stream service.</summary>
    public SdpStreamService()
    {
    }

    /// <summary>Resolves valid synchronization-source attributes in media attribute order.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>Independent source-attribute projections retaining their backing attributes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public List<SdpSourceAttribute> ResolveSourceAttributes(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpStreamServiceCore.ResolveSourceAttributes(description.MediaDescriptions[mediaIndex]);
    }

    /// <summary>Resolves valid synchronization-source groups in media attribute order.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>Independent source-group projections retaining their backing attributes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public List<SdpSourceGroupAttribute> ResolveSourceGroups(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpStreamServiceCore.ResolveSourceGroups(description.MediaDescriptions[mediaIndex]);
    }

    /// <summary>Resolves valid media- and source-level MSID associations in media attribute order.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>Independent MSID projections retaining their backing attributes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public List<SdpMediaStreamId> ResolveMediaStreamIds(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpStreamServiceCore.ResolveMediaStreamIds(description.MediaDescriptions[mediaIndex]);
    }

    /// <summary>Resolves valid session-level MSID semantic declarations in source order.</summary>
    /// <param name="description">The session description containing the semantic declarations.</param>
    /// <returns>Independent MSID semantic projections retaining their backing attributes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    public List<SdpMediaStreamSemanticAttribute> ResolveMediaStreamSemantics(SdpSessionDescription description)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        return SdpStreamServiceCore.ResolveMediaStreamSemantics(description);
    }

    /// <summary>Resolves valid RID attributes in media attribute order.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>Independent RID projections retaining their backing attributes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public List<SdpRidAttribute> ResolveRids(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpStreamServiceCore.ResolveRids(description.MediaDescriptions[mediaIndex]);
    }

    /// <summary>Resolves the first valid simulcast attribute of a media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>An independent simulcast projection, or <c>null</c> when none is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public SdpSimulcastAttribute? ResolveSimulcast(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpStreamServiceCore.ResolveSimulcast(description.MediaDescriptions[mediaIndex]);
    }

    /// <summary>Atomically renames one RID and every dependency and simulcast reference to it.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="currentId">The exact existing RID identifier.</param>
    /// <param name="replacementId">The unique replacement RID identifier.</param>
    /// <returns>The renamed RID projection linked to its updated source attribute.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or an identifier is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    /// <exception cref="ArgumentException">Thrown when an identifier or the referenced RID graph cannot be edited safely.</exception>
    public SdpRidAttribute RenameRid(SdpSessionDescription description, int mediaIndex, string currentId, string replacementId)
    {
        ValidateMediaSelection(description, mediaIndex);
        ValidateRidIdentifier(currentId, "currentId");
        ValidateRidIdentifier(replacementId, "replacementId");
        return SdpStreamServiceCore.RenameRid(description.MediaDescriptions[mediaIndex], currentId, replacementId);
    }

    /// <summary>Atomically removes one RID and its dependency and simulcast references.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="ridId">The exact RID identifier to remove.</param>
    /// <returns><c>true</c> when the RID existed and was removed; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or RID identifier is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    /// <exception cref="ArgumentException">Thrown when the identifier or referenced RID graph cannot be edited safely.</exception>
    public bool RemoveRid(SdpSessionDescription description, int mediaIndex, string ridId)
    {
        ValidateMediaSelection(description, mediaIndex);
        ValidateRidIdentifier(ridId, "ridId");
        return SdpStreamServiceCore.RemoveRid(description.MediaDescriptions[mediaIndex], ridId);
    }

    /// <summary>Atomically retains selected simulcast layers and removes unselected RID references.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <param name="retainedRidIds">The distinct simulcast RID identifiers to retain.</param>
    /// <returns>The filtered simulcast projection, or <c>null</c> when no simulcast layer remains.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or retained-identifier collection is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    /// <exception cref="ArgumentException">Thrown when an identifier or the referenced RID graph cannot be edited safely.</exception>
    public SdpSimulcastAttribute? FilterSimulcastLayers(SdpSessionDescription description, int mediaIndex, List<string> retainedRidIds)
    {
        ValidateMediaSelection(description, mediaIndex);
        if (retainedRidIds == null)
        {
            throw new ArgumentNullException("retainedRidIds");
        }
        int index = 0;
        while (index < retainedRidIds.Count)
        {
            string identifier = retainedRidIds[index];
            if (identifier == null || !SdpRidAttributeCodecCore.IsRidIdentifier(identifier) || retainedRidIds.IndexOf(identifier) != index)
            {
                throw new ArgumentException("Retained RID identifiers must be distinct and use valid RID identifier syntax.", "retainedRidIds");
            }
            index = index + 1;
        }
        return SdpStreamServiceCore.FilterSimulcastLayers(description.MediaDescriptions[mediaIndex], retainedRidIds);
    }

    /// <summary>Resolves valid image-constraint attributes in media attribute order.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>Independent image-constraint projections retaining their backing attributes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public List<SdpImageAttribute> ResolveImageAttributes(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpStreamServiceCore.ResolveImageAttributes(description.MediaDescriptions[mediaIndex]);
    }

    internal void ValidateMediaSelection(SdpSessionDescription description, int mediaIndex)
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

    internal void ValidateRidIdentifier(string identifier, string parameterName)
    {
        if (identifier == null)
        {
            throw new ArgumentNullException(parameterName);
        }
        if (!SdpRidAttributeCodecCore.IsRidIdentifier(identifier))
        {
            throw new ArgumentException("A valid RID identifier is required.", parameterName);
        }
    }
}
