using System.Collections.Generic;

namespace sdp;

/// <summary>Resolves media groups and performs controlled group mutations.</summary>
public interface ISdpGroupService
{
    /// <summary>Resolves valid session-level group attributes in source order.</summary>
    /// <param name="description">The session description containing the group attributes.</param>
    /// <returns>Independent group projections that retain their backing source attributes.</returns>
    List<SdpGroupAttribute> ResolveGroups(SdpSessionDescription description);

    /// <summary>Resolves the first valid media identifier of a media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>An independent media-identifier projection, or <c>null</c> when none is valid.</returns>
    SdpMediaIdAttribute? ResolveMediaId(SdpSessionDescription description, int mediaIndex);

    /// <summary>Finds the first media section with an exact media identifier.</summary>
    /// <param name="description">The session description to search.</param>
    /// <param name="mediaId">The exact media identifier to find.</param>
    /// <returns>The zero-based media-section index, or <c>-1</c> when no section matches.</returns>
    int FindMediaIndex(SdpSessionDescription description, string mediaId);

    /// <summary>Creates and appends a session-level group attribute.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="semantic">The recognized semantic, or <see cref="SdpGroupSemantic.Unknown"/> for an extension semantic.</param>
    /// <param name="semanticToken">The extension token, or the canonical token or an empty string for a recognized semantic.</param>
    /// <param name="identificationTags">The ordered identification tags.</param>
    /// <returns>The created group projection linked to its source attribute.</returns>
    SdpGroupAttribute AddGroup(SdpSessionDescription description, SdpGroupSemantic semantic, string semanticToken, List<string> identificationTags);

    /// <summary>Atomically replaces a group projection and its backing source attribute.</summary>
    /// <param name="group">The group projection to update.</param>
    /// <param name="semantic">The recognized semantic, or <see cref="SdpGroupSemantic.Unknown"/> for an extension semantic.</param>
    /// <param name="semanticToken">The extension token, or the canonical token or an empty string for a recognized semantic.</param>
    /// <param name="identificationTags">The replacement ordered identification tags.</param>
    void UpdateGroup(SdpGroupAttribute group, SdpGroupSemantic semantic, string semanticToken, List<string> identificationTags);

    /// <summary>Removes the source attribute represented by a resolved group.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="group">The resolved group whose source attribute will be removed.</param>
    /// <returns><c>true</c> when the source attribute was present and removed; otherwise, <c>false</c>.</returns>
    bool RemoveGroup(SdpSessionDescription description, SdpGroupAttribute group);
}
