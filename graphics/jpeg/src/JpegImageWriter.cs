using System.Collections.Generic;

namespace jpeg;

/// <summary>Default service for encoding in-memory images as JPEG data.</summary>
internal class JpegImageWriter : IJpegWriter
{
    internal JpegBaselineSof0Encoder BaselineEncoder = new JpegBaselineSof0Encoder();
    internal JpegDefaultQuantizationTables DefaultQuantizationTables = new JpegDefaultQuantizationTables();
    internal JpegHuffmanTableGenerator HuffmanTableGenerator = new JpegHuffmanTableGenerator();
    internal JpegRgbToYCbCrConverter RgbToYCbCrConverter = new JpegRgbToYCbCrConverter();
    internal JpegEncodingOptionsResolver OptionsResolver = new JpegEncodingOptionsResolver();
    internal JpegProgressiveSof2Encoder ProgressiveEncoder = new JpegProgressiveSof2Encoder();
    internal JpegExtendedSequentialSof1Encoder ExtendedSequentialEncoder = new JpegExtendedSequentialSof1Encoder();
    internal JpegLosslessSof3HuffmanEncoder LosslessHuffmanEncoder = new JpegLosslessSof3HuffmanEncoder();
    internal JpegLosslessSof11ArithmeticEncoder LosslessArithmeticEncoder = new JpegLosslessSof11ArithmeticEncoder();
    internal JpegIntegerImageSource IntegerImageSource = new JpegIntegerImageSource();
    internal JpegEncoderComponentConfigurator ComponentConfigurator = new JpegEncoderComponentConfigurator();
    internal JpegSequentialDctScanEncoder SequentialScanEncoder = new JpegSequentialDctScanEncoder();
    internal JpegEncoderTableConfigurator TableConfigurator = new JpegEncoderTableConfigurator();
    internal JpegRestartIntervalResolver RestartIntervalResolver = new JpegRestartIntervalResolver();
    internal JpegEncoderPresetResolver PresetResolver = new JpegEncoderPresetResolver();

    /// <summary>Creates an independent JPEG image writer.</summary>
    internal JpegImageWriter()
    {
    }

    /// <summary>Implements image-only abbreviated encoding with explicitly supplied tables.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="tables">The quantization and Huffman tables used by the encoder.</param>
    /// <returns>A newly allocated image-only JPEG byte array.</returns>
    public byte[] WriteAbbreviatedImage(JpegImage image, JpegTableSet tables)
    {
        return WriteAbbreviatedImageWithOptions(image, tables, new JpegWriteOptions());
    }

    /// <summary>Implements image-only abbreviated encoding with explicitly supplied tables and options.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="tables">The quantization and Huffman tables used by the encoder.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <returns>A newly allocated image-only JPEG byte array.</returns>
    public byte[] WriteAbbreviatedImageWithOptions(JpegImage image, JpegTableSet tables, JpegWriteOptions options)
    {
        options = PresetResolver.Resolve(image, options);
        ValidateOptions(options);
        if (OptionsResolver.ResolveProcess(options) != JpegProcess.BaselineDct)
        {
            throw JpegUnsupportedException.Create("Image-only abbreviated encoding currently supports baseline sequential JPEG output.");
        }

        if (IsNonInterleaved(options))
        {
            throw JpegUnsupportedException.Create("Image-only abbreviated encoding requires an interleaved scan.");
        }

        if (options.PreserveQuantizationTables
            || options.Tables.QuantizationTables.Count > 0
            || options.Tables.HuffmanTables.Count > 0
            || options.Tables.ArithmeticConditioningTables.Count > 0)
        {
            throw JpegWriteValidationException.Create("Abbreviated image encoding uses the tables argument instead of option-supplied tables.");
        }

        JpegPlanarImageBuffer rawPlanar = CreatePlanarSource(image, options);
        List<JpegEncoderComponent> components = ComponentConfigurator.ResolveComponents(rawPlanar.ComponentCount, options, false);
        JpegFrameHeader frame = CreateFrameForProcess(rawPlanar.Width, rawPlanar.Height, rawPlanar.ComponentCount, 8, JpegProcess.BaselineDct, components, image, options);
        int restartInterval = RestartIntervalResolver.Resolve(frame, options);
        JpegPlanarImageBuffer planar = ComponentConfigurator.ConfigureByteSource(rawPlanar, frame, components, options.ChromaDownsamplingFilter);
        JpegScanHeader scan = CreateBaselineScan(frame);
        List<JpegScanHeader> scanHeaders = CreateSingleScanList(scan);
        TableConfigurator.ApplyScanTableAssignments(scanHeaders, options);
        JpegTableStore tableStore = new JpegTableSetCodec().CreateTableStore(tables);
        TableConfigurator.ValidateTableReferences(frame, scanHeaders, tableStore, false, false);
        byte[] entropyData = BaselineEncoder.EncodeWithRestartInterval(frame, scan, tableStore, planar, restartInterval);
        return WriteAbbreviatedContainer(image, frame, scan, entropyData, options, restartInterval);
    }

    /// <summary>Implements encoding with default options for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    public byte[] Write(JpegImage image)
    {
        return WriteWithOptions(image, new JpegWriteOptions());
    }

    /// <summary>Implements encoding with explicit options for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    public byte[] WriteWithOptions(JpegImage image, JpegWriteOptions options)
    {
        return WriteWithOptionsAndCancellation(image, options, default(System.Threading.CancellationToken));
    }

    /// <summary>Implements result-returning encoding for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <returns>The encoded bytes and resolved settings.</returns>
    public JpegWriteResult WriteWithResult(JpegImage image, JpegWriteOptions options)
    {
        JpegWriteOptions resolved = PresetResolver.Resolve(image, options);
        byte[] data = WriteResolvedOptionsAndCancellation(image, resolved, default(System.Threading.CancellationToken));
        JpegInfo info = new JpegIdentificationService().Identify(data);
        JpegWriteResult result = new JpegWriteResult();
        result.Data = data;
        result.ResolvedOptions = PresetResolver.CreateInspectedOptions(resolved, info);
        return result;
    }

    /// <summary>Implements cancellable encoding for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    public byte[] WriteWithCancellation(JpegImage image, System.Threading.CancellationToken cancellationToken)
    {
        return WriteWithOptionsAndCancellation(image, new JpegWriteOptions(), cancellationToken);
    }

    /// <summary>Implements cancellable encoding with options for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    public byte[] WriteWithOptionsAndCancellation(JpegImage image, JpegWriteOptions options, System.Threading.CancellationToken cancellationToken)
    {
        JpegWriteOptions resolved = PresetResolver.Resolve(image, options);
        return WriteResolvedOptionsAndCancellation(image, resolved, cancellationToken);
    }

    internal byte[] WriteResolvedOptionsAndCancellation(JpegImage image, JpegWriteOptions options, System.Threading.CancellationToken cancellationToken)
    {
        ValidateOptions(options);
        cancellationToken.ThrowIfCancellationRequested();
        JpegProcess process = OptionsResolver.ResolveProcess(options);
        OptionsResolver.ValidateProcessSupport(process);
        ValidateProgressiveScanScriptForProcess(options, process);
        TableConfigurator.ValidateTablesForProcess(process, options);
        int precision = OptionsResolver.ResolvePrecision(image, options, process);
        OptionsResolver.ValidatePrecisionForProcess(precision, process);
        ValidatePointTransformForProcess(options, process, precision);
        if (process == JpegProcess.ProgressiveDct)
        {
            return WriteProgressiveProcess(image, options, cancellationToken);
        }

        if (process == JpegProcess.ExtendedSequentialDct)
        {
            return WriteExtendedSequentialProcess(image, options, precision, cancellationToken);
        }

        if (process == JpegProcess.LosslessSequential)
        {
            return WriteLosslessHuffmanProcess(image, options, precision, cancellationToken);
        }

        if (process == JpegProcess.LosslessSequentialArithmetic)
        {
            return WriteLosslessArithmeticProcess(image, options, precision, cancellationToken);
        }

        return WriteBaselineProcess(image, options, cancellationToken);
    }

