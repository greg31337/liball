using System.Collections.Generic;

namespace jpeg;

/// <summary>Default service for decoding JPEG data into in-memory images.</summary>
internal class JpegImageReader : IJpegReader
{
    internal JpegBaselineSof0FullImageDecoder BaselineDecoder = new JpegBaselineSof0FullImageDecoder();
    internal JpegGrayscaleOutputConverter GrayscaleConverter = new JpegGrayscaleOutputConverter();
    internal IJpegChromaUpsampler ChromaUpsampler = new JpegFastBoxChromaUpsampler();
    internal IJpegChromaUpsampler FancyChromaUpsampler = new JpegFancyChromaUpsampler();
    internal JpegYCbCrToRgbConverter YCbCrConverter = new JpegYCbCrToRgbConverter();
    internal JpegYCbCrRawOutputConverter YCbCrRawConverter = new JpegYCbCrRawOutputConverter();
    internal JpegYCbCrRangeConverter YCbCrRangeConverter = new JpegYCbCrRangeConverter();
    internal JpegRgbRawConverter RgbRawConverter = new JpegRgbRawConverter();
    internal JpegCmykOutputConverter CmykOutputConverter = new JpegCmykOutputConverter();
    internal JpegCmykToRgbConverter CmykToRgbConverter = new JpegCmykToRgbConverter();
    internal JpegYcckToCmykConverter YcckToCmykConverter = new JpegYcckToCmykConverter();
    internal JpegYcckToRgbConverter YcckToRgbConverter = new JpegYcckToRgbConverter();
    internal JpegImageFactoryCore ImageFactory = new JpegImageFactoryCore();
    internal JpegRgbOutputPacker RgbPacker = new JpegRgbOutputPacker();
    internal JpegProgressiveSof2FullImageDecoder ProgressiveDecoder = new JpegProgressiveSof2FullImageDecoder();
    internal JpegExtendedSequentialSof1Decoder ExtendedSequentialDecoder = new JpegExtendedSequentialSof1Decoder();
    internal JpegLosslessSof3HuffmanDecoder LosslessHuffmanDecoder = new JpegLosslessSof3HuffmanDecoder();
    internal JpegLosslessSof11ArithmeticDecoder LosslessArithmeticDecoder = new JpegLosslessSof11ArithmeticDecoder();
    internal JpegIntegerPlanarAdapter IntegerPlanarAdapter = new JpegIntegerPlanarAdapter();
    internal JpegExifOrientationTransformer OrientationTransformer = new JpegExifOrientationTransformer();
    internal JpegIccPolicyEvaluator IccPolicyEvaluator = new JpegIccPolicyEvaluator();
    internal JpegReadGeometryResolver GeometryResolver = new JpegReadGeometryResolver();
    internal JpegReadGeometryProcessor GeometryProcessor = new JpegReadGeometryProcessor();

    /// <summary>Creates an independent JPEG image reader.</summary>
    internal JpegImageReader()
    {
    }

    /// <summary>Implements image-only abbreviated decoding with explicitly supplied tables.</summary>
    /// <param name="data">The complete image-only JPEG data.</param>
    /// <param name="tables">The tables required by the image.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadAbbreviatedImage(byte[] data, JpegTableSet tables)
    {
        return ReadAbbreviatedImageWithOptions(data, tables, new JpegReadOptions());
    }

    /// <summary>Implements image-only abbreviated decoding with explicitly supplied tables and options.</summary>
    /// <param name="data">The complete image-only JPEG data.</param>
    /// <param name="tables">The tables required by the image.</param>
    /// <param name="options">Options controlling decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadAbbreviatedImageWithOptions(byte[] data, JpegTableSet tables, JpegReadOptions options)
    {
        JpegReadOptions suppliedOptions = new JpegTableSetCodec().CreateReadOptions(options, tables);
        JpegInfo info = JpegIdentifier.Identify(data, suppliedOptions);
        if (!info.IsAbbreviatedImageStream)
        {
            throw JpegInvalidDataException.Create("The JPEG data is not an abbreviated image-only stream.");
        }

        return ReadWithOptions(data, suppliedOptions);
    }

    /// <summary>Implements byte-array decoding for <see cref="IJpegReader"/>.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage Read(byte[] data)
    {
        return ReadWithOptions(data, new JpegReadOptions());
    }

    /// <summary>Implements byte-array decoding with options for <see cref="IJpegReader"/>.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <param name="options">Options controlling parsing, pixel conversion, scaling, and limits.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadWithOptions(byte[] data, JpegReadOptions options)
    {
        return ReadWithOptionsAndCancellation(data, options, default(System.Threading.CancellationToken));
    }

    /// <summary>Implements cancellable byte-array decoding for <see cref="IJpegReader"/>.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadWithCancellation(byte[] data, System.Threading.CancellationToken cancellationToken)
    {
        return ReadWithOptionsAndCancellation(data, new JpegReadOptions(), cancellationToken);
    }

