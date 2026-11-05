using System;

namespace avif;

internal class MinimizedImageWriteModel
{
    internal bool Representable;
    internal CanonicalItemPayload? MainItem;
    internal byte[]? Configuration;
    internal byte[] IccProfile;
    internal int Width;
    internal int Height;
    internal int BitDepth;
    internal int ChromaSubsampling;
    internal bool HorizontalCentered;
    internal bool VerticalCentered;
    internal bool FullRange;
    internal int Primaries;
    internal int Transfer;
    internal int Matrix;
    internal int Orientation;

    internal MinimizedImageWriteModel()
    {
        Representable = false;
        MainItem = null;
        Configuration = System.Array.Empty<byte>();
        IccProfile = System.Array.Empty<byte>();
        Width = 0;
        Height = 0;
        BitDepth = 0;
        ChromaSubsampling = 0;
        HorizontalCentered = false;
        VerticalCentered = false;
        FullRange = false;
        Primaries = 0;
        Transfer = 0;
        Matrix = 0;
        Orientation = 1;
    }
}

internal static class MinimizedImageBoxWriter
{
    internal static MinimizedImageWriteModel CreateModel(
        CanonicalItemContainerRequest request)
    {
        MinimizedImageWriteModel model = new MinimizedImageWriteModel();
        if (request == null
            || request.Options == null
            || request.Items == null
            || request.References == null
            || request.Groups == null
            || request.Items.Count != 1
            || request.PrimaryItemId != 1
            || request.References.Count != 0
            || request.Groups.Count != 0
            || request.Options.UseItemData
            || request.Options.Force64BitOffsets
            || request.Options.ForceLargeBoxHeaders)
        {
            return model;
        }

        CanonicalItemPayload item = request.Items[0];
        if (item == null
            || item.ItemId != 1
            || item.ItemType != "av01"
            || item.Name != string.Empty
            || item.Hidden
            || !item.HasData
            || item.Data == null
            || item.Data.LongLength == 0
            || item.Data.LongLength > 268435456
            || item.Properties == null)
        {
            return model;
        }

        ParsedAv1Configuration? configuration = null;
        ParsedImageSpatialExtents? extents = null;
        ParsedPixelInformation? pixels = null;
        ParsedColourInformation? nclx = null;
        ParsedColourInformation? icc = null;
        ParsedImageRotation? rotation = null;
        ParsedImageMirror? mirror = null;
        byte[]? configurationBytes = null;
        int associationIndex = 0;
        while (associationIndex < item.Properties.Count)
        {
            CanonicalItemPropertyAssociation association = item.Properties[associationIndex];
            if (association == null || association.RawProperty == null)
            {
                return model;
            }

            CanonicalParsedProperty parsed = CanonicalBrandSelector.ParseProperty(
                association.RawProperty);
            if (parsed.Code != AvifResultCode.Success)
            {
                return model;
            }

            string type = (parsed.Box ?? throw new InvalidOperationException("Invalid internal state.")).Type;
            if (type == "av1C" && configuration == null && association.Essential)
            {
                Av1ItemConfigurationParseResult value =
                    Av1ItemConfigurationBox.ParsePayload(
                        association.RawProperty.AsSpan(
                            (int)(parsed.Box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset,
                            (int)parsed.Box.PayloadSize));
                if (value.Code != AvifResultCode.Success || parsed.Box.PayloadSize != 4)
                {
                    return model;
                }

                configuration = value.Configuration;
                configurationBytes = association.RawProperty.AsSpan(
                    (int)parsed.Box.PayloadOffset,
                    4).ToArray();
            }
            else if (type == "ispe" && extents == null && !association.Essential)
            {
                ImageSpatialExtentsParseResult value = ImageSpatialExtentsProperty.Parse(
                    association.RawProperty,
                    parsed.Box,
                    parsed.Resources);
                if (value.Code != AvifResultCode.Success)
                {
                    return model;
                }

                extents = value.Extents;
            }
            else if (type == "pixi" && pixels == null && !association.Essential)
            {
                PixelInformationParseResult value = PixelInformationProperty.Parse(
                    association.RawProperty,
                    parsed.Box);
                if (value.Code != AvifResultCode.Success || (value.Information ?? throw new InvalidOperationException("Invalid internal state.")).IsExtended())
                {
                    return model;
                }

                pixels = value.Information;
            }
            else if (type == "colr")
            {
                AdditionalItemPropertyParseResult value = AdditionalItemPropertyBox.Parse(
                    association.RawProperty,
                    parsed.Box,
                    parsed.Resources);
                if (value.Code != AvifResultCode.Success
                    || value.ColourInformation == null
                    || !association.Essential
                        && request.Options.HeaderFormat
                            != AvifHeaderFormat.Minimized)
                {
                    return model;
                }

                if (value.ColourInformation.GetColourType() == "nclx" && nclx == null)
                {
                    nclx = value.ColourInformation;
                }
                else if (value.ColourInformation.GetColourType() == "prof" && icc == null)
                {
                    icc = value.ColourInformation;
                }
                else
                {
                    return model;
                }
            }
            else if (type == "irot" && rotation == null && association.Essential)
            {
                AdditionalItemPropertyParseResult value = AdditionalItemPropertyBox.Parse(
                    association.RawProperty,
                    parsed.Box,
                    parsed.Resources);
                if (value.Code != AvifResultCode.Success || value.ImageRotation == null)
                {
                    return model;
                }

                rotation = value.ImageRotation;
            }
            else if (type == "imir" && mirror == null && association.Essential)
            {
                AdditionalItemPropertyParseResult value = AdditionalItemPropertyBox.Parse(
                    association.RawProperty,
                    parsed.Box,
                    parsed.Resources);
                if (value.Code != AvifResultCode.Success || value.ImageMirror == null)
                {
                    return model;
                }

                mirror = value.ImageMirror;
            }
            else
            {
                return model;
            }

            associationIndex++;
        }

        if (configuration == null || extents == null || pixels == null || nclx == null)
        {
            return model;
        }

        int width = (int)extents.GetWidth();
        int height = (int)extents.GetHeight();
        if (width > 32768
            || height > 32768
            || !PixelsMatchConfiguration(pixels, configuration)
            || !TrySetChroma(model, configuration)
            || !TrySetOrientation(model, rotation, mirror)
            || nclx.GetColourPrimaries() > byte.MaxValue
            || nclx.GetTransferCharacteristics() > byte.MaxValue
            || nclx.GetMatrixCoefficients() > byte.MaxValue)
        {
            return new MinimizedImageWriteModel();
        }

        byte[] iccBytes = icc == null ? System.Array.Empty<byte>() : icc.GetData();
        if (iccBytes.LongLength > 1048576)
        {
            return new MinimizedImageWriteModel();
        }

        model.Representable = true;
        model.MainItem = item;
        model.Configuration = configurationBytes;
        model.IccProfile = iccBytes;
        model.Width = width;
        model.Height = height;
        model.BitDepth = configuration.GetBitDepth();
        model.FullRange = nclx.IsFullRange();
        model.Primaries = nclx.GetColourPrimaries();
        model.Transfer = nclx.GetTransferCharacteristics();
        model.Matrix = nclx.GetMatrixCoefficients();
        return model;
    }

    internal static bool PixelsMatchConfiguration(
        ParsedPixelInformation pixels,
        ParsedAv1Configuration configuration)
    {
        if (pixels.GetChannelCount() != configuration.GetChannelCount())
        {
            return false;
        }

        int index = 0;
        while (index < pixels.GetChannelCount())
        {
            ParsedPixelChannel? channel = pixels.GetChannel(index);
            if (channel == null || channel.GetBitDepth() != configuration.GetBitDepth())
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool TrySetChroma(
        MinimizedImageWriteModel model,
        ParsedAv1Configuration configuration)
    {
        if (configuration.IsMonochrome())
        {
            model.ChromaSubsampling = 0;
            return configuration.HasChromaSubsamplingX()
                && configuration.HasChromaSubsamplingY()
                && configuration.GetChromaSamplePosition() == 0;
        }

        if (!configuration.HasChromaSubsamplingX()
            && !configuration.HasChromaSubsamplingY())
        {
            model.ChromaSubsampling = 3;
            return configuration.GetChromaSamplePosition() == 0;
        }

        if (configuration.HasChromaSubsamplingX()
            && !configuration.HasChromaSubsamplingY())
        {
            model.ChromaSubsampling = 2;
            return configuration.GetChromaSamplePosition() == 0;
        }

        if (!configuration.HasChromaSubsamplingX())
        {
            return false;
        }

        model.ChromaSubsampling = 1;
        int position = configuration.GetChromaSamplePosition();
        if (position == 0)
        {
            model.HorizontalCentered = true;
            model.VerticalCentered = false;
            return true;
        }

        if (position == 1)
        {
            model.HorizontalCentered = false;
            model.VerticalCentered = true;
            return true;
        }

        if (position == 2)
        {
            model.HorizontalCentered = false;
            model.VerticalCentered = false;
            return true;
        }

        return false;
    }

    internal static bool TrySetOrientation(
        MinimizedImageWriteModel model,
        ParsedImageRotation? rotation,
        ParsedImageMirror? mirror)
    {
        int angle = rotation == null ? 0 : rotation.GetQuarterTurnsCounterClockwise();
        int axis = mirror == null ? -1 : mirror.GetAxis();
        int orientation = 0;
        if (angle == 0 && axis == -1)
        {
            orientation = 1;
        }
        else if (angle == 0 && axis == 1)
        {
            orientation = 2;
        }
        else if (angle == 2 && axis == -1)
        {
            orientation = 3;
        }
        else if (angle == 0 && axis == 0)
        {
            orientation = 4;
        }
        else if (angle == 1 && axis == 0)
        {
            orientation = 5;
        }
        else if (angle == 3 && axis == -1)
        {
            orientation = 6;
        }
        else if (angle == 3 && axis == 0)
        {
            orientation = 7;
        }
        else if (angle == 1 && axis == -1)
        {
            orientation = 8;
        }

        if (orientation == 0)
        {
            return false;
        }

        model.Orientation = orientation;
        return true;
    }

    internal static CanonicalItemContainerLayout Measure(
        MinimizedImageWriteModel model,
        long maximumOutputBytes)
    {
        CanonicalItemContainerLayout layout = new CanonicalItemContainerLayout(
            AvifResultCode.InvalidArgument);
        if (model == null || !model.Representable || maximumOutputBytes < 0)
        {
            return layout;
        }

        byte[] header = BuildHeader(model);
        if (header.Length == 0)
        {
            return layout;
        }

        long miniSize = 8L + header.Length + (model.Configuration ?? throw new InvalidOperationException("Invalid internal state.")).Length
            + model.IccProfile.Length + (model.MainItem ?? throw new InvalidOperationException("Invalid internal state.")).Data.LongLength;
        long fileSize = 16 + miniSize;
        if (fileSize > maximumOutputBytes || fileSize > System.Array.MaxLength)
        {
            return new CanonicalItemContainerLayout(AvifResultCode.LimitExceeded);
        }

        layout.Code = AvifResultCode.Success;
        layout.FileTypeSize = 16;
        layout.MinimizedImageSize = miniSize;
        layout.FileSize = fileSize;
        layout.MediaPayloadOffset = 24 + header.Length
            + model.Configuration.Length + model.IccProfile.Length;
        layout.UsesMinimizedFormat = true;
        layout.ItemOffsets.Add(layout.MediaPayloadOffset);
        return layout;
    }

    internal static AvifResultCode Write(
        IsoBoxWriter writer,
        MinimizedImageWriteModel model)
    {
        if (writer == null || model == null || !model.Representable)
        {
            return AvifResultCode.InvalidArgument;
        }

        byte[] header = BuildHeader(model);
        if (header.Length == 0)
        {
            return AvifResultCode.InvalidState;
        }

        AvifResultCode code = FileTypeBox.Write(
            writer,
            "mif3",
            IsoBoxType.GetValue("avif"),
            System.Array.Empty<string>());
        if (code == AvifResultCode.Success)
        {
            code = writer.BeginBox("mini");
        }

        if (code == AvifResultCode.Success)
        {
            code = writer.Write(header);
        }

        if (code == AvifResultCode.Success)
        {
            code = writer.Write(model.Configuration);
        }

        if (code == AvifResultCode.Success && model.IccProfile.Length > 0)
        {
            code = writer.Write(model.IccProfile);
        }

        if (code == AvifResultCode.Success)
        {
            code = writer.Write((model.MainItem ?? throw new InvalidOperationException("Invalid internal state.")).Data);
        }

        return code == AvifResultCode.Success ? writer.EndBox() : code;
    }

    internal static byte[] BuildHeader(MinimizedImageWriteModel model)
    {
        byte[] buffer = new byte[64];
        BitWriter writer = BitWriter.Create(buffer.Length);
        bool hasIcc = model.IccProfile.Length > 0;
        bool largeDimensions = model.Width > 128 || model.Height > 128;
        bool largeMetadata = model.IccProfile.Length > 1024;
        bool largeItemData = (model.MainItem ?? throw new InvalidOperationException("Invalid internal state.")).Data.LongLength > 32768;
        int dimensionBits = largeDimensions ? 15 : 7;
        int metadataBits = largeMetadata ? 20 : 10;
        int itemDataBits = largeItemData ? 28 : 15;
        writer.WriteBits(buffer, 0, 2);
        writer.WriteBit(buffer, 0);
        writer.WriteBit(buffer, 0);
        writer.WriteBit(buffer, model.FullRange ? 1 : 0);
        writer.WriteBit(buffer, 0);
        writer.WriteBit(buffer, 1);
        writer.WriteBit(buffer, 0);
        writer.WriteBit(buffer, hasIcc ? 1 : 0);
        writer.WriteBit(buffer, 0);
        writer.WriteBit(buffer, 0);
        writer.WriteBits(buffer, (ulong)model.ChromaSubsampling, 2);
        writer.WriteBits(buffer, (ulong)(model.Orientation - 1), 3);
        writer.WriteBit(buffer, largeDimensions ? 1 : 0);
        writer.WriteBits(buffer, (ulong)(model.Width - 1), dimensionBits);
        writer.WriteBits(buffer, (ulong)(model.Height - 1), dimensionBits);
        if (model.ChromaSubsampling == 1 || model.ChromaSubsampling == 2)
        {
            writer.WriteBit(buffer, model.HorizontalCentered ? 1 : 0);
        }

        if (model.ChromaSubsampling == 1)
        {
            writer.WriteBit(buffer, model.VerticalCentered ? 1 : 0);
        }

        writer.WriteBit(buffer, model.BitDepth > 8 ? 1 : 0);
        if (model.BitDepth > 8)
        {
            writer.WriteBits(buffer, (ulong)(model.BitDepth - 9), 3);
        }

        writer.WriteBits(buffer, (ulong)model.Primaries, 8);
        writer.WriteBits(buffer, (ulong)model.Transfer, 8);
        writer.WriteBits(buffer, (ulong)model.Matrix, 8);
        if (hasIcc)
        {
            writer.WriteBit(buffer, largeMetadata ? 1 : 0);
        }

        writer.WriteBit(buffer, 0);
        writer.WriteBit(buffer, largeItemData ? 1 : 0);
        if (hasIcc)
        {
            writer.WriteBits(buffer, (ulong)(model.IccProfile.Length - 1), metadataBits);
        }

        writer.WriteBits(buffer, 4, 3);
        writer.WriteBits(buffer, (ulong)(model.MainItem.Data.LongLength - 1), itemDataBits);
        while (writer.Code == AvifResultCode.Success && !writer.IsByteAligned())
        {
            writer.WriteBit(buffer, 0);
        }

        if (writer.Code != AvifResultCode.Success)
        {
            return System.Array.Empty<byte>();
        }

        return buffer.AsSpan(0, (int)(writer.BitPosition / 8)).ToArray();
    }
}
