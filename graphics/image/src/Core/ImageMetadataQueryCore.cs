using System;
using System.Collections.Generic;

namespace image;

/// <summary>Finds metadata tags by key or by a family, group, or directory path.</summary>
internal static class ImageMetadataQueryCore
{
    internal static List<MetadataTag> GetTags(ImageMetadataDocument? document)
    {
        List<MetadataTag> results = new List<MetadataTag>();
        if (document == null)
        {
            return results;
        }

        CollectTagsFromDirectories(results, document.Directories);
        if (document.Frames != null)
        {
            foreach (ImageMetadataFrame frame in document.Frames)
            {
                if (frame != null)
                {
                    CollectTagsFromDirectories(results, frame.Directories);
                }
            }
        }
        return results;
    }

    /// <summary>Finds every tag matching a case-insensitive key or path.</summary>
    /// <remarks>Searches top-level and frame directories recursively while preserving their source order.</remarks>
    /// <param name="document">The document to search.</param>
    /// <param name="keyOrPath">A tag key or path expression such as a family/group/tag path.</param>
    /// <returns>A new list containing all matches; the list is empty for null input or an empty query.</returns>
    public static List<MetadataTag> Find(ImageMetadataDocument? document, string? keyOrPath)
    {
        List<MetadataTag> results = new List<MetadataTag>();

        if (document == null || IsEmpty(keyOrPath))
        {
            return results;
        }

        SearchDirectories(results, document.Directories, keyOrPath);

        if (document.Frames != null)
        {
            foreach (ImageMetadataFrame frame in document.Frames)
            {
                if (frame != null)
                {
                    SearchDirectories(results, frame.Directories, keyOrPath);
                }
            }
        }

        return results;
    }

    /// <summary>Finds the first tag matching a case-insensitive key or path.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="keyOrPath">A tag key or path expression.</param>
    /// <returns>The first match in source order, or null when no tag matches.</returns>
    public static MetadataTag? FindFirst(ImageMetadataDocument? document, string? keyOrPath)
    {
        List<MetadataTag> results = Find(document, keyOrPath);
        if (results.Count == 0)
        {
            return null;
        }

        return results[0];
    }

    internal static List<MetadataTag> GetTagsForFrame(ImageMetadataDocument? document, int frameIndex)
    {
        List<MetadataTag> results = new List<MetadataTag>();
        if (document == null || frameIndex < 0)
        {
            return results;
        }

        CollectFrameTagsFromDirectories(results, document.Directories, frameIndex);
        if (document.Frames != null)
        {
            foreach (ImageMetadataFrame frame in document.Frames)
            {
                if (frame != null && frame.Index == frameIndex)
                {
                    CollectTagsFromDirectories(results, frame.Directories);
                }
            }
        }
        return results;
    }

    internal static List<MetadataTag> FindTagsForFrame(ImageMetadataDocument? document, int frameIndex, string? keyOrPath)
    {
        List<MetadataTag> results = new List<MetadataTag>();
        if (document == null || frameIndex < 0 || IsEmpty(keyOrPath))
        {
            return results;
        }

        SearchFrameDirectories(results, document.Directories, frameIndex, keyOrPath);
        if (document.Frames != null)
        {
            foreach (ImageMetadataFrame frame in document.Frames)
            {
                if (frame != null && frame.Index == frameIndex)
                {
                    SearchDirectories(results, frame.Directories, keyOrPath);
                }
            }
        }
        return results;
    }

    internal static List<MetadataDirectory> GetDirectories(ImageMetadataDocument? document)
    {
        List<MetadataDirectory> results = new List<MetadataDirectory>();
        if (document == null)
        {
            return results;
        }

        CollectDirectories(results, document.Directories);
        if (document.Frames != null)
        {
            foreach (ImageMetadataFrame frame in document.Frames)
            {
                if (frame != null)
                {
                    CollectDirectories(results, frame.Directories);
                }
            }
        }
        return results;
    }