    internal byte[] WriteBaselineProcess(JpegImage image, JpegWriteOptions options, System.Threading.CancellationToken cancellationToken)
    {
        JpegPlanarImageBuffer rawPlanar = CreatePlanarSource(image, options);
        cancellationToken.ThrowIfCancellationRequested();
        List<JpegEncoderComponent> components = ComponentConfigurator.ResolveComponents(rawPlanar.ComponentCount, options, false);
        JpegFrameHeader frame = CreateFrameForProcess(rawPlanar.Width, rawPlanar.Height, rawPlanar.ComponentCount, 8, JpegProcess.BaselineDct, components, image, options);
        int restartInterval = RestartIntervalResolver.Resolve(frame, options);
        JpegPlanarImageBuffer planar = ComponentConfigurator.ConfigureByteSource(rawPlanar, frame, components, options.ChromaDownsamplingFilter);
        if (IsNonInterleaved(options))
        {
            List<JpegScanHeader> separateHeaders = CreateSequentialComponentScans(frame);
            TableConfigurator.ApplyScanTableAssignments(separateHeaders, options);
            JpegTableStore separateTables = CreateDctTableStore(image, planar.ComponentCount, options);
            TableConfigurator.ValidateTableReferences(frame, separateHeaders, separateTables, false, false);
            List<JpegProgressiveScanData> separateScans = EncodeBaselineNonInterleaved(frame, separateHeaders, separateTables, planar, restartInterval);
            cancellationToken.ThrowIfCancellationRequested();
            return WriteMultiScanContainer(image, frame, separateScans, separateTables, options, restartInterval);
        }

        JpegScanHeader scan = CreateBaselineScan(frame);
        List<JpegScanHeader> scanHeaders = CreateSingleScanList(scan);
        TableConfigurator.ApplyScanTableAssignments(scanHeaders, options);
        JpegTableStore tables = CreateTableStore(image, frame, scan, planar, options, restartInterval);
        TableConfigurator.ValidateTableReferences(frame, scanHeaders, tables, false, false);
        cancellationToken.ThrowIfCancellationRequested();
        byte[] entropyData = BaselineEncoder.EncodeWithRestartInterval(frame, scan, tables, planar, restartInterval);
        cancellationToken.ThrowIfCancellationRequested();
        return WriteContainer(image, frame, scan, tables, entropyData, options, restartInterval);
    }

    internal byte[] WriteProgressiveProcess(JpegImage image, JpegWriteOptions options, System.Threading.CancellationToken cancellationToken)
    {
        JpegPlanarImageBuffer rawPlanar = CreatePlanarSource(image, options);
        cancellationToken.ThrowIfCancellationRequested();
        List<JpegEncoderComponent> components = ComponentConfigurator.ResolveComponents(rawPlanar.ComponentCount, options, false);
        JpegFrameHeader frame = CreateFrameForProcess(rawPlanar.Width, rawPlanar.Height, rawPlanar.ComponentCount, 8, JpegProcess.ProgressiveDct, components, image, options);
        int restartInterval = RestartIntervalResolver.Resolve(frame, options);
        JpegPlanarImageBuffer planar = ComponentConfigurator.ConfigureByteSource(rawPlanar, frame, components, options.ChromaDownsamplingFilter);
        List<JpegScanHeader> scanScript;
        if (options.ProgressiveScanScript.Scans.Count > 0)
        {
            scanScript = ProgressiveEncoder.CopyPublicScanScript(options.ProgressiveScanScript);
        }
        else if (IsNonInterleaved(options))
        {
            scanScript = CreateProgressiveNonInterleavedScript(frame);
        }
        else
        {
            scanScript = ProgressiveEncoder.CreateDefaultScanScript(frame);
        }

        TableConfigurator.ApplyScanTableAssignments(scanScript, options);
        ValidateNonInterleavedProgressiveScans(scanScript, options);
        ProgressiveEncoder.ValidateScanScript(frame, scanScript);
        JpegTableStore tables = CreateDctTableStore(image, planar.ComponentCount, options);
        TableConfigurator.ValidateTableReferences(frame, scanScript, tables, false, false);
        List<JpegProgressiveScanData> scans = ProgressiveEncoder.EncodeWithScanScriptAndRestartInterval(
            frame,
            tables,
            planar,
            scanScript,
            restartInterval);

        cancellationToken.ThrowIfCancellationRequested();
        return WriteProgressiveContainer(image, frame, scans, tables, options, restartInterval);
    }

    internal byte[] WriteExtendedSequentialProcess(JpegImage image, JpegWriteOptions options, int precision, System.Threading.CancellationToken cancellationToken)
    {
        JpegPlanarIntImageBuffer rawSource = IntegerImageSource.Create(image, precision);
        cancellationToken.ThrowIfCancellationRequested();
        List<JpegEncoderComponent> components = ComponentConfigurator.ResolveComponents(rawSource.ComponentCount, options, false);
        JpegFrameHeader frame = CreateFrameForProcess(rawSource.Width, rawSource.Height, rawSource.ComponentCount, precision, JpegProcess.ExtendedSequentialDct, components, image, options);
        int restartInterval = RestartIntervalResolver.Resolve(frame, options);
        JpegPlanarIntImageBuffer source = ComponentConfigurator.ConfigureIntegerSource(rawSource, frame, components, options.ChromaDownsamplingFilter);
        if (IsNonInterleaved(options))
        {
            List<JpegScanHeader> separateHeaders = CreateSequentialComponentScans(frame);
            TableConfigurator.ApplyScanTableAssignments(separateHeaders, options);
            JpegTableStore separateTables = CreateDctTableStore(image, source.ComponentCount, options);
            TableConfigurator.ValidateTableReferences(frame, separateHeaders, separateTables, false, false);
            List<JpegProgressiveScanData> separateScans = EncodeExtendedNonInterleaved(frame, separateHeaders, separateTables, source, restartInterval);
            cancellationToken.ThrowIfCancellationRequested();
            return WriteMultiScanContainer(image, frame, separateScans, separateTables, options, restartInterval);
        }

        JpegScanHeader scan = CreateBaselineScan(frame);
        List<JpegScanHeader> scanHeaders = CreateSingleScanList(scan);
        TableConfigurator.ApplyScanTableAssignments(scanHeaders, options);
        JpegTableStore tables = CreateDctTableStore(image, source.ComponentCount, options);
        TableConfigurator.ValidateTableReferences(frame, scanHeaders, tables, false, false);
        byte[] entropyData = ExtendedSequentialEncoder.EncodeWithRestartInterval(frame, scan, tables, source, restartInterval);
        cancellationToken.ThrowIfCancellationRequested();
        return WriteContainer(image, frame, scan, tables, entropyData, options, restartInterval);
    }

