using System;

namespace raw;

/// <summary>Describes one exact Nikon type-3 main-IFD maker-note entry.</summary>
public class RawNikonMakerNoteTag
{
    /// <summary>The Nikon maker-note tag identifier.</summary>
    public ushort Id;
    /// <summary>The reviewed name, or a stable hexadecimal name for an unknown tag.</summary>
    public string Name;
    /// <summary>The earliest library-reviewed schema that can encode this tag.</summary>
    public RawNikonMakerNoteVersion MinimumVersion;
    /// <summary>The exact typed value.</summary>
    public RawTagValue Value;
    /// <summary>The entry byte position within the maker-note payload.</summary>
    public int EntryPosition;
    /// <summary>The value byte position within the maker-note payload.</summary>
    public int ValuePosition;
    /// <summary>Whether the tag has a reviewed encoding and its existing storage is safe to replace.</summary>
    public bool Writable;
    /// <summary>Whether the tag is a key, encrypted payload, Capture/Edit block, or other data that must remain byte-exact.</summary>
    public bool Protected;

    /// <summary>Creates a Nikon maker-note tag model.</summary>
    /// <param name="id">The tag identifier.</param>
    /// <param name="name">The reviewed or stable fallback name.</param>
    /// <param name="minimumVersion">The earliest reviewed encoding schema.</param>
    /// <param name="value">The exact typed value.</param>
    /// <param name="entryPosition">The entry position.</param>
    /// <param name="valuePosition">The value position.</param>
    /// <param name="writable">Whether replacement is reviewed and storage-safe.</param>
    /// <param name="protectedValue">Whether the exact bytes are protected from encoding.</param>
    public RawNikonMakerNoteTag(ushort id, string name, RawNikonMakerNoteVersion minimumVersion, RawTagValue value, int entryPosition, int valuePosition, bool writable, bool protectedValue)
    {
        Id = id;
        Name = name == null ? string.Empty : name;
        MinimumVersion = minimumVersion;
        Value = value;
        EntryPosition = entryPosition;
        ValuePosition = valuePosition;
        Writable = writable;
        Protected = protectedValue;
    }

    /// <summary>Returns an independent copy of the exact encoded value bytes.</summary>
    /// <returns>The copied bytes.</returns>
    internal byte[] CopyBytes()
    {
        return Value == null ? System.Array.Empty<byte>() : Value.CopyBytes();
    }

    /// <summary>Decodes a reviewed fixed-width Nikon ASCII value through its first null byte.</summary>
    /// <returns>The decoded seven-bit text.</returns>
    /// <exception cref="RawFormatException">The tag does not contain valid Nikon ASCII storage.</exception>
    internal string GetText()
    {
        if (Value == null || Value.Type != RawTagType.Ascii || Value.RawBytes == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A Nikon ASCII tag is required.", ValuePosition);
        }
        int length = 0;
        while (length < Value.RawBytes.Length && Value.RawBytes[length] != 0)
        {
            if (Value.RawBytes[length] > 127)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Nikon text contains a non-ASCII byte.", ValuePosition + length);
            }
            length++;
        }
        return System.Text.Encoding.ASCII.GetString(Value.RawBytes, 0, length);
    }
}

/// <summary>Requests one version-gated replacement of an existing reviewed Nikon maker-note tag.</summary>
public class RawNikonMakerNoteEdit
{
    /// <summary>The Nikon maker-note tag identifier.</summary>
    public ushort TagId;
    /// <summary>The selected library-reviewed encoding schema.</summary>
    public RawNikonMakerNoteVersion Version;
    /// <summary>The replacement typed value.</summary>
    public RawTagValue Value;

    /// <summary>Creates a Nikon maker-note replacement.</summary>
    /// <param name="tagId">The existing tag identifier.</param>
    /// <param name="version">The reviewed encoding schema to apply.</param>
    /// <param name="value">The replacement typed value.</param>
    public RawNikonMakerNoteEdit(ushort tagId, RawNikonMakerNoteVersion version, RawTagValue value)
    {
        TagId = tagId;
        Version = version;
        Value = value;
    }
}

/// <summary>Retains a Nikon color-balance block, its exact original bytes, and any independently decoded working copy.</summary>
public class RawNikonColorBalance
{
    /// <summary>The four-character color-balance data version.</summary>
    public string Version;
    /// <summary>Whether the reviewed version uses Nikon's serial/shutter XOR stream.</summary>
    public bool Encrypted;
    /// <summary>Whether this version and its required numeric key tags were decoded.</summary>
    public bool DecryptionSupported;
    /// <summary>The exact original tag bytes, including encrypted bytes.</summary>
    public byte[] OriginalBytes;
    /// <summary>An independent decoded working copy, or an empty array when unsupported.</summary>
    public byte[] DecodedBytes;
    /// <summary>The four channel letters corresponding to the reported levels.</summary>
    public string ChannelOrder;
    /// <summary>The four decoded unsigned levels in stored channel order, or an empty array.</summary>
    public ushort[] Levels;
    /// <summary>The first encrypted byte position within the tag value, or <c>-1</c>.</summary>
    public int DecryptStart;
    /// <summary>The first color level position within the tag value, or <c>-1</c>.</summary>
    public int LevelsPosition;

    /// <summary>Creates a retained Nikon color-balance model.</summary>
    /// <param name="version">The four-character version.</param>
    /// <param name="encrypted">Whether the version is encrypted.</param>
    /// <param name="decryptionSupported">Whether decoding succeeded.</param>
    /// <param name="originalBytes">The exact original value.</param>
    /// <param name="decodedBytes">The decoded working copy.</param>
    /// <param name="channelOrder">The level channel order.</param>
    /// <param name="levels">The decoded levels.</param>
    /// <param name="decryptStart">The first encrypted byte.</param>
    /// <param name="levelsPosition">The first level byte.</param>
    public RawNikonColorBalance(string version, bool encrypted, bool decryptionSupported, byte[] originalBytes, byte[]? decodedBytes, string channelOrder, ushort[]? levels, int decryptStart, int levelsPosition)
    {
        Version = version == null ? string.Empty : version;
        Encrypted = encrypted;
        DecryptionSupported = decryptionSupported;
        OriginalBytes = originalBytes == null ? System.Array.Empty<byte>() : RawTextValue.Copy(originalBytes);
        DecodedBytes = decodedBytes == null ? System.Array.Empty<byte>() : RawTextValue.Copy(decodedBytes);
        ChannelOrder = channelOrder == null ? string.Empty : channelOrder;
        Levels = levels == null ? System.Array.Empty<ushort>() : CopyLevels(levels);
        DecryptStart = decryptStart;
        LevelsPosition = levelsPosition;
    }

    /// <summary>Returns an independent copy of the exact original bytes.</summary>
    /// <returns>The copied original value.</returns>
    internal byte[] CopyOriginalBytes()
    {
        return RawTextValue.Copy(OriginalBytes);
    }

    /// <summary>Returns an independent copy of the decoded working bytes.</summary>
    /// <returns>The copied decoded value.</returns>
    internal byte[] CopyDecodedBytes()
    {
        return RawTextValue.Copy(DecodedBytes);
    }

    internal static ushort[] CopyLevels(ushort[] source)
    {
        ushort[] result = new ushort[source.Length];
        source.AsSpan().CopyTo(result);
        return result;
    }
}