    /// <summary>Implements cancellable byte-array decoding with options for <see cref="IJpegReader"/>.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <param name="options">Options controlling parsing, pixel conversion, scaling, and limits.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadWithOptionsAndCancellation(byte[] data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        ValidateOptions(options);
        JpegInfo info = JpegIdentifier.IdentifyBytesWithOptionsAndCancellation(data, options, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateReadSupport(info);
        ApplyIccBehavior(info, options);
        JpegReadGeometry geometry = GeometryResolver.Resolve(info.Frame, options);
        JpegReadOptions resolvedOptions = GeometryResolver.CreateResolvedOptions(options, geometry);
        JpegTableStore tables = CreateTableStore(info);
        JpegPlanarImageBuffer planar = DecodeWholeImage(data, info, tables, resolvedOptions, geometry, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        JpegImage image = ConvertPlanarToImage(planar, info, resolvedOptions);
        image = GeometryProcessor.CropOutputImage(image, geometry);
        cancellationToken.ThrowIfCancellationRequested();
        ApplyInfo(image, info);
        GeometryProcessor.ApplyResultGeometry(image, geometry);
        if (options.AutoOrient)
        {
            image = OrientationTransformer.Apply(image);
        }

        return image;
    }

    internal JpegPlanarImageBuffer DecodeWholeImage(
        byte[] data,
        JpegInfo info,
        JpegTableStore tables,
        JpegReadOptions options,
        JpegReadGeometry geometry,
        System.Threading.CancellationToken cancellationToken)
    {
        if (info.Process == JpegProcess.ProgressiveDct)
        {
            List<JpegProgressiveScanData> scans = ExtractProgressiveScanData(data, options, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return ProgressiveDecoder.DecodeWithOptions(info.Frame, scans, tables, info.RestartInterval, options);
        }

        byte[] entropyData = ExtractSingleScanEntropyData(data, options);
        cancellationToken.ThrowIfCancellationRequested();
        if (info.Process == JpegProcess.BaselineDct)
        {
            return BaselineDecoder.DecodeWithOptions(info.Frame, info.LastScanHeader, tables, entropyData, info.RestartInterval, options);
        }

        ValidateNonBaselineGeometry(geometry);
        JpegPlanarIntImageBuffer integerPlanar;
        if (info.Process == JpegProcess.ExtendedSequentialDct)
        {
            integerPlanar = ExtendedSequentialDecoder.DecodeWithRestartInterval(info.Frame, info.LastScanHeader, tables, entropyData, info.RestartInterval);
        }
        else if (info.Process == JpegProcess.LosslessSequential)
        {
            integerPlanar = LosslessHuffmanDecoder.DecodeWithRestartInterval(info.Frame, info.LastScanHeader, tables, entropyData, info.RestartInterval);
        }
        else
        {
            integerPlanar = LosslessArithmeticDecoder.DecodeWithRestartInterval(info.Frame, info.LastScanHeader, tables, entropyData, info.RestartInterval);
        }

        return IntegerPlanarAdapter.Convert(integerPlanar, info.Frame, info.ColorSpace, info.Subsampling);
    }

    internal void ValidateNonBaselineGeometry(JpegReadGeometry geometry)
    {
        if (geometry.ScaleBlockSize != JpegConstants.BlockWidth)
        {
            throw JpegUnsupportedException.Create("Reduced IDCT scaling is supported only for DCT decoding paths that expose scaled inverse transforms.");
        }
    }

    internal void ApplyIccBehavior(JpegInfo info, JpegReadOptions options)
    {
        if (!info.Metadata.HasIccProfile)
        {
            return;
        }

        IccPolicyEvaluator.Apply(info.Metadata.IccProfile, options.IccBehaviorPolicy);
        if (options.IccBehaviorPolicy != JpegIccBehaviorPolicy.Ignore)
        {
            return;
        }

        info.Metadata.HasIccProfileChunks = false;
        info.Metadata.HasIccProfile = false;
        info.Metadata.HasCicp = false;
        info.Metadata.IccProfileChunks.Clear();
        info.Metadata.IccProfile = new byte[0];
        info.Metadata.Icc = new JpegIccProfile();
        info.Metadata.Cicp = new JpegCicpMetadata();
    }

    /// <summary>Implements span decoding for <see cref="IJpegReader"/>.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadSpan(System.ReadOnlySpan<byte> data)
    {
        return Read(data.ToArray());
    }

    /// <summary>Implements span decoding with options for <see cref="IJpegReader"/>.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <param name="options">Options controlling decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadSpanWithOptions(System.ReadOnlySpan<byte> data, JpegReadOptions options)
    {
        return ReadWithOptions(data.ToArray(), options);
    }

    /// <summary>Implements cancellable span decoding for <see cref="IJpegReader"/>.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadSpanWithCancellation(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ReadWithCancellation(data.ToArray(), cancellationToken);
    }

    /// <summary>Implements cancellable span decoding with options for <see cref="IJpegReader"/>.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <param name="options">Options controlling decoding.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        ValidateOptions(options);
        cancellationToken.ThrowIfCancellationRequested();
        return ReadWithOptionsAndCancellation(data.ToArray(), options, cancellationToken);
    }

    /// <summary>Implements file decoding for <see cref="IJpegReader"/>.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadFile(string path)
    {
        byte[] data = new JpegIo().ReadFile(path, default(System.Threading.CancellationToken));
        return Read(data);
    }

    /// <summary>Implements file decoding with options for <see cref="IJpegReader"/>.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="options">Options controlling decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadFileWithOptions(string path, JpegReadOptions options)
    {
        ValidateOptions(options);
        byte[] data = new JpegIo().ReadFile(path, default(System.Threading.CancellationToken));
        return ReadWithOptions(data, options);
    }

