namespace jpeg;

/// <summary>Provides the high-level entry points for inspecting, decoding, encoding, transforming, and updating JPEG data.</summary>
/// <remarks>All byte-array results are newly allocated. Methods that accept a byte array read it without taking ownership of it.</remarks>
internal static class Jpeg
{
    /// <summary>Creates an identifier service that can inspect JPEG data without decoding pixels.</summary>
    /// <remarks>The returned interface can be injected into application services and replaced by a test implementation.</remarks>
    /// <returns>A new independent JPEG identifier.</returns>
    public static IJpegIdentifier CreateIdentifier()
    {
        return new JpegIdentificationService();
    }

    /// <summary>Creates a reader service that can decode JPEG data.</summary>
    /// <remarks>The returned interface can be injected into application services and replaced by a test implementation.</remarks>
    /// <returns>A new independent JPEG reader.</returns>
    public static IJpegReader CreateReader()
    {
        return new JpegImageReader();
    }

    /// <summary>Creates a writer service that can encode JPEG data.</summary>
    /// <remarks>The returned interface can be injected into application services and replaced by a test implementation.</remarks>
    /// <returns>A new independent JPEG writer.</returns>
    public static IJpegWriter CreateWriter()
    {
        return new JpegImageWriter();
    }

    /// <summary>Reads the structure and metadata of a JPEG byte array without decoding its pixels.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    /// <exception cref="JpegInvalidDataException">Thrown when the data is not a valid JPEG stream under the selected error policy.</exception>
    public static JpegInfo Identify(byte[] data)
    {
        return JpegIdentifier.Identify(data);
    }

    /// <summary>Reads the structure and metadata of a JPEG byte array using explicit limits and parsing policies.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <param name="options">Limits and policies used while parsing the data.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>data</c> or <c>options</c> is null.</exception>
    public static JpegInfo Identify(byte[] data, JpegReadOptions options)
    {
        return JpegIdentifier.Identify(data, options);
    }

    /// <summary>Reads JPEG information from a byte array without decoding pixels.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public static JpegInfo IdentifyBytes(byte[] data)
    {
        return Identify(data);
    }

    /// <summary>Reads JPEG information from a byte array with explicit parsing options.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <param name="options">Limits and policies used while parsing the data.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public static JpegInfo IdentifyBytes(byte[] data, JpegReadOptions options)
    {
        return Identify(data, options);
    }

    /// <summary>Reads JPEG information from a file without decoding pixels.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <c>path</c> is empty.</exception>
    public static JpegInfo IdentifyFile(string path)
    {
        return Identify(new JpegIo().ReadFile(path, default(System.Threading.CancellationToken)));
    }

    /// <summary>Reads JPEG information from a file with explicit parsing options.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="options">Limits and policies used while parsing the file.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public static JpegInfo IdentifyFile(string path, JpegReadOptions options)
    {
        return Identify(new JpegIo().ReadFile(path, default(System.Threading.CancellationToken)), options);
    }

    /// <summary>Reads JPEG information from a stream without decoding pixels.</summary>
    /// <remarks>The method reads from the stream's current position and does not close the stream.</remarks>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public static JpegInfo Identify(System.IO.Stream stream)
    {
        byte[] data = new JpegIo().ReadStream(stream, default(System.Threading.CancellationToken));
        return JpegIdentifier.Identify(data);
    }

    /// <summary>Reads JPEG information from a stream with explicit parsing options.</summary>
    /// <remarks>The method reads from the stream's current position and does not close the stream.</remarks>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <param name="options">Limits and policies used while parsing the stream.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public static JpegInfo Identify(System.IO.Stream stream, JpegReadOptions options)
    {
        byte[] data = new JpegIo().ReadStream(stream, default(System.Threading.CancellationToken));
        return JpegIdentifier.Identify(data, options);
    }

    /// <summary>Decodes a JPEG byte array into an in-memory pixel image.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <returns>A newly allocated image containing decoded pixels and retained metadata.</returns>
    /// <exception cref="JpegUnsupportedException">Thrown when the JPEG coding process or requested output is not supported.</exception>
    public static JpegImage Read(byte[] data)
    {
        return CreateReader().Read(data);
    }

