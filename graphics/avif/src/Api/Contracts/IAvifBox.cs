using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifBox"/>.</summary>
public interface IAvifBox
{
    /// <summary>Returns the four-character box type.</summary>
    /// <returns>The parsed box type.</returns>
    string GetTypeName();

    /// <summary>Returns the slash-separated path to this box.</summary>
    /// <returns>The box path.</returns>
    string GetPath();

    /// <summary>Returns the box header byte offset.</summary>
    /// <returns>The zero-based byte offset.</returns>
    long GetOffset();

    /// <summary>Returns the complete declared box size.</summary>
    /// <returns>The box size in bytes.</returns>
    long GetSize();

    /// <summary>Returns the size of the box header.</summary>
    /// <returns>The header size in bytes.</returns>
    int GetHeaderSize();

    /// <summary>Reports whether the box uses a 64-bit large-size header.</summary>
    /// <returns><c>true</c> when a large-size header is present.</returns>
    bool UsesLargeSize();

    /// <summary>Reports whether a zero size extends the box to the containing range end.</summary>
    /// <returns><c>true</c> when the box extends to the end.</returns>
    bool ExtendsToEnd();

    /// <summary>Reports whether this is a UUID box.</summary>
    /// <returns><c>true</c> for a UUID box.</returns>
    bool IsUuid();

    /// <summary>Reports whether the parser has no specialized box semantics.</summary>
    /// <returns><c>true</c> when the box is opaque to the parser.</returns>
    bool IsUnknown();

    /// <summary>Reports whether version and flags were parsed from this box.</summary>
    /// <returns><c>true</c> when FullBox fields are available.</returns>
    bool IsFullBox();

    /// <summary>Returns the FullBox version, or zero when unavailable.</summary>
    /// <returns>The FullBox version.</returns>
    int GetVersion();

    /// <summary>Returns the FullBox flags, or zero when unavailable.</summary>
    /// <returns>The 24-bit FullBox flags.</returns>
    int GetFlags();

    /// <summary>Returns a copy of the UUID user type.</summary>
    /// <returns>The sixteen-byte user type, or an empty array.</returns>
    byte[] GetUserType();

    /// <summary>Returns the number of immediate child boxes.</summary>
    /// <returns>The child count.</returns>
    int GetChildCount();

    /// <summary>Returns one immediate child box.</summary>
    /// <param name="index">The zero-based child index.</param>
    /// <returns>The child, or null when the index is outside the collection.</returns>
    AvifBox? GetChild(int index);
}
