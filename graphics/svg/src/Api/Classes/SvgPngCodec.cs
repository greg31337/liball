namespace svg;

/// <summary>Probes, decodes, and deterministically encodes bounded portable PNG pixel data.</summary>
public class SvgPngCodec : ISvgPngCodec
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgPngCodec()
    {
    }

    /// <summary>Probes a PNG header using secure raster defaults.</summary>
    /// <param name="bytes">The PNG byte source.</param>
    /// <returns>The intrinsic dimensions and header facts, or a stable failure.</returns>
    public SvgPngProbeResult ProbeWithBytes(byte[] bytes)
    {
        return new SvgPngCodec().ProbeWithBytesAndLimits(bytes, new SvgRasterLimits());
    }

    /// <summary>Probes a PNG header under explicit dimension and pixel limits.</summary>
    /// <param name="bytes">The PNG byte source.</param>
    /// <param name="limits">The raster limits applied before accepting dimensions.</param>
    /// <returns>The intrinsic dimensions and header facts, or a stable failure.</returns>
    public SvgPngProbeResult ProbeWithBytesAndLimits(byte[]? bytes, SvgRasterLimits limits)
    {
        SvgPngProbeResult result = new SvgPngProbeResult();
        if (bytes == null || !new SvgPngCodec().ValidLimits(limits)) return result;
        if (bytes.Length < 33)
        {
            result.Error = SvgPngProbeError.TruncatedHeader;
            return result;
        }
        if (!new SvgPngCodec().HasSignature(bytes))
        {
            result.Error = SvgPngProbeError.InvalidSignature;
            return result;
        }
        if (new SvgPngCodec().ReadUInt32(bytes, 8) != 13 || bytes[12] != 73 || bytes[13] != 72 || bytes[14] != 68 || bytes[15] != 82)
        {
            result.Error = SvgPngProbeError.InvalidHeader;
            return result;
        }
        uint width = new SvgPngCodec().ReadUInt32(bytes, 16);
        uint height = new SvgPngCodec().ReadUInt32(bytes, 20);
        int bitDepth = bytes[24];
        int colorType = bytes[25];
        if (width == 0 || height == 0 || width > 2147483647 || height > 2147483647 || !new SvgPngCodec().ValidFormat(bitDepth, colorType) || bytes[26] != 0 || bytes[27] != 0 || bytes[28] > 1 || !new SvgPngCodec().ValidHeaderCrc(bytes))
        {
            result.Error = SvgPngProbeError.InvalidHeader;
            return result;
        }
        long pixels = (long)width * height;
        if (width > limits.MaxWidth || height > limits.MaxHeight || pixels > limits.MaxSurfacePixels || pixels > limits.MaxPixelBytes / 4)
        {
            result.Error = SvgPngProbeError.DimensionLimitExceeded;
            return result;
        }
        result.Success = true;
        result.Error = SvgPngProbeError.None;
        result.Width = (int)width;
        result.Height = (int)height;
        result.BitDepth = bitDepth;
        result.ColorType = colorType;
        result.InterlaceMethod = bytes[28];
        result.HasAlpha = colorType == 4 || colorType == 6;
        result.IntrinsicSize = SvgIntrinsicSize.Create(result.Width, result.Height);
        return result;
    }

    /// <summary>Decodes one complete PNG using secure raster defaults.</summary>
    /// <param name="bytes">The complete PNG byte source.</param>
    /// <returns>The premultiplied sRGB pixels or a stable failure.</returns>
    public SvgPngDecodeResult DecodeWithBytes(byte[] bytes)
    {
        return new SvgPngCodec().DecodeWithBytesAndLimits(bytes, new SvgRasterLimits());
    }

    /// <summary>Decodes one complete PNG under explicit chunk, dimension, byte, and inflate limits.</summary>
    /// <param name="bytes">The complete PNG byte source.</param>
    /// <param name="limits">The raster and PNG security limits.</param>
    /// <returns>The premultiplied sRGB pixels or a stable failure.</returns>
    public SvgPngDecodeResult DecodeWithBytesAndLimits(byte[]? bytes, SvgRasterLimits limits)
    {
        SvgPngDecodeResult result = new SvgPngDecodeResult();
        if (bytes == null || !new SvgPngCodec().ValidDecodeLimits(limits)) return result;
        SvgPngProbeResult probe = new SvgPngCodec().ProbeWithBytesAndLimits(bytes, limits);
        if (!probe.Success)
        {
            result.Error = new SvgPngCodec().ProbeDecodeError(probe.Error);
            return result;
        }
        SvgPngDecodeState state = new SvgPngDecodeState(bytes, limits, result);
        state.Width = probe.Width;
        state.Height = probe.Height;
        state.BitDepth = probe.BitDepth;
        state.ColorType = probe.ColorType;
        state.InterlaceMethod = probe.InterlaceMethod;
        if (!new SvgPngCodec().ReadChunks(state)) return result;
        long expected = new SvgPngCodec().ExpectedInflatedBytes(state);
        if (expected < 0 || expected > limits.MaxPngDecodedBytes || expected > System.Array.MaxLength)
        {
            result.Error = SvgPngDecodeError.DecodedSizeLimitExceeded;
            return result;
        }
        SvgPngInflateResult inflated = new SvgPngCodec().Inflate(state.Compressed, (int)expected);
        if (!inflated.Success)
        {
            result.Error = SvgPngDecodeError.DecompressionFailed;
            return result;
        }
        SvgPixelBufferResult created = SvgPixelBuffer.Create(state.Width, state.Height, SvgPixelColorSpace.Srgb, limits);
        if (!created.Success)
        {
            result.Error = SvgPngDecodeError.DimensionLimitExceeded;
            return result;
        }
        state.Pixels = created.Buffer;
        state.Inflated = inflated.Bytes;
        if (!new SvgPngCodec().DecodeScanlines(state)) return result;
        result.Success = true;
        result.Error = SvgPngDecodeError.None;
        result.Pixels = state.Pixels;
        result.Width = state.Width;
        result.Height = state.Height;
        result.BitDepth = state.BitDepth;
        result.ColorType = state.ColorType;
        result.InterlaceMethod = state.InterlaceMethod;
        result.BytesInflated = (state.Inflated ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length;
        return result;
    }

    /// <summary>Encodes premultiplied pixels as a deterministic RGBA8 PNG using secure defaults.</summary>
    /// <param name="pixels">The positive-size premultiplied pixel buffer.</param>
    /// <returns>The complete PNG bytes or a stable failure.</returns>
    public SvgPngEncodeResult EncodeWithPixels(SvgPixelBuffer pixels)
    {
        return new SvgPngCodec().EncodeWithPixelsAndOptions(pixels, new SvgPngEncodeOptions());
    }

    /// <summary>Encodes premultiplied pixels as a deterministic RGBA8 PNG.</summary>
    /// <param name="pixels">The positive-size premultiplied pixel buffer.</param>
    /// <param name="options">The filtering, compression, metadata, and security configuration.</param>
    /// <returns>The complete PNG bytes or a stable failure.</returns>
    public SvgPngEncodeResult EncodeWithPixelsAndOptions(SvgPixelBuffer pixels, SvgPngEncodeOptions options)
    {
        SvgPngEncodeResult result = new SvgPngEncodeResult();
        SvgPngEncodeError validation = new SvgPngCodec().ValidateEncode(pixels, options);
        if (validation != SvgPngEncodeError.None)
        {
            result.Error = validation;
            return result;
        }
        long rowBytesValue = (long)pixels.Width * 4;
        long filteredValue = (rowBytesValue + 1) * pixels.Height;
        if (filteredValue > options.RasterLimits.MaxPngDecodedBytes || filteredValue > System.Array.MaxLength)
        {
            result.Error = SvgPngEncodeError.ByteLimitExceeded;
            return result;
        }
        byte[] filtered = new SvgPngCodec().EncodeScanlines(pixels, (int)rowBytesValue, (int)filteredValue, options.FilterMode);
        SvgPngCompressResult compressed = new SvgPngCodec().Compress(filtered, options.CompressionMode);
        if (!compressed.Success)
        {
            result.Error = SvgPngEncodeError.CompressionFailed;
            return result;
        }
        int metadataBytes = options.IncludeSrgbChunk ? 13 : 0;
        if (options.PixelsPerMeterX > 0) metadataBytes = metadataBytes + 21;
        long encodedValue = 8L + 25 + metadataBytes + 12 + (compressed.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length + 12;
        if (encodedValue > options.MaximumEncodedBytes || encodedValue > System.Array.MaxLength)
        {
            result.Error = SvgPngEncodeError.ByteLimitExceeded;
            return result;
        }
        byte[] encoded = new SvgPngCodec().BuildPng(pixels.Width, pixels.Height, compressed.Bytes, options, (int)encodedValue);
        result.Success = true;
        result.Error = SvgPngEncodeError.None;
        result.Bytes = encoded;
        result.BytesWritten = encoded.Length;
        return result;
    }

    /// <summary>Encodes premultiplied pixels and writes the complete PNG to a writable stream using secure defaults.</summary>
    /// <param name="pixels">The positive-size premultiplied pixel buffer.</param>
    /// <param name="destination">The writable destination stream.</param>
    /// <returns>The PNG bytes and stream-write outcome.</returns>
    public SvgPngEncodeResult EncodeWithPixelsAndDestination(SvgPixelBuffer pixels, System.IO.Stream destination)
    {
        return new SvgPngCodec().EncodeWithPixelsAndDestinationAndOptions(pixels, destination, new SvgPngEncodeOptions());
    }

    /// <summary>Encodes premultiplied pixels and writes the complete PNG to a writable stream.</summary>
    /// <param name="pixels">The positive-size premultiplied pixel buffer.</param>
    /// <param name="destination">The writable destination stream.</param>
    /// <param name="options">The filtering, compression, metadata, and security configuration.</param>
    /// <returns>The PNG bytes and stream-write outcome.</returns>
    public SvgPngEncodeResult EncodeWithPixelsAndDestinationAndOptions(SvgPixelBuffer pixels, System.IO.Stream? destination, SvgPngEncodeOptions options)
    {
        if (destination == null || !destination.CanWrite) return new SvgPngEncodeResult();
        SvgPngEncodeResult result = new SvgPngCodec().EncodeWithPixelsAndOptions(pixels, options);
        if (!result.Success) return result;
        System.Threading.Tasks.Task task = System.Threading.Tasks.Task.Run(() => destination.Write(result.Bytes, 0, result.Bytes.Length));
        System.IAsyncResult pending = task;
        pending.AsyncWaitHandle.WaitOne();
        if (task.IsFaulted)
        {
            System.AggregateException observed = task.Exception;
            if (observed != null) observed.GetBaseException();
            result.Success = false;
            result.Error = SvgPngEncodeError.StreamWriteFailed;
            result.BytesWritten = 0;
            return result;
        }
        if (task.IsCanceled)
        {
            result.Success = false;
            result.Error = SvgPngEncodeError.StreamWriteFailed;
            result.BytesWritten = 0;
            return result;
        }
        return result;
    }

    /// <summary>Encodes premultiplied pixels as a base64 PNG data URL using secure defaults.</summary>
    /// <param name="pixels">The positive-size premultiplied pixel buffer.</param>
    /// <returns>The PNG bytes and data URL or a stable failure.</returns>
    public SvgPngEncodeResult EncodeDataUrlWithPixels(SvgPixelBuffer pixels)
    {
        return new SvgPngCodec().EncodeDataUrlWithPixelsAndOptions(pixels, new SvgPngEncodeOptions());
    }

    /// <summary>Encodes premultiplied pixels as a base64 PNG data URL.</summary>
    /// <param name="pixels">The positive-size premultiplied pixel buffer.</param>
    /// <param name="options">The filtering, compression, metadata, and security configuration.</param>
    /// <returns>The PNG bytes and data URL or a stable failure.</returns>
    public SvgPngEncodeResult EncodeDataUrlWithPixelsAndOptions(SvgPixelBuffer pixels, SvgPngEncodeOptions options)
    {
        SvgPngEncodeResult result = new SvgPngCodec().EncodeWithPixelsAndOptions(pixels, options);
        if (!result.Success) return result;
        SvgDataUrlEncodeResult encoded = new SvgDataUrl().EncodeWithBytesAndMediaTypeAndUseBase64AndMaxDecodedBytes(result.Bytes, SvgMimeTypes.Png, true, options.MaximumEncodedBytes);
        if (!encoded.Success)
        {
            result.Success = false;
            result.Error = SvgPngEncodeError.DataUrlFailed;
            result.DataUrl = "";
            return result;
        }
        result.DataUrl = encoded.DataUrl;
        return result;
    }
SvgPngEncodeError ValidateEncode(SvgPixelBuffer? pixels, SvgPngEncodeOptions? options)
    {
        if (pixels == null || options == null || !new SvgPngCodec().ValidDecodeLimits(options.RasterLimits) || options.MaximumEncodedBytes < 1) return SvgPngEncodeError.InvalidArgument;
        if ((int)options.FilterMode < 0 || (int)options.FilterMode > 5 || (int)options.CompressionMode < 0 || (int)options.CompressionMode > 2) return SvgPngEncodeError.InvalidArgument;
        if (options.PixelsPerMeterX < 0 || options.PixelsPerMeterY < 0 || options.PixelsPerMeterX == 0 && options.PixelsPerMeterY != 0 || options.PixelsPerMeterX != 0 && options.PixelsPerMeterY == 0) return SvgPngEncodeError.InvalidArgument;
        if (pixels.Data == null || pixels.Width < 1 || pixels.Height < 1 || pixels.Offset < 0 || pixels.Stride < 0) return SvgPngEncodeError.InvalidArgument;
        if (pixels.ColorSpace != SvgPixelColorSpace.Srgb && pixels.ColorSpace != SvgPixelColorSpace.SrgbLinear) return SvgPngEncodeError.InvalidArgument;
        long pixelCount = (long)pixels.Width * pixels.Height;
        if (pixels.Width > options.RasterLimits.MaxWidth || pixels.Height > options.RasterLimits.MaxHeight || pixelCount > options.RasterLimits.MaxSurfacePixels) return SvgPngEncodeError.DimensionLimitExceeded;
        long rowBytes = (long)pixels.Width * 4;
        if (rowBytes > int.MaxValue || pixels.RowBytes != rowBytes || pixels.Stride < rowBytes) return SvgPngEncodeError.InvalidArgument;
        long footprint = (long)(pixels.Height - 1) * pixels.Stride + rowBytes;
        if (pixels.ByteLength != footprint || pixels.Offset > pixels.Data.Length || (long)pixels.Offset + footprint > pixels.Data.Length) return SvgPngEncodeError.InvalidArgument;
        if (footprint > options.RasterLimits.MaxPixelBytes) return SvgPngEncodeError.ByteLimitExceeded;
        int y = 0;
        while (y < pixels.Height)
        {
            int index = pixels.Offset + y * pixels.Stride;
            int end = index + pixels.RowBytes;
            while (index < end)
            {
                byte alpha = pixels.Data[index + 3];
                if (pixels.Data[index] > alpha || pixels.Data[index + 1] > alpha || pixels.Data[index + 2] > alpha) return SvgPngEncodeError.InvalidArgument;
                index = index + 4;
            }
            y = y + 1;
        }
        return SvgPngEncodeError.None;
    }
byte[] EncodeScanlines(SvgPixelBuffer pixels, int rowBytes, int totalBytes, SvgPngFilterMode mode)
    {
        byte[] filtered = new byte[totalBytes];
        byte[] row = new byte[rowBytes];
        byte[] previous = new byte[rowBytes];
        byte[] candidate = new byte[rowBytes];
        byte[] best = new byte[rowBytes];
        int y = 0;
        while (y < pixels.Height)
        {
            new SvgPngCodec().FillStraightRow(pixels, y, row);
            int filter = mode == SvgPngFilterMode.Adaptive ? new SvgPngCodec().ChooseFilter(row, previous, candidate, best) : (int)mode - 1;
            if (mode != SvgPngFilterMode.Adaptive) new SvgPngCodec().FilterRow(row, previous, filter, best);
            int destination = y * (rowBytes + 1);
            filtered[destination] = (byte)filter;
            System.Array.Copy(best, 0, filtered, destination + 1, rowBytes);
            System.Array.Copy(row, previous, rowBytes);
            y = y + 1;
        }
        return filtered;
    }
void FillStraightRow(SvgPixelBuffer pixels, int y, byte[] row)
    {
        int source = pixels.Offset + y * pixels.Stride;
        int destination = 0;
        while (destination < row.Length)
        {
            byte alpha = pixels.Data[source + 3];
            byte red = new SvgPngCodec().Unpremultiply(pixels.Data[source], alpha);
            byte green = new SvgPngCodec().Unpremultiply(pixels.Data[source + 1], alpha);
            byte blue = new SvgPngCodec().Unpremultiply(pixels.Data[source + 2], alpha);
            if (pixels.ColorSpace == SvgPixelColorSpace.SrgbLinear)
            {
                red = new SvgPngCodec().LinearToSrgb(red);
                green = new SvgPngCodec().LinearToSrgb(green);
                blue = new SvgPngCodec().LinearToSrgb(blue);
            }
            row[destination] = red;
            row[destination + 1] = green;
            row[destination + 2] = blue;
            row[destination + 3] = alpha;
            source = source + 4;
            destination = destination + 4;
        }
    }
byte Unpremultiply(byte channel, byte alpha)
    {
        if (alpha == 0) return 0;
        int straight = (channel * 255 + alpha / 2) / alpha;
        return (byte)(straight > 255 ? 255 : straight);
    }
byte LinearToSrgb(byte value)
    {
        double linear = value / 255.0;
        double encoded = linear <= 0.0031308 ? 12.92 * linear : 1.055 * System.Math.Pow(linear, 1.0 / 2.4) - 0.055;
        return (byte)System.Math.Round(encoded * 255, System.MidpointRounding.AwayFromZero);
    }
int ChooseFilter(byte[] row, byte[] previous, byte[] candidate, byte[] best)
    {
        long bestScore = long.MaxValue;
        int selected = 0;
        int filter = 0;
        while (filter < 5)
        {
            long score = new SvgPngCodec().FilterRow(row, previous, filter, candidate);
            if (score < bestScore)
            {
                bestScore = score;
                selected = filter;
                System.Array.Copy(candidate, best, candidate.Length);
            }
            filter = filter + 1;
        }
        return selected;
    }
long FilterRow(byte[] row, byte[] previous, int filter, byte[] destination)
    {
        long score = 0;
        int index = 0;
        while (index < row.Length)
        {
            int left = index < 4 ? 0 : row[index - 4];
            int up = previous[index];
            int upperLeft = index < 4 ? 0 : previous[index - 4];
            int predictor = 0;
            if (filter == 1) predictor = left;
            else if (filter == 2) predictor = up;
            else if (filter == 3) predictor = (left + up) / 2;
            else if (filter == 4) predictor = new SvgPngCodec().Paeth(left, up, upperLeft);
            int filtered = (row[index] - predictor) & 255;
            destination[index] = (byte)filtered;
            score = score + (filtered < 128 ? filtered : 256 - filtered);
            index = index + 1;
        }
        return score;
    }
int Paeth(int left, int up, int upperLeft)
    {
        int estimate = left + up - upperLeft;
        int leftDistance = System.Math.Abs(estimate - left);
        int upDistance = System.Math.Abs(estimate - up);
        int upperLeftDistance = System.Math.Abs(estimate - upperLeft);
        if (leftDistance <= upDistance && leftDistance <= upperLeftDistance) return left;
        if (upDistance <= upperLeftDistance) return up;
        return upperLeft;
    }
SvgPngCompressResult Compress(byte[] bytes, SvgPngCompressionMode mode)
    {
        System.Threading.Tasks.Task<SvgPngCompressResult> task = System.Threading.Tasks.Task.Run(() => new SvgPngCodec().CompressUnsafe(bytes, mode));
        System.IAsyncResult pending = task;
        pending.AsyncWaitHandle.WaitOne();
        if (task.IsFaulted)
        {
            System.AggregateException observed = task.Exception;
            if (observed != null) observed.GetBaseException();
            return new SvgPngCompressResult(false, null);
        }
        if (task.IsCanceled) return new SvgPngCompressResult(false, null);
        return task.Result;
    }
SvgPngCompressResult CompressUnsafe(byte[] bytes, SvgPngCompressionMode mode)
    {
        System.IO.Compression.CompressionLevel level = System.IO.Compression.CompressionLevel.Fastest;
        if (mode == SvgPngCompressionMode.Balanced) level = System.IO.Compression.CompressionLevel.Optimal;
        else if (mode == SvgPngCompressionMode.Smallest) level = System.IO.Compression.CompressionLevel.SmallestSize;
        using (System.IO.MemoryStream output = new System.IO.MemoryStream())
        {
            using (System.IO.Compression.ZLibStream zlib = new System.IO.Compression.ZLibStream(output, level, true))
            {
                zlib.Write(bytes, 0, bytes.Length);
            }
            return new SvgPngCompressResult(true, output.ToArray());
        }
    }
byte[] BuildPng(int width, int height, byte[] compressed, SvgPngEncodeOptions options, int length)
    {
        byte[] result = new byte[length];
        byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10];
        System.Array.Copy(signature, result, signature.Length);
        byte[] header = new byte[13];
        new SvgPngCodec().WriteUInt32(header, 0, (uint)width);
        new SvgPngCodec().WriteUInt32(header, 4, (uint)height);
        header[8] = 8;
        header[9] = 6;
        int offset = new SvgPngCodec().WriteChunk(result, 8, "IHDR", header);
        if (options.IncludeSrgbChunk) offset = new SvgPngCodec().WriteChunk(result, offset, "sRGB", [0]);
        if (options.PixelsPerMeterX > 0)
        {
            byte[] physical = new byte[9];
            new SvgPngCodec().WriteUInt32(physical, 0, (uint)options.PixelsPerMeterX);
            new SvgPngCodec().WriteUInt32(physical, 4, (uint)options.PixelsPerMeterY);
            physical[8] = 1;
            offset = new SvgPngCodec().WriteChunk(result, offset, "pHYs", physical);
        }
        offset = new SvgPngCodec().WriteChunk(result, offset, "IDAT", compressed);
        new SvgPngCodec().WriteChunk(result, offset, "IEND", System.Array.Empty<byte>());
        return result;
    }
int WriteChunk(byte[] destination, int offset, string type, byte[] data)
    {
        new SvgPngCodec().WriteUInt32(destination, offset, (uint)data.Length);
        int typeOffset = offset + 4;
        int index = 0;
        while (index < 4)
        {
            destination[typeOffset + index] = (byte)type[index];
            index = index + 1;
        }
        System.Array.Copy(data, 0, destination, offset + 8, data.Length);
        uint crc = new SvgPngCodec().ChunkCrc(destination, typeOffset, data.Length + 4);
        new SvgPngCodec().WriteUInt32(destination, offset + 8 + data.Length, crc);
        return offset + 12 + data.Length;
    }
uint ChunkCrc(byte[] bytes, int offset, int count)
    {
        uint crc = 4294967295;
        int index = offset;
        while (index < offset + count)
        {
            crc = crc ^ bytes[index];
            int bit = 0;
            while (bit < 8)
            {
                crc = (crc & 1) == 0 ? crc >> 1 : 3988292384 ^ crc >> 1;
                bit = bit + 1;
            }
            index = index + 1;
        }
        return crc ^ 4294967295;
    }
void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)((value >> 24) & 255);
        bytes[offset + 1] = (byte)((value >> 16) & 255);
        bytes[offset + 2] = (byte)((value >> 8) & 255);
        bytes[offset + 3] = (byte)(value & 255);
    }
