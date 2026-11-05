using System;

namespace raw;

internal static class MetadataReconciler
{
    internal static void Apply(RawMetadata metadata, RawMetadataOptions options)
    {
        if (metadata == null || options == null || !options.Validate().IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Valid metadata and reconciliation options are required.", -1);
        }
        options.CheckCancellation();
        System.Collections.Generic.List<RawMetadataField> candidates = new System.Collections.Generic.List<RawMetadataField>();
        if (options.ReadExif || options.ReadGps)
        {
            AddExif(candidates, metadata);
        }
        if (options.ReadXmp)
        {
            AddXmp(candidates, metadata.XmpPackets, options);
        }
        if (options.ReadIptc)
        {
            AddIptc(candidates, metadata.IptcRecords, options);
        }
        if (metadata.PendingEdits != null && metadata.PendingEdits.Length > 0)
        {
            MetadataEditor.ApplyPending(candidates, metadata.PendingEdits);
        }
        Resolve(metadata, candidates, options);
    }

    internal static void Resolve(RawMetadata metadata, System.Collections.Generic.List<RawMetadataField> candidates, RawMetadataOptions options)
    {
        System.Collections.Generic.List<RawMetadataField> unified = new System.Collections.Generic.List<RawMetadataField>();
        System.Collections.Generic.List<RawMetadataConflict> conflicts = new System.Collections.Generic.List<RawMetadataConflict>();
        int candidateIndex = 0;
        while (candidateIndex < candidates.Count)
        {
            options.CheckCancellation();
            RawMetadataField candidate = candidates[candidateIndex];
            if (FirstGroupIndex(candidates, candidate.Name, candidate.Language) == candidateIndex)
            {
                RawMetadataField[] group = GetGroup(candidates, candidate.Name, candidate.Language);
                bool conflict = HasDifferentValues(group, candidate.Name);
                RawMetadataField selected = SelectCandidate(group, options.Reconcile ? options.Precedence : RawMetadataPrecedence.None);
                if (conflict)
                {
                    bool resolved = options.Reconcile;
                    conflicts.Add(new RawMetadataConflict(candidate.Name, candidate.Language, group, resolved, resolved ? selected.Source : RawMetadataSource.None, resolved ? selected.Values : null));
                    if (resolved)
                    {
                        unified.Add(selected);
                    }
                }
                else
                {
                    unified.Add(selected);
                }
            }
            candidateIndex++;
        }
        metadata.UnifiedFields = unified.ToArray();
        metadata.SourceFields = candidates.ToArray();
        metadata.Conflicts = conflicts.ToArray();
        metadata.Reconciled = options.Reconcile;
        metadata.Precedence = options.Reconcile ? options.Precedence : RawMetadataPrecedence.None;
    }

    internal static void AddExif(System.Collections.Generic.List<RawMetadataField> fields, RawMetadata metadata)
    {
        AddExifText(fields, metadata, "Make", metadata.Make, "IFD0", 271);
        AddExifText(fields, metadata, "Model", metadata.Model, "IFD0", 272);
        AddExifText(fields, metadata, "LensMake", metadata.LensMake, string.Empty, 42035);
        AddExifText(fields, metadata, "LensModel", metadata.LensModel, string.Empty, 42036);
        AddExifText(fields, metadata, "Software", ExifCodec.GetText(metadata, "IFD0", 305), "IFD0", 305);
        AddExifText(fields, metadata, "Artist", ExifCodec.GetText(metadata, "IFD0", 315), "IFD0", 315);
        AddExifText(fields, metadata, "Copyright", ExifCodec.GetText(metadata, "IFD0", 33432), "IFD0", 33432);
        AddExifText(fields, metadata, "CameraOwnerName", ExifCodec.GetFirstText(metadata, RawIfdKind.Exif, 42032), string.Empty, 42032);
        AddExifText(fields, metadata, "BodySerialNumber", ExifCodec.GetFirstText(metadata, RawIfdKind.Exif, 42033), string.Empty, 42033);
        AddExifText(fields, metadata, "LensSerialNumber", ExifCodec.GetFirstText(metadata, RawIfdKind.Exif, 42037), string.Empty, 42037);
        AddExifText(fields, metadata, "ImageUniqueID", ExifCodec.GetFirstText(metadata, RawIfdKind.Exif, 42016), string.Empty, 42016);
        if (metadata.UserComment != null && metadata.UserComment.Valid)
        {
            RawTag? tag = ExifCodec.FindTag(metadata, RawIfdKind.Exif, 37510);
            Add(fields, "UserComment", metadata.UserComment.Text, string.Empty, RawMetadataSource.Exif, tag == null ? -1 : tag.EntryOffset, tag == null ? string.Empty : tag.IfdPath, 37510);
        }
        AddExifTime(fields, metadata, "ModifyDate", metadata.ModifiedTime, 306);
        AddExifTime(fields, metadata, "DateCreated", metadata.OriginalTime, 36867);
        AddExifTime(fields, metadata, "CreateDate", metadata.DigitizedTime, 36868);
        if (metadata.GpsLocation != null)
        {
            if (metadata.GpsLocation.HasCoordinates)
            {
                Add(fields, "GPSLatitude", metadata.GpsLocation.Latitude.ToString("R", System.Globalization.CultureInfo.InvariantCulture), string.Empty, RawMetadataSource.Exif, -1, "GPS", 2);
                Add(fields, "GPSLongitude", metadata.GpsLocation.Longitude.ToString("R", System.Globalization.CultureInfo.InvariantCulture), string.Empty, RawMetadataSource.Exif, -1, "GPS", 4);
            }
            if (metadata.GpsLocation.HasAltitude)
            {
                Add(fields, "GPSAltitude", metadata.GpsLocation.Altitude.ToString("R", System.Globalization.CultureInfo.InvariantCulture), string.Empty, RawMetadataSource.Exif, -1, "GPS", 6);
            }
        }
    }