    internal static List<MetadataDirectory> FindDirectoriesByFamily(ImageMetadataDocument? document, MetadataFamily family)
    {
        List<MetadataDirectory> results = new List<MetadataDirectory>();
        List<MetadataDirectory> directories = GetDirectories(document);
        foreach (MetadataDirectory directory in directories)
        {
            if (directory != null && directory.Family == family)
            {
                results.Add(directory);
            }
        }
        return results;
    }

    internal static List<MetadataDirectory> FindDirectoriesByPath(ImageMetadataDocument? document, string? path)
    {
        List<MetadataDirectory> results = new List<MetadataDirectory>();
        if (IsEmpty(path))
        {
            return results;
        }

        List<MetadataDirectory> directories = GetDirectories(document);
        foreach (MetadataDirectory directory in directories)
        {
            if (MatchesDirectory(directory, path))
            {
                results.Add(directory);
            }
        }
        return results;
    }

    internal static List<MetadataDirectory> GetDirectoriesForFrame(ImageMetadataDocument? document, int frameIndex)
    {
        List<MetadataDirectory> results = new List<MetadataDirectory>();
        if (document == null || frameIndex < 0)
        {
            return results;
        }

        CollectFrameDirectories(results, document.Directories, frameIndex);
        if (document.Frames != null)
        {
            foreach (ImageMetadataFrame frame in document.Frames)
            {
                if (frame != null && frame.Index == frameIndex)
                {
                    CollectDirectories(results, frame.Directories);
                }
            }
        }
        return results;
    }

    internal static List<MetadataProfile> GetProfiles(ImageMetadataDocument? document)
    {
        List<MetadataProfile> results = new List<MetadataProfile>();
        if (document == null || document.Profiles == null)
        {
            return results;
        }
        foreach (MetadataProfile profile in document.Profiles)
        {
            if (profile != null)
            {
                results.Add(profile);
            }
        }
        return results;
    }

    internal static List<MetadataProfile> FindProfilesByFamily(ImageMetadataDocument? document, MetadataFamily family)
    {
        List<MetadataProfile> results = new List<MetadataProfile>();
        List<MetadataProfile> profiles = GetProfiles(document);
        foreach (MetadataProfile profile in profiles)
        {
            if (profile.Family == family)
            {
                results.Add(profile);
            }
        }
        return results;
    }

    internal static List<MetadataProfile> FindProfilesByKind(ImageMetadataDocument? document, MetadataProfileKind kind)
    {
        List<MetadataProfile> results = new List<MetadataProfile>();
        List<MetadataProfile> profiles = GetProfiles(document);
        foreach (MetadataProfile profile in profiles)
        {
            if (profile.Kind == kind)
            {
                results.Add(profile);
            }
        }
        return results;
    }

    internal static List<MetadataProfile> FindProfilesByKey(ImageMetadataDocument? document, string? key)
    {
        List<MetadataProfile> results = new List<MetadataProfile>();
        if (IsEmpty(key))
        {
            return results;
        }
        List<MetadataProfile> profiles = GetProfiles(document);
        foreach (MetadataProfile profile in profiles)
        {
            if (SameText(profile.Key, key))
            {
                results.Add(profile);
            }
        }
        return results;
    }

    internal static List<MetadataProfile> FindProfilesForFrame(ImageMetadataDocument? document, int frameIndex)
    {
        List<MetadataProfile> results = new List<MetadataProfile>();
        if (frameIndex < 0)
        {
            return results;
        }
        List<MetadataProfile> profiles = GetProfiles(document);
        foreach (MetadataProfile profile in profiles)
        {
            if (profile.SourceFrameIndex == frameIndex)
            {
                results.Add(profile);
            }
        }
        return results;
    }

    internal static List<MetadataProfile> FindProfilesByRole(ImageMetadataDocument? document, ImageMetadataRole role)
    {
        List<MetadataProfile> results = new List<MetadataProfile>();
        List<MetadataProfile> profiles = GetProfiles(document);
        foreach (MetadataProfile profile in profiles)
        {
            if (profile.Role == role)
            {
                results.Add(profile);
            }
        }
        return results;
    }

