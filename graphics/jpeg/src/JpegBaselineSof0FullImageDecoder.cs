namespace jpeg;

internal class JpegBaselineSof0FullImageDecoder
{
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();
    internal JpegPlanarComponentBufferAllocator PlanarAllocator = new JpegPlanarComponentBufferAllocator();
    internal JpegCoefficientQuantizer Quantizer = new JpegCoefficientQuantizer();
    internal JpegInverseDctTransformer InverseDct = new JpegInverseDctTransformer();

    internal JpegPlanarImageBuffer Decode(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, byte[] entropyData)
    {
        return DecodeWithOptions(frame, scan, tables, entropyData, 0, CreateDefaultReadOptions());
    }

    internal JpegPlanarImageBuffer DecodeWithRestartInterval(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, byte[] entropyData, int restartInterval)
    {
        return DecodeWithOptions(frame, scan, tables, entropyData, restartInterval, CreateDefaultReadOptions());
    }

    internal JpegPlanarImageBuffer DecodeWithOptions(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        byte[] entropyData,
        int restartInterval,
        JpegReadOptions options)
    {
        ValidateInputs(frame, scan, tables, entropyData, restartInterval, options);
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegPlanarImageBuffer destination = AllocateScaledDestination(layout, options);
        DecodeIntoWithOptions(frame, scan, tables, entropyData, restartInterval, options, destination);
        return destination;
    }

    internal void DecodeInto(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        byte[] entropyData,
        JpegPlanarImageBuffer destination)
    {
        DecodeIntoWithOptions(frame, scan, tables, entropyData, 0, CreateDefaultReadOptions(), destination);
    }

    internal void DecodeIntoWithOptions(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        byte[] entropyData,
        int restartInterval,
        JpegReadOptions options,
        JpegPlanarImageBuffer destination)
    {
        ValidateInputs(frame, scan, tables, entropyData, restartInterval, options);
        ValidateScaledDestination(destination, frame, options);

        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegBaselineHuffmanEntropyDecoder entropyDecoder = new JpegBaselineHuffmanEntropyDecoder(entropyData);
        entropyDecoder.ConfigureRestartInterval(restartInterval);

        int mcuY = 0;
        while (mcuY < layout.McuRows)
        {
            int mcuX = 0;
            while (mcuX < layout.McusPerRow)
            {
                DecodeMcu(frame, scan, tables, layout, entropyDecoder, options, destination, mcuX, mcuY);
                entropyDecoder.RecordMcuDecoded();
                ConsumeRestartMarkerIfExpected(entropyDecoder, layout, mcuX, mcuY);
                mcuX = mcuX + 1;
            }

            mcuY = mcuY + 1;
        }
    }

    internal void DecodeMcu(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegMcuLayout layout,
        JpegBaselineHuffmanEntropyDecoder entropyDecoder,
        JpegReadOptions options,
        JpegPlanarImageBuffer destination,
        int mcuX,
        int mcuY)
    {
        int scanComponentIndex = 0;
        while (scanComponentIndex < scan.Components.Count)
        {
            JpegScanComponent scanComponent = scan.Components[scanComponentIndex];
            JpegFrameComponent frameComponent = FindFrameComponent(frame, scanComponent.Selector);
            JpegMcuComponentLayout componentLayout = LayoutCalculator.FindComponentLayout(layout, scanComponent.Selector);
            JpegPlanarComponentBuffer componentBuffer = PlanarAllocator.FindComponentBuffer(destination, scanComponent.Selector);
            JpegQuantizationTable quantizationTable = FindQuantizationTable(tables, frameComponent.QuantizationTableIdentifier);
            DecodeMcuComponentBlockGroup(
                tables,
                scanComponent,
                componentLayout,
                componentBuffer,
                quantizationTable,
                entropyDecoder,
                options,
                mcuX,
                mcuY);
            scanComponentIndex = scanComponentIndex + 1;
        }
    }

