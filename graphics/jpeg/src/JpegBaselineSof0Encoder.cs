namespace jpeg;

internal class JpegBaselineSof0Encoder
{
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();
    internal JpegIntegerForwardDct ForwardDct = new JpegIntegerForwardDct();
    internal JpegCoefficientQuantizer Quantizer = new JpegCoefficientQuantizer();
    internal JpegCoefficientOrderConverter OrderConverter = new JpegCoefficientOrderConverter();
    internal JpegBaselineSof0FullImageDecoder BaselineDecoderValidation = new JpegBaselineSof0FullImageDecoder();

    internal byte[] Encode(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, JpegPlanarImageBuffer source)
    {
        return EncodeWithRestartInterval(frame, scan, tables, source, 0);
    }

    internal byte[] EncodeWithRestartInterval(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, JpegPlanarImageBuffer source, int restartInterval)
    {
        ValidateInputs(frame, scan, tables, source, restartInterval);
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegBaselineHuffmanEntropyEncoder entropyEncoder = new JpegBaselineHuffmanEntropyEncoder();
        entropyEncoder.ConfigureRestartInterval(restartInterval);

        int mcuY = 0;
        while (mcuY < layout.McuRows)
        {
            int mcuX = 0;
            while (mcuX < layout.McusPerRow)
            {
                EncodeMcu(frame, scan, tables, layout, source, entropyEncoder, mcuX, mcuY);
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
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegMcuLayout layout,
        JpegPlanarImageBuffer source,
        JpegBaselineHuffmanEntropyEncoder entropyEncoder,
        int mcuX,
        int mcuY)
    {
        int scanComponentIndex = 0;
        while (scanComponentIndex < scan.Components.Count)
        {
            JpegScanComponent scanComponent = scan.Components[scanComponentIndex];
            JpegFrameComponent frameComponent = BaselineDecoderValidation.FindFrameComponent(frame, scanComponent.Selector);
            JpegMcuComponentLayout componentLayout = LayoutCalculator.FindComponentLayout(layout, scanComponent.Selector);
            JpegPlanarComponentBuffer componentBuffer = FindComponentBuffer(source, scanComponent.Selector);
            JpegQuantizationTable quantizationTable = FindQuantizationTable(tables, frameComponent.QuantizationTableIdentifier);
            EncodeMcuComponentBlockGroup(
                tables,
                scanComponent,
                componentLayout,
                componentBuffer,
                quantizationTable,
                entropyEncoder,
                mcuX,
                mcuY);
            scanComponentIndex = scanComponentIndex + 1;
        }
    }

    internal void EncodeMcuComponentBlockGroup(
        JpegTableStore tables,
        JpegScanComponent scanComponent,
        JpegMcuComponentLayout componentLayout,
        JpegPlanarComponentBuffer componentBuffer,
        JpegQuantizationTable quantizationTable,
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
                int sourceX = (mcuX * componentLayout.HorizontalSamplingFactor + horizontalIndex) * JpegConstants.BlockWidth;
                int sourceY = (mcuY * componentLayout.VerticalSamplingFactor + verticalIndex) * JpegConstants.BlockHeight;
                byte[] samples = ReadBlockFromComponent(componentBuffer, sourceX, sourceY);
                int[] naturalCoefficients = ForwardDct.ForwardTransformSamples(samples);
                int[] quantizedNaturalCoefficients = Quantizer.QuantizeBlock(naturalCoefficients, quantizationTable);
                int[] zigZagCoefficients = OrderConverter.NaturalToZigZag(quantizedNaturalCoefficients);
                entropyEncoder.EncodeBlockForScanComponent(
                    tables.HuffmanTables,
                    scanComponent,
                    zigZagCoefficients,
                    componentLayout.ComponentIndex);
                horizontalIndex = horizontalIndex + 1;
            }

            verticalIndex = verticalIndex + 1;
        }
    }

    internal byte[] ReadBlockFromComponent(JpegPlanarComponentBuffer component, int sourceX, int sourceY)
    {
        ValidateComponent(component);
        if (sourceX < 0 || sourceY < 0)
        {
            throw new System.ArgumentOutOfRangeException("sourceX");
        }

        byte[] block = new byte[JpegConstants.BlockSampleCount];
        int y = 0;
        while (y < JpegConstants.BlockHeight)
        {
            int componentY = ClampCoordinate(sourceY + y, component.Height);
            int x = 0;
            while (x < JpegConstants.BlockWidth)
            {
                int componentX = ClampCoordinate(sourceX + x, component.Width);
                block[y * JpegConstants.BlockWidth + x] = component.Samples[componentY * component.Stride + componentX];
                x = x + 1;
            }

            y = y + 1;
        }

        return block;
    }

