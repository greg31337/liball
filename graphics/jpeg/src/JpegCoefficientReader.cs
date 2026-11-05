namespace jpeg;

internal class JpegCoefficientReader
{
    internal JpegImageReader ImageReader = new JpegImageReader();
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();

    internal JpegCoefficientImage Read(byte[] data)
    {
        return ReadWithOptions(data, new JpegReadOptions());
    }

    internal JpegCoefficientImage ReadWithOptions(byte[] data, JpegReadOptions options)
    {
        JpegIdentifier identifier = JpegIdentifier.Create(data, options);
        JpegInfo info = identifier.Read();
        ValidateCoefficientReadSupport(info);

        JpegTableStore tables = ImageReader.CreateTableStore(info);
        ValidateQuantizationTables(info.Frame, tables);
        JpegMcuLayout layout = LayoutCalculator.Calculate(info.Frame);
        JpegCoefficientImage image = AllocateCoefficientImage(info, layout);
        image.Markers = identifier.InspectedMarkers;
        if (info.Process == JpegProcess.ProgressiveDct)
        {
            DecodeProgressiveCoefficients(data, options, info, tables, image);
            return image;
        }

        ValidateSequentialScan(info.Frame, info.LastScanHeader);
        byte[] entropyData = ImageReader.ExtractSingleScanEntropyData(data, options);
        if (info.Process == JpegProcess.ExtendedSequentialDct)
        {
            DecodeExtendedCoefficients(
                info.Frame,
                info.LastScanHeader,
                tables,
                entropyData,
                info.RestartInterval,
                layout,
                image);
            return image;
        }

        DecodeBaselineCoefficients(info.Frame, info.LastScanHeader, tables, entropyData, info.RestartInterval, layout, image);
        return image;
    }

    internal void ValidateCoefficientReadSupport(JpegInfo info)
    {
        ImageReader.ValidateReadSupport(info);
        if (info.Process != JpegProcess.BaselineDct
            && info.Process != JpegProcess.ExtendedSequentialDct
            && info.Process != JpegProcess.ProgressiveDct)
        {
            throw JpegUnsupportedException.Create("Coefficient reading currently supports baseline SOF0, extended sequential SOF1, and progressive SOF2 JPEG data.");
        }
    }

    internal void ValidateQuantizationTables(JpegFrameHeader frame, JpegTableStore tables)
    {
        JpegBaselineSof0FullImageDecoder decoder = new JpegBaselineSof0FullImageDecoder();
        foreach (JpegFrameComponent component in frame.Components)
        {
            decoder.FindQuantizationTable(tables, component.QuantizationTableIdentifier);
        }
    }

    internal void ValidateSequentialScan(JpegFrameHeader frame, JpegScanHeader scan)
    {
        new JpegBaselineSof0FullImageDecoder().ValidateScan(frame, scan);
    }

    internal void DecodeProgressiveCoefficients(
        byte[] data,
        JpegReadOptions options,
        JpegInfo info,
        JpegTableStore tables,
        JpegCoefficientImage image)
    {
        System.Collections.Generic.List<JpegProgressiveScanData> scans = ImageReader.ExtractProgressiveScanData(
            data,
            options,
            default(System.Threading.CancellationToken));
        JpegProgressiveCoefficientBuffer coefficients = new JpegProgressiveSof2FullImageDecoder().DecodeCoefficientsWithOptions(
            info.Frame,
            scans,
            tables,
            info.RestartInterval,
            options);
        CopyProgressiveCoefficients(coefficients, image);
    }

