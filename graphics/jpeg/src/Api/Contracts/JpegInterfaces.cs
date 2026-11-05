namespace jpeg;

/// <summary>Identifies JPEG data from byte arrays and streams without decoding pixels.</summary>
/// <remarks>Implementations read streams from their current position and leave them open.</remarks>
public interface IJpegIdentifier
{
    /// <summary>Reads JPEG structure and metadata without decoding pixels.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    JpegInfo Identify(byte[] data);

    /// <summary>Reads JPEG structure and metadata using explicit limits and parsing policies.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <param name="options">Limits and policies used while parsing the data.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    JpegInfo IdentifyWithOptions(byte[] data, JpegReadOptions options);

    /// <summary>Reads JPEG structure and metadata while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    JpegInfo IdentifyWithCancellation(byte[] data, System.Threading.CancellationToken cancellationToken);

    /// <summary>Reads JPEG structure and metadata using explicit options while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <param name="options">Limits and policies used while parsing the data.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    JpegInfo IdentifyWithOptionsAndCancellation(byte[] data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Reads JPEG structure and metadata from a read-only span without decoding pixels.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    JpegInfo IdentifySpan(System.ReadOnlySpan<byte> data);

    /// <summary>Reads JPEG structure and metadata from a read-only span using explicit options.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <param name="options">Limits and policies used while parsing the data.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    JpegInfo IdentifySpanWithOptions(System.ReadOnlySpan<byte> data, JpegReadOptions options);

    /// <summary>Reads JPEG structure and metadata from a read-only span while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    JpegInfo IdentifySpanWithCancellation(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);

    /// <summary>Reads JPEG structure and metadata from a read-only span using explicit options while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <param name="options">Limits and policies used while parsing the data.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    JpegInfo IdentifySpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Reads JPEG structure and metadata from a file without decoding pixels.</summary>
    /// <param name="path">The path of the JPEG file to inspect.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    JpegInfo IdentifyFile(string path);

    /// <summary>Reads JPEG structure and metadata from a file using explicit options.</summary>
    /// <param name="path">The path of the JPEG file to inspect.</param>
    /// <param name="options">Limits and policies used while parsing the file.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    JpegInfo IdentifyFileWithOptions(string path, JpegReadOptions options);

    /// <summary>Reads JPEG structure and metadata from a file while observing cancellation.</summary>
    /// <param name="path">The path of the JPEG file to inspect.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    JpegInfo IdentifyFileWithCancellation(string path, System.Threading.CancellationToken cancellationToken);

    /// <summary>Reads JPEG structure and metadata from a file using explicit options while observing cancellation.</summary>
    /// <param name="path">The path of the JPEG file to inspect.</param>
    /// <param name="options">Limits and policies used while parsing the file.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    JpegInfo IdentifyFileWithOptionsAndCancellation(string path, JpegReadOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Reads JPEG structure and metadata from a stream without decoding pixels.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    JpegInfo IdentifyStream(System.IO.Stream stream);

    /// <summary>Reads JPEG structure and metadata from a stream using explicit limits and parsing policies.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <param name="options">Limits and policies used while parsing the stream.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    JpegInfo IdentifyStreamWithOptions(System.IO.Stream stream, JpegReadOptions options);

    /// <summary>Reads JPEG structure and metadata from a stream while observing cancellation.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    JpegInfo IdentifyStreamWithCancellation(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken);

    /// <summary>Reads JPEG structure and metadata from a stream using explicit options while observing cancellation.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <param name="options">Limits and policies used while parsing the stream.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    JpegInfo IdentifyStreamWithOptionsAndCancellation(System.IO.Stream stream, JpegReadOptions options, System.Threading.CancellationToken cancellationToken);
}

/// <summary>Decodes JPEG data from byte arrays and streams.</summary>
/// <remarks>Implementations read streams from their current position and leave them open.</remarks>
public interface IJpegReader
{
    /// <summary>Decodes an abbreviated image-only JPEG byte array using explicitly supplied tables.</summary>
    /// <param name="data">The complete image-only JPEG data.</param>
    /// <param name="tables">The quantization and entropy tables required by the image.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadAbbreviatedImage(byte[] data, JpegTableSet tables);

    /// <summary>Decodes an abbreviated image-only JPEG byte array using explicitly supplied tables and options.</summary>
    /// <param name="data">The complete image-only JPEG data.</param>
    /// <param name="tables">The quantization and entropy tables required by the image.</param>
    /// <param name="options">Options controlling parsing, pixel conversion, scaling, and limits.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadAbbreviatedImageWithOptions(byte[] data, JpegTableSet tables, JpegReadOptions options);