    internal int ClampCoordinate(int value, int length)
    {
        if (value < 0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        if (length <= 0)
        {
            throw JpegWriteValidationException.Create("Cannot read a sample from an empty component.");
        }

        if (value >= length)
        {
            return length - 1;
        }

        return value;
    }

    internal void WriteRestartMarkerIfExpected(JpegBaselineHuffmanEntropyEncoder entropyEncoder, JpegMcuLayout layout, int mcuX, int mcuY)
    {
        if (!entropyEncoder.ShouldWriteRestartMarker())
        {
            return;
        }

        if (IsLastMcu(layout, mcuX, mcuY))
        {
            return;
        }

        entropyEncoder.WriteRestartMarker();
    }

    internal bool IsLastMcu(JpegMcuLayout layout, int mcuX, int mcuY)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        return mcuY == layout.McuRows - 1 && mcuX == layout.McusPerRow - 1;
    }

    internal JpegPlanarComponentBuffer FindComponentBuffer(JpegPlanarImageBuffer source, int identifier)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (source.Components == null)
        {
            throw JpegWriteValidationException.Create("Planar component list cannot be null.");
        }

        foreach (JpegPlanarComponentBuffer component in source.Components)
        {
            if (component.Identifier == identifier)
            {
                return component;
            }
        }

        throw JpegWriteValidationException.Create("Planar component buffer was not found.");
    }

    internal JpegQuantizationTable FindQuantizationTable(JpegTableStore tables, int identifier)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        if (tables.QuantizationTables == null)
        {
            throw JpegWriteValidationException.Create("Quantization table list cannot be null.");
        }

        foreach (JpegQuantizationTable table in tables.QuantizationTables)
        {
            if (table.Identifier == identifier)
            {
                return table;
            }
        }

        throw JpegWriteValidationException.Create("Required quantization table is missing.");
    }

    internal void ValidateInputs(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegPlanarImageBuffer source,
        int restartInterval)
    {
        BaselineDecoderValidation.ValidateFrame(frame);
        BaselineDecoderValidation.ValidateScan(frame, scan);
        ValidateTables(tables);
        ValidateSource(source, frame);
        if (restartInterval < 0 || restartInterval > 65535)
        {
            throw new System.ArgumentOutOfRangeException("restartInterval");
        }
    }

    internal void ValidateTables(JpegTableStore tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        if (tables.QuantizationTables == null)
        {
            throw JpegWriteValidationException.Create("Quantization table list cannot be null.");
        }

        if (tables.HuffmanTables == null)
        {
            throw JpegWriteValidationException.Create("Huffman table list cannot be null.");
        }
    }

    internal void ValidateSource(JpegPlanarImageBuffer source, JpegFrameHeader frame)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (source.Width != frame.Width || source.Height != frame.Height)
        {
            throw JpegWriteValidationException.Create("Planar source dimensions must match the frame.");
        }

        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        if (source.ComponentCount != layout.ComponentCount)
        {
            throw JpegWriteValidationException.Create("Planar source component count must match the frame.");
        }

        int index = 0;
        while (index < layout.ComponentCount)
        {
            JpegPlanarComponentBuffer component = FindComponentBuffer(source, layout.Components[index].Identifier);
            ValidateSourceComponent(component, layout.Components[index]);
            index = index + 1;
        }
    }

    internal void ValidateSourceComponent(JpegPlanarComponentBuffer component, JpegMcuComponentLayout layout)
    {
        ValidateComponent(component);
        if (component.Width != layout.SampleWidth || component.Height != layout.SampleHeight)
        {
            throw JpegWriteValidationException.Create("Planar source component dimensions must match the frame sampling.");
        }
    }

    internal void ValidateComponent(JpegPlanarComponentBuffer component)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        if (component.Width <= 0)
        {
            throw JpegWriteValidationException.Create("Planar component width must be greater than zero.");
        }

        if (component.Height <= 0)
        {
            throw JpegWriteValidationException.Create("Planar component height must be greater than zero.");
        }

        if (component.Stride < component.Width)
        {
            throw JpegWriteValidationException.Create("Planar component stride cannot be smaller than width.");
        }

        long minimumLength = (long)component.Stride * (long)component.Height;
        if (component.Samples == null || minimumLength < 0 || component.Samples.Length < minimumLength)
        {
            throw JpegWriteValidationException.Create("Planar component sample buffer is too small.");
        }
    }
}
