using System;
using System.Collections.Generic;

namespace sdp;

/// <summary>Resolves media groups and performs controlled group mutations.</summary>
public class SdpGroupService : ISdpGroupService
{
    /// <summary>Initializes an SDP media-group service.</summary>
    public SdpGroupService()
    {
    }

    /// <summary>Resolves valid session-level group attributes in source order.</summary>
    /// <param name="description">The session description containing the group attributes.</param>
    /// <returns>Independent group projections that retain their backing source attributes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    public List<SdpGroupAttribute> ResolveGroups(SdpSessionDescription description)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        return SdpGroupServiceCore.ResolveGroups(description);
    }

    /// <summary>Resolves the first valid media identifier of a media section.</summary>
    /// <param name="description">The session description containing the media section.</param>
    /// <param name="mediaIndex">The zero-based media-section index.</param>
    /// <returns>An independent media-identifier projection, or <c>null</c> when none is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index does not identify a media section.</exception>
    public SdpMediaIdAttribute? ResolveMediaId(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaSelection(description, mediaIndex);
        return SdpGroupServiceCore.ResolveMediaId(description.MediaDescriptions[mediaIndex]);
    }

    /// <summary>Finds the first media section with an exact media identifier.</summary>
    /// <param name="description">The session description to search.</param>
    /// <param name="mediaId">The exact media identifier to find.</param>
    /// <returns>The zero-based media-section index, or <c>-1</c> when no section matches.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or media identifier argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the media identifier is not a token.</exception>
    public int FindMediaIndex(SdpSessionDescription description, string mediaId)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        if (mediaId == null)
        {
            throw new ArgumentNullException("mediaId");
        }
        SdpGroupServiceCore.ValidateMediaId(mediaId);
        return SdpGroupServiceCore.FindMediaIndex(description, mediaId);
    }

    /// <summary>Creates and appends a session-level group attribute.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="semantic">The recognized semantic, or <see cref="SdpGroupSemantic.Unknown"/> for an extension semantic.</param>
    /// <param name="semanticToken">The extension token, or the canonical token or an empty string for a recognized semantic.</param>
    /// <param name="identificationTags">The ordered identification tags.</param>
    /// <returns>The created group projection linked to its source attribute.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description, semantic token, or identification-tag collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the semantic or an identification tag cannot be represented.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the semantic is not a defined value.</exception>
    public SdpGroupAttribute AddGroup(SdpSessionDescription description, SdpGroupSemantic semantic, string semanticToken, List<string> identificationTags)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        ValidateGroupInput(semanticToken, identificationTags);
        SdpGroupAttribute group = SdpGroupServiceCore.CreateGroup(semantic, semanticToken, identificationTags);
        SdpGroupServiceCore.AddGroup(description, group);
        return group;
    }

    /// <summary>Atomically replaces a group projection and its backing source attribute.</summary>
    /// <param name="group">The group projection to update.</param>
    /// <param name="semantic">The recognized semantic, or <see cref="SdpGroupSemantic.Unknown"/> for an extension semantic.</param>
    /// <param name="semanticToken">The extension token, or the canonical token or an empty string for a recognized semantic.</param>
    /// <param name="identificationTags">The replacement ordered identification tags.</param>
    /// <exception cref="ArgumentNullException">Thrown when the group, semantic token, or identification-tag collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the semantic or an identification tag cannot be represented.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the semantic is not a defined value.</exception>
    public void UpdateGroup(SdpGroupAttribute group, SdpGroupSemantic semantic, string semanticToken, List<string> identificationTags)
    {
        if (group == null)
        {
            throw new ArgumentNullException("group");
        }
        ValidateGroupInput(semanticToken, identificationTags);
        SdpGroupAttribute replacement = SdpGroupServiceCore.CreateGroup(semantic, semanticToken, identificationTags);
        SdpGroupServiceCore.UpdateGroup(group, replacement);
    }

    /// <summary>Removes the source attribute represented by a resolved group.</summary>
    /// <param name="description">The session description to update.</param>
    /// <param name="group">The resolved group whose source attribute will be removed.</param>
    /// <returns><c>true</c> when the source attribute was present and removed; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or group argument is null.</exception>
    public bool RemoveGroup(SdpSessionDescription description, SdpGroupAttribute group)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        if (group == null)
        {
            throw new ArgumentNullException("group");
        }
        return SdpGroupServiceCore.RemoveGroup(description, group);
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

    internal void ValidateGroupInput(string semanticToken, List<string> identificationTags)
    {
        if (semanticToken == null)
        {
            throw new ArgumentNullException("semanticToken");
        }
        if (identificationTags == null)
        {
            throw new ArgumentNullException("identificationTags");
        }
        int index = 0;
        while (index < identificationTags.Count)
        {
            if (identificationTags[index] == null)
            {
                throw new ArgumentException("A group identification-tag collection must not contain null entries.", "identificationTags");
            }
            index = index + 1;
        }
    }
}
