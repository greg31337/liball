namespace jpeg;

internal class JpegCoefficientWriter
{
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();
    internal JpegBaselineSof0FullImageDecoder BaselineValidation = new JpegBaselineSof0FullImageDecoder();
    internal JpegCoefficientReader CoefficientReader = new JpegCoefficientReader();
    internal JpegProgressiveSof2Encoder ProgressiveEncoder = new JpegProgressiveSof2Encoder();

    internal byte[] Write(JpegCoefficientImage image)
    {
        ValidateImage(image);
        JpegFrameHeader frame = ResolveFrame(image);
        JpegTableStore tables = CreateTableStore(image);
        if (IsProgressiveModel(image, frame))
        {
            return WriteProgressive(image, frame, tables);
        }

        JpegScanHeader scan = ResolveScan(image);
        if (IsExtendedSequentialModel(image, frame))
        {
            byte[] extendedEntropyData = EncodeExtendedEntropy(image, frame, scan, tables);
            return WriteContainer(image, frame, scan, tables, extendedEntropyData);
        }

        BaselineValidation.ValidateFrame(frame);
        BaselineValidation.ValidateScan(frame, scan);

        byte[] entropyData = EncodeEntropy(image, frame, scan, tables);
        return WriteContainer(image, frame, scan, tables, entropyData);
    }

    internal void WriteToStream(JpegCoefficientImage image, System.IO.Stream stream)
    {
        new JpegIo().ValidateWritableStream(stream);
        byte[] data = Write(image);
        new JpegIo().WriteStream(stream, data, default(System.Threading.CancellationToken));
    }

    internal void WriteFile(string path, JpegCoefficientImage image)
    {
        Jpeg.ValidateFilePath(path);
        new JpegIo().WriteFile(path, Write(image), default(System.Threading.CancellationToken));
    }

    internal void ValidateImage(JpegCoefficientImage image)
    {
        if (image == null)
        {
            throw new System.ArgumentNullException("image");
        }

        if (image.Components == null || image.Components.Count <= 0)
        {
            throw JpegWriteValidationException.Create("Coefficient image must contain components.");
        }

        if (image.QuantizationTables == null || image.QuantizationTables.Count <= 0)
        {
            throw JpegWriteValidationException.Create("Coefficient image must contain quantization tables.");
        }

        if (image.HuffmanTables == null || image.HuffmanTables.Count <= 0)
        {
            throw JpegWriteValidationException.Create("Coefficient image must contain Huffman tables.");
        }

        if (image.ScanHeaders == null)
        {
            throw JpegWriteValidationException.Create("Coefficient scan-header collection cannot be null.");
        }

        if (image.ArithmeticConditioningTables == null)
        {
            throw JpegWriteValidationException.Create("Coefficient arithmetic-conditioning table collection cannot be null.");
        }

        JpegFrameHeader frame = ResolveFrame(image);
        JpegTableStore tables = CreateTableStore(image);
        ValidateCodingModel(image, frame);
        ValidateSummaryFields(image, frame);
        ValidateRestartFields(image);
        ValidateTableModels(tables);
        ValidateCoefficientComponents(image, frame);
        PrepareCallerHuffmanTables(image.HuffmanTables);
        if (IsProgressiveModel(image, frame))
        {
            ValidateProgressiveModel(image, frame, tables);
            return;
        }

        if (image.ScanHeaders.Count > 1)
        {
            throw JpegUnsupportedException.Create("Sequential coefficient writing supports one interleaved scan.");
        }

        JpegScanHeader scan = ResolveScan(image);
        if (IsExtendedSequentialModel(image, frame))
        {
            JpegExtendedSequentialSof1Decoder validation = new JpegExtendedSequentialSof1Decoder();
            validation.ValidateFrame(frame);
            validation.ValidateScan(frame, scan);
            ValidateTableReferences(frame, scan, tables);
            return;
        }

        BaselineValidation.ValidateFrame(frame);
        BaselineValidation.ValidateScan(frame, scan);
        ValidateTableReferences(frame, scan, tables);
    }