    /// <summary>Decodes a JPEG byte array into an in-memory pixel image.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage Read(byte[] data);

    /// <summary>Decodes a JPEG byte array using explicit output, metadata, and resource policies.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <param name="options">Options controlling parsing, pixel conversion, scaling, and limits.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadWithOptions(byte[] data, JpegReadOptions options);

    /// <summary>Decodes a JPEG byte array while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadWithCancellation(byte[] data, System.Threading.CancellationToken cancellationToken);

    /// <summary>Decodes a JPEG byte array using explicit options while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <param name="options">Options controlling parsing, pixel conversion, scaling, and limits.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadWithOptionsAndCancellation(byte[] data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Decodes JPEG data from a read-only span.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadSpan(System.ReadOnlySpan<byte> data);

    /// <summary>Decodes JPEG data from a read-only span using explicit options.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <param name="options">Options controlling parsing, pixel conversion, scaling, and limits.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadSpanWithOptions(System.ReadOnlySpan<byte> data, JpegReadOptions options);

    /// <summary>Decodes JPEG data from a read-only span while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadSpanWithCancellation(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);

    /// <summary>Decodes JPEG data from a read-only span using explicit options while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <param name="options">Options controlling parsing, pixel conversion, scaling, and limits.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Decodes JPEG data from a file.</summary>
    /// <param name="path">The path of the JPEG file to decode.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadFile(string path);

    /// <summary>Decodes JPEG data from a file using explicit options.</summary>
    /// <param name="path">The path of the JPEG file to decode.</param>
    /// <param name="options">Options controlling parsing, pixel conversion, scaling, and limits.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadFileWithOptions(string path, JpegReadOptions options);

    /// <summary>Decodes JPEG data from a file while observing cancellation.</summary>
    /// <param name="path">The path of the JPEG file to decode.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadFileWithCancellation(string path, System.Threading.CancellationToken cancellationToken);

    /// <summary>Decodes JPEG data from a file using explicit options while observing cancellation.</summary>
    /// <param name="path">The path of the JPEG file to decode.</param>
    /// <param name="options">Options controlling parsing, pixel conversion, scaling, and limits.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadFileWithOptionsAndCancellation(string path, JpegReadOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Decodes JPEG data from a stream into an in-memory pixel image.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadStream(System.IO.Stream stream);

    /// <summary>Decodes JPEG data from a stream using explicit output, metadata, and resource policies.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <param name="options">Options controlling parsing, pixel conversion, scaling, and limits.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadStreamWithOptions(System.IO.Stream stream, JpegReadOptions options);

    /// <summary>Decodes JPEG data from a stream while observing cancellation.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadStreamWithCancellation(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken);

    /// <summary>Decodes JPEG data from a stream using explicit options while observing cancellation.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <param name="options">Options controlling parsing, pixel conversion, scaling, and limits.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    JpegImage ReadStreamWithOptionsAndCancellation(System.IO.Stream stream, JpegReadOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Decodes an image and publishes its packed pixels one row at a time through one reusable buffer.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <param name="options">Options controlling decoding and resource limits.</param>
    /// <param name="callback">The callback invoked once for every decoded output row.</param>
    /// <param name="cancellationToken">The token checked before and after every callback.</param>
    /// <returns>The decoded image also represented by the row callbacks.</returns>
    JpegImage ReadRowsWithOptionsAndCancellation(byte[] data, JpegReadOptions options, IJpegDecodedRowCallback callback, System.Threading.CancellationToken cancellationToken);

    /// <summary>Decodes a progressive JPEG and publishes a pixel preview after every scan.</summary>
    /// <param name="data">The complete progressive JPEG data to decode.</param>
    /// <param name="options">Options controlling decoding and resource limits.</param>
    /// <param name="callback">The callback invoked after each completed scan.</param>
    /// <param name="cancellationToken">The token checked before and after every scan and callback.</param>
    /// <returns>The final decoded image.</returns>
    JpegImage ReadProgressivePreviewsWithOptionsAndCancellation(byte[] data, JpegReadOptions options, IJpegProgressivePreviewCallback callback, System.Threading.CancellationToken cancellationToken);
}

/// <summary>Encodes in-memory images to JPEG byte arrays and streams.</summary>
/// <remarks>Implementations write streams at their current position and leave them open.</remarks>
public interface IJpegWriter
{
    /// <summary>Encodes an image-only abbreviated JPEG byte array using explicitly supplied tables.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="tables">The quantization and Huffman tables used by the encoder.</param>
    /// <returns>A newly allocated image-only JPEG byte array.</returns>
    byte[] WriteAbbreviatedImage(JpegImage image, JpegTableSet tables);

