using System;

namespace avif;

/// <summary>
/// Owns or views a planar monochrome or YUV image with optional full-resolution alpha.
/// </summary>
public class AvifImage : IAvifImage, IAvifDiagnosticSource
{
    AvifPlane?[]? planes;
    int width;
    int height;
    int bitDepth;
    int alignment;
    AvifChromaFormat chromaFormat;
    AvifChromaSamplePosition chromaSamplePosition;
    AvifRange range;
    AvifColorInfo? colorInfo;
    AvifIccProfile? iccProfile;
    AvifAlphaMode alphaMode;
    AvifSecurityLimits? limits;
    AvifResultCode code;
    bool closed;

    /// <summary>Allocates a planar image using default limits and tightly packed rows.</summary>
    /// <param name="width">The luma width in pixels.</param>
    /// <param name="height">The luma height in pixels.</param>
    /// <param name="bitDepth">The integer sample depth: 8, 10, 12, or 16.</param>
    /// <param name="chromaFormat">The monochrome or YUV chroma layout.</param>
    /// <param name="hasAlpha">Whether to allocate a full-resolution straight-alpha plane.</param>
    /// <returns>The initialized image.</returns>
    internal static AvifImage Create(
        int width,
        int height,
        int bitDepth,
        AvifChromaFormat chromaFormat,
        bool hasAlpha)
    {
        return new AvifImage(
            width,
            height,
            bitDepth,
            chromaFormat,
            AvifRange.Full,
            AvifChromaSamplePosition.Unknown,
            hasAlpha ? AvifAlphaMode.Straight : AvifAlphaMode.None,
            1,
            new AvifSecurityLimits());
    }

    /// <summary>Allocates a planar image with explicit metadata, alignment, and limits.</summary>
    /// <param name="width">The luma width in pixels.</param>
    /// <param name="height">The luma height in pixels.</param>
    /// <param name="bitDepth">The integer sample depth: 8, 10, 12, or 16.</param>
    /// <param name="chromaFormat">The monochrome or YUV chroma layout.</param>
    /// <param name="range">The encoded integer sample range.</param>
    /// <param name="chromaSamplePosition">The chroma position relative to luma.</param>
    /// <param name="alphaMode">The alpha representation, or <c>None</c>.</param>
    /// <param name="alignment">The positive row-stride alignment in bytes.</param>
    /// <param name="limits">The allocation and dimension limits.</param>
    public AvifImage(
        int width,
        int height,
        int bitDepth,
        AvifChromaFormat chromaFormat,
        AvifRange range,
        AvifChromaSamplePosition chromaSamplePosition,
        AvifAlphaMode alphaMode,
        int alignment,
        AvifSecurityLimits? limits)
    {
        Initialize(
            width,
            height,
            bitDepth,
            chromaFormat,
            range,
            chromaSamplePosition,
            alphaMode,
            alignment,
            limits);
    }

    /// <summary>Allocates a planar image with explicit metadata, alignment, and limits.</summary>
    /// <returns>The initialized image.</returns>
    internal static AvifImage Create(
        int width,
        int height,
        int bitDepth,
        AvifChromaFormat chromaFormat,
        AvifRange range,
        AvifChromaSamplePosition chromaSamplePosition,
        AvifAlphaMode alphaMode,
        int alignment,
        AvifSecurityLimits? limits)
    {
        return new AvifImage(
            width,
            height,
            bitDepth,
            chromaFormat,
            range,
            chromaSamplePosition,
            alphaMode,
            alignment,
            limits);
    }

    internal static AvifImage Create(AvifResultCode code)
    {
        return FromCode(code);
    }

    internal static AvifImage FromCode(AvifResultCode code)
    {
        AvifImage result = new AvifImage(
            0,
            0,
            8,
            AvifChromaFormat.Monochrome,
            AvifRange.Full,
            AvifChromaSamplePosition.Unknown,
            AvifAlphaMode.None,
            1,
            new AvifSecurityLimits());
        result.planes = new AvifPlane[4];
        result.width = 0;
        result.height = 0;
        result.bitDepth = 0;
        result.colorInfo = CreateDefaultColorInfo(AvifRange.Full);
        result.code = code;
        result.closed = code == AvifResultCode.Closed;
        return result;
    }