    internal void CopyProgressiveCoefficients(JpegProgressiveCoefficientBuffer source, JpegCoefficientImage destination)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        foreach (JpegProgressiveCoefficientComponentBuffer sourceComponent in source.Components)
        {
            JpegCoefficientComponent destinationComponent = FindComponent(destination, sourceComponent.Identifier);
            if (sourceComponent.BlocksPerRow != destinationComponent.BlocksPerRow
                || sourceComponent.BlockRows != destinationComponent.BlockRows
                || sourceComponent.Coefficients.Length != destinationComponent.Coefficients.Length)
            {
                throw JpegInvalidDataException.Create("Progressive coefficient layout does not match the public coefficient image.");
            }

            System.Array.Copy(
                sourceComponent.Coefficients,
                0,
                destinationComponent.Coefficients,
                0,
                sourceComponent.Coefficients.Length);
        }
    }

    internal JpegCoefficientImage ReadStream(System.IO.Stream stream)
    {
        return ReadStreamWithOptions(stream, new JpegReadOptions());
    }

    internal JpegCoefficientImage ReadStreamWithOptions(System.IO.Stream stream, JpegReadOptions options)
    {
        byte[] data = ImageReader.ReadAllBytes(stream);
        return ReadWithOptions(data, options);
    }

    internal JpegCoefficientImage AllocateCoefficientImage(JpegInfo info, JpegMcuLayout layout)
    {
        JpegCoefficientImage image = new JpegCoefficientImage();
        image.Width = info.Width;
        image.Height = info.Height;
        image.Precision = info.Precision;
        image.ComponentCount = info.ComponentCount;
        image.Process = info.Process;
        image.EntropyCoding = info.ArithmeticCoding ? JpegEntropyCoding.Arithmetic : JpegEntropyCoding.Huffman;
        image.Progressive = info.Progressive;
        image.ArithmeticCoding = info.ArithmeticCoding;
        image.ColorSpace = info.ColorSpace;
        image.Subsampling = info.Subsampling;
        image.Frame = info.Frame;
        image.ScanHeaders = info.ScanHeaders;
        image.QuantizationTables = info.QuantizationTables;
        image.HuffmanTables = info.HuffmanTables;
        image.ArithmeticConditioningTables = info.ArithmeticConditioningTables;
        image.HasRestartInterval = info.HasRestartInterval;
        image.RestartInterval = info.RestartInterval;
        image.RestartIntervalUnit = JpegRestartIntervalUnit.McuBlocks;
        image.RestartMarkerCount = info.RestartMarkerCount;
        image.Metadata = info.Metadata;
        image.Diagnostics = info.Diagnostics;
        image.DiagnosticCollection = info.DiagnosticCollection;

        int index = 0;
        while (index < layout.ComponentCount)
        {
            image.Components.Add(AllocateComponent(info.Frame.Components[index], layout.Components[index], index));
            index = index + 1;
        }

        return image;
    }

    internal JpegCoefficientComponent AllocateComponent(JpegFrameComponent frameComponent, JpegMcuComponentLayout layout, int componentIndex)
    {
        JpegCoefficientComponent component = new JpegCoefficientComponent();
        component.ComponentIndex = componentIndex;
        component.Identifier = frameComponent.Identifier;
        component.HorizontalSamplingFactor = frameComponent.HorizontalSamplingFactor;
        component.VerticalSamplingFactor = frameComponent.VerticalSamplingFactor;
        component.QuantizationTableIdentifier = frameComponent.QuantizationTableIdentifier;
        component.BlocksPerRow = layout.BlocksPerRow;
        component.BlockRows = layout.BlockRows;
        component.Coefficients = new int[layout.TotalBlockCount * JpegConstants.BlockSampleCount];
        return component;
    }

    internal void DecodeBaselineCoefficients(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        byte[] entropyData,
        int restartInterval,
        JpegMcuLayout layout,
        JpegCoefficientImage image)
    {
        JpegBaselineHuffmanEntropyDecoder entropyDecoder = new JpegBaselineHuffmanEntropyDecoder(entropyData);
        entropyDecoder.ConfigureRestartInterval(restartInterval);

        int mcuY = 0;
        while (mcuY < layout.McuRows)
        {
            int mcuX = 0;
            while (mcuX < layout.McusPerRow)
            {
                DecodeMcu(frame, scan, tables, layout, image, entropyDecoder, mcuX, mcuY);
                entropyDecoder.RecordMcuDecoded();
                ConsumeRestartMarkerIfExpected(entropyDecoder, layout, mcuX, mcuY);
                mcuX = mcuX + 1;
            }

            mcuY = mcuY + 1;
        }
    }

    internal void DecodeExtendedCoefficients(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        byte[] entropyData,
        int restartInterval,
        JpegMcuLayout layout,
        JpegCoefficientImage image)
    {
        JpegSequentialHuffmanEntropyDecoder entropyDecoder = new JpegSequentialHuffmanEntropyDecoder(entropyData);
        entropyDecoder.ConfigureRestartInterval(restartInterval);
        int mcuY = 0;
        while (mcuY < layout.McuRows)
        {
            int mcuX = 0;
            while (mcuX < layout.McusPerRow)
            {
                DecodeExtendedMcu(scan, tables, layout, image, entropyDecoder, mcuX, mcuY);
                entropyDecoder.RecordMcuDecoded();
                ConsumeExtendedRestartMarkerIfExpected(entropyDecoder, layout, mcuX, mcuY);
                mcuX = mcuX + 1;
            }

            mcuY = mcuY + 1;
        }
    }

    internal void DecodeExtendedMcu(
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegMcuLayout layout,
        JpegCoefficientImage image,
        JpegSequentialHuffmanEntropyDecoder entropyDecoder,
        int mcuX,
        int mcuY)
    {
        int scanComponentIndex = 0;
        while (scanComponentIndex < scan.Components.Count)
        {
            JpegScanComponent scanComponent = scan.Components[scanComponentIndex];
            JpegMcuComponentLayout componentLayout = LayoutCalculator.FindComponentLayout(layout, scanComponent.Selector);
            DecodeExtendedMcuComponent(scanComponent, componentLayout, tables, image, entropyDecoder, mcuX, mcuY);
            scanComponentIndex = scanComponentIndex + 1;
        }
    }

    internal void DecodeExtendedMcuComponent(
        JpegScanComponent scanComponent,
        JpegMcuComponentLayout componentLayout,
        JpegTableStore tables,
        JpegCoefficientImage image,
        JpegSequentialHuffmanEntropyDecoder entropyDecoder,
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
                int[] coefficients = entropyDecoder.DecodeBlockForScanComponent(
                    tables.HuffmanTables,
                    scanComponent,
                    componentLayout.ComponentIndex);
                if (blockX < componentLayout.BlocksPerRow && blockY < componentLayout.BlockRows)
                {
                    JpegCoefficientComponent component = FindComponent(image, scanComponent.Selector);
                    CopyBlockToComponent(coefficients, component, blockX, blockY);
                }

                horizontalIndex = horizontalIndex + 1;
            }

            verticalIndex = verticalIndex + 1;
        }
    }

    internal void ConsumeExtendedRestartMarkerIfExpected(
        JpegSequentialHuffmanEntropyDecoder entropyDecoder,
        JpegMcuLayout layout,
        int mcuX,
        int mcuY)
    {
        if (!entropyDecoder.ShouldExpectRestartMarker())
        {
            return;
        }

        if (mcuY == layout.McuRows - 1 && mcuX == layout.McusPerRow - 1)
        {
            return;
        }

        if (!entropyDecoder.BitReader.AdvanceToMarker())
        {
            throw JpegInvalidDataException.Create("Expected restart marker was not present in extended sequential entropy data.");
        }

        entropyDecoder.ConsumeRestartMarkerLookahead();
    }

    internal void DecodeMcu(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegMcuLayout layout,
        JpegCoefficientImage image,
        JpegBaselineHuffmanEntropyDecoder entropyDecoder,
        int mcuX,
        int mcuY)
    {
        int scanComponentIndex = 0;
        while (scanComponentIndex < scan.Components.Count)
        {
            JpegScanComponent scanComponent = scan.Components[scanComponentIndex];
            JpegMcuComponentLayout componentLayout = LayoutCalculator.FindComponentLayout(layout, scanComponent.Selector);
            DecodeMcuComponent(scanComponent, componentLayout, tables, image, entropyDecoder, mcuX, mcuY);
            scanComponentIndex = scanComponentIndex + 1;
        }
    }

    internal void DecodeMcuComponent(
        JpegScanComponent scanComponent,
        JpegMcuComponentLayout componentLayout,
        JpegTableStore tables,
        JpegCoefficientImage image,
        JpegBaselineHuffmanEntropyDecoder entropyDecoder,
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
                int[] coefficients = entropyDecoder.DecodeBlockForScanComponent(tables.HuffmanTables, scanComponent, componentLayout.ComponentIndex);
                if (blockX < componentLayout.BlocksPerRow && blockY < componentLayout.BlockRows)
                {
                    JpegCoefficientComponent component = FindComponent(image, scanComponent.Selector);
                    CopyBlockToComponent(coefficients, component, blockX, blockY);
                }

                horizontalIndex = horizontalIndex + 1;
            }

            verticalIndex = verticalIndex + 1;
        }
    }

    internal void ConsumeRestartMarkerIfExpected(JpegBaselineHuffmanEntropyDecoder entropyDecoder, JpegMcuLayout layout, int mcuX, int mcuY)
    {
        if (!entropyDecoder.ShouldExpectRestartMarker())
        {
            return;
        }

        if (mcuY == layout.McuRows - 1 && mcuX == layout.McusPerRow - 1)
        {
            return;
        }

        if (!entropyDecoder.BitReader.AdvanceToMarker())
        {
            throw JpegInvalidDataException.Create("Expected restart marker was not present in entropy data.");
        }

        entropyDecoder.ConsumeRestartMarkerLookahead();
    }

    internal JpegCoefficientComponent FindComponent(JpegCoefficientImage image, int identifier)
    {
        if (image == null)
        {
            throw new System.ArgumentNullException("image");
        }

        foreach (JpegCoefficientComponent component in image.Components)
        {
            if (component.Identifier == identifier)
            {
                return component;
            }
        }

        throw JpegInvalidDataException.Create("Coefficient component was not found.");
    }

    internal void CopyBlockToComponent(int[] block, JpegCoefficientComponent component, int blockX, int blockY)
    {
        if (block == null)
        {
            throw new System.ArgumentNullException("block");
        }

        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        if (block.Length < JpegConstants.BlockSampleCount)
        {
            throw JpegInvalidDataException.Create("Coefficient block must contain 64 entries.");
        }

        int offset = GetCoefficientOffset(component, blockX, blockY);
        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            component.Coefficients[offset + index] = block[index];
            index = index + 1;
        }
    }

    internal int GetCoefficientOffset(JpegCoefficientComponent component, int blockX, int blockY)
    {
        if (blockX < 0 || blockX >= component.BlocksPerRow)
        {
            throw new System.ArgumentOutOfRangeException("blockX");
        }

        if (blockY < 0 || blockY >= component.BlockRows)
        {
            throw new System.ArgumentOutOfRangeException("blockY");
        }

        return (blockY * component.BlocksPerRow + blockX) * JpegConstants.BlockSampleCount;
    }
}
