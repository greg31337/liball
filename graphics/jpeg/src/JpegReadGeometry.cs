namespace jpeg;

internal class JpegReadGeometry
{
    internal int SourceWidth;
    internal int SourceHeight;
    internal int ScaleBlockSize;
    internal int ScaledWidth;
    internal int ScaledHeight;
    internal JpegRectangle SourceRegion = new JpegRectangle();
    internal JpegRectangle OutputRectangle = new JpegRectangle();
    internal bool FastMcuCropApplied;
}

internal class JpegReadGeometryResolver
{
    internal JpegScaledIntegerInverseDct ScaledInverseDct = new JpegScaledIntegerInverseDct();
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();

    internal JpegReadGeometry Resolve(JpegFrameHeader frame, JpegReadOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        ValidateTargets(options);
        int scaleBlockSize = ResolveScaleBlockSize(layout.Width, layout.Height, options);
        JpegRectangle sourceRegion = ResolveSourceRegion(layout, options);

        JpegReadGeometry geometry = new JpegReadGeometry();
        geometry.SourceWidth = layout.Width;
        geometry.SourceHeight = layout.Height;
        geometry.ScaleBlockSize = scaleBlockSize;
        geometry.ScaledWidth = ScaleDimension(layout.Width, scaleBlockSize);
        geometry.ScaledHeight = ScaleDimension(layout.Height, scaleBlockSize);
        geometry.SourceRegion = sourceRegion;
        geometry.OutputRectangle = ScaleRectangle(sourceRegion, scaleBlockSize);
        geometry.FastMcuCropApplied = options.FastMcuCrop && RegionWasRequested(options.DecodeRegion);
        return geometry;
    }

    internal int ResolveScaleBlockSize(int sourceWidth, int sourceHeight, JpegReadOptions options)
    {
        if (options.TargetWidth > 0 || options.TargetHeight > 0)
        {
            int candidate = JpegConstants.BlockWidth;
            while (candidate > 1)
            {
                if (TargetAcceptsScale(sourceWidth, sourceHeight, candidate, options.TargetWidth, options.TargetHeight))
                {
                    return candidate;
                }

                candidate = candidate - 1;
            }

            return 1;
        }

        int namedScale = (int)options.IdctScaleRatio;
        if (namedScale != 0)
        {
            if (namedScale < 1 || namedScale > JpegConstants.BlockWidth)
            {
                throw new System.ArgumentOutOfRangeException("IdctScaleRatio");
            }

            return namedScale;
        }

        return ScaledInverseDct.GetScaledBlockSize(options.IdctScaleNumerator, options.IdctScaleDenominator);
    }

    internal bool TargetAcceptsScale(int sourceWidth, int sourceHeight, int scaleBlockSize, int targetWidth, int targetHeight)
    {
        int width = ScaleDimension(sourceWidth, scaleBlockSize);
        int height = ScaleDimension(sourceHeight, scaleBlockSize);
        bool widthAccepted = targetWidth == 0 || width <= targetWidth;
        bool heightAccepted = targetHeight == 0 || height <= targetHeight;
        return widthAccepted && heightAccepted;
    }

    internal int ScaleDimension(int dimension, int scaleBlockSize)
    {
        long scaled = (long)dimension * (long)scaleBlockSize;
        long result = (scaled + JpegConstants.BlockWidth - 1) / JpegConstants.BlockWidth;
        if (result > int.MaxValue)
        {
            throw JpegResourceLimitException.Create("Scaled output dimension is too large.");
        }

        return (int)result;
    }