    internal void ValidateCodingModel(JpegCoefficientImage image, JpegFrameHeader frame)
    {
        if ((image.Process != JpegProcess.Unknown
                && image.Process != JpegProcess.BaselineDct
                && image.Process != JpegProcess.ExtendedSequentialDct
                && image.Process != JpegProcess.ProgressiveDct)
            || (frame.Process != JpegProcess.Unknown
                && frame.Process != JpegProcess.BaselineDct
                && frame.Process != JpegProcess.ExtendedSequentialDct
                && frame.Process != JpegProcess.ProgressiveDct)
            || image.ArithmeticCoding
            || frame.ArithmeticCoding
            || image.EntropyCoding == JpegEntropyCoding.Arithmetic)
        {
            throw JpegUnsupportedException.Create("Coefficient writing currently supports baseline SOF0, extended sequential SOF1, and progressive SOF2 Huffman JPEG coefficients.");
        }

        if (image.EntropyCoding != JpegEntropyCoding.Automatic && image.EntropyCoding != JpegEntropyCoding.Huffman)
        {
            throw JpegWriteValidationException.Create("Coefficient entropy coding is invalid.");
        }

        int precision = image.Precision;
        if (precision == 0)
        {
            precision = frame.Precision;
        }

        bool extended = IsExtendedSequentialModel(image, frame);
        if (extended && (precision != 12 || frame.Precision != 12))
        {
            throw JpegUnsupportedException.Create("Extended sequential coefficient writing requires 12-bit precision.");
        }

        if (!extended && (precision != 8 || frame.Precision != 8))
        {
            throw JpegUnsupportedException.Create("Baseline and progressive coefficient writing requires 8-bit precision.");
        }

        bool progressive = IsProgressiveModel(image, frame);
        if (progressive
            && ((image.Process != JpegProcess.Unknown && image.Process != JpegProcess.ProgressiveDct)
                || (frame.Process != JpegProcess.Unknown && frame.Process != JpegProcess.ProgressiveDct)))
        {
            throw JpegWriteValidationException.Create("Progressive coefficient flags conflict with the selected JPEG process.");
        }
    }

    internal bool IsProgressiveModel(JpegCoefficientImage image, JpegFrameHeader frame)
    {
        return image.Process == JpegProcess.ProgressiveDct
            || frame.Process == JpegProcess.ProgressiveDct
            || image.Progressive
            || frame.Progressive;
    }

    internal bool IsExtendedSequentialModel(JpegCoefficientImage image, JpegFrameHeader frame)
    {
        return image.Process == JpegProcess.ExtendedSequentialDct
            || frame.Process == JpegProcess.ExtendedSequentialDct;
    }

    internal void ValidateProgressiveModel(JpegCoefficientImage image, JpegFrameHeader frame, JpegTableStore tables)
    {
        if (image.ScanHeaders.Count <= 0)
        {
            throw JpegWriteValidationException.Create("Progressive coefficient writing requires a complete scan script.");
        }

        ProgressiveEncoder.ValidateFrame(frame);
        ProgressiveEncoder.ValidateScanScript(frame, image.ScanHeaders);
        foreach (JpegScanHeader scan in image.ScanHeaders)
        {
            ValidateTableReferences(frame, scan, tables);
        }
    }

    internal void ValidateSummaryFields(JpegCoefficientImage image, JpegFrameHeader frame)
    {
        if (frame.Width <= 0 || frame.Height <= 0)
        {
            throw JpegWriteValidationException.Create("Coefficient frame dimensions must be greater than zero.");
        }

        if (image.Width != 0 && image.Width != frame.Width)
        {
            throw JpegWriteValidationException.Create("Coefficient image width must match the frame width.");
        }

        if (image.Height != 0 && image.Height != frame.Height)
        {
            throw JpegWriteValidationException.Create("Coefficient image height must match the frame height.");
        }

        if (image.Precision != 0 && image.Precision != frame.Precision)
        {
            throw JpegWriteValidationException.Create("Coefficient image precision must match the frame precision.");
        }

        if (image.ComponentCount != 0 && image.ComponentCount != frame.Components.Count)
        {
            throw JpegWriteValidationException.Create("Coefficient image component count must match the frame component count.");
        }

        if (image.Process != JpegProcess.Unknown
            && frame.Process != JpegProcess.Unknown
            && image.Process != frame.Process)
        {
            throw JpegWriteValidationException.Create("Coefficient image process must match the frame process.");
        }
    }

