using System;

namespace sdp;

/// <summary>Clones SDP descriptions and performs ordered core-model mutations.</summary>
public class SdpEditor : ISdpEditor
{
    /// <summary>Initializes an SDP editor.</summary>
    public SdpEditor()
    {
    }

    /// <summary>Creates a deep copy of a session description and all nested mutable state.</summary>
    /// <param name="description">The description to copy.</param>
    /// <returns>An independent copy of the complete description.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when nested mutable state is uninitialized.</exception>
    public SdpSessionDescription CloneDescription(SdpSessionDescription description)
    {
        ValidateDescription(description);
        return SdpCloneCore.CopyDescription(description);
    }

    /// <summary>Appends an attribute to session scope.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="attribute">The attribute instance to append.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public void AddSessionAttribute(SdpSessionDescription description, SdpAttribute attribute)
    {
        ValidateDescription(description);
        ValidateAttribute(attribute);
        description.Attributes.Add(attribute);
    }

    /// <summary>Appends an attribute to a media scope.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <param name="attribute">The attribute instance to append.</param>
    /// <exception cref="ArgumentNullException">Thrown when the description or attribute argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is invalid.</exception>
    public void AddMediaAttribute(SdpSessionDescription description, int mediaIndex, SdpAttribute attribute)
    {
        ValidateMediaIndex(description, mediaIndex);
        ValidateAttribute(attribute);
        description.MediaDescriptions[mediaIndex].Attributes.Add(attribute);
    }

    /// <summary>Inserts an attribute at a session-scope index.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="attributeIndex">The zero-based insertion index.</param>
    /// <param name="attribute">The attribute instance to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when the description or attribute argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the attribute index is invalid.</exception>
    public void InsertSessionAttribute(SdpSessionDescription description, int attributeIndex, SdpAttribute attribute)
    {
        ValidateDescription(description);
        ValidateAttributeInsertIndex(description.Attributes.Count, attributeIndex);
        ValidateAttribute(attribute);
        description.Attributes.Insert(attributeIndex, attribute);
    }

    /// <summary>Inserts an attribute at a media-scope index.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <param name="attributeIndex">The zero-based insertion index.</param>
    /// <param name="attribute">The attribute instance to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when the description or attribute argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an index is invalid.</exception>
    public void InsertMediaAttribute(SdpSessionDescription description, int mediaIndex, int attributeIndex, SdpAttribute attribute)
    {
        ValidateMediaIndex(description, mediaIndex);
        ValidateAttributeInsertIndex(description.MediaDescriptions[mediaIndex].Attributes.Count, attributeIndex);
        ValidateAttribute(attribute);
        description.MediaDescriptions[mediaIndex].Attributes.Insert(attributeIndex, attribute);
    }

    /// <summary>Replaces a session-scope attribute.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="attributeIndex">The zero-based attribute index.</param>
    /// <param name="attribute">The replacement attribute instance.</param>
    /// <returns>The attribute that was replaced.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or attribute argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the attribute index is invalid.</exception>
    public SdpAttribute ReplaceSessionAttribute(SdpSessionDescription description, int attributeIndex, SdpAttribute attribute)
    {
        ValidateDescription(description);
        ValidateAttributeIndex(description.Attributes.Count, attributeIndex);
        ValidateAttribute(attribute);
        return SdpMutationCore.ReplaceAttribute(description.Attributes, attributeIndex, attribute);
    }

    /// <summary>Replaces a media-scope attribute.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <param name="attributeIndex">The zero-based attribute index.</param>
    /// <param name="attribute">The replacement attribute instance.</param>
    /// <returns>The attribute that was replaced.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or attribute argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an index is invalid.</exception>
    public SdpAttribute ReplaceMediaAttribute(SdpSessionDescription description, int mediaIndex, int attributeIndex, SdpAttribute attribute)
    {
        ValidateMediaIndex(description, mediaIndex);
        ValidateAttributeIndex(description.MediaDescriptions[mediaIndex].Attributes.Count, attributeIndex);
        ValidateAttribute(attribute);
        return SdpMutationCore.ReplaceAttribute(description.MediaDescriptions[mediaIndex].Attributes, attributeIndex, attribute);
    }

