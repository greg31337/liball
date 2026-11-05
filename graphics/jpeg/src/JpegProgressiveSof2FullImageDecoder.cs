using System.Collections.Generic;

namespace jpeg;

internal class JpegProgressiveScanData
{
    internal JpegScanHeader Scan = new JpegScanHeader();
    internal byte[] EntropyData = new byte[0];
}

internal class JpegProgressiveCoefficientComponentBuffer
{
    internal int ComponentIndex;
    internal int Identifier;
    internal int BlocksPerRow;
    internal int BlockRows;
    internal int[] Coefficients = new int[0];
}

internal class JpegProgressiveCoefficientBuffer
{
    internal int Width;
    internal int Height;
    internal int ComponentCount;
    internal List<JpegProgressiveCoefficientComponentBuffer> Components = new List<JpegProgressiveCoefficientComponentBuffer>();
}

internal class JpegProgressiveScanCallbackUpdate
{
    internal int ScanIndex;
    internal int ScanCount;
    internal bool Completed;
    internal JpegScanHeader Scan = new JpegScanHeader();
    internal JpegProgressiveCoefficientBuffer Coefficients = new JpegProgressiveCoefficientBuffer();
    internal JpegPlanarImageBuffer Preview = new JpegPlanarImageBuffer();
}

internal class JpegProgressiveSof2FullImageDecoder
{
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();
    internal JpegPlanarComponentBufferAllocator PlanarAllocator = new JpegPlanarComponentBufferAllocator();
    internal JpegCoefficientQuantizer Quantizer = new JpegCoefficientQuantizer();
    internal JpegInverseDctTransformer InverseDct = new JpegInverseDctTransformer();
    internal JpegBaselineSof0FullImageDecoder BaselineDecoder = new JpegBaselineSof0FullImageDecoder();

    internal JpegPlanarImageBuffer Decode(JpegFrameHeader frame, List<JpegProgressiveScanData> scans, JpegTableStore tables)
    {
        return DecodeWithOptions(frame, scans, tables, 0, CreateDefaultReadOptions());
    }

    internal JpegPlanarImageBuffer DecodeWithRestartInterval(
        JpegFrameHeader frame,
        List<JpegProgressiveScanData> scans,
        JpegTableStore tables,
        int restartInterval)
    {
        return DecodeWithOptions(frame, scans, tables, restartInterval, CreateDefaultReadOptions());
    }

    internal JpegPlanarImageBuffer DecodeWithOptions(
        JpegFrameHeader frame,
        List<JpegProgressiveScanData> scans,
        JpegTableStore tables,
        int restartInterval,
        JpegReadOptions options)
    {
        JpegProgressiveCoefficientBuffer coefficients = DecodeCoefficientsWithOptions(frame, scans, tables, restartInterval, options);
        return BuildPlanarImage(frame, tables, coefficients, options);
    }

    internal JpegPlanarImageBuffer DecodeWithScanCallback(
        JpegFrameHeader frame,
        List<JpegProgressiveScanData> scans,
        JpegTableStore tables,
        System.Action<JpegProgressiveScanCallbackUpdate> callback)
    {
        return DecodeWithScanCallbackOptions(frame, scans, tables, 0, CreateDefaultReadOptions(), callback);
    }