    internal void ValidateRestartFields(JpegCoefficientImage image)
    {
        if (image.RestartIntervalUnit != JpegRestartIntervalUnit.McuBlocks)
        {
            throw JpegWriteValidationException.Create("Coefficient restart intervals must be measured in encoded MCU blocks.");
        }

        if (image.RestartInterval < 0 || image.RestartInterval > 65535)
        {
            throw JpegWriteValidationException.Create("Coefficient restart interval must be between zero and 65535.");
        }

        if (!image.HasRestartInterval && image.RestartInterval != 0)
        {
            throw JpegWriteValidationException.Create("A coefficient restart interval requires HasRestartInterval to be true.");
        }
    }

    internal void ValidateTableModels(JpegTableStore tables)
    {
        new JpegQuantizationTableWriter().WritePayloads(tables.QuantizationTables);
        new JpegHuffmanTableWriter().WritePayloads(tables.HuffmanTables);
        new JpegArithmeticConditioningTableWriter().WritePayloads(tables.ArithmeticConditioningTables);
        ValidateUniqueQuantizationIdentifiers(tables.QuantizationTables);
        ValidateUniqueHuffmanIdentifiers(tables.HuffmanTables);
        ValidateUniqueArithmeticIdentifiers(tables.ArithmeticConditioningTables);
    }

    internal void ValidateUniqueQuantizationIdentifiers(System.Collections.Generic.List<JpegQuantizationTable> tables)
    {
        int index = 0;
        while (index < tables.Count)
        {
            int previous = 0;
            while (previous < index)
            {
                if (tables[previous].Identifier == tables[index].Identifier)
                {
                    throw JpegWriteValidationException.Create("Coefficient quantization table identifiers must be unique.");
                }

                previous = previous + 1;
            }

            index = index + 1;
        }
    }

    internal void ValidateUniqueHuffmanIdentifiers(System.Collections.Generic.List<JpegHuffmanTable> tables)
    {
        int index = 0;
        while (index < tables.Count)
        {
            int previous = 0;
            while (previous < index)
            {
                if (tables[previous].TableClass == tables[index].TableClass
                    && tables[previous].Identifier == tables[index].Identifier)
                {
                    throw JpegWriteValidationException.Create("Coefficient Huffman table class and identifier pairs must be unique.");
                }

                previous = previous + 1;
            }

            index = index + 1;
        }
    }

    internal void ValidateUniqueArithmeticIdentifiers(System.Collections.Generic.List<JpegArithmeticConditioningTable> tables)
    {
        int index = 0;
        while (index < tables.Count)
        {
            int previous = 0;
            while (previous < index)
            {
                if (tables[previous].TableClass == tables[index].TableClass
                    && tables[previous].Identifier == tables[index].Identifier)
                {
                    throw JpegWriteValidationException.Create("Coefficient arithmetic-conditioning table class and identifier pairs must be unique.");
                }

                previous = previous + 1;
            }

            index = index + 1;
        }
    }

    internal void ValidateTableReferences(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables)
    {
        foreach (JpegFrameComponent component in frame.Components)
        {
            if (!ContainsQuantizationTable(tables.QuantizationTables, component.QuantizationTableIdentifier))
            {
                throw JpegWriteValidationException.Create("A coefficient frame component references a missing quantization table.");
            }
        }

        foreach (JpegScanComponent component in scan.Components)
        {
            if (!ContainsHuffmanTable(tables.HuffmanTables, 0, component.DcEntropyTableIdentifier)
                || !ContainsHuffmanTable(tables.HuffmanTables, 1, component.AcEntropyTableIdentifier))
            {
                throw JpegWriteValidationException.Create("A coefficient scan component references a missing Huffman table.");
            }
        }
    }

    internal bool ContainsQuantizationTable(System.Collections.Generic.List<JpegQuantizationTable> tables, int identifier)
    {
        foreach (JpegQuantizationTable table in tables)
        {
            if (table.Identifier == identifier)
            {
                return true;
            }
        }

        return false;
    }

    internal bool ContainsHuffmanTable(System.Collections.Generic.List<JpegHuffmanTable> tables, int tableClass, int identifier)
    {
        foreach (JpegHuffmanTable table in tables)
        {
            if (table.TableClass == tableClass && table.Identifier == identifier)
            {
                return true;
            }
        }

        return false;
    }