    internal void DecodeMcuComponentBlockGroup(
        JpegTableStore tables,
        JpegScanComponent scanComponent,
        JpegMcuComponentLayout componentLayout,
        JpegPlanarComponentBuffer componentBuffer,
        JpegQuantizationTable quantizationTable,
        JpegBaselineHuffmanEntropyDecoder entropyDecoder,
        JpegReadOptions options,
        int mcuX,
        int mcuY)
    {
        int verticalIndex = 0;
        while (verticalIndex < componentLayout.VerticalSamplingFactor)
        {
            int horizontalIndex = 0;
            while (horizontalIndex < componentLayout.HorizontalSamplingFactor)
            {
                int[] zigZagCoefficients = entropyDecoder.DecodeBlockForScanComponent(
                    tables.HuffmanTables,
                    scanComponent,
                    componentLayout.ComponentIndex);
                int[] naturalCoefficients = Quantizer.DequantizeZigZagBlock(zigZagCoefficients, quantizationTable);
                int scaledBlockSize = InverseDct.Scaled.GetScaledBlockSize(options.IdctScaleNumerator, options.IdctScaleDenominator);
                byte[] samples;
                if (scaledBlockSize == JpegConstants.BlockWidth)
                {
                    samples = InverseDct.InverseTransformBlockToSamples(naturalCoefficients, options);
                }
                else
                {
                    samples = InverseDct.InverseTransformBlockToScaledSamples(naturalCoefficients, options);
                }

                int destinationX = (mcuX * componentLayout.HorizontalSamplingFactor + horizontalIndex) * scaledBlockSize;
                int destinationY = (mcuY * componentLayout.VerticalSamplingFactor + verticalIndex) * scaledBlockSize;
                if (BlockStartsInsideComponent(componentBuffer, destinationX, destinationY))
                {
                    CopyScaledBlockToComponent(samples, componentBuffer, destinationX, destinationY, scaledBlockSize);
                }

                horizontalIndex = horizontalIndex + 1;
            }

            verticalIndex = verticalIndex + 1;
        }
    }

    internal bool BlockStartsInsideComponent(JpegPlanarComponentBuffer component, int destinationX, int destinationY)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        if (destinationX < 0 || destinationY < 0)
        {
            throw new System.ArgumentOutOfRangeException("destinationX");
        }