    /// <summary>Decodes a JPEG byte array using explicit output, metadata, and resource policies.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <param name="options">Options controlling parsing, pixel conversion, scaling, and limits.</param>
    /// <returns>A newly allocated image containing decoded pixels and retained metadata.</returns>
    public static JpegImage Read(byte[] data, JpegReadOptions options)
    {
        return CreateReader().ReadWithOptions(data, options);
    }

    /// <summary>Decodes a JPEG byte array; this is an alias for <see cref="Read(byte[])"/>.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public static JpegImage Decode(byte[] data)
    {
        return Read(data);
    }

    /// <summary>Decodes a JPEG byte array with options; this is an alias for <see cref="Read(byte[], JpegReadOptions)"/>.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <param name="options">Options controlling parsing, output, and resource limits.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public static JpegImage Decode(byte[] data, JpegReadOptions options)
    {
        return Read(data, options);
    }

    /// <summary>Decodes a JPEG file into an in-memory pixel image.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public static JpegImage ReadFile(string path)
    {
        return Read(new JpegIo().ReadFile(path, default(System.Threading.CancellationToken)));
    }

    /// <summary>Decodes a JPEG file using explicit read options.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="options">Options controlling parsing, output, and resource limits.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public static JpegImage ReadFile(string path, JpegReadOptions options)
    {
        return Read(new JpegIo().ReadFile(path, default(System.Threading.CancellationToken)), options);
    }

    /// <summary>Decodes JPEG data from a stream into an in-memory pixel image.</summary>
    /// <remarks>The method reads from the current position and leaves the stream open.</remarks>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public static JpegImage Read(System.IO.Stream stream)
    {
        return CreateReader().ReadStream(stream);
    }

    /// <summary>Decodes JPEG data from a stream using explicit read options.</summary>
    /// <remarks>The method reads from the current position and leaves the stream open.</remarks>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <param name="options">Options controlling parsing, output, and resource limits.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public static JpegImage Read(System.IO.Stream stream, JpegReadOptions options)
    {
        return CreateReader().ReadStreamWithOptions(stream, options);
    }

    /// <summary>Encodes an image as JPEG data using default write options.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <returns>A newly allocated array containing the complete JPEG stream.</returns>
    public static byte[] Write(JpegImage image)
    {
        return CreateWriter().Write(image);
    }

    /// <summary>Encodes an image as JPEG data using explicit quality, sampling, and metadata options.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <returns>A newly allocated array containing the complete JPEG stream.</returns>
    public static byte[] Write(JpegImage image, JpegWriteOptions options)
    {
        return CreateWriter().WriteWithOptions(image, options);
    }

    /// <summary>Encodes an image; this is an alias for <see cref="Write(JpegImage)"/>.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    public static byte[] Encode(JpegImage image)
    {
        return Write(image);
    }

    /// <summary>Encodes an image with options; this is an alias for <see cref="Write(JpegImage, JpegWriteOptions)"/>.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    public static byte[] Encode(JpegImage image, JpegWriteOptions options)
    {
        return Write(image, options);
    }

    /// <summary>Copies packed pixels into an image and encodes them with default JPEG options.</summary>
    /// <param name="pixels">Packed pixel bytes in row-major order with no row padding.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="pixelFormat">The channel layout of <c>pixels</c>.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    public static byte[] EncodePixels(byte[] pixels, int width, int height, JpegPixelFormat pixelFormat)
    {
        JpegImage image = CreateImageFromPixels(pixels, width, height, pixelFormat);
        return Write(image);
    }

    /// <summary>Copies packed pixels into an image and encodes them with explicit JPEG options.</summary>
    /// <param name="pixels">Packed pixel bytes in row-major order with no row padding.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="pixelFormat">The channel layout of <c>pixels</c>.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    public static byte[] EncodePixels(byte[] pixels, int width, int height, JpegPixelFormat pixelFormat, JpegWriteOptions options)
    {
        JpegImage image = CreateImageFromPixels(pixels, width, height, pixelFormat);
        return Write(image, options);
    }