    internal void ValidateCoefficientComponents(JpegCoefficientImage image, JpegFrameHeader frame)
    {
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        if (image.Components.Count != layout.ComponentCount)
        {
            throw JpegWriteValidationException.Create("Coefficient component count must match the frame component count.");
        }

        int index = 0;
        while (index < layout.ComponentCount)
        {
            JpegCoefficientComponent component = image.Components[index];
            JpegFrameComponent frameComponent = frame.Components[index];
            JpegMcuComponentLayout componentLayout = layout.Components[index];
            ValidateComponent(component);
            if (component.ComponentIndex != index
                || component.Identifier != frameComponent.Identifier
                || component.HorizontalSamplingFactor != frameComponent.HorizontalSamplingFactor
                || component.VerticalSamplingFactor != frameComponent.VerticalSamplingFactor
                || component.QuantizationTableIdentifier != frameComponent.QuantizationTableIdentifier
                || component.BlocksPerRow != componentLayout.BlocksPerRow
                || component.BlockRows != componentLayout.BlockRows)
            {
                throw JpegWriteValidationException.Create("Coefficient component layout must match its frame component and MCU geometry.");
            }

            index = index + 1;
        }
    }

    internal void PrepareCallerHuffmanTables(System.Collections.Generic.List<JpegHuffmanTable> tables)
    {
        JpegHuffmanTableWriter writer = new JpegHuffmanTableWriter();
        JpegHuffmanTableParser parser = new JpegHuffmanTableParser();
        foreach (JpegHuffmanTable table in tables)
        {
            writer.ValidateTable(table);
            parser.PrepareCallerTable(table);
        }
    }

    internal JpegFrameHeader ResolveFrame(JpegCoefficientImage image)
    {
        if (image.Frame != null && image.Frame.Components != null && image.Frame.Components.Count > 0)
        {
            return image.Frame;
        }

        JpegFrameHeader frame = new JpegFrameHeader();
        bool progressive = image.Process == JpegProcess.ProgressiveDct || image.Progressive;
        bool extended = image.Process == JpegProcess.ExtendedSequentialDct;
        frame.Marker = JpegMarker.StartOfFrame0;
        frame.Value = JpegConstants.MarkerStartOfFrame0;
        frame.Process = JpegProcess.BaselineDct;
        if (extended)
        {
            frame.Marker = JpegMarker.StartOfFrame1;
            frame.Value = JpegConstants.MarkerStartOfFrame1;
            frame.Process = JpegProcess.ExtendedSequentialDct;
        }

        if (progressive)
        {
            frame.Marker = JpegMarker.StartOfFrame2;
            frame.Value = JpegConstants.MarkerStartOfFrame2;
            frame.Process = JpegProcess.ProgressiveDct;
        }

        frame.Width = image.Width;
        frame.Height = image.Height;
        frame.Precision = image.Precision;
        if (frame.Precision == 0)
        {
            frame.Precision = extended ? 12 : 8;
        }
        frame.ComponentCount = image.Components.Count;
        frame.Progressive = progressive;

        int index = 0;
        while (index < image.Components.Count)
        {
            JpegCoefficientComponent source = image.Components[index];
            JpegFrameComponent component = new JpegFrameComponent();
            component.Identifier = source.Identifier;
            component.HorizontalSamplingFactor = source.HorizontalSamplingFactor;
            component.VerticalSamplingFactor = source.VerticalSamplingFactor;
            component.QuantizationTableIdentifier = source.QuantizationTableIdentifier;
            frame.Components.Add(component);
            index = index + 1;
        }

        return frame;
    }

    internal byte[] EncodeExtendedEntropy(
        JpegCoefficientImage image,
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables)
    {
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegSequentialHuffmanEntropyEncoder entropyEncoder = new JpegSequentialHuffmanEntropyEncoder();
        entropyEncoder.ConfigureRestartInterval(GetRestartInterval(image));
        int mcuY = 0;
        while (mcuY < layout.McuRows)
        {
            int mcuX = 0;
            while (mcuX < layout.McusPerRow)
            {
                EncodeExtendedMcu(image, scan, tables, layout, entropyEncoder, mcuX, mcuY);
                entropyEncoder.RecordMcuEncoded();
                WriteExtendedRestartMarkerIfExpected(entropyEncoder, layout, mcuX, mcuY);
                mcuX = mcuX + 1;
            }

            mcuY = mcuY + 1;
        }

        entropyEncoder.FlushToByteBoundary();
        return entropyEncoder.ToArray();
    }