    internal JpegPlanarImageBuffer DecodeWithScanCallbackOptions(
        JpegFrameHeader frame,
        List<JpegProgressiveScanData> scans,
        JpegTableStore tables,
        int restartInterval,
        JpegReadOptions options,
        System.Action<JpegProgressiveScanCallbackUpdate> callback)
    {
        if (callback == null)
        {
            throw new System.ArgumentNullException("callback");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        ValidateInputs(frame, scans, tables, restartInterval);
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegProgressiveCoefficientBuffer coefficients = AllocateCoefficientBufferWithLimit(layout, options.MaxProgressiveStateBytes);
        JpegPlanarImageBuffer preview = new JpegPlanarImageBuffer();

        int scanIndex = 0;
        while (scanIndex < scans.Count)
        {
            DecodeScan(frame, scans[scanIndex].Scan, tables, scans[scanIndex].EntropyData, restartInterval, layout, coefficients);
            preview = BuildPlanarImage(frame, tables, coefficients, options);
            EnforcePlanarCallbackOutputLimit(preview, options);
            PublishScanCallback(callback, scanIndex, scans.Count, scans[scanIndex].Scan, coefficients, preview);
            scanIndex = scanIndex + 1;
        }

        return preview;
    }

    internal void EnforcePlanarCallbackOutputLimit(JpegPlanarImageBuffer preview, JpegReadOptions options)
    {
        if (options.MaxCallbackOutputBytes < 0)
        {
            return;
        }

        long outputBytes = 0;
        int index = 0;
        while (index < preview.ComponentCount)
        {
            JpegPlanarComponentBuffer component = preview.Components[index];
            outputBytes = outputBytes + component.Samples.LongLength + component.IntSamples.LongLength * 4;
            index = index + 1;
        }

        if (outputBytes > options.MaxCallbackOutputBytes)
        {
            throw JpegResourceLimitException.Create("Progressive scan callback exceeds the configured maximum callback output bytes.");
        }
    }

    internal JpegProgressiveCoefficientBuffer DecodeCoefficients(JpegFrameHeader frame, List<JpegProgressiveScanData> scans, JpegTableStore tables)
    {
        return DecodeCoefficientsWithRestartInterval(frame, scans, tables, 0);
    }

    internal JpegProgressiveCoefficientBuffer DecodeCoefficientsWithRestartInterval(
        JpegFrameHeader frame,
        List<JpegProgressiveScanData> scans,
        JpegTableStore tables,
        int restartInterval)
    {
        ValidateInputs(frame, scans, tables, restartInterval);
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegProgressiveCoefficientBuffer coefficients = AllocateCoefficientBuffer(layout);

        int scanIndex = 0;
        while (scanIndex < scans.Count)
        {
            DecodeScan(frame, scans[scanIndex].Scan, tables, scans[scanIndex].EntropyData, restartInterval, layout, coefficients);
            scanIndex = scanIndex + 1;
        }

        return coefficients;
    }

    internal JpegProgressiveCoefficientBuffer DecodeCoefficientsWithOptions(
        JpegFrameHeader frame,
        List<JpegProgressiveScanData> scans,
        JpegTableStore tables,
        int restartInterval,
        JpegReadOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        ValidateInputs(frame, scans, tables, restartInterval);
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegProgressiveCoefficientBuffer coefficients = AllocateCoefficientBufferWithLimit(layout, options.MaxProgressiveStateBytes);

        int scanIndex = 0;
        while (scanIndex < scans.Count)
        {
            DecodeScan(frame, scans[scanIndex].Scan, tables, scans[scanIndex].EntropyData, restartInterval, layout, coefficients);
            scanIndex = scanIndex + 1;
        }

        return coefficients;
    }

    internal void DecodeScan(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        byte[] entropyData,
        int restartInterval,
        JpegMcuLayout layout,
        JpegProgressiveCoefficientBuffer coefficients)
    {
        JpegProgressiveHuffmanEntropyDecoder entropyDecoder = new JpegProgressiveHuffmanEntropyDecoder(entropyData);
        entropyDecoder.ConfigureRestartInterval(restartInterval);
        if (scan.SpectralStart == 0 && scan.SpectralEnd == 0)
        {
            DecodeDcScan(frame, scan, tables, layout, coefficients, entropyDecoder);
            return;
        }

        DecodeAcScan(scan, tables, layout, coefficients, entropyDecoder);
    }

    internal void DecodeDcScan(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegMcuLayout layout,
        JpegProgressiveCoefficientBuffer coefficients,
        JpegProgressiveHuffmanEntropyDecoder entropyDecoder)
    {
        if (GetScanComponentCount(scan) == 1)
        {
            DecodeSingleComponentDcScan(frame, scan, tables, layout, coefficients, entropyDecoder);
            return;
        }

        DecodeInterleavedDcScan(frame, scan, tables, layout, coefficients, entropyDecoder);
    }

    internal void DecodeInterleavedDcScan(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegMcuLayout layout,
        JpegProgressiveCoefficientBuffer coefficients,
        JpegProgressiveHuffmanEntropyDecoder entropyDecoder)
    {
        int mcuY = 0;
        while (mcuY < layout.McuRows)
        {
            int mcuX = 0;
            while (mcuX < layout.McusPerRow)
            {
                int scanComponentIndex = 0;
                while (scanComponentIndex < scan.Components.Count)
                {
                    DecodeInterleavedDcComponent(frame, scan, tables, layout, coefficients, entropyDecoder, scanComponentIndex, mcuX, mcuY);
                    scanComponentIndex = scanComponentIndex + 1;
                }

                RecordAndConsumeRestart(entropyDecoder, IsLastMcu(layout, mcuX, mcuY));
                mcuX = mcuX + 1;
            }

            mcuY = mcuY + 1;
        }
    }

    internal void DecodeInterleavedDcComponent(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegMcuLayout layout,
        JpegProgressiveCoefficientBuffer coefficients,
        JpegProgressiveHuffmanEntropyDecoder entropyDecoder,
        int scanComponentIndex,
        int mcuX,
        int mcuY)
    {
        JpegScanComponent scanComponent = scan.Components[scanComponentIndex];
        JpegMcuComponentLayout componentLayout = LayoutCalculator.FindComponentLayout(layout, scanComponent.Selector);
        int verticalIndex = 0;
        while (verticalIndex < componentLayout.VerticalSamplingFactor)
        {
            int horizontalIndex = 0;
            while (horizontalIndex < componentLayout.HorizontalSamplingFactor)
            {
                int blockX = mcuX * componentLayout.HorizontalSamplingFactor + horizontalIndex;
                int blockY = mcuY * componentLayout.VerticalSamplingFactor + verticalIndex;
                if (blockX < componentLayout.BlocksPerRow && blockY < componentLayout.BlockRows)
                {
                    DecodeDcBlock(frame, scan, tables, coefficients, entropyDecoder, scanComponent, componentLayout, blockX, blockY);
                }

                horizontalIndex = horizontalIndex + 1;
            }

            verticalIndex = verticalIndex + 1;
        }
    }

    internal void DecodeSingleComponentDcScan(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegMcuLayout layout,
        JpegProgressiveCoefficientBuffer coefficients,
        JpegProgressiveHuffmanEntropyDecoder entropyDecoder)
    {
        JpegScanComponent scanComponent = scan.Components[0];
        JpegMcuComponentLayout componentLayout = LayoutCalculator.FindComponentLayout(layout, scanComponent.Selector);
        int blockY = 0;
        while (blockY < componentLayout.BlockRows)
        {
            int blockX = 0;
            while (blockX < componentLayout.BlocksPerRow)
            {
                DecodeDcBlock(frame, scan, tables, coefficients, entropyDecoder, scanComponent, componentLayout, blockX, blockY);
                RecordAndConsumeRestart(entropyDecoder, blockY == componentLayout.BlockRows - 1 && blockX == componentLayout.BlocksPerRow - 1);
                blockX = blockX + 1;
            }

            blockY = blockY + 1;
        }
    }

    internal void DecodeDcBlock(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegProgressiveCoefficientBuffer coefficients,
        JpegProgressiveHuffmanEntropyDecoder entropyDecoder,
        JpegScanComponent scanComponent,
        JpegMcuComponentLayout componentLayout,
        int blockX,
        int blockY)
    {
        JpegProgressiveCoefficientComponentBuffer componentBuffer = FindCoefficientComponentBuffer(coefficients, scanComponent.Selector);
        int coefficientOffset = GetCoefficientOffset(componentBuffer, blockX, blockY);
        if (scan.SuccessiveHigh == 0)
        {
            int[] block = entropyDecoder.DecodeDcFirstBlockForScanComponent(tables.HuffmanTables, scan, scanComponent, componentLayout.ComponentIndex);
            componentBuffer.Coefficients[coefficientOffset] = block[0];
            return;
        }

        int[] coefficientsBlock = CopyBlockFromCoefficientBuffer(componentBuffer, blockX, blockY);
        entropyDecoder.DecodeDcRefinementBlockForScan(scan, coefficientsBlock);
        CopyBlockToCoefficientBuffer(coefficientsBlock, componentBuffer, blockX, blockY);
    }

    internal void DecodeAcScan(
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegMcuLayout layout,
        JpegProgressiveCoefficientBuffer coefficients,
        JpegProgressiveHuffmanEntropyDecoder entropyDecoder)
    {
        JpegScanComponent scanComponent = scan.Components[0];
        JpegMcuComponentLayout componentLayout = LayoutCalculator.FindComponentLayout(layout, scanComponent.Selector);
        JpegProgressiveCoefficientComponentBuffer componentBuffer = FindCoefficientComponentBuffer(coefficients, scanComponent.Selector);
        JpegHuffmanTable acTable = entropyDecoder.BaselineDecoder.FindTable(tables.HuffmanTables, 1, scanComponent.AcEntropyTableIdentifier);

        int blockY = 0;
        while (blockY < componentLayout.BlockRows)
        {
            int blockX = 0;
            while (blockX < componentLayout.BlocksPerRow)
            {
                int[] coefficientsBlock = CopyBlockFromCoefficientBuffer(componentBuffer, blockX, blockY);
                if (scan.SuccessiveHigh == 0)
                {
                    entropyDecoder.DecodeAcFirstBlockForScan(scan, acTable, coefficientsBlock);
                }
                else
                {
                    entropyDecoder.DecodeAcRefinementBlockForScan(scan, acTable, coefficientsBlock);
                }

                CopyBlockToCoefficientBuffer(coefficientsBlock, componentBuffer, blockX, blockY);
                RecordAndConsumeRestart(entropyDecoder, blockY == componentLayout.BlockRows - 1 && blockX == componentLayout.BlocksPerRow - 1);
                blockX = blockX + 1;
            }

            blockY = blockY + 1;
        }
    }

    internal JpegPlanarImageBuffer BuildPlanarImage(
        JpegFrameHeader frame,
        JpegTableStore tables,
        JpegProgressiveCoefficientBuffer coefficients,
        JpegReadOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegPlanarImageBuffer image = BaselineDecoder.AllocateScaledDestination(layout, options);
        int componentIndex = 0;
        while (componentIndex < layout.ComponentCount)
        {
            JpegMcuComponentLayout componentLayout = layout.Components[componentIndex];
            JpegFrameComponent frameComponent = BaselineDecoder.FindFrameComponent(frame, componentLayout.Identifier);
            JpegQuantizationTable quantizationTable = BaselineDecoder.FindQuantizationTable(tables, frameComponent.QuantizationTableIdentifier);
            JpegProgressiveCoefficientComponentBuffer coefficientComponent = FindCoefficientComponentBuffer(coefficients, componentLayout.Identifier);
            JpegPlanarComponentBuffer componentBuffer = PlanarAllocator.FindComponentBuffer(image, componentLayout.Identifier);
            BuildPlanarComponent(componentLayout, coefficientComponent, componentBuffer, quantizationTable, options);
            componentIndex = componentIndex + 1;
        }

        return image;
    }

    internal void PublishScanCallback(
        System.Action<JpegProgressiveScanCallbackUpdate> callback,
        int scanIndex,
        int scanCount,
        JpegScanHeader scan,
        JpegProgressiveCoefficientBuffer coefficients,
        JpegPlanarImageBuffer preview)
    {
        if (callback == null)
        {
            throw new System.ArgumentNullException("callback");
        }

        JpegProgressiveScanCallbackUpdate update = new JpegProgressiveScanCallbackUpdate();
        update.ScanIndex = scanIndex;
        update.ScanCount = scanCount;
        update.Completed = scanIndex + 1 == scanCount;
        update.Scan = scan;
        update.Coefficients = CloneCoefficientBuffer(coefficients);
        update.Preview = preview;
        callback(update);
    }

    internal JpegProgressiveCoefficientBuffer CloneCoefficientBuffer(JpegProgressiveCoefficientBuffer source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        JpegProgressiveCoefficientBuffer destination = new JpegProgressiveCoefficientBuffer();
        destination.Width = source.Width;
        destination.Height = source.Height;
        destination.ComponentCount = source.ComponentCount;
        int componentIndex = 0;
        while (componentIndex < source.Components.Count)
        {
            destination.Components.Add(CloneCoefficientComponentBuffer(source.Components[componentIndex]));
            componentIndex = componentIndex + 1;
        }

        return destination;
    }

    internal JpegProgressiveCoefficientComponentBuffer CloneCoefficientComponentBuffer(JpegProgressiveCoefficientComponentBuffer source)
    {
        ValidateCoefficientComponent(source);
        JpegProgressiveCoefficientComponentBuffer destination = new JpegProgressiveCoefficientComponentBuffer();
        destination.ComponentIndex = source.ComponentIndex;
        destination.Identifier = source.Identifier;
        destination.BlocksPerRow = source.BlocksPerRow;
        destination.BlockRows = source.BlockRows;
        destination.Coefficients = new int[source.Coefficients.Length];
        int index = 0;
        while (index < source.Coefficients.Length)
        {
            destination.Coefficients[index] = source.Coefficients[index];
            index = index + 1;
        }

        return destination;
    }

    internal void BuildPlanarComponent(
        JpegMcuComponentLayout componentLayout,
        JpegProgressiveCoefficientComponentBuffer coefficientComponent,
        JpegPlanarComponentBuffer componentBuffer,
        JpegQuantizationTable quantizationTable,
        JpegReadOptions options)
    {
        int blockY = 0;
        while (blockY < componentLayout.BlockRows)
        {
            int blockX = 0;
            while (blockX < componentLayout.BlocksPerRow)
            {
                int[] zigZagCoefficients = CopyBlockFromCoefficientBuffer(coefficientComponent, blockX, blockY);
                int[] naturalCoefficients = Quantizer.DequantizeZigZagBlock(zigZagCoefficients, quantizationTable);
                int blockSize = InverseDct.Scaled.GetScaledBlockSize(options.IdctScaleNumerator, options.IdctScaleDenominator);
                byte[] samples;
                if (blockSize == JpegConstants.BlockWidth)
                {
                    samples = InverseDct.InverseTransformBlockToSamples(naturalCoefficients, options);
                }
                else
                {
                    samples = InverseDct.InverseTransformBlockToScaledSamples(naturalCoefficients, options);
                }

                BaselineDecoder.CopyScaledBlockToComponent(samples, componentBuffer, blockX * blockSize, blockY * blockSize, blockSize);
                blockX = blockX + 1;
            }

            blockY = blockY + 1;
        }
    }

    internal JpegProgressiveCoefficientBuffer AllocateCoefficientBuffer(JpegMcuLayout layout)
    {
        return AllocateCoefficientBufferWithLimit(layout, -1);
    }

    internal JpegProgressiveCoefficientBuffer AllocateCoefficientBufferWithLimit(JpegMcuLayout layout, long maxProgressiveStateBytes)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        JpegProgressiveCoefficientBuffer buffer = new JpegProgressiveCoefficientBuffer();
        buffer.Width = layout.Width;
        buffer.Height = layout.Height;
        buffer.ComponentCount = layout.ComponentCount;
        long totalBytes = 0;
        int componentIndex = 0;
        while (componentIndex < layout.ComponentCount)
        {
            JpegMcuComponentLayout componentLayout = layout.Components[componentIndex];
            JpegProgressiveCoefficientComponentBuffer component = new JpegProgressiveCoefficientComponentBuffer();
            component.ComponentIndex = componentLayout.ComponentIndex;
            component.Identifier = componentLayout.Identifier;
            component.BlocksPerRow = componentLayout.BlocksPerRow;
            component.BlockRows = componentLayout.BlockRows;
            long elementCount = (long)componentLayout.TotalBlockCount * JpegConstants.BlockSampleCount;
            totalBytes = totalBytes + elementCount * 4;
            if (maxProgressiveStateBytes >= 0 && totalBytes > maxProgressiveStateBytes)
            {
                throw JpegResourceLimitException.Create("JPEG progressive coefficient state exceeds the configured maximum progressive state bytes.");
            }

            if (elementCount > int.MaxValue)
            {
                throw JpegResourceLimitException.Create("JPEG progressive coefficient state is too large.");
            }

            component.Coefficients = new int[(int)elementCount];
            buffer.Components.Add(component);
            componentIndex = componentIndex + 1;
        }

        return buffer;
    }

