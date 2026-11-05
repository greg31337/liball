namespace raw;

internal static class RawThumbnailCodec
{
    internal static RawThumbnail GetDocumentThumbnail(RawDocument document, int index)
    {
        if (document == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A document is required for thumbnail selection.", -1);
        }
        return document.GetThumbnail(index);
    }

    internal static byte[] ExtractDocumentPayload(RawDocument document, RawPayloadBlock block, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        RequireLimits(limits);
        cancellationToken.ThrowIfCancellationRequested();
        if (document == null || block == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A document and one of its mapped payload blocks are required.", -1);
        }
        CheckedMath.FileSize(block.Length, limits);
        CheckedMath.AllocationSize(block.Length, 1, limits);
        byte[] result = document.ExtractPayload(block);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    internal static RawRgbImage DecodeThumbnail(RawThumbnail thumbnail, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        RequireLimits(limits);
        cancellationToken.ThrowIfCancellationRequested();
        if (thumbnail == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A thumbnail is required for decode.", -1);
        }
        if (thumbnail.EncodedBytes == null || thumbnail.EncodedBytes.LongLength > limits.MaxPreviewBytes || thumbnail.EncodedBytes.LongLength > limits.MaxAllocationBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The encoded thumbnail exceeds the configured preview or allocation limit.", -1);
        }
        return thumbnail.Decode(limits, cancellationToken);
    }

    internal static RawThumbnail EncodeJpegThumbnail(RawRgbImage image, int index, int quality, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        RequireLimits(limits);
        cancellationToken.ThrowIfCancellationRequested();
        if (image == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A valid rendered image is required for baseline JPEG encoding.", -1);
        }
        long pixelCount = CheckedMath.PixelCount(image.Width, image.Height, limits);
        if (!image.Validate(limits).IsValid())
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A valid rendered image is required for baseline JPEG encoding.", -1);
        }
        if (index < 0 || quality < 1 || quality > 100)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "The thumbnail index must be nonnegative and JPEG quality must be between one and 100.", -1);
        }
        if (image.ChannelCount != 1 && image.ChannelCount != 3 || image.ByteSamples == null || image.ByteSamples.Length == 0)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "Baseline JPEG thumbnail encoding requires an eight-bit grayscale or RGB image.", -1);
        }
        if (image.ColorSpace != RawColorSpace.Srgb || image.TransferFunction != RawTransferFunction.Srgb)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "Baseline JPEG thumbnail encoding requires sRGB color and transfer semantics.", -1);
        }

        int planeCount = image.ChannelCount == 1 ? 1 : 3;
        CheckedMath.DecodedSize(pixelCount, planeCount, limits);
        CheckedMath.AllocationSize(pixelCount, 1, limits);
        BaselineJpegImage jpeg = new BaselineJpegImage();
        jpeg.Width = image.Width;
        jpeg.Height = image.Height;
        jpeg.Precision = 8;
        BaselineJpegPlane first = CreatePlane(1, image.Width, image.Height, pixelCount);
        jpeg.Planes.Add(first);
        if (planeCount == 1)
        {
            int grayIndex = FindChannel(image, RawChannelKind.Gray);
            if (grayIndex < 0)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidDescriptor, "A grayscale JPEG source requires one gray channel.", -1);
            }
            CopyGray(image, first, grayIndex, cancellationToken);
        }
        else
        {
            int redIndex = FindChannel(image, RawChannelKind.Red);
            int greenIndex = FindChannel(image, RawChannelKind.Green);
            int blueIndex = FindChannel(image, RawChannelKind.Blue);
            if (redIndex < 0 || greenIndex < 0 || blueIndex < 0)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidDescriptor, "An RGB JPEG source requires unique red, green, and blue channels.", -1);
            }
            BaselineJpegPlane cb = CreatePlane(2, image.Width, image.Height, pixelCount);
            BaselineJpegPlane cr = CreatePlane(3, image.Width, image.Height, pixelCount);
            jpeg.Planes.Add(cb);
            jpeg.Planes.Add(cr);
            ConvertRgb(image, first, cb, cr, redIndex, greenIndex, blueIndex, cancellationToken);
        }

        byte[] encoded = BaselineJpegEncoder.Encode(jpeg, new BaselineJpegEncodeParameters(quality, 1, 1, 0), limits, cancellationToken);
        if (encoded.LongLength > limits.MaxPreviewBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The encoded JPEG thumbnail exceeds the configured preview limit.", -1);
        }
        RawOrientation orientation = image.OrientationApplied ? RawOrientation.Normal : image.Orientation;
        RawThumbnail result = new RawThumbnail(index, RawThumbnailKind.Jpeg, image.Width, image.Height, orientation, "image/jpeg", encoded, null, image.Diagnostics);
        if (!result.Validate(limits).IsValid())
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The encoded JPEG thumbnail did not pass model validation.", -1);
        }
        return result;
    }

    internal static BaselineJpegPlane CreatePlane(byte identifier, int width, int height, long pixelCount)
    {
        BaselineJpegPlane plane = new BaselineJpegPlane();
        plane.Identifier = identifier;
        plane.Width = width;
        plane.Height = height;
        plane.HorizontalSampling = 1;
        plane.VerticalSampling = 1;
        plane.Samples = new byte[CheckedMath.ToInt32(pixelCount)];
        return plane;
    }

    internal static int FindChannel(RawRgbImage image, RawChannelKind kind)
    {
        int found = -1;
        int index = 0;
        while (index < image.Channels.Length)
        {
            if (image.Channels[index].Kind == kind)
            {
                if (found >= 0)
                {
                    return -1;
                }
                found = index;
            }
            index++;
        }
        return found;
    }

    internal static void CopyGray(RawRgbImage image, BaselineJpegPlane plane, int grayIndex, System.Threading.CancellationToken cancellationToken)
    {
        int y = 0;
        while (y < image.Height)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int x = 0;
            while (x < image.Width)
            {
                (plane.Samples ?? throw new System.InvalidOperationException("A required value is unavailable."))[y * image.Width + x] = image.ByteSamples[y * image.RowStrideSamples + x * image.ChannelCount + grayIndex];
                x++;
            }
            y++;
        }
    }

    internal static void ConvertRgb(RawRgbImage image, BaselineJpegPlane yPlane, BaselineJpegPlane cbPlane, BaselineJpegPlane crPlane, int redIndex, int greenIndex, int blueIndex, System.Threading.CancellationToken cancellationToken)
    {
        int y = 0;
        while (y < image.Height)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int x = 0;
            while (x < image.Width)
            {
                int source = y * image.RowStrideSamples + x * image.ChannelCount;
                int destination = y * image.Width + x;
                int red = image.ByteSamples[source + redIndex];
                int green = image.ByteSamples[source + greenIndex];
                int blue = image.ByteSamples[source + blueIndex];
                (yPlane.Samples ?? throw new System.InvalidOperationException("A required value is unavailable."))[destination] = YCbCrConverter.Clip(0.299 * red + 0.587 * green + 0.114 * blue);
                (cbPlane.Samples ?? throw new System.InvalidOperationException("A required value is unavailable."))[destination] = YCbCrConverter.Clip(128.0 - 0.168736 * red - 0.331264 * green + 0.5 * blue);
                (crPlane.Samples ?? throw new System.InvalidOperationException("A required value is unavailable."))[destination] = YCbCrConverter.Clip(128.0 + 0.5 * red - 0.418688 * green - 0.081312 * blue);
                x++;
            }
            y++;
        }
    }

    internal static void RequireLimits(RawLimits limits)
    {
        if (limits == null || !limits.Validate().IsValid())
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "Valid resource limits are required for thumbnail operations.", -1);
        }
    }
}