    internal void EncodeExtendedMcu(
        JpegCoefficientImage image,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegMcuLayout layout,
        JpegSequentialHuffmanEntropyEncoder entropyEncoder,
        int mcuX,
        int mcuY)
    {
        int scanComponentIndex = 0;
        while (scanComponentIndex < scan.Components.Count)
        {
            JpegScanComponent scanComponent = scan.Components[scanComponentIndex];
            JpegMcuComponentLayout componentLayout = LayoutCalculator.FindComponentLayout(layout, scanComponent.Selector);
            EncodeExtendedMcuComponent(image, scanComponent, componentLayout, tables, entropyEncoder, mcuX, mcuY);
            scanComponentIndex = scanComponentIndex + 1;
        }
    }

    internal void EncodeExtendedMcuComponent(
        JpegCoefficientImage image,
        JpegScanComponent scanComponent,
        JpegMcuComponentLayout componentLayout,
        JpegTableStore tables,
        JpegSequentialHuffmanEntropyEncoder entropyEncoder,
        int mcuX,
        int mcuY)
    {
        int verticalIndex = 0;
        while (verticalIndex < componentLayout.VerticalSamplingFactor)
        {
            int horizontalIndex = 0;
            while (horizontalIndex < componentLayout.HorizontalSamplingFactor)
            {
                int blockX = mcuX * componentLayout.HorizontalSamplingFactor + horizontalIndex;
                int blockY = mcuY * componentLayout.VerticalSamplingFactor + verticalIndex;
                int[] block = CreateEmptyBlock();
                if (blockX < componentLayout.BlocksPerRow && blockY < componentLayout.BlockRows)
                {
                    JpegCoefficientComponent component = CoefficientReader.FindComponent(image, scanComponent.Selector);
                    block = CopyBlockFromComponent(component, blockX, blockY);
                }

                entropyEncoder.EncodeBlockForScanComponent(
                    tables.HuffmanTables,
                    scanComponent,
                    block,
                    componentLayout.ComponentIndex);
                horizontalIndex = horizontalIndex + 1;
            }

            verticalIndex = verticalIndex + 1;
        }
    }

    internal void WriteExtendedRestartMarkerIfExpected(
        JpegSequentialHuffmanEntropyEncoder entropyEncoder,
        JpegMcuLayout layout,
        int mcuX,
        int mcuY)
    {
        if (!entropyEncoder.ShouldWriteRestartMarker())
        {
            return;
        }

        if (mcuY == layout.McuRows - 1 && mcuX == layout.McusPerRow - 1)
        {
            return;
        }

        entropyEncoder.WriteRestartMarker();
    }

    internal byte[] WriteProgressive(JpegCoefficientImage image, JpegFrameHeader frame, JpegTableStore tables)
    {
        JpegProgressiveCoefficientBuffer coefficients = CreateProgressiveBuffer(image, frame);
        System.Collections.Generic.List<JpegProgressiveScanData> scans = ProgressiveEncoder.EncodeScanScript(
            frame,
            tables,
            coefficients,
            image.ScanHeaders,
            GetRestartInterval(image));
        return WriteProgressiveContainer(image, frame, tables, scans);
    }

    internal JpegProgressiveCoefficientBuffer CreateProgressiveBuffer(JpegCoefficientImage image, JpegFrameHeader frame)
    {
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegProgressiveCoefficientBuffer result = ProgressiveEncoder.ProgressiveDecoder.AllocateCoefficientBuffer(layout);
        foreach (JpegCoefficientComponent source in image.Components)
        {
            JpegProgressiveCoefficientComponentBuffer destination =
                ProgressiveEncoder.ProgressiveDecoder.FindCoefficientComponentBuffer(result, source.Identifier);
            if (source.BlocksPerRow != destination.BlocksPerRow
                || source.BlockRows != destination.BlockRows
                || source.Coefficients.Length != destination.Coefficients.Length)
            {
                throw JpegWriteValidationException.Create("Progressive coefficient layout does not match the frame geometry.");
            }

            System.Array.Copy(source.Coefficients, 0, destination.Coefficients, 0, source.Coefficients.Length);
        }

        return result;
    }