bool ReadChunks(SvgPngDecodeState state)
    {
        System.IO.MemoryStream compressed = new System.IO.MemoryStream();
        int position = 8;
        bool sawHeader = false;
        bool sawData = false;
        bool endedData = false;
        bool sawEnd = false;
        while (position < state.Bytes.Length && !sawEnd)
        {
            if (state.Result.ChunksRead >= state.Limits.MaxPngChunks) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.ChunkLimitExceeded);
            if (state.Bytes.Length - position < 12) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidChunk);
            uint lengthValue = new SvgPngCodec().ReadUInt32(state.Bytes, position);
            if (lengthValue > int.MaxValue) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidChunk);
            int length = (int)lengthValue;
            long endValue = (long)position + 12 + length;
            if (endValue > state.Bytes.Length) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidChunk);
            int typeOffset = position + 4;
            int dataOffset = position + 8;
            int crcOffset = dataOffset + length;
            if (!new SvgPngCodec().ValidChunkType(state.Bytes, typeOffset)) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidChunk);
            if (!new SvgPngCodec().ValidChunkCrc(state.Bytes, typeOffset, 4 + length, new SvgPngCodec().ReadUInt32(state.Bytes, crcOffset))) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.CrcMismatch);
            state.Result.ChunksRead = state.Result.ChunksRead + 1;
            bool header = new SvgPngCodec().Chunk(state.Bytes, typeOffset, 73, 72, 68, 82);
            bool palette = new SvgPngCodec().Chunk(state.Bytes, typeOffset, 80, 76, 84, 69);
            bool transparency = new SvgPngCodec().Chunk(state.Bytes, typeOffset, 116, 82, 78, 83);
            bool data = new SvgPngCodec().Chunk(state.Bytes, typeOffset, 73, 68, 65, 84);
            bool end = new SvgPngCodec().Chunk(state.Bytes, typeOffset, 73, 69, 78, 68);
            if (!sawHeader)
            {
                if (!header || position != 8 || length != 13) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidHeader);
                sawHeader = true;
            }
            else if (header) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidHeader);
            else if (palette)
            {
                if (sawData || state.Palette != null || !new SvgPngCodec().ReadPalette(state, dataOffset, length)) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidPalette);
            }
            else if (transparency)
            {
                if (sawData || state.HasTransparency || !new SvgPngCodec().ReadTransparency(state, dataOffset, length)) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidChunk);
            }
            else if (data)
            {
                if (endedData) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidChunk);
                if (state.ColorType == 3 && state.Palette == null) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidPalette);
                if (compressed.Length > state.Limits.MaxPngDecodedBytes - length) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.DecodedSizeLimitExceeded);
                sawData = true;
                compressed.Write(state.Bytes, dataOffset, length);
            }
            else if (end)
            {
                if (!sawData || length != 0 || endValue != state.Bytes.Length) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidChunk);
                sawEnd = true;
            }
            else
            {
                if (sawData) endedData = true;
                if ((state.Bytes[typeOffset] & 32) == 0) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidChunk);
            }
            if (sawData && !data && !end) endedData = true;
            position = (int)endValue;
        }
        if (!sawHeader || !sawData || !sawEnd || position != state.Bytes.Length) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidChunk);
        if (state.ColorType == 3 && state.Palette == null) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidPalette);
        state.Compressed = compressed.ToArray();
        return true;
    }
