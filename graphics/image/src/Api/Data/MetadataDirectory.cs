using System.Collections.Generic;

namespace image;

/// <summary>Groups related metadata tags and nested directories from one standard or container structure.</summary>
public class MetadataDirectory
{
    /// <summary>The metadata standard or source family represented by this directory.</summary>
    public MetadataFamily Family;
    /// <summary>The source-specific group name, such as an IFD or container block name.</summary>
    public string Group;
    /// <summary>The human-readable directory name.</summary>
    public string Name;
    /// <summary>The source hierarchy path used by path-based queries.</summary>
    public string Path;
    /// <summary>The zero-based owning frame index, or negative one for document-level metadata.</summary>
    public int FrameIndex;
    /// <summary>The zero-based source byte offset, or negative one when unavailable.</summary>
    public long Offset;
    /// <summary>The source byte length of the directory when known.</summary>
    public long Length;
    /// <summary>The zero-based occurrence among repeated directories of the same kind.</summary>
    public int RepeatIndex;
    /// <summary>The tags in their original source order.</summary>
    public List<MetadataTag> Tags;
    /// <summary>The nested directories in their original source order.</summary>
    public List<MetadataDirectory> Children;

    /// <summary>Creates an empty directory with initialized tag and child collections.</summary>
    public MetadataDirectory()
    {
        Family = MetadataFamily.Unknown;
        Group = "";
        Name = "";
        Path = "";
        FrameIndex = -1;
        Offset = -1;
        Length = 0;
        RepeatIndex = 0;
        Tags = new List<MetadataTag>();
        Children = new List<MetadataDirectory>();
    }

    /// <summary>Creates an empty named directory for a metadata family and group.</summary>
    /// <param name="family">The metadata standard or source family.</param>
    /// <param name="group">The source-specific group name.</param>
    /// <param name="name">The human-readable directory name.</param>
    /// <returns>A directory initialized with the supplied identity.</returns>
    internal static MetadataDirectory Create(MetadataFamily family, string group, string name)
    {
        MetadataDirectory directory = new MetadataDirectory();
        directory.Family = family;
        directory.Group = group;
        directory.Name = name;
        return directory;
    }

    internal MetadataTag AddTag(MetadataTag? tag)
    {
        MetadataTag? actual = tag;
        if (actual == null)
        {
            actual = new MetadataTag();
        }

        if (actual.Group.Length == 0)
        {
            actual.Group = Group;
        }

        if (actual.Family == MetadataFamily.Unknown)
        {
            actual.Family = Family;
        }

        actual.RepeatIndex = Tags.Count;
        Tags.Add(actual);
        return actual;
    }

    internal MetadataTag AddTagWithValue(string key, string name, MetadataValue value)
    {
        MetadataValue actualValue = value;
        if (actualValue == null)
        {
            actualValue = new MetadataValue();
        }

        MetadataTag tag = MetadataTag.Create(key, name, Family, actualValue);
        tag.Group = Group;
        return AddTag(tag);
    }

    internal MetadataDirectory AddChild(MetadataDirectory? directory)
    {
        MetadataDirectory? actual = directory;
        if (actual == null)
        {
            actual = new MetadataDirectory();
        }

        actual.FrameIndex = FrameIndex;
        Children.Add(actual);
        return actual;
    }

    internal MetadataDirectory AddChildWithName(MetadataFamily family, string group, string name)
    {
        MetadataDirectory directory = Create(family, group, name);
        return AddChild(directory);
    }
}