    /// <summary>Returns the current image state.</summary>
    /// <returns>The stable result code, including closure of shared storage.</returns>
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
        int index = 0;
        while (index < (planes ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            if (planes[index] != null
                && (planes[index] ?? throw new InvalidOperationException("Invalid internal state.")).GetCode() != AvifResultCode.Success)
            {
                return (planes[index] ?? throw new InvalidOperationException("Invalid internal state.")).GetCode();
            }
            index++;
        }
        return AvifResultCode.Success;
    }

    /// <summary>Returns diagnostics for image allocation, decoding, or rendering state.</summary>
    /// <returns>The structured image diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromCode(
            GetCode(),
            AvifDiagnosticCategory.Rendering,
            "The image allocation, decode, or rendering operation did not complete successfully.");
    }

    /// <summary>Returns the luma width in pixels.</summary>
    /// <returns>The image width, or zero after closing.</returns>
    public int GetWidth()
    {
        return closed ? 0 : width;
    }

    /// <summary>Returns the luma height in pixels.</summary>
    /// <returns>The image height, or zero after closing.</returns>
    public int GetHeight()
    {
        return closed ? 0 : height;
    }

    /// <summary>Returns the integer sample bit depth.</summary>
    /// <returns>The image bit depth.</returns>
    public int GetBitDepth()
    {
        return bitDepth;
    }

    /// <summary>Returns the chroma layout.</summary>
    /// <returns>The monochrome, 4:2:0, 4:2:2, or 4:4:4 layout.</returns>
    public AvifChromaFormat GetChromaFormat()
    {
        return chromaFormat;
    }

    /// <summary>Returns the chroma sample position.</summary>
    /// <returns>The stored chroma position.</returns>
    public AvifChromaSamplePosition GetChromaSamplePosition()
    {
        return chromaSamplePosition;
    }

    /// <summary>Returns the integer sample range.</summary>
    /// <returns>The full or limited range.</returns>
    public AvifRange GetRange()
    {
        return range;
    }

    /// <summary>Returns an independent copy of the image's H.273 color signaling.</summary>
    /// <returns>The color information, or null after the image is closed.</returns>
    public AvifColorInfo? GetColorInfo()
    {
        return closed ? null : CopyColorInfo(colorInfo);
    }

    /// <summary>Replaces the image's H.273 color signaling and sample-range interpretation.</summary>
    /// <param name="value">The color information to copy.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode SetColorInfo(AvifColorInfo? value)
    {
        AvifResultCode state = GetCode();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (value == null
            || !IsValidRange(value.Range)
            || !IsByteCodePoint((int)value.ColorPrimaries)
            || !IsByteCodePoint((int)value.TransferCharacteristics)
            || !IsByteCodePoint((int)value.MatrixCoefficients))
        {
            return AvifResultCode.InvalidArgument;
        }
        colorInfo = CopyColorInfo(value);
        range = value.Range;
        return AvifResultCode.Success;
    }

    /// <summary>Returns the validated embedded ICC profile.</summary>
    /// <returns>The immutable profile, or null when no profile is attached.</returns>
    public AvifIccProfile? GetIccProfile()
    {
        return closed ? null : iccProfile;
    }

    /// <summary>Attaches or clears a validated embedded ICC profile.</summary>
    /// <param name="value">The profile to attach, or null to clear it.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode SetIccProfile(AvifIccProfile? value)
    {
        AvifResultCode state = GetCode();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (value == null)
        {
            iccProfile = null;
            return AvifResultCode.Success;
        }
        if (value.GetCode() != AvifResultCode.Success)
        {
            return value.GetCode();
        }
        AvifIccProfile copy = AvifIccProfile.Create(value.GetData(), limits);
        if (copy.GetCode() != AvifResultCode.Success)
        {
            return copy.GetCode();
        }
        iccProfile = copy;
        return AvifResultCode.Success;
    }

    /// <summary>Returns the alpha representation.</summary>
    /// <returns>The none, straight, or premultiplied alpha mode.</returns>
    public AvifAlphaMode GetAlphaMode()
    {
        return alphaMode;
    }

    /// <summary>Attaches a decoded monochrome auxiliary image as full-resolution alpha.</summary>
    /// <param name="auxiliary">The monochrome alpha image.</param>
    /// <param name="premultiplied">Whether this image's color samples are premultiplied.</param>
    /// <param name="filter">The filter used when auxiliary dimensions differ.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode AttachAlpha(
        AvifImage auxiliary,
        bool premultiplied,
        AvifResamplingFilter filter)
    {
        return AlphaComposer.AttachAlpha(this, auxiliary, premultiplied, filter);
    }

    /// <summary>Returns the number of allocated or viewed planes.</summary>
    /// <returns>One through four planes according to chroma and alpha.</returns>
    public int GetPlaneCount()
    {
        int count = 0;
        int index = 0;
        while (index < (planes ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            if (planes[index] != null)
            {
                count++;
            }
            index++;
        }
        return count;
    }

    /// <summary>Returns one image plane.</summary>
    /// <param name="kind">The requested luma, chroma, or alpha kind.</param>
    /// <returns>The plane, or null when that kind is absent.</returns>
    public AvifPlane? GetPlane(AvifPlaneKind kind)
    {
        int index = (int)kind;
        if (closed || index < 0 || index >= (planes ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            return null;
        }
        return planes[index];
    }

    /// <summary>Returns the combined allocated row storage in bytes.</summary>
    /// <returns>The sum of stride multiplied by height for every plane.</returns>
    public long GetStorageByteCount()
    {
        if (closed)
        {
            return 0;
        }
        long total = 0;
        int index = 0;
        while (index < (planes ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            AvifPlane? plane = planes[index];
            if (plane != null)
            {
                total += (long)plane.GetStride() * plane.GetHeight();
            }
            index++;
        }
        return total;
    }

    /// <summary>Creates a non-owning image view sharing this image's plane storage.</summary>
    /// <param name="x">The left luma coordinate.</param>
    /// <param name="y">The top luma coordinate.</param>
    /// <param name="width">The view width in luma pixels.</param>
    /// <param name="height">The view height in luma pixels.</param>
    /// <returns>A shared image view, or an image carrying an error code.</returns>
    public AvifImage CreateView(int x, int y, int width, int height)
    {
        AvifResultCode state = GetCode();
        if (state != AvifResultCode.Success)
        {
            return AvifImage.FromCode(state);
        }
        if (x < 0 || y < 0 || width <= 0 || height <= 0
            || x > this.width || y > this.height
            || width > this.width - x || height > this.height - y)
        {
            return AvifImage.FromCode(AvifResultCode.InvalidArgument);
        }
        bool subsamplingX = HasSubsamplingX(chromaFormat);
        bool subsamplingY = HasSubsamplingY(chromaFormat);
        if (subsamplingX && (x & 1) != 0
            || subsamplingY && (y & 1) != 0)
        {
            return AvifImage.FromCode(AvifResultCode.InvalidArgument);
        }

        AvifImage view = AvifImage.FromCode(AvifResultCode.Success);
        view.width = width;
        view.height = height;
        view.bitDepth = bitDepth;
        view.alignment = alignment;
        view.chromaFormat = chromaFormat;
        view.chromaSamplePosition = chromaSamplePosition;
        view.range = range;
        view.colorInfo = CopyColorInfo(colorInfo);
        view.iccProfile = iccProfile;
        view.alphaMode = alphaMode;
        view.limits = ResourceGuard.CopyLimits(limits);
        (view.planes ?? throw new InvalidOperationException("Invalid internal state."))[(int)AvifPlaneKind.Y] = ((planes ?? throw new InvalidOperationException("Invalid internal state."))[
            (int)AvifPlaneKind.Y] ?? throw new InvalidOperationException("Invalid internal state.")).CreateView(x, y, width, height);
        if (chromaFormat != AvifChromaFormat.Monochrome)
        {
            int chromaX = subsamplingX ? x >> 1 : x;
            int chromaY = subsamplingY ? y >> 1 : y;
            int chromaWidth = subsamplingX
                ? (width >> 1) + (width & 1)
                : width;
            int chromaHeight = subsamplingY
                ? (height >> 1) + (height & 1)
                : height;
            view.planes[(int)AvifPlaneKind.U] = (planes[
                (int)AvifPlaneKind.U] ?? throw new InvalidOperationException("Invalid internal state.")).CreateView(
                    chromaX, chromaY, chromaWidth, chromaHeight);
            view.planes[(int)AvifPlaneKind.V] = (planes[
                (int)AvifPlaneKind.V] ?? throw new InvalidOperationException("Invalid internal state.")).CreateView(
                    chromaX, chromaY, chromaWidth, chromaHeight);
        }
        if (alphaMode != AvifAlphaMode.None)
        {
            view.planes[(int)AvifPlaneKind.Alpha] = (planes[
                (int)AvifPlaneKind.Alpha] ?? throw new InvalidOperationException("Invalid internal state.")).CreateView(
                    x, y, width, height);
        }
        view.code = view.GetCode();
        return view;
    }

    /// <summary>Creates an independent copy of this image and its visible samples.</summary>
    /// <returns>An owning image copy, or an image carrying an error code.</returns>
    public AvifImage CreateCopy()
    {
        AvifResultCode state = GetCode();
        if (state != AvifResultCode.Success)
        {
            return AvifImage.FromCode(state);
        }
        AvifImage copy = new AvifImage(
            width,
            height,
            bitDepth,
            chromaFormat,
            range,
            chromaSamplePosition,
            alphaMode,
            alignment,
            limits);
        if (copy.GetCode() != AvifResultCode.Success)
        {
            return copy;
        }
        AvifResultCode result = copy.CopyFrom(this);
        if (result != AvifResultCode.Success)
        {
            copy.Close();
            return AvifImage.FromCode(result);
        }
        copy.colorInfo = CopyColorInfo(colorInfo);
        copy.iccProfile = iccProfile;
        return copy;
    }

    /// <summary>Copies all visible planes from a layout-compatible image.</summary>
    /// <param name="source">The source image.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode CopyFrom(AvifImage source)
    {
        AvifResultCode state = GetCode();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (source == null || source.GetCode() != AvifResultCode.Success
            || source.width != width || source.height != height
            || source.bitDepth != bitDepth
            || source.chromaFormat != chromaFormat
            || source.alphaMode != alphaMode)
        {
            return AvifResultCode.InvalidArgument;
        }
        int index = 0;
        while (index < (planes ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            if (planes[index] != null)
            {
                AvifResultCode result = (planes[index] ?? throw new InvalidOperationException("Invalid internal state.")).CopyFrom(
                    (source.planes ?? throw new InvalidOperationException("Invalid internal state."))[index]);
                if (result != AvifResultCode.Success)
                {
                    return result;
                }
            }
            index++;
        }
        return AvifResultCode.Success;
    }

    /// <summary>Closes every plane and releases owned allocations.</summary>
    /// <returns><c>Success</c>; repeated calls are harmless.</returns>
    public AvifResultCode Close()
    {
        if (closed)
        {
            return AvifResultCode.Success;
        }
        int index = 0;
        while (index < (planes ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            if (planes[index] != null)
            {
                (planes[index] ?? throw new InvalidOperationException("Invalid internal state.")).Close();
                planes[index] = null;
            }
            index++;
        }
        width = 0;
        height = 0;
        iccProfile = null;
        closed = true;
        return AvifResultCode.Success;
    }

    void Initialize(
        int requestedWidth,
        int requestedHeight,
        int requestedBitDepth,
        AvifChromaFormat requestedChromaFormat,
        AvifRange requestedRange,
        AvifChromaSamplePosition requestedChromaSamplePosition,
        AvifAlphaMode requestedAlphaMode,
        int requestedAlignment,
        AvifSecurityLimits? requestedLimits)
    {
        planes = new AvifPlane[4];
        width = requestedWidth;
        height = requestedHeight;
        bitDepth = requestedBitDepth;
        alignment = requestedAlignment;
        chromaFormat = requestedChromaFormat;
        chromaSamplePosition = requestedChromaSamplePosition;
        range = requestedRange;
        colorInfo = CreateDefaultColorInfo(requestedRange);
        iccProfile = null;
        alphaMode = requestedAlphaMode;
        limits = ResourceGuard.CopyLimits(requestedLimits);
        code = AvifResultCode.Success;
        closed = false;

        ResourceGuard guard = new ResourceGuard(requestedLimits);
        if (guard.Code != AvifResultCode.Success)
        {
            code = guard.Code;
            return;
        }
        code = guard.CheckImageDimensions(requestedWidth, requestedHeight);
        if (code != AvifResultCode.Success)
        {
            return;
        }
        if (!IsValidBitDepth(requestedBitDepth)
            || !IsValidChromaFormat(requestedChromaFormat)
            || !IsValidRange(requestedRange)
            || !IsValidChromaPosition(requestedChromaSamplePosition)
            || !IsValidAlphaMode(requestedAlphaMode)
            || requestedAlignment <= 0)
        {
            code = AvifResultCode.InvalidArgument;
            return;
        }

        int bytesPerSample = requestedBitDepth > 8 ? 2 : 1;
        bool subsamplingX = HasSubsamplingX(requestedChromaFormat);
        bool subsamplingY = HasSubsamplingY(requestedChromaFormat);
        int chromaWidth = subsamplingX
            ? (requestedWidth >> 1) + (requestedWidth & 1)
            : requestedWidth;
        int chromaHeight = subsamplingY
            ? (requestedHeight >> 1) + (requestedHeight & 1)
            : requestedHeight;
        CheckedIntegerResult storage = AddPlaneStorage(
            0,
            requestedWidth,
            requestedHeight,
            bytesPerSample,
            requestedAlignment,
            limits.MaxDecodedBytes);
        if (storage.Success
            && requestedChromaFormat != AvifChromaFormat.Monochrome)
        {
            storage = AddPlaneStorage(
                storage.Value,
                chromaWidth,
                chromaHeight,
                bytesPerSample,
                requestedAlignment,
                limits.MaxDecodedBytes);
            if (storage.Success)
            {
                storage = AddPlaneStorage(
                    storage.Value,
                    chromaWidth,
                    chromaHeight,
                    bytesPerSample,
                    requestedAlignment,
                    limits.MaxDecodedBytes);
            }
        }
        if (storage.Success && requestedAlphaMode != AvifAlphaMode.None)
        {
            storage = AddPlaneStorage(
                storage.Value,
                requestedWidth,
                requestedHeight,
                bytesPerSample,
                requestedAlignment,
                limits.MaxDecodedBytes);
        }
        if (!storage.Success)
        {
            code = storage.Code;
            return;
        }

        planes[(int)AvifPlaneKind.Y] = AvifPlane.Create(
            requestedWidth,
            requestedHeight,
            requestedBitDepth,
            AvifPlaneKind.Y,
            requestedAlignment,
            limits.MaxDecodedBytes);
        if (requestedChromaFormat != AvifChromaFormat.Monochrome)
        {
            planes[(int)AvifPlaneKind.U] = AvifPlane.Create(
                chromaWidth,
                chromaHeight,
                requestedBitDepth,
                AvifPlaneKind.U,
                requestedAlignment,
                limits.MaxDecodedBytes);
            planes[(int)AvifPlaneKind.V] = AvifPlane.Create(
                chromaWidth,
                chromaHeight,
                requestedBitDepth,
                AvifPlaneKind.V,
                requestedAlignment,
                limits.MaxDecodedBytes);
        }
        if (requestedAlphaMode != AvifAlphaMode.None)
        {
            planes[(int)AvifPlaneKind.Alpha] = AvifPlane.Create(
                requestedWidth,
                requestedHeight,
                requestedBitDepth,
                AvifPlaneKind.Alpha,
                requestedAlignment,
                limits.MaxDecodedBytes);
        }
        code = GetCode();
        if (code != AvifResultCode.Success)
        {
            CloseAllocatedPlanes();
        }
    }

    void CloseAllocatedPlanes()
    {
        int index = 0;
        while (index < (planes ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            if (planes[index] != null)
            {
                (planes[index] ?? throw new InvalidOperationException("Invalid internal state.")).Close();
                planes[index] = null;
            }
            index++;
        }
    }

    static CheckedIntegerResult AddPlaneStorage(
        long total,
        int planeWidth,
        int planeHeight,
        int bytesPerSample,
        int alignment,
        long maxBytes)
    {
        CheckedIntegerResult stride = CheckedMath.ComputeStride(
            planeWidth, 1, bytesPerSample, alignment, maxBytes);
        if (!stride.Success)
        {
            return stride;
        }
        CheckedIntegerResult size = CheckedMath.ComputePlaneSize(
            (int)stride.Value, planeHeight, maxBytes);
        if (!size.Success)
        {
            return size;
        }
        CheckedIntegerResult combined = CheckedMath.AddInt64(
            total, size.Value);
        if (!combined.Success)
        {
            return combined;
        }
        return combined.Value > maxBytes
            ? new CheckedIntegerResult(
                false, 0, AvifResultCode.LimitExceeded)
            : combined;
    }

    static bool HasSubsamplingX(AvifChromaFormat value)
    {
        return value == AvifChromaFormat.Yuv420
            || value == AvifChromaFormat.Yuv422;
    }

    static bool HasSubsamplingY(AvifChromaFormat value)
    {
        return value == AvifChromaFormat.Yuv420;
    }

    static bool IsValidBitDepth(int value)
    {
        return value == 8 || value == 10 || value == 12 || value == 16;
    }

    static bool IsValidChromaFormat(AvifChromaFormat value)
    {
        int numeric = (int)value;
        return numeric >= (int)AvifChromaFormat.Monochrome
            && numeric <= (int)AvifChromaFormat.Yuv444;
    }

    static bool IsValidRange(AvifRange value)
    {
        return value == AvifRange.Limited || value == AvifRange.Full;
    }

    static bool IsByteCodePoint(int value)
    {
        return value >= 0 && value <= byte.MaxValue;
    }

    static AvifColorInfo CreateDefaultColorInfo(AvifRange range)
    {
        AvifColorInfo value = AvifColorInfo.Create();
        value.Range = range;
        return value;
    }

    static AvifColorInfo CopyColorInfo(AvifColorInfo? value)
    {
        return AvifColorInfo.Create(
            (value ?? throw new InvalidOperationException("Invalid internal state.")).ColorPrimaries,
            value.TransferCharacteristics,
            value.MatrixCoefficients,
            value.Range);
    }

    static bool IsValidChromaPosition(AvifChromaSamplePosition value)
    {
        int numeric = (int)value;
        return numeric >= (int)AvifChromaSamplePosition.Unknown
            && numeric <= (int)AvifChromaSamplePosition.Colocated;
    }

    static bool IsValidAlphaMode(AvifAlphaMode value)
    {
        int numeric = (int)value;
        return numeric >= (int)AvifAlphaMode.None
            && numeric <= (int)AvifAlphaMode.Premultiplied;
    }

    internal int GetAlignmentValue()
    {
        return alignment;
    }

    internal AvifSecurityLimits? GetLimitsValue()
    {
        return limits;
    }

    internal void ReplaceAlphaPlane(
        AvifPlane alpha,
        AvifAlphaMode mode)
    {
        if ((planes ?? throw new InvalidOperationException("Invalid internal state."))[(int)AvifPlaneKind.Alpha] != null)
        {
            (planes[(int)AvifPlaneKind.Alpha] ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        }
        planes[(int)AvifPlaneKind.Alpha] = alpha;
        alphaMode = mode;
    }
}