bool ReadPalette(SvgPngDecodeState state, int offset, int length)
    {
        if (state.ColorType == 0 || state.ColorType == 4 || length < 3 || length > 768 || length % 3 != 0) return false;
        int entries = length / 3;
        if (state.ColorType == 3 && entries > 1 << state.BitDepth) return false;
        state.Palette = new byte[length];
        System.Array.Copy(state.Bytes, offset, state.Palette, 0, length);
        return true;
    }
bool ReadTransparency(SvgPngDecodeState state, int offset, int length)
    {
        if (state.ColorType == 0)
        {
            if (length != 2) return false;
            state.TransparentGray = new SvgPngCodec().ReadUInt16(state.Bytes, offset);
        }
        else if (state.ColorType == 2)
        {
            if (length != 6) return false;
            state.TransparentRed = new SvgPngCodec().ReadUInt16(state.Bytes, offset);
            state.TransparentGreen = new SvgPngCodec().ReadUInt16(state.Bytes, offset + 2);
            state.TransparentBlue = new SvgPngCodec().ReadUInt16(state.Bytes, offset + 4);
        }
        else if (state.ColorType == 3)
        {
            if (state.Palette == null || length > state.Palette.Length / 3) return false;
            state.PaletteAlpha = new byte[state.Palette.Length / 3];
            int index = 0;
            while (index < state.PaletteAlpha.Length)
            {
                state.PaletteAlpha[index] = index < length ? state.Bytes[offset + index] : (byte)255;
                index = index + 1;
            }
        }
        else return false;
        state.HasTransparency = true;
        return true;
    }