    /// <summary>Encodes an image and replaces or creates a JPEG file.</summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="image">The pixel image and metadata to encode.</param>
    public static void WriteFile(string path, JpegImage image)
    {
        ValidateFilePath(path);
        new JpegIo().WriteFile(path, Write(image), default(System.Threading.CancellationToken));
    }

    /// <summary>Encodes an image with explicit options and replaces or creates a JPEG file.</summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    public static void WriteFile(string path, JpegImage image, JpegWriteOptions options)
    {
        ValidateFilePath(path);
        new JpegIo().WriteFile(path, Write(image, options), default(System.Threading.CancellationToken));
    }

    /// <summary>Encodes an image to a writable stream using default options.</summary>
    /// <remarks>The method writes at the current position and leaves the stream open.</remarks>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    public static void Write(JpegImage image, System.IO.Stream stream)
    {
        CreateWriter().WriteToStream(image, stream);
    }

    /// <summary>Encodes an image to a writable stream using explicit options.</summary>
    /// <remarks>The method writes at the current position and leaves the stream open.</remarks>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    public static void Write(JpegImage image, System.IO.Stream stream, JpegWriteOptions options)
    {
        CreateWriter().WriteToStreamWithOptions(image, stream, options);
    }

    /// <summary>Rotates JPEG coefficients 90 degrees clockwise without re-encoding pixel values.</summary>
    /// <remarks>The image dimensions must satisfy the transformer's MCU-boundary requirements.</remarks>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated transformed JPEG stream.</returns>
    public static byte[] Rotate90(byte[] data)
    {
        JpegLosslessTransformer transformer = new JpegLosslessTransformer();
        return transformer.Rotate90(data);
    }

    /// <summary>Rotates JPEG coefficients 180 degrees without re-encoding pixel values.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated transformed JPEG stream.</returns>
    public static byte[] Rotate180(byte[] data)
    {
        JpegLosslessTransformer transformer = new JpegLosslessTransformer();
        return transformer.Rotate180(data);
    }

    /// <summary>Rotates JPEG coefficients 270 degrees clockwise without re-encoding pixel values.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated transformed JPEG stream.</returns>
    public static byte[] Rotate270(byte[] data)
    {
        JpegLosslessTransformer transformer = new JpegLosslessTransformer();
        return transformer.Rotate270(data);
    }

    /// <summary>Mirrors JPEG coefficients from left to right without re-encoding pixel values.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated transformed JPEG stream.</returns>
    public static byte[] FlipHorizontal(byte[] data)
    {
        JpegLosslessTransformer transformer = new JpegLosslessTransformer();
        return transformer.FlipHorizontal(data);
    }

    /// <summary>Mirrors JPEG coefficients from top to bottom without re-encoding pixel values.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated transformed JPEG stream.</returns>
    public static byte[] FlipVertical(byte[] data)
    {
        JpegLosslessTransformer transformer = new JpegLosslessTransformer();
        return transformer.FlipVertical(data);
    }

    /// <summary>Reflects JPEG coefficients across the top-left to bottom-right diagonal.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated transformed JPEG stream.</returns>
    public static byte[] Transpose(byte[] data)
    {
        JpegLosslessTransformer transformer = new JpegLosslessTransformer();
        return transformer.Transpose(data);
    }

    /// <summary>Reflects JPEG coefficients across the top-right to bottom-left diagonal.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated transformed JPEG stream.</returns>
    public static byte[] Transverse(byte[] data)
    {
        JpegLosslessTransformer transformer = new JpegLosslessTransformer();
        return transformer.Transverse(data);
    }

    /// <summary>Crops JPEG coefficients to an MCU-aligned rectangle without re-encoding pixel values.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="x">The left edge of the crop in pixels.</param>
    /// <param name="y">The top edge of the crop in pixels.</param>
    /// <param name="width">The crop width in pixels.</param>
    /// <param name="height">The crop height in pixels.</param>
    /// <returns>A newly allocated cropped JPEG stream.</returns>
    public static byte[] Crop(byte[] data, int x, int y, int width, int height)
    {
        JpegLosslessTransformer transformer = new JpegLosslessTransformer();
        return transformer.Crop(data, x, y, width, height);
    }