    internal static void AddExifText(System.Collections.Generic.List<RawMetadataField> fields, RawMetadata metadata, string name, string value, string path, ushort tagId)
    {
        if (value == null || value.Length == 0)
        {
            return;
        }
        RawTag? tag = path.Length == 0 ? ExifCodec.FindTag(metadata, RawIfdKind.Exif, tagId) : FindTag(metadata, path, tagId);
        Add(fields, name, value, string.Empty, RawMetadataSource.Exif, tag == null ? -1 : tag.EntryOffset, tag == null ? path : tag.IfdPath, tagId);
    }

    internal static void AddExifTime(System.Collections.Generic.List<RawMetadataField> fields, RawMetadata metadata, string name, RawExifTimestamp? timestamp, ushort tagId)
    {
        if (timestamp == null || !timestamp.Valid)
        {
            return;
        }
        string value = timestamp.DateTimeText;
        if (timestamp.Subseconds.Length > 0)
        {
            value += "." + timestamp.Subseconds;
        }
        value += timestamp.OffsetText;
        RawTag? tag = tagId == 306 ? FindTag(metadata, "IFD0", tagId) : ExifCodec.FindTag(metadata, RawIfdKind.Exif, tagId);
        Add(fields, name, value, string.Empty, RawMetadataSource.Exif, tag == null ? -1 : tag.EntryOffset, tag == null ? string.Empty : tag.IfdPath, tagId);
    }

    internal static void AddXmp(System.Collections.Generic.List<RawMetadataField> fields, RawXmpPacket[] packets, RawMetadataOptions options)
    {
        if (packets == null)
        {
            return;
        }
        int packetIndex = 0;
        while (packetIndex < packets.Length)
        {
            options.CheckCancellation();
            RawXmpPacket packet = packets[packetIndex];
            if (packet != null && packet.Fields != null)
            {
                int fieldIndex = 0;
                while (fieldIndex < packet.Fields.Length)
                {
                    RawXmpField field = packet.Fields[fieldIndex];
                    string name = XmpName(field);
                    if (name.Length > 0)
                    {
                        Add(fields, name, field.Value, field.Language, RawMetadataSource.Xmp, packet.Offset, field.Path, -1);
                    }
                    fieldIndex++;
                }
            }
            packetIndex++;
        }
    }

    internal static void AddIptc(System.Collections.Generic.List<RawMetadataField> fields, RawIptcRecord[] records, RawMetadataOptions options)
    {
        if (records == null)
        {
            return;
        }
        int recordIndex = 0;
        while (recordIndex < records.Length)
        {
            options.CheckCancellation();
            RawIptcRecord record = records[recordIndex];
            if (record != null && record.Datasets != null)
            {
                int datasetIndex = 0;
                while (datasetIndex < record.Datasets.Length)
                {
                    RawIptcDataset dataset = record.Datasets[datasetIndex];
                    string name = IptcName(dataset);
                    if (name.Length > 0 && dataset.Valid && (dataset.Text.Length > 0 || dataset.ValueKind == RawIptcValueKind.UnsignedInteger))
                    {
                        string value = IptcValue(record, dataset, name);
                        Add(fields, name, value, string.Empty, RawMetadataSource.Iptc, record.Offset, record.IfdPath, dataset.RecordNumber * 256 + dataset.DatasetNumber);
                    }
                    datasetIndex++;
                }
            }
            recordIndex++;
        }
    }