long ExpectedInflatedBytes(SvgPngDecodeState state)
    {
        int channels = new SvgPngCodec().ChannelCount(state.ColorType);
        if (state.InterlaceMethod == 0) return new SvgPngCodec().PassBytes(state.Width, state.Height, channels, state.BitDepth);
        int[] startsX = [0, 4, 0, 2, 0, 1, 0];
        int[] startsY = [0, 0, 4, 0, 2, 0, 1];
        int[] stepsX = [8, 8, 4, 4, 2, 2, 1];
        int[] stepsY = [8, 8, 8, 4, 4, 2, 2];
        long total = 0;
        int pass = 0;
        while (pass < 7)
        {
            int width = new SvgPngCodec().PassSize(state.Width, startsX[pass], stepsX[pass]);
            int height = new SvgPngCodec().PassSize(state.Height, startsY[pass], stepsY[pass]);
            long bytes = new SvgPngCodec().PassBytes(width, height, channels, state.BitDepth);
            if (bytes < 0 || total > long.MaxValue - bytes) return -1;
            total = total + bytes;
            pass = pass + 1;
        }
        return total;
    }
long PassBytes(int width, int height, int channels, int bitDepth)
    {
        if (width == 0 || height == 0) return 0;
        long bits = (long)width * channels * bitDepth;
        long rowBytes = (bits + 7) / 8;
        if (rowBytes > long.MaxValue - 1 || height > long.MaxValue / (rowBytes + 1)) return -1;
        return (rowBytes + 1) * height;
    }
