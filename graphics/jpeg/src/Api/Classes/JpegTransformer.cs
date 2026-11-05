namespace jpeg;

/// <summary>Default service for lossless JPEG coefficient transforms.</summary>
public class JpegTransformer : IJpegTransformer
{
    /// <summary>Creates an independent JPEG transformer.</summary>
    public JpegTransformer()
    {
    }

    /// <summary>Rotates JPEG coefficients 90 degrees clockwise.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A transformed JPEG byte array.</returns>
    public byte[] Rotate90(byte[] data)
    {
        return Jpeg.Rotate90(data);
    }

    /// <summary>Rotates JPEG coefficients 180 degrees.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A transformed JPEG byte array.</returns>
    public byte[] Rotate180(byte[] data)
    {
        return Jpeg.Rotate180(data);
    }

    /// <summary>Rotates JPEG coefficients 270 degrees clockwise.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A transformed JPEG byte array.</returns>
    public byte[] Rotate270(byte[] data)
    {
        return Jpeg.Rotate270(data);
    }

    /// <summary>Mirrors JPEG coefficients from left to right.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A transformed JPEG byte array.</returns>
    public byte[] FlipHorizontal(byte[] data)
    {
        return Jpeg.FlipHorizontal(data);
    }

    /// <summary>Mirrors JPEG coefficients from top to bottom.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A transformed JPEG byte array.</returns>
    public byte[] FlipVertical(byte[] data)
    {
        return Jpeg.FlipVertical(data);
    }

    /// <summary>Reflects JPEG coefficients across the top-left to bottom-right diagonal.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A transformed JPEG byte array.</returns>
    public byte[] Transpose(byte[] data)
    {
        return Jpeg.Transpose(data);
    }

    /// <summary>Reflects JPEG coefficients across the top-right to bottom-left diagonal.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A transformed JPEG byte array.</returns>
    public byte[] Transverse(byte[] data)
    {
        return Jpeg.Transverse(data);
    }

    /// <summary>Crops JPEG coefficients to an MCU-aligned rectangle.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="x">The left edge of the crop in pixels.</param>
    /// <param name="y">The top edge of the crop in pixels.</param>
    /// <param name="width">The crop width in pixels.</param>
    /// <param name="height">The crop height in pixels.</param>
    /// <returns>A cropped JPEG byte array.</returns>
    public byte[] Crop(byte[] data, int x, int y, int width, int height)
    {
        return Jpeg.Crop(data, x, y, width, height);
    }