    internal byte[] WriteLosslessHuffmanProcess(JpegImage image, JpegWriteOptions options, int precision, System.Threading.CancellationToken cancellationToken)
    {
        ValidateLosslessScanLayout(options);
        JpegPlanarIntImageBuffer rawSource = IntegerImageSource.Create(image, precision);
        cancellationToken.ThrowIfCancellationRequested();
        List<JpegEncoderComponent> components = ComponentConfigurator.ResolveComponents(rawSource.ComponentCount, options, true);
        JpegFrameHeader frame = CreateFrameForProcess(rawSource.Width, rawSource.Height, rawSource.ComponentCount, precision, JpegProcess.LosslessSequential, components, image, options);
        int restartInterval = RestartIntervalResolver.Resolve(frame, options);
        JpegPlanarIntImageBuffer source = ComponentConfigurator.ConfigureIntegerSource(rawSource, frame, components, options.ChromaDownsamplingFilter);
        JpegScanHeader scan = CreateLosslessScan(frame, options);
        List<JpegScanHeader> scanHeaders = CreateSingleScanList(scan);
        TableConfigurator.ApplyScanTableAssignments(scanHeaders, options);
        JpegTableStore tables = CreateLosslessHuffmanTableStore(options);
        TableConfigurator.ValidateTableReferences(frame, scanHeaders, tables, true, false);
        byte[] entropyData = LosslessHuffmanEncoder.EncodeWithRestartInterval(frame, scan, tables, source, restartInterval);
        cancellationToken.ThrowIfCancellationRequested();
        return WriteContainer(image, frame, scan, tables, entropyData, options, restartInterval);
    }

    internal byte[] WriteLosslessArithmeticProcess(JpegImage image, JpegWriteOptions options, int precision, System.Threading.CancellationToken cancellationToken)
    {
        ValidateLosslessScanLayout(options);
        JpegPlanarIntImageBuffer rawSource = IntegerImageSource.Create(image, precision);
        cancellationToken.ThrowIfCancellationRequested();
        List<JpegEncoderComponent> components = ComponentConfigurator.ResolveComponents(rawSource.ComponentCount, options, true);
        JpegFrameHeader frame = CreateFrameForProcess(rawSource.Width, rawSource.Height, rawSource.ComponentCount, precision, JpegProcess.LosslessSequentialArithmetic, components, image, options);
        int restartInterval = RestartIntervalResolver.Resolve(frame, options);
        JpegPlanarIntImageBuffer source = ComponentConfigurator.ConfigureIntegerSource(rawSource, frame, components, options.ChromaDownsamplingFilter);
        JpegScanHeader scan = CreateLosslessScan(frame, options);
        List<JpegScanHeader> scanHeaders = CreateSingleScanList(scan);
        TableConfigurator.ApplyScanTableAssignments(scanHeaders, options);
        JpegTableStore tables = new JpegTableStore();
        tables.ArithmeticConditioningTables = TableConfigurator.ResolveArithmeticTables(options);
        TableConfigurator.ValidateTableReferences(frame, scanHeaders, tables, true, true);
        byte[] entropyData = LosslessArithmeticEncoder.EncodeWithRestartInterval(frame, scan, tables, source, restartInterval);
        cancellationToken.ThrowIfCancellationRequested();
        return WriteContainer(image, frame, scan, tables, entropyData, options, restartInterval);
    }

    /// <summary>Implements span encoding for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="destination">The destination for the complete JPEG data.</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteToSpan(JpegImage image, System.Span<byte> destination)
    {
        byte[] data = Write(image);
        return CopyEncodedBytesToSpan(data, destination);
    }

    /// <summary>Implements span encoding with options for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="destination">The destination for the complete JPEG data.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteToSpanWithOptions(JpegImage image, System.Span<byte> destination, JpegWriteOptions options)
    {
        byte[] data = WriteWithOptions(image, options);
        return CopyEncodedBytesToSpan(data, destination);
    }

    /// <summary>Implements cancellable span encoding for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="destination">The destination for the complete JPEG data.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteToSpanWithCancellation(JpegImage image, System.Span<byte> destination, System.Threading.CancellationToken cancellationToken)
    {
        byte[] data = WriteWithCancellation(image, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return CopyEncodedBytesToSpan(data, destination);
    }

    /// <summary>Implements cancellable span encoding with options for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="destination">The destination for the complete JPEG data.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteToSpanWithOptionsAndCancellation(JpegImage image, System.Span<byte> destination, JpegWriteOptions options, System.Threading.CancellationToken cancellationToken)
    {
        byte[] data = WriteWithOptionsAndCancellation(image, options, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return CopyEncodedBytesToSpan(data, destination);
    }

    /// <summary>Implements file encoding for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="path">The destination file path.</param>
    public void WriteFile(JpegImage image, string path)
    {
        Jpeg.ValidateFilePath(path);
        byte[] data = Write(image);
        new JpegIo().WriteFile(path, data, default(System.Threading.CancellationToken));
    }

    /// <summary>Implements file encoding with options for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    public void WriteFileWithOptions(JpegImage image, string path, JpegWriteOptions options)
    {
        Jpeg.ValidateFilePath(path);
        byte[] data = WriteWithOptions(image, options);
        new JpegIo().WriteFile(path, data, default(System.Threading.CancellationToken));
    }

    /// <summary>Implements cancellable file encoding for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    public void WriteFileWithCancellation(JpegImage image, string path, System.Threading.CancellationToken cancellationToken)
    {
        Jpeg.ValidateFilePath(path);
        byte[] data = WriteWithCancellation(image, cancellationToken);
        new JpegIo().WriteFile(path, data, cancellationToken);
    }

    /// <summary>Implements cancellable file encoding with options for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    public void WriteFileWithOptionsAndCancellation(JpegImage image, string path, JpegWriteOptions options, System.Threading.CancellationToken cancellationToken)
    {
        Jpeg.ValidateFilePath(path);
        byte[] data = WriteWithOptionsAndCancellation(image, options, cancellationToken);
        new JpegIo().WriteFile(path, data, cancellationToken);
    }

    /// <summary>Implements stream encoding with default options for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    public void WriteToStream(JpegImage image, System.IO.Stream stream)
    {
        WriteToStreamWithOptions(image, stream, new JpegWriteOptions());
    }

    /// <summary>Implements stream encoding with explicit options for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The pixel image and metadata to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    public void WriteToStreamWithOptions(JpegImage image, System.IO.Stream stream, JpegWriteOptions options)
    {
        ValidateWritableStream(stream);
        byte[] data = WriteWithOptions(image, options);
        new JpegIo().WriteStream(stream, data, default(System.Threading.CancellationToken));
    }

    /// <summary>Implements cancellable stream encoding for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    public void WriteToStreamWithCancellation(JpegImage image, System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)
    {
        WriteToStreamWithOptionsAndCancellation(image, stream, new JpegWriteOptions(), cancellationToken);
    }

    /// <summary>Implements cancellable stream encoding with options for <see cref="IJpegWriter"/>.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    public void WriteToStreamWithOptionsAndCancellation(JpegImage image, System.IO.Stream stream, JpegWriteOptions options, System.Threading.CancellationToken cancellationToken)
    {
        ValidateWritableStream(stream);
        byte[] data = WriteWithOptionsAndCancellation(image, options, cancellationToken);
        WriteEncodedBytesWithCancellation(data, stream, cancellationToken);
    }

    /// <summary>Implements cancellable row callback encoding for <see cref="IJpegWriter"/>.</summary>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="pixelFormat">The packed source pixel layout.</param>
    /// <param name="colorSpace">The interpreted source color space.</param>
    /// <param name="options">Options controlling JPEG encoding.</param>
    /// <param name="callback">The callback that fills each source row.</param>
    /// <param name="cancellationToken">The token checked around every callback.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    public byte[] WriteRowsWithOptionsAndCancellation(int width, int height, JpegPixelFormat pixelFormat, JpegColorSpace colorSpace, JpegWriteOptions options, IJpegEncodeRowCallback callback, System.Threading.CancellationToken cancellationToken)
    {
        ValidateOptions(options);
        ValidateEncodeRowCallback(callback);
        ValidateEncodeRowPixelFormat(pixelFormat);
        cancellationToken.ThrowIfCancellationRequested();

        JpegInterleavedPixelBufferAllocator allocator = new JpegInterleavedPixelBufferAllocator();
        int componentCount = allocator.ResolveComponentCount(pixelFormat, 0);
        JpegImage image = allocator.CreateImage(width, height, pixelFormat, colorSpace, componentCount);
        if (componentCount == 1)
        {
            image.Subsampling = JpegSubsampling.Grayscale;
        }
        else
        {
            image.Subsampling = JpegSubsampling.Ratio444;
        }

        byte[] reusableSamples = new byte[image.RowStride];
        JpegRowBuffer row = new JpegRowBuffer();
        row.RowCount = height;
        row.Width = width;
        row.ComponentCount = componentCount;
        row.PixelFormat = pixelFormat;
        row.ColorSpace = colorSpace;
        row.SampleCount = reusableSamples.Length;
        row.Samples = reusableSamples;

        int rowIndex = 0;
        while (rowIndex < height)
        {
            cancellationToken.ThrowIfCancellationRequested();
            row.RowIndex = rowIndex;
            row.RowCount = height;
            row.Width = width;
            row.ComponentCount = componentCount;
            row.PixelFormat = pixelFormat;
            row.ColorSpace = colorSpace;
            row.SampleCount = reusableSamples.Length;
            row.Samples = reusableSamples;
            callback.FillEncodeRow(row);
            cancellationToken.ThrowIfCancellationRequested();
            if (row.Samples != reusableSamples)
            {
                throw JpegWriteValidationException.Create("Encode row callbacks cannot replace the reusable sample buffer.");
            }

            System.Array.Copy(reusableSamples, 0, image.Pixels, rowIndex * image.RowStride, reusableSamples.Length);
            rowIndex = rowIndex + 1;
        }

        return WriteWithOptionsAndCancellation(image, options, cancellationToken);
    }

    internal void ValidateEncodeRowCallback(IJpegEncodeRowCallback callback)
    {
        if (callback == null)
        {
            throw new System.ArgumentNullException("callback");
        }
    }

    internal void ValidateEncodeRowPixelFormat(JpegPixelFormat pixelFormat)
    {
        if (pixelFormat != JpegPixelFormat.L8 && pixelFormat != JpegPixelFormat.Rgb24 && pixelFormat != JpegPixelFormat.YCbCr24)
        {
            throw JpegUnsupportedException.Create("Row callback encoding supports L8, Rgb24, and YCbCr24 input rows.");
        }
    }

    internal int CopyEncodedBytesToSpan(byte[] data, System.Span<byte> destination)
    {
        if (destination.Length < data.Length)
        {
            throw new System.ArgumentException("Destination span is too small for the encoded JPEG data.", "destination");
        }

        System.MemoryExtensions.AsSpan(data).CopyTo(destination);
        return data.Length;
    }

    internal void ValidateWritableStream(System.IO.Stream stream)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }

        if (!stream.CanWrite)
        {
            throw new System.ArgumentException("Stream must be writable.", "stream");
        }
    }