    internal JpegScanHeader ResolveScan(JpegCoefficientImage image)
    {
        if (image.ScanHeaders != null && image.ScanHeaders.Count > 0)
        {
            return image.ScanHeaders[0];
        }

        JpegScanHeader scan = new JpegScanHeader();
        scan.ComponentCount = image.Components.Count;
        scan.SpectralStart = 0;
        scan.SpectralEnd = 63;
        scan.SuccessiveHigh = 0;
        scan.SuccessiveLow = 0;

        int index = 0;
        while (index < image.Components.Count)
        {
            JpegScanComponent component = new JpegScanComponent();
            component.Selector = image.Components[index].Identifier;
            component.DcEntropyTableIdentifier = 0;
            component.AcEntropyTableIdentifier = 0;
            scan.Components.Add(component);
            index = index + 1;
        }

        return scan;
    }

    internal JpegTableStore CreateTableStore(JpegCoefficientImage image)
    {
        JpegTableStore tables = new JpegTableStore();
        tables.QuantizationTables = image.QuantizationTables;
        tables.HuffmanTables = image.HuffmanTables;
        tables.ArithmeticConditioningTables = image.ArithmeticConditioningTables;
        return tables;
    }

    internal byte[] EncodeEntropy(JpegCoefficientImage image, JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables)
    {
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegBaselineHuffmanEntropyEncoder entropyEncoder = new JpegBaselineHuffmanEntropyEncoder();
        entropyEncoder.ConfigureRestartInterval(GetRestartInterval(image));

        int mcuY = 0;
        while (mcuY < layout.McuRows)
        {
            int mcuX = 0;
            while (mcuX < layout.McusPerRow)
            {
                EncodeMcu(image, scan, tables, layout, entropyEncoder, mcuX, mcuY);
                entropyEncoder.RecordMcuEncoded();
                WriteRestartMarkerIfExpected(entropyEncoder, layout, mcuX, mcuY);
                mcuX = mcuX + 1;
            }

            mcuY = mcuY + 1;
        }

        entropyEncoder.FlushToByteBoundary();
        return entropyEncoder.ToArray();
    }

    internal void EncodeMcu(
        JpegCoefficientImage image,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegMcuLayout layout,
        JpegBaselineHuffmanEntropyEncoder entropyEncoder,
        int mcuX,
        int mcuY)
    {
        int scanComponentIndex = 0;
        while (scanComponentIndex < scan.Components.Count)
        {
            JpegScanComponent scanComponent = scan.Components[scanComponentIndex];
            JpegMcuComponentLayout componentLayout = LayoutCalculator.FindComponentLayout(layout, scanComponent.Selector);
            EncodeMcuComponent(image, scanComponent, componentLayout, tables, entropyEncoder, mcuX, mcuY);
            scanComponentIndex = scanComponentIndex + 1;
        }
    }

    internal void EncodeMcuComponent(
        JpegCoefficientImage image,
        JpegScanComponent scanComponent,
        JpegMcuComponentLayout componentLayout,
        JpegTableStore tables,
        JpegBaselineHuffmanEntropyEncoder entropyEncoder,
        int mcuX,
        int mcuY)
    {
        int verticalIndex = 0;
        while (verticalIndex < componentLayout.VerticalSamplingFactor)
        {
            int horizontalIndex = 0;
            while (horizontalIndex < componentLayout.HorizontalSamplingFactor)
            {
                int blockX = mcuX * componentLayout.HorizontalSamplingFactor + horizontalIndex;
                int blockY = mcuY * componentLayout.VerticalSamplingFactor + verticalIndex;
                int[] block = CreateEmptyBlock();
                if (blockX < componentLayout.BlocksPerRow && blockY < componentLayout.BlockRows)
                {
                    JpegCoefficientComponent component = CoefficientReader.FindComponent(image, scanComponent.Selector);
                    block = CopyBlockFromComponent(component, blockX, blockY);
                }

                entropyEncoder.EncodeBlockForScanComponent(tables.HuffmanTables, scanComponent, block, componentLayout.ComponentIndex);
                horizontalIndex = horizontalIndex + 1;
            }

            verticalIndex = verticalIndex + 1;
        }
    }

