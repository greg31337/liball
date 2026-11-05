using System.Collections.Generic;

namespace avif;

/// <summary>
/// Describes one parsed ISO base media file format box and its children.
/// </summary>
public class AvifBox : IAvifBox
{
    string type;
    string path;
    long offset;
    long size;
    int headerSize;
    bool largeSize;
    bool extendsToEnd;
    bool uuid;
    bool unknown;
    bool fullBox;
    int version;
    int flags;
    byte[] userType;
    List<AvifBox> children;

    internal AvifBox(
        string type,
        string path,
        long offset,
        long size,
        int headerSize,
        bool largeSize,
        bool extendsToEnd,
        bool uuid,
        bool unknown,
        bool fullBox,
        int version,
        int flags,
        byte[] userType,
        List<AvifBox> children)
    {
        this.type = type;
        this.path = path;
        this.offset = offset;
        this.size = size;
        this.headerSize = headerSize;
        this.largeSize = largeSize;
        this.extendsToEnd = extendsToEnd;
        this.uuid = uuid;
        this.unknown = unknown;
        this.fullBox = fullBox;
        this.version = version;
        this.flags = flags;
        this.userType = (byte[])userType.Clone();
        this.children = new List<AvifBox>(children);
    }

    /// <summary>Returns the four-character box type.</summary>
    /// <returns>The parsed box type.</returns>
    public string GetTypeName()
    {
        return type;
    }

    /// <summary>Returns the slash-separated path to this box.</summary>
    /// <returns>The box path.</returns>
    public string GetPath()
    {
        return path;
    }

    /// <summary>Returns the box header byte offset.</summary>
    /// <returns>The zero-based byte offset.</returns>
    public long GetOffset()
    {
        return offset;
    }

    /// <summary>Returns the complete declared box size.</summary>
    /// <returns>The box size in bytes.</returns>
    public long GetSize()
    {
        return size;
    }

    /// <summary>Returns the size of the box header.</summary>
    /// <returns>The header size in bytes.</returns>
    public int GetHeaderSize()
    {
        return headerSize;
    }

    /// <summary>Reports whether the box uses a 64-bit large-size header.</summary>
    /// <returns><c>true</c> when a large-size header is present.</returns>
    public bool UsesLargeSize()
    {
        return largeSize;
    }

    /// <summary>Reports whether a zero size extends the box to the containing range end.</summary>
    /// <returns><c>true</c> when the box extends to the end.</returns>
    public bool ExtendsToEnd()
    {
        return extendsToEnd;
    }

    /// <summary>Reports whether this is a UUID box.</summary>
    /// <returns><c>true</c> for a UUID box.</returns>
    public bool IsUuid()
    {
        return uuid;
    }

    /// <summary>Reports whether the parser has no specialized box semantics.</summary>
    /// <returns><c>true</c> when the box is opaque to the parser.</returns>
    public bool IsUnknown()
    {
        return unknown;
    }

    /// <summary>Reports whether version and flags were parsed from this box.</summary>
    /// <returns><c>true</c> when FullBox fields are available.</returns>
    public bool IsFullBox()
    {
        return fullBox;
    }

    /// <summary>Returns the FullBox version, or zero when unavailable.</summary>
    /// <returns>The FullBox version.</returns>
    public int GetVersion()
    {
        return version;
    }

    /// <summary>Returns the FullBox flags, or zero when unavailable.</summary>
    /// <returns>The 24-bit FullBox flags.</returns>
    public int GetFlags()
    {
        return flags;
    }

    /// <summary>Returns a copy of the UUID user type.</summary>
    /// <returns>The sixteen-byte user type, or an empty array.</returns>
    public byte[] GetUserType()
    {
        return (byte[])userType.Clone();
    }

    /// <summary>Returns the number of immediate child boxes.</summary>
    /// <returns>The child count.</returns>
    public int GetChildCount()
    {
        return children.Count;
    }

    /// <summary>Returns one immediate child box.</summary>
    /// <param name="index">The zero-based child index.</param>
    /// <returns>The child, or null when the index is outside the collection.</returns>
    public AvifBox? GetChild(int index)
    {
        if (index < 0 || index >= children.Count)
        {
            return null;
        }

        return children[index];
    }

    internal void AddChild(AvifBox child)
    {
        children.Add(child);
    }
}