    internal JpegRectangle ResolveSourceRegion(JpegMcuLayout layout, JpegReadOptions options)
    {
        if (options.DecodeRegion == null)
        {
            throw new System.ArgumentNullException("DecodeRegion");
        }

        JpegRectangle requested = options.DecodeRegion;
        if (!RegionWasRequested(requested))
        {
            if (requested.X != 0 || requested.Y != 0)
            {
                throw JpegInvalidDataException.Create("An empty decode region must start at the origin.");
            }

            return CreateRectangle(0, 0, layout.Width, layout.Height);
        }

        ValidateRequestedRegion(requested, layout.Width, layout.Height);
        if (!options.FastMcuCrop)
        {
            return CreateRectangle(requested.X, requested.Y, requested.Width, requested.Height);
        }

        int left = requested.X / layout.McuWidth * layout.McuWidth;
        int top = requested.Y / layout.McuHeight * layout.McuHeight;
        int right = AlignEnd(requested.X + requested.Width, layout.McuWidth, layout.Width);
        int bottom = AlignEnd(requested.Y + requested.Height, layout.McuHeight, layout.Height);
        return CreateRectangle(left, top, right - left, bottom - top);
    }

    internal bool RegionWasRequested(JpegRectangle rectangle)
    {
        return rectangle.Width != 0 || rectangle.Height != 0;
    }

    internal void ValidateRequestedRegion(JpegRectangle rectangle, int sourceWidth, int sourceHeight)
    {
        if (rectangle.X < 0 || rectangle.Y < 0 || rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            throw JpegInvalidDataException.Create("Decode region coordinates and dimensions are invalid.");
        }

        long right = (long)rectangle.X + (long)rectangle.Width;
        long bottom = (long)rectangle.Y + (long)rectangle.Height;
        if (right > sourceWidth || bottom > sourceHeight)
        {
            throw JpegInvalidDataException.Create("Decode region exceeds the encoded image bounds.");
        }
    }

    internal int AlignEnd(int value, int alignment, int limit)
    {
        long aligned = ((long)value + (long)alignment - 1) / alignment * alignment;
        if (aligned > limit)
        {
            return limit;
        }

        return (int)aligned;
    }

    internal JpegRectangle ScaleRectangle(JpegRectangle source, int scaleBlockSize)
    {
        long sourceRight = (long)source.X + (long)source.Width;
        long sourceBottom = (long)source.Y + (long)source.Height;
        int left = (int)((long)source.X * scaleBlockSize / JpegConstants.BlockWidth);
        int top = (int)((long)source.Y * scaleBlockSize / JpegConstants.BlockHeight);
        int right = (int)((sourceRight * scaleBlockSize + JpegConstants.BlockWidth - 1) / JpegConstants.BlockWidth);
        int bottom = (int)((sourceBottom * scaleBlockSize + JpegConstants.BlockHeight - 1) / JpegConstants.BlockHeight);
        return CreateRectangle(left, top, right - left, bottom - top);
    }

    internal JpegRectangle CreateRectangle(int x, int y, int width, int height)
    {
        JpegRectangle rectangle = new JpegRectangle();
        rectangle.X = x;
        rectangle.Y = y;
        rectangle.Width = width;
        rectangle.Height = height;
        return rectangle;
    }

    internal void ValidateTargets(JpegReadOptions options)
    {
        if (options.TargetWidth < 0 || options.TargetHeight < 0)
        {
            throw JpegInvalidDataException.Create("Target dimensions cannot be negative.");
        }
    }

