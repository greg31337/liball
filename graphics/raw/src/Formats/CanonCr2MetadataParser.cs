namespace raw;

internal class CanonCr2Extraction
{
    internal RawCanonCr2Metadata? Metadata;
    internal RawReadProfile? Profile;
    internal CanonEarlyRawHeader? EarlyHeader;
    internal RawDiagnostic[] Diagnostics;

    internal CanonCr2Extraction()
    {
        Diagnostics = System.Array.Empty<RawDiagnostic>();
    }
}

internal static class CanonCr2MetadataParser
{
    internal const string Eos6DMarkIiProfile = "canon.eos-6d-mark-ii.cr2.raw-3x2.firmware-1.0.2.read";
    internal const string Eos6DMarkIiMRawProfile = "canon.eos-6d-mark-ii.cr2.mraw-3x2.firmware-1.0.2.read";
    internal const string Eos6DMarkIiSRawProfile = "canon.eos-6d-mark-ii.cr2.sraw-3x2.firmware-1.0.2.read";
    internal const string Eos20dProfile = "canon.eos-20d.cr2.raw-3x2.firmware-2.0.3.read";
    internal const string Eos1dProfile = "canon.eos-1d.tiff.raw-3x2.firmware-1.4.0.read";

    internal static CanonCr2Extraction Parse(System.ReadOnlyMemory<byte> source, RawFormat format, TiffIfdGraph graph, TiffImageDescriptor? rawImage, RawMetadata metadata, RawOpenOptions options)
    {
        CanonCr2Extraction extraction = new CanonCr2Extraction();
        RawMakerNote? note = FindCanonNote(metadata);
        string modelId = GetModelId(note);
        if (modelId.Length == 0 && (metadata == null || metadata.Make.Length == 0 || metadata.Model.Length == 0))
        {
            return extraction;
        }
        RawProfileMatchInput input = CreateMatchInput(format, graph, rawImage, metadata, note, options.AllowExperimentalProfiles);
        RawProfileMatchResult match;
        if (options.ProfileCatalogue == null)
        {
            RawProfileSchemaDocument profiles = RawBuiltInProfileCatalogue.CreateDocument();
            match = RawProfileMatcher.MatchRead(profiles.ReadProfiles, input);
        }
        else
        {
            match = options.ProfileCatalogue.MatchReadProfile(input);
        }
        if (match == null)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The configured profile source returned no Canon match result.", -1);
        }
        if (!match.Matched)
        {
            if (match.Candidates.Length != 0)
            {
                extraction.Diagnostics = ToWarnings(match.Diagnostics);
            }
            return extraction;
        }
        if (note == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The selected Canon profile requires a Canon maker note.", -1);
        }
        RawCanonCr2Metadata result;
        if ((match.ReadProfile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Id == Eos6DMarkIiProfile || match.ReadProfile.Id == Eos6DMarkIiMRawProfile || match.ReadProfile.Id == Eos6DMarkIiSRawProfile)
        {
            result = ExtractEos6DMarkIi(graph, rawImage, metadata, note, match.ReadProfile);
        }
        else if (match.ReadProfile.Id == Eos20dProfile)
        {
            CanonEarlyRawHeader header = InspectDimensionlessRaw(source, rawImage, match.ReadProfile, options);
            result = ExtractEarly(metadata, note, match.ReadProfile, header, true);
            extraction.EarlyHeader = header;
        }
        else if (match.ReadProfile.Id == Eos1dProfile)
        {
            long offset = GetRawDataOffset(note);
            CanonEarlyRawHeader header = CanonEarlyRawLayout.Inspect(source, offset, source.Length - offset, match.ReadProfile, options.Limits, options.CancellationToken);
            result = ExtractEarly(metadata, note, match.ReadProfile, header, false);
            extraction.EarlyHeader = header;
        }
        else
        {
            throw new RawProfileException(RawDiagnosticCode.UnsupportedProfile, "The selected Canon profile does not have reviewed metadata extraction rules.", -1);
        }
        extraction.Metadata = result;
        extraction.Profile = match.ReadProfile;
        return extraction;
    }