    internal void WriteEncodedBytesWithCancellation(byte[] data, System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)
    {
        new JpegIo().WriteStream(stream, data, cancellationToken);
    }

    internal void ValidateOptions(JpegWriteOptions options)
    {
        OptionsResolver.ValidateOptions(options);
        if (options.ScanLayout != JpegScanLayout.Automatic
            && options.ScanLayout != JpegScanLayout.Interleaved
            && options.ScanLayout != JpegScanLayout.NonInterleaved)
        {
            throw JpegWriteValidationException.Create("The selected scan layout is not recognized.");
        }

        if (options.Components == null)
        {
            throw new System.ArgumentException("Encoder component mapping collection cannot be null.", "options");
        }

        if (options.ProgressiveScanScript == null || options.ProgressiveScanScript.Scans == null)
        {
            throw new System.ArgumentException("Progressive scan script and scan collection cannot be null.", "options");
        }

        TableConfigurator.ValidateOptions(options);
    }

    internal void ValidateProgressiveScanScriptForProcess(JpegWriteOptions options, JpegProcess process)
    {
        if (options.ProgressiveScanScript.Scans.Count > 0 && process != JpegProcess.ProgressiveDct)
        {
            throw JpegWriteValidationException.Create("A progressive scan script requires the progressive Huffman DCT process.");
        }
    }

    internal void ValidateNonInterleavedProgressiveScans(List<JpegScanHeader> scans, JpegWriteOptions options)
    {
        if (!IsNonInterleaved(options))
        {
            return;
        }

        foreach (JpegScanHeader scan in scans)
        {
            if (scan.Components.Count != 1)
            {
                throw JpegWriteValidationException.Create("A non-interleaved progressive script must contain one component per scan.");
            }
        }
    }

    internal bool IsNonInterleaved(JpegWriteOptions options)
    {
        return options.ScanLayout == JpegScanLayout.NonInterleaved;
    }

    internal void ValidateLosslessScanLayout(JpegWriteOptions options)
    {
        if (IsNonInterleaved(options))
        {
            throw JpegUnsupportedException.Create("Lossless encoding currently requires one interleaved scan.");
        }
    }

    internal List<JpegScanHeader> CreateSequentialComponentScans(JpegFrameHeader frame)
    {
        List<JpegScanHeader> scans = new List<JpegScanHeader>();
        int index = 0;
        while (index < frame.Components.Count)
        {
            JpegScanHeader scan = new JpegScanHeader();
            scan.ComponentCount = 1;
            scan.SpectralStart = 0;
            scan.SpectralEnd = 63;
            scan.SuccessiveHigh = 0;
            scan.SuccessiveLow = 0;
            JpegScanComponent component = new JpegScanComponent();
            component.Selector = frame.Components[index].Identifier;
            component.DcEntropyTableIdentifier = ResolveEntropyTableIdentifier(index, frame.ComponentCount);
            component.AcEntropyTableIdentifier = ResolveEntropyTableIdentifier(index, frame.ComponentCount);
            scan.Components.Add(component);
            scans.Add(scan);
            index = index + 1;
        }

        return scans;
    }

    internal List<JpegScanHeader> CreateSingleScanList(JpegScanHeader scan)
    {
        List<JpegScanHeader> scans = new List<JpegScanHeader>();
        scans.Add(scan);
        return scans;
    }

    internal List<JpegProgressiveScanData> EncodeBaselineNonInterleaved(
        JpegFrameHeader frame,
        List<JpegScanHeader> scanHeaders,
        JpegTableStore tables,
        JpegPlanarImageBuffer source,
        int restartInterval)
    {
        List<JpegProgressiveScanData> scans = new List<JpegProgressiveScanData>();
        int index = 0;
        while (index < scanHeaders.Count)
        {
            JpegProgressiveScanData scan = new JpegProgressiveScanData();
            scan.Scan = scanHeaders[index];
            scan.EntropyData = SequentialScanEncoder.EncodeBaselineComponentScan(
                frame,
                scan.Scan,
                tables,
                source,
                restartInterval);
            scans.Add(scan);
            index = index + 1;
        }

        return scans;
    }

