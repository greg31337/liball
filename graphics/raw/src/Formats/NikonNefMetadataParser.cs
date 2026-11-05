namespace raw;

internal class NikonNefExtraction
{
    internal RawNikonNefMetadata? Metadata;
    internal RawReadProfile? Profile;
    internal RawDiagnostic[] Diagnostics;

    internal NikonNefExtraction()
    {
        Diagnostics = System.Array.Empty<RawDiagnostic>();
    }
}

internal static class NikonNefMetadataParser
{
    internal const string Z7UncompressedProfile = "nikon.z-7.nef.uncompressed-14bit.fx.read.experimental";
    internal const string Z8LosslessProfile = "nikon.z-8.nef.lossless-14bit.fx.read.experimental";

    internal static NikonNefExtraction Parse(TiffIfdGraph graph, TiffImageDescriptor rawImage, RawMetadata metadata, RawOpenOptions options)
    {
        NikonNefExtraction extraction = new NikonNefExtraction();
        RawMakerNote? note = FindNikonNote(metadata);
        if (metadata == null || metadata.Make.Length == 0 || metadata.Model.Length == 0)
        {
            return extraction;
        }
        int makerCompression = note == null ? -1 : ReadMakerScalar(note, 0x0093, -1);
        RawCompression unsupportedCompression = NikonHeRecognition.GetCompression(rawImage == null ? -1 : CanonCr2MetadataParser.ToInt(rawImage.CompressionCode), makerCompression);
        if (NikonHeRecognition.IsCompression(unsupportedCompression))
        {
            extraction.Metadata = ExtractUnsupportedHe(rawImage, metadata, note, unsupportedCompression);
            extraction.Diagnostics = new RawDiagnostic[] { NikonHeRecognition.CreateDiagnostic(note, unsupportedCompression) };
            extraction.Metadata.Diagnostics = extraction.Diagnostics;
            return extraction;
        }
        RawProfileMatchInput input = CreateMatchInput(graph, rawImage, metadata, note, options.AllowExperimentalProfiles);
        RawProfileMatchResult match;
        if (options.ProfileCatalogue == null)
        {
            match = RawProfileMatcher.MatchRead(RawBuiltInProfileCatalogue.CreateDocument().ReadProfiles, input);
        }
        else
        {
            match = options.ProfileCatalogue.MatchReadProfile(input);
        }
        if (match == null)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The configured profile source returned no Nikon match result.", -1);
        }
        if (!match.Matched)
        {
            if (match.Candidates.Length != 0)
            {
                extraction.Diagnostics = ToWarnings(match.Diagnostics);
            }
            return extraction;
        }
        if (!NikonCompressionCodec.IsProfile(match.ReadProfile) && !NefUncompressedDecoder.IsProfile(match.ReadProfile) && !NefVariantDecoder.IsProfile(match.ReadProfile))
        {
            throw new RawProfileException(RawDiagnosticCode.UnsupportedProfile, "The selected Nikon profile does not have reviewed metadata extraction rules.", -1);
        }
        if (note == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The selected Nikon profile requires a Nikon type-3 maker note.", -1);
        }
        extraction.Metadata = Extract(rawImage, metadata, note, match.ReadProfile);
        extraction.Profile = match.ReadProfile;
        return extraction;
    }

    internal static RawProfileMatchInput CreateMatchInput(TiffIfdGraph graph, TiffImageDescriptor? rawImage, RawMetadata metadata, RawMakerNote? note, bool allowExperimental)
    {
        RawProfileMatchInput input = new RawProfileMatchInput();
        input.Format = RawFormat.Nef;
        input.Make = metadata == null ? string.Empty : metadata.Make;
        input.Model = metadata == null ? string.Empty : metadata.Model;
        input.HasByteOrder = graph != null && graph.Header != null;
        input.ByteOrder = input.HasByteOrder ? (graph ?? throw new System.InvalidOperationException("A required value is unavailable.")).Header.ByteOrder : RawByteOrder.LittleEndian;
        input.HasBigTiff = input.HasByteOrder;
        input.BigTiff = input.HasByteOrder && (graph ?? throw new System.InvalidOperationException("A required value is unavailable.")).Header.BigTiff;
        if (rawImage != null)
        {
            input.Width = CanonCr2MetadataParser.ToInt(rawImage.Width);
            input.Height = CanonCr2MetadataParser.ToInt(rawImage.Height);
            input.Compression = CanonCr2MetadataParser.ToInt(rawImage.CompressionCode);
            input.SamplesPerPixel = rawImage.Ifd.FindEntry(277) == null ? 0 : CanonCr2MetadataParser.ToInt(rawImage.SamplesPerPixel);
            input.BitDepth = rawImage.Ifd.FindEntry(258) == null || rawImage.BitsPerSample.Length == 0 ? 0 : CanonCr2MetadataParser.ToInt(rawImage.BitsPerSample[0]);
        }
        input.IfdPaths = CanonCr2MetadataParser.GetIfdPaths(graph);
        input.Tags = CanonCr2MetadataParser.GetTags(graph);
        input.MakerNoteSignature = note == null ? string.Empty : note.Signature;
        input.AllowExperimental = allowExperimental;
        return input;
    }

    internal static RawNikonNefMetadata Extract(TiffImageDescriptor? rawImage, RawMetadata metadata, RawMakerNote note, RawReadProfile? profile)
    {
        if (rawImage == null || rawImage.Ifd == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "The selected Nikon profile requires a raw image IFD.", -1);
        }
        int width = CanonCr2MetadataParser.ToInt(rawImage.Width);
        int height = CanonCr2MetadataParser.ToInt(rawImage.Height);
        if (width != (profile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Geometry.StoredWidth || height != profile.Geometry.StoredHeight)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The Nikon raw IFD dimensions disagree with the selected profile.", rawImage.Ifd.AbsoluteOffset);
        }
        RawNikonNefMetadata result = new RawNikonNefMetadata();
        result.ProfileId = profile.Id;
        result.Model = metadata.Model;
        result.StoredWidth = width;
        result.StoredHeight = height;
        result.ActiveArea = ReadArea(rawImage.Ifd, 50829, RawAreaKind.Active, width, height, profile.Geometry.ActiveArea);
        if (profile.Geometry.ActiveArea != null && !CanonCr2MetadataParser.SameArea(result.ActiveArea, profile.Geometry.ActiveArea))
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The Nikon active-area tag disagrees with the selected profile.", rawImage.Ifd.AbsoluteOffset);
        }
        result.DefaultCrop = ReadDefaultCrop(rawImage.Ifd, result.ActiveArea, width, height);
        result.MaskedAreas = ReadMaskedAreas(rawImage.Ifd, width, height, profile.Geometry.MaskedAreas);
        bool nonCfa = NefVariantDecoder.IsProfile(profile);
        result.Cfa = nonCfa ? null : RawCodec.CreateTiffCfa(rawImage);
        if (!nonCfa && result.Cfa == null)
        {
            result.Cfa = CanonCr2MetadataParser.CopyCfa(profile.Geometry.Cfa);
        }
        if (!nonCfa && result.Cfa == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The selected Nikon profile requires a CFA pattern.", rawImage.Ifd.AbsoluteOffset);
        }
        if (!nonCfa && profile.Geometry.Cfa != null && !SameCfa(result.Cfa, profile.Geometry.Cfa))
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The Nikon CFA tag disagrees with the selected profile.", rawImage.Ifd.AbsoluteOffset);
        }
        if (profile.Geometry.Cfa != null)
        {
            result.Cfa = CanonCr2MetadataParser.CopyCfa(profile.Geometry.Cfa);
        }
        ReadBlackLevels(rawImage.Ifd, profile, result);
        result.WhiteLevels = ReadNumbers(rawImage.Ifd.FindEntry(50717), profile.Calibration.WhiteLevels, "WhiteLevel");
        result.LinearizationTable = ReadCurve(rawImage.Ifd.FindEntry(50712));
        result.MakerNoteVersion = note.NikonVersion;
        ReadColorBalance(note, profile, result);
        result.TiffCompressionCode = CanonCr2MetadataParser.ToInt(rawImage.CompressionCode);
        result.NefCompressionCode = ReadMakerScalar(note, 0x0093, -1);
        result.Compression = ResolveCompression(result.TiffCompressionCode, result.NefCompressionCode);
        ReadCompressionInfo(note, result);
        ReadLens(note, result);
        result.Quality = ReadMakerText(note, 0x0004);
        result.WhiteBalanceName = ReadMakerText(note, 0x0005);
        result.FocusMode = ReadMakerText(note, 0x0007);
        result.NoiseReduction = ReadMakerText(note, 0x0095);
        result.IsoValues = ReadMakerWords(note, 0x0002);
        result.ShootingMode = ReadMakerScalar(note, 0x0089, -1);
        result.ShutterCount = ReadMakerLong(note, 0x00A7, -1);
        return result;
    }

    internal static void ReadBlackLevels(TiffIfd ifd, RawReadProfile profile, RawNikonNefMetadata result)
    {
        double[] dimensions = ReadNumbers(ifd.FindEntry(50713), null, "BlackLevelRepeatDim");
        if (dimensions.Length == 0)
        {
            result.BlackLevelRepeatWidth = 1;
            result.BlackLevelRepeatHeight = 1;
        }
        else
        {
            if (dimensions.Length != 2 || dimensions[0] < 1 || dimensions[1] < 1 || dimensions[0] > int.MaxValue || dimensions[1] > int.MaxValue || dimensions[0] != System.Math.Floor(dimensions[0]) || dimensions[1] != System.Math.Floor(dimensions[1]))
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Nikon BlackLevelRepeatDim must contain two positive integers.", ifd.AbsoluteOffset);
            }
            result.BlackLevelRepeatHeight = (int)dimensions[0];
            result.BlackLevelRepeatWidth = (int)dimensions[1];
        }
        result.BlackLevels = ReadNumbers(ifd.FindEntry(50714), profile.Calibration.BlackLevels, "BlackLevel");
        long repeatCount = (long)result.BlackLevelRepeatWidth * result.BlackLevelRepeatHeight;
        long variantCount = NefVariantDecoder.IsProfile(profile) ? repeatCount * profile.Fingerprint.SamplesPerPixel : repeatCount;
        if (result.BlackLevels.Length != 0 && result.BlackLevels.LongLength != repeatCount && result.BlackLevels.LongLength != variantCount)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Nikon black-level values do not match their repeat geometry.", ifd.AbsoluteOffset);
        }
    }

    internal static void ReadColorBalance(RawMakerNote note, RawReadProfile profile, RawNikonNefMetadata result)
    {
        RawNikonColorBalance? balance = note.NikonColorBalance;
        if (balance == null)
        {
            result.AsShotWhiteBalance = CanonCr2MetadataParser.CopyDoubles(profile.Calibration.WhiteBalance);
            return;
        }
        result.ColorBalanceVersion = balance.Version;
        result.ColorBalanceEncrypted = balance.Encrypted;
        result.ColorBalanceDecoded = balance.DecryptionSupported;
        result.ColorBalanceChannelOrder = balance.ChannelOrder;
        if (balance.DecryptionSupported)
        {
            result.AsShotWhiteBalance = new double[balance.Levels.Length];
            int index = 0;
            while (index < balance.Levels.Length)
            {
                result.AsShotWhiteBalance[index] = balance.Levels[index];
                index++;
            }
        }
        else
        {
            result.AsShotWhiteBalance = CanonCr2MetadataParser.CopyDoubles(profile.Calibration.WhiteBalance);
        }
    }

    internal static void ReadLens(RawMakerNote note, RawNikonNefMetadata result)
    {
        result.LensType = ReadMakerScalar(note, 0x0083, -1);
        RawNikonMakerNoteTag? specification = note.FindNikonTag(0x0084);
        if (specification != null && specification.Value != null)
        {
            if (specification.Value.Type != RawTagType.Rational || specification.Value.Count != 4)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Nikon Lens must contain four unsigned rationals.", note.Offset + specification.ValuePosition);
            }
            result.LensSpecification = specification.Value.GetRationalValues();
        }
        RawNikonMakerNoteTag? data = note.FindNikonTag(0x0098);
        if (data != null && data.Value != null)
        {
            result.LensData = data.CopyBytes();
            result.LensDataVersion = VersionPrefix(result.LensData);
        }
    }

    internal static RawCompression ResolveCompression(int tiffCode, int nefCode)
    {
        if (tiffCode == 1 || nefCode == 2)
        {
            return RawCompression.None;
        }
        if (tiffCode != 34713)
        {
            return RawCompression.Unknown;
        }
        if (nefCode == NikonHeRecognition.HeMakerCode)
        {
            return RawCompression.NikonHe;
        }
        if (nefCode == NikonHeRecognition.HeStarMakerCode)
        {
            return RawCompression.NikonHeStar;
        }
        if (nefCode == 1 || nefCode == 4)
        {
            return RawCompression.NikonLossy;
        }
        if (nefCode == 3)
        {
            return RawCompression.NikonLossless;
        }
        return RawCompression.Unknown;
    }

    internal static RawNikonNefMetadata ExtractUnsupportedHe(TiffImageDescriptor? rawImage, RawMetadata metadata, RawMakerNote? note, RawCompression compression)
    {
        if (rawImage == null || rawImage.Ifd == null || metadata == null || note == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Nikon HE recognition requires a raw IFD and Nikon maker note.", -1);
        }
        int width = CanonCr2MetadataParser.ToInt(rawImage.Width);
        int height = CanonCr2MetadataParser.ToInt(rawImage.Height);
        RawNikonNefMetadata result = new RawNikonNefMetadata();
        result.Model = metadata.Model;
        result.StoredWidth = width;
        result.StoredHeight = height;
        RawArea fullActive = new RawArea(RawAreaKind.Active, 0, 0, width, height);
        result.ActiveArea = ReadArea(rawImage.Ifd, 50829, RawAreaKind.Active, width, height, fullActive);
        result.DefaultCrop = ReadDefaultCrop(rawImage.Ifd, result.ActiveArea, width, height);
        result.MaskedAreas = ReadMaskedAreas(rawImage.Ifd, width, height, null);
        result.Cfa = RawCodec.CreateTiffCfa(rawImage);
        RawReadProfile emptyProfile = new RawReadProfile();
        ReadBlackLevels(rawImage.Ifd, emptyProfile, result);
        result.WhiteLevels = ReadNumbers(rawImage.Ifd.FindEntry(50717), null, "WhiteLevel");
        result.LinearizationTable = ReadCurve(rawImage.Ifd.FindEntry(50712));
        result.MakerNoteVersion = note.NikonVersion;
        ReadColorBalance(note, emptyProfile, result);
        result.TiffCompressionCode = CanonCr2MetadataParser.ToInt(rawImage.CompressionCode);
        result.NefCompressionCode = ReadMakerScalar(note, 0x0093, -1);
        result.Compression = compression;
        ReadCompressionInfo(note, result);
        ReadLens(note, result);
        result.Quality = ReadMakerText(note, 0x0004);
        result.WhiteBalanceName = ReadMakerText(note, 0x0005);
        result.FocusMode = ReadMakerText(note, 0x0007);
        result.NoiseReduction = ReadMakerText(note, 0x0095);
        result.IsoValues = ReadMakerWords(note, 0x0002);
        result.ShootingMode = ReadMakerScalar(note, 0x0089, -1);
        result.ShutterCount = ReadMakerLong(note, 0x00A7, -1);
        return result;
    }

    internal static void ReadCompressionInfo(RawMakerNote note, RawNikonNefMetadata result)
    {
        RawNikonMakerNoteTag? tag = note.FindNikonTag(0x0096);
        if (tag == null)
        {
            tag = note.FindNikonTag(0x008c);
        }
        if (tag == null || tag.Value == null)
        {
            return;
        }
        if (tag.Value.Type != RawTagType.Undefined && tag.Value.Type != RawTagType.Byte)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Nikon compression parameters must be stored as bytes.", note.Offset + tag.ValuePosition);
        }
        result.CompressionInfoTag = tag.Id;
        result.CompressionInfo = tag.CopyBytes();
        result.CompressionInfoByteOrder = note.ByteOrder;
    }

    internal static void ApplyToImage(RawImageInfo image, RawNikonNefMetadata metadata, RawReadProfile profile)
    {
        if (image == null || metadata == null || profile == null)
        {
            return;
        }
        image.BitDepth = profile.Fingerprint.BitDepth;
        image.Photometric = RawPhotometricInterpretation.ColorFilterArray;
        image.Cfa = CanonCr2MetadataParser.CopyCfa(metadata.Cfa);
        image.ActiveArea = CanonCr2MetadataParser.CopyArea(metadata.ActiveArea, RawAreaKind.Active);
        image.DefaultCrop = CanonCr2MetadataParser.CopyArea(metadata.DefaultCrop, RawAreaKind.DefaultCrop);
        image.Compression = metadata.Compression;
        if (NefVariantDecoder.IsProfile(profile))
        {
            NefVariantDecoder.ApplyToImage(image, profile);
        }
        else if (image.Channels.Length == 1)
        {
            image.Channels[0] = new RawChannelDescription("CFA", RawChannelKind.Custom);
        }
        if (image.Planes.Length == 1)
        {
            image.Planes[0] = new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, image.Width, image.Height, image.BitDepth, image.Width);
        }
        if (image.Payload != null)
        {
            image.Payload.Compression = metadata.Compression;
            image.Payload.Decodable = NefUncompressedDecoder.IsProfile(profile) || NikonCompressionCodec.IsProfile(profile) && metadata.CompressionInfo.Length != 0 || NefVariantDecoder.IsProfile(profile);
        }
    }

    internal static RawArea? ReadArea(TiffIfd ifd, ushort tagId, RawAreaKind kind, int width, int height, RawArea? fallback)
    {
        double[] values = ReadNumbers(ifd.FindEntry(tagId), null, "Area");
        if (values.Length == 0)
        {
            return CanonCr2MetadataParser.CopyArea(fallback, kind);
        }
        if (values.Length != 4)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A Nikon sensor-area tag must contain top, left, bottom, and right.", ifd.AbsoluteOffset);
        }
        int top = ExactInt(values[0], "area top", ifd.AbsoluteOffset);
        int left = ExactInt(values[1], "area left", ifd.AbsoluteOffset);
        int bottom = ExactInt(values[2], "area bottom", ifd.AbsoluteOffset);
        int right = ExactInt(values[3], "area right", ifd.AbsoluteOffset);
        RawArea area = new RawArea(kind, left, top, right - left, bottom - top);
        if (!area.Validate(width, height).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A Nikon sensor area exceeds the stored image.", ifd.AbsoluteOffset);
        }
        return area;
    }

    internal static RawArea? ReadDefaultCrop(TiffIfd ifd, RawArea? active, int width, int height)
    {
        double[] origin = ReadNumbers(ifd.FindEntry(50719), null, "DefaultCropOrigin");
        double[] size = ReadNumbers(ifd.FindEntry(50720), null, "DefaultCropSize");
        if (origin.Length == 0 && size.Length == 0)
        {
            return CanonCr2MetadataParser.CopyArea(active, RawAreaKind.DefaultCrop);
        }
        if (origin.Length != 2 || size.Length != 2)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Nikon default-crop origin and size must both contain two values.", ifd.AbsoluteOffset);
        }
        RawArea crop = new RawArea(RawAreaKind.DefaultCrop, ExactInt(origin[0], "crop x", ifd.AbsoluteOffset), ExactInt(origin[1], "crop y", ifd.AbsoluteOffset), ExactInt(size[0], "crop width", ifd.AbsoluteOffset), ExactInt(size[1], "crop height", ifd.AbsoluteOffset));
        if (!crop.Validate(width, height).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The Nikon default crop exceeds the stored image.", ifd.AbsoluteOffset);
        }
        return crop;
    }

    internal static RawArea[] ReadMaskedAreas(TiffIfd ifd, int width, int height, RawArea[]? fallback)
    {
        double[] values = ReadNumbers(ifd.FindEntry(50830), null, "MaskedAreas");
        if (values.Length == 0)
        {
            return CanonCr2MetadataParser.CopyAreas(fallback);
        }
        if (values.Length % 4 != 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Nikon masked areas must contain groups of four coordinates.", ifd.AbsoluteOffset);
        }
        RawArea[] areas = new RawArea[values.Length / 4];
        int index = 0;
        while (index < areas.Length)
        {
            int offset = index * 4;
            int top = ExactInt(values[offset], "masked top", ifd.AbsoluteOffset);
            int left = ExactInt(values[offset + 1], "masked left", ifd.AbsoluteOffset);
            int bottom = ExactInt(values[offset + 2], "masked bottom", ifd.AbsoluteOffset);
            int right = ExactInt(values[offset + 3], "masked right", ifd.AbsoluteOffset);
            areas[index] = new RawArea(RawAreaKind.OpticalBlack, left, top, right - left, bottom - top);
            if (!areas[index].Validate(width, height).IsValid())
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A Nikon masked area exceeds the stored image.", ifd.AbsoluteOffset);
            }
            index++;
        }
        return areas;
    }

    internal static double[] ReadNumbers(TiffEntry? entry, double[]? fallback, string name)
    {
        if (entry == null)
        {
            return CanonCr2MetadataParser.CopyDoubles(fallback);
        }
        if (entry.FieldType == TiffFieldType.Byte)
        {
            byte[] source = entry.GetByteValues();
            double[] values = new double[source.Length];
            int index = 0;
            while (index < source.Length)
            {
                values[index] = source[index];
                index++;
            }
            return values;
        }
        if (entry.FieldType == TiffFieldType.Short || entry.FieldType == TiffFieldType.Long || entry.FieldType == TiffFieldType.Ifd || entry.FieldType == TiffFieldType.Long8 || entry.FieldType == TiffFieldType.Ifd8)
        {
            ulong[] source = TiffImageData.GetUnsignedValues(entry);
            double[] values = new double[source.Length];
            int index = 0;
            while (index < source.Length)
            {
                values[index] = source[index];
                index++;
            }
            return values;
        }
        if (entry.FieldType == TiffFieldType.Rational || entry.FieldType == TiffFieldType.SignedRational)
        {
            RawRational[] source = entry.GetRationalValues();
            double[] values = new double[source.Length];
            int index = 0;
            while (index < source.Length)
            {
                values[index] = source[index].ToDouble();
                index++;
            }
            return values;
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Nikon " + name + " uses an unsupported numeric TIFF type.", entry.EntryOffset);
    }

    internal static ushort[] ReadCurve(TiffEntry? entry)
    {
        double[] source = ReadNumbers(entry, null, "LinearizationTable");
        ushort[] result = new ushort[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            int value = ExactInt(source[index], "linearization sample", entry == null ? -1 : entry.EntryOffset);
            if (value < 0 || value > ushort.MaxValue)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A Nikon linearization sample exceeds unsigned 16-bit storage.", (entry ?? throw new System.InvalidOperationException("A required value is unavailable.")).EntryOffset);
            }
            result[index] = (ushort)value;
            index++;
        }
        return result;
    }

    internal static int ExactInt(double value, string name, long offset)
    {
        if (!RawModelValidation.IsFinite(value) || value < int.MinValue || value > int.MaxValue || value != System.Math.Floor(value))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Nikon " + name + " must be an exact integer.", offset);
        }
        return (int)value;
    }

    internal static bool SameCfa(RawCfaPattern? first, RawCfaPattern second)
    {
        if (first == null || second == null || first.RepeatWidth != second.RepeatWidth || first.RepeatHeight != second.RepeatHeight || first.Colors.Length != second.Colors.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Colors.Length)
        {
            if (first.Colors[index] != second.Colors[index])
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static RawMakerNote? FindNikonNote(RawMetadata metadata)
    {
        if (metadata == null || metadata.MakerNotes == null)
        {
            return null;
        }
        int index = 0;
        while (index < metadata.MakerNotes.Length)
        {
            RawMakerNote note = metadata.MakerNotes[index];
            if (note != null && note.Signature == "NikonEmbeddedTiff")
            {
                return note;
            }
            index++;
        }
        return null;
    }

    internal static string ReadMakerText(RawMakerNote note, ushort id)
    {
        RawNikonMakerNoteTag? tag = note.FindNikonTag(id);
        if (tag == null || tag.Value == null)
        {
            return string.Empty;
        }
        if (tag.Value.Type != RawTagType.Ascii)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A reviewed Nikon text tag has an unexpected type.", note.Offset + tag.ValuePosition);
        }
        return tag.GetText();
    }

    internal static int[] ReadMakerWords(RawMakerNote note, ushort id)
    {
        RawNikonMakerNoteTag? tag = note.FindNikonTag(id);
        if (tag == null || tag.Value == null)
        {
            return System.Array.Empty<int>();
        }
        if (tag.Value.Type != RawTagType.Short)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A reviewed Nikon word tag has an unexpected type.", note.Offset + tag.ValuePosition);
        }
        ushort[] source = tag.Value.GetUInt16Values();
        int[] values = new int[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            values[index] = source[index];
            index++;
        }
        return values;
    }

    internal static int ReadMakerScalar(RawMakerNote note, ushort id, int fallback)
    {
        RawNikonMakerNoteTag? tag = note.FindNikonTag(id);
        if (tag == null || tag.Value == null)
        {
            return fallback;
        }
        if (tag.Value.Type == RawTagType.Byte && tag.Value.Count == 1)
        {
            return tag.Value.GetByteValues()[0];
        }
        if (tag.Value.Type == RawTagType.Short && tag.Value.Count == 1)
        {
            return tag.Value.GetUInt16Values()[0];
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A reviewed Nikon scalar tag has an unexpected type or count.", note.Offset + tag.ValuePosition);
    }

    internal static long ReadMakerLong(RawMakerNote note, ushort id, long fallback)
    {
        RawNikonMakerNoteTag? tag = note.FindNikonTag(id);
        if (tag == null || tag.Value == null)
        {
            return fallback;
        }
        if (tag.Value.Type != RawTagType.Long || tag.Value.Count != 1)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A reviewed Nikon long tag has an unexpected type or count.", note.Offset + tag.ValuePosition);
        }
        return tag.Value.GetUInt32Values()[0];
    }

    internal static string VersionPrefix(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4)
        {
            return string.Empty;
        }
        int index = 0;
        while (index < 4)
        {
            if (bytes[index] < (byte)'0' || bytes[index] > (byte)'9')
            {
                return string.Empty;
            }
            index++;
        }
        return System.Text.Encoding.ASCII.GetString(bytes, 0, 4);
    }

    internal static RawDiagnostic[] ToWarnings(RawDiagnostic[] diagnostics)
    {
        RawDiagnostic[] warnings = new RawDiagnostic[diagnostics.Length];
        int index = 0;
        while (index < diagnostics.Length)
        {
            RawDiagnostic source = diagnostics[index];
            warnings[index] = new RawDiagnostic(source.Code, RawSeverity.Warning, source.Format, source.Offset, source.IfdPath, source.Tag, "NikonNefMetadataParser", source.Message, false);
            index++;
        }
        return warnings;
    }
}
