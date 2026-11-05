namespace raw;

/// <summary>Describes one structurally proven offset stored inside a maker note.</summary>
public class RawMakerNoteOffsetReference
{
    /// <summary>The byte position of the stored offset within the maker-note payload.</summary>
    public int Position;
    /// <summary>The encoded offset width in bytes.</summary>
    public int Width;
    /// <summary>The original unsigned stored value.</summary>
    public ulong Value;
    /// <summary>The referenced byte position within the maker-note payload.</summary>
    public int TargetPosition;
    /// <summary>Whether relocation must add the maker-note movement delta to the stored value.</summary>
    public bool AdjustOnRelocation;

    /// <summary>Creates a maker-note offset reference.</summary>
    /// <param name="position">The stored offset position.</param>
    /// <param name="width">The encoded width.</param>
    /// <param name="value">The original stored value.</param>
    /// <param name="targetPosition">The referenced maker-note byte position.</param>
    /// <param name="adjustOnRelocation">Whether relocation changes the stored value.</param>
    public RawMakerNoteOffsetReference(int position, int width, ulong value, int targetPosition, bool adjustOnRelocation)
    {
        Position = position;
        Width = width;
        Value = value;
        TargetPosition = targetPosition;
        AdjustOnRelocation = adjustOnRelocation;
    }
}

/// <summary>Reports the selected safe placement strategy for one preserved maker note.</summary>
public class RawMakerNoteWritePlan
{
    /// <summary>Whether the requested placement can be written safely.</summary>
    public bool CanWrite;
    /// <summary>The selected writer strategy.</summary>
    public RawMakerNoteWriteStrategy Strategy;
    /// <summary>The original absolute maker-note offset.</summary>
    public long OriginalOffset;
    /// <summary>The requested absolute maker-note offset.</summary>
    public long PlannedOffset;
    /// <summary>The signed movement from the original to planned offset.</summary>
    public long OffsetDelta;
    /// <summary>The exact or safely patched maker-note payload, or an empty array when rejected.</summary>
    public byte[] Bytes;
    /// <summary>The block-level dry-run change.</summary>
    public RawWriteChange[] Changes;
    /// <summary>Safety and compatibility diagnostics.</summary>
    public RawDiagnostic[] Diagnostics;

    /// <summary>Creates a maker-note placement plan.</summary>
    /// <param name="canWrite">Whether the requested placement is safe.</param>
    /// <param name="strategy">The selected strategy.</param>
    /// <param name="originalOffset">The original absolute offset.</param>
    /// <param name="plannedOffset">The planned absolute offset.</param>
    /// <param name="offsetDelta">The signed placement delta.</param>
    /// <param name="bytes">The planned payload bytes.</param>
    /// <param name="changes">The dry-run block changes.</param>
    /// <param name="diagnostics">Safety diagnostics.</param>
    public RawMakerNoteWritePlan(bool canWrite, RawMakerNoteWriteStrategy strategy, long originalOffset, long plannedOffset, long offsetDelta, byte[]? bytes, RawWriteChange[]? changes, RawDiagnostic[]? diagnostics)
    {
        CanWrite = canWrite;
        Strategy = strategy;
        OriginalOffset = originalOffset;
        PlannedOffset = plannedOffset;
        OffsetDelta = offsetDelta;
        Bytes = bytes == null ? System.Array.Empty<byte>() : bytes;
        Changes = changes == null ? System.Array.Empty<RawWriteChange>() : changes;
        Diagnostics = diagnostics == null ? System.Array.Empty<RawDiagnostic>() : diagnostics;
    }
}

