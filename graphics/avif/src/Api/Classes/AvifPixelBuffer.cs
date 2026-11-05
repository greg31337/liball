using System;

namespace avif;

/// <summary>
/// Owns, wraps, or views a packed integer or linear half-float pixel buffer.
/// </summary>
public class AvifPixelBuffer : IAvifPixelBuffer, IAvifDiagnosticSource
{
    AvifPackedPixelStorage? storage;
    AvifPackedPixelFormatInfo? format;
    int width;
    int height;
    int stride;
    int offset;
    int spanLength;
    int alignment;
    long maxBytes;
    AvifResultCode code;
    bool closesStorage;
    bool ownsStorage;
    bool closed;

    /// <summary>Allocates tightly packed pixels using default resource limits.</summary>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="pixelFormat">The packed component order.</param>
    /// <param name="bitDepth">The integer component depth, or 16 for half float.</param>
    /// <param name="floatingPoint">Whether RGB/RGBA components contain linear half floats.</param>
    /// <returns>The initialized pixel buffer.</returns>
    internal static AvifPixelBuffer Create(
        int width,
        int height,
        AvifPixelFormat pixelFormat,
        int bitDepth,
        bool floatingPoint)
    {
        return new AvifPixelBuffer(
            width,
            height,
            pixelFormat,
            bitDepth,
            floatingPoint,
            1,
            new AvifSecurityLimits());
    }

    /// <summary>Allocates packed pixels with explicit stride alignment and limits.</summary>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="pixelFormat">The packed component order.</param>
    /// <param name="bitDepth">The integer component depth, or 16 for half float.</param>
    /// <param name="floatingPoint">Whether RGB/RGBA components contain linear half floats.</param>
    /// <param name="alignment">The positive row-stride alignment in bytes.</param>
    /// <param name="limits">The allocation and dimension limits.</param>
    public AvifPixelBuffer(
        int width,
        int height,
        AvifPixelFormat pixelFormat,
        int bitDepth,
        bool floatingPoint,
        int alignment,
        AvifSecurityLimits limits)
    {
        InitializeAllocated(
            width,
            height,
            pixelFormat,
            bitDepth,
            floatingPoint,
            alignment,
            limits);
    }

    /// <summary>Allocates packed pixels with explicit stride alignment and limits.</summary>
    /// <returns>The initialized pixel buffer.</returns>
    internal static AvifPixelBuffer Create(
        int width,
        int height,
        AvifPixelFormat pixelFormat,
        int bitDepth,
        bool floatingPoint,
        int alignment,
        AvifSecurityLimits limits)
    {
        return new AvifPixelBuffer(
            width,
            height,
            pixelFormat,
            bitDepth,
            floatingPoint,
            alignment,
            limits);
    }

    /// <summary>Wraps a caller-owned byte-array region without taking ownership.</summary>
    /// <param name="pixels">The caller-owned pixel bytes.</param>
    /// <param name="offset">The first accessible byte.</param>
    /// <param name="length">The accessible byte count.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="stride">The distance in bytes between adjacent rows.</param>
    /// <param name="pixelFormat">The packed component order.</param>
    /// <param name="bitDepth">The integer component depth, or 16 for half float.</param>
    /// <param name="floatingPoint">Whether RGB/RGBA components contain linear half floats.</param>
    /// <returns>The initialized caller-owned pixel buffer.</returns>
    internal static AvifPixelBuffer Wrap(
        byte[] pixels,
        int offset,
        int length,
        int width,
        int height,
        int stride,
        AvifPixelFormat pixelFormat,
        int bitDepth,
        bool floatingPoint)
    {
        AvifPixelBuffer result = new AvifPixelBuffer(
            0,
            0,
            pixelFormat,
            bitDepth,
            floatingPoint,
            1,
            new AvifSecurityLimits());
        result.InitializeCallerOwned(
            pixels,
            offset,
            length,
            width,
            height,
            stride,
            pixelFormat,
            bitDepth,
            floatingPoint);
        return result;
    }

    /// <summary>Wraps a caller-owned byte-array region without taking ownership.</summary>
    /// <returns>The initialized caller-owned pixel buffer.</returns>
    internal static AvifPixelBuffer Create(
        byte[] pixels,
        int offset,
        int length,
        int width,
        int height,
        int stride,
        AvifPixelFormat pixelFormat,
        int bitDepth,
        bool floatingPoint)
    {
        return Wrap(
            pixels,
            offset,
            length,
            width,
            height,
            stride,
            pixelFormat,
            bitDepth,
            floatingPoint);
    }