    internal List<JpegProgressiveScanData> EncodeExtendedNonInterleaved(
        JpegFrameHeader frame,
        List<JpegScanHeader> scanHeaders,
        JpegTableStore tables,
        JpegPlanarIntImageBuffer source,
        int restartInterval)
    {
        List<JpegProgressiveScanData> scans = new List<JpegProgressiveScanData>();
        int index = 0;
        while (index < scanHeaders.Count)
        {
            JpegProgressiveScanData scan = new JpegProgressiveScanData();
            scan.Scan = scanHeaders[index];
            scan.EntropyData = SequentialScanEncoder.EncodeExtendedComponentScan(
                frame,
                scan.Scan,
                tables,
                source,
                restartInterval);
            scans.Add(scan);
            index = index + 1;
        }

        return scans;
    }

    internal List<JpegScanHeader> CreateProgressiveNonInterleavedScript(JpegFrameHeader frame)
    {
        List<JpegScanHeader> scans = new List<JpegScanHeader>();
        int index = 0;
        while (index < frame.Components.Count)
        {
            JpegScanHeader dcScan = new JpegScanHeader();
            dcScan.ComponentCount = 1;
            dcScan.SpectralStart = 0;
            dcScan.SpectralEnd = 0;
            dcScan.SuccessiveHigh = 0;
            dcScan.SuccessiveLow = 0;
            dcScan.Components.Add(ProgressiveEncoder.CreateScanComponent(frame.Components[index].Identifier));
            scans.Add(dcScan);
            index = index + 1;
        }

        index = 0;
        while (index < frame.Components.Count)
        {
            scans.Add(ProgressiveEncoder.CreateAcFirstScan(frame.Components[index].Identifier));
            index = index + 1;
        }

        return scans;
    }

    internal void ValidatePointTransformForProcess(JpegWriteOptions options, JpegProcess process, int precision)
    {
        bool lossless = process == JpegProcess.LosslessSequential || process == JpegProcess.LosslessSequentialArithmetic;
        if (!lossless && options.PointTransform != 0)
        {
            throw JpegWriteValidationException.Create("Point transforms apply only to lossless JPEG processes.");
        }

        if (!lossless && options.LosslessPredictor != JpegLosslessPredictorSelection.Left)
        {
            throw JpegWriteValidationException.Create("Lossless predictor selection applies only to lossless JPEG processes.");
        }

        if (lossless && options.PointTransform >= precision)
        {
            throw JpegWriteValidationException.Create("Lossless point transform must be less than the sample precision.");
        }
    }

    internal JpegFrameHeader CreateFrameForProcess(
        int width,
        int height,
        int componentCount,
        int precision,
        JpegProcess process,
        List<JpegEncoderComponent> components,
        JpegImage image,
        JpegWriteOptions options)
    {
        JpegFrameHeader frame = new JpegFrameHeader();
        frame.Marker = (JpegMarker)(int)process;
        frame.Value = (int)process;
        frame.Process = process;
        frame.Width = width;
        frame.Height = height;
        frame.Precision = precision;
        frame.ComponentCount = componentCount;
        frame.Progressive = OptionsResolver.IsProgressiveProcess(process);
        frame.ArithmeticCoding = OptionsResolver.IsArithmeticProcess(process);

        int index = 0;
        while (index < componentCount)
        {
            JpegEncoderComponent mapping = components[index];
            JpegFrameComponent component = new JpegFrameComponent();
            component.Identifier = mapping.Identifier;
            component.HorizontalSamplingFactor = mapping.HorizontalSamplingFactor;
            component.VerticalSamplingFactor = mapping.VerticalSamplingFactor;
            component.QuantizationTableIdentifier = TableConfigurator.ResolveComponentQuantizationTableIdentifier(
                mapping,
                index,
                componentCount,
                image,
                options,
                process == JpegProcess.LosslessSequential || process == JpegProcess.LosslessSequentialArithmetic);
            frame.Components.Add(component);
            index = index + 1;
        }

        return frame;
    }

    internal JpegScanHeader CreateLosslessScan(JpegFrameHeader frame, JpegWriteOptions options)
    {
        JpegScanHeader scan = new JpegScanHeader();
        scan.ComponentCount = frame.ComponentCount;
        scan.SpectralStart = (int)options.LosslessPredictor;
        scan.SpectralEnd = 0;
        scan.SuccessiveHigh = 0;
        scan.SuccessiveLow = options.PointTransform;
        int index = 0;
        while (index < frame.ComponentCount)
        {
            JpegScanComponent component = new JpegScanComponent();
            component.Selector = frame.Components[index].Identifier;
            component.DcEntropyTableIdentifier = 0;
            component.AcEntropyTableIdentifier = 0;
            scan.Components.Add(component);
            index = index + 1;
        }

        return scan;
    }

    internal JpegTableStore CreateDctTableStore(JpegImage image, int componentCount, JpegWriteOptions options)
    {
        JpegTableStore tables = new JpegTableStore();
        tables.QuantizationTables = TableConfigurator.ResolveQuantizationTables(
            image,
            CreateQuantizationTables(componentCount, options),
            options);
        tables.HuffmanTables = TableConfigurator.ResolveHuffmanTables(CreateComprehensiveDctHuffmanTables(componentCount), options);
        return tables;
    }

    internal JpegTableStore CreateLosslessHuffmanTableStore(JpegWriteOptions options)
    {
        JpegTableStore tables = new JpegTableStore();
        List<JpegHuffmanTable> defaults = new List<JpegHuffmanTable>();
        defaults.Add(CreateComprehensiveHuffmanTable(0, 0, true));
        tables.HuffmanTables = TableConfigurator.ResolveHuffmanTables(defaults, options);
        return tables;
    }

    internal List<JpegHuffmanTable> CreateComprehensiveDctHuffmanTables(int componentCount)
    {
        List<JpegHuffmanTable> tables = new List<JpegHuffmanTable>();
        tables.Add(CreateComprehensiveHuffmanTable(0, 0, false));
        tables.Add(CreateComprehensiveHuffmanTable(1, 0, false));
        if (componentCount > 1)
        {
            tables.Add(CreateComprehensiveHuffmanTable(0, 1, false));
            tables.Add(CreateComprehensiveHuffmanTable(1, 1, false));
        }

        return tables;
    }

    internal JpegHuffmanTable CreateComprehensiveHuffmanTable(int tableClass, int identifier, bool lossless)
    {
        JpegHuffmanTable table = new JpegHuffmanTable();
        table.TableClass = tableClass;
        table.Identifier = identifier;
        if (tableClass == 0)
        {
            int symbolCount = 16;
            if (lossless)
            {
                symbolCount = 17;
            }

            table.CodeCounts[4] = symbolCount;
            table.Symbols = new int[symbolCount];
            int index = 0;
            while (index < symbolCount)
            {
                table.Symbols[index] = index;
                index = index + 1;
            }
        }
        else
        {
            table.CodeCounts[7] = 242;
            table.Symbols = CreateComprehensiveAcSymbols();
        }

        new JpegHuffmanTableParser().PrepareCallerTable(table);
        return table;
    }

    internal int[] CreateComprehensiveAcSymbols()
    {
        int[] symbols = new int[242];
        symbols[0] = 0;
        symbols[1] = 240;
        int offset = 2;
        int run = 0;
        while (run < 16)
        {
            int size = 1;
            while (size < 16)
            {
                symbols[offset] = (run << 4) + size;
                offset = offset + 1;
                size = size + 1;
            }

            run = run + 1;
        }

        return symbols;
    }

