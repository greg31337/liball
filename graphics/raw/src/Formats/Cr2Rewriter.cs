using System;

namespace raw;

internal class Cr2RewriteReplacement
{
    internal RawPayloadBlock Original;
    internal RawPreviewMetadataEdit Edit;

    internal Cr2RewriteReplacement(RawPayloadBlock original, RawPreviewMetadataEdit edit)
    {
        Original = original;
        Edit = edit;
    }
}

internal class Cr2RewriteBlock
{
    internal string Name;
    internal string Path;
    internal long OriginalOffset;
    internal long OriginalLength;
    internal byte[] Bytes;
    internal TiffLayoutBlockKind Kind;
    internal bool Raw;

    internal Cr2RewriteBlock(string name, string path, long originalOffset, long originalLength, byte[] bytes, TiffLayoutBlockKind kind, bool raw)
    {
        Name = name;
        Path = path;
        OriginalOffset = originalOffset;
        OriginalLength = originalLength;
        Bytes = bytes;
        Kind = kind;
        Raw = raw;
    }
}

internal class Cr2ExifField
{
    internal string Name;
    internal string Path;
    internal ushort Tag;

    internal Cr2ExifField(string name, string path, ushort tag)
    {
        Name = name;
        Path = path;
        Tag = tag;
    }
}

internal class Cr2RewriteState
{
    internal RawDocument Document;
    internal RawWriteOptions Options;
    internal Cr2RewriteReplacement[] Replacements;
    internal Cr2RewriteBlock[] Blocks;
    internal TiffLayoutPlan? Plan;

    internal Cr2RewriteState(RawDocument document, RawWriteOptions options)
    {
        Document = document;
        Options = options;
        Replacements = System.Array.Empty<Cr2RewriteReplacement>();
        Blocks = System.Array.Empty<Cr2RewriteBlock>();
        Plan = null;
    }
}

internal static class Cr2Rewriter
{
    internal static byte[] Rewrite(RawDocument document, RawWriteOptions options)
    {
        Cr2RewriteState state = CreateState(document, options);
        byte[] output = TiffWriter.WriteToArray(state.Plan, options.Limits, options.CancellationToken);
        ValidateOutput(state, output);
        return output;
    }

    internal static Cr2RewriteState CreateState(RawDocument document, RawWriteOptions options)
    {
        Validate(document, options);
        Cr2RewriteState state = new Cr2RewriteState(document, options);
        state.Replacements = CreateReplacements(document, options);
        state.Blocks = CreateBlocks(state);
        TiffLayoutRequest request = CreateRequest(state);
        state.Plan = TiffLayoutPlanner.Plan(request, options.Limits, options.CancellationToken);
        UpdateCr2Header(state);
        ApplyMakerNotes(state);
        return state;
    }

