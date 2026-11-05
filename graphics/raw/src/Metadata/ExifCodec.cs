namespace raw;

internal static class ExifCodec
{
    internal static RawMetadata Decode(TiffIfdGraph graph, RawLimits? limits)
    {
        RawMetadataOptions options = new RawMetadataOptions();
        options.Limits = limits;
        return Decode(graph, options);
    }

    internal static RawMetadata Decode(TiffIfdGraph graph, RawMetadataOptions options)
    {
        if (graph == null)
        {
            return new RawMetadata(null);
        }
        if (options == null || !options.Validate().IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Valid metadata options are required.", -1);
        }
        RawLimits? limits = options.Limits;
        options.CheckCancellation();
        RawIfd[] ifds = new RawIfd[graph.Ifds.Length];
        int index = 0;
        while (index < graph.Ifds.Length)
        {
            TiffIfd source = graph.Ifds[index];
            RawIfdKind kind = GetIfdKind(source.Path);
            RawTag[] tags = new RawTag[source.Entries.Length];
            int tagIndex = 0;
            while (tagIndex < source.Entries.Length)
            {
                TiffEntry entry = source.Entries[tagIndex];
                string name = RawExifTagNames.GetName(kind, entry.Tag);
                bool known = RawExifTagNames.IsKnown(kind, entry.Tag);
                RawTagValue value = new RawTagValue((RawTagType)(ushort)entry.FieldType, entry.Count, entry.ByteOrder, entry.RawBytes);
                tags[tagIndex] = new RawTag(entry.Tag, name, source.Path, entry.EntryOffset, known, value);
                tagIndex++;
            }
            ifds[index] = new RawIfd(source.Path, kind, source.AbsoluteOffset, tags);
            index++;
        }

        RawMetadata metadata = new RawMetadata(ifds);
        if (options.ReadExif)
        {
            metadata.Make = GetText(metadata, "IFD0", 271);
            metadata.Model = GetText(metadata, "IFD0", 272);
            metadata.LensMake = GetFirstText(metadata, RawIfdKind.Exif, 42035);
            metadata.LensModel = GetFirstText(metadata, RawIfdKind.Exif, 42036);
            metadata.ExifVersion = GetVersion(metadata, 36864);
            metadata.InteroperabilityIndex = GetFirstText(metadata, RawIfdKind.Interoperability, 1);
            metadata.UserComment = GetPrefixedText(metadata, RawIfdKind.Exif, 37510);
            metadata.ModifiedTime = GetTimestamp(metadata, 306, 36880, 37520, false);
            metadata.OriginalTime = GetTimestamp(metadata, 36867, 36881, 37521, true);
            metadata.DigitizedTime = GetTimestamp(metadata, 36868, 36882, 37522, true);
        }
        if (options.ReadGps)
        {
            metadata.GpsLocation = GetGpsLocation(metadata);
        }
        if (options.ReadXmp)
        {
            metadata.XmpPackets = XmpCodec.Discover(graph, limits);
            metadata.IptcCoreFields = IptcXmpCodec.Extract(metadata.XmpPackets, RawIptcSchema.Core);
            metadata.IptcExtensionFields = IptcXmpCodec.Extract(metadata.XmpPackets, RawIptcSchema.Extension);
        }
        if (options.ReadIptc)
        {
            metadata.IptcRecords = IptcCodec.Discover(graph, limits);
        }
        if (options.ReadIcc)
        {
            metadata.IccProfiles = IccCodec.Discover(graph, limits);
        }
        if (options.ReadMakerNotes)
        {
            metadata.MakerNotes = MakerNoteCodec.Discover(graph, metadata, limits);
        }
        MetadataReconciler.Apply(metadata, options);
        return metadata;
    }

    internal static TiffLayoutEntryInput EncodeTag(RawTag tag)
    {
        if (tag == null || tag.Value == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A typed tag is required for Exif encoding.", -1);
        }
        TiffFieldType type = (TiffFieldType)(ushort)tag.Value.Type;
        TiffFieldTypes.GetElementSize(type);
        return new TiffLayoutEntryInput(tag.Id, type, tag.Value.Count, tag.Value.CopyBytes(), null);
    }

    internal static RawIfdKind GetIfdKind(string path)
    {
        if (path == null)
        {
            return RawIfdKind.Unknown;
        }
        if (path.IndexOf("/Interop", System.StringComparison.Ordinal) >= 0)
        {
            return RawIfdKind.Interoperability;
        }
        if (path.IndexOf("/GPS", System.StringComparison.Ordinal) >= 0)
        {
            return RawIfdKind.Gps;
        }
        if (path.IndexOf("/Exif", System.StringComparison.Ordinal) >= 0)
        {
            return RawIfdKind.Exif;
        }
        if (path.IndexOf("/SubIFD", System.StringComparison.Ordinal) >= 0)
        {
            return RawIfdKind.SubIfd;
        }
        if (path.Length >= 3 && path[0] == 'I' && path[1] == 'F' && path[2] == 'D')
        {
            return RawIfdKind.Image;
        }
        return RawIfdKind.Unknown;
    }

    internal static string GetText(RawMetadata metadata, string path, ushort tagId)
    {
        RawIfd? ifd = metadata.FindIfd(path);
        RawTag? tag = ifd == null ? null : ifd.FindTag(tagId);
        return GetText(tag);
    }