    internal int[] CopyBlockFromComponent(JpegCoefficientComponent component, int blockX, int blockY)
    {
        ValidateComponent(component);
        int offset = CoefficientReader.GetCoefficientOffset(component, blockX, blockY);
        int[] block = new int[JpegConstants.BlockSampleCount];
        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            block[index] = component.Coefficients[offset + index];
            index = index + 1;
        }

        return block;
    }

    internal int[] CreateEmptyBlock()
    {
        return new int[JpegConstants.BlockSampleCount];
    }

    internal void ValidateComponent(JpegCoefficientComponent component)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        if (component.BlocksPerRow <= 0 || component.BlockRows <= 0)
        {
            throw JpegWriteValidationException.Create("Coefficient component block dimensions must be greater than zero.");
        }

        long blockCount = (long)component.BlocksPerRow * (long)component.BlockRows;
        if (blockCount > int.MaxValue / JpegConstants.BlockSampleCount)
        {
            throw JpegWriteValidationException.Create("Coefficient component buffer is too large.");
        }

        long minimumLength = blockCount * JpegConstants.BlockSampleCount;
        if (component.Coefficients == null || component.Coefficients.Length < minimumLength)
        {
            throw JpegWriteValidationException.Create("Coefficient component buffer is too small.");
        }
    }

    internal int GetRestartInterval(JpegCoefficientImage image)
    {
        if (image.HasRestartInterval)
        {
            return image.RestartInterval;
        }

        return 0;
    }

    internal void WriteRestartMarkerIfExpected(JpegBaselineHuffmanEntropyEncoder entropyEncoder, JpegMcuLayout layout, int mcuX, int mcuY)
    {
        if (!entropyEncoder.ShouldWriteRestartMarker())
        {
            return;
        }

        if (mcuY == layout.McuRows - 1 && mcuX == layout.McusPerRow - 1)
        {
            return;
        }

        entropyEncoder.WriteRestartMarker();
    }

    internal byte[] WriteContainer(
        JpegCoefficientImage image,
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        byte[] entropyData)
    {
        JpegMarkerSegmentWriter writer = new JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        WriteMetadata(writer, image);
        new JpegQuantizationTableWriter().WriteMarkerSegment(writer, tables.QuantizationTables);
        new JpegHuffmanTableWriter().WriteMarkerSegment(writer, tables.HuffmanTables);
        if (tables.ArithmeticConditioningTables.Count > 0)
        {
            new JpegArithmeticConditioningTableWriter().WriteMarkerSegment(writer, tables.ArithmeticConditioningTables);
        }

        if (image.HasRestartInterval)
        {
            new JpegRestartIntervalWriter().WriteMarkerSegment(writer, GetRestartInterval(image));
        }

        new JpegFrameHeaderWriter().WriteMarkerSegment(writer, frame);
        new JpegScanHeaderWriter().WriteMarkerSegment(writer, scan);
        writer.Writer.WriteBytes(entropyData);
        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    internal byte[] WriteProgressiveContainer(
        JpegCoefficientImage image,
        JpegFrameHeader frame,
        JpegTableStore tables,
        System.Collections.Generic.List<JpegProgressiveScanData> scans)
    {
        JpegMarkerSegmentWriter writer = new JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        WriteMetadata(writer, image);
        new JpegQuantizationTableWriter().WriteMarkerSegment(writer, tables.QuantizationTables);
        new JpegHuffmanTableWriter().WriteMarkerSegment(writer, tables.HuffmanTables);
        if (tables.ArithmeticConditioningTables.Count > 0)
        {
            new JpegArithmeticConditioningTableWriter().WriteMarkerSegment(writer, tables.ArithmeticConditioningTables);
        }

        if (image.HasRestartInterval)
        {
            new JpegRestartIntervalWriter().WriteMarkerSegment(writer, GetRestartInterval(image));
        }

        new JpegFrameHeaderWriter().WriteMarkerSegment(writer, frame);
        foreach (JpegProgressiveScanData scan in scans)
        {
            new JpegScanHeaderWriter().WriteMarkerSegment(writer, scan.Scan);
            writer.Writer.WriteBytes(scan.EntropyData);
        }

        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    internal void WriteMetadata(JpegMarkerSegmentWriter writer, JpegCoefficientImage image)
    {
        if (image.Metadata == null)
        {
            return;
        }

        new JpegMetadataWriter().WriteMarkerSegments(writer, image.Metadata);
    }
}
