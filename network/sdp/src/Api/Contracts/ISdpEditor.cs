namespace sdp;

/// <summary>Clones SDP descriptions and performs ordered core-model mutations.</summary>
public interface ISdpEditor
{
    /// <summary>Creates a deep copy of a session description and all nested mutable state.</summary>
    /// <param name="description">The description to copy.</param>
    /// <returns>An independent copy of the complete description.</returns>
    SdpSessionDescription CloneDescription(SdpSessionDescription description);

    /// <summary>Appends an attribute to session scope.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="attribute">The attribute instance to append.</param>
    void AddSessionAttribute(SdpSessionDescription description, SdpAttribute attribute);

    /// <summary>Appends an attribute to a media scope.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <param name="attribute">The attribute instance to append.</param>
    void AddMediaAttribute(SdpSessionDescription description, int mediaIndex, SdpAttribute attribute);

    /// <summary>Inserts an attribute at a session-scope index.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="attributeIndex">The zero-based insertion index.</param>
    /// <param name="attribute">The attribute instance to insert.</param>
    void InsertSessionAttribute(SdpSessionDescription description, int attributeIndex, SdpAttribute attribute);

    /// <summary>Inserts an attribute at a media-scope index.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <param name="attributeIndex">The zero-based insertion index.</param>
    /// <param name="attribute">The attribute instance to insert.</param>
    void InsertMediaAttribute(SdpSessionDescription description, int mediaIndex, int attributeIndex, SdpAttribute attribute);

    /// <summary>Replaces a session-scope attribute.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="attributeIndex">The zero-based attribute index.</param>
    /// <param name="attribute">The replacement attribute instance.</param>
    /// <returns>The attribute that was replaced.</returns>
    SdpAttribute ReplaceSessionAttribute(SdpSessionDescription description, int attributeIndex, SdpAttribute attribute);

    /// <summary>Replaces a media-scope attribute.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <param name="attributeIndex">The zero-based attribute index.</param>
    /// <param name="attribute">The replacement attribute instance.</param>
    /// <returns>The attribute that was replaced.</returns>
    SdpAttribute ReplaceMediaAttribute(SdpSessionDescription description, int mediaIndex, int attributeIndex, SdpAttribute attribute);

    /// <summary>Removes one session-scope attribute by index.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="attributeIndex">The zero-based attribute index.</param>
    /// <returns>The removed attribute.</returns>
    SdpAttribute RemoveSessionAttributeAt(SdpSessionDescription description, int attributeIndex);

    /// <summary>Removes one media-scope attribute by index.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <param name="attributeIndex">The zero-based attribute index.</param>
    /// <returns>The removed attribute.</returns>
    SdpAttribute RemoveMediaAttributeAt(SdpSessionDescription description, int mediaIndex, int attributeIndex);

    /// <summary>Removes all session-scope attributes with an ASCII case-insensitive name match.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="attributeName">The attribute name to remove.</param>
    /// <returns>The number of removed attributes.</returns>
    int RemoveSessionAttributesByName(SdpSessionDescription description, string attributeName);

    /// <summary>Removes all media-scope attributes with an ASCII case-insensitive name match.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <param name="attributeName">The attribute name to remove.</param>
    /// <returns>The number of removed attributes.</returns>
    int RemoveMediaAttributesByName(SdpSessionDescription description, int mediaIndex, string attributeName);

    /// <summary>Appends a media description.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="media">The media-description instance to append.</param>
    void AddMedia(SdpSessionDescription description, SdpMediaDescription media);

    /// <summary>Inserts a media description at an index.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based insertion index.</param>
    /// <param name="media">The media-description instance to insert.</param>
    void InsertMedia(SdpSessionDescription description, int mediaIndex, SdpMediaDescription media);

    /// <summary>Moves a media description to a different final index.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="sourceMediaIndex">The current zero-based media index.</param>
    /// <param name="destinationMediaIndex">The zero-based index occupied by the media after the move.</param>
    void MoveMedia(SdpSessionDescription description, int sourceMediaIndex, int destinationMediaIndex);

    /// <summary>Removes one media description by index.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <returns>The removed media-description instance.</returns>
    SdpMediaDescription RemoveMediaAt(SdpSessionDescription description, int mediaIndex);

    /// <summary>Rejects a media description by setting its port to zero.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    void RejectMedia(SdpSessionDescription description, int mediaIndex);

    /// <summary>Reactivates a rejected media description with an explicit nonzero port.</summary>
    /// <param name="description">The description to update.</param>
    /// <param name="mediaIndex">The zero-based media index.</param>
    /// <param name="port">The nonzero transport port.</param>
    void ReactivateMedia(SdpSessionDescription description, int mediaIndex, int port);

    /// <summary>Atomically increments the origin session version.</summary>
    /// <param name="description">The description to update.</param>
    void IncrementOriginVersion(SdpSessionDescription description);

    /// <summary>Normalizes a description to canonical field, token, whitespace, timing, attribute, format, and line-ending forms.</summary>
    /// <param name="description">The description to normalize in place.</param>
    void NormalizeDescription(SdpSessionDescription description);
}