SvgPngInflateResult Inflate(byte[]? compressed, int expected)
    {
        System.Threading.Tasks.Task<SvgPngInflateResult> task = System.Threading.Tasks.Task.Run(() => new SvgPngCodec().InflateUnsafe(compressed, expected));
        System.IAsyncResult pending = task;
        pending.AsyncWaitHandle.WaitOne();
        if (task.IsFaulted)
        {
            System.AggregateException observed = task.Exception;
            if (observed != null) observed.GetBaseException();
            return new SvgPngInflateResult(false, null);
        }
        if (task.IsCanceled) return new SvgPngInflateResult(false, null);
        return task.Result;
    }
SvgPngInflateResult InflateUnsafe(byte[]? compressed, int expected)
    {
        using (System.IO.MemoryStream input = new System.IO.MemoryStream((compressed ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), false))
        using (System.IO.Compression.ZLibStream zlib = new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress, false))
        {
            byte[] output = new byte[expected];
            int offset = 0;
            while (offset < output.Length)
            {
                int read = zlib.Read(output, offset, output.Length - offset);
                if (read == 0) return new SvgPngInflateResult(false, null);
                offset = offset + read;
            }
            if (zlib.ReadByte() != -1) return new SvgPngInflateResult(false, null);
            return new SvgPngInflateResult(true, output);
        }
    }