    internal static List<MetadataProfile> FindRoleProfilesForFrame(ImageMetadataDocument? document, int frameIndex, ImageMetadataRole role)
    {
        List<MetadataProfile> results = new List<MetadataProfile>();
        if (frameIndex < 0)
        {
            return results;
        }
        List<MetadataProfile> profiles = GetProfiles(document);
        foreach (MetadataProfile profile in profiles)
        {
            if (profile.SourceFrameIndex == frameIndex && profile.Role == role)
            {
                results.Add(profile);
            }
        }
        return results;
    }

    internal static List<MetadataRawBlock> GetRawBlocks(ImageMetadataDocument? document)
    {
        List<MetadataRawBlock> results = new List<MetadataRawBlock>();
        if (document == null || document.RawBlocks == null)
        {
            return results;
        }
        foreach (MetadataRawBlock rawBlock in document.RawBlocks)
        {
            if (rawBlock != null)
            {
                results.Add(rawBlock);
            }
        }
        return results;
    }

    internal static List<MetadataRawBlock> FindRawBlocksByFamily(ImageMetadataDocument? document, MetadataFamily family)
    {
        List<MetadataRawBlock> results = new List<MetadataRawBlock>();
        List<MetadataRawBlock> rawBlocks = GetRawBlocks(document);
        foreach (MetadataRawBlock rawBlock in rawBlocks)
        {
            if (rawBlock.Family == family)
            {
                results.Add(rawBlock);
            }
        }
        return results;
    }

    internal static List<MetadataRawBlock> FindRawBlocksByType(ImageMetadataDocument? document, string? blockType)
    {
        List<MetadataRawBlock> results = new List<MetadataRawBlock>();
        if (IsEmpty(blockType))
        {
            return results;
        }
        List<MetadataRawBlock> rawBlocks = GetRawBlocks(document);
        foreach (MetadataRawBlock rawBlock in rawBlocks)
        {
            if (string.Equals(rawBlock.BlockType, blockType, StringComparison.Ordinal))
            {
                results.Add(rawBlock);
            }
        }
        return results;
    }

    internal static List<ImageMetadataFrame> FindFramesByRole(ImageMetadataDocument? document, ImageMetadataRole role)
    {
        List<ImageMetadataFrame> results = new List<ImageMetadataFrame>();
        if (document == null || document.Frames == null)
        {
            return results;
        }
        foreach (ImageMetadataFrame frame in document.Frames)
        {
            if (frame != null && frame.Role == role)
            {
                results.Add(frame);
            }
        }
        return results;
    }

    internal static List<ImageMetadataFrame> FindRelatedFrames(ImageMetadataDocument? document, int parentFrameIndex, ImageMetadataRole role)
    {
        List<ImageMetadataFrame> results = new List<ImageMetadataFrame>();
        if (document == null || document.Frames == null || parentFrameIndex < 0)
        {
            return results;
        }
        foreach (ImageMetadataFrame frame in document.Frames)
        {
            if (frame != null && frame.ParentFrameIndex == parentFrameIndex && frame.Role == role)
            {
                results.Add(frame);
            }
        }
        return results;
    }

    internal static void CollectTagsFromDirectories(List<MetadataTag> results, List<MetadataDirectory> directories)
    {
        if (results == null || directories == null)
        {
            return;
        }
        foreach (MetadataDirectory directory in directories)
        {
            CollectTagsFromDirectory(results, directory);
        }
    }

    internal static void CollectTagsFromDirectory(List<MetadataTag> results, MetadataDirectory directory)
    {
        if (results == null || directory == null)
        {
            return;
        }
        if (directory.Tags != null)
        {
            foreach (MetadataTag tag in directory.Tags)
            {
                if (tag != null)
                {
                    results.Add(tag);
                }
            }
        }
        if (directory.Children != null)
        {
            foreach (MetadataDirectory child in directory.Children)
            {
                CollectTagsFromDirectory(results, child);
            }
        }
    }