    internal static string GetFirstText(RawMetadata metadata, RawIfdKind kind, ushort tagId)
    {
        RawTag? tag = FindTag(metadata, kind, tagId);
        return GetText(tag);
    }

    internal static string GetText(RawTag? tag)
    {
        if (tag == null || tag.Value == null)
        {
            return string.Empty;
        }
        if (tag.Value.Type != RawTagType.Ascii && tag.Value.Type != RawTagType.Utf8)
        {
            return string.Empty;
        }
        return tag.Value.GetText().Text;
    }

    internal static string GetVersion(RawMetadata metadata, ushort tagId)
    {
        RawTag? tag = FindTag(metadata, RawIfdKind.Exif, tagId);
        if (tag == null || tag.Value.Type != RawTagType.Undefined)
        {
            return string.Empty;
        }
        return System.Text.Encoding.ASCII.GetString(tag.Value.RawBytes);
    }

    internal static RawTextValue? GetPrefixedText(RawMetadata metadata, RawIfdKind kind, ushort tagId)
    {
        RawTag? tag = FindTag(metadata, kind, tagId);
        if (tag == null || tag.Value.Type != RawTagType.Undefined)
        {
            return null;
        }
        return tag.Value.GetText();
    }

    internal static RawExifTimestamp? GetTimestamp(RawMetadata metadata, ushort dateTagId, ushort offsetTagId, ushort subsecondTagId, bool dateInExif)
    {
        string date;
        if (dateInExif)
        {
            date = GetFirstText(metadata, RawIfdKind.Exif, dateTagId);
        }
        else
        {
            date = GetText(metadata, "IFD0", dateTagId);
        }
        if (date.Length == 0)
        {
            return null;
        }
        string offset = GetFirstText(metadata, RawIfdKind.Exif, offsetTagId);
        string subseconds = GetFirstText(metadata, RawIfdKind.Exif, subsecondTagId);
        return new RawExifTimestamp(date, offset, subseconds);
    }

    internal static RawGpsLocation? GetGpsLocation(RawMetadata metadata)
    {
        RawTag? latitudeTag = FindTag(metadata, RawIfdKind.Gps, 2);
        RawTag? longitudeTag = FindTag(metadata, RawIfdKind.Gps, 4);
        RawTag? latitudeRefTag = FindTag(metadata, RawIfdKind.Gps, 1);
        RawTag? longitudeRefTag = FindTag(metadata, RawIfdKind.Gps, 3);
        bool hasCoordinates = ValidCoordinate(latitudeTag) && ValidCoordinate(longitudeTag) && latitudeRefTag != null && longitudeRefTag != null;
        double latitude = 0;
        double longitude = 0;
        if (hasCoordinates)
        {
            latitude = ToDegrees((latitudeTag ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetRationalValues());
            longitude = ToDegrees((longitudeTag ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetRationalValues());
            string latitudeRef = GetText(latitudeRefTag);
            string longitudeRef = GetText(longitudeRefTag);
            if (latitudeRef == "S")
            {
                latitude = -latitude;
            }
            else if (latitudeRef != "N")
            {
                hasCoordinates = false;
            }
            if (longitudeRef == "W")
            {
                longitude = -longitude;
            }
            else if (longitudeRef != "E")
            {
                hasCoordinates = false;
            }
            if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            {
                hasCoordinates = false;
            }
        }

        RawTag? altitudeTag = FindTag(metadata, RawIfdKind.Gps, 6);
        RawTag? altitudeRefTag = FindTag(metadata, RawIfdKind.Gps, 5);
        bool hasAltitude = altitudeTag != null && altitudeTag.Value.Type == RawTagType.Rational && altitudeTag.Value.Count == 1 && altitudeRefTag != null && altitudeRefTag.Value.Type == RawTagType.Byte && altitudeRefTag.Value.Count == 1;
        double altitude = 0;
        byte altitudeReference = 0;
        if (hasAltitude)
        {
            RawRational value = (altitudeTag ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetRationalValues()[0];
            if (value.Denominator == 0)
            {
                hasAltitude = false;
            }
            else
            {
                altitude = value.ToDouble();
                altitudeReference = (altitudeRefTag ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetByteValues()[0];
                if (altitudeReference == 1 || altitudeReference == 3)
                {
                    altitude = -altitude;
                }
                else if (altitudeReference != 0 && altitudeReference != 2)
                {
                    hasAltitude = false;
                }
            }
        }
        if (!hasCoordinates && !hasAltitude)
        {
            return null;
        }
        return new RawGpsLocation(hasCoordinates, latitude, longitude, hasAltitude, altitude, altitudeReference);
    }

    internal static bool ValidCoordinate(RawTag? tag)
    {
        if (tag == null || tag.Value.Type != RawTagType.Rational || tag.Value.Count != 3)
        {
            return false;
        }
        RawRational[] values = tag.Value.GetRationalValues();
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Denominator == 0)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static double ToDegrees(RawRational[] values)
    {
        return values[0].ToDouble() + values[1].ToDouble() / 60.0 + values[2].ToDouble() / 3600.0;
    }

    internal static RawTag? FindTag(RawMetadata metadata, RawIfdKind kind, ushort tagId)
    {
        int index = 0;
        while (index < metadata.Ifds.Length)
        {
            RawIfd ifd = metadata.Ifds[index];
            if (ifd != null && ifd.Kind == kind)
            {
                RawTag? tag = ifd.FindTag(tagId);
                if (tag != null)
                {
                    return tag;
                }
            }
            index++;
        }
        return null;
    }
}