    internal JpegReadOptions CreateResolvedOptions(JpegReadOptions source, JpegReadGeometry geometry)
    {
        JpegReadOptions copy = new JpegReadOptions();
        copy.MetadataPolicy = source.MetadataPolicy;
        copy.ColorPolicy = source.ColorPolicy;
        copy.IccBehaviorPolicy = source.IccBehaviorPolicy;
        copy.ErrorPolicy = source.ErrorPolicy;
        copy.OutputPixelFormat = source.OutputPixelFormat;
        copy.ChromaUpsamplingMode = source.ChromaUpsamplingMode;
        copy.RawComponentOutput = source.RawComponentOutput;
        copy.UnknownColorHandling = source.UnknownColorHandling;
        copy.AdobeInversionMode = source.AdobeInversionMode;
        copy.YCbCrRange = source.YCbCrRange;
        copy.InverseDctMethod = source.InverseDctMethod;
        copy.IdctScaleNumerator = geometry.ScaleBlockSize;
        copy.IdctScaleDenominator = JpegConstants.BlockWidth;
        copy.IdctScaleRatio = (JpegIdctScaleRatio)geometry.ScaleBlockSize;
        copy.TargetWidth = source.TargetWidth;
        copy.TargetHeight = source.TargetHeight;
        copy.DecodeRegion = source.DecodeRegion;
        copy.FastMcuCrop = source.FastMcuCrop;
        copy.MaxWidth = source.MaxWidth;
        copy.MaxHeight = source.MaxHeight;
        copy.MaxComponents = source.MaxComponents;
        copy.MaxScans = source.MaxScans;
        copy.MaxMarkerPayloadBytes = source.MaxMarkerPayloadBytes;
        copy.MaxRetainedMarkers = source.MaxRetainedMarkers;
        copy.MaxTables = source.MaxTables;
        copy.MaxMetadataBytes = source.MaxMetadataBytes;
        copy.MaxEntropyBytes = source.MaxEntropyBytes;
        copy.MaxOutputPixels = source.MaxOutputPixels;
        copy.MaxOutputBytes = source.MaxOutputBytes;
        copy.MaxComponentBufferBytes = source.MaxComponentBufferBytes;
        copy.MaxProgressiveStateBytes = source.MaxProgressiveStateBytes;
        copy.MaxCallbackOutputBytes = source.MaxCallbackOutputBytes;
        copy.TableStore = source.TableStore;
        copy.AutoOrient = source.AutoOrient;
        copy.SkipMetadata = source.SkipMetadata;
        return copy;
    }
}

internal class JpegReadGeometryProcessor
{
    internal JpegInterleavedPixelBufferAllocator InterleavedAllocator = new JpegInterleavedPixelBufferAllocator();

    internal JpegImage CropOutputImage(JpegImage source, JpegReadGeometry geometry)
    {
        if (source.BufferLayout == JpegBufferLayout.Planar)
        {
            return CropPlanarImage(source, geometry);
        }

        return CropPackedImage(source, geometry);
    }

    internal JpegImage CropPlanarImage(JpegImage source, JpegReadGeometry geometry)
    {
        JpegRectangle rectangle = geometry.OutputRectangle;
        if (rectangle.X == 0 && rectangle.Y == 0 && rectangle.Width == source.Width && rectangle.Height == source.Height)
        {
            return source;
        }

        JpegPlanarImageBuffer sourceBuffer = source.PlanarBuffer;
        JpegPlanarImageBuffer destination = new JpegPlanarImageBuffer();
        destination.Width = rectangle.Width;
        destination.Height = rectangle.Height;
        destination.Precision = sourceBuffer.Precision;
        destination.ComponentCount = sourceBuffer.ComponentCount;
        destination.ColorSpace = sourceBuffer.ColorSpace;
        destination.Subsampling = sourceBuffer.Subsampling;
        destination.Ownership = JpegBufferOwnership.LibraryOwned;

        int index = 0;
        while (index < sourceBuffer.ComponentCount)
        {
            destination.Components.Add(CropPlanarComponent(sourceBuffer.Components[index], sourceBuffer.Width, sourceBuffer.Height, rectangle));
            index = index + 1;
        }

        JpegImage result = new JpegImage();
        result.Width = rectangle.Width;
        result.Height = rectangle.Height;
        result.Precision = source.Precision;
        result.ComponentCount = source.ComponentCount;
        result.PixelFormat = JpegPixelFormat.RawComponents;
        result.ColorSpace = source.ColorSpace;
        result.Subsampling = source.Subsampling;
        result.BufferLayout = JpegBufferLayout.Planar;
        result.BufferOwnership = JpegBufferOwnership.LibraryOwned;
        result.PlanarBuffer = destination;
        return result;
    }