    internal static string XmpName(RawXmpField field)
    {
        if (field == null)
        {
            return string.Empty;
        }
        string uri = field.NamespaceUri;
        string name = field.Name;
        if (uri == "http://ns.adobe.com/tiff/1.0/")
        {
            return name == "Make" || name == "Model" || name == "Software" || name == "Artist" || name == "Copyright" ? name : string.Empty;
        }
        if (uri == "http://ns.adobe.com/exif/1.0/")
        {
            if (name == "DateTimeOriginal") return "DateCreated";
            if (name == "DateTimeDigitized") return "CreateDate";
            if (name == "UserComment" || name == "GPSLatitude" || name == "GPSLongitude" || name == "GPSAltitude") return name;
            return string.Empty;
        }
        if (uri == "http://cipa.jp/exif/1.0/" || uri == "http://ns.adobe.com/exif/1.0/aux/")
        {
            if (name == "LensMake" || name == "LensModel" || name == "LensSerialNumber" || name == "SerialNumber" || name == "BodySerialNumber" || name == "CameraOwnerName") return name;
            return string.Empty;
        }
        if (uri == IptcXmpCodec.XmpNamespace)
        {
            if (name == "ModifyDate") return "ModifyDate";
            if (name == "CreateDate") return "CreateDate";
            if (name == "CreatorTool") return "OriginatingProgram";
            return string.Empty;
        }
        if (uri == IptcXmpCodec.DublinCoreNamespace)
        {
            if (name == "title") return "Title";
            if (name == "creator") return "Creator";
            if (name == "description") return "Description";
            if (name == "subject") return "Keywords";
            if (name == "rights") return "Copyright";
            return string.Empty;
        }
        if (uri == "http://ns.adobe.com/xap/1.0/mm/")
        {
            if (name == "DocumentID" || name == "InstanceID" || name == "OriginalDocumentID") return name;
            return string.Empty;
        }
        if (uri == IptcXmpCodec.PhotoshopNamespace)
        {
            return PhotoshopName(name);
        }
        if (uri == IptcXmpCodec.CoreNamespace || uri == IptcXmpCodec.ExtensionNamespace || uri == IptcXmpCodec.PlusNamespace || uri == IptcXmpCodec.XmpRightsNamespace)
        {
            return name;
        }
        return string.Empty;
    }

    internal static string PhotoshopName(string name)
    {
        if (name == "DateCreated") return "DateCreated";
        if (name == "Headline") return "Headline";
        if (name == "City") return "City";
        if (name == "State") return "State";
        if (name == "Country") return "Country";
        if (name == "Credit") return "Credit";
        if (name == "Source") return "Source";
        if (name == "Instructions") return "Instructions";
        if (name == "Category") return "Category";
        if (name == "SupplementalCategories") return "SupplementalCategories";
        if (name == "TransmissionReference") return "TransmissionReference";
        if (name == "Urgency") return "Urgency";
        if (name == "AuthorsPosition") return "BylineTitle";
        if (name == "CaptionWriter") return "WriterEditor";
        return string.Empty;
    }

    internal static string IptcName(RawIptcDataset dataset)
    {
        if (dataset == null || dataset.RecordNumber != 2)
        {
            return string.Empty;
        }
        string name = dataset.Name;
        if (name == "ObjectName") return "Title";
        if (name == "Byline") return "Creator";
        if (name == "CaptionAbstract") return "Description";
        if (name == "CopyrightNotice") return "Copyright";
        if (name == "ProvinceState") return "State";
        if (name == "CountryName") return "Country";
        if (name == "SpecialInstructions") return "Instructions";
        if (name == "SupplementalCategory") return "SupplementalCategories";
        if (name == "OriginalTransmissionReference") return "TransmissionReference";
        if (name == "DigitalCreationDate") return "CreateDate";
        if (name == "TimeCreated" || name == "DigitalCreationTime") return string.Empty;
        return name;
    }