    internal JpegPlanarImageBuffer CreatePlanarSource(JpegImage image, JpegWriteOptions options)
    {
        ValidateImageBasics(image);
        if (image.BufferLayout == JpegBufferLayout.Planar
            || (image.BufferLayout == JpegBufferLayout.Interleaved
                && IsSupportedByteComponentPixelFormat(image.PixelFormat)))
        {
            return CreateByteComponentPlanarSource(image);
        }

        if (image.PixelFormat == JpegPixelFormat.L8)
        {
            return CreateGrayscalePlanarSource(image);
        }

        if (image.PixelFormat == JpegPixelFormat.Rgb24)
        {
            JpegImage yCbCrImage = RgbToYCbCrConverter.ConvertRgbImageToYCbCrImage(image);
            return CreateThreeComponentPlanarSource(yCbCrImage);
        }

        if (image.PixelFormat == JpegPixelFormat.YCbCr24)
        {
            return CreateThreeComponentPlanarSource(image);
        }

        if (image.PixelFormat == JpegPixelFormat.RawComponents)
        {
            return CreateByteComponentPlanarSource(image);
        }

        throw JpegUnsupportedException.Create("Jpeg.Write currently supports L8, Rgb24, YCbCr24, and raw component input images.");
    }

    internal bool IsSupportedByteComponentPixelFormat(JpegPixelFormat pixelFormat)
    {
        return pixelFormat == JpegPixelFormat.L8
            || pixelFormat == JpegPixelFormat.Rgb24
            || pixelFormat == JpegPixelFormat.YCbCr24
            || pixelFormat == JpegPixelFormat.RawComponents;
    }

    internal JpegPlanarImageBuffer CreateByteComponentPlanarSource(JpegImage image)
    {
        JpegPlanarIntImageBuffer source = IntegerImageSource.Create(image, 8);
        if ((image.PixelFormat == JpegPixelFormat.Rgb24
                || image.ColorSpace == JpegColorSpace.RGB)
            && source.ComponentCount == 3)
        {
            return ConvertRgbComponentSourceToPlanar(source);
        }

        JpegPlanarImageBuffer planar = CreateEmptyBytePlanarSource(source, image.ColorSpace, image.Subsampling);
        int componentIndex = 0;
        while (componentIndex < source.ComponentCount)
        {
            CopyIntegerComponentToByte(source.Components[componentIndex], planar.Components[componentIndex]);
            componentIndex = componentIndex + 1;
        }

        return planar;
    }

    internal JpegPlanarImageBuffer ConvertRgbComponentSourceToPlanar(JpegPlanarIntImageBuffer source)
    {
        JpegPlanarImageBuffer planar = CreateEmptyBytePlanarSource(source, JpegColorSpace.YCbCr, JpegSubsampling.Ratio444);
        JpegPlanarIntComponentBuffer red = source.Components[0];
        JpegPlanarIntComponentBuffer green = source.Components[1];
        JpegPlanarIntComponentBuffer blue = source.Components[2];
        int y = 0;
        while (y < source.Height)
        {
            int x = 0;
            while (x < source.Width)
            {
                int sourceIndex = y * red.Stride + x;
                int destinationIndex = y * planar.Components[0].Stride + x;
                int redSample = red.Samples[sourceIndex];
                int greenSample = green.Samples[y * green.Stride + x];
                int blueSample = blue.Samples[y * blue.Stride + x];
                planar.Components[0].Samples[destinationIndex] = (byte)RgbToYCbCrConverter.ConvertSampleToY(redSample, greenSample, blueSample);
                planar.Components[1].Samples[destinationIndex] = (byte)RgbToYCbCrConverter.ConvertSampleToCb(redSample, greenSample, blueSample);
                planar.Components[2].Samples[destinationIndex] = (byte)RgbToYCbCrConverter.ConvertSampleToCr(redSample, greenSample, blueSample);
                x = x + 1;
            }

            y = y + 1;
        }

        return planar;
    }

    internal JpegPlanarImageBuffer CreateEmptyBytePlanarSource(
        JpegPlanarIntImageBuffer source,
        JpegColorSpace colorSpace,
        JpegSubsampling subsampling)
    {
        JpegPlanarImageBuffer planar = new JpegPlanarImageBuffer();
        planar.Width = source.Width;
        planar.Height = source.Height;
        planar.Precision = 8;
        planar.ComponentCount = source.ComponentCount;
        planar.ColorSpace = colorSpace;
        planar.Subsampling = subsampling;
        planar.Ownership = JpegBufferOwnership.LibraryOwned;
        int componentIndex = 0;
        while (componentIndex < source.ComponentCount)
        {
            planar.Components.Add(CreateComponent(componentIndex, componentIndex + 1, source.Width, source.Height));
            componentIndex = componentIndex + 1;
        }

        return planar;
    }

