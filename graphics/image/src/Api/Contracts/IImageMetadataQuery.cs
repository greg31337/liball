using System.Collections.Generic;

namespace image;

/// <summary>Defines metadata tag lookup operations.</summary>
public interface IImageMetadataQuery
{
    /// <summary>Gets all tags from document and frame directories.</summary>
    /// <param name="document">The document to enumerate, or null.</param>
    /// <returns>The tags in document and frame source order.</returns>
    List<MetadataTag> GetTags(ImageMetadataDocument? document);

    /// <summary>Finds all tags matching a stable key or directory path.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="keyOrPath">The key or path to match, or null.</param>
    /// <returns>The matching tags in document order.</returns>
    List<MetadataTag> Find(ImageMetadataDocument? document, string? keyOrPath);

    /// <summary>Finds the first tag matching a stable key or directory path.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="keyOrPath">The key or path to match, or null.</param>
    /// <returns>The first matching tag, or null when no tag matches.</returns>
    MetadataTag? FindFirst(ImageMetadataDocument? document, string? keyOrPath);

    /// <summary>Gets all tags scoped to one frame.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>The frame-scoped tags in source order.</returns>
    List<MetadataTag> GetTagsForFrame(ImageMetadataDocument? document, int frameIndex);

    /// <summary>Finds frame-scoped tags matching a stable key or directory path.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <param name="keyOrPath">The key or path to match, or null.</param>
    /// <returns>The matching frame-scoped tags in source order.</returns>
    List<MetadataTag> FindTagsForFrame(ImageMetadataDocument? document, int frameIndex, string? keyOrPath);

    /// <summary>Gets all document-level and frame-level directories recursively.</summary>
    /// <param name="document">The document to enumerate, or null.</param>
    /// <returns>The directories in depth-first source order.</returns>
    List<MetadataDirectory> GetDirectories(ImageMetadataDocument? document);

    /// <summary>Finds directories belonging to a metadata family.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="family">The family to match.</param>
    /// <returns>The matching directories in depth-first source order.</returns>
    List<MetadataDirectory> FindDirectoriesByFamily(ImageMetadataDocument? document, MetadataFamily family);

    /// <summary>Finds directories matching their source hierarchy path or family and group path.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="path">The directory path to match, or null.</param>
    /// <returns>The matching directories in depth-first source order.</returns>
    List<MetadataDirectory> FindDirectoriesByPath(ImageMetadataDocument? document, string? path);

    /// <summary>Gets directories scoped to one frame recursively.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>The frame-scoped directories in depth-first source order.</returns>
    List<MetadataDirectory> GetDirectoriesForFrame(ImageMetadataDocument? document, int frameIndex);

    /// <summary>Gets all preserved metadata profiles and image previews.</summary>
    /// <param name="document">The document to enumerate, or null.</param>
    /// <returns>The profiles in source order.</returns>
    List<MetadataProfile> GetProfiles(ImageMetadataDocument? document);

    /// <summary>Finds profiles belonging to a metadata family.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="family">The family to match.</param>
    /// <returns>The matching profiles in source order.</returns>
    List<MetadataProfile> FindProfilesByFamily(ImageMetadataDocument? document, MetadataFamily family);

    /// <summary>Finds profiles of a standard or purpose kind.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="kind">The profile kind to match.</param>
    /// <returns>The matching profiles in source order.</returns>
    List<MetadataProfile> FindProfilesByKind(ImageMetadataDocument? document, MetadataProfileKind kind);

    /// <summary>Finds profiles matching a stable key.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="key">The stable key to match, or null.</param>
    /// <returns>The matching profiles in source order.</returns>
    List<MetadataProfile> FindProfilesByKey(ImageMetadataDocument? document, string? key);

    /// <summary>Finds profiles represented by or associated with one frame.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="frameIndex">The zero-based source frame index.</param>
    /// <returns>The matching profiles in source order.</returns>
    List<MetadataProfile> FindProfilesForFrame(ImageMetadataDocument? document, int frameIndex);

    /// <summary>Finds image-bearing profiles assigned a thumbnail, preview, or other role.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="role">The image role to match.</param>
    /// <returns>The matching profiles in source order.</returns>
    List<MetadataProfile> FindProfilesByRole(ImageMetadataDocument? document, ImageMetadataRole role);

    /// <summary>Finds image-bearing profiles having both a source frame and role.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="frameIndex">The zero-based source frame index.</param>
    /// <param name="role">The image role to match.</param>
    /// <returns>The matching profiles in source order.</returns>
    List<MetadataProfile> FindRoleProfilesForFrame(ImageMetadataDocument? document, int frameIndex, ImageMetadataRole role);

    /// <summary>Gets all recognized raw metadata ranges.</summary>
    /// <param name="document">The document to enumerate, or null.</param>
    /// <returns>The raw blocks in source order.</returns>
    List<MetadataRawBlock> GetRawBlocks(ImageMetadataDocument? document);

    /// <summary>Finds raw metadata blocks belonging to a family.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="family">The family to match.</param>
    /// <returns>The matching raw blocks in source order.</returns>
    List<MetadataRawBlock> FindRawBlocksByFamily(ImageMetadataDocument? document, MetadataFamily family);

    /// <summary>Finds raw metadata blocks matching a container block, marker, or chunk type.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="blockType">The block type to match, or null.</param>
    /// <returns>The matching raw blocks in source order.</returns>
    List<MetadataRawBlock> FindRawBlocksByType(ImageMetadataDocument? document, string? blockType);

    /// <summary>Finds frames assigned a thumbnail, preview, or other role.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="role">The frame role to match.</param>
    /// <returns>The matching frames in source order.</returns>
    List<ImageMetadataFrame> FindFramesByRole(ImageMetadataDocument? document, ImageMetadataRole role);

    /// <summary>Finds child frames related to a parent frame and assigned a specific role.</summary>
    /// <param name="document">The document to search, or null.</param>
    /// <param name="parentFrameIndex">The zero-based parent frame index.</param>
    /// <param name="role">The child frame role to match.</param>
    /// <returns>The matching related frames in source order.</returns>
    List<ImageMetadataFrame> FindRelatedFrames(ImageMetadataDocument? document, int parentFrameIndex, ImageMetadataRole role);
}
