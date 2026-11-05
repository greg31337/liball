namespace jpeg;

/// <summary>Transforms JPEG coefficient data without re-encoding pixel values.</summary>
public interface IJpegTransformer
{
    /// <summary>Rotates JPEG coefficients 90 degrees clockwise.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated transformed JPEG stream.</returns>
    byte[] Rotate90(byte[] data);

    /// <summary>Rotates JPEG coefficients 180 degrees.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated transformed JPEG stream.</returns>
    byte[] Rotate180(byte[] data);

    /// <summary>Rotates JPEG coefficients 270 degrees clockwise.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated transformed JPEG stream.</returns>
    byte[] Rotate270(byte[] data);

    /// <summary>Mirrors JPEG coefficients from left to right.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated transformed JPEG stream.</returns>
    byte[] FlipHorizontal(byte[] data);

    /// <summary>Mirrors JPEG coefficients from top to bottom.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated transformed JPEG stream.</returns>
    byte[] FlipVertical(byte[] data);

    /// <summary>Reflects JPEG coefficients across the top-left to bottom-right diagonal.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated transformed JPEG stream.</returns>
    byte[] Transpose(byte[] data);

    /// <summary>Reflects JPEG coefficients across the top-right to bottom-left diagonal.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated transformed JPEG stream.</returns>
    byte[] Transverse(byte[] data);

    /// <summary>Crops JPEG coefficients to an MCU-aligned rectangle.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="x">The left edge of the crop in pixels.</param>
    /// <param name="y">The top edge of the crop in pixels.</param>
    /// <param name="width">The crop width in pixels.</param>
    /// <param name="height">The crop height in pixels.</param>
    /// <returns>A newly allocated cropped JPEG stream.</returns>
    byte[] Crop(byte[] data, int x, int y, int width, int height);