    /// <summary>Replaces all COM marker text while preserving encoded image data and unrelated metadata.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="comments">The replacement comments, in marker order.</param>
    /// <returns>A newly allocated JPEG stream containing the replacement comments.</returns>
    public static byte[] SetComments(byte[] data, System.Collections.Generic.List<string> comments)
    {
        JpegMetadataUpdater updater = new JpegMetadataUpdater();
        return updater.SetComments(data, comments);
    }

    /// <summary>Replaces the ICC profile, splitting it across APP2 segments when necessary.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="profile">The raw ICC profile bytes.</param>
    /// <returns>A newly allocated JPEG stream containing the replacement profile.</returns>
    public static byte[] SetIccProfile(byte[] data, byte[] profile)
    {
        JpegMetadataUpdater updater = new JpegMetadataUpdater();
        return updater.SetIccProfile(data, profile);
    }

    /// <summary>Replaces the Exif APP1 payload while preserving encoded image data and unrelated metadata.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="exifPayload">The raw Exif payload, including its Exif identifier.</param>
    /// <returns>A newly allocated JPEG stream containing the replacement Exif data.</returns>
    public static byte[] SetExif(byte[] data, byte[] exifPayload)
    {
        JpegMetadataUpdater updater = new JpegMetadataUpdater();
        return updater.SetExif(data, exifPayload);
    }

    /// <summary>Applies selected Exif convenience-field replacements while preserving encoded image data.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="metadata">Parsed Exif metadata whose selected fields contain replacement values.</param>
    /// <returns>A newly allocated JPEG stream containing the replaced Exif fields.</returns>
    public static byte[] ReplaceExifFields(byte[] data, JpegExifMetadata metadata)
    {
        JpegMetadataUpdater updater = new JpegMetadataUpdater();
        return updater.ReplaceExifFields(data, metadata);
    }

    /// <summary>Replaces the standard XMP APP1 payload while preserving encoded image data and unrelated metadata.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="xmpPayload">The raw XMP payload, including its namespace identifier.</param>
    /// <returns>A newly allocated JPEG stream containing the replacement XMP data.</returns>
    public static byte[] SetXmp(byte[] data, byte[] xmpPayload)
    {
        JpegMetadataUpdater updater = new JpegMetadataUpdater();
        return updater.SetXmp(data, xmpPayload);
    }

