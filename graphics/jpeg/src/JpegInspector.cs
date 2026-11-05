using System.Collections.Generic;

namespace jpeg;

/// <summary>Provides advanced access to JPEG marker segments and quantized DCT coefficients.</summary>
/// <remarks>Coefficient operations avoid pixel reconstruction and are intended for inspection or lossless coefficient edits.</remarks>
internal static class JpegInspector
{
    /// <summary>Reads every marker segment from a JPEG byte array in stream order.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <returns>Marker descriptions with copied payloads and source offsets.</returns>
    public static JpegMarkerSegment[] ReadMarkers(byte[] data)
    {
        JpegMarkerSegmentReader reader = new JpegMarkerSegmentReader(data);
        return ReadAllMarkers(reader);
    }

    /// <summary>Reads every marker segment from a stream in stream order.</summary>
    /// <remarks>The method reads from the current position and leaves the stream open.</remarks>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <returns>Marker descriptions with copied payloads and source offsets.</returns>
    public static JpegMarkerSegment[] ReadMarkers(System.IO.Stream stream)
    {
        byte[] data = new JpegIo().ReadStream(stream, default(System.Threading.CancellationToken));
        return ReadMarkers(data);
    }

    /// <summary>Reads every marker segment from a JPEG file in stream order.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <returns>Marker descriptions with copied payloads and source offsets.</returns>
    public static JpegMarkerSegment[] ReadMarkersFile(string path)
    {
        byte[] data = new JpegIo().ReadFile(path, default(System.Threading.CancellationToken));
        return ReadMarkers(data);
    }

    /// <summary>Decodes quantized DCT coefficients from a JPEG byte array without reconstructing pixels.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <returns>A mutable coefficient image that retains the data required for rewriting.</returns>
    public static JpegCoefficientImage ReadCoefficients(byte[] data)
    {
        JpegCoefficientReader reader = new JpegCoefficientReader();
        return reader.Read(data);
    }

    /// <summary>Decodes quantized DCT coefficients using explicit parsing and resource options.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Options controlling parsing, metadata, and limits.</param>
    /// <returns>A mutable coefficient image that retains the data required for rewriting.</returns>
    public static JpegCoefficientImage ReadCoefficients(byte[] data, JpegReadOptions options)
    {
        JpegCoefficientReader reader = new JpegCoefficientReader();
        return reader.ReadWithOptions(data, options);
    }

    /// <summary>Decodes quantized DCT coefficients from a stream without reconstructing pixels.</summary>
    /// <remarks>The method reads from the current position and leaves the stream open.</remarks>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <returns>A mutable coefficient image that retains the data required for rewriting.</returns>
    public static JpegCoefficientImage ReadCoefficients(System.IO.Stream stream)
    {
        JpegCoefficientReader reader = new JpegCoefficientReader();
        return reader.ReadStream(stream);
    }

    /// <summary>Decodes quantized DCT coefficients from a stream using explicit options.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <param name="options">Options controlling parsing, metadata, and limits.</param>
    /// <returns>A mutable coefficient image that retains the data required for rewriting.</returns>
    public static JpegCoefficientImage ReadCoefficients(System.IO.Stream stream, JpegReadOptions options)
    {
        JpegCoefficientReader reader = new JpegCoefficientReader();
        return reader.ReadStreamWithOptions(stream, options);
    }

    /// <summary>Decodes quantized DCT coefficients from a JPEG file.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <returns>A mutable coefficient image that retains the data required for rewriting.</returns>
    public static JpegCoefficientImage ReadCoefficientsFile(string path)
    {
        byte[] data = new JpegIo().ReadFile(path, default(System.Threading.CancellationToken));
        return ReadCoefficients(data);
    }

    /// <summary>Decodes quantized DCT coefficients from a JPEG file using explicit options.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="options">Options controlling parsing, metadata, and limits.</param>
    /// <returns>A mutable coefficient image that retains the data required for rewriting.</returns>
    public static JpegCoefficientImage ReadCoefficientsFile(string path, JpegReadOptions options)
    {
        byte[] data = new JpegIo().ReadFile(path, default(System.Threading.CancellationToken));
        return ReadCoefficients(data, options);
    }

    /// <summary>Writes a coefficient image as a complete JPEG stream without re-quantizing its values.</summary>
    /// <param name="image">A coefficient image previously read by this inspector.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    public static byte[] WriteCoefficients(JpegCoefficientImage image)
    {
        JpegCoefficientWriter writer = new JpegCoefficientWriter();
        return writer.Write(image);
    }

    /// <summary>Writes a coefficient image to a stream without re-quantizing its values.</summary>
    /// <remarks>The method writes at the current position and leaves the stream open.</remarks>
    /// <param name="image">A coefficient image previously read by this inspector.</param>
    /// <param name="stream">The writable destination stream.</param>
    public static void WriteCoefficients(JpegCoefficientImage image, System.IO.Stream stream)
    {
        JpegCoefficientWriter writer = new JpegCoefficientWriter();
        writer.WriteToStream(image, stream);
    }

    /// <summary>Writes a coefficient image to a JPEG file without re-quantizing its values.</summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="image">A coefficient image previously read by this inspector.</param>
    public static void WriteCoefficientsFile(string path, JpegCoefficientImage image)
    {
        JpegCoefficientWriter writer = new JpegCoefficientWriter();
        writer.WriteFile(path, image);
    }

    internal static JpegMarkerSegment[] ReadAllMarkers(JpegMarkerSegmentReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        List<JpegMarkerSegment> markers = new List<JpegMarkerSegment>();
        JpegMarkerSegment soi = reader.ReadStartOfImage();
        markers.Add(soi);

        bool inEntropyData = false;
        bool sawEndOfImage = false;
        while (!sawEndOfImage)
        {
            JpegMarkerSegment segment;
            if (inEntropyData)
            {
                segment = reader.ReadEntropyMarkerSegment();
            }
            else
            {
                segment = reader.ReadNextSegment();
            }

            markers.Add(segment);
            if (segment.Value == JpegConstants.MarkerEndOfImage)
            {
                sawEndOfImage = true;
                continue;
            }

            if (segment.Value == JpegConstants.MarkerStartOfScan)
            {
                inEntropyData = true;
                continue;
            }

            if (segment.Value >= JpegConstants.MarkerRestart0 && segment.Value <= JpegConstants.MarkerRestart7)
            {
                if (inEntropyData)
                {
                    inEntropyData = true;
                }
                else
                {
                    inEntropyData = false;
                }

                continue;
            }

            if (segment.Value == JpegConstants.MarkerDefineNumberOfLines && segment.FoundAfterEntropyData)
            {
                inEntropyData = true;
                continue;
            }

            inEntropyData = false;
        }

        return markers.ToArray();
    }
}