    /// <summary>Removes one session-scope attribute by index.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="attributeIndex">The zero-based attribute index.</param>
    /// <returns>The removed attribute.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the attribute index is invalid.</exception>
    public SdpAttribute RemoveSessionAttributeAt(SdpSessionDescription description, int attributeIndex)
    {
        ValidateDescription(description);
        ValidateAttributeIndex(description.Attributes.Count, attributeIndex);
        return SdpMutationCore.RemoveAttributeAt(description.Attributes, attributeIndex);
    }

    /// <summary>Removes one media-scope attribute by index.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <param name="attributeIndex">The zero-based attribute index.</param>
    /// <returns>The removed attribute.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an index is invalid.</exception>
    public SdpAttribute RemoveMediaAttributeAt(SdpSessionDescription description, int mediaIndex, int attributeIndex)
    {
        ValidateMediaIndex(description, mediaIndex);
        ValidateAttributeIndex(description.MediaDescriptions[mediaIndex].Attributes.Count, attributeIndex);
        return SdpMutationCore.RemoveAttributeAt(description.MediaDescriptions[mediaIndex].Attributes, attributeIndex);
    }

    /// <summary>Removes all session-scope attributes with an ASCII case-insensitive name match.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="attributeName">The attribute name to remove.</param>
    /// <returns>The number of removed attributes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or attribute-name argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the attribute name is not a token.</exception>
    public int RemoveSessionAttributesByName(SdpSessionDescription description, string attributeName)
    {
        ValidateDescription(description);
        ValidateAttributeName(attributeName);
        return SdpMutationCore.RemoveAttributesByName(description.Attributes, attributeName);
    }

    /// <summary>Removes all media-scope attributes with an ASCII case-insensitive name match.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <param name="attributeName">The attribute name to remove.</param>
    /// <returns>The number of removed attributes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description or attribute-name argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the attribute name is not a token.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is invalid.</exception>
    public int RemoveMediaAttributesByName(SdpSessionDescription description, int mediaIndex, string attributeName)
    {
        ValidateMediaIndex(description, mediaIndex);
        ValidateAttributeName(attributeName);
        return SdpMutationCore.RemoveAttributesByName(description.MediaDescriptions[mediaIndex].Attributes, attributeName);
    }

    /// <summary>Appends a media description.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="media">The media-description instance to append.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public void AddMedia(SdpSessionDescription description, SdpMediaDescription media)
    {
        ValidateDescription(description);
        ValidateMedia(media);
        description.MediaDescriptions.Add(media);
    }

    /// <summary>Inserts a media description at an index.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based insertion index.</param>
    /// <param name="media">The media-description instance to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the insertion index is invalid.</exception>
    public void InsertMedia(SdpSessionDescription description, int mediaIndex, SdpMediaDescription media)
    {
        ValidateDescription(description);
        ValidateMediaInsertIndex(description.MediaDescriptions.Count, mediaIndex);
        ValidateMedia(media);
        description.MediaDescriptions.Insert(mediaIndex, media);
    }

    /// <summary>Moves a media description to a different final index.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="sourceMediaIndex">The current zero-based media index.</param>
    /// <param name="destinationMediaIndex">The zero-based index occupied by the media after the move.</param>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an index is invalid.</exception>
    public void MoveMedia(SdpSessionDescription description, int sourceMediaIndex, int destinationMediaIndex)
    {
        ValidateDescription(description);
        if (sourceMediaIndex < 0 || sourceMediaIndex >= description.MediaDescriptions.Count)
        {
            throw new ArgumentOutOfRangeException("sourceMediaIndex");
        }
        if (destinationMediaIndex < 0 || destinationMediaIndex >= description.MediaDescriptions.Count)
        {
            throw new ArgumentOutOfRangeException("destinationMediaIndex");
        }
        ValidateMedia(description.MediaDescriptions[sourceMediaIndex]);
        ValidateMedia(description.MediaDescriptions[destinationMediaIndex]);
        SdpMutationCore.MoveMedia(description.MediaDescriptions, sourceMediaIndex, destinationMediaIndex);
    }

    /// <summary>Removes one media description by index.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>The removed media-description instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is invalid.</exception>
    public SdpMediaDescription RemoveMediaAt(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaIndex(description, mediaIndex);
        return SdpMutationCore.RemoveMediaAt(description.MediaDescriptions, mediaIndex);
    }

