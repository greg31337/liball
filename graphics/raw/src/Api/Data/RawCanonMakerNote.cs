namespace raw;

/// <summary>Describes one exact Canon main-IFD maker-note entry.</summary>
public class RawCanonMakerNoteTag
{
    /// <summary>The Canon maker-note tag identifier.</summary>
    public ushort Id;
    /// <summary>The reviewed name, or a stable hexadecimal name for an unknown tag.</summary>
    public string Name;
    /// <summary>The earliest library-reviewed schema that can encode this tag.</summary>
    public RawCanonMakerNoteVersion MinimumVersion;
    /// <summary>The exact typed value.</summary>
    public RawTagValue Value;
    /// <summary>The entry byte position within the maker-note payload.</summary>
    public int EntryPosition;
    /// <summary>The value byte position within the maker-note payload.</summary>
    public int ValuePosition;
    /// <summary>Whether the tag has a reviewed encoding and its existing storage is safe to replace.</summary>
    public bool Writable;

    /// <summary>Creates a Canon maker-note tag model.</summary>
    /// <param name="id">The tag identifier.</param>
    /// <param name="name">The reviewed or stable fallback name.</param>
    /// <param name="minimumVersion">The earliest reviewed encoding schema.</param>
    /// <param name="value">The exact typed value.</param>
    /// <param name="entryPosition">The entry position.</param>
    /// <param name="valuePosition">The value position.</param>
    /// <param name="writable">Whether replacement is reviewed and storage-safe.</param>
    public RawCanonMakerNoteTag(ushort id, string name, RawCanonMakerNoteVersion minimumVersion, RawTagValue value, int entryPosition, int valuePosition, bool writable)
    {
        Id = id;
        Name = name == null ? string.Empty : name;
        MinimumVersion = minimumVersion;
        Value = value;
        EntryPosition = entryPosition;
        ValuePosition = valuePosition;
        Writable = writable;
    }

    /// <summary>Returns an independent copy of the exact encoded value bytes.</summary>
    /// <returns>The copied bytes.</returns>
    internal byte[] CopyBytes()
    {
        return Value == null ? System.Array.Empty<byte>() : Value.CopyBytes();
    }

    /// <summary>Decodes a reviewed fixed-width Canon ASCII value through its first null byte.</summary>
    /// <returns>The decoded seven-bit text.</returns>
    /// <exception cref="RawFormatException">The tag does not contain valid Canon ASCII storage.</exception>
    internal string GetText()
    {
        if (Value == null || Value.Type != RawTagType.Ascii || Value.RawBytes == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A Canon ASCII tag is required.", ValuePosition);
        }
        int length = 0;
        while (length < Value.RawBytes.Length && Value.RawBytes[length] != 0)
        {
            if (Value.RawBytes[length] > 127)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Canon text contains a non-ASCII byte.", ValuePosition + length);
            }
            length++;
        }
        if (length == Value.RawBytes.Length)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Canon text storage is not null terminated.", ValuePosition);
        }
        return System.Text.Encoding.ASCII.GetString(Value.RawBytes, 0, length);
    }
}

/// <summary>Requests one version-gated replacement of an existing reviewed Canon maker-note tag.</summary>
public class RawCanonMakerNoteEdit
{
    /// <summary>The Canon maker-note tag identifier.</summary>
    public ushort TagId;
    /// <summary>The selected library-reviewed encoding schema.</summary>
    public RawCanonMakerNoteVersion Version;
    /// <summary>The replacement typed value.</summary>
    public RawTagValue Value;

    /// <summary>Creates a Canon maker-note replacement.</summary>
    /// <param name="tagId">The existing tag identifier.</param>
    /// <param name="version">The reviewed encoding schema to apply.</param>
    /// <param name="value">The replacement typed value.</param>
    public RawCanonMakerNoteEdit(ushort tagId, RawCanonMakerNoteVersion version, RawTagValue value)
    {
        TagId = tagId;
        Version = version;
        Value = value;
    }
}