    /// <summary>Rotates bytes 90 degrees with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Transformed data.</returns>
    public byte[] Rotate90WithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegLosslessTransformer().Rotate90WithOptionsAndCancellation(data, options, cancellationToken);
    }

    /// <summary>Rotates a span 90 degrees with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Transformed data.</returns>
    public byte[] Rotate90SpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return Rotate90WithOptionsAndCancellation(data.ToArray(), options, cancellationToken);
    }

    /// <summary>Rotates a stream 90 degrees with options and cancellation.</summary>
    /// <param name="input">Input.</param><param name="output">Output.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void Rotate90StreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteStreamResult(output, Rotate90WithOptionsAndCancellation(ReadStreamInputWithCancellation(input, cancellationToken), options, cancellationToken), cancellationToken);
    }

    /// <summary>Rotates a file 90 degrees with options and cancellation.</summary>
    /// <param name="inputPath">Input path.</param><param name="outputPath">Output path.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void Rotate90FileWithOptionsAndCancellation(string inputPath, string outputPath, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteFileResult(outputPath, Rotate90WithOptionsAndCancellation(ReadFileInputWithCancellation(inputPath, cancellationToken), options, cancellationToken), cancellationToken);
    }

    /// <summary>Rotates bytes 180 degrees with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Transformed data.</returns>
    public byte[] Rotate180WithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegLosslessTransformer().Rotate180WithOptionsAndCancellation(data, options, cancellationToken);
    }

    /// <summary>Rotates a span 180 degrees with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Transformed data.</returns>
    public byte[] Rotate180SpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return Rotate180WithOptionsAndCancellation(data.ToArray(), options, cancellationToken);
    }

    /// <summary>Rotates a stream 180 degrees with options and cancellation.</summary>
    /// <param name="input">Input.</param><param name="output">Output.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void Rotate180StreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteStreamResult(output, Rotate180WithOptionsAndCancellation(ReadStreamInputWithCancellation(input, cancellationToken), options, cancellationToken), cancellationToken);
    }

    /// <summary>Rotates a file 180 degrees with options and cancellation.</summary>
    /// <param name="inputPath">Input path.</param><param name="outputPath">Output path.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void Rotate180FileWithOptionsAndCancellation(string inputPath, string outputPath, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteFileResult(outputPath, Rotate180WithOptionsAndCancellation(ReadFileInputWithCancellation(inputPath, cancellationToken), options, cancellationToken), cancellationToken);
    }

    /// <summary>Rotates bytes 270 degrees with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Transformed data.</returns>
    public byte[] Rotate270WithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegLosslessTransformer().Rotate270WithOptionsAndCancellation(data, options, cancellationToken);
    }

    /// <summary>Rotates a span 270 degrees with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Transformed data.</returns>
    public byte[] Rotate270SpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return Rotate270WithOptionsAndCancellation(data.ToArray(), options, cancellationToken);
    }

    /// <summary>Rotates a stream 270 degrees with options and cancellation.</summary>
    /// <param name="input">Input.</param><param name="output">Output.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void Rotate270StreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteStreamResult(output, Rotate270WithOptionsAndCancellation(ReadStreamInputWithCancellation(input, cancellationToken), options, cancellationToken), cancellationToken);
    }

    /// <summary>Rotates a file 270 degrees with options and cancellation.</summary>
    /// <param name="inputPath">Input path.</param><param name="outputPath">Output path.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void Rotate270FileWithOptionsAndCancellation(string inputPath, string outputPath, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteFileResult(outputPath, Rotate270WithOptionsAndCancellation(ReadFileInputWithCancellation(inputPath, cancellationToken), options, cancellationToken), cancellationToken);
    }

    /// <summary>Flips bytes horizontally with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Transformed data.</returns>
    public byte[] FlipHorizontalWithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegLosslessTransformer().FlipHorizontalWithOptionsAndCancellation(data, options, cancellationToken);
    }

    /// <summary>Flips a span horizontally with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Transformed data.</returns>
    public byte[] FlipHorizontalSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return FlipHorizontalWithOptionsAndCancellation(data.ToArray(), options, cancellationToken);
    }

    /// <summary>Flips a stream horizontally with options and cancellation.</summary>
    /// <param name="input">Input.</param><param name="output">Output.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void FlipHorizontalStreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteStreamResult(output, FlipHorizontalWithOptionsAndCancellation(ReadStreamInputWithCancellation(input, cancellationToken), options, cancellationToken), cancellationToken);
    }

    /// <summary>Flips a file horizontally with options and cancellation.</summary>
    /// <param name="inputPath">Input path.</param><param name="outputPath">Output path.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void FlipHorizontalFileWithOptionsAndCancellation(string inputPath, string outputPath, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteFileResult(outputPath, FlipHorizontalWithOptionsAndCancellation(ReadFileInputWithCancellation(inputPath, cancellationToken), options, cancellationToken), cancellationToken);
    }

    /// <summary>Flips bytes vertically with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Transformed data.</returns>
    public byte[] FlipVerticalWithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegLosslessTransformer().FlipVerticalWithOptionsAndCancellation(data, options, cancellationToken);
    }

    /// <summary>Flips a span vertically with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Transformed data.</returns>
    public byte[] FlipVerticalSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return FlipVerticalWithOptionsAndCancellation(data.ToArray(), options, cancellationToken);
    }

    /// <summary>Flips a stream vertically with options and cancellation.</summary>
    /// <param name="input">Input.</param><param name="output">Output.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void FlipVerticalStreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteStreamResult(output, FlipVerticalWithOptionsAndCancellation(ReadStreamInputWithCancellation(input, cancellationToken), options, cancellationToken), cancellationToken);
    }

    /// <summary>Flips a file vertically with options and cancellation.</summary>
    /// <param name="inputPath">Input path.</param><param name="outputPath">Output path.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void FlipVerticalFileWithOptionsAndCancellation(string inputPath, string outputPath, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteFileResult(outputPath, FlipVerticalWithOptionsAndCancellation(ReadFileInputWithCancellation(inputPath, cancellationToken), options, cancellationToken), cancellationToken);
    }

    /// <summary>Transposes bytes with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Transformed data.</returns>
    public byte[] TransposeWithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegLosslessTransformer().TransposeWithOptionsAndCancellation(data, options, cancellationToken);
    }

    /// <summary>Transposes a span with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Transformed data.</returns>
    public byte[] TransposeSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return TransposeWithOptionsAndCancellation(data.ToArray(), options, cancellationToken);
    }

    /// <summary>Transposes a stream with options and cancellation.</summary>
    /// <param name="input">Input.</param><param name="output">Output.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void TransposeStreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteStreamResult(output, TransposeWithOptionsAndCancellation(ReadStreamInputWithCancellation(input, cancellationToken), options, cancellationToken), cancellationToken);
    }

    /// <summary>Transposes a file with options and cancellation.</summary>
    /// <param name="inputPath">Input path.</param><param name="outputPath">Output path.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void TransposeFileWithOptionsAndCancellation(string inputPath, string outputPath, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteFileResult(outputPath, TransposeWithOptionsAndCancellation(ReadFileInputWithCancellation(inputPath, cancellationToken), options, cancellationToken), cancellationToken);
    }

    /// <summary>Transverses bytes with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Transformed data.</returns>
    public byte[] TransverseWithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegLosslessTransformer().TransverseWithOptionsAndCancellation(data, options, cancellationToken);
    }

    /// <summary>Transverses a span with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Transformed data.</returns>
    public byte[] TransverseSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return TransverseWithOptionsAndCancellation(data.ToArray(), options, cancellationToken);
    }

    /// <summary>Transverses a stream with options and cancellation.</summary>
    /// <param name="input">Input.</param><param name="output">Output.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void TransverseStreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteStreamResult(output, TransverseWithOptionsAndCancellation(ReadStreamInputWithCancellation(input, cancellationToken), options, cancellationToken), cancellationToken);
    }

    /// <summary>Transverses a file with options and cancellation.</summary>
    /// <param name="inputPath">Input path.</param><param name="outputPath">Output path.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void TransverseFileWithOptionsAndCancellation(string inputPath, string outputPath, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteFileResult(outputPath, TransverseWithOptionsAndCancellation(ReadFileInputWithCancellation(inputPath, cancellationToken), options, cancellationToken), cancellationToken);
    }

    /// <summary>Crops bytes with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="x">Crop X.</param><param name="y">Crop Y.</param><param name="width">Crop width.</param><param name="height">Crop height.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Transformed data.</returns>
    public byte[] CropWithOptionsAndCancellation(byte[] data, int x, int y, int width, int height, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegLosslessTransformer().CropWithOptionsAndCancellation(data, x, y, width, height, options, cancellationToken);
    }

    /// <summary>Crops a span with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="x">Crop X.</param><param name="y">Crop Y.</param><param name="width">Crop width.</param><param name="height">Crop height.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Transformed data.</returns>
    public byte[] CropSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, int x, int y, int width, int height, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return CropWithOptionsAndCancellation(data.ToArray(), x, y, width, height, options, cancellationToken);
    }

    /// <summary>Crops a stream with options and cancellation.</summary>
    /// <param name="input">Input.</param><param name="output">Output.</param><param name="x">Crop X.</param><param name="y">Crop Y.</param><param name="width">Crop width.</param><param name="height">Crop height.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void CropStreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, int x, int y, int width, int height, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteStreamResult(output, CropWithOptionsAndCancellation(ReadStreamInputWithCancellation(input, cancellationToken), x, y, width, height, options, cancellationToken), cancellationToken);
    }

    /// <summary>Crops a file with options and cancellation.</summary>
    /// <param name="inputPath">Input path.</param><param name="outputPath">Output path.</param><param name="x">Crop X.</param><param name="y">Crop Y.</param><param name="width">Crop width.</param><param name="height">Crop height.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void CropFileWithOptionsAndCancellation(string inputPath, string outputPath, int x, int y, int width, int height, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteFileResult(outputPath, CropWithOptionsAndCancellation(ReadFileInputWithCancellation(inputPath, cancellationToken), x, y, width, height, options, cancellationToken), cancellationToken);
    }

    /// <summary>Converts YCbCr JPEG data to grayscale by removing chroma coefficients.</summary>
    /// <param name="data">Source data.</param><returns>Grayscale data.</returns>
    public byte[] ConvertToGrayscale(byte[] data)
    {
        return new JpegLosslessTransformer().ConvertToGrayscale(data);
    }

    /// <summary>Converts bytes to grayscale with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Grayscale data.</returns>
    public byte[] ConvertToGrayscaleWithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegLosslessTransformer().ConvertToGrayscaleWithOptionsAndCancellation(data, options, cancellationToken);
    }

    /// <summary>Converts a span to grayscale with options and cancellation.</summary>
    /// <param name="data">Source data.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param><returns>Grayscale data.</returns>
    public byte[] ConvertToGrayscaleSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return ConvertToGrayscaleWithOptionsAndCancellation(data.ToArray(), options, cancellationToken);
    }

    /// <summary>Converts a stream to grayscale with options and cancellation.</summary>
    /// <param name="input">Input.</param><param name="output">Output.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void ConvertToGrayscaleStreamWithOptionsAndCancellation(System.IO.Stream input, System.IO.Stream output, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteStreamResult(output, ConvertToGrayscaleWithOptionsAndCancellation(ReadStreamInputWithCancellation(input, cancellationToken), options, cancellationToken), cancellationToken);
    }

    /// <summary>Converts a file to grayscale with options and cancellation.</summary>
    /// <param name="inputPath">Input path.</param><param name="outputPath">Output path.</param><param name="options">Options.</param><param name="cancellationToken">Cancellation token.</param>
    public void ConvertToGrayscaleFileWithOptionsAndCancellation(string inputPath, string outputPath, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        WriteFileResult(outputPath, ConvertToGrayscaleWithOptionsAndCancellation(ReadFileInputWithCancellation(inputPath, cancellationToken), options, cancellationToken), cancellationToken);
    }

    internal byte[] ReadStreamInputWithCancellation(System.IO.Stream input, System.Threading.CancellationToken cancellationToken)
    {
        if (input == null)
        {
            throw new System.ArgumentNullException("input");
        }
        if (!input.CanRead)
        {
            throw new System.ArgumentException("Stream must be readable.", "input");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new JpegIo().ReadStream(input, cancellationToken);
    }

    internal void WriteStreamResult(System.IO.Stream output, byte[] data, System.Threading.CancellationToken cancellationToken)
    {
        if (output == null)
        {
            throw new System.ArgumentNullException("output");
        }
        if (!output.CanWrite)
        {
            throw new System.ArgumentException("Stream must be writable.", "output");
        }

        cancellationToken.ThrowIfCancellationRequested();
        new JpegIo().WriteStream(output, data, cancellationToken);
    }

    internal byte[] ReadFileInputWithCancellation(string path, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegIo().ReadFile(path, cancellationToken);
    }

    internal void WriteFileResult(string path, byte[] data, System.Threading.CancellationToken cancellationToken)
    {
        new JpegIo().WriteFile(path, data, cancellationToken);
    }
}