    internal static void CollectFrameTagsFromDirectories(List<MetadataTag> results, List<MetadataDirectory> directories, int frameIndex)
    {
        if (results == null || directories == null)
        {
            return;
        }
        foreach (MetadataDirectory directory in directories)
        {
            CollectFrameTagsFromDirectory(results, directory, frameIndex);
        }
    }

    internal static void CollectFrameTagsFromDirectory(List<MetadataTag> results, MetadataDirectory directory, int frameIndex)
    {
        if (results == null || directory == null)
        {
            return;
        }
        if (directory.FrameIndex == frameIndex && directory.Tags != null)
        {
            foreach (MetadataTag tag in directory.Tags)
            {
                if (tag != null)
                {
                    results.Add(tag);
                }
            }
        }
        if (directory.Children != null)
        {
            foreach (MetadataDirectory child in directory.Children)
            {
                CollectFrameTagsFromDirectory(results, child, frameIndex);
            }
        }
    }

    internal static void SearchFrameDirectories(List<MetadataTag> results, List<MetadataDirectory> directories, int frameIndex, string? keyOrPath)
    {
        if (results == null || directories == null)
        {
            return;
        }
        foreach (MetadataDirectory directory in directories)
        {
            SearchFrameDirectory(results, directory, frameIndex, keyOrPath);
        }
    }

    internal static void SearchFrameDirectory(List<MetadataTag> results, MetadataDirectory directory, int frameIndex, string? keyOrPath)
    {
        if (results == null || directory == null)
        {
            return;
        }
        if (directory.FrameIndex == frameIndex && directory.Tags != null)
        {
            foreach (MetadataTag tag in directory.Tags)
            {
                if (MatchesTag(directory, tag, keyOrPath))
                {
                    results.Add(tag);
                }
            }
        }
        if (directory.Children != null)
        {
            foreach (MetadataDirectory child in directory.Children)
            {
                SearchFrameDirectory(results, child, frameIndex, keyOrPath);
            }
        }
    }

    internal static void CollectDirectories(List<MetadataDirectory> results, List<MetadataDirectory> directories)
    {
        if (results == null || directories == null)
        {
            return;
        }
        foreach (MetadataDirectory directory in directories)
        {
            CollectDirectory(results, directory);
        }
    }

    internal static void CollectDirectory(List<MetadataDirectory> results, MetadataDirectory directory)
    {
        if (results == null || directory == null)
        {
            return;
        }
        results.Add(directory);
        if (directory.Children != null)
        {
            foreach (MetadataDirectory child in directory.Children)
            {
                CollectDirectory(results, child);
            }
        }
    }

    internal static void CollectFrameDirectories(List<MetadataDirectory> results, List<MetadataDirectory> directories, int frameIndex)
    {
        if (results == null || directories == null)
        {
            return;
        }
        foreach (MetadataDirectory directory in directories)
        {
            CollectFrameDirectory(results, directory, frameIndex);
        }
    }

    internal static void CollectFrameDirectory(List<MetadataDirectory> results, MetadataDirectory directory, int frameIndex)
    {
        if (results == null || directory == null)
        {
            return;
        }
        if (directory.FrameIndex == frameIndex)
        {
            results.Add(directory);
        }
        if (directory.Children != null)
        {
            foreach (MetadataDirectory child in directory.Children)
            {
                CollectFrameDirectory(results, child, frameIndex);
            }
        }
    }

    internal static bool MatchesDirectory(MetadataDirectory directory, string? path)
    {
        if (directory == null || IsEmpty(path))
        {
            return false;
        }
        if (SameText(directory.Path, path))
        {
            return true;
        }
        if (!IsEmpty(directory.Group))
        {
            string family = directory.Family.ToString();
            if (SameText(family + "/" + directory.Group, path)) return true;
            if (SameText(family + "." + directory.Group, path)) return true;
        }
        return false;
    }