    internal static void Validate(RawDocument document, RawWriteOptions options)
    {
        if (options == null || !options.Validate().IsValid() || options.Mode != RawWriteMode.PreservingRewrite)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Valid preserving-rewrite options are required for CR2 rewriting.", -1);
        }
        options.CancellationToken.ThrowIfCancellationRequested();
        if (document == null || document.Format != RawFormat.Cr2 || document.FormatDetails == null || document.FormatDetails.Cr2Header == null || document.TiffGraph == null || document.TiffGraph.Header == null || document.TiffGraph.Header.BigTiff || !document.SourceRetained || document.Source.Length == 0)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A source-retaining classic TIFF CR2 document is required for preserving rewrite.", -1);
        }
        if (!document.MetadataLoaded || document.Metadata == null || document.Metadata.MakerNotes == null || !document.PreviewsLoaded || document.Thumbnails == null || document.PayloadMap == null || !document.PayloadMap.Validate().IsValid())
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "CR2 preserving rewrite requires fully loaded metadata, maker notes, previews, and a valid payload map.", -1);
        }
        if (options.UnknownDataPolicy != RawUnknownDataPolicy.Preserve)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "CR2 preserving rewrite currently requires byte-preserving unknown-data policy.", -1);
        }
        ValidatePendingMetadata(document);
    }

    internal static void ValidatePendingMetadata(RawDocument document)
    {
        RawMetadataEdit[] edits = document.Metadata.PendingEdits;
        int index = 0;
        while (edits != null && index < edits.Length)
        {
            RawMetadataEdit edit = edits[index];
            if (edit == null)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidMetadata, "Every staged metadata edit must be populated.", -1);
            }
            if (edit.Source == RawMetadataSource.Xmp || edit.Source == RawMetadataSource.Iptc || edit.Source == RawMetadataSource.None && (HasSourceField(document.Metadata, edit.Name, RawMetadataSource.Xmp) || HasSourceField(document.Metadata, edit.Name, RawMetadataSource.Iptc)))
            {
                throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "A CR2 rewrite cannot partially apply a staged XMP or IPTC representation edit.", -1);
            }
            if (edit.Source == RawMetadataSource.Exif || edit.Source == RawMetadataSource.None)
            {
                Cr2ExifField? field = ExifField(edit.Name);
                if (field == null || edit.Language != null && edit.Language.Length != 0 || edit.Kind != RawMetadataEditKind.Remove && (edit.Values == null || edit.Values.Length != 1))
                {
                    throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "The staged Exif edit is not representable by the conservative CR2 metadata writer.", -1);
                }
            }
            index++;
        }
    }

    internal static bool HasSourceField(RawMetadata metadata, string name, RawMetadataSource source)
    {
        int index = 0;
        while (metadata.SourceFields != null && index < metadata.SourceFields.Length)
        {
            RawMetadataField field = metadata.SourceFields[index];
            if (field != null && field.Name == name && field.Source == source)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static Cr2RewriteReplacement[] CreateReplacements(RawDocument document, RawWriteOptions options)
    {
        System.Collections.Generic.List<RawPayloadBlock> candidates = new System.Collections.Generic.List<RawPayloadBlock>();
        int blockIndex = 0;
        while (blockIndex < document.PayloadMap.Blocks.Length)
        {
            RawPayloadBlock block = document.PayloadMap.Blocks[blockIndex];
            if (block.Kind == RawPayloadKind.Preview || block.Kind == RawPayloadKind.Thumbnail)
            {
                candidates.Add(block);
            }
            blockIndex++;
        }
        if (candidates.Count != document.Thumbnails.Length)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "Every CR2 preview must map to one independently preserved payload before replacement.", -1);
        }
        System.Collections.Generic.List<Cr2RewriteReplacement> replacements = new System.Collections.Generic.List<Cr2RewriteReplacement>();
        int index = 0;
        while (document.PendingPreviewEdits != null && index < document.PendingPreviewEdits.Length)
        {
            RawPreviewMetadataEdit edit = document.PendingPreviewEdits[index];
            if (edit == null || edit.Index < 0 || edit.Index >= candidates.Count || edit.Kind != RawThumbnailKind.Jpeg)
            {
                throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "CR2 preview replacement requires one existing baseline-JPEG payload.", -1);
            }
            RawThumbnail preview = new RawThumbnail(edit.Index, edit.Kind, edit.Width, edit.Height, edit.Orientation, edit.MimeType, edit.EncodedBytes, null, null);
            Cr2Encoder.ValidatePreview(preview, options, "replacement preview");
            int prior = FindReplacement(replacements, candidates[edit.Index]);
            Cr2RewriteReplacement replacement = new Cr2RewriteReplacement(candidates[edit.Index], edit);
            if (prior < 0)
            {
                replacements.Add(replacement);
            }
            else
            {
                replacements[prior] = replacement;
            }
            index++;
        }
        return replacements.ToArray();
    }

    internal static int FindReplacement(System.Collections.Generic.List<Cr2RewriteReplacement> replacements, RawPayloadBlock block)
    {
        int index = 0;
        while (index < replacements.Count)
        {
            if (replacements[index].Original == block)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    internal static Cr2RewriteBlock[] CreateBlocks(Cr2RewriteState state)
    {
        System.Collections.Generic.List<Cr2RewriteBlock> blocks = new System.Collections.Generic.List<Cr2RewriteBlock>();
        TiffImageDescriptor[] images = (state.Document.TiffGraph ?? throw new System.InvalidOperationException("A required value is unavailable.")).Images;
        int imageIndex = 0;
        while (imageIndex < images.Length)
        {
            TiffImageDescriptor image = images[imageIndex];
            int dataIndex = 0;
            while (dataIndex < image.Blocks.Length)
            {
                TiffDataBlock data = image.Blocks[dataIndex];
                long offset = RawCodec.RelativeTiffOffset(data.Offset, state.Document.TiffGraph);
                long length = CheckedMath.ToInt64(data.ByteCount);
                AddBlock(state, blocks, image.Path + "-" + (data.Tiled ? "tile-" : "strip-") + data.Index.ToString(System.Globalization.CultureInfo.InvariantCulture), offset, length, data.Tiled ? TiffLayoutBlockKind.Tile : TiffLayoutBlockKind.Strip);
                dataIndex++;
            }
            imageIndex++;
        }
        int ifdIndex = 0;
        while (ifdIndex < state.Document.TiffGraph.Ifds.Length)
        {
            TiffIfd ifd = state.Document.TiffGraph.Ifds[ifdIndex];
            TiffEntry? offsetEntry = ifd.FindEntry(513);
            TiffEntry? lengthEntry = ifd.FindEntry(514);
            ulong[] offsets = TiffImageData.GetUnsignedValues(offsetEntry);
            ulong[] lengths = TiffImageData.GetUnsignedValues(lengthEntry);
            if (offsets.Length == 1 && lengths.Length == 1 && lengths[0] != 0)
            {
                long offset = RawCodec.RelativeTiffOffset(offsets[0], state.Document.TiffGraph);
                AddBlock(state, blocks, ifd.Path + "-jpeg-interchange", offset, CheckedMath.ToInt64(lengths[0]), TiffLayoutBlockKind.Opaque);
            }
            else if (offsets.Length != 0 || lengths.Length != 0)
            {
                throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "CR2 JPEG interchange payload arrays are not supported by preserving rewrite.", ifd.AbsoluteOffset);
            }
            ifdIndex++;
        }
        int opaqueIndex = 0;
        while (opaqueIndex < state.Document.PayloadMap.Blocks.Length)
        {
            RawPayloadBlock opaque = state.Document.PayloadMap.Blocks[opaqueIndex];
            if (opaque != null && opaque.Kind == RawPayloadKind.Opaque && FindBlock(blocks, opaque.Offset, opaque.Length) == null)
            {
                AddBlock(state, blocks, "opaque-island-" + opaqueIndex.ToString(System.Globalization.CultureInfo.InvariantCulture), opaque.Offset, opaque.Length, TiffLayoutBlockKind.Opaque);
            }
            opaqueIndex++;
        }
        return blocks.ToArray();
    }

    internal static void AddBlock(Cr2RewriteState state, System.Collections.Generic.List<Cr2RewriteBlock> blocks, string name, long offset, long length, TiffLayoutBlockKind kind)
    {
        if (offset < 0 || length <= 0 || offset > state.Document.Source.Length || length > state.Document.Source.Length - offset)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidOffset, "A preserved CR2 payload exceeds the retained source.", offset);
        }
        Cr2RewriteBlock? existing = FindBlock(blocks, offset, length);
        if (existing != null)
        {
            return;
        }
        RawPayloadBlock? mapped = FindMappedBlock(state.Document.PayloadMap, offset, length);
        byte[] bytes = state.Document.Source.Slice((int)offset, (int)length).ToArray();
        Cr2RewriteReplacement? replacement = mapped == null ? null : FindReplacement(state.Replacements, mapped);
        if (replacement != null)
        {
            bytes = RawTextValue.Copy(replacement.Edit.EncodedBytes);
        }
        string path = mapped == null ? name : mapped.Path;
        bool raw = mapped != null && mapped.Kind == RawPayloadKind.RawImage;
        blocks.Add(new Cr2RewriteBlock(name, path, offset, length, bytes, kind, raw));
    }

    internal static Cr2RewriteBlock? FindBlock(System.Collections.Generic.List<Cr2RewriteBlock> blocks, long offset, long length)
    {
        int index = 0;
        while (index < blocks.Count)
        {
            Cr2RewriteBlock block = blocks[index];
            if (block.OriginalOffset == offset && block.OriginalLength == length)
            {
                return block;
            }
            index++;
        }
        return null;
    }

    internal static Cr2RewriteBlock? FindBlock(Cr2RewriteBlock[] blocks, long offset, long length)
    {
        int index = 0;
        while (index < blocks.Length)
        {
            Cr2RewriteBlock block = blocks[index];
            if (block.OriginalOffset == offset && block.OriginalLength == length)
            {
                return block;
            }
            index++;
        }
        return null;
    }

    internal static RawPayloadBlock? FindMappedBlock(RawPayloadMap map, long offset, long length)
    {
        int index = 0;
        while (index < map.Blocks.Length)
        {
            RawPayloadBlock block = map.Blocks[index];
            if (block.Offset == offset && block.Length == length)
            {
                return block;
            }
            index++;
        }
        return null;
    }

    internal static Cr2RewriteReplacement? FindReplacement(Cr2RewriteReplacement[] replacements, RawPayloadBlock block)
    {
        int index = 0;
        while (index < replacements.Length)
        {
            if (replacements[index].Original == block)
            {
                return replacements[index];
            }
            index++;
        }
        return null;
    }

    internal static TiffLayoutRequest CreateRequest(Cr2RewriteState state)
    {
        return CreateRequest(state, false, HeaderExtra(state.Document), "IFD0");
    }

    internal static TiffLayoutRequest CreateRequest(Cr2RewriteState state, bool bigTiff, byte[] headerExtra, string firstIfdPath)
    {
        TiffIfdGraph? graph = state.Document.TiffGraph;
        TiffLayoutIfdInput[] ifds = new TiffLayoutIfdInput[(graph ?? throw new System.InvalidOperationException("A required value is unavailable.")).Ifds.Length];
        int ifdIndex = 0;
        while (ifdIndex < graph.Ifds.Length)
        {
            TiffIfd ifd = graph.Ifds[ifdIndex];
            System.Collections.Generic.List<TiffLayoutEntryInput> entries = new System.Collections.Generic.List<TiffLayoutEntryInput>();
            int entryIndex = 0;
            while (entryIndex < ifd.Entries.Length)
            {
                TiffLayoutEntryInput? entry = CreateEntry(state, ifd, ifd.Entries[entryIndex]);
                if (entry != null)
                {
                    entries.Add(entry);
                }
                entryIndex++;
            }
            AddPublicAndPendingEntries(state, ifd, entries);
            ifds[ifdIndex] = new TiffLayoutIfdInput(ifd.Path, entries.ToArray(), NextIfdPath(graph, ifd));
            ifdIndex++;
        }
        TiffLayoutBlockInput[] blocks = new TiffLayoutBlockInput[state.Blocks.Length];
        int blockIndex = 0;
        while (blockIndex < state.Blocks.Length)
        {
            Cr2RewriteBlock block = state.Blocks[blockIndex];
            blocks[blockIndex] = new TiffLayoutBlockInput(block.Name, block.Kind, block.Bytes.Length, block.Bytes);
            blockIndex++;
        }
        return TiffLayoutRequest.CreateWithHeaderExtraBytes(bigTiff, graph.Header.ByteOrder, headerExtra, firstIfdPath, ifds, blocks);
    }

    internal static byte[] HeaderExtra(RawDocument document)
    {
        int headerSize = (document.TiffGraph ?? throw new System.InvalidOperationException("A required value is unavailable.")).Header.HeaderSize;
        long firstIfd = ((document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Cr2Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).FirstIfdOffset;
        if (firstIfd < headerSize || firstIfd > document.Source.Length || firstIfd - headerSize < 8)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidContainer, "The retained CR2 header extension is invalid.", firstIfd);
        }
        int length = CheckedMath.ToInt32(firstIfd - headerSize);
        return document.Source.Slice(headerSize, length).ToArray();
    }

    internal static TiffLayoutEntryInput? CreateEntry(Cr2RewriteState state, TiffIfd ifd, TiffEntry source)
    {
        RawMetadataEdit? edit = PendingEdit(state.Document.Metadata, ifd.Path, source.Tag);
        if (edit != null && edit.Kind == RawMetadataEditKind.Remove)
        {
            return null;
        }
        TiffFieldType fieldType = source.FieldType;
        ulong count = source.Count;
        byte[] bytes = source.CopyRawBytes();
        RawIfd? publicIfd = state.Document.Metadata.FindIfd(ifd.Path);
        RawTag? publicTag = publicIfd == null ? null : publicIfd.FindTag(source.Tag);
        if (publicTag != null && publicTag.Value != null)
        {
            fieldType = (TiffFieldType)(ushort)publicTag.Value.Type;
            count = publicTag.Value.Count;
            bytes = publicTag.Value.CopyBytes();
        }
        if (edit != null && edit.Kind != RawMetadataEditKind.Remove)
        {
            RawTagValue value = EncodeExifEdit(edit);
            fieldType = (TiffFieldType)(ushort)value.Type;
            count = value.Count;
            bytes = value.CopyBytes();
        }
        System.Collections.Generic.List<TiffLayoutReference> references = DirectoryReferences(state.Document.TiffGraph, ifd, source.Tag);
        ApplyImageReferences(state, ifd, source.Tag, bytes, references);
        return new TiffLayoutEntryInput(source.Tag, fieldType, count, bytes, references.ToArray());
    }

    internal static void AddPublicAndPendingEntries(Cr2RewriteState state, TiffIfd ifd, System.Collections.Generic.List<TiffLayoutEntryInput> entries)
    {
        RawIfd? publicIfd = state.Document.Metadata.FindIfd(ifd.Path);
        int index = 0;
        while (publicIfd != null && index < publicIfd.Tags.Length)
        {
            RawTag tag = publicIfd.Tags[index];
            if (tag != null && tag.Value != null && ifd.FindEntry(tag.Id) == null && FindEntry(entries, tag.Id) == null)
            {
                RawMetadataEdit? edit = PendingEdit(state.Document.Metadata, ifd.Path, tag.Id);
                if (edit == null || edit.Kind != RawMetadataEditKind.Remove)
                {
                    RawTagValue value = edit == null ? tag.Value : EncodeExifEdit(edit);
                    entries.Add(new TiffLayoutEntryInput(tag.Id, (TiffFieldType)(ushort)value.Type, value.Count, value.CopyBytes(), null));
                }
            }
            index++;
        }
        RawMetadataEdit[] edits = state.Document.Metadata.PendingEdits;
        index = 0;
        while (edits != null && index < edits.Length)
        {
            RawMetadataEdit edit = edits[index];
            Cr2ExifField? field = ExifField(edit.Name);
            if (field != null && field.Path == ifd.Path && edit.Kind == RawMetadataEditKind.Add && FindEntry(entries, field.Tag) == null)
            {
                RawTagValue value = EncodeExifEdit(edit);
                entries.Add(new TiffLayoutEntryInput(field.Tag, (TiffFieldType)(ushort)value.Type, value.Count, value.CopyBytes(), null));
            }
            index++;
        }
    }

    internal static TiffLayoutEntryInput? FindEntry(System.Collections.Generic.List<TiffLayoutEntryInput> entries, ushort tag)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (entries[index].Tag == tag)
            {
                return entries[index];
            }
            index++;
        }
        return null;
    }

    internal static RawMetadataEdit? PendingEdit(RawMetadata metadata, string? path, ushort tag)
    {
        RawMetadataEdit? match = null;
        int index = 0;
        while (metadata.PendingEdits != null && index < metadata.PendingEdits.Length)
        {
            RawMetadataEdit edit = metadata.PendingEdits[index];
            Cr2ExifField? field = edit == null ? null : ExifField(edit.Name);
            if (field != null && field.Path == path && field.Tag == tag && ((edit ?? throw new System.InvalidOperationException("A required value is unavailable.")).Source == RawMetadataSource.Exif || edit.Source == RawMetadataSource.None))
            {
                match = edit;
            }
            index++;
        }
        return match;
    }

    internal static RawTagValue EncodeExifEdit(RawMetadataEdit edit)
    {
        if (edit.Values == null || edit.Values.Length != 1)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidMetadata, "A scalar Exif edit requires exactly one value.", -1);
        }
        string text = edit.Values[0];
        if (text == null || text.IndexOf('\0') >= 0 || !Ascii(text))
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidMetadata, "The conservative CR2 Exif writer requires seven-bit text without embedded nulls.", -1);
        }
        return RawTagValue.CreateAscii(text);
    }

    internal static bool Ascii(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] > 0x7f)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static Cr2ExifField? ExifField(string name)
    {
        if (name == "Make") return new Cr2ExifField(name, "IFD0", 271);
        if (name == "Model") return new Cr2ExifField(name, "IFD0", 272);
        if (name == "Software") return new Cr2ExifField(name, "IFD0", 305);
        if (name == "Artist") return new Cr2ExifField(name, "IFD0", 315);
        if (name == "Copyright") return new Cr2ExifField(name, "IFD0", 33432);
        if (name == "LensMake") return new Cr2ExifField(name, "IFD0/Exif", 42035);
        if (name == "LensModel") return new Cr2ExifField(name, "IFD0/Exif", 42036);
        if (name == "CameraOwnerName") return new Cr2ExifField(name, "IFD0/Exif", 42032);
        if (name == "BodySerialNumber") return new Cr2ExifField(name, "IFD0/Exif", 42033);
        if (name == "LensSerialNumber") return new Cr2ExifField(name, "IFD0/Exif", 42037);
        if (name == "ImageUniqueID") return new Cr2ExifField(name, "IFD0/Exif", 42016);
        return null;
    }

    internal static System.Collections.Generic.List<TiffLayoutReference> DirectoryReferences(TiffIfdGraph? graph, TiffIfd ifd, ushort tag)
    {
        System.Collections.Generic.List<TiffLayoutReference> references = new System.Collections.Generic.List<TiffLayoutReference>();
        int index = 0;
        while (index < (graph ?? throw new System.InvalidOperationException("A required value is unavailable.")).Links.Length)
        {
            TiffIfdLink link = graph.Links[index];
            if (link != null && link.Kind != TiffIfdLinkKind.Next && link.SourcePath == ifd.Path && link.Tag == tag)
            {
                references.Add(new TiffLayoutReference(link.Index, TiffLayoutReferenceKind.Ifd, link.TargetPath));
            }
            index++;
        }
        return references;
    }

    internal static void ApplyImageReferences(Cr2RewriteState state, TiffIfd ifd, ushort tag, byte[] bytes, System.Collections.Generic.List<TiffLayoutReference> references)
    {
        TiffImageDescriptor? image = FindImage(state.Document.TiffGraph, ifd);
        if (image != null && (tag == 273 || tag == 324 || tag == 279 || tag == 325))
        {
            bool tiled = tag == 324 || tag == 325;
            bool offsets = tag == 273 || tag == 324;
            int index = 0;
            while (index < image.Blocks.Length)
            {
                TiffDataBlock data = image.Blocks[index];
                if (data.Tiled == tiled)
                {
                    long offset = RawCodec.RelativeTiffOffset(data.Offset, state.Document.TiffGraph);
                    long length = CheckedMath.ToInt64(data.ByteCount);
                    Cr2RewriteBlock? block = FindBlock(state.Blocks, offset, length);
                    if (block == null)
                    {
                        throw new RawWriteException(RawDiagnosticCode.WritePlanningFailed, "A TIFF image reference has no preserved rewrite block.", offset);
                    }
                    if (offsets)
                    {
                        references.Add(new TiffLayoutReference(data.Index, TiffLayoutReferenceKind.Block, block.Name));
                    }
                    else
                    {
                        WriteUnsigned(bytes, data.Index, (ulong)block.Bytes.Length, (ifd.FindEntry(tag) ?? throw new System.InvalidOperationException("A required value is unavailable.")).FieldType, (state.Document.TiffGraph ?? throw new System.InvalidOperationException("A required value is unavailable.")).Header.ByteOrder);
                    }
                }
                index++;
            }
        }
        if (tag == 513 || tag == 514)
        {
            TiffEntry? offsetEntry = ifd.FindEntry(513);
            TiffEntry? lengthEntry = ifd.FindEntry(514);
            ulong[] sourceOffsets = TiffImageData.GetUnsignedValues(offsetEntry);
            ulong[] sourceLengths = TiffImageData.GetUnsignedValues(lengthEntry);
            if (sourceOffsets.Length == 1 && sourceLengths.Length == 1)
            {
                long offset = RawCodec.RelativeTiffOffset(sourceOffsets[0], state.Document.TiffGraph);
                long length = CheckedMath.ToInt64(sourceLengths[0]);
                Cr2RewriteBlock? block = FindBlock(state.Blocks, offset, length);
                if (tag == 513)
                {
                    references.Add(new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, (block ?? throw new System.InvalidOperationException("A required value is unavailable.")).Name));
                }
                else
                {
                    WriteUnsigned(bytes, 0, (ulong)(block ?? throw new System.InvalidOperationException("A required value is unavailable.")).Bytes.Length, (lengthEntry ?? throw new System.InvalidOperationException("A required value is unavailable.")).FieldType, (state.Document.TiffGraph ?? throw new System.InvalidOperationException("A required value is unavailable.")).Header.ByteOrder);
                }
            }
        }
    }

    internal static TiffImageDescriptor? FindImage(TiffIfdGraph? graph, TiffIfd ifd)
    {
        int index = 0;
        while (index < (graph ?? throw new System.InvalidOperationException("A required value is unavailable.")).Images.Length)
        {
            if (graph.Images[index].Ifd == ifd)
            {
                return graph.Images[index];
            }
            index++;
        }
        return null;
    }

    internal static void WriteUnsigned(byte[] bytes, int index, ulong value, TiffFieldType type, RawByteOrder byteOrder)
    {
        int size = TiffFieldTypes.GetElementSize(type);
        int offset = index * size;
        if (offset < 0 || offset > bytes.Length - size)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "A rewritten TIFF unsigned array index is outside its preserved value.", -1);
        }
        if (type == TiffFieldType.Short)
        {
            if (value > ushort.MaxValue) throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "A rewritten TIFF SHORT value exceeds 16 bits.", -1);
            EndianWriter.WriteUInt16(bytes.AsSpan(offset, 2), (ushort)value, byteOrder);
            return;
        }
        if (type == TiffFieldType.Long || type == TiffFieldType.Ifd)
        {
            if (value > uint.MaxValue) throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "A rewritten TIFF LONG value exceeds 32 bits.", -1);
            EndianWriter.WriteUInt32(bytes.AsSpan(offset, 4), (uint)value, byteOrder);
            return;
        }
        if (type == TiffFieldType.Long8 || type == TiffFieldType.Ifd8)
        {
            EndianWriter.WriteUInt64(bytes.AsSpan(offset, 8), value, byteOrder);
            return;
        }
        throw new RawWriteException(RawDiagnosticCode.InvalidMetadata, "A rewritten TIFF offset or byte-count field is not unsigned integer storage.", -1);
    }

    internal static string? NextIfdPath(TiffIfdGraph graph, TiffIfd ifd)
    {
        int index = 0;
        while (index < graph.Links.Length)
        {
            TiffIfdLink link = graph.Links[index];
            if (link != null && link.Kind == TiffIfdLinkKind.Next && link.SourcePath == ifd.Path)
            {
                return link.TargetPath;
            }
            index++;
        }
        if (ifd.NextIfdOffset == 0)
        {
            return null;
        }
        index = 0;
        while (index < graph.Ifds.Length)
        {
            if (graph.Ifds[index].Offset == ifd.NextIfdOffset)
            {
                return graph.Ifds[index].Path;
            }
            index++;
        }
        throw new RawWriteException(RawDiagnosticCode.WritePlanningFailed, "A CR2 next-IFD link targets an unknown directory.", ifd.AbsoluteOffset);
    }

    internal static void UpdateCr2Header(Cr2RewriteState state)
    {
        if (((state.Plan ?? throw new System.InvalidOperationException("A required value is unavailable.")).HeaderExtraBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length < 8)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidContainer, "The planned CR2 header extension is truncated.", 8);
        }
        TiffLayoutIfdPlan raw = TiffLayoutPlanner.FindIfd(state.Plan, ((state.Document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Cr2Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawIfdPath);
        if (raw.Offset > uint.MaxValue)
        {
            throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "The rewritten CR2 raw IFD exceeds classic TIFF offset storage.", -1);
        }
        EndianWriter.WriteUInt32(state.Plan.HeaderExtraBytes.AsSpan(4, 4), (uint)raw.Offset, state.Plan.ByteOrder);
    }

    internal static void ApplyMakerNotes(Cr2RewriteState state)
    {
        int index = 0;
        while (index < state.Document.Metadata.MakerNotes.Length)
        {
            RawMakerNote note = state.Document.Metadata.MakerNotes[index];
            TiffLayoutIfdPlan ifd = TiffLayoutPlanner.FindIfd(state.Plan, note.IfdPath);
            TiffLayoutEntryPlan? entry = FindPlannedEntry(ifd, 37500);
            if (entry == null || entry.Inline || entry.ValueLength != (note.RawBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
            {
                throw new RawWriteException(RawDiagnosticCode.WritePlanningFailed, "A preserved maker note no longer has matching TIFF value storage.", note.Offset);
            }
            RawMakerNoteWritePlan makerPlan = note.PlanWrite((long)entry.ValueOffset, state.Options.Limits);
            if (!makerPlan.CanWrite)
            {
                throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "The maker note cannot retain a fixed anchor or use a proven relocation strategy.", note.Offset);
            }
            entry.ValueBytes = makerPlan.Bytes;
            UpdateCanonOriginalDecisionOffset(state, note, entry.ValueBytes);
            index++;
        }
    }

    internal static void UpdateCanonOriginalDecisionOffset(Cr2RewriteState state, RawMakerNote note, byte[] makerBytes)
    {
        if (note == null || note.Signature != "CanonIfd" || note.CanonTags == null)
        {
            return;
        }
        RawCanonMakerNoteTag? pointer = null;
        int tagIndex = 0;
        while (tagIndex < note.CanonTags.Length)
        {
            RawCanonMakerNoteTag tag = note.CanonTags[tagIndex];
            if (tag != null && tag.Id == 0x0083 && tag.Value != null && tag.Value.Type == RawTagType.Long && tag.Value.Count == 1)
            {
                pointer = tag;
                break;
            }
            tagIndex++;
        }
        if (pointer == null)
        {
            return;
        }
        long sourceOffset = CheckedMath.ToInt64(pointer.Value.GetUInt32Values()[0]) - (state.Document.TiffGraph ?? throw new System.InvalidOperationException("A required value is unavailable.")).ContainerStart;
        RawPayloadBlock[] authenticity = state.Document.PayloadMap.Find(RawPayloadKind.Authenticity);
        RawPayloadBlock? original = null;
        int blockIndex = 0;
        while (blockIndex < authenticity.Length)
        {
            if (authenticity[blockIndex].Offset == sourceOffset && authenticity[blockIndex].Path.EndsWith("/OriginalDecisionData", System.StringComparison.Ordinal))
            {
                original = authenticity[blockIndex];
                break;
            }
            blockIndex++;
        }
        Cr2RewriteBlock? sourceBlock = original == null ? null : FindBlock(state.Blocks, original.Offset, original.Length);
        if (sourceBlock == null)
        {
            throw new RawWriteException(RawDiagnosticCode.WritePlanningFailed, "Canon Original Decision Data has no preserved output block.", sourceOffset);
        }
        TiffLayoutBlockPlan outputBlock = TiffLayoutPlanner.FindBlock(state.Plan, sourceBlock.Name);
        if (outputBlock.Offset > uint.MaxValue || pointer.ValuePosition < 0 || pointer.ValuePosition > makerBytes.Length - 4)
        {
            throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "Canon Original Decision Data cannot be represented by its preserved offset field.", sourceOffset);
        }
        EndianWriter.WriteUInt32(makerBytes.AsSpan(pointer.ValuePosition, 4), (uint)outputBlock.Offset, pointer.Value.ByteOrder);
    }

    internal static TiffLayoutEntryPlan? FindPlannedEntry(TiffLayoutIfdPlan ifd, ushort tag)
    {
        int index = 0;
        while (index < (ifd.Entries ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            if (ifd.Entries[index].Tag == tag)
            {
                return ifd.Entries[index];
            }
            index++;
        }
        return null;
    }

    internal static void ValidateOutput(Cr2RewriteState state, byte[] output)
    {
        RawOpenOptions openOptions = new RawOpenOptions();
        openOptions.AllowExperimentalProfiles = true;
        openOptions.ReadMetadata = true;
        openOptions.ReadPreviews = true;
        openOptions.ReadRawPayload = true;
        RawDocument reopened = RawCodec.Open(output, null, openOptions);
        if (reopened.Format != RawFormat.Cr2 || (reopened.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Cr2Header == null || (reopened.FormatDetails.Cr2Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawIfdPath != ((state.Document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Cr2Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawIfdPath)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The rewritten CR2 failed header and raw-IFD reopen validation.", -1);
        }
        if (state.Options.VerifyRawPayloadIdentity)
        {
            VerifyRawBytes(state.Document, reopened);
        }
        VerifyPreviews(state, reopened);
        VerifyPreservedPayloads(state, reopened);
    }

    internal static void VerifyRawBytes(RawDocument original, RawDocument reopened)
    {
        RawPayloadBlock[] sourceBlocks = original.PayloadMap.Find(RawPayloadKind.RawImage);
        RawPayloadBlock[] outputBlocks = reopened.PayloadMap.Find(RawPayloadKind.RawImage);
        if (sourceBlocks.Length == 0 || sourceBlocks.Length != outputBlocks.Length)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The rewritten CR2 raw payload map changed.", -1);
        }
        int index = 0;
        while (index < sourceBlocks.Length)
        {
            if (!SameBytes(original.ExtractPayload(sourceBlocks[index]), reopened.ExtractPayload(outputBlocks[index])))
            {
                throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The rewritten CR2 raw payload bytes are not identical.", outputBlocks[index].Offset);
            }
            index++;
        }
    }

    internal static void VerifyPreviews(Cr2RewriteState state, RawDocument reopened)
    {
        if (reopened.Thumbnails.Length != state.Document.Thumbnails.Length)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The rewritten CR2 preview collection changed unexpectedly.", -1);
        }
        int index = 0;
        while (index < reopened.Thumbnails.Length)
        {
            byte[] expected = state.Document.Thumbnails[index].EncodedBytes;
            if (!SameBytes(expected, reopened.Thumbnails[index].EncodedBytes))
            {
                throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "A rewritten CR2 preview does not match its reused or replacement bytes.", -1);
            }
            index++;
        }
    }

    internal static void VerifyPreservedPayloads(Cr2RewriteState state, RawDocument reopened)
    {
        int index = 0;
        while (index < state.Document.PayloadMap.Blocks.Length)
        {
            RawPayloadBlock source = state.Document.PayloadMap.Blocks[index];
            if (source.PreserveByteExact && source.Kind != RawPayloadKind.MakerNote && source.Kind != RawPayloadKind.RawImage && FindReplacement(state.Replacements, source) == null)
            {
                RawPayloadBlock? target = FindPath(reopened.PayloadMap, source.Kind, source.Path);
                if (target == null || !SameBytes(state.Document.ExtractPayload(source), reopened.ExtractPayload(target)))
                {
                    throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "A mapped CR2 payload was not preserved byte for byte.", source.Offset);
                }
            }
            index++;
        }
    }

    internal static RawPayloadBlock? FindPath(RawPayloadMap map, RawPayloadKind kind, string path)
    {
        int index = 0;
        while (index < map.Blocks.Length)
        {
            RawPayloadBlock block = map.Blocks[index];
            if (block.Kind == kind && block.Path == path)
            {
                return block;
            }
            index++;
        }
        return null;
    }

    internal static bool SameBytes(byte[] first, byte[] second)
    {
        if (first == null || second == null || first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index++;
        }
        return true;
    }
}