    /// <summary>Rotates bytes 90 degrees with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed JPEG data.</returns>
    byte[] Rotate90WithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Rotates a span 90 degrees with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed JPEG data.</returns>
    byte[] Rotate90SpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Rotates a stream 90 degrees with options and cancellation.</summary>
    /// <param name="input">Source stream.</param><param name="output">Destination stream.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void Rotate90StreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Rotates a file 90 degrees with options and cancellation.</summary>
    /// <param name="inputPath">Source path.</param><param name="outputPath">Destination path.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void Rotate90FileWithOptionsAndCancellation(string inputPath, string outputPath, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Rotates bytes 180 degrees with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed JPEG data.</returns>
    byte[] Rotate180WithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Rotates a span 180 degrees with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed JPEG data.</returns>
    byte[] Rotate180SpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Rotates a stream 180 degrees with options and cancellation.</summary>
    /// <param name="input">Source stream.</param><param name="output">Destination stream.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void Rotate180StreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Rotates a file 180 degrees with options and cancellation.</summary>
    /// <param name="inputPath">Source path.</param><param name="outputPath">Destination path.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void Rotate180FileWithOptionsAndCancellation(string inputPath, string outputPath, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Rotates bytes 270 degrees with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed JPEG data.</returns>
    byte[] Rotate270WithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Rotates a span 270 degrees with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed JPEG data.</returns>
    byte[] Rotate270SpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Rotates a stream 270 degrees with options and cancellation.</summary>
    /// <param name="input">Source stream.</param><param name="output">Destination stream.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void Rotate270StreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Rotates a file 270 degrees with options and cancellation.</summary>
    /// <param name="inputPath">Source path.</param><param name="outputPath">Destination path.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void Rotate270FileWithOptionsAndCancellation(string inputPath, string outputPath, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Flips bytes horizontally with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed JPEG data.</returns>
    byte[] FlipHorizontalWithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Flips a span horizontally with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed JPEG data.</returns>
    byte[] FlipHorizontalSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Flips a stream horizontally with options and cancellation.</summary>
    /// <param name="input">Source stream.</param><param name="output">Destination stream.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void FlipHorizontalStreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Flips a file horizontally with options and cancellation.</summary>
    /// <param name="inputPath">Source path.</param><param name="outputPath">Destination path.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void FlipHorizontalFileWithOptionsAndCancellation(string inputPath, string outputPath, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Flips bytes vertically with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed JPEG data.</returns>
    byte[] FlipVerticalWithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Flips a span vertically with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed JPEG data.</returns>
    byte[] FlipVerticalSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Flips a stream vertically with options and cancellation.</summary>
    /// <param name="input">Source stream.</param><param name="output">Destination stream.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void FlipVerticalStreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Flips a file vertically with options and cancellation.</summary>
    /// <param name="inputPath">Source path.</param><param name="outputPath">Destination path.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void FlipVerticalFileWithOptionsAndCancellation(string inputPath, string outputPath, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Transposes bytes with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed JPEG data.</returns>
    byte[] TransposeWithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Transposes a span with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed JPEG data.</returns>
    byte[] TransposeSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Transposes a stream with options and cancellation.</summary>
    /// <param name="input">Source stream.</param><param name="output">Destination stream.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void TransposeStreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Transposes a file with options and cancellation.</summary>
    /// <param name="inputPath">Source path.</param><param name="outputPath">Destination path.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void TransposeFileWithOptionsAndCancellation(string inputPath, string outputPath, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Transverses bytes with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed JPEG data.</returns>
    byte[] TransverseWithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Transverses a span with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed JPEG data.</returns>
    byte[] TransverseSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Transverses a stream with options and cancellation.</summary>
    /// <param name="input">Source stream.</param><param name="output">Destination stream.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void TransverseStreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Transverses a file with options and cancellation.</summary>
    /// <param name="inputPath">Source path.</param><param name="outputPath">Destination path.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void TransverseFileWithOptionsAndCancellation(string inputPath, string outputPath, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Crops bytes with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="x">Crop X.</param><param name="y">Crop Y.</param><param name="width">Crop width.</param><param name="height">Crop height.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed JPEG data.</returns>
    byte[] CropWithOptionsAndCancellation(byte[] data, int x, int y, int width, int height, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Crops a span with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="x">Crop X.</param><param name="y">Crop Y.</param><param name="width">Crop width.</param><param name="height">Crop height.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed JPEG data.</returns>
    byte[] CropSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, int x, int y, int width, int height, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Crops a stream with options and cancellation.</summary>
    /// <param name="input">Source stream.</param><param name="output">Destination stream.</param><param name="x">Crop X.</param><param name="y">Crop Y.</param><param name="width">Crop width.</param><param name="height">Crop height.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void CropStreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, int x, int y, int width, int height, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Crops a file with options and cancellation.</summary>
    /// <param name="inputPath">Source path.</param><param name="outputPath">Destination path.</param><param name="x">Crop X.</param><param name="y">Crop Y.</param><param name="width">Crop width.</param><param name="height">Crop height.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void CropFileWithOptionsAndCancellation(string inputPath, string outputPath, int x, int y, int width, int height, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Converts YCbCr JPEG data to grayscale by removing chroma coefficients.</summary>
    /// <param name="data">Source JPEG data.</param>
    /// <returns>Grayscale JPEG data.</returns>
    byte[] ConvertToGrayscale(byte[] data);
    /// <summary>Converts bytes to grayscale with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Grayscale JPEG data.</returns>
    byte[] ConvertToGrayscaleWithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Converts a span to grayscale with options and cancellation.</summary>
    /// <param name="data">Source JPEG data.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    /// <returns>Grayscale JPEG data.</returns>
    byte[] ConvertToGrayscaleSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Converts a stream to grayscale with options and cancellation.</summary>
    /// <param name="input">Source stream.</param><param name="output">Destination stream.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void ConvertToGrayscaleStreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
    /// <summary>Converts a file to grayscale with options and cancellation.</summary>
    /// <param name="inputPath">Source path.</param><param name="outputPath">Destination path.</param><param name="options">Transform options.</param><param name="cancellationToken">Cancellation token.</param>
    void ConvertToGrayscaleFileWithOptionsAndCancellation(string inputPath, string outputPath, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken);
}