    internal static RawProfileMatchInput CreateMatchInput(RawFormat format, TiffIfdGraph graph, TiffImageDescriptor? rawImage, RawMetadata metadata, RawMakerNote? note, bool allowExperimental)
    {
        RawProfileMatchInput input = new RawProfileMatchInput();
        input.Format = format;
        input.Make = metadata == null ? string.Empty : metadata.Make;
        input.Model = metadata == null ? string.Empty : metadata.Model;
        input.CameraId = GetModelId(note);
        input.Firmware = GetFirmware(note);
        input.HasByteOrder = graph != null && graph.Header != null;
        input.ByteOrder = input.HasByteOrder ? (graph ?? throw new System.InvalidOperationException("A required value is unavailable.")).Header.ByteOrder : RawByteOrder.LittleEndian;
        input.HasBigTiff = input.HasByteOrder;
        input.BigTiff = input.HasByteOrder && (graph ?? throw new System.InvalidOperationException("A required value is unavailable.")).Header.BigTiff;
        if (rawImage != null)
        {
            input.Width = ToInt(rawImage.Width);
            input.Height = ToInt(rawImage.Height);
            input.Compression = ToInt(rawImage.CompressionCode);
            input.SamplesPerPixel = rawImage.Ifd.FindEntry(277) == null ? 0 : ToInt(rawImage.SamplesPerPixel);
            input.BitDepth = rawImage.Ifd.FindEntry(258) == null || rawImage.BitsPerSample.Length == 0 ? 0 : ToInt(rawImage.BitsPerSample[0]);
        }
        input.IfdPaths = GetIfdPaths(graph);
        input.Tags = GetTags(graph);
        input.MakerNoteSignature = note == null ? string.Empty : note.Signature == "CanonIfd" ? "Canon" : note.Signature;
        input.AllowExperimental = allowExperimental;
        return input;
    }