        return destinationX < component.PaddedWidth && destinationY < component.PaddedHeight;
    }

    internal void CopyBlockToComponent(byte[] block, JpegPlanarComponentBuffer component, int destinationX, int destinationY)
    {
        ValidateSampleBlock(block);
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        if (destinationX < 0 || destinationY < 0)
        {
            throw new System.ArgumentOutOfRangeException("destinationX");
        }

        if (destinationX + JpegConstants.BlockWidth > component.PaddedWidth)
        {
            throw JpegInvalidDataException.Create("Decoded block exceeds the component width.");
        }

        if (destinationY + JpegConstants.BlockHeight > component.PaddedHeight)
        {
            throw JpegInvalidDataException.Create("Decoded block exceeds the component height.");
        }

        int y = 0;
        while (y < JpegConstants.BlockHeight)
        {
            int sourceOffset = y * JpegConstants.BlockWidth;
            int destinationOffset = (destinationY + y) * component.Stride + destinationX;
            int x = 0;
            while (x < JpegConstants.BlockWidth)
            {
                component.Samples[destinationOffset + x] = block[sourceOffset + x];
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void CopyScaledBlockToComponent(byte[] block, JpegPlanarComponentBuffer component, int destinationX, int destinationY, int blockSize)
    {
        if (block == null)
        {
            throw new System.ArgumentNullException("block");
        }

        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        if (blockSize < 1 || blockSize > JpegConstants.BlockWidth || block.Length < blockSize * blockSize)
        {
            throw JpegInvalidDataException.Create("Scaled sample block dimensions are invalid.");
        }

        if (destinationX < 0 || destinationY < 0
            || destinationX + blockSize > component.PaddedWidth
            || destinationY + blockSize > component.PaddedHeight)
        {
            throw JpegInvalidDataException.Create("Scaled decoded block exceeds the component buffer.");
        }

        int y = 0;
        while (y < blockSize)
        {
            int sourceOffset = y * blockSize;
            int destinationOffset = (destinationY + y) * component.Stride + destinationX;
            int x = 0;
            while (x < blockSize)
            {
                component.Samples[destinationOffset + x] = block[sourceOffset + x];
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal JpegPlanarImageBuffer AllocateScaledDestination(JpegMcuLayout layout, JpegReadOptions options)
    {
        int blockSize = InverseDct.Scaled.GetScaledBlockSize(options.IdctScaleNumerator, options.IdctScaleDenominator);
        EnforceComponentBufferLimit(layout, blockSize, options);
        if (blockSize == JpegConstants.BlockWidth)
        {
            return PlanarAllocator.Allocate(layout);
        }

        JpegPlanarImageBuffer buffer = new JpegPlanarImageBuffer();
        buffer.Width = ScaleDecodedDimension(layout.Width, blockSize);
        buffer.Height = ScaleDecodedDimension(layout.Height, blockSize);
        buffer.Precision = 8;
        buffer.ComponentCount = layout.ComponentCount;
        buffer.Subsampling = JpegSubsampling.Custom;
        buffer.Ownership = JpegBufferOwnership.LibraryOwned;
        int index = 0;
        while (index < layout.ComponentCount)
        {
            buffer.Components.Add(AllocateScaledComponent(layout.Components[index], blockSize));
            index = index + 1;
        }

        return buffer;
    }

    internal JpegPlanarComponentBuffer AllocateScaledComponent(JpegMcuComponentLayout layout, int blockSize)
    {
        JpegPlanarComponentBuffer component = new JpegPlanarComponentBuffer();
        component.ComponentIndex = layout.ComponentIndex;
        component.Identifier = layout.Identifier;
        component.Precision = 8;
        component.HorizontalSamplingFactor = layout.HorizontalSamplingFactor;
        component.VerticalSamplingFactor = layout.VerticalSamplingFactor;
        component.Width = ScaleDecodedDimension(layout.SampleWidth, blockSize);
        component.Height = ScaleDecodedDimension(layout.SampleHeight, blockSize);
        component.PaddedWidth = layout.BlocksPerRow * blockSize;
        component.PaddedHeight = layout.BlockRows * blockSize;
        component.Stride = component.PaddedWidth;
        component.SampleStorage = JpegSampleStorage.Byte;
        component.Ownership = JpegBufferOwnership.LibraryOwned;
        int length = PlanarAllocator.GetStridedSampleLength(component.Stride, component.PaddedHeight);
        component.Samples = new byte[length];
        return component;
    }

    internal void EnforceComponentBufferLimit(JpegMcuLayout layout, int blockSize, JpegReadOptions options)
    {
        if (options.MaxComponentBufferBytes < 0)
        {
            return;
        }

        long totalBytes = 0;
        int index = 0;
        while (index < layout.ComponentCount)
        {
            JpegMcuComponentLayout component = layout.Components[index];
            long paddedWidth = (long)component.BlocksPerRow * blockSize;
            long paddedHeight = (long)component.BlockRows * blockSize;
            totalBytes = totalBytes + paddedWidth * paddedHeight;
            if (totalBytes > options.MaxComponentBufferBytes)
            {
                throw JpegResourceLimitException.Create("JPEG component buffers exceed the configured maximum component buffer bytes.");
            }

            index = index + 1;
        }
    }

    internal int ScaleDecodedDimension(int dimension, int blockSize)
    {
        long scaled = (long)dimension * (long)blockSize;
        long result = (scaled + JpegConstants.BlockWidth - 1) / JpegConstants.BlockWidth;
        if (result > int.MaxValue)
        {
            throw JpegResourceLimitException.Create("Scaled component dimension is too large.");
        }

        return (int)result;
    }

    internal void ConsumeRestartMarkerIfExpected(JpegBaselineHuffmanEntropyDecoder entropyDecoder, JpegMcuLayout layout, int mcuX, int mcuY)
    {
        if (!entropyDecoder.ShouldExpectRestartMarker())
        {
            return;
        }

        if (IsLastMcu(layout, mcuX, mcuY))
        {
            return;
        }

        if (!entropyDecoder.BitReader.AdvanceToMarker())
        {
            throw JpegInvalidDataException.Create("Expected restart marker was not present in entropy data.");
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

    internal JpegFrameComponent FindFrameComponent(JpegFrameHeader frame, int identifier)
    {
        if (frame == null)
        {
            throw new System.ArgumentNullException("frame");
        }

        if (frame.Components == null)
        {
            throw JpegInvalidDataException.Create("Frame component list cannot be null.");
        }

        foreach (JpegFrameComponent component in frame.Components)
        {
            if (component.Identifier == identifier)
            {
                return component;
            }
        }

        throw JpegInvalidDataException.Create("Scan component selector does not match a frame component.");
    }

    internal JpegQuantizationTable FindQuantizationTable(JpegTableStore tables, int identifier)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        if (tables.QuantizationTables == null)
        {
            throw JpegInvalidDataException.Create("Quantization table list cannot be null.");
        }

        foreach (JpegQuantizationTable table in tables.QuantizationTables)
        {
            if (table.Identifier == identifier)
            {
                return table;
            }
        }

        throw JpegInvalidDataException.Create("Required quantization table is missing.");
    }

    internal void ValidateInputs(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        byte[] entropyData,
        int restartInterval,
        JpegReadOptions options)
    {
        ValidateFrame(frame);
        ValidateScan(frame, scan);
        ValidateTables(tables);
        if (entropyData == null)
        {
            throw new System.ArgumentNullException("entropyData");
        }

        if (restartInterval < 0 || restartInterval > 65535)
        {
            throw new System.ArgumentOutOfRangeException("restartInterval");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
    }

    internal void ValidateFrame(JpegFrameHeader frame)
    {
        LayoutCalculator.ValidateFrame(frame);
        if (frame.Precision != 8)
        {
            throw JpegInvalidDataException.Create("Baseline SOF0 decoder only supports 8-bit sample precision.");
        }

        if (frame.Process != JpegProcess.Unknown && frame.Process != JpegProcess.BaselineDct)
        {
            throw JpegInvalidDataException.Create("Frame is not a baseline SOF0 frame.");
        }

        if (frame.Marker != JpegMarker.Unknown && frame.Marker != JpegMarker.StartOfFrame0)
        {
            throw JpegInvalidDataException.Create("Frame marker is not SOF0.");
        }

        if (frame.Value != 0 && frame.Value != JpegConstants.MarkerStartOfFrame0)
        {
            throw JpegInvalidDataException.Create("Frame marker value is not SOF0.");
        }

        if (frame.Progressive || frame.ArithmeticCoding)
        {
            throw JpegInvalidDataException.Create("Baseline SOF0 decoder does not support progressive or arithmetic frames.");
        }
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

        if (scan.SpectralStart != 0 || scan.SpectralEnd != 63 || scan.SuccessiveHigh != 0 || scan.SuccessiveLow != 0)
        {
            throw JpegInvalidDataException.Create("Baseline SOF0 decoder requires a sequential full-spectrum scan.");
        }

        int frameComponentCount = LayoutCalculator.GetComponentCount(frame);
        int scanComponentCount = scan.Components.Count;
        if (scan.ComponentCount != 0 && scan.ComponentCount != scanComponentCount)
        {
            throw JpegInvalidDataException.Create("Scan component count must match the component list.");
        }

        if (scanComponentCount != frameComponentCount)
        {
            throw JpegInvalidDataException.Create("Baseline full-image decoder requires one scan containing every frame component.");
        }

        int index = 0;
        while (index < scanComponentCount)
        {
            ValidateScanComponent(frame, scan, index);
            index = index + 1;
        }
    }

    internal void ValidateScanComponent(JpegFrameHeader frame, JpegScanHeader scan, int scanComponentIndex)
    {
        JpegScanComponent component = scan.Components[scanComponentIndex];
        if (component == null)
        {
            throw JpegInvalidDataException.Create("Scan component cannot be null.");
        }

        if (component.DcEntropyTableIdentifier < 0 || component.DcEntropyTableIdentifier > 3)
        {
            throw JpegInvalidDataException.Create("SOS DC entropy table identifier must be between 0 and 3.");
        }

        if (component.AcEntropyTableIdentifier < 0 || component.AcEntropyTableIdentifier > 3)
        {
            throw JpegInvalidDataException.Create("SOS AC entropy table identifier must be between 0 and 3.");
        }

        int index = 0;
        while (index < scanComponentIndex)
        {
            if (scan.Components[index].Selector == component.Selector)
            {
                throw JpegInvalidDataException.Create("SOS component selectors must be unique.");
            }

            index = index + 1;
        }

        FindFrameComponent(frame, component.Selector);
    }

    internal void ValidateTables(JpegTableStore tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        if (tables.QuantizationTables == null)
        {
            throw JpegInvalidDataException.Create("Quantization table list cannot be null.");
        }

        if (tables.HuffmanTables == null)
        {
            throw JpegInvalidDataException.Create("Huffman table list cannot be null.");
        }
    }

    internal void ValidateDestination(JpegPlanarImageBuffer destination, JpegFrameHeader frame)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        if (destination.Width != layout.Width || destination.Height != layout.Height || destination.ComponentCount != layout.ComponentCount)
        {
            throw JpegInvalidDataException.Create("Destination planar buffer does not match the frame dimensions.");
        }

        PlanarAllocator.ValidateLayout(layout);
        int index = 0;
        while (index < layout.ComponentCount)
        {
            JpegPlanarComponentBuffer component = PlanarAllocator.FindComponentBuffer(destination, layout.Components[index].Identifier);
            ValidateDestinationComponent(component, layout.Components[index]);
            index = index + 1;
        }
    }

    internal void ValidateScaledDestination(JpegPlanarImageBuffer destination, JpegFrameHeader frame, JpegReadOptions options)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        int blockSize = InverseDct.Scaled.GetScaledBlockSize(options.IdctScaleNumerator, options.IdctScaleDenominator);
        int expectedWidth = ScaleDecodedDimension(layout.Width, blockSize);
        int expectedHeight = ScaleDecodedDimension(layout.Height, blockSize);
        if (destination.Width != expectedWidth || destination.Height != expectedHeight || destination.ComponentCount != layout.ComponentCount)
        {
            throw JpegInvalidDataException.Create("Destination planar buffer does not match the scaled frame dimensions.");
        }

        int index = 0;
        while (index < layout.ComponentCount)
        {
            JpegMcuComponentLayout componentLayout = layout.Components[index];
            JpegPlanarComponentBuffer component = PlanarAllocator.FindComponentBuffer(destination, componentLayout.Identifier);
            int componentWidth = ScaleDecodedDimension(componentLayout.SampleWidth, blockSize);
            int componentHeight = ScaleDecodedDimension(componentLayout.SampleHeight, blockSize);
            int paddedWidth = componentLayout.BlocksPerRow * blockSize;
            int paddedHeight = componentLayout.BlockRows * blockSize;
            if (component.Width != componentWidth || component.Height != componentHeight
                || component.PaddedWidth != paddedWidth || component.PaddedHeight != paddedHeight)
            {
                throw JpegInvalidDataException.Create("Destination component does not match the scaled frame dimensions.");
            }

            if (component.Stride < paddedWidth)
            {
                throw JpegInvalidDataException.Create("Scaled destination component stride is too small.");
            }

            long minimumLength = (long)component.Stride * (long)paddedHeight;
            if (component.Samples == null || component.Samples.Length < minimumLength)
            {
                throw JpegInvalidDataException.Create("Scaled destination component sample buffer is too small.");
            }

            index = index + 1;
        }
    }

    internal void ValidateDestinationComponent(JpegPlanarComponentBuffer component, JpegMcuComponentLayout layout)
    {
        if (component.Width != layout.SampleWidth || component.Height != layout.SampleHeight)
        {
            throw JpegInvalidDataException.Create("Destination component dimensions do not match the frame.");
        }

        if (component.PaddedWidth != layout.PaddedSampleWidth || component.PaddedHeight != layout.PaddedSampleHeight)
        {
            throw JpegInvalidDataException.Create("Destination component padding does not match the frame.");
        }

        if (component.Stride < component.PaddedWidth)
        {
            throw JpegInvalidDataException.Create("Destination component stride is too small.");
        }

        long minimumLength = (long)component.Stride * (long)component.PaddedHeight;
        if (component.Samples == null || minimumLength < 0 || component.Samples.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("Destination component sample buffer is too small.");
        }
    }

    internal void ValidateSampleBlock(byte[] block)
    {
        if (block == null)
        {
            throw new System.ArgumentNullException("block");
        }

        if (block.Length < JpegConstants.BlockSampleCount)
        {
            throw JpegInvalidDataException.Create("Sample block must contain 64 entries.");
        }
    }

    internal JpegReadOptions CreateDefaultReadOptions()
    {
        return new JpegReadOptions();
    }
}
