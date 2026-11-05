namespace icc;

internal class EmbeddedImageInspection
{
    internal bool Success;
    internal bool LimitExceeded;
    internal int Width;
    internal int Height;
    internal int Components;
    internal bool IsCompressed;
    internal string Error;

    internal EmbeddedImageInspection()
    {
        Success = false;
        LimitExceeded = false;
        Width = 0;
        Height = 0;
        Components = 0;
        IsCompressed = false;
        Error = string.Empty;
    }
}

internal static class EmbeddedImageTagCodec
{
    internal const int NormalHeaderSize = 16;
    internal const int HeightHeaderSize = 24;
    internal const uint PngEncoding = 0;
    internal const uint TiffEncoding = 1;
    internal const int DefaultMaximumExpandedBytes = 67108864;

    internal static void DecodeEmbeddedImage(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        bool heightMap = typeSignature == IccTypeSignatures.EmbeddedHeightImage;
        int headerSize = heightMap ? HeightHeaderSize : NormalHeaderSize;
        if (payload.Length <= headerSize)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"An embedded {(heightMap ? "height" : "normal")} image requires a {headerSize}-byte header followed by image data.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The embedded-image reserved bytes shall be zero.");
            return;
        }

        BigEndianReader reader = new BigEndianReader(payload, state.AbsolutePayloadOffset);
        reader.SkipExactBytes(8);
        uint seamless = reader.ReadUInt32();
        uint encoding = reader.ReadUInt32();
        if (seamless > 1)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The embedded-image seamless indicator shall be zero or one.");
            return;
        }

        if (encoding != PngEncoding && encoding != TiffEncoding)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The embedded-image encoding format shall identify PNG or TIFF.");
            return;
        }

        double minimumHeight = 0.0;
        double maximumHeight = 0.0;
        if (heightMap)
        {
            if (!Icc2ArrayTagCodec.ContainsOnlyFiniteFloatingPointValues(payload.Slice(16, 8), IccTypeSignatures.Float32Array))
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "Embedded height-image limits shall be finite and ordered from minimum to maximum.");
                return;
            }

            minimumHeight = IccNumberCodec.ReadFloat32(reader);
            maximumHeight = IccNumberCodec.ReadFloat32(reader);
            if (!CurveTagCodec.IsFinite(minimumHeight) || !CurveTagCodec.IsFinite(maximumHeight) || minimumHeight > maximumHeight)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "Embedded height-image limits shall be finite and ordered from minimum to maximum.");
                return;
            }
        }

        if (state.HasProfileContext && state.CurrentNestingDepth == 0 && state.TagSignature != IccTagSignatures.SurfaceMap)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "An embedded height or normal image is permitted as top-level data only for the surfaceMapTag.");
            return;
        }

        System.ReadOnlyMemory<byte> imageBytes = payload.Slice(headerSize);
        EmbeddedImageInspection inspection = InspectImage(imageBytes, encoding, heightMap, state.MaximumDecompressedBytes);
        if (!inspection.Success)
        {
            if (inspection.LimitExceeded)
            {
                CompleteLimitFailure(state, payload, typeSignature, inspection.Error);
            }
            else
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, inspection.Error);
            }

            return;
        }

        IccEmbeddedImage image = new IccEmbeddedImage();
        image.Width = inspection.Width;
        image.Height = inspection.Height;
        image.Components = inspection.Components;
        image.EncodingSignature = encoding;
        image.Bytes = imageBytes.ToArray();
        image.IsCompressed = inspection.IsCompressed;
        image.IsSeamless = seamless == 1;
        image.MinimumHeightMeters = minimumHeight;
        image.MaximumHeightMeters = maximumHeight;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.EmbeddedImage);
        data.EmbeddedImage = image;
        if (state.CurrentNestingDepth == 0 && state.TagSignature == IccTagSignatures.SurfaceMap)
        {
            data.SurfaceMap = CreateSurfaceMap(image, heightMap);
        }

        string kind = heightMap ? "height" : "normal";
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded a {inspection.Width} by {inspection.Height} embedded {kind} image.");
    }

    internal static byte[] SerializeEmbeddedImage(IccEmbeddedImage image, uint typeSignature)
    {
        return SerializeEmbeddedImageWithLimit(image, typeSignature, DefaultMaximumExpandedBytes);
    }

    internal static byte[] SerializeEmbeddedImageWithLimit(IccEmbeddedImage image, uint typeSignature, int maximumExpandedBytes)
    {
        if (image == null || image.Bytes == null)
        {
            throw new IccProfileWriteException("Embedded image data and bytes cannot be null.");
        }

        bool heightMap = typeSignature == IccTypeSignatures.EmbeddedHeightImage;
        if (!heightMap && typeSignature != IccTypeSignatures.EmbeddedNormalImage)
        {
            throw new IccProfileWriteException("An embedded image shall use the embedded height-image or normal-image type signature.");
        }

        if (image.EncodingSignature != PngEncoding && image.EncodingSignature != TiffEncoding)
        {
            throw new IccProfileWriteException("The embedded-image encoding format shall identify PNG or TIFF.");
        }

        if (maximumExpandedBytes < 0)
        {
            throw new IccProfileWriteException("The maximum expanded image size cannot be negative.");
        }

        if (heightMap && (!CurveTagCodec.IsFinite(image.MinimumHeightMeters)
            || !CurveTagCodec.IsFinite(image.MaximumHeightMeters)
            || image.MinimumHeightMeters > image.MaximumHeightMeters))
        {
            throw new IccProfileWriteException("Embedded height-image limits shall be finite and ordered from minimum to maximum.");
        }

        EmbeddedImageInspection inspection = InspectImage(image.Bytes, image.EncodingSignature, heightMap, maximumExpandedBytes);
        if (!inspection.Success)
        {
            throw new IccProfileWriteException(inspection.Error);
        }

        if (image.Width != inspection.Width || image.Height != inspection.Height || image.Components != inspection.Components)
        {
            throw new IccProfileWriteException("Embedded-image dimensions and component count shall match the encoded image metadata.");
        }

        if (image.IsCompressed != inspection.IsCompressed)
        {
            throw new IccProfileWriteException("The embedded-image compression flag shall match the encoded image metadata.");
        }

        int headerSize = heightMap ? HeightHeaderSize : NormalHeaderSize;
        long payloadSize = headerSize + (long)image.Bytes.Length;
        if (payloadSize > System.Array.MaxLength)
        {
            throw new IccProfileWriteException("The embedded-image payload exceeds the supported managed array size.");
        }

        byte[] payload = new byte[(int)payloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(typeSignature);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32(image.IsSeamless ? 1u : 0u);
        writer.WriteUInt32(image.EncodingSignature);
        if (heightMap)
        {
            IccNumberCodec.WriteFloat32(writer, IccNumberCodec.ConvertDoubleToFloat32(image.MinimumHeightMeters));
            IccNumberCodec.WriteFloat32(writer, IccNumberCodec.ConvertDoubleToFloat32(image.MaximumHeightMeters));
        }

        writer.WriteExactBytes(image.Bytes);
        return payload;
    }

    internal static EmbeddedImageInspection InspectImage(System.ReadOnlyMemory<byte> bytes, uint encoding, bool heightMap, int maximumExpandedBytes)
    {
        if (maximumExpandedBytes < 0)
        {
            EmbeddedImageInspection invalidLimit = new EmbeddedImageInspection();
            invalidLimit.LimitExceeded = true;
            invalidLimit.Error = "The maximum expanded image size cannot be negative.";
            return invalidLimit;
        }

        if (encoding == PngEncoding)
        {
            return PngImageInspector.InspectPng(bytes, heightMap, maximumExpandedBytes);
        }

        return TiffImageInspector.InspectTiff(bytes, heightMap, maximumExpandedBytes);
    }

    internal static IccSurfaceMap CreateSurfaceMap(IccEmbeddedImage image, bool heightMap)
    {
        IccSurfaceMap surface = new IccSurfaceMap();
        surface.Width = image.Width;
        surface.Height = image.Height;
        if (heightMap)
        {
            surface.HeightImage = image;
            surface.Scale = new double[] { image.MinimumHeightMeters, image.MaximumHeightMeters };
        }
        else
        {
            surface.NormalImage = image;
        }

        return surface;
    }

    internal static void CompleteLimitFailure(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, string message)
    {
        IccTagData data = new IccTagData();
        if (state.PreserveOriginalBytes)
        {
            UnknownTagCodec.PopulateRawFallback(data, payload, typeSignature, true);
        }

        IccOperationResult result = UnknownTagCodec.CreateDecodeResult(state, IccOperationStatus.ResourceLimitExceeded);
        result.Message = message;
        UnknownTagCodec.CompleteDecodeState(state, data, result);
    }
}
