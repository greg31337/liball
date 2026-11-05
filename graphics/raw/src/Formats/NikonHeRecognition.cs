namespace raw;

internal static class NikonHeRecognition
{
    internal const int HeMakerCode = 13;
    internal const int HeStarMakerCode = 14;
    internal const string Stage = "NikonHeRecognition";
    internal const string HeMessage = "Nikon High Efficiency (HE) NEF raw compression is recognized, but no decoder is enabled.";
    internal const string HeStarMessage = "Nikon High Efficiency Star (HE*) NEF raw compression is recognized, but no decoder is enabled.";

    internal static bool IsMakerCode(int code)
    {
        return code == HeMakerCode || code == HeStarMakerCode;
    }

    internal static bool IsCompression(RawCompression compression)
    {
        return compression == RawCompression.NikonHe || compression == RawCompression.NikonHeStar;
    }

    internal static RawCompression GetCompression(int tiffCode, int makerCode)
    {
        if (tiffCode != 34713)
        {
            return RawCompression.Unknown;
        }
        if (makerCode == HeMakerCode)
        {
            return RawCompression.NikonHe;
        }
        if (makerCode == HeStarMakerCode)
        {
            return RawCompression.NikonHeStar;
        }
        return RawCompression.Unknown;
    }

    internal static string GetMessage(RawCompression compression)
    {
        return compression == RawCompression.NikonHeStar ? HeStarMessage : HeMessage;
    }

    internal static RawDiagnostic CreateDiagnostic(RawMakerNote? note, RawCompression compression)
    {
        long offset = note == null ? -1 : note.Offset;
        if (note != null)
        {
            RawNikonMakerNoteTag? tag = note.FindNikonTag(0x0093);
            if (tag != null)
            {
                offset = note.Offset + tag.ValuePosition;
            }
        }
        return new RawDiagnostic(RawDiagnosticCode.UnsupportedCodec, RawSeverity.Warning, RawFormat.Nef, offset, note == null ? string.Empty : note.IfdPath, 0x0093, Stage, GetMessage(compression), false);
    }

    internal static void ApplyToImages(System.Collections.Generic.List<RawImageInfo> images, NefStructure structure, RawNikonNefMetadata metadata)
    {
        if (images == null || structure == null || structure.RawImages == null || metadata == null || !IsCompression(metadata.Compression))
        {
            return;
        }
        int index = 0;
        while (index < structure.RawImages.Length)
        {
            NefRawImage raw = structure.RawImages[index];
            int modelIndex = raw.Descriptor.ImageIndex;
            if (modelIndex >= 0 && modelIndex < images.Count && raw.Descriptor.Width == (ulong)metadata.StoredWidth && raw.Descriptor.Height == (ulong)metadata.StoredHeight)
            {
                RawImageInfo image = images[modelIndex];
                image.Compression = metadata.Compression;
                image.Cfa = CanonCr2MetadataParser.CopyCfa(metadata.Cfa);
                image.ActiveArea = CanonCr2MetadataParser.CopyArea(metadata.ActiveArea, RawAreaKind.Active);
                image.DefaultCrop = CanonCr2MetadataParser.CopyArea(metadata.DefaultCrop, RawAreaKind.DefaultCrop);
                if (image.Payload != null)
                {
                    image.Payload.Compression = metadata.Compression;
                    image.Payload.Decodable = false;
                }
            }
            index++;
        }
    }
}