    internal static string IptcValue(RawIptcRecord record, RawIptcDataset dataset, string name)
    {
        if (dataset.ValueKind == RawIptcValueKind.UnsignedInteger)
        {
            return dataset.UnsignedValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        if (dataset.Name == "DateCreated")
        {
            return CombineIptcDateTime(dataset.Text, record.GetDataset(2, 60));
        }
        if (dataset.Name == "DigitalCreationDate")
        {
            return CombineIptcDateTime(dataset.Text, record.GetDataset(2, 63));
        }
        return Normalize(name, dataset.Text);
    }

    internal static string CombineIptcDateTime(string date, RawIptcDataset? time)
    {
        string normalizedDate = Normalize("DateCreated", date);
        if (time == null || !time.Valid || time.Text.Length < 6)
        {
            return normalizedDate;
        }
        string result = normalizedDate + "T" + time.Text.Substring(0, 2) + ":" + time.Text.Substring(2, 2) + ":" + time.Text.Substring(4, 2);
        if (time.Text.Length == 11)
        {
            result += time.Text.Substring(6, 3) + ":" + time.Text.Substring(9, 2);
        }
        return result;
    }

    internal static void Add(System.Collections.Generic.List<RawMetadataField> fields, string name, string value, string language, RawMetadataSource source, long offset, string? path, int tag)
    {
        if (name == null || name.Length == 0 || value == null)
        {
            return;
        }
        string selectedLanguage = language == null || language == "x-default" ? string.Empty : language;
        string normalized = Normalize(name, value);
        RawMetadataField? existing = Find(fields, name, selectedLanguage, source);
        if (existing == null)
        {
            fields.Add(new RawMetadataField(name, new string[] { normalized }, selectedLanguage, source, offset, path, tag));
            return;
        }
        if (!Contains(existing.Values, normalized))
        {
            string[] values = new string[existing.Values.Length + 1];
            existing.Values.AsSpan().CopyTo(values);
            values[values.Length - 1] = normalized;
            existing.Values = values;
        }
    }

    internal static string Normalize(string name, string value)
    {
        string result = value.Trim();
        if ((name == "DateCreated" || name == "CreateDate" || name == "ModifyDate") && result.Length >= 19 && result[4] == ':' && result[7] == ':' && result[10] == ' ')
        {
            result = result.Substring(0, 4) + "-" + result.Substring(5, 2) + "-" + result.Substring(8, 2) + "T" + result.Substring(11);
        }
        if ((name == "DateCreated" || name == "CreateDate") && result.Length == 8 && Digits(result))
        {
            result = result.Substring(0, 4) + "-" + result.Substring(4, 2) + "-" + result.Substring(6, 2);
        }
        return result;
    }

    internal static bool Digits(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static RawMetadataField? Find(System.Collections.Generic.List<RawMetadataField> fields, string name, string language, RawMetadataSource source)
    {
        int index = 0;
        while (index < fields.Count)
        {
            RawMetadataField field = fields[index];
            if (field.Name == name && field.Language == language && field.Source == source)
            {
                return field;
            }
            index++;
        }
        return null;
    }

    internal static RawTag? FindTag(RawMetadata metadata, string path, ushort tagId)
    {
        RawIfd? ifd = metadata.FindIfd(path);
        return ifd == null ? null : ifd.FindTag(tagId);
    }

    internal static int FirstGroupIndex(System.Collections.Generic.List<RawMetadataField> fields, string name, string language)
    {
        int index = 0;
        while (index < fields.Count)
        {
            if (fields[index].Name == name && fields[index].Language == language)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    internal static RawMetadataField[] GetGroup(System.Collections.Generic.List<RawMetadataField> fields, string name, string language)
    {
        System.Collections.Generic.List<RawMetadataField> result = new System.Collections.Generic.List<RawMetadataField>();
        int index = 0;
        while (index < fields.Count)
        {
            RawMetadataField field = fields[index];
            if (field.Name == name && field.Language == language)
            {
                result.Add(field);
            }
            index++;
        }
        return result.ToArray();
    }

    internal static bool HasDifferentValues(RawMetadataField[] fields, string name)
    {
        int index = 1;
        while (index < fields.Length)
        {
            if (!SameValues(fields[0].Values, fields[index].Values, name == "Keywords" || name == "SupplementalCategories"))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool SameValues(string[] first, string[] second, bool unordered)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (unordered)
            {
                if (!Contains(second, first[index]))
                {
                    return false;
                }
            }
            else if (first[index] != second[index])
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool Contains(string[] values, string value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static RawMetadataField SelectCandidate(RawMetadataField[] fields, RawMetadataPrecedence precedence)
    {
        RawMetadataField selected = fields[0];
        int selectedRank = Rank(selected.Source, precedence);
        int index = 1;
        while (index < fields.Length)
        {
            int rank = Rank(fields[index].Source, precedence);
            if (rank < selectedRank)
            {
                selected = fields[index];
                selectedRank = rank;
            }
            index++;
        }
        return selected;
    }

    internal static int Rank(RawMetadataSource source, RawMetadataPrecedence precedence)
    {
        if (precedence == RawMetadataPrecedence.Xmp)
        {
            if (source == RawMetadataSource.Xmp) return 0;
            if (source == RawMetadataSource.Exif) return 1;
            return 2;
        }
        if (precedence == RawMetadataPrecedence.Iptc)
        {
            if (source == RawMetadataSource.Iptc) return 0;
            if (source == RawMetadataSource.Xmp) return 1;
            return 2;
        }
        if (source == RawMetadataSource.Exif) return 0;
        if (source == RawMetadataSource.Xmp) return 1;
        return 2;
    }
}