    internal static void ValidateFilePath(string path)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        if (path.Length == 0)
        {
            throw new System.ArgumentException("Path cannot be empty.", "path");
        }
    }

    /// <summary>Creates an image by copying a tightly packed pixel buffer.</summary>
    /// <remarks>The supported input formats are <see cref="JpegPixelFormat.L8"/>, <see cref="JpegPixelFormat.Rgb24"/>, and <see cref="JpegPixelFormat.YCbCr24"/>.</remarks>
    /// <param name="pixels">Packed pixel bytes in row-major order with no row padding.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="pixelFormat">The channel layout of <c>pixels</c>.</param>
    /// <returns>An image containing a private copy of the supplied pixels.</returns>
    public static JpegImage CreateImageFromPixels(byte[] pixels, int width, int height, JpegPixelFormat pixelFormat)
    {
        ValidatePixelByteArray(pixels, width, height, pixelFormat);

        JpegImage image = new JpegImage();
        image.Width = width;
        image.Height = height;
        image.Precision = 8;
        image.ComponentCount = GetPixelFormatComponentCount(pixelFormat);
        image.PixelFormat = pixelFormat;
        image.ColorSpace = GetPixelFormatColorSpace(pixelFormat);
        image.Subsampling = GetPixelFormatSubsampling(pixelFormat);
        image.Pixels = new byte[pixels.Length];
        System.Array.Copy(pixels, 0, image.Pixels, 0, pixels.Length);
        int bytesPerPixel = GetPixelFormatByteCount(pixelFormat);
        JpegInterleavedPixelBuffer buffer = new JpegInterleavedPixelBuffer();
        buffer.Width = width;
        buffer.Height = height;
        buffer.Precision = 8;
        buffer.ComponentCount = image.ComponentCount;
        buffer.PixelFormat = pixelFormat;
        buffer.SamplesPerPixel = image.ComponentCount;
        buffer.BytesPerPixel = bytesPerPixel;
        buffer.Stride = width * bytesPerPixel;
        buffer.SampleStorage = JpegSampleStorage.Byte;
        buffer.Ownership = JpegBufferOwnership.LibraryOwned;
        buffer.Pixels = image.Pixels;
        image.BufferLayout = JpegBufferLayout.Interleaved;
        image.BufferOwnership = JpegBufferOwnership.LibraryOwned;
        image.RowStride = buffer.Stride;
        image.InterleavedBuffer = buffer;
        return image;
    }

    internal static void ValidatePixelByteArray(byte[] pixels, int width, int height, JpegPixelFormat pixelFormat)
    {
        if (pixels == null)
        {
            throw new System.ArgumentNullException("pixels");
        }

        if (width <= 0)
        {
            throw JpegWriteValidationException.Create("Pixel image width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw JpegWriteValidationException.Create("Pixel image height must be greater than zero.");
        }

        int bytesPerPixel = GetPixelFormatByteCount(pixelFormat);
        long minimumLength = (long)width * (long)height * (long)bytesPerPixel;
        if (minimumLength > int.MaxValue)
        {
            throw JpegWriteValidationException.Create("Pixel byte array is too large.");
        }

        if (pixels.Length < (int)minimumLength)
        {
            throw JpegWriteValidationException.Create("Pixel byte array is too small.");
        }
    }

    internal static int GetPixelFormatByteCount(JpegPixelFormat pixelFormat)
    {
        if (pixelFormat == JpegPixelFormat.L8)
        {
            return 1;
        }

        if (pixelFormat == JpegPixelFormat.Rgb24 || pixelFormat == JpegPixelFormat.YCbCr24)
        {
            return 3;
        }

        throw JpegUnsupportedException.Create("Byte-array JPEG encoding currently supports L8, Rgb24, and YCbCr24 pixels.");
    }

    internal static int GetPixelFormatComponentCount(JpegPixelFormat pixelFormat)
    {
        if (pixelFormat == JpegPixelFormat.L8)
        {
            return 1;
        }

        if (pixelFormat == JpegPixelFormat.Rgb24 || pixelFormat == JpegPixelFormat.YCbCr24)
        {
            return 3;
        }

        throw JpegUnsupportedException.Create("Byte-array JPEG encoding currently supports L8, Rgb24, and YCbCr24 pixels.");
    }

    internal static JpegColorSpace GetPixelFormatColorSpace(JpegPixelFormat pixelFormat)
    {
        if (pixelFormat == JpegPixelFormat.L8)
        {
            return JpegColorSpace.Grayscale;
        }

        if (pixelFormat == JpegPixelFormat.Rgb24)
        {
            return JpegColorSpace.RGB;
        }

        if (pixelFormat == JpegPixelFormat.YCbCr24)
        {
            return JpegColorSpace.YCbCr;
        }

        throw JpegUnsupportedException.Create("Byte-array JPEG encoding currently supports L8, Rgb24, and YCbCr24 pixels.");
    }

    internal static JpegSubsampling GetPixelFormatSubsampling(JpegPixelFormat pixelFormat)
    {
        if (pixelFormat == JpegPixelFormat.L8)
        {
            return JpegSubsampling.Grayscale;
        }

        if (pixelFormat == JpegPixelFormat.Rgb24 || pixelFormat == JpegPixelFormat.YCbCr24)
        {
            return JpegSubsampling.Ratio444;
        }

        throw JpegUnsupportedException.Create("Byte-array JPEG encoding currently supports L8, Rgb24, and YCbCr24 pixels.");
    }
}