    internal static void SearchDirectories(List<MetadataTag> results, List<MetadataDirectory> directories, string? keyOrPath)
    {
        if (results == null || directories == null)
        {
            return;
        }

        foreach (MetadataDirectory directory in directories)
        {
            SearchDirectory(results, directory, keyOrPath);
        }
    }

    internal static void SearchDirectory(List<MetadataTag> results, MetadataDirectory directory, string? keyOrPath)
    {
        if (results == null || directory == null)
        {
            return;
        }

        if (directory.Tags != null)
        {
            foreach (MetadataTag tag in directory.Tags)
            {
                if (MatchesTag(directory, tag, keyOrPath))
                {
                    results.Add(tag);
                }
            }
        }

        if (directory.Children != null)
        {
            foreach (MetadataDirectory child in directory.Children)
            {
                SearchDirectory(results, child, keyOrPath);
            }
        }
    }

    internal static bool MatchesTag(MetadataDirectory directory, MetadataTag tag, string? keyOrPath)
    {
        if (tag == null || IsEmpty(keyOrPath))
        {
            return false;
        }

        if (SameText(tag.Key, keyOrPath))
        {
            return true;
        }

        MetadataFamily family = tag.Family;
        string group = tag.Group;
        if (directory != null)
        {
            if (family == MetadataFamily.Unknown)
            {
                family = directory.Family;
            }

            if (IsEmpty(group))
            {
                group = directory.Group;
            }
        }

        if (MatchesFamilyGroupTag(family, group, tag.Name, keyOrPath))
        {
            return true;
        }

        if (MatchesFamilyGroupTag(family, group, tag.Key, keyOrPath))
        {
            return true;
        }

        if (directory != null && !IsEmpty(directory.Path))
        {
            if (MatchesDirectoryPathTag(directory.Path, tag.Name, keyOrPath))
            {
                return true;
            }

            if (MatchesDirectoryPathTag(directory.Path, tag.Key, keyOrPath))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool MatchesFamilyGroupTag(MetadataFamily family, string group, string tagName, string? keyOrPath)
    {
        if (IsEmpty(tagName))
        {
            return false;
        }

        string familyText = family.ToString();

        if (!IsEmpty(group))
        {
            if (SameText(familyText + "." + group + "." + tagName, keyOrPath))
            {
                return true;
            }

            if (SameText(familyText + "/" + group + "/" + tagName, keyOrPath))
            {
                return true;
            }
        }

        if (SameText(familyText + "." + tagName, keyOrPath))
        {
            return true;
        }

        if (SameText(familyText + "/" + tagName, keyOrPath))
        {
            return true;
        }

        return false;
    }

    internal static bool MatchesDirectoryPathTag(string directoryPath, string tagName, string? keyOrPath)
    {
        if (IsEmpty(directoryPath) || IsEmpty(tagName))
        {
            return false;
        }

        string trimmedPath = TrimSlashes(directoryPath);
        if (IsEmpty(trimmedPath))
        {
            return false;
        }

        if (SameText(trimmedPath + "/" + tagName, keyOrPath))
        {
            return true;
        }

        if (SameText(trimmedPath + "." + tagName, keyOrPath))
        {
            return true;
        }

        return false;
    }

    internal static string TrimSlashes(string? text)
    {
        if (text == null)
        {
            return "";
        }

        int start = 0;
        int end = text.Length;

        while (start < end && (text[start] == '/' || text[start] == '\\'))
        {
            start = start + 1;
        }

        while (end > start && (text[end - 1] == '/' || text[end - 1] == '\\'))
        {
            end = end - 1;
        }

        return text.Substring(start, end - start);
    }

    internal static bool SameText(string left, string? right)
    {
        return string.Equals(TrimSlashes(left), TrimSlashes(right), StringComparison.Ordinal);
    }

    internal static bool IsEmpty(string? text)
    {
        return text == null || text.Length == 0;
    }
}