bool DecodeScanlines(SvgPngDecodeState state)
    {
        if (state.InterlaceMethod == 0)
        {
            if (!new SvgPngCodec().DecodePass(state, 0, 0, 1, 1, state.Width, state.Height)) return false;
        }
        else
        {
            int[] startsX = [0, 4, 0, 2, 0, 1, 0];
            int[] startsY = [0, 0, 4, 0, 2, 0, 1];
            int[] stepsX = [8, 8, 4, 4, 2, 2, 1];
            int[] stepsY = [8, 8, 8, 4, 4, 2, 2];
            int pass = 0;
            while (pass < 7)
            {
                int width = new SvgPngCodec().PassSize(state.Width, startsX[pass], stepsX[pass]);
                int height = new SvgPngCodec().PassSize(state.Height, startsY[pass], stepsY[pass]);
                if (!new SvgPngCodec().DecodePass(state, startsX[pass], startsY[pass], stepsX[pass], stepsY[pass], width, height)) return false;
                pass = pass + 1;
            }
        }
        if (state.RawOffset != (state.Inflated ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidScanline);
        return true;
    }
bool DecodePass(SvgPngDecodeState state, int startX, int startY, int stepX, int stepY, int width, int height)
    {
        if (width == 0 || height == 0) return true;
        int channels = new SvgPngCodec().ChannelCount(state.ColorType);
        long rowValue = ((long)width * channels * state.BitDepth + 7) / 8;
        if (rowValue > int.MaxValue) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.DecodedSizeLimitExceeded);
        int rowBytes = (int)rowValue;
        int bytesPerPixel = System.Math.Max(1, (channels * state.BitDepth + 7) / 8);
        byte[] previous = new byte[rowBytes];
        byte[] row = new byte[rowBytes];
        int rowIndex = 0;
        while (rowIndex < height)
        {
            if (state.RawOffset >= (state.Inflated ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidScanline);
            int filter = state.Inflated[state.RawOffset];
            state.RawOffset = state.RawOffset + 1;
            if (filter < 0 || filter > 4 || state.RawOffset > state.Inflated.Length - rowBytes) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidScanline);
            int index = 0;
            while (index < rowBytes)
            {
                int encoded = state.Inflated[state.RawOffset + index];
                int left = index < bytesPerPixel ? 0 : row[index - bytesPerPixel];
                int above = previous[index];
                int upperLeft = index < bytesPerPixel ? 0 : previous[index - bytesPerPixel];
                int predictor = new SvgPngCodec().FilterPredictor(filter, left, above, upperLeft);
                row[index] = (byte)((encoded + predictor) & 255);
                index = index + 1;
            }
            state.RawOffset = state.RawOffset + rowBytes;
            int outputY = startY + rowIndex * stepY;
            if (!new SvgPngCodec().DecodeRow(state, row, startX, outputY, stepX, width)) return false;
            byte[] swap = previous;
            previous = row;
            row = swap;
            rowIndex = rowIndex + 1;
        }
        return true;
    }