    internal static RawCanonCr2Metadata ExtractEos6DMarkIi(TiffIfdGraph graph, TiffImageDescriptor? rawImage, RawMetadata metadata, RawMakerNote note, RawReadProfile profile)
    {
        int[] sensor = GetSignedWords(RequireTag(note, 0x00e0, "SensorInfo"));
        int[] settings = GetSignedWords(RequireTag(note, 0x0001, "CameraSettings"));
        ushort[] color = GetUnsignedWords(RequireTag(note, 0x4001, "ColorData"));
        if (sensor.Length <= 12 || settings.Length <= 3 || color.Length <= 0x30e)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The Canon maker-note sensor, camera-settings, or color-data block is truncated.", note.Offset);
        }
        int sensorWidth = sensor[1];
        int sensorHeight = sensor[2];
        int left = sensor[5];
        int top = sensor[6];
        int right = sensor[7];
        int bottom = sensor[8];
        if (sensorWidth != profile.Geometry.StoredWidth || sensorHeight != profile.Geometry.StoredHeight || left < 0 || top < 0 || right < left || bottom < top || right >= sensorWidth || bottom >= sensorHeight)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Canon SensorInfo does not satisfy the selected profile geometry.", note.Offset);
        }
        RawArea active = new RawArea(RawAreaKind.Active, left, top, right - left + 1, bottom - top + 1);
        if (!SameArea(active, profile.Geometry.ActiveArea))
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Canon SensorInfo active borders disagree with the selected profile.", note.Offset);
        }
        int colorVersion = color[0];
        if (colorVersion != 15 || color.Length != 1602)
        {
            throw new RawProfileException(RawDiagnosticCode.UnsupportedProfile, "The Canon ColorData block version or length is not reviewed for the selected profile.", note.Offset);
        }
        int[] sliceValues = GetSliceValues(rawImage);
        int[] sliceWidths = ExpandSlices(sliceValues);
        bool subsampled = CanonEarlyRawLayout.HasQuirk(profile.Codec, CanonSRawDecoder.SubsampledQuirk);
        int horizontal = 1;
        int vertical = 1;
        int generation = -1;
        if (subsampled)
        {
            if (settings.Length <= 46)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The Canon sRAW/mRAW profile requires CameraSettings SRAWQuality.", note.Offset);
            }
            if (CanonEarlyRawLayout.HasQuirk(profile.Codec, CanonSRawDecoder.Layout420Quirk))
            {
                horizontal = 2;
                vertical = 2;
                if (settings[46] != 1)
                {
                    throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Canon SRAWQuality does not identify the profile's mRAW layout.", note.Offset);
                }
            }
            else if (CanonEarlyRawLayout.HasQuirk(profile.Codec, CanonSRawDecoder.Layout422Quirk))
            {
                horizontal = 2;
                vertical = 1;
                if (settings[46] != 2)
                {
                    throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Canon SRAWQuality does not identify the profile's sRAW layout.", note.Offset);
                }
            }
            else
            {
                throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The Canon subsampled profile does not define a component layout.", note.Offset);
            }
            RequireSRawType(rawImage);
            generation = CanonSRawDecoder.GetGeneration(profile.Codec);
            if (generation < 0)
            {
                throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The Canon subsampled profile does not define a reconstruction generation.", note.Offset);
            }
        }
        int sliceGeometryWidth = sensorWidth;
        if (subsampled)
        {
            if (sensorWidth % horizontal != 0 || sensorHeight % vertical != 0)
            {
                throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Canon sRAW/mRAW sensor dimensions are not divisible by the selected sampling factors.", note.Offset);
            }
            sliceGeometryWidth = CheckedMath.ToInt32(CheckedMath.Multiply(sensorWidth / horizontal, 2 + horizontal * vertical));
        }
        Cr2LosslessJpegDecoder.ValidateProfileSlices(sliceWidths, profile.Codec.SliceWidths, sliceGeometryWidth);

        RawCanonCr2Metadata result = new RawCanonCr2Metadata();
        result.ProfileId = profile.Id;
        result.Model = metadata.Model;
        result.ModelId = GetModelId(note);
        result.Quality = settings[3];
        result.SRawQuality = settings.Length > 46 ? settings[46] : -1;
        result.SensorWidth = sensorWidth;
        result.SensorHeight = sensorHeight;
        result.ActiveLeft = left;
        result.ActiveTop = top;
        result.ActiveRight = right;
        result.ActiveBottom = bottom;
        result.ActiveArea = active;
        result.MaskedAreas = GetMaskedAreas(sensor, sensorWidth, sensorHeight);
        result.Cfa = subsampled ? null : RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);
        result.ColorDataVersion = colorVersion;
        result.ColorData = color;
        result.AsShotWhiteBalance = CopyWords(color, 0x3f, 4);
        result.BlackLevels = subsampled ? new double[] { 0.0, 0.0, 0.0 } : CopyWords(color, 0x30a, 4);
        result.WhiteLevels = subsampled ? GetSRawWhiteLevel(color[0x30e]) : CopyWords(color, 0x30e, 1);
        result.ColorMatrix = CopyDoubles(profile.Calibration.ColorMatrix);
        result.RawSliceValues = sliceValues;
        result.SliceWidths = sliceWidths;
        result.IsSubsampled = subsampled;
        result.SubsamplingHorizontal = horizontal;
        result.SubsamplingVertical = vertical;
        result.SRawGeneration = generation;
        result.SRawHue = subsampled ? GetSRawHue(profile.Codec, horizontal, vertical) : 0;
        result.SRawCoefficients = subsampled ? CanonSRawDecoder.ExtractCoefficients(color, CanonEarlyRawLayout.HasQuirk(profile.Codec, CanonSRawDecoder.InvertWhiteBalanceQuirk)) : System.Array.Empty<int>();
        result.LosslessJpegComponentCount = subsampled ? 3 : 4;
        return result;
    }

    internal static int GetSRawHue(RawProfileCodec codec, int horizontal, int vertical)
    {
        int samples = horizontal * vertical;
        return CanonEarlyRawLayout.HasQuirk(codec, CanonSRawDecoder.NewHueQuirk) ? (samples - 1) >> 1 : samples;
    }

    internal static void RequireSRawType(TiffImageDescriptor? rawImage)
    {
        TiffEntry? entry = rawImage == null || rawImage.Ifd == null ? null : rawImage.Ifd.FindEntry(50885);
        ulong[] values = TiffImageData.GetUnsignedValues(entry);
        if (values.Length != 1 || values[0] != 4)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The selected Canon sRAW/mRAW profile requires raw-type tag 50885 value four.", rawImage == null || rawImage.Ifd == null ? -1 : rawImage.Ifd.AbsoluteOffset);
        }
    }

    internal static double[] GetSRawWhiteLevel(ushort value)
    {
        long scaled = ((value + 1) & value) == 0 ? ((long)value + 1) * 4 - 1 : (long)value * 4;
        if (scaled <= 0 || scaled > ushort.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The scaled Canon sRAW/mRAW white level is invalid.", -1);
        }
        return new double[] { scaled };
    }

    internal static CanonEarlyRawHeader InspectDimensionlessRaw(System.ReadOnlyMemory<byte> source, TiffImageDescriptor? rawImage, RawReadProfile profile, RawOpenOptions options)
    {
        if (rawImage == null || rawImage.Blocks == null || rawImage.Blocks.Length != 1)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "The early CR2 profile requires one raw strip payload.", -1);
        }
        TiffDataBlock block = rawImage.Blocks[0];
        return CanonEarlyRawLayout.Inspect(source, CheckedMath.ToInt64(block.Offset), CheckedMath.ToInt64(block.ByteCount), profile, options.Limits, options.CancellationToken);
    }

    internal static RawCanonCr2Metadata ExtractEarly(RawMetadata metadata, RawMakerNote note, RawReadProfile profile, CanonEarlyRawHeader header, bool requireSensorInfo)
    {
        RawArea? active = CopyArea(profile.Geometry.ActiveArea, RawAreaKind.Active);
        RawCanonMakerNoteTag? sensorTag = FindCanonTag(note, 0x00e0);
        if (requireSensorInfo)
        {
            if (sensorTag == null)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The early CR2 profile requires Canon SensorInfo.", note.Offset);
            }
            int[] sensor = GetSignedWords(sensorTag);
            if (sensor.Length <= 8 || sensor[1] != profile.Geometry.StoredWidth || sensor[2] != profile.Geometry.StoredHeight)
            {
                throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Canon SensorInfo does not match the early profile geometry.", note.Offset);
            }
            RawArea observed = new RawArea(RawAreaKind.Active, sensor[5], sensor[6], sensor[7] - sensor[5] + 1, sensor[8] - sensor[6] + 1);
            if (!SameArea(observed, active))
            {
                throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Canon SensorInfo active borders disagree with the early profile.", note.Offset);
            }
        }
        RawCanonCr2Metadata result = new RawCanonCr2Metadata();
        result.ProfileId = profile.Id;
        result.Model = metadata.Model;
        result.ModelId = GetModelId(note);
        result.SensorWidth = header.StoredWidth;
        result.SensorHeight = header.StoredHeight;
        result.ActiveLeft = (active ?? throw new System.InvalidOperationException("A required value is unavailable.")).X;
        result.ActiveTop = active.Y;
        result.ActiveRight = active.X + active.Width - 1;
        result.ActiveBottom = active.Y + active.Height - 1;
        result.ActiveArea = active;
        result.MaskedAreas = CopyAreas(profile.Geometry.MaskedAreas);
        result.Cfa = CopyCfa(profile.Geometry.Cfa);
        result.BlackLevels = CopyDoubles(profile.Calibration.BlackLevels);
        result.WhiteLevels = CopyDoubles(profile.Calibration.WhiteLevels);
        result.ColorMatrix = CopyDoubles(profile.Calibration.ColorMatrix);
        result.SliceWidths = CopyIntegers(profile.Codec.SliceWidths);
        result.RawDataOffset = header.Offset;
        result.LosslessJpegCodedWidth = header.CodedWidth;
        result.LosslessJpegCodedHeight = header.CodedHeight;
        result.LosslessJpegComponentCount = header.ComponentCount;
        result.HalfHeightDoubleWidth = CanonEarlyRawLayout.HasQuirk(profile.Codec, CanonEarlyRawLayout.HalfHeightDoubleWidthQuirk);
        RawCanonMakerNoteTag? settings = FindCanonTag(note, 0x0001);
        if (settings != null)
        {
            int[] values = GetSignedWords(settings);
            result.Quality = values.Length > 3 ? values[3] : -1;
            result.SRawQuality = values.Length > 46 ? values[46] : -1;
        }
        return result;
    }

    internal static long GetRawDataOffset(RawMakerNote note)
    {
        RawCanonMakerNoteTag? tag = FindCanonTag(note, 0x0081);
        if (tag == null || tag.Value == null || tag.Value.Type != RawTagType.Long || tag.Value.Count != 1)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The selected early Canon profile requires one RawDataOffset value.", note.Offset);
        }
        return tag.Value.GetUInt32Values()[0];
    }

    internal static void ApplyToImage(RawImageInfo image, RawCanonCr2Metadata metadata, RawReadProfile profile)
    {
        if (image == null || metadata == null || profile == null)
        {
            return;
        }
        image.BitDepth = metadata.IsSubsampled ? 16 : profile.Codec.StoredWordBits;
        image.Photometric = metadata.IsSubsampled ? RawPhotometricInterpretation.Rgb : RawPhotometricInterpretation.ColorFilterArray;
        image.Cfa = metadata.IsSubsampled ? null : CopyCfa(metadata.Cfa);
        image.ActiveArea = CopyArea(metadata.ActiveArea, RawAreaKind.Active);
        image.DefaultCrop = CopyArea(metadata.ActiveArea, RawAreaKind.DefaultCrop);
        if (metadata.IsSubsampled)
        {
            image.Channels = new RawChannelDescription[]
            {
                new RawChannelDescription("R", RawChannelKind.Red),
                new RawChannelDescription("G", RawChannelKind.Green),
                new RawChannelDescription("B", RawChannelKind.Blue)
            };
            image.Planes = new RawPlaneInfo[]
            {
                new RawPlaneInfo(0, "R", RawChannelKind.Red, image.Width, image.Height, image.BitDepth, image.Width),
                new RawPlaneInfo(1, "G", RawChannelKind.Green, image.Width, image.Height, image.BitDepth, image.Width),
                new RawPlaneInfo(2, "B", RawChannelKind.Blue, image.Width, image.Height, image.BitDepth, image.Width)
            };
        }
        else if (image.Channels.Length == 1)
        {
            image.Channels[0] = new RawChannelDescription("CFA", RawChannelKind.Custom);
        }
        if (!metadata.IsSubsampled && image.Planes.Length == 1)
        {
            image.Planes[0] = new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, image.Width, image.Height, image.BitDepth, image.Width);
        }
    }

    internal static RawMakerNote? FindCanonNote(RawMetadata metadata)
    {
        if (metadata == null || metadata.MakerNotes == null)
        {
            return null;
        }
        int index = 0;
        while (index < metadata.MakerNotes.Length)
        {
            RawMakerNote note = metadata.MakerNotes[index];
            if (note != null && (note.Signature == "CanonIfd" || note.Signature == "Canon"))
            {
                return note;
            }
            index++;
        }
        return null;
    }

    internal static RawCanonMakerNoteTag RequireTag(RawMakerNote note, ushort id, string name)
    {
        RawCanonMakerNoteTag? tag = FindCanonTag(note, id);
        if (tag == null || tag.Value == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The selected Canon profile requires the " + name + " maker-note tag.", note.Offset);
        }
        return tag;
    }

    internal static RawCanonMakerNoteTag? FindCanonTag(RawMakerNote note, ushort id)
    {
        RawCanonMakerNoteTag? tag = note == null ? null : note.FindCanonTag(id);
        return tag == null ? CanonEarlyRawLayout.FindTag(note, id) : tag;
    }

    internal static string GetModelId(RawMakerNote? note)
    {
        if (note == null)
        {
            return string.Empty;
        }
        RawCanonMakerNoteTag? tag = FindCanonTag(note, 0x0010);
        if (tag == null || tag.Value == null || tag.Value.Type != RawTagType.Long || tag.Value.Count != 1)
        {
            return string.Empty;
        }
        return tag.Value.GetUInt32Values()[0].ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string GetFirmware(RawMakerNote? note)
    {
        if (note == null)
        {
            return string.Empty;
        }
        RawCanonMakerNoteTag? tag = FindCanonTag(note, 0x0007);
        if (tag == null || tag.Value == null || tag.Value.Type != RawTagType.Ascii)
        {
            return string.Empty;
        }
        string text = tag.GetText();
        int index = 0;
        while (index < text.Length && (text[index] < '0' || text[index] > '9'))
        {
            index++;
        }
        return index == text.Length ? text : text.Substring(index);
    }

    internal static int[] GetSignedWords(RawCanonMakerNoteTag tag)
    {
        if (tag.Value.Type == RawTagType.SignedShort)
        {
            short[] source = tag.Value.GetInt16Values();
            int[] result = new int[source.Length];
            int index = 0;
            while (index < source.Length)
            {
                result[index] = source[index];
                index++;
            }
            return result;
        }
        ushort[] unsigned = GetUnsignedWords(tag);
        int[] converted = new int[unsigned.Length];
        int unsignedIndex = 0;
        while (unsignedIndex < unsigned.Length)
        {
            converted[unsignedIndex] = BitUtility.ToInt16Bits(unsigned[unsignedIndex]);
            unsignedIndex++;
        }
        return converted;
    }

    internal static ushort[] GetUnsignedWords(RawCanonMakerNoteTag tag)
    {
        if (tag.Value.Type == RawTagType.Short)
        {
            return tag.Value.GetUInt16Values();
        }
        if (tag.Value.Type == RawTagType.SignedShort)
        {
            short[] source = tag.Value.GetInt16Values();
            ushort[] converted = new ushort[source.Length];
            int index = 0;
            while (index < source.Length)
            {
                converted[index] = BitUtility.ToUInt16Bits(source[index]);
                index++;
            }
            return converted;
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A Canon profile word-array tag has an unsupported type.", tag.ValuePosition);
    }

    internal static int[] GetSliceValues(TiffImageDescriptor? rawImage)
    {
        ulong[] source = TiffImageData.GetUnsignedValues((rawImage ?? throw new System.InvalidOperationException("A required value is unavailable.")).Ifd.FindEntry(50752));
        if (source.Length != 3 || source[0] > int.MaxValue || source[1] > int.MaxValue || source[2] > int.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The selected Canon profile requires a three-word CR2 slice tag.", rawImage.Ifd.AbsoluteOffset);
        }
        return new int[] { (int)source[0], (int)source[1], (int)source[2] };
    }

    internal static int[] ExpandSlices(int[] values)
    {
        if (values[0] < 0 || values[0] > 4096 || values[0] > 0 && values[1] <= 0 || values[2] <= 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The CR2 slice tag contains invalid extracted widths.", -1);
        }
        int[] widths = new int[values[0] + 1];
        int index = 0;
        while (index < values[0])
        {
            widths[index] = values[1];
            index++;
        }
        widths[widths.Length - 1] = values[2];
        return widths;
    }

    internal static RawArea[] GetMaskedAreas(int[] sensor, int width, int height)
    {
        int left = sensor[9];
        int top = sensor[10];
        int right = sensor[11];
        int bottom = sensor[12];
        if (left < 0 || top < 0 || right < left || bottom < top || right >= width || bottom >= height)
        {
            return System.Array.Empty<RawArea>();
        }
        return new RawArea[] { new RawArea(RawAreaKind.OpticalBlack, left, top, right - left + 1, bottom - top + 1) };
    }

    internal static double[] CopyWords(ushort[] source, int offset, int length)
    {
        double[] result = new double[length];
        int index = 0;
        while (index < length)
        {
            result[index] = source[offset + index];
            index++;
        }
        return result;
    }

    internal static double[] CopyDoubles(double[]? source)
    {
        if (source == null)
        {
            return System.Array.Empty<double>();
        }
        double[] result = new double[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }

    internal static int[] CopyIntegers(int[] source)
    {
        if (source == null)
        {
            return System.Array.Empty<int>();
        }
        int[] result = new int[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }

    internal static RawArea[] CopyAreas(RawArea[]? source)
    {
        if (source == null)
        {
            return System.Array.Empty<RawArea>();
        }
        RawArea[] result = new RawArea[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            RawArea area = source[index] ?? throw new System.InvalidOperationException("A masked area cannot be null.");
            result[index] = CopyArea(area, area.Kind) ?? throw new System.InvalidOperationException("A masked area copy cannot be null.");
            index++;
        }
        return result;
    }

    internal static RawCfaPattern? CopyCfa(RawCfaPattern? source)
    {
        if (source == null)
        {
            return null;
        }
        RawCfaColor[] colors = new RawCfaColor[source.Colors.Length];
        System.Array.Copy(source.Colors, colors, colors.Length);
        return new RawCfaPattern(source.Layout, source.RepeatWidth, source.RepeatHeight, colors);
    }

    internal static RawArea? CopyArea(RawArea? source, RawAreaKind kind)
    {
        return source == null ? null : new RawArea(kind, source.X, source.Y, source.Width, source.Height);
    }

    internal static bool SameArea(RawArea? first, RawArea? second)
    {
        return first != null && second != null && first.X == second.X && first.Y == second.Y && first.Width == second.Width && first.Height == second.Height;
    }

    internal static string[] GetIfdPaths(TiffIfdGraph? graph)
    {
        if (graph == null || graph.Ifds == null)
        {
            return System.Array.Empty<string>();
        }
        string[] paths = new string[graph.Ifds.Length];
        int index = 0;
        while (index < graph.Ifds.Length)
        {
            paths[index] = graph.Ifds[index].Path;
            index++;
        }
        return paths;
    }

    internal static string[] GetTags(TiffIfdGraph? graph)
    {
        if (graph == null || graph.Ifds == null)
        {
            return System.Array.Empty<string>();
        }
        int count = 0;
        int index = 0;
        while (index < graph.Ifds.Length)
        {
            count += graph.Ifds[index].Entries.Length;
            index++;
        }
        string[] tags = new string[count];
        int position = 0;
        index = 0;
        while (index < graph.Ifds.Length)
        {
            TiffIfd ifd = graph.Ifds[index];
            int entryIndex = 0;
            while (entryIndex < ifd.Entries.Length)
            {
                tags[position] = ifd.Path + "/" + ifd.Entries[entryIndex].Tag.ToString(System.Globalization.CultureInfo.InvariantCulture);
                position++;
                entryIndex++;
            }
            index++;
        }
        return tags;
    }

    internal static RawDiagnostic[] ToWarnings(RawDiagnostic[] diagnostics)
    {
        RawDiagnostic[] warnings = new RawDiagnostic[diagnostics.Length];
        int index = 0;
        while (index < diagnostics.Length)
        {
            RawDiagnostic source = diagnostics[index];
            warnings[index] = new RawDiagnostic(source.Code, RawSeverity.Warning, source.Format, source.Offset, source.IfdPath, source.Tag, "CanonCr2MetadataParser", source.Message, false);
            index++;
        }
        return warnings;
    }

    internal static int ToInt(ulong value)
    {
        return value > int.MaxValue ? 0 : (int)value;
    }
}