    internal JpegProgressiveCoefficientComponentBuffer FindCoefficientComponentBuffer(JpegProgressiveCoefficientBuffer buffer, int identifier)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (buffer.Components == null)
        {
            throw JpegInvalidDataException.Create("Progressive coefficient component list cannot be null.");
        }

        foreach (JpegProgressiveCoefficientComponentBuffer component in buffer.Components)
        {
            if (component.Identifier == identifier)
            {
                return component;
            }
        }

        throw JpegInvalidDataException.Create("Progressive coefficient component was not found.");
    }

    internal int[] CopyBlockFromCoefficientBuffer(JpegProgressiveCoefficientComponentBuffer component, int blockX, int blockY)
    {
        ValidateCoefficientComponent(component);
        int offset = GetCoefficientOffset(component, blockX, blockY);
        int[] block = new int[JpegConstants.BlockSampleCount];
        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            block[index] = component.Coefficients[offset + index];
            index = index + 1;
        }

        return block;
    }

    internal void CopyBlockToCoefficientBuffer(int[] block, JpegProgressiveCoefficientComponentBuffer component, int blockX, int blockY)
    {
        ValidateCoefficientBlock(block);
        ValidateCoefficientComponent(component);
        int offset = GetCoefficientOffset(component, blockX, blockY);
        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            component.Coefficients[offset + index] = block[index];
            index = index + 1;
        }
    }

    internal int GetCoefficientOffset(JpegProgressiveCoefficientComponentBuffer component, int blockX, int blockY)
    {
        ValidateCoefficientComponent(component);
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

    internal void RecordAndConsumeRestart(JpegProgressiveHuffmanEntropyDecoder entropyDecoder, bool isLastUnit)
    {
        entropyDecoder.RecordMcuDecoded();
        if (!entropyDecoder.BaselineDecoder.ShouldExpectRestartMarker())
        {
            return;
        }

        if (isLastUnit)
        {
            return;
        }

        if (!entropyDecoder.BitReader.AdvanceToMarker())
        {
            throw JpegInvalidDataException.Create("Expected restart marker was not present in progressive entropy data.");
        }

        entropyDecoder.ConsumeRestartMarkerLookahead();
    }

    internal bool IsLastMcu(JpegMcuLayout layout, int mcuX, int mcuY)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        return mcuY == layout.McuRows - 1 && mcuX == layout.McusPerRow - 1;
    }

    internal int GetScanComponentCount(JpegScanHeader scan)
    {
        if (scan.ComponentCount != 0)
        {
            return scan.ComponentCount;
        }

        if (scan.Components == null)
        {
            return 0;
        }

        return scan.Components.Count;
    }

    internal void ValidateInputs(JpegFrameHeader frame, List<JpegProgressiveScanData> scans, JpegTableStore tables, int restartInterval)
    {
        ValidateFrame(frame);
        ValidateScans(frame, scans);
        BaselineDecoder.ValidateTables(tables);
        if (restartInterval < 0 || restartInterval > 65535)
        {
            throw new System.ArgumentOutOfRangeException("restartInterval");
        }
    }

    internal void ValidateFrame(JpegFrameHeader frame)
    {
        LayoutCalculator.ValidateFrame(frame);
        if (frame.Precision != 8)
        {
            throw JpegInvalidDataException.Create("Progressive SOF2 decoder only supports 8-bit sample precision.");
        }

        if (frame.Process != JpegProcess.Unknown && frame.Process != JpegProcess.ProgressiveDct)
        {
            throw JpegInvalidDataException.Create("Frame is not a progressive SOF2 frame.");
        }

        if (frame.Marker != JpegMarker.Unknown && frame.Marker != JpegMarker.StartOfFrame2)
        {
            throw JpegInvalidDataException.Create("Frame marker is not SOF2.");
        }

        if (frame.Value != 0 && frame.Value != JpegConstants.MarkerStartOfFrame2)
        {
            throw JpegInvalidDataException.Create("Frame marker value is not SOF2.");
        }

        if (frame.ArithmeticCoding)
        {
            throw JpegInvalidDataException.Create("Progressive SOF2 Huffman decoder does not support arithmetic frames.");
        }
    }

    internal void ValidateScans(JpegFrameHeader frame, List<JpegProgressiveScanData> scans)
    {
        if (scans == null)
        {
            throw new System.ArgumentNullException("scans");
        }

        if (scans.Count <= 0)
        {
            throw JpegInvalidDataException.Create("Progressive decode requires at least one scan.");
        }

        int index = 0;
        while (index < scans.Count)
        {
            ValidateScanData(frame, scans[index]);
            index = index + 1;
        }
    }

    internal void ValidateScanData(JpegFrameHeader frame, JpegProgressiveScanData scanData)
    {
        if (scanData == null)
        {
            throw JpegInvalidDataException.Create("Progressive scan data cannot be null.");
        }

        if (scanData.EntropyData == null)
        {
            throw new System.ArgumentNullException("EntropyData");
        }

        ValidateScan(frame, scanData.Scan);
    }

    internal void ValidateScan(JpegFrameHeader frame, JpegScanHeader scan)
    {
        if (scan == null)
        {
            throw new System.ArgumentNullException("scan");
        }

        if (scan.Components == null)
        {
            throw JpegInvalidDataException.Create("Scan component list cannot be null.");
        }

        int componentCount = GetScanComponentCount(scan);
        if (componentCount <= 0 || componentCount != scan.Components.Count)
        {
            throw JpegInvalidDataException.Create("Scan component count must match the component list.");
        }

        int index = 0;
        while (index < componentCount)
        {
            BaselineDecoder.ValidateScanComponent(frame, scan, index);
            index = index + 1;
        }

        JpegProgressiveHuffmanEntropyDecoder validator = new JpegProgressiveHuffmanEntropyDecoder(new byte[0]);
        if (scan.SpectralStart == 0 && scan.SpectralEnd == 0)
        {
            if (scan.SuccessiveHigh == 0)
            {
                validator.ValidateDcFirstScan(scan);
            }
            else
            {
                validator.ValidateDcRefinementScan(scan);
            }

            return;
        }

        if (scan.SuccessiveHigh == 0)
        {
            validator.ValidateAcFirstScan(scan);
            return;
        }

        validator.ValidateAcRefinementScan(scan);
    }

    internal void ValidateCoefficientComponent(JpegProgressiveCoefficientComponentBuffer component)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        if (component.BlocksPerRow <= 0 || component.BlockRows <= 0)
        {
            throw JpegInvalidDataException.Create("Progressive coefficient component dimensions must be non-empty.");
        }

        long minimumLength = (long)component.BlocksPerRow * (long)component.BlockRows * JpegConstants.BlockSampleCount;
        if (component.Coefficients == null || minimumLength < 0 || component.Coefficients.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("Progressive coefficient buffer is too small.");
        }
    }

    internal void ValidateCoefficientBlock(int[] block)
    {
        if (block == null)
        {
            throw new System.ArgumentNullException("block");
        }

        if (block.Length < JpegConstants.BlockSampleCount)
        {
            throw JpegInvalidDataException.Create("Coefficient block must contain 64 entries.");
        }
    }

    internal JpegReadOptions CreateDefaultReadOptions()
    {
        return new JpegReadOptions();
    }
}