    internal void CopyIntegerComponentToByte(JpegPlanarIntComponentBuffer source, JpegPlanarComponentBuffer destination)
    {
        int y = 0;
        while (y < destination.Height)
        {
            int x = 0;
            while (x < destination.Width)
            {
                destination.Samples[y * destination.Stride + x] = (byte)source.Samples[y * source.Stride + x];
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void ValidateImageBasics(JpegImage image)
    {
        if (image == null)
        {
            throw new System.ArgumentNullException("image");
        }

        if (image.Width <= 0)
        {
            throw JpegWriteValidationException.Create("Image width must be greater than zero.");
        }

        if (image.Height <= 0)
        {
            throw JpegWriteValidationException.Create("Image height must be greater than zero.");
        }

        if (image.Precision != 0 && image.Precision != 8)
        {
            throw JpegUnsupportedException.Create("Jpeg.Write currently supports 8-bit sample precision.");
        }

        if (image.Pixels == null)
        {
            throw new System.ArgumentNullException("Pixels");
        }
    }

    internal JpegPlanarImageBuffer CreateGrayscalePlanarSource(JpegImage image)
    {
        ValidatePixelBufferLength(image, 1);

        JpegPlanarImageBuffer planar = new JpegPlanarImageBuffer();
        planar.Width = image.Width;
        planar.Height = image.Height;
        planar.Precision = 8;
        planar.ComponentCount = 1;
        planar.ColorSpace = JpegColorSpace.Grayscale;
        planar.Subsampling = JpegSubsampling.Grayscale;
        planar.Ownership = JpegBufferOwnership.LibraryOwned;
        JpegPlanarComponentBuffer component = CreateComponent(0, 1, image.Width, image.Height);
        int sourceStride = ResolveImageRowStride(image, 1);
        int y = 0;
        while (y < image.Height)
        {
            System.Array.Copy(image.Pixels, y * sourceStride, component.Samples, y * component.Stride, image.Width);
            y = y + 1;
        }

        planar.Components.Add(component);
        return planar;
    }

    internal JpegPlanarImageBuffer CreateThreeComponentPlanarSource(JpegImage image)
    {
        ValidatePixelBufferLength(image, 3);

        JpegPlanarImageBuffer planar = new JpegPlanarImageBuffer();
        planar.Width = image.Width;
        planar.Height = image.Height;
        planar.Precision = 8;
        planar.ComponentCount = 3;
        planar.ColorSpace = JpegColorSpace.YCbCr;
        planar.Subsampling = JpegSubsampling.Ratio444;
        planar.Ownership = JpegBufferOwnership.LibraryOwned;
        JpegPlanarComponentBuffer first = CreateComponent(0, 1, image.Width, image.Height);
        JpegPlanarComponentBuffer second = CreateComponent(1, 2, image.Width, image.Height);
        JpegPlanarComponentBuffer third = CreateComponent(2, 3, image.Width, image.Height);

        int sourceStride = ResolveImageRowStride(image, 3);
        int y = 0;
        while (y < image.Height)
        {
            int pixelIndex = y * sourceStride;
            int sampleIndex = y * image.Width;
            int x = 0;
            while (x < image.Width)
            {
                first.Samples[sampleIndex] = image.Pixels[pixelIndex];
                second.Samples[sampleIndex] = image.Pixels[pixelIndex + 1];
                third.Samples[sampleIndex] = image.Pixels[pixelIndex + 2];
                pixelIndex = pixelIndex + 3;
                sampleIndex = sampleIndex + 1;
                x = x + 1;
            }

            y = y + 1;
        }

        planar.Components.Add(first);
        planar.Components.Add(second);
        planar.Components.Add(third);
        return planar;
    }

    internal JpegPlanarComponentBuffer CreateComponent(int componentIndex, int identifier, int width, int height)
    {
        JpegPlanarComponentBuffer component = new JpegPlanarComponentBuffer();
        component.ComponentIndex = componentIndex;
        component.Identifier = identifier;
        component.Precision = 8;
        component.HorizontalSamplingFactor = 1;
        component.VerticalSamplingFactor = 1;
        component.Width = width;
        component.Height = height;
        component.PaddedWidth = width;
        component.PaddedHeight = height;
        component.Stride = width;
        component.SampleStorage = JpegSampleStorage.Byte;
        component.Ownership = JpegBufferOwnership.LibraryOwned;
        component.Samples = new byte[width * height];
        return component;
    }

    internal void ValidatePixelBufferLength(JpegImage image, int bytesPerPixel)
    {
        int rowStride = ResolveImageRowStride(image, bytesPerPixel);
        long minimumLength = (long)rowStride * (long)image.Height;
        if (minimumLength > int.MaxValue)
        {
            throw JpegWriteValidationException.Create("Image pixel buffer is too large.");
        }

        if (image.Pixels.Length < (int)minimumLength)
        {
            throw JpegWriteValidationException.Create("Image pixel buffer is too small.");
        }
    }

    internal int ResolveImageRowStride(JpegImage image, int bytesPerPixel)
    {
        long minimumStride = (long)image.Width * (long)bytesPerPixel;
        if (minimumStride > int.MaxValue)
        {
            throw JpegWriteValidationException.Create("Image row is too large.");
        }

        int stride = image.RowStride;
        if (stride == 0)
        {
            stride = (int)minimumStride;
        }

        if (stride < (int)minimumStride)
        {
            throw JpegWriteValidationException.Create("Image row stride is too small for the packed pixels.");
        }

        return stride;
    }

    internal JpegFrameHeader CreateBaselineFrame(JpegPlanarImageBuffer source)
    {
        JpegFrameHeader frame = new JpegFrameHeader();
        frame.Marker = JpegMarker.StartOfFrame0;
        frame.Value = JpegConstants.MarkerStartOfFrame0;
        frame.Process = JpegProcess.BaselineDct;
        frame.Width = source.Width;
        frame.Height = source.Height;
        frame.Precision = 8;
        frame.ComponentCount = source.ComponentCount;

        int index = 0;
        while (index < source.ComponentCount)
        {
            JpegFrameComponent component = new JpegFrameComponent();
            component.Identifier = source.Components[index].Identifier;
            component.HorizontalSamplingFactor = source.Components[index].HorizontalSamplingFactor;
            component.VerticalSamplingFactor = source.Components[index].VerticalSamplingFactor;
            component.QuantizationTableIdentifier = ResolveQuantizationTableIdentifier(index, source.ComponentCount);
            frame.Components.Add(component);
            index = index + 1;
        }

        return frame;
    }

    internal JpegScanHeader CreateBaselineScan(JpegFrameHeader frame)
    {
        JpegScanHeader scan = new JpegScanHeader();
        scan.ComponentCount = frame.ComponentCount;
        scan.SpectralStart = 0;
        scan.SpectralEnd = 63;
        scan.SuccessiveHigh = 0;
        scan.SuccessiveLow = 0;

        int index = 0;
        while (index < frame.ComponentCount)
        {
            JpegScanComponent component = new JpegScanComponent();
            component.Selector = frame.Components[index].Identifier;
            component.DcEntropyTableIdentifier = ResolveEntropyTableIdentifier(index, frame.ComponentCount);
            component.AcEntropyTableIdentifier = ResolveEntropyTableIdentifier(index, frame.ComponentCount);
            scan.Components.Add(component);
            index = index + 1;
        }

        return scan;
    }

    internal int ResolveQuantizationTableIdentifier(int componentIndex, int componentCount)
    {
        if (componentCount == 3 && componentIndex > 0)
        {
            return 1;
        }

        return 0;
    }

    internal int ResolveEntropyTableIdentifier(int componentIndex, int componentCount)
    {
        if (componentCount == 3 && componentIndex > 0)
        {
            return 1;
        }

        return 0;
    }

    internal JpegTableStore CreateTableStore(
        JpegImage image,
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegPlanarImageBuffer source,
        JpegWriteOptions options,
        int restartInterval)
    {
        JpegTableStore tables = new JpegTableStore();
        tables.QuantizationTables = TableConfigurator.ResolveQuantizationTables(
            image,
            CreateQuantizationTables(source.ComponentCount, options),
            options);
        List<JpegHuffmanTable> generated = CreateComprehensiveDctHuffmanTables(source.ComponentCount);
        if (options.OptimizeHuffmanTables)
        {
            generated = CreateOptimizedHuffmanTables(
                frame,
                scan,
                tables.QuantizationTables,
                source,
                restartInterval);
        }

        tables.HuffmanTables = TableConfigurator.ResolveHuffmanTables(generated, options);
        return tables;
    }

    internal List<JpegQuantizationTable> CreateQuantizationTables(int componentCount, JpegWriteOptions options)
    {
        List<JpegQuantizationTable> tables = new List<JpegQuantizationTable>();
        tables.Add(DefaultQuantizationTables.CreateLuminanceTableWithQuality(0, DefaultQuantizationTables.ResolveLuminanceQuality(options)));
        if (componentCount > 1)
        {
            tables.Add(DefaultQuantizationTables.CreateChrominanceTableWithQuality(1, DefaultQuantizationTables.ResolveChrominanceQuality(options)));
        }

        return tables;
    }

    internal List<JpegHuffmanTable> CreateOptimizedHuffmanTables(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        List<JpegQuantizationTable> quantizationTables,
        JpegPlanarImageBuffer source,
        int restartInterval)
    {
        JpegTableStore tableStore = new JpegTableStore();
        tableStore.QuantizationTables = quantizationTables;
        tableStore.HuffmanTables = new List<JpegHuffmanTable>();
        List<JpegHuffmanStatistics> statistics = CollectHuffmanStatistics(frame, scan, tableStore, source, restartInterval);
        List<JpegHuffmanTable> huffmanTables = new List<JpegHuffmanTable>();
        foreach (JpegHuffmanStatistics item in statistics)
        {
            huffmanTables.Add(HuffmanTableGenerator.GenerateTable(item));
        }

        return huffmanTables;
    }

    internal List<JpegHuffmanStatistics> CollectHuffmanStatistics(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegPlanarImageBuffer source,
        int restartInterval)
    {
        JpegHuffmanStatisticsCollector collector = new JpegHuffmanStatisticsCollector();
        List<JpegHuffmanStatistics> statistics = new List<JpegHuffmanStatistics>();
        JpegMcuLayout layout = BaselineEncoder.LayoutCalculator.Calculate(frame);

        int mcuCountSinceRestart = 0;
        int mcuY = 0;
        while (mcuY < layout.McuRows)
        {
            int mcuX = 0;
            while (mcuX < layout.McusPerRow)
            {
                CollectMcuStatistics(frame, scan, tables, layout, source, collector, statistics, mcuX, mcuY);
                mcuCountSinceRestart = mcuCountSinceRestart + 1;
                if (restartInterval > 0 && mcuCountSinceRestart == restartInterval && !BaselineEncoder.IsLastMcu(layout, mcuX, mcuY))
                {
                    collector.ResetDcPredictors();
                    mcuCountSinceRestart = 0;
                }

                mcuX = mcuX + 1;
            }

            mcuY = mcuY + 1;
        }

        return statistics;
    }

    internal void CollectMcuStatistics(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegMcuLayout layout,
        JpegPlanarImageBuffer source,
        JpegHuffmanStatisticsCollector collector,
        List<JpegHuffmanStatistics> statistics,
        int mcuX,
        int mcuY)
    {
        int scanComponentIndex = 0;
        while (scanComponentIndex < scan.Components.Count)
        {
            JpegScanComponent scanComponent = scan.Components[scanComponentIndex];
            JpegFrameComponent frameComponent = BaselineEncoder.BaselineDecoderValidation.FindFrameComponent(frame, scanComponent.Selector);
            JpegMcuComponentLayout componentLayout = BaselineEncoder.LayoutCalculator.FindComponentLayout(layout, scanComponent.Selector);
            JpegPlanarComponentBuffer componentBuffer = BaselineEncoder.FindComponentBuffer(source, scanComponent.Selector);
            JpegQuantizationTable quantizationTable = BaselineEncoder.FindQuantizationTable(tables, frameComponent.QuantizationTableIdentifier);
            JpegHuffmanStatistics dcStatistics = FindOrCreateStatistics(statistics, 0, scanComponent.DcEntropyTableIdentifier);
            JpegHuffmanStatistics acStatistics = FindOrCreateStatistics(statistics, 1, scanComponent.AcEntropyTableIdentifier);
            CollectMcuComponentStatistics(componentLayout, componentBuffer, quantizationTable, collector, dcStatistics, acStatistics, mcuX, mcuY);
            scanComponentIndex = scanComponentIndex + 1;
        }
    }

    internal void CollectMcuComponentStatistics(
        JpegMcuComponentLayout componentLayout,
        JpegPlanarComponentBuffer componentBuffer,
        JpegQuantizationTable quantizationTable,
        JpegHuffmanStatisticsCollector collector,
        JpegHuffmanStatistics dcStatistics,
        JpegHuffmanStatistics acStatistics,
        int mcuX,
        int mcuY)
    {
        int verticalIndex = 0;
        while (verticalIndex < componentLayout.VerticalSamplingFactor)
        {
            int horizontalIndex = 0;
            while (horizontalIndex < componentLayout.HorizontalSamplingFactor)
            {
                int sourceX = (mcuX * componentLayout.HorizontalSamplingFactor + horizontalIndex) * JpegConstants.BlockWidth;
                int sourceY = (mcuY * componentLayout.VerticalSamplingFactor + verticalIndex) * JpegConstants.BlockHeight;
                byte[] samples = BaselineEncoder.ReadBlockFromComponent(componentBuffer, sourceX, sourceY);
                int[] naturalCoefficients = BaselineEncoder.ForwardDct.ForwardTransformSamples(samples);
                int[] quantizedNaturalCoefficients = BaselineEncoder.Quantizer.QuantizeBlock(naturalCoefficients, quantizationTable);
                int[] zigZagCoefficients = BaselineEncoder.OrderConverter.NaturalToZigZag(quantizedNaturalCoefficients);
                collector.CollectBlock(zigZagCoefficients, componentLayout.ComponentIndex, dcStatistics, acStatistics);
                horizontalIndex = horizontalIndex + 1;
            }

            verticalIndex = verticalIndex + 1;
        }
    }

    internal JpegHuffmanStatistics FindOrCreateStatistics(List<JpegHuffmanStatistics> statistics, int tableClass, int identifier)
    {
        foreach (JpegHuffmanStatistics item in statistics)
        {
            if (item.TableClass == tableClass && item.Identifier == identifier)
            {
                return item;
            }
        }

        JpegHuffmanStatisticsCollector collector = new JpegHuffmanStatisticsCollector();
        JpegHuffmanStatistics created = collector.CreateStatistics(tableClass, identifier);
        statistics.Add(created);
        return created;
    }

    internal byte[] WriteContainer(
        JpegImage image,
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        byte[] entropyData,
        JpegWriteOptions options,
        int restartInterval)
    {
        JpegMarkerSegmentWriter writer = new JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        WriteMetadata(writer, image, options);
        WriteTableMarkers(writer, tables);
        if (restartInterval > 0)
        {
            new JpegRestartIntervalWriter().WriteMarkerSegment(writer, restartInterval);
        }

        new JpegFrameHeaderWriter().WriteMarkerSegment(writer, frame);
        new JpegScanHeaderWriter().WriteMarkerSegment(writer, scan);
        writer.Writer.WriteBytes(entropyData);
        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    internal byte[] WriteProgressiveContainer(
        JpegImage image,
        JpegFrameHeader frame,
        List<JpegProgressiveScanData> scans,
        JpegTableStore tables,
        JpegWriteOptions options,
        int restartInterval)
    {
        JpegMarkerSegmentWriter writer = new JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        WriteMetadata(writer, image, options);
        WriteTableMarkers(writer, tables);
        if (restartInterval > 0)
        {
            new JpegRestartIntervalWriter().WriteMarkerSegment(writer, restartInterval);
        }

        new JpegFrameHeaderWriter().WriteMarkerSegment(writer, frame);
        int index = 0;
        while (index < scans.Count)
        {
            new JpegScanHeaderWriter().WriteMarkerSegment(writer, scans[index].Scan);
            writer.Writer.WriteBytes(scans[index].EntropyData);
            index = index + 1;
        }

        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    internal byte[] WriteMultiScanContainer(
        JpegImage image,
        JpegFrameHeader frame,
        List<JpegProgressiveScanData> scans,
        JpegTableStore tables,
        JpegWriteOptions options,
        int restartInterval)
    {
        return WriteProgressiveContainer(image, frame, scans, tables, options, restartInterval);
    }

    internal void WriteTableMarkers(JpegMarkerSegmentWriter writer, JpegTableStore tables)
    {
        if (tables.QuantizationTables.Count > 0)
        {
            new JpegQuantizationTableWriter().WriteMarkerSegment(writer, tables.QuantizationTables);
        }

        if (tables.HuffmanTables.Count > 0)
        {
            new JpegHuffmanTableWriter().WriteMarkerSegment(writer, tables.HuffmanTables);
        }

        if (tables.ArithmeticConditioningTables.Count > 0)
        {
            new JpegArithmeticConditioningTableWriter().WriteMarkerSegment(writer, tables.ArithmeticConditioningTables);
        }
    }

    internal byte[] WriteAbbreviatedContainer(
        JpegImage image,
        JpegFrameHeader frame,
        JpegScanHeader scan,
        byte[] entropyData,
        JpegWriteOptions options,
        int restartInterval)
    {
        JpegMarkerSegmentWriter writer = new JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        WriteMetadata(writer, image, options);
        if (restartInterval > 0)
        {
            new JpegRestartIntervalWriter().WriteMarkerSegment(writer, restartInterval);
        }

        new JpegFrameHeaderWriter().WriteMarkerSegment(writer, frame);
        new JpegScanHeaderWriter().WriteMarkerSegment(writer, scan);
        writer.Writer.WriteBytes(entropyData);
        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    internal void WriteMetadata(JpegMarkerSegmentWriter writer, JpegImage image, JpegWriteOptions options)
    {
        if (image.Metadata == null)
        {
            return;
        }

        new JpegMetadataWriter().WriteMarkerSegmentsWithPolicy(writer, image.Metadata, options.MetadataPolicy);
    }
}