int FilterPredictor(int filter, int left, int above, int upperLeft)
    {
        if (filter == 0) return 0;
        if (filter == 1) return left;
        if (filter == 2) return above;
        if (filter == 3) return (left + above) / 2;
        int estimate = left + above - upperLeft;
        int leftDistance = System.Math.Abs(estimate - left);
        int aboveDistance = System.Math.Abs(estimate - above);
        int upperLeftDistance = System.Math.Abs(estimate - upperLeft);
        if (leftDistance <= aboveDistance && leftDistance <= upperLeftDistance) return left;
        return aboveDistance <= upperLeftDistance ? above : upperLeft;
    }
bool DecodeRow(SvgPngDecodeState state, byte[] row, int startX, int y, int stepX, int width)
    {
        int bitOffset = 0;
        int pixel = 0;
        while (pixel < width)
        {
            int first = new SvgPngCodec().ReadSample(row, bitOffset, state.BitDepth);
            bitOffset = bitOffset + state.BitDepth;
            int second = 0;
            int third = 0;
            int fourth = 0;
            int channels = new SvgPngCodec().ChannelCount(state.ColorType);
            if (channels > 1)
            {
                second = new SvgPngCodec().ReadSample(row, bitOffset, state.BitDepth);
                bitOffset = bitOffset + state.BitDepth;
            }
            if (channels > 2)
            {
                third = new SvgPngCodec().ReadSample(row, bitOffset, state.BitDepth);
                bitOffset = bitOffset + state.BitDepth;
            }
            if (channels > 3)
            {
                fourth = new SvgPngCodec().ReadSample(row, bitOffset, state.BitDepth);
                bitOffset = bitOffset + state.BitDepth;
            }
            if (!new SvgPngCodec().WriteDecodedPixel(state, startX + pixel * stepX, y, first, second, third, fourth)) return false;
            pixel = pixel + 1;
        }
        return true;
    }