    /// <summary>Implements cancellable file decoding for <see cref="IJpegReader"/>.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadFileWithCancellation(string path, System.Threading.CancellationToken cancellationToken)
    {
        byte[] data = new JpegIo().ReadFile(path, cancellationToken);
        return ReadWithCancellation(data, cancellationToken);
    }

    /// <summary>Implements cancellable file decoding with options for <see cref="IJpegReader"/>.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="options">Options controlling decoding.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadFileWithOptionsAndCancellation(string path, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        ValidateOptions(options);
        byte[] data = new JpegIo().ReadFile(path, cancellationToken);
        return ReadWithOptionsAndCancellation(data, options, cancellationToken);
    }

    /// <summary>Implements stream decoding for <see cref="IJpegReader"/>.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadStream(System.IO.Stream stream)
    {
        return ReadStreamWithOptions(stream, new JpegReadOptions());
    }

    /// <summary>Implements stream decoding with options for <see cref="IJpegReader"/>.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <param name="options">Options controlling parsing, pixel conversion, scaling, and limits.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadStreamWithOptions(System.IO.Stream stream, JpegReadOptions options)
    {
        ValidateOptions(options);
        byte[] data = ReadAllBytes(stream);
        return ReadWithOptions(data, options);
    }

    /// <summary>Implements cancellable stream decoding for <see cref="IJpegReader"/>.</summary>
    /// <param name="stream">A readable stream positioned at the JPEG data.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadStreamWithCancellation(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)
    {
        return ReadStreamWithOptionsAndCancellation(stream, new JpegReadOptions(), cancellationToken);
    }

    /// <summary>Implements cancellable stream decoding with options for <see cref="IJpegReader"/>.</summary>
    /// <param name="stream">A readable stream positioned at the JPEG data.</param>
    /// <param name="options">Options controlling decoding.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadStreamWithOptionsAndCancellation(System.IO.Stream stream, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        ValidateOptions(options);
        byte[] data = ReadAllBytesWithCancellation(stream, cancellationToken);
        return ReadWithOptionsAndCancellation(data, options, cancellationToken);
    }

    /// <summary>Implements cancellable row callback decoding for <see cref="IJpegReader"/>.</summary>
    /// <param name="data">The complete JPEG data to decode.</param>
    /// <param name="options">Options controlling decoding.</param>
    /// <param name="callback">The callback invoked for each packed output row.</param>
    /// <param name="cancellationToken">The token checked around every callback.</param>
    /// <returns>The decoded image.</returns>
    public JpegImage ReadRowsWithOptionsAndCancellation(byte[] data, JpegReadOptions options, IJpegDecodedRowCallback callback, System.Threading.CancellationToken cancellationToken)
    {
        ValidateOptions(options);
        ValidateDecodedRowCallback(callback);
        cancellationToken.ThrowIfCancellationRequested();
        JpegImage image = ReadWithOptionsAndCancellation(data, options, cancellationToken);
        PublishDecodedRows(image, options, callback, cancellationToken);
        return image;
    }

    /// <summary>Implements cancellable progressive preview callbacks for <see cref="IJpegReader"/>.</summary>
    /// <param name="data">The complete progressive JPEG data to decode.</param>
    /// <param name="options">Options controlling decoding.</param>
    /// <param name="callback">The callback invoked after each progressive scan.</param>
    /// <param name="cancellationToken">The token checked around every scan and callback.</param>
    /// <returns>The final decoded image.</returns>
    public JpegImage ReadProgressivePreviewsWithOptionsAndCancellation(byte[] data, JpegReadOptions options, IJpegProgressivePreviewCallback callback, System.Threading.CancellationToken cancellationToken)
    {
        ValidateOptions(options);
        ValidateProgressivePreviewCallback(callback);
        cancellationToken.ThrowIfCancellationRequested();
        JpegInfo info = JpegIdentifier.IdentifyBytesWithOptionsAndCancellation(data, options, cancellationToken);
        ValidateProgressiveReadSupport(info);
        ApplyIccBehavior(info, options);
        JpegReadGeometry geometry = GeometryResolver.Resolve(info.Frame, options);
        JpegReadOptions resolvedOptions = GeometryResolver.CreateResolvedOptions(options, geometry);
        List<JpegProgressiveScanData> scans = ExtractProgressiveScanData(data, options, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return DecodeProgressivePreviews(info, scans, resolvedOptions, geometry, callback, cancellationToken);
    }

    internal void PublishDecodedRows(JpegImage image, JpegReadOptions options, IJpegDecodedRowCallback callback, System.Threading.CancellationToken cancellationToken)
    {
        int bytesPerPixel = image.InterleavedBuffer.BytesPerPixel;
        if (bytesPerPixel <= 0)
        {
            JpegInterleavedPixelBufferAllocator allocator = new JpegInterleavedPixelBufferAllocator();
            bytesPerPixel = allocator.GetBytesPerPixel(image.PixelFormat, image.ComponentCount);
        }

        long rowLength = (long)image.Width * (long)bytesPerPixel;
        if (rowLength > int.MaxValue)
        {
            throw JpegResourceLimitException.Create("Decoded callback row is too large.");
        }

        if (options.MaxCallbackOutputBytes >= 0 && rowLength > options.MaxCallbackOutputBytes)
        {
            throw JpegResourceLimitException.Create("Decoded callback row exceeds the configured maximum callback output bytes.");
        }

        int sampleCount = (int)rowLength;
        byte[] reusableSamples = new byte[sampleCount];
        JpegRowBuffer row = new JpegRowBuffer();
        row.RowCount = image.Height;
        row.Width = image.Width;
        row.ComponentCount = image.ComponentCount;
        row.PixelFormat = image.PixelFormat;
        row.ColorSpace = image.ColorSpace;
        row.SampleCount = sampleCount;
        row.Samples = reusableSamples;

        int rowIndex = 0;
        while (rowIndex < image.Height)
        {
            cancellationToken.ThrowIfCancellationRequested();
            row.RowIndex = rowIndex;
            row.RowCount = image.Height;
            row.Width = image.Width;
            row.ComponentCount = image.ComponentCount;
            row.PixelFormat = image.PixelFormat;
            row.ColorSpace = image.ColorSpace;
            row.SampleCount = sampleCount;
            row.Samples = reusableSamples;
            System.Array.Copy(image.Pixels, rowIndex * image.RowStride, reusableSamples, 0, sampleCount);
            callback.OnDecodedRow(row);
            cancellationToken.ThrowIfCancellationRequested();
            if (row.Samples != reusableSamples)
            {
                throw JpegInvalidDataException.Create("Decoded row callbacks cannot replace the reusable sample buffer.");
            }

            rowIndex = rowIndex + 1;
        }
    }

    internal JpegImage DecodeProgressivePreviews(
        JpegInfo info,
        List<JpegProgressiveScanData> scans,
        JpegReadOptions options,
        JpegReadGeometry geometry,
        IJpegProgressivePreviewCallback callback,
        System.Threading.CancellationToken cancellationToken)
    {
        JpegTableStore tables = CreateTableStore(info);
        ProgressiveDecoder.ValidateInputs(info.Frame, scans, tables, info.RestartInterval);
        JpegMcuLayout layout = ProgressiveDecoder.LayoutCalculator.Calculate(info.Frame);
        JpegProgressiveCoefficientBuffer coefficients = ProgressiveDecoder.AllocateCoefficientBufferWithLimit(layout, options.MaxProgressiveStateBytes);
        JpegProgressivePreview update = new JpegProgressivePreview();
        JpegImage result = new JpegImage();

        int scanIndex = 0;
        while (scanIndex < scans.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            JpegProgressiveScanData scan = scans[scanIndex];
            ProgressiveDecoder.DecodeScan(info.Frame, scan.Scan, tables, scan.EntropyData, info.RestartInterval, layout, coefficients);
            cancellationToken.ThrowIfCancellationRequested();
            JpegPlanarImageBuffer planar = ProgressiveDecoder.BuildPlanarImage(info.Frame, tables, coefficients, options);
            result = ConvertPlanarToImage(planar, info, options);
            result = GeometryProcessor.CropOutputImage(result, geometry);
            ApplyInfo(result, info);
            GeometryProcessor.ApplyResultGeometry(result, geometry);
            if (options.AutoOrient)
            {
                result = OrientationTransformer.Apply(result);
            }

            EnforceCallbackOutputLimit(result, options);
            update.ScanIndex = scanIndex;
            update.ScanCount = scans.Count;
            update.Completed = scanIndex == scans.Count - 1;
            update.Image = result;
            callback.OnProgressivePreview(update);
            cancellationToken.ThrowIfCancellationRequested();
            scanIndex = scanIndex + 1;
        }

        return result;
    }

    internal void EnforceCallbackOutputLimit(JpegImage image, JpegReadOptions options)
    {
        if (options.MaxCallbackOutputBytes < 0)
        {
            return;
        }

        long outputBytes = 0;
        if (image.BufferLayout == JpegBufferLayout.Planar)
        {
            int index = 0;
            while (index < image.PlanarBuffer.ComponentCount)
            {
                JpegPlanarComponentBuffer component = image.PlanarBuffer.Components[index];
                outputBytes = outputBytes + component.Samples.LongLength + component.IntSamples.LongLength * 4;
                index = index + 1;
            }
        }
        else
        {
            outputBytes = image.Pixels.LongLength + image.InterleavedBuffer.IntSamples.LongLength * 4;
        }

        if (outputBytes > options.MaxCallbackOutputBytes)
        {
            throw JpegResourceLimitException.Create("Progressive preview exceeds the configured maximum callback output bytes.");
        }
    }

    internal List<JpegProgressiveScanData> ExtractProgressiveScanData(
        byte[] data,
        JpegReadOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        JpegMarkerSegmentReader reader = new JpegMarkerSegmentReader(data);
        reader.MaxMarkerPayloadBytes = options.MaxMarkerPayloadBytes;
        reader.ReadStartOfImage();
        List<JpegProgressiveScanData> scans = new List<JpegProgressiveScanData>();
        JpegProgressiveScanData activeScan = new JpegProgressiveScanData();
        JpegByteWriter entropyWriter = new JpegByteWriter();
        bool inEntropyData = false;
        bool sawEndOfImage = false;
        long entropyStart = 0;

        while (!sawEndOfImage)
        {
            cancellationToken.ThrowIfCancellationRequested();
            JpegMarkerSegment segment;
            if (inEntropyData)
            {
                segment = reader.ReadEntropyMarkerSegment();
                CopyEntropyBytes(data, entropyStart, segment.MarkerOffset, entropyWriter, options);
                if (segment.Value >= JpegConstants.MarkerRestart0 && segment.Value <= JpegConstants.MarkerRestart7)
                {
                    entropyWriter.WriteUInt16(segment.Value);
                    EnforceEntropyLimit(entropyWriter.AbsoluteOffset, options);
                    entropyStart = segment.EndOffset;
                    continue;
                }

                if (segment.Value == JpegConstants.MarkerDefineNumberOfLines)
                {
                    entropyStart = segment.EndOffset;
                    continue;
                }

                activeScan.EntropyData = entropyWriter.ToArray();
                scans.Add(activeScan);
                inEntropyData = false;
            }
            else
            {
                segment = reader.ReadNextSegment();
            }

            if (segment.Value == JpegConstants.MarkerEndOfImage)
            {
                sawEndOfImage = true;
            }

            if (segment.Value == JpegConstants.MarkerStartOfScan)
            {
                activeScan = new JpegProgressiveScanData();
                activeScan.Scan = new JpegScanHeaderParser().Parse(segment.Payload);
                entropyWriter = new JpegByteWriter();
                entropyStart = segment.EndOffset;
                inEntropyData = true;
            }
        }

        return scans;
    }

    internal void ValidateProgressiveReadSupport(JpegInfo info)
    {
        if (!info.SawStartOfFrame || !info.SawStartOfScan || !info.SawEndOfImage)
        {
            throw JpegInvalidDataException.Create("Progressive JPEG data must contain SOF, SOS, and EOI markers.");
        }

        if (info.Process != JpegProcess.ProgressiveDct || info.ArithmeticCoding)
        {
            throw JpegUnsupportedException.Create("Progressive previews require Huffman-coded progressive SOF2 JPEG data.");
        }

        if (info.Precision != 8)
        {
            throw JpegUnsupportedException.Create("Progressive previews currently support 8-bit JPEG samples.");
        }

        if (info.ComponentCount != 1 && info.ComponentCount != 3)
        {
            throw JpegUnsupportedException.Create("Progressive previews currently support grayscale and three-component JPEG images.");
        }
    }

    internal void ValidateDecodedRowCallback(IJpegDecodedRowCallback callback)
    {
        if (callback == null)
        {
            throw new System.ArgumentNullException("callback");
        }
    }

    internal void ValidateProgressivePreviewCallback(IJpegProgressivePreviewCallback callback)
    {
        if (callback == null)
        {
            throw new System.ArgumentNullException("callback");
        }
    }

    internal byte[] ReadAllBytes(System.IO.Stream stream)
    {
        return new JpegIo().ReadStream(stream, default(System.Threading.CancellationToken));
    }

    internal byte[] ReadAllBytesWithCancellation(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegIo().ReadStream(stream, cancellationToken);
    }

    internal void ValidateOptions(JpegReadOptions options)
    {
        new JpegReadOptionsValidator().Validate(options);
    }

    internal void ValidateReadSupport(JpegInfo info)
    {
        if (info == null)
        {
            throw new System.ArgumentNullException("info");
        }

        string unsupportedMessage = GetUnsupportedDiagnosticMessage(info);
        if (unsupportedMessage.Length > 0)
        {
            throw JpegUnsupportedException.Create(unsupportedMessage);
        }

        if (!info.SawStartOfFrame || !info.SawStartOfScan || !info.SawEndOfImage)
        {
            throw JpegInvalidDataException.Create("JPEG image data must contain SOF, SOS, and EOI markers.");
        }

        if (info.Process == JpegProcess.ProgressiveDct)
        {
            ValidateProgressiveReadSupport(info);
            return;
        }

        if (info.Process != JpegProcess.BaselineDct
            && info.Process != JpegProcess.ExtendedSequentialDct
            && info.Process != JpegProcess.LosslessSequential
            && info.Process != JpegProcess.LosslessSequentialArithmetic)
        {
            throw JpegUnsupportedException.Create("The JPEG coding process is recognized but is not supported for whole-image decoding.");
        }

        if (info.ScanCount != 1)
        {
            throw JpegUnsupportedException.Create("Sequential and lossless whole-image decoding requires one scan.");
        }

        if (info.Process == JpegProcess.BaselineDct && info.Precision != 8)
        {
            throw JpegUnsupportedException.Create("Baseline JPEG decoding requires 8-bit samples.");
        }

        if (info.Process == JpegProcess.ExtendedSequentialDct && info.Precision != 12)
        {
            throw JpegUnsupportedException.Create("Extended sequential JPEG decoding requires 12-bit samples.");
        }

        if ((info.Process == JpegProcess.LosslessSequential || info.Process == JpegProcess.LosslessSequentialArithmetic)
            && (info.Precision < 2 || info.Precision > 16))
        {
            throw JpegUnsupportedException.Create("Lossless JPEG decoding requires sample precision between 2 and 16 bits.");
        }
    }

    internal string GetUnsupportedDiagnosticMessage(JpegInfo info)
    {
        foreach (JpegDiagnostic diagnostic in info.Diagnostics)
        {
            if (diagnostic.Code == JpegDiagnosticCode.UnsupportedProcess)
            {
                return diagnostic.Message;
            }
        }

        return "";
    }

    internal byte[] ExtractSingleScanEntropyData(byte[] data, JpegReadOptions options)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        JpegMarkerSegmentReader reader = new JpegMarkerSegmentReader(data);
        reader.MaxMarkerPayloadBytes = options.MaxMarkerPayloadBytes;
        reader.ReadStartOfImage();

        JpegByteWriter entropyWriter = new JpegByteWriter();
        bool inEntropyData = false;
        long entropyStart = 0;
        int scanCount = 0;
        bool sawEndOfImage = false;
        while (!sawEndOfImage)
        {
            JpegMarkerSegment segment;
            if (inEntropyData)
            {
                segment = reader.ReadEntropyMarkerSegment();
                CopyEntropyBytes(data, entropyStart, segment.MarkerOffset, entropyWriter, options);
            }
            else
            {
                segment = reader.ReadNextSegment();
            }

            if (segment.Value == JpegConstants.MarkerEndOfImage)
            {
                sawEndOfImage = true;
            }

            if (inEntropyData && segment.Value >= JpegConstants.MarkerRestart0 && segment.Value <= JpegConstants.MarkerRestart7)
            {
                entropyWriter.WriteUInt16(segment.Value);
                EnforceEntropyLimit(entropyWriter.AbsoluteOffset, options);
                entropyStart = segment.EndOffset;
                continue;
            }

            if (inEntropyData && segment.Value == JpegConstants.MarkerDefineNumberOfLines)
            {
                entropyStart = segment.EndOffset;
                continue;
            }

            inEntropyData = false;
            if (segment.Value == JpegConstants.MarkerStartOfScan)
            {
                scanCount = scanCount + 1;
                if (scanCount > 1)
                {
                    throw JpegUnsupportedException.Create("Jpeg.Read currently supports a single full-image baseline scan.");
                }

                entropyStart = segment.EndOffset;
                inEntropyData = true;
            }
        }

        return entropyWriter.ToArray();
    }

    internal void CopyEntropyBytes(byte[] data, long startOffset, long endOffset, JpegByteWriter writer, JpegReadOptions options)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (startOffset < 0 || endOffset < startOffset || endOffset > data.Length)
        {
            throw JpegInvalidDataException.Create("JPEG entropy byte range is invalid.");
        }

        int index = (int)startOffset;
        int end = (int)endOffset;
        while (index < end)
        {
            writer.WriteByte(data[index]);
            index = index + 1;
        }

        EnforceEntropyLimit(writer.AbsoluteOffset, options);
    }

    internal void EnforceEntropyLimit(long entropyBytes, JpegReadOptions options)
    {
        if (options.MaxEntropyBytes >= 0 && entropyBytes > options.MaxEntropyBytes)
        {
            throw JpegResourceLimitException.Create("JPEG entropy bytes exceed the configured maximum entropy bytes.");
        }
    }

    internal JpegTableStore CreateTableStore(JpegInfo info)
    {
        JpegTableStore store = new JpegTableStore();
        store.QuantizationTables = info.QuantizationTables;
        store.HuffmanTables = info.HuffmanTables;
        store.ArithmeticConditioningTables = info.ArithmeticConditioningTables;
        return store;
    }

    internal JpegImage ConvertPlanarToImage(JpegPlanarImageBuffer planar, JpegInfo info, JpegReadOptions options)
    {
        planar.ColorSpace = info.ColorSpace;
        planar.Subsampling = info.Subsampling;
        if (planar.Precision > 8)
        {
            if (options.OutputPixelFormat != JpegPixelFormat.Unknown
                && options.OutputPixelFormat != JpegPixelFormat.RawComponents)
            {
                throw JpegUnsupportedException.Create("Samples wider than eight bits are returned as raw planar components.");
            }

            return ImageFactory.CreateImageFromPlanarBuffer(planar);
        }

        JpegPixelFormat outputPixelFormat = ResolveOutputPixelFormat(info, options);
        if (options.RawComponentOutput || outputPixelFormat == JpegPixelFormat.RawComponents)
        {
            return ImageFactory.CreateImageFromPlanarBuffer(planar);
        }

        if (info.ColorSpace == JpegColorSpace.Unknown)
        {
            if (options.UnknownColorHandling == JpegUnknownColorHandling.Exact || options.ColorPolicy == JpegColorPolicy.FailIfUnsupported)
            {
                throw JpegUnsupportedException.Create("The JPEG color interpretation is unknown and exact color handling was requested.");
            }

            return ImageFactory.CreateImageFromPlanarBuffer(planar);
        }

        if (info.ColorSpace == JpegColorSpace.Grayscale)
        {
            return ConvertGrayscalePlanarToImage(planar, outputPixelFormat);
        }

        if (info.ColorSpace == JpegColorSpace.YCbCr)
        {
            return ConvertYCbCrPlanarToImage(planar, info, options, outputPixelFormat);
        }

        if (info.ColorSpace == JpegColorSpace.RGB)
        {
            return ConvertRgbPlanarToImage(planar, options, outputPixelFormat);
        }

        if (info.ColorSpace == JpegColorSpace.CMYK)
        {
            return ConvertCmykPlanarToImage(planar, info, options, outputPixelFormat);
        }

        if (info.ColorSpace == JpegColorSpace.YCCK)
        {
            return ConvertYcckPlanarToImage(planar, options, outputPixelFormat);
        }

        throw JpegUnsupportedException.Create("The JPEG color interpretation is not supported.");
    }

    internal JpegPixelFormat ResolveOutputPixelFormat(JpegInfo info, JpegReadOptions options)
    {
        if (options.OutputPixelFormat != JpegPixelFormat.Unknown)
        {
            return options.OutputPixelFormat;
        }

        if (info.ComponentCount == 1)
        {
            return JpegPixelFormat.L8;
        }

        if (info.ColorSpace == JpegColorSpace.RGB)
        {
            return JpegPixelFormat.Rgb24;
        }

        if (info.ColorSpace == JpegColorSpace.YCbCr)
        {
            if (options.ColorPolicy == JpegColorPolicy.ConvertToRgb)
            {
                return JpegPixelFormat.Rgb24;
            }

            return JpegPixelFormat.YCbCr24;
        }

        if (info.ColorSpace == JpegColorSpace.CMYK || info.ColorSpace == JpegColorSpace.YCCK)
        {
            if (options.ColorPolicy == JpegColorPolicy.ConvertToRgb)
            {
                return JpegPixelFormat.Rgb24;
            }

            return JpegPixelFormat.Cmyk32;
        }

        if (options.RawComponentOutput)
        {
            return JpegPixelFormat.RawComponents;
        }

        return JpegPixelFormat.Unknown;
    }

    internal JpegImage ConvertGrayscalePlanarToImage(JpegPlanarImageBuffer planar, JpegPixelFormat outputPixelFormat)
    {
        if (outputPixelFormat != JpegPixelFormat.L8)
        {
            throw JpegUnsupportedException.Create("Jpeg.Read currently supports L8 output for grayscale JPEG images.");
        }

        return GrayscaleConverter.ConvertPlanarToImage(planar);
    }

    internal JpegImage ConvertYCbCrPlanarToImage(JpegPlanarImageBuffer planar, JpegInfo info, JpegReadOptions options, JpegPixelFormat outputPixelFormat)
    {
        JpegPlanarImageBuffer ranged = ApplyYCbCrRange(planar, options);
        JpegPlanarImageBuffer fullResolution = UpsampleChromaWithOptions(ranged, options);
        if (outputPixelFormat == JpegPixelFormat.YCbCr24)
        {
            JpegImage rawImage = YCbCrRawConverter.ConvertPlanarToImage(fullResolution);
            rawImage.Subsampling = info.Subsampling;
            return rawImage;
        }

        if (outputPixelFormat == JpegPixelFormat.Rgb24)
        {
            return YCbCrConverter.ConvertPlanarToImage(fullResolution);
        }

        if (outputPixelFormat == JpegPixelFormat.Bgr24 || outputPixelFormat == JpegPixelFormat.Rgba32 || outputPixelFormat == JpegPixelFormat.Bgra32)
        {
            JpegImage rgb = YCbCrConverter.ConvertPlanarToImage(fullResolution);
            return RgbPacker.PackImageToImage(rgb, outputPixelFormat);
        }

        throw JpegUnsupportedException.Create("Jpeg.Read currently supports RGB, BGR, RGBA, BGRA, and YCbCr output for three-component JPEG images.");
    }

    internal JpegImage ConvertRgbPlanarToImage(JpegPlanarImageBuffer planar, JpegReadOptions options, JpegPixelFormat outputPixelFormat)
    {
        JpegPlanarImageBuffer fullResolution = UpsampleChromaWithOptions(planar, options);
        if (outputPixelFormat == JpegPixelFormat.Rgb24)
        {
            return RgbRawConverter.ConvertPlanarToImage(fullResolution);
        }

        if (outputPixelFormat == JpegPixelFormat.Bgr24 || outputPixelFormat == JpegPixelFormat.Rgba32 || outputPixelFormat == JpegPixelFormat.Bgra32)
        {
            JpegImage rgb = RgbRawConverter.ConvertPlanarToImage(fullResolution);
            return RgbPacker.PackImageToImage(rgb, outputPixelFormat);
        }

        throw JpegUnsupportedException.Create("RGB JPEG images support RGB, BGR, RGBA, and BGRA output.");
    }

    internal JpegImage ConvertCmykPlanarToImage(JpegPlanarImageBuffer planar, JpegInfo info, JpegReadOptions options, JpegPixelFormat outputPixelFormat)
    {
        JpegPlanarImageBuffer fullResolution = UpsampleChromaWithOptions(planar, options);
        bool invert = ResolveAdobeInversion(info, options);
        JpegImage cmyk = CmykOutputConverter.ConvertPlanarToImageWithInversion(fullResolution, invert);
        if (outputPixelFormat == JpegPixelFormat.Cmyk32)
        {
            return cmyk;
        }

        if (outputPixelFormat == JpegPixelFormat.Rgb24 || outputPixelFormat == JpegPixelFormat.Bgr24 || outputPixelFormat == JpegPixelFormat.Rgba32 || outputPixelFormat == JpegPixelFormat.Bgra32)
        {
            JpegImage rgb = CmykToRgbConverter.ConvertImageToRgbImage(cmyk);
            return PackRgbOutput(rgb, outputPixelFormat);
        }

        throw JpegUnsupportedException.Create("CMYK JPEG images support CMYK, RGB, BGR, RGBA, and BGRA output.");
    }

    internal JpegImage ConvertYcckPlanarToImage(JpegPlanarImageBuffer planar, JpegReadOptions options, JpegPixelFormat outputPixelFormat)
    {
        JpegPlanarImageBuffer ranged = ApplyYCbCrRange(planar, options);
        JpegPlanarImageBuffer fullResolution = UpsampleChromaWithOptions(ranged, options);
        if (outputPixelFormat == JpegPixelFormat.Cmyk32)
        {
            return YcckToCmykConverter.ConvertPlanarToImage(fullResolution);
        }

        if (outputPixelFormat == JpegPixelFormat.Rgb24 || outputPixelFormat == JpegPixelFormat.Bgr24 || outputPixelFormat == JpegPixelFormat.Rgba32 || outputPixelFormat == JpegPixelFormat.Bgra32)
        {
            JpegImage rgb = YcckToRgbConverter.ConvertPlanarToImage(fullResolution);
            return PackRgbOutput(rgb, outputPixelFormat);
        }

        throw JpegUnsupportedException.Create("YCCK JPEG images support CMYK, RGB, BGR, RGBA, and BGRA output.");
    }

    internal JpegImage PackRgbOutput(JpegImage rgb, JpegPixelFormat outputPixelFormat)
    {
        if (outputPixelFormat == JpegPixelFormat.Rgb24)
        {
            return rgb;
        }

        return RgbPacker.PackImageToImage(rgb, outputPixelFormat);
    }

    internal bool ResolveAdobeInversion(JpegInfo info, JpegReadOptions options)
    {
        if (options.AdobeInversionMode == JpegAdobeInversionMode.ForceInverted)
        {
            return true;
        }

        if (options.AdobeInversionMode == JpegAdobeInversionMode.ForceNonInverted)
        {
            return false;
        }

        return info.AdobeInvertsCmyk;
    }

    internal JpegPlanarImageBuffer ApplyYCbCrRange(JpegPlanarImageBuffer planar, JpegReadOptions options)
    {
        if (options.YCbCrRange == JpegYCbCrRange.Limited)
        {
            return YCbCrRangeConverter.ExpandLimitedRange(planar);
        }

        return planar;
    }

    internal JpegPlanarImageBuffer UpsampleChroma(JpegPlanarImageBuffer planar)
    {
        return UpsampleComponents(planar, ChromaUpsampler);
    }

    internal JpegPlanarImageBuffer UpsampleChromaWithOptions(JpegPlanarImageBuffer planar, JpegReadOptions options)
    {
        if (options.ChromaUpsamplingMode == JpegChromaUpsamplingMode.Fancy)
        {
            return UpsampleComponents(planar, FancyChromaUpsampler);
        }

        return UpsampleComponents(planar, ChromaUpsampler);
    }

    internal JpegPlanarImageBuffer UpsampleComponents(JpegPlanarImageBuffer planar, IJpegChromaUpsampler upsampler)
    {
        if (planar.ComponentCount < 3)
        {
            return planar;
        }

        JpegPlanarImageBuffer result = new JpegPlanarImageBuffer();
        result.Width = planar.Width;
        result.Height = planar.Height;
        result.Precision = planar.Precision;
        result.ComponentCount = planar.ComponentCount;
        result.ColorSpace = planar.ColorSpace;
        result.Subsampling = planar.Subsampling;
        result.Ownership = planar.Ownership;
        result.Components.Add(planar.Components[0]);

        int index = 1;
        while (index < planar.ComponentCount)
        {
            JpegPlanarComponentBuffer component = planar.Components[index];
            if (component.Width == planar.Width && component.Height == planar.Height)
            {
                result.Components.Add(component);
            }
            else
            {
                result.Components.Add(upsampler.UpsampleComponent(component, planar.Width, planar.Height));
            }

            index = index + 1;
        }

        return result;
    }

    internal void ApplyInfo(JpegImage image, JpegInfo info)
    {
        image.Precision = info.Precision;
        image.Metadata = info.Metadata;
        image.QuantizationTables = info.QuantizationTables;
        image.ComponentQuantizationTableIdentifiers = new List<int>();
        foreach (JpegFrameComponent component in info.Components)
        {
            image.ComponentQuantizationTableIdentifiers.Add(component.QuantizationTableIdentifier);
        }

        image.Diagnostics = info.Diagnostics;
        image.DiagnosticCollection = info.DiagnosticCollection;
        if (image.BufferLayout == JpegBufferLayout.Planar)
        {
            image.BufferOwnership = image.PlanarBuffer.Ownership;
            image.RowStride = 0;
            return;
        }

        JpegInterleavedPixelBufferAllocator allocator = new JpegInterleavedPixelBufferAllocator();
        int bytesPerPixel = allocator.GetBytesPerPixel(image.PixelFormat, image.ComponentCount);
        JpegInterleavedPixelBuffer buffer = new JpegInterleavedPixelBuffer();
        buffer.Width = image.Width;
        buffer.Height = image.Height;
        buffer.Precision = image.Precision;
        buffer.ComponentCount = image.ComponentCount;
        buffer.PixelFormat = image.PixelFormat;
        buffer.SamplesPerPixel = image.ComponentCount;
        buffer.BytesPerPixel = bytesPerPixel;
        buffer.Stride = allocator.GetStride(image.Width, bytesPerPixel);
        buffer.SampleStorage = JpegSampleStorage.Byte;
        buffer.Ownership = JpegBufferOwnership.LibraryOwned;
        buffer.Pixels = image.Pixels;
        image.BufferLayout = JpegBufferLayout.Interleaved;
        image.BufferOwnership = JpegBufferOwnership.LibraryOwned;
        image.RowStride = buffer.Stride;
        image.InterleavedBuffer = buffer;
    }
}