    internal JpegPlanarComponentBuffer CropPlanarComponent(JpegPlanarComponentBuffer source, int imageWidth, int imageHeight, JpegRectangle rectangle)
    {
        int left = (int)((long)rectangle.X * source.Width / imageWidth);
        int top = (int)((long)rectangle.Y * source.Height / imageHeight);
        int right = (int)(((long)(rectangle.X + rectangle.Width) * source.Width + imageWidth - 1) / imageWidth);
        int bottom = (int)(((long)(rectangle.Y + rectangle.Height) * source.Height + imageHeight - 1) / imageHeight);
        int width = right - left;
        int height = bottom - top;

        JpegPlanarComponentBuffer result = new JpegPlanarComponentBuffer();
        result.ComponentIndex = source.ComponentIndex;
        result.Identifier = source.Identifier;
        result.Precision = source.Precision;
        result.HorizontalSamplingFactor = source.HorizontalSamplingFactor;
        result.VerticalSamplingFactor = source.VerticalSamplingFactor;
        result.Width = width;
        result.Height = height;
        result.PaddedWidth = width;
        result.PaddedHeight = height;
        result.Stride = width;
        result.SampleStorage = source.SampleStorage;
        result.Ownership = JpegBufferOwnership.LibraryOwned;
        if (source.SampleStorage == JpegSampleStorage.Byte)
        {
            result.Samples = new byte[width * height];
            int y = 0;
            while (y < height)
            {
                System.Array.Copy(source.Samples, (top + y) * source.Stride + left, result.Samples, y * width, width);
                y = y + 1;
            }
        }
        else
        {
            result.IntSamples = new int[width * height];
            int y = 0;
            while (y < height)
            {
                System.Array.Copy(source.IntSamples, (top + y) * source.Stride + left, result.IntSamples, y * width, width);
                y = y + 1;
            }
        }

        return result;
    }

    internal JpegImage CropPackedImage(JpegImage source, JpegReadGeometry geometry)
    {
        JpegRectangle rectangle = geometry.OutputRectangle;
        if (rectangle.X == 0 && rectangle.Y == 0 && rectangle.Width == source.Width && rectangle.Height == source.Height)
        {
            return source;
        }

        int bytesPerPixel = InterleavedAllocator.GetBytesPerPixel(source.PixelFormat, source.ComponentCount);
        JpegImage result = InterleavedAllocator.CreateImage(rectangle.Width, rectangle.Height, source.PixelFormat, source.ColorSpace, source.ComponentCount);
        result.Precision = source.Precision;
        result.Subsampling = source.Subsampling;
        int rowByteCount = rectangle.Width * bytesPerPixel;
        int sourceStride = source.RowStride;
        if (sourceStride == 0)
        {
            sourceStride = source.Width * bytesPerPixel;
        }

        int y = 0;
        while (y < rectangle.Height)
        {
            int sourceOffset = (rectangle.Y + y) * sourceStride + rectangle.X * bytesPerPixel;
            int destinationOffset = y * result.RowStride;
            System.Array.Copy(source.Pixels, sourceOffset, result.Pixels, destinationOffset, rowByteCount);
            y = y + 1;
        }

        return result;
    }

    internal void ApplyResultGeometry(JpegImage image, JpegReadGeometry geometry)
    {
        image.SourceWidth = geometry.SourceWidth;
        image.SourceHeight = geometry.SourceHeight;
        image.AppliedIdctScaleRatio = (JpegIdctScaleRatio)geometry.ScaleBlockSize;
        if (geometry.ScaleBlockSize == JpegConstants.BlockWidth)
        {
            image.AppliedIdctScaleNumerator = 1;
            image.AppliedIdctScaleDenominator = 1;
        }
        else
        {
            image.AppliedIdctScaleNumerator = geometry.ScaleBlockSize;
            image.AppliedIdctScaleDenominator = JpegConstants.BlockWidth;
        }

        image.DecodedRegion = CopyRectangle(geometry.SourceRegion);
        image.OutputRectangle = CopyRectangle(geometry.OutputRectangle);
        image.FastMcuCropApplied = geometry.FastMcuCropApplied;
    }

    internal JpegRectangle CopyRectangle(JpegRectangle source)
    {
        JpegRectangle copy = new JpegRectangle();
        copy.X = source.X;
        copy.Y = source.Y;
        copy.Width = source.Width;
        copy.Height = source.Height;
        return copy;
    }
}
