using System;

namespace raw;

internal static class MetadataEditor
{
    internal static RawMetadataEditReport Apply(RawMetadata metadata, RawMetadataEditSet editSet, bool dryRun)
    {
        return ApplyCore(null, metadata, editSet, dryRun);
    }

    internal static RawMetadataEditReport Apply(RawDocument document, RawMetadataEditSet editSet, bool dryRun)
    {
        RawMetadata? metadata = document == null ? null : document.Metadata;
        return ApplyCore(document, metadata, editSet, dryRun);
    }

    internal static RawMetadataEditReport ApplyCore(RawDocument? document, RawMetadata? metadata, RawMetadataEditSet editSet, bool dryRun)
    {
        System.Collections.Generic.List<RawMetadataChange> changes = new System.Collections.Generic.List<RawMetadataChange>();
        System.Collections.Generic.List<RawDiagnostic> diagnostics = new System.Collections.Generic.List<RawDiagnostic>();
        if (metadata == null || editSet == null || editSet.Edits == null || editSet.PreviewEdits == null)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.InvalidArgument, "A metadata model and complete edit set are required.", "MetadataEdit"));
            return new RawMetadataEditReport(dryRun, false, changes.ToArray(), diagnostics.ToArray());
        }
        System.Collections.Generic.List<RawMetadataField> working = CloneFields(metadata.SourceFields == null || metadata.SourceFields.Length == 0 ? metadata.UnifiedFields : metadata.SourceFields);
        int editIndex = 0;
        while (editIndex < editSet.Edits.Length)
        {
            RawMetadataEdit edit = editSet.Edits[editIndex];
            ApplyFieldEdit(working, edit, changes, diagnostics);
            editIndex++;
        }
        int previewIndex = 0;
        while (previewIndex < editSet.PreviewEdits.Length)
        {
            ValidatePreview(document, editSet.PreviewEdits[previewIndex], changes, diagnostics);
            previewIndex++;
        }
        if (HasErrors(diagnostics))
        {
            return new RawMetadataEditReport(dryRun, false, changes.ToArray(), diagnostics.ToArray());
        }
        if (!dryRun)
        {
            RawMetadataOptions options = CurrentOptions(metadata);
            MetadataReconciler.Resolve(metadata, working, options);
            AppendPendingEdits(metadata, editSet.Edits);
            previewIndex = 0;
            while (previewIndex < editSet.PreviewEdits.Length)
            {
                CommitPreview(document, editSet.PreviewEdits[previewIndex]);
                previewIndex++;
            }
            if (document != null)
            {
                AppendPendingPreviews(document, editSet.PreviewEdits);
            }
            int changeIndex = 0;
            while (changeIndex < changes.Count)
            {
                changes[changeIndex].Applied = true;
                changeIndex++;
            }
        }
        return new RawMetadataEditReport(dryRun, !dryRun, changes.ToArray(), diagnostics.ToArray());
    }

    internal static void ApplyPending(System.Collections.Generic.List<RawMetadataField> fields, RawMetadataEdit[] edits)
    {
        System.Collections.Generic.List<RawMetadataChange> changes = new System.Collections.Generic.List<RawMetadataChange>();
        System.Collections.Generic.List<RawDiagnostic> diagnostics = new System.Collections.Generic.List<RawDiagnostic>();
        int index = 0;
        while (index < edits.Length)
        {
            ApplyFieldEdit(fields, edits[index], changes, diagnostics);
            index++;
        }
        if (HasErrors(diagnostics))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Pending metadata edits are inconsistent with the preserved source representations.", -1);
        }
    }

    internal static void ApplyFieldEdit(System.Collections.Generic.List<RawMetadataField> fields, RawMetadataEdit edit, System.Collections.Generic.List<RawMetadataChange> changes, System.Collections.Generic.List<RawDiagnostic> diagnostics)
    {
        if (!ValidEdit(edit, diagnostics))
        {
            return;
        }
        string? language = edit.Language == "x-default" ? string.Empty : edit.Language;
        string[] values = NormalizeValues(edit.Name, edit.Values);
        System.Collections.Generic.List<RawMetadataField> matches = Matches(fields, edit.Name, language, edit.Source);
        if (edit.Kind == RawMetadataEditKind.Add)
        {
            if (matches.Count != 0)
            {
                diagnostics.Add(Diagnostic(RawDiagnosticCode.InvalidMetadata, "An add operation cannot replace an existing metadata field.", Location(edit.Source, edit.Name, language)));
                return;
            }
            RawMetadataField added = new RawMetadataField(edit.Name, values, language, edit.Source, -1, "PendingEdit", -1);
            fields.Add(added);
            changes.Add(new RawMetadataChange(RawMetadataEditKind.Add, Location(edit.Source, edit.Name, language), edit.Source, null, values, false));
            return;
        }
        if (edit.Kind == RawMetadataEditKind.Replace)
        {
            if (matches.Count == 0)
            {
                diagnostics.Add(Diagnostic(RawDiagnosticCode.InvalidMetadata, "A replace operation requires an existing metadata field.", Location(edit.Source, edit.Name, language)));
                return;
            }
            int matchIndex = 0;
            while (matchIndex < matches.Count)
            {
                RawMetadataField field = matches[matchIndex];
                if (!MetadataReconciler.SameValues(field.Values, values, edit.Name == "Keywords" || edit.Name == "SupplementalCategories"))
                {
                    changes.Add(new RawMetadataChange(RawMetadataEditKind.Replace, Location(field.Source, field.Name, field.Language), field.Source, field.Values, values, false));
                    field.Values = RawMetadataField.Copy(values);
                    field.Offset = -1;
                    field.Path = "PendingEdit";
                    field.Tag = -1;
                }
                matchIndex++;
            }
            return;
        }
        int removeIndex = 0;
        while (removeIndex < matches.Count)
        {
            RawMetadataField field = matches[removeIndex];
            changes.Add(new RawMetadataChange(RawMetadataEditKind.Remove, Location(field.Source, field.Name, field.Language), field.Source, field.Values, null, false));
            fields.Remove(field);
            removeIndex++;
        }
    }

    internal static bool ValidEdit(RawMetadataEdit edit, System.Collections.Generic.List<RawDiagnostic> diagnostics)
    {
        if (edit == null || edit.Name == null || edit.Name.Length == 0)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.InvalidArgument, "Every metadata edit requires a nonempty field name.", "MetadataEdit"));
            return false;
        }
        if (edit.Kind < RawMetadataEditKind.Add || edit.Kind > RawMetadataEditKind.Remove)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.InvalidArgument, "The metadata field edit kind is not defined.", "MetadataEdit/" + edit.Name));
            return false;
        }
        if (edit.Source < RawMetadataSource.None || edit.Source > RawMetadataSource.Iptc || edit.Kind == RawMetadataEditKind.Add && edit.Source == RawMetadataSource.None)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.InvalidArgument, "An add requires one defined target representation; replace and remove may target every source.", "MetadataEdit/" + edit.Name));
            return false;
        }
        if (edit.Kind != RawMetadataEditKind.Remove)
        {
            if (edit.Values == null || edit.Values.Length == 0)
            {
                diagnostics.Add(Diagnostic(RawDiagnosticCode.InvalidArgument, "Add and replace operations require at least one value.", "MetadataEdit/" + edit.Name));
                return false;
            }
            int valueIndex = 0;
            while (valueIndex < edit.Values.Length)
            {
                if (edit.Values[valueIndex] == null)
                {
                    diagnostics.Add(Diagnostic(RawDiagnosticCode.InvalidArgument, "Metadata replacement values cannot be null.", "MetadataEdit/" + edit.Name));
                    return false;
                }
                valueIndex++;
            }
        }
        return true;
    }

    internal static string[] NormalizeValues(string name, string[] values)
    {
        if (values == null)
        {
            return System.Array.Empty<string>();
        }
        System.Collections.Generic.List<string> normalized = new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < values.Length)
        {
            string value = MetadataReconciler.Normalize(name, values[index]);
            if (!Contains(normalized, value))
            {
                normalized.Add(value);
            }
            index++;
        }
        return normalized.ToArray();
    }

    internal static System.Collections.Generic.List<RawMetadataField> Matches(System.Collections.Generic.List<RawMetadataField> fields, string name, string? language, RawMetadataSource source)
    {
        System.Collections.Generic.List<RawMetadataField> matches = new System.Collections.Generic.List<RawMetadataField>();
        int index = 0;
        while (index < fields.Count)
        {
            RawMetadataField field = fields[index];
            bool languageMatches = language == null || field.Language == language;
            bool sourceMatches = source == RawMetadataSource.None || field.Source == source;
            if (field.Name == name && languageMatches && sourceMatches)
            {
                matches.Add(field);
            }
            index++;
        }
        return matches;
    }

    internal static System.Collections.Generic.List<RawMetadataField> CloneFields(RawMetadataField[] fields)
    {
        System.Collections.Generic.List<RawMetadataField> result = new System.Collections.Generic.List<RawMetadataField>();
        if (fields == null)
        {
            return result;
        }
        int index = 0;
        while (index < fields.Length)
        {
            RawMetadataField field = fields[index];
            if (field != null)
            {
                result.Add(new RawMetadataField(field.Name, field.Values, field.Language, field.Source, field.Offset, field.Path, field.Tag));
            }
            index++;
        }
        return result;
    }

    internal static RawMetadataOptions CurrentOptions(RawMetadata metadata)
    {
        RawMetadataOptions options = new RawMetadataOptions();
        options.Reconcile = metadata.Reconciled;
        options.Precedence = metadata.Reconciled ? metadata.Precedence : RawMetadataPrecedence.None;
        return options;
    }

    internal static void ValidatePreview(RawDocument? document, RawPreviewMetadataEdit edit, System.Collections.Generic.List<RawMetadataChange> changes, System.Collections.Generic.List<RawDiagnostic> diagnostics)
    {
        if (document == null || document.Thumbnails == null || edit == null || edit.Index < 0 || edit.Index >= document.Thumbnails.Length || document.Thumbnails[edit.Index] == null)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.InvalidArgument, "A preview replacement requires an existing thumbnail collection index.", "Preview"));
            return;
        }
        RawLimits limits = new RawLimits();
        if (edit.Kind <= RawThumbnailKind.Unknown || edit.Kind > RawThumbnailKind.Opaque || edit.Width <= 0 || edit.Height <= 0 || edit.Width > limits.MaxWidth || edit.Height > limits.MaxHeight || edit.Orientation < RawOrientation.Unknown || edit.Orientation > RawOrientation.Rotate270Clockwise || edit.MimeType == null || edit.MimeType.Length == 0 || edit.EncodedBytes == null || edit.EncodedBytes.Length == 0 || edit.EncodedBytes.LongLength > limits.MaxPreviewBytes || edit.EncodedBytes.LongLength > limits.MaxAllocationBytes)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.InvalidArgument, "Preview replacement representation, geometry, orientation, media type, or bytes are invalid.", PreviewLocation(edit.Index)));
            return;
        }
        long pixels = (long)edit.Width * edit.Height;
        if (edit.Kind == RawThumbnailKind.Rgb8 && edit.EncodedBytes.LongLength != pixels * 3 || edit.Kind == RawThumbnailKind.Gray8 && edit.EncodedBytes.LongLength != pixels)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.InvalidLength, "Uncompressed preview byte length does not match its replacement geometry.", PreviewLocation(edit.Index)));
            return;
        }
        RawThumbnail original = document.Thumbnails[edit.Index];
        changes.Add(new RawMetadataChange(RawMetadataEditKind.PreviewUpdate, PreviewLocation(edit.Index), RawMetadataSource.None, PreviewValues(original), PreviewValues(edit), false));
    }

    internal static void CommitPreview(RawDocument? document, RawPreviewMetadataEdit edit)
    {
        RawThumbnail thumbnail = (document ?? throw new System.InvalidOperationException("A required value is unavailable.")).Thumbnails[edit.Index];
        RawPayloadBlock? originalPayload = thumbnail.Payload;
        thumbnail.Kind = edit.Kind;
        thumbnail.Width = edit.Width;
        thumbnail.Height = edit.Height;
        thumbnail.Orientation = edit.Orientation;
        thumbnail.MimeType = edit.MimeType;
        thumbnail.EncodedBytes = RawTextValue.Copy(edit.EncodedBytes);
        thumbnail.Payload = null;
        int imageIndex = AssociatedImage(document, edit.Index, originalPayload);
        if (imageIndex >= 0)
        {
            RawImageInfo info = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable."))[imageIndex];
            info.Width = edit.Width;
            info.Height = edit.Height;
            info.Orientation = edit.Orientation;
            info.Payload = null;
            if (edit.Kind == RawThumbnailKind.Gray8)
            {
                info.BitDepth = 8;
                info.SampleKind = RawSampleKind.UnsignedInteger;
                info.Photometric = RawPhotometricInterpretation.BlackIsZero;
                info.Compression = RawCompression.None;
                info.Channels = new RawChannelDescription[] { new RawChannelDescription("Y", RawChannelKind.Gray) };
                info.Planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "Gray", RawChannelKind.Gray, edit.Width, edit.Height, 8, edit.Width) };
            }
            else if (edit.Kind == RawThumbnailKind.Rgb8)
            {
                info.BitDepth = 8;
                info.SampleKind = RawSampleKind.UnsignedInteger;
                info.Photometric = RawPhotometricInterpretation.Rgb;
                info.Compression = RawCompression.None;
                info.Channels = new RawChannelDescription[] { new RawChannelDescription("R", RawChannelKind.Red), new RawChannelDescription("G", RawChannelKind.Green), new RawChannelDescription("B", RawChannelKind.Blue) };
                info.Planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "RGB", RawChannelKind.Unknown, edit.Width, edit.Height, 8, edit.Width * 3) };
            }
            else if (edit.Kind == RawThumbnailKind.Jpeg)
            {
                info.Compression = RawCompression.BaselineJpeg;
                UpdatePlaneGeometry(info, edit.Width, edit.Height);
            }
            else
            {
                info.Compression = RawCompression.Unknown;
                UpdatePlaneGeometry(info, edit.Width, edit.Height);
            }
            if (info.StoredArea != null)
            {
                info.StoredArea.Width = edit.Width;
                info.StoredArea.Height = edit.Height;
            }
        }
        string ifdPath = PayloadIfdPath(originalPayload);
        if (ifdPath.Length > 0 && document.Metadata != null)
        {
            RawIfd? ifd = document.Metadata.FindIfd(ifdPath);
            if (ifd != null)
            {
                SetUnsignedTag(ifd, 256, "ImageWidth", (ulong)edit.Width);
                SetUnsignedTag(ifd, 257, "ImageLength", (ulong)edit.Height);
                SetUnsignedTag(ifd, 274, "Orientation", (ulong)edit.Orientation);
                if (edit.Kind == RawThumbnailKind.Gray8)
                {
                    SetUnsignedTag(ifd, 258, "BitsPerSample", 8);
                    SetUnsignedTag(ifd, 259, "Compression", 1);
                    SetUnsignedTag(ifd, 262, "PhotometricInterpretation", 1);
                    SetUnsignedTag(ifd, 277, "SamplesPerPixel", 1);
                }
                else if (edit.Kind == RawThumbnailKind.Rgb8)
                {
                    SetShortArrayTag(ifd, 258, "BitsPerSample", new ushort[] { 8, 8, 8 });
                    SetUnsignedTag(ifd, 259, "Compression", 1);
                    SetUnsignedTag(ifd, 262, "PhotometricInterpretation", 2);
                    SetUnsignedTag(ifd, 277, "SamplesPerPixel", 3);
                }
                else if (edit.Kind == RawThumbnailKind.Jpeg)
                {
                    SetUnsignedTag(ifd, 259, "Compression", 7);
                }
            }
        }
    }

    internal static void UpdatePlaneGeometry(RawImageInfo info, int width, int height)
    {
        int index = 0;
        while (info.Planes != null && index < info.Planes.Length)
        {
            RawPlaneInfo plane = info.Planes[index];
            if (plane != null)
            {
                plane.Width = width;
                plane.Height = height;
                int channelCount = info.GetChannelCount();
                plane.RowStrideSamples = width * (channelCount <= 0 ? 1 : channelCount);
            }
            index++;
        }
    }

    internal static int AssociatedImage(RawDocument document, int thumbnailIndex, RawPayloadBlock? payload)
    {
        if (payload != null && payload.ImageIndex >= 0 && document.Images != null)
        {
            int index = 0;
            while (index < document.Images.Length)
            {
                if (document.Images[index] != null && document.Images[index].Index == payload.ImageIndex)
                {
                    return index;
                }
                index++;
            }
        }
        int previewIndex = 0;
        int imageIndex = 0;
        while (document.Images != null && imageIndex < document.Images.Length)
        {
            RawImageInfo info = document.Images[imageIndex];
            if (info != null && info.Kind != RawImageKind.Raw && info.Kind != RawImageKind.Mask)
            {
                if (previewIndex == thumbnailIndex)
                {
                    return imageIndex;
                }
                previewIndex++;
            }
            imageIndex++;
        }
        return -1;
    }

    internal static void SetUnsignedTag(RawIfd ifd, ushort id, string name, ulong value)
    {
        RawTag? tag = ifd.FindTag(id);
        RawByteOrder byteOrder = tag == null || tag.Value == null ? RawByteOrder.LittleEndian : tag.Value.ByteOrder;
        RawTagValue encoded;
        if ((tag != null && (tag.Value ?? throw new System.InvalidOperationException("A required value is unavailable.")).Type == RawTagType.Short || tag == null && id != 256 && id != 257) && value <= ushort.MaxValue)
        {
            encoded = RawTagValue.CreateUInt16(new ushort[] { (ushort)value }, byteOrder);
        }
        else if (tag != null && (tag.Value ?? throw new System.InvalidOperationException("A required value is unavailable.")).Type == RawTagType.Long8)
        {
            encoded = RawTagValue.CreateUInt64(new ulong[] { value }, byteOrder);
        }
        else
        {
            encoded = RawTagValue.CreateUInt32(new uint[] { (uint)value }, byteOrder);
        }
        if (tag != null)
        {
            tag.Value = encoded;
            return;
        }
        RawTag[] tags = new RawTag[ifd.Tags.Length + 1];
        ifd.Tags.AsSpan().CopyTo(tags);
        tags[tags.Length - 1] = new RawTag(id, name, ifd.Path, -1, true, encoded);
        ifd.Tags = tags;
    }

    internal static void SetShortArrayTag(RawIfd ifd, ushort id, string name, ushort[] values)
    {
        RawTag? tag = ifd.FindTag(id);
        RawByteOrder byteOrder = tag == null || tag.Value == null ? RawByteOrder.LittleEndian : tag.Value.ByteOrder;
        RawTagValue encoded = RawTagValue.CreateUInt16(values, byteOrder);
        if (tag != null)
        {
            tag.Value = encoded;
            return;
        }
        RawTag[] tags = new RawTag[ifd.Tags.Length + 1];
        ifd.Tags.AsSpan().CopyTo(tags);
        tags[tags.Length - 1] = new RawTag(id, name, ifd.Path, -1, true, encoded);
        ifd.Tags = tags;
    }

    internal static void AppendPendingEdits(RawMetadata metadata, RawMetadataEdit[] edits)
    {
        if (edits.Length == 0)
        {
            return;
        }
        RawMetadataEdit[] current = metadata.PendingEdits == null ? System.Array.Empty<RawMetadataEdit>() : metadata.PendingEdits;
        RawMetadataEdit[] result = new RawMetadataEdit[current.Length + edits.Length];
        current.AsSpan().CopyTo(result);
        int index = 0;
        while (index < edits.Length)
        {
            RawMetadataEdit edit = edits[index];
            result[current.Length + index] = edit == null ? throw new System.InvalidOperationException("A validated metadata edit cannot be null.") : new RawMetadataEdit(edit.Kind, edit.Name, edit.Values, edit.Language, edit.Source);
            index++;
        }
        metadata.PendingEdits = result;
    }

    internal static void AppendPendingPreviews(RawDocument document, RawPreviewMetadataEdit[] edits)
    {
        if (edits.Length == 0)
        {
            return;
        }
        RawPreviewMetadataEdit[] current = document.PendingPreviewEdits == null ? System.Array.Empty<RawPreviewMetadataEdit>() : document.PendingPreviewEdits;
        RawPreviewMetadataEdit[] result = new RawPreviewMetadataEdit[current.Length + edits.Length];
        current.AsSpan().CopyTo(result);
        int index = 0;
        while (index < edits.Length)
        {
            RawPreviewMetadataEdit edit = edits[index];
            result[current.Length + index] = new RawPreviewMetadataEdit(edit.Index, edit.Kind, edit.Width, edit.Height, edit.Orientation, edit.MimeType, edit.EncodedBytes);
            index++;
        }
        document.PendingPreviewEdits = result;
    }

    internal static void AppendScrub(RawMetadataEditSet editSet, RawPrivacyScrubPreset preset)
    {
        if (preset < RawPrivacyScrubPreset.Location || preset > RawPrivacyScrubPreset.All)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The privacy scrub preset is not defined.", -1);
        }
        if (preset == RawPrivacyScrubPreset.Location || preset == RawPrivacyScrubPreset.All)
        {
            AppendNames(editSet, new string[] { "GPSLatitude", "GPSLongitude", "GPSAltitude", "GPSTimeStamp", "GPSDateStamp", "City", "State", "Country", "CountryCode", "SubLocation", "Location", "LocationCreated", "LocationShown" });
        }
        if (preset == RawPrivacyScrubPreset.Personal || preset == RawPrivacyScrubPreset.All)
        {
            AppendNames(editSet, new string[] { "Creator", "Artist", "BylineTitle", "WriterEditor", "Copyright", "Credit", "Source", "Instructions", "UserComment", "Contact", "OwnerName", "CameraOwnerName" });
        }
        if (preset == RawPrivacyScrubPreset.Device || preset == RawPrivacyScrubPreset.All)
        {
            AppendNames(editSet, new string[] { "Make", "Model", "LensMake", "LensModel", "SerialNumber", "BodySerialNumber", "LensSerialNumber", "Software", "OriginatingProgram", "ImageUniqueID", "DocumentID", "InstanceID", "OriginalDocumentID" });
        }
        if (preset == RawPrivacyScrubPreset.All)
        {
            AppendNames(editSet, new string[] { "DateCreated", "CreateDate", "ModifyDate" });
        }
    }

    internal static void AppendNames(RawMetadataEditSet editSet, string[] names)
    {
        int index = 0;
        while (index < names.Length)
        {
            editSet.AppendEdit(new RawMetadataEdit(RawMetadataEditKind.Remove, names[index], null, null, RawMetadataSource.None));
            index++;
        }
    }

    internal static string[] PreviewValues(RawThumbnail thumbnail)
    {
        return new string[] { thumbnail.Kind.ToString(), thumbnail.Width.ToString(System.Globalization.CultureInfo.InvariantCulture), thumbnail.Height.ToString(System.Globalization.CultureInfo.InvariantCulture), thumbnail.Orientation.ToString(), thumbnail.MimeType, thumbnail.EncodedBytes.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) };
    }

    internal static string[] PreviewValues(RawPreviewMetadataEdit edit)
    {
        return new string[] { edit.Kind.ToString(), edit.Width.ToString(System.Globalization.CultureInfo.InvariantCulture), edit.Height.ToString(System.Globalization.CultureInfo.InvariantCulture), edit.Orientation.ToString(), edit.MimeType, edit.EncodedBytes.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) };
    }

    internal static string PayloadIfdPath(RawPayloadBlock? payload)
    {
        if (payload == null || payload.Path == null)
        {
            return string.Empty;
        }
        int slash = payload.Path.IndexOf('/', System.StringComparison.Ordinal);
        return slash < 0 ? string.Empty : payload.Path.Substring(0, slash);
    }

    internal static string Location(RawMetadataSource source, string name, string? language)
    {
        string result = "Metadata/" + source.ToString() + "/" + name;
        if (language != null && language.Length > 0)
        {
            result += "[" + language + "]";
        }
        return result;
    }

    internal static string PreviewLocation(int index)
    {
        return "Preview/" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static bool Contains(System.Collections.Generic.List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool HasErrors(System.Collections.Generic.List<RawDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index].Severity == RawSeverity.Error)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static RawDiagnostic Diagnostic(RawDiagnosticCode code, string message, string location)
    {
        return new RawDiagnostic(code, RawSeverity.Error, RawFormat.Unknown, -1, location, -1, "MetadataEdit", message, false);
    }
}