/// <summary>Retains one exact maker note together with its proven offset-base semantics.</summary>
public class RawMakerNote
{
    /// <summary>The exact maker-note value bytes.</summary>
    public byte[]? RawBytes;
    /// <summary>The camera manufacturer text used for conservative layout recognition.</summary>
    public string? Manufacturer;
    /// <summary>The recognized payload signature, or an empty string.</summary>
    public string? Signature;
    /// <summary>The absolute first maker-note byte.</summary>
    public long Offset;
    /// <summary>The absolute containing Exif entry offset, or <c>-1</c>.</summary>
    public long EntryOffset;
    /// <summary>The containing Exif IFD path.</summary>
    public string? IfdPath;
    /// <summary>The byte order used by recognized internal TIFF structures.</summary>
    public RawByteOrder ByteOrder;
    /// <summary>The recognized internal offset origin.</summary>
    public RawMakerNoteOffsetBase OffsetBase;
    /// <summary>The absolute offset represented by a stored zero offset.</summary>
    public long OffsetBaseAbsolute;
    /// <summary>The embedded header byte position, or <c>-1</c>.</summary>
    public int EmbeddedHeaderPosition;
    /// <summary>Structurally proven internal offset references.</summary>
    public RawMakerNoteOffsetReference[]? OffsetReferences;
    /// <summary>Whether the complete recognized layout can be relocated without guessing.</summary>
    public bool RelocationSupported;
    /// <summary>Every Canon main-IFD entry, including unknown entries, or an empty array for other layouts.</summary>
    public RawCanonMakerNoteTag[]? CanonTags;
    /// <summary>Every Nikon type-3 main-IFD entry, including protected and unknown entries, or an empty array for other layouts.</summary>
    public RawNikonMakerNoteTag[]? NikonTags;
    /// <summary>The exact four-character Nikon maker-note data version, or an empty string.</summary>
    public string? NikonVersion;
    /// <summary>The retained Nikon color-balance block, or <c>null</c> when tag 0x0097 is absent.</summary>
    public RawNikonColorBalance? NikonColorBalance;

    /// <summary>Parses standalone maker-note bytes using their containing TIFF context.</summary>
    /// <param name="rawBytes">The exact maker-note bytes.</param>
    /// <param name="offset">The absolute maker-note offset.</param>
    /// <param name="entryOffset">The absolute containing Exif entry offset, or <c>-1</c>.</param>
    /// <param name="ifdPath">The containing IFD path.</param>
    /// <param name="manufacturer">The camera manufacturer text.</param>
    /// <param name="tiffBaseOffset">The absolute containing TIFF header offset.</param>
    /// <param name="tiffByteOrder">The containing TIFF byte order.</param>
    /// <param name="limits">Resource limits.</param>
    /// <returns>The parsed maker note.</returns>
    internal static RawMakerNote Parse(byte[] rawBytes, long offset, long entryOffset, string ifdPath, string manufacturer, long tiffBaseOffset, RawByteOrder tiffByteOrder, RawLimits limits)
    {
        return MakerNoteCodec.Parse(rawBytes, offset, entryOffset, ifdPath, manufacturer, tiffBaseOffset, tiffByteOrder, limits);
    }

    internal RawMakerNote(byte[] rawBytes, string manufacturer, string signature, long offset, long entryOffset, string? ifdPath, RawByteOrder byteOrder, RawMakerNoteOffsetBase offsetBase, long offsetBaseAbsolute, int embeddedHeaderPosition, RawMakerNoteOffsetReference[]? references, bool relocationSupported)
    {
        RawBytes = rawBytes;
        Manufacturer = manufacturer;
        Signature = signature;
        Offset = offset;
        EntryOffset = entryOffset;
        IfdPath = ifdPath;
        ByteOrder = byteOrder;
        OffsetBase = offsetBase;
        OffsetBaseAbsolute = offsetBaseAbsolute;
        EmbeddedHeaderPosition = embeddedHeaderPosition;
        OffsetReferences = references;
        RelocationSupported = relocationSupported;
        CanonTags = System.Array.Empty<RawCanonMakerNoteTag>();
        NikonTags = System.Array.Empty<RawNikonMakerNoteTag>();
        NikonVersion = string.Empty;
        NikonColorBalance = null;
    }