    internal static AvifPixelBuffer Create(AvifResultCode code)
    {
        return FromCode(code);
    }

    internal static AvifPixelBuffer FromCode(AvifResultCode code)
    {
        AvifPixelBuffer result = new AvifPixelBuffer(
            0,
            0,
            AvifPixelFormat.Rgba,
            8,
            false,
            1,
            new AvifSecurityLimits());
        result.InitializeError(code);
        result.closed = code == AvifResultCode.Closed;
        return result;
    }

    /// <summary>Returns the current buffer state.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        if (closed)
        {
            return AvifResultCode.Closed;
        }
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        if (storage == null || storage.Closed)
        {
            return AvifResultCode.Closed;
        }
        return storage.GetCode();
    }

    /// <summary>Returns diagnostics for packed-pixel allocation or rendering.</summary>
    /// <returns>The structured rendering diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromCode(
            GetCode(),
            AvifDiagnosticCategory.Rendering,
            "The packed-pixel rendering operation did not complete successfully.");
    }

    /// <summary>Returns the width in pixels.</summary>
    /// <returns>The visible width, or zero after closing.</returns>
    public int GetWidth()
    {
        return closed ? 0 : width;
    }

    /// <summary>Returns the height in pixels.</summary>
    /// <returns>The visible height, or zero after closing.</returns>
    public int GetHeight()
    {
        return closed ? 0 : height;
    }

    /// <summary>Returns the row stride in bytes.</summary>
    /// <returns>The distance between adjacent packed rows.</returns>
    public int GetStride()
    {
        return closed ? 0 : stride;
    }

    /// <summary>Returns the visible byte count in one row.</summary>
    /// <returns>The width multiplied by the packed pixel size.</returns>
    public int GetRowByteCount()
    {
        return closed || format == null ? 0 : width * format.BytesPerPixel;
    }

    /// <summary>Returns the byte size of one pixel.</summary>
    /// <returns>The packed pixel size.</returns>
    public int GetBytesPerPixel()
    {
        return format == null ? 0 : format.BytesPerPixel;
    }

    /// <summary>Returns the component count.</summary>
    /// <returns>One through four components, or three logical components for RGB565.</returns>
    public int GetChannelCount()
    {
        return format == null ? 0 : format.ChannelCount;
    }

    /// <summary>Returns the component bit depth.</summary>
    /// <returns>The configured integer depth, or 16 for half float.</returns>
    public int GetBitDepth()
    {
        return format == null ? 0 : format.BitDepth;
    }

    /// <summary>Returns the packed component order.</summary>
    /// <returns>The pixel format.</returns>
    public AvifPixelFormat GetPixelFormat()
    {
        return format == null ? AvifPixelFormat.Gray : format.Format;
    }

    /// <summary>Returns whether components contain linear half-float values.</summary>
    /// <returns><c>true</c> for half-float RGB/RGBA; otherwise <c>false</c>.</returns>
    public bool IsFloatingPoint()
    {
        return format != null && format.FloatingPoint;
    }

    /// <summary>Returns whether the packed layout contains alpha.</summary>
    /// <returns><c>true</c> for a two- or four-component alpha layout.</returns>
    public bool HasAlpha()
    {
        return format != null && format.HasAlpha;
    }

    /// <summary>Returns whether this object owns its pixel allocation.</summary>
    /// <returns><c>true</c> for allocated roots; <c>false</c> for caller buffers and views.</returns>
    public bool OwnsStorage()
    {
        return ownsStorage;
    }

    /// <summary>Returns one writable visible pixel row.</summary>
    /// <param name="row">The zero-based row index.</param>
    /// <returns>The visible row bytes, or an empty span when unavailable.</returns>
    public Span<byte> GetRowSpan(int row)
    {
        if (GetCode() != AvifResultCode.Success
            || row < 0 || row >= height)
        {
            return Span<byte>.Empty;
        }
        return (storage ?? throw new InvalidOperationException("Invalid internal state.")).Data.AsSpan(
            offset + row * stride,
            width * (format ?? throw new InvalidOperationException("Invalid internal state.")).BytesPerPixel);
    }

    /// <summary>Returns the accessible bytes through the end of the last visible row.</summary>
    /// <returns>A span including inter-row padding, or an empty span when unavailable.</returns>
    public Span<byte> GetSpan()
    {
        return GetCode() == AvifResultCode.Success
            ? (storage ?? throw new InvalidOperationException("Invalid internal state.")).Data.AsSpan(offset, spanLength)
            : Span<byte>.Empty;
    }

    /// <summary>Creates a non-owning rectangular packed-pixel view.</summary>
    /// <param name="x">The left pixel coordinate.</param>
    /// <param name="y">The top pixel coordinate.</param>
    /// <param name="width">The view width in pixels.</param>
    /// <param name="height">The view height in pixels.</param>
    /// <returns>A shared view, or a buffer carrying an error code.</returns>
    public AvifPixelBuffer CreateView(int x, int y, int width, int height)
    {
        AvifResultCode state = GetCode();
        if (state != AvifResultCode.Success)
        {
            return FromCode(state);
        }
        if (x < 0 || y < 0 || width <= 0 || height <= 0
            || x > this.width || y > this.height
            || width > this.width - x || height > this.height - y)
        {
            return FromCode(AvifResultCode.InvalidArgument);
        }

        AvifPixelBuffer view = FromCode(AvifResultCode.Success);
        view.storage = storage;
        view.format = format;
        view.width = width;
        view.height = height;
        view.stride = stride;
        view.offset = offset + y * stride + x * (format ?? throw new InvalidOperationException("Invalid internal state.")).BytesPerPixel;
        view.spanLength = (height - 1) * stride
            + width * format.BytesPerPixel;
        view.alignment = alignment;
        view.maxBytes = maxBytes;
        view.code = AvifResultCode.Success;
        view.closesStorage = false;
        view.ownsStorage = false;
        view.closed = false;
        return view;
    }

    /// <summary>Creates an independent copy of all visible packed pixels.</summary>
    /// <returns>An owning packed buffer copy, or a buffer carrying an error code.</returns>
    public AvifPixelBuffer CreateCopy()
    {
        AvifResultCode state = GetCode();
        if (state != AvifResultCode.Success)
        {
            return FromCode(state);
        }
        AvifPixelBuffer copy = FromCode(AvifResultCode.Success);
        copy.InitializeAllocatedCore(
            width,
            height,
            (format ?? throw new InvalidOperationException("Invalid internal state.")).Format,
            format.BitDepth,
            format.FloatingPoint,
            alignment,
            maxBytes);
        if (copy.GetCode() != AvifResultCode.Success)
        {
            return copy;
        }
        AvifResultCode result = copy.CopyFrom(this);
        if (result != AvifResultCode.Success)
        {
            copy.Close();
            return FromCode(result);
        }
        return copy;
    }

    /// <summary>Converts between straight and premultiplied alpha representations.</summary>
    /// <param name="sourcePremultiplied">Whether the current color components are premultiplied.</param>
    /// <param name="outputPremultiplied">Whether output color components should be premultiplied.</param>
    /// <returns>An owning converted copy or a buffer carrying an error code.</returns>
    public AvifPixelBuffer ConvertAlphaMode(
        bool sourcePremultiplied,
        bool outputPremultiplied)
    {
        return AlphaComposer.ConvertAlphaMode(
            this,
            sourcePremultiplied,
            outputPremultiplied);
    }

    /// <summary>Composites these foreground pixels over another packed buffer.</summary>
    /// <param name="background">The normalized straight or premultiplied background pixels.</param>
    /// <param name="foregroundPremultiplied">Whether this buffer's color is premultiplied.</param>
    /// <param name="backgroundPremultiplied">Whether background color is premultiplied.</param>
    /// <param name="outputPremultiplied">Whether output color should remain premultiplied.</param>
    /// <returns>An owning source-over result or a buffer carrying an error code.</returns>
    public AvifPixelBuffer CompositeOver(
        AvifPixelBuffer background,
        bool foregroundPremultiplied,
        bool backgroundPremultiplied,
        bool outputPremultiplied)
    {
        return AlphaComposer.CompositeOver(
            this,
            background,
            foregroundPremultiplied,
            backgroundPremultiplied,
            outputPremultiplied);
    }

    /// <summary>Copies every visible row from a layout-compatible packed buffer.</summary>
    /// <param name="source">The source packed pixels.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode CopyFrom(AvifPixelBuffer source)
    {
        AvifResultCode state = GetCode();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (source == null || source.GetCode() != AvifResultCode.Success
            || source.width != width || source.height != height
            || (source.format ?? throw new InvalidOperationException("Invalid internal state.")).Format != (format ?? throw new InvalidOperationException("Invalid internal state.")).Format
            || source.format.BitDepth != format.BitDepth
            || source.format.FloatingPoint != format.FloatingPoint)
        {
            return AvifResultCode.InvalidArgument;
        }

        int rowBytes = width * format.BytesPerPixel;
        if (source.storage == storage)
        {
            CheckedIntegerResult scratchSize =
                CheckedMath.ComputeAllocationSize(
                    (long)rowBytes * height, 1, maxBytes);
            if (!scratchSize.Success)
            {
                return scratchSize.Code;
            }
            PooledByteBuffer scratch = new PooledByteBuffer(
                (int)scratchSize.Value, maxBytes, true);
            if (scratch.Code != AvifResultCode.Success)
            {
                return scratch.Code;
            }
            int row = 0;
            while (row < height)
            {
                source.GetRowSpan(row).CopyTo(
                    scratch.Data.AsSpan(row * rowBytes, rowBytes));
                row++;
            }
            row = 0;
            while (row < height)
            {
                scratch.Data.AsSpan(row * rowBytes, rowBytes).CopyTo(
                    GetRowSpan(row));
                row++;
            }
            scratch.Close();
            return AvifResultCode.Success;
        }

        int copyRow = 0;
        while (copyRow < height)
        {
            source.GetRowSpan(copyRow).CopyTo(GetRowSpan(copyRow));
            copyRow++;
        }
        return AvifResultCode.Success;
    }

    /// <summary>Closes this buffer and releases owned storage.</summary>
    /// <returns><c>Success</c>; repeated calls are harmless.</returns>
    public AvifResultCode Close()
    {
        if (closed)
        {
            return AvifResultCode.Success;
        }
        closed = true;
        if (closesStorage && storage != null)
        {
            storage.Close();
        }
        storage = null;
        width = 0;
        height = 0;
        stride = 0;
        offset = 0;
        spanLength = 0;
        closesStorage = false;
        ownsStorage = false;
        return AvifResultCode.Success;
    }

    void InitializeAllocated(
        int requestedWidth,
        int requestedHeight,
        AvifPixelFormat requestedFormat,
        int requestedBitDepth,
        bool requestedFloatingPoint,
        int requestedAlignment,
        AvifSecurityLimits limits)
    {
        ResourceGuard guard = new ResourceGuard(limits);
        if (guard.Code != AvifResultCode.Success)
        {
            InitializeError(guard.Code);
            return;
        }
        AvifResultCode dimensions = guard.CheckImageDimensions(
            requestedWidth, requestedHeight);
        if (dimensions != AvifResultCode.Success)
        {
            InitializeError(dimensions);
            return;
        }
        InitializeAllocatedCore(
            requestedWidth,
            requestedHeight,
            requestedFormat,
            requestedBitDepth,
            requestedFloatingPoint,
            requestedAlignment,
            guard.Limits.MaxDecodedBytes);
    }

    void InitializeAllocatedCore(
        int requestedWidth,
        int requestedHeight,
        AvifPixelFormat requestedFormat,
        int requestedBitDepth,
        bool requestedFloatingPoint,
        int requestedAlignment,
        long requestedMaxBytes)
    {
        format = CreateFormatInfo(
            requestedFormat,
            requestedBitDepth,
            requestedFloatingPoint);
        if (format == null || requestedAlignment <= 0)
        {
            InitializeError(AvifResultCode.InvalidArgument);
            return;
        }
        CheckedIntegerResult computedStride = CheckedMath.ComputeStride(
            requestedWidth,
            1,
            format.BytesPerPixel,
            requestedAlignment,
            requestedMaxBytes);
        if (!computedStride.Success)
        {
            InitializeError(computedStride.Code);
            return;
        }
        CheckedIntegerResult size = CheckedMath.ComputePlaneSize(
            (int)computedStride.Value,
            requestedHeight,
            requestedMaxBytes);
        if (!size.Success || size.Value > System.Array.MaxLength)
        {
            InitializeError(
                size.Success
                    ? AvifResultCode.LimitExceeded
                    : size.Code);
            return;
        }

        storage = AvifPackedPixelStorage.Allocate(
            (int)size.Value, requestedMaxBytes);
        code = (storage.Pooled ?? throw new InvalidOperationException("Invalid internal state.")).Code;
        width = code == AvifResultCode.Success ? requestedWidth : 0;
        height = code == AvifResultCode.Success ? requestedHeight : 0;
        stride = code == AvifResultCode.Success
            ? (int)computedStride.Value
            : 0;
        offset = 0;
        spanLength = code == AvifResultCode.Success
            ? (requestedHeight - 1) * stride
                + requestedWidth * format.BytesPerPixel
            : 0;
        alignment = requestedAlignment;
        maxBytes = requestedMaxBytes;
        closesStorage = true;
        ownsStorage = code == AvifResultCode.Success;
        closed = false;
    }

    void InitializeCallerOwned(
        byte[] pixels,
        int requestedOffset,
        int requestedLength,
        int requestedWidth,
        int requestedHeight,
        int requestedStride,
        AvifPixelFormat requestedFormat,
        int requestedBitDepth,
        bool requestedFloatingPoint)
    {
        format = CreateFormatInfo(
            requestedFormat,
            requestedBitDepth,
            requestedFloatingPoint);
        if (pixels == null || format == null
            || requestedOffset < 0 || requestedLength < 0
            || requestedWidth <= 0 || requestedHeight <= 0
            || requestedStride <= 0
            || requestedOffset > pixels.Length
            || requestedLength > pixels.Length - requestedOffset)
        {
            InitializeError(AvifResultCode.InvalidArgument);
            return;
        }
        CheckedIntegerResult rowBytes = CheckedMath.MultiplyInt64(
            requestedWidth, format.BytesPerPixel);
        CheckedIntegerResult precedingRows = CheckedMath.MultiplyInt64(
            requestedHeight - 1, requestedStride);
        if (!rowBytes.Success || !precedingRows.Success
            || rowBytes.Value > int.MaxValue
            || requestedStride < rowBytes.Value)
        {
            InitializeError(
                rowBytes.Success && precedingRows.Success
                    && rowBytes.Value <= int.MaxValue
                    ? AvifResultCode.InvalidArgument
                    : AvifResultCode.LimitExceeded);
            return;
        }
        CheckedIntegerResult required = CheckedMath.AddInt64(
            precedingRows.Value, rowBytes.Value);
        if (!required.Success || required.Value > requestedLength
            || required.Value > int.MaxValue)
        {
            InitializeError(
                required.Success
                    ? AvifResultCode.BufferTooSmall
                    : AvifResultCode.LimitExceeded);
            return;
        }

        storage = new AvifPackedPixelStorage(pixels);
        width = requestedWidth;
        height = requestedHeight;
        stride = requestedStride;
        offset = requestedOffset;
        spanLength = (int)required.Value;
        alignment = 1;
        maxBytes = requestedLength;
        code = AvifResultCode.Success;
        closesStorage = true;
        ownsStorage = false;
        closed = false;
    }

    void InitializeError(AvifResultCode error)
    {
        storage = null;
        format = null;
        width = 0;
        height = 0;
        stride = 0;
        offset = 0;
        spanLength = 0;
        alignment = 1;
        maxBytes = 0;
        code = error;
        closesStorage = false;
        ownsStorage = false;
        closed = error == AvifResultCode.Closed;
    }

    static AvifPackedPixelFormatInfo? CreateFormatInfo(
        AvifPixelFormat pixelFormat,
        int bitDepth,
        bool floatingPoint)
    {
        int channels = GetChannelCount(pixelFormat);
        if (channels == 0)
        {
            return null;
        }
        if (pixelFormat == AvifPixelFormat.Rgb565)
        {
            if (bitDepth != 8 || floatingPoint)
            {
                return null;
            }
        }
        else if (floatingPoint)
        {
            if (bitDepth != 16
                || pixelFormat != AvifPixelFormat.Rgb
                    && pixelFormat != AvifPixelFormat.Rgba)
            {
                return null;
            }
        }
        else if (bitDepth != 8 && bitDepth != 10
            && bitDepth != 12 && bitDepth != 16)
        {
            return null;
        }

        AvifPackedPixelFormatInfo info =
            new AvifPackedPixelFormatInfo();
        info.Format = pixelFormat;
        info.BitDepth = bitDepth;
        info.FloatingPoint = floatingPoint;
        info.ChannelCount = channels;
        info.BytesPerComponent = bitDepth > 8 ? 2 : 1;
        info.BytesPerPixel = pixelFormat == AvifPixelFormat.Rgb565
            ? 2
            : channels * info.BytesPerComponent;
        info.HasAlpha = pixelFormat == AvifPixelFormat.GrayAlpha
            || pixelFormat == AvifPixelFormat.Rgba
            || pixelFormat == AvifPixelFormat.Bgra
            || pixelFormat == AvifPixelFormat.Argb
            || pixelFormat == AvifPixelFormat.Abgr;
        return info;
    }

    static int GetChannelCount(AvifPixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
            case AvifPixelFormat.Gray:
                return 1;
            case AvifPixelFormat.GrayAlpha:
                return 2;
            case AvifPixelFormat.Rgb:
            case AvifPixelFormat.Bgr:
            case AvifPixelFormat.Rgb565:
                return 3;
            case AvifPixelFormat.Rgba:
            case AvifPixelFormat.Bgra:
            case AvifPixelFormat.Argb:
            case AvifPixelFormat.Abgr:
                return 4;
            default:
                return 0;
        }
    }
}