    /// <summary>Encodes an image-only abbreviated JPEG byte array using explicitly supplied tables and options.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="tables">The quantization and Huffman tables used by the encoder.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <returns>A newly allocated image-only JPEG byte array.</returns>
    byte[] WriteAbbreviatedImageWithOptions(JpegImage image, JpegTableSet tables, JpegWriteOptions options);

    /// <summary>Encodes an image as JPEG data using default write options.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    byte[] Write(JpegImage image);

    /// <summary>Encodes an image as JPEG data using explicit write options.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    byte[] WriteWithOptions(JpegImage image, JpegWriteOptions options);

    /// <summary>Encodes an image and reports the concrete settings selected after preset and automatic-option resolution.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <returns>The encoded bytes and resolved settings.</returns>
    JpegWriteResult WriteWithResult(JpegImage image, JpegWriteOptions options);

    /// <summary>Encodes an image while observing cancellation.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    byte[] WriteWithCancellation(JpegImage image, System.Threading.CancellationToken cancellationToken);

    /// <summary>Encodes an image using explicit options while observing cancellation.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    byte[] WriteWithOptionsAndCancellation(JpegImage image, JpegWriteOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Encodes an image into a caller-provided span.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="destination">The destination that receives the complete JPEG data.</param>
    /// <returns>The number of bytes written to <c>destination</c>.</returns>
    int WriteToSpan(JpegImage image, System.Span<byte> destination);

    /// <summary>Encodes an image into a caller-provided span using explicit options.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="destination">The destination that receives the complete JPEG data.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <returns>The number of bytes written to <c>destination</c>.</returns>
    int WriteToSpanWithOptions(JpegImage image, System.Span<byte> destination, JpegWriteOptions options);

    /// <summary>Encodes an image into a caller-provided span while observing cancellation.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="destination">The destination that receives the complete JPEG data.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    /// <returns>The number of bytes written to <c>destination</c>.</returns>
    int WriteToSpanWithCancellation(JpegImage image, System.Span<byte> destination, System.Threading.CancellationToken cancellationToken);

    /// <summary>Encodes an image into a caller-provided span using explicit options while observing cancellation.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="destination">The destination that receives the complete JPEG data.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    /// <returns>The number of bytes written to <c>destination</c>.</returns>
    int WriteToSpanWithOptionsAndCancellation(JpegImage image, System.Span<byte> destination, JpegWriteOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Encodes an image to a file using default options.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="path">The destination file path.</param>
    void WriteFile(JpegImage image, string path);

    /// <summary>Encodes an image to a file using explicit options.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    void WriteFileWithOptions(JpegImage image, string path, JpegWriteOptions options);

    /// <summary>Encodes an image to a file while observing cancellation.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    void WriteFileWithCancellation(JpegImage image, string path, System.Threading.CancellationToken cancellationToken);

    /// <summary>Encodes an image to a file using explicit options while observing cancellation.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    void WriteFileWithOptionsAndCancellation(JpegImage image, string path, JpegWriteOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Encodes an image to a writable stream using default write options.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    void WriteToStream(JpegImage image, System.IO.Stream stream);

    /// <summary>Encodes an image to a writable stream using explicit write options.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    void WriteToStreamWithOptions(JpegImage image, System.IO.Stream stream, JpegWriteOptions options);

    /// <summary>Encodes an image to a writable stream while observing cancellation.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    void WriteToStreamWithCancellation(JpegImage image, System.IO.Stream stream, System.Threading.CancellationToken cancellationToken);

    /// <summary>Encodes an image to a writable stream using explicit options while observing cancellation.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    void WriteToStreamWithOptionsAndCancellation(JpegImage image, System.IO.Stream stream, JpegWriteOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Requests tightly packed source rows through one reusable buffer and encodes them as JPEG data.</summary>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="pixelFormat">The packed source pixel layout.</param>
    /// <param name="colorSpace">The interpreted source color space.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <param name="callback">The callback that fills each requested source row.</param>
    /// <param name="cancellationToken">The token checked before and after every callback.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    byte[] WriteRowsWithOptionsAndCancellation(int width, int height, JpegPixelFormat pixelFormat, JpegColorSpace colorSpace, JpegWriteOptions options, IJpegEncodeRowCallback callback, System.Threading.CancellationToken cancellationToken);
}