bool WriteDecodedPixel(SvgPngDecodeState state, int x, int y, int first, int second, int third, int fourth)
    {
        int red = 0;
        int green = 0;
        int blue = 0;
        int alpha = 255;
        if (state.ColorType == 0)
        {
            red = new SvgPngCodec().ScaleSample(first, state.BitDepth);
            green = red;
            blue = red;
            if (state.HasTransparency && first == state.TransparentGray) alpha = 0;
        }
        else if (state.ColorType == 2)
        {
            red = new SvgPngCodec().ScaleSample(first, state.BitDepth);
            green = new SvgPngCodec().ScaleSample(second, state.BitDepth);
            blue = new SvgPngCodec().ScaleSample(third, state.BitDepth);
            if (state.HasTransparency && first == state.TransparentRed && second == state.TransparentGreen && third == state.TransparentBlue) alpha = 0;
        }
        else if (state.ColorType == 3)
        {
            int entries = state.Palette == null ? 0 : state.Palette.Length / 3;
            if (first < 0 || first >= entries) return new SvgPngCodec().DecodeFailure(state, SvgPngDecodeError.InvalidPalette);
            red = (state.Palette ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[first * 3];
            green = state.Palette[first * 3 + 1];
            blue = state.Palette[first * 3 + 2];
            if (state.PaletteAlpha != null) alpha = state.PaletteAlpha[first];
        }
        else if (state.ColorType == 4)
        {
            red = new SvgPngCodec().ScaleSample(first, state.BitDepth);
            green = red;
            blue = red;
            alpha = new SvgPngCodec().ScaleSample(second, state.BitDepth);
        }
        else
        {
            red = new SvgPngCodec().ScaleSample(first, state.BitDepth);
            green = new SvgPngCodec().ScaleSample(second, state.BitDepth);
            blue = new SvgPngCodec().ScaleSample(third, state.BitDepth);
            alpha = new SvgPngCodec().ScaleSample(fourth, state.BitDepth);
        }
        int offset = (state.Pixels ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Offset + y * state.Pixels.Stride + x * 4;
        state.Pixels.Data[offset] = (byte)((red * alpha + 127) / 255);
        state.Pixels.Data[offset + 1] = (byte)((green * alpha + 127) / 255);
        state.Pixels.Data[offset + 2] = (byte)((blue * alpha + 127) / 255);
        state.Pixels.Data[offset + 3] = (byte)alpha;
        return true;
    }
int ReadSample(byte[] row, int bitOffset, int bitDepth)
    {
        int byteOffset = bitOffset / 8;
        int shift = 8 - bitDepth - bitOffset % 8;
        if (bitDepth < 8) return row[byteOffset] >> shift & (1 << bitDepth) - 1;
        if (bitDepth == 8) return row[byteOffset];
        return row[byteOffset] << 8 | row[byteOffset + 1];
    }
int ScaleSample(int value, int bitDepth)
    {
        if (bitDepth == 8) return value;
        int maximum = bitDepth == 16 ? 65535 : (1 << bitDepth) - 1;
        return (value * 255 + maximum / 2) / maximum;
    }
int ChannelCount(int colorType)
    {
        if (colorType == 0 || colorType == 3) return 1;
        if (colorType == 2) return 3;
        if (colorType == 4) return 2;
        return 4;
    }
int PassSize(int total, int start, int step)
    {
        return total <= start ? 0 : (total - start + step - 1) / step;
    }
bool Chunk(byte[] bytes, int offset, int first, int second, int third, int fourth)
    {
        return bytes[offset] == first && bytes[offset + 1] == second && bytes[offset + 2] == third && bytes[offset + 3] == fourth;
    }
bool ValidChunkType(byte[] bytes, int offset)
    {
        int index = 0;
        while (index < 4)
        {
            int value = bytes[offset + index];
            if (value < 65 || value > 90 && value < 97 || value > 122) return false;
            index = index + 1;
        }
        return (bytes[offset + 2] & 32) == 0;
    }
bool DecodeFailure(SvgPngDecodeState state, SvgPngDecodeError error)
    {
        if (state.Result.Error == SvgPngDecodeError.InvalidArgument) state.Result.Error = error;
        return false;
    }
SvgPngDecodeError ProbeDecodeError(SvgPngProbeError error)
    {
        if (error == SvgPngProbeError.InvalidArgument) return SvgPngDecodeError.InvalidArgument;
        if (error == SvgPngProbeError.InvalidSignature) return SvgPngDecodeError.InvalidSignature;
        if (error == SvgPngProbeError.DimensionLimitExceeded) return SvgPngDecodeError.DimensionLimitExceeded;
        return SvgPngDecodeError.InvalidHeader;
    }
bool ValidDecodeLimits(SvgRasterLimits limits)
    {
        return new SvgPngCodec().ValidLimits(limits) && limits.MaxPngChunks > 0 && limits.MaxPngDecodedBytes > 0;
    }
bool ValidLimits(SvgRasterLimits limits)
    {
        return limits != null && limits.MaxWidth > 0 && limits.MaxHeight > 0 && limits.MaxSurfacePixels > 0 && limits.MaxPixelBytes > 0;
    }
bool HasSignature(byte[] bytes)
    {
        return bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71 && bytes[4] == 13 && bytes[5] == 10 && bytes[6] == 26 && bytes[7] == 10;
    }
bool ValidFormat(int bitDepth, int colorType)
    {
        if (colorType == 0) return bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8 || bitDepth == 16;
        if (colorType == 2) return bitDepth == 8 || bitDepth == 16;
        if (colorType == 3) return bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8;
        if (colorType == 4 || colorType == 6) return bitDepth == 8 || bitDepth == 16;
        return false;
    }
bool ValidHeaderCrc(byte[] bytes)
    {
        uint expected = new SvgPngCodec().ReadUInt32(bytes, 29);
        uint crc = 4294967295;
        int index = 12;
        while (index < 29)
        {
            crc = crc ^ bytes[index];
            int bit = 0;
            while (bit < 8)
            {
                crc = (crc & 1) == 0 ? crc >> 1 : 3988292384 ^ crc >> 1;
                bit = bit + 1;
            }
            index = index + 1;
        }
        return (crc ^ 4294967295) == expected;
    }
uint ReadUInt32(byte[] bytes, int offset)
    {
        return (uint)bytes[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }
int ReadUInt16(byte[] bytes, int offset)
    {
        return bytes[offset] << 8 | bytes[offset + 1];
    }
bool ValidChunkCrc(byte[] bytes, int offset, int count, uint expected)
    {
        uint crc = 4294967295;
        int index = offset;
        while (index < offset + count)
        {
            crc = crc ^ bytes[index];
            int bit = 0;
            while (bit < 8)
            {
                crc = (crc & 1) == 0 ? crc >> 1 : 3988292384 ^ crc >> 1;
                bit = bit + 1;
            }
            index = index + 1;
        }
        return (crc ^ 4294967295) == expected;
    }
}