    /// <summary>Rejects a media description by setting its port to zero.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index is invalid.</exception>
    public void RejectMedia(SdpSessionDescription description, int mediaIndex)
    {
        ValidateMediaIndex(description, mediaIndex);
        description.MediaDescriptions[mediaIndex].Port = 0;
    }

    /// <summary>Reactivates a rejected media description with an explicit nonzero port.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <param name="port">The nonzero transport port.</param>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the media index or port is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the media description is not rejected.</exception>
    public void ReactivateMedia(SdpSessionDescription description, int mediaIndex, int port)
    {
        ValidateMediaIndex(description, mediaIndex);
        SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
        if (port <= 0 || port > 65535 || media.HasPortCount && media.PortCount > 65536 - port)
        {
            throw new ArgumentOutOfRangeException("port");
        }
        if (media.Port != 0)
        {
            throw new InvalidOperationException("Only a rejected media description can be reactivated.");
        }
        media.Port = port;
    }

    /// <summary>Atomically increments the origin session version.</summary>
    /// <param name="description">The description to update.</param>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the origin version is absent, invalid, or cannot be incremented.</exception>
    public void IncrementOriginVersion(SdpSessionDescription description)
    {
        ValidateDescription(description);
        SdpMutationCore.IncrementOriginVersion(description);
    }

    /// <summary>Normalizes a description to canonical field, token, whitespace, timing, attribute, format, and line-ending forms.</summary>
    /// <param name="description">The description to normalize in place.</param>
    /// <remarks>Normalization preserves semantic model values but discards retained raw-line provenance that no longer describes the normalized state.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when the description argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when state required for normalization is uninitialized, invalid, or unrepresentable.</exception>
    public void NormalizeDescription(SdpSessionDescription description)
    {
        ValidateDescription(description);
        SdpSessionDescription validationCopy = SdpCloneCore.CopyDescription(description);
        SdpNormalizationCore.NormalizeDescription(validationCopy);
        SdpNormalizationCore.NormalizeDescription(description);
    }

    internal void ValidateDescription(SdpSessionDescription description)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
        if (description.Attributes == null || description.MediaDescriptions == null)
        {
            throw new ArgumentException("The description must contain initialized attribute and media collections.", "description");
        }
    }

    internal void ValidateAttribute(SdpAttribute attribute)
    {
        if (attribute == null)
        {
            throw new ArgumentNullException("attribute");
        }
    }

    internal void ValidateAttributeIndex(int attributeCount, int attributeIndex)
    {
        if (attributeIndex < 0 || attributeIndex >= attributeCount)
        {
            throw new ArgumentOutOfRangeException("attributeIndex");
        }
    }

    internal void ValidateAttributeInsertIndex(int attributeCount, int attributeIndex)
    {
        if (attributeIndex < 0 || attributeIndex > attributeCount)
        {
            throw new ArgumentOutOfRangeException("attributeIndex");
        }
    }

    internal void ValidateMedia(SdpMediaDescription media)
    {
        if (media == null)
        {
            throw new ArgumentNullException("media");
        }
        if (media.Attributes == null)
        {
            throw new ArgumentException("The media description must contain an initialized attribute collection.", "media");
        }
    }

    internal void ValidateMediaIndex(SdpSessionDescription description, int mediaIndex)
    {
        ValidateDescription(description);
        if (mediaIndex < 0 || mediaIndex >= description.MediaDescriptions.Count)
        {
            throw new ArgumentOutOfRangeException("mediaIndex");
        }
        ValidateMedia(description.MediaDescriptions[mediaIndex]);
    }

    internal void ValidateMediaInsertIndex(int mediaCount, int mediaIndex)
    {
        if (mediaIndex < 0 || mediaIndex > mediaCount)
        {
            throw new ArgumentOutOfRangeException("mediaIndex");
        }
    }

    internal void ValidateAttributeName(string attributeName)
    {
        if (attributeName == null)
        {
            throw new ArgumentNullException("attributeName");
        }
        if (!SdpCommonAttributeCodecCore.IsTokenValue(attributeName))
        {
            throw new ArgumentException("The attribute name must use nonempty token syntax.", "attributeName");
        }
    }
}