    /// <summary>Returns an independent copy of the complete maker-note bytes.</summary>
    /// <returns>The copied maker-note payload.</returns>
    internal byte[] CopyBytes()
    {
        return RawTextValue.Copy(RawBytes);
    }

    /// <summary>Plans byte-exact anchoring, proven relocation, or safe rejection for a requested absolute offset.</summary>
    /// <param name="plannedOffset">The requested absolute maker-note offset.</param>
    /// <param name="limits">Resource limits.</param>
    /// <returns>The deterministic placement plan.</returns>
    internal RawMakerNoteWritePlan PlanWrite(long plannedOffset, RawLimits limits)
    {
        return MakerNoteLayout.Plan(this, plannedOffset, limits);
    }

    /// <summary>Validates preserved bytes, offsets, and relocation evidence.</summary>
    /// <param name="limits">Resource limits.</param>
    /// <returns>All model findings.</returns>
    internal RawValidationReport Validate(RawLimits limits)
    {
        return MakerNoteLayout.Validate(this, limits);
    }

    /// <summary>Finds a unique Canon main-IFD tag by identifier.</summary>
    /// <param name="tagId">The Canon tag identifier.</param>
    /// <returns>The unique tag, or <c>null</c> when absent or duplicated.</returns>
    internal RawCanonMakerNoteTag? FindCanonTag(ushort tagId)
    {
        RawCanonMakerNoteTag? match = null;
        if (CanonTags == null)
        {
            return null;
        }
        int index = 0;
        while (index < CanonTags.Length)
        {
            RawCanonMakerNoteTag tag = CanonTags[index];
            if (tag != null && tag.Id == tagId)
            {
                if (match != null)
                {
                    return null;
                }
                match = tag;
            }
            index++;
        }
        return match;
    }

    /// <summary>Encodes version-gated replacements into a new Canon maker note without changing its layout or unknown bytes.</summary>
    /// <param name="edits">The ordered existing-tag replacements.</param>
    /// <param name="limits">Resource limits.</param>
    /// <returns>A newly parsed maker note containing the replacements.</returns>
    /// <exception cref="RawWriteException">The note, schema version, tag, type, count, or fixed storage is unsupported.</exception>
    internal RawMakerNote EncodeCanon(RawCanonMakerNoteEdit[] edits, RawLimits limits)
    {
        return CanonMakerNoteCodec.Encode(this, edits, limits);
    }

    /// <summary>Finds a unique Nikon type-3 main-IFD tag by identifier.</summary>
    /// <param name="tagId">The Nikon tag identifier.</param>
    /// <returns>The unique tag, or <c>null</c> when absent or duplicated.</returns>
    internal RawNikonMakerNoteTag? FindNikonTag(ushort tagId)
    {
        RawNikonMakerNoteTag? match = null;
        if (NikonTags == null)
        {
            return null;
        }
        int index = 0;
        while (index < NikonTags.Length)
        {
            RawNikonMakerNoteTag tag = NikonTags[index];
            if (tag != null && tag.Id == tagId)
            {
                if (match != null)
                {
                    return null;
                }
                match = tag;
            }
            index++;
        }
        return match;
    }

    /// <summary>Encodes version-gated replacements into a new Nikon maker note without changing protected, encrypted, Capture/Edit, unknown, or layout bytes.</summary>
    /// <param name="edits">The ordered existing-tag replacements.</param>
    /// <param name="limits">Resource limits.</param>
    /// <returns>A newly parsed maker note containing the replacements.</returns>
    /// <exception cref="RawWriteException">The note, schema version, tag, type, count, or fixed storage is unsupported.</exception>
    internal RawMakerNote EncodeNikon(RawNikonMakerNoteEdit[] edits, RawLimits limits)
    {
        return NikonMakerNoteCodec.Encode(this, edits, limits);
    }

}
