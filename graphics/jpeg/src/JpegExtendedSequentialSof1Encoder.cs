namespace jpeg;

internal class JpegExtendedSequentialSof1Encoder
{
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();
    internal JpegIntegerForwardDct ForwardDct = new JpegIntegerForwardDct();
    internal JpegCoefficientQuantizer Quantizer = new JpegCoefficientQuantizer();
    internal JpegCoefficientOrderConverter OrderConverter = new JpegCoefficientOrderConverter();
    internal JpegExtendedSequentialSof1Decoder DecoderValidation = new JpegExtendedSequentialSof1Decoder();
    internal JpegBaselineSof0Encoder BaselineEncoder = new JpegBaselineSof0Encoder();

    internal byte[] Encode(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, JpegPlanarIntImageBuffer source)
    {
        return EncodeWithRestartInterval(frame, scan, tables, source, 0);
    }

    internal byte[] EncodeWithRestartInterval(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, JpegPlanarIntImageBuffer source, int restartInterval)
    {
        ValidateInputs(frame, scan, tables, source, restartInterval);
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegSequentialHuffmanEntropyEncoder entropyEncoder = new JpegSequentialHuffmanEntropyEncoder();
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
        JpegPlanarIntImageBuffer source,
        JpegSequentialHuffmanEntropyEncoder entropyEncoder,
        int mcuX,
        int mcuY)
    {
        int scanComponentIndex = 0;
        while (scanComponentIndex < scan.Components.Count)
        {
            JpegScanComponent scanComponent = scan.Components[scanComponentIndex];
            JpegFrameComponent frameComponent = DecoderValidation.BaselineDecoder.FindFrameComponent(frame, scanComponent.Selector);
            JpegMcuComponentLayout componentLayout = LayoutCalculator.FindComponentLayout(layout, scanComponent.Selector);
            JpegPlanarIntComponentBuffer componentBuffer = DecoderValidation.FindComponentBuffer(source, scanComponent.Selector);
            JpegQuantizationTable quantizationTable = BaselineEncoder.FindQuantizationTable(tables, frameComponent.QuantizationTableIdentifier);
            EncodeMcuComponentBlockGroup(tables, scanComponent, componentLayout, componentBuffer, quantizationTable, entropyEncoder, frame.Precision, mcuX, mcuY);
            scanComponentIndex = scanComponentIndex + 1;
        }
    }

    internal void EncodeMcuComponentBlockGroup(
        JpegTableStore tables,
        JpegScanComponent scanComponent,
        JpegMcuComponentLayout componentLayout,
        JpegPlanarIntComponentBuffer componentBuffer,
        JpegQuantizationTable quantizationTable,
        JpegSequentialHuffmanEntropyEncoder entropyEncoder,
        int precision,
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
                int[] centeredSamples = ReadCenteredBlockFromComponent(componentBuffer, sourceX, sourceY, precision);
                int[] naturalCoefficients = ForwardDct.ForwardTransformBlock(centeredSamples);
                int[] quantizedNaturalCoefficients = Quantizer.QuantizeBlock(naturalCoefficients, quantizationTable);
                int[] zigZagCoefficients = OrderConverter.NaturalToZigZag(quantizedNaturalCoefficients);
                entropyEncoder.EncodeBlockForScanComponent(tables.HuffmanTables, scanComponent, zigZagCoefficients, componentLayout.ComponentIndex);
                horizontalIndex = horizontalIndex + 1;
            }

            verticalIndex = verticalIndex + 1;
        }
    }

    internal int[] ReadCenteredBlockFromComponent(JpegPlanarIntComponentBuffer component, int sourceX, int sourceY, int precision)
    {
        ValidateComponent(component, precision);
        if (sourceX < 0 || sourceY < 0)
        {
            throw new System.ArgumentOutOfRangeException("sourceX");
        }

        int center = 1 << (precision - 1);
        int[] block = new int[JpegConstants.BlockSampleCount];
        int y = 0;
        while (y < JpegConstants.BlockHeight)
        {
            int componentY = ClampCoordinate(sourceY + y, component.Height);
            int x = 0;
            while (x < JpegConstants.BlockWidth)
            {
                int componentX = ClampCoordinate(sourceX + x, component.Width);
                block[y * JpegConstants.BlockWidth + x] = component.Samples[componentY * component.Stride + componentX] - center;
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

    internal void WriteRestartMarkerIfExpected(JpegSequentialHuffmanEntropyEncoder entropyEncoder, JpegMcuLayout layout, int mcuX, int mcuY)
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

    internal void ValidateInputs(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegPlanarIntImageBuffer source,
        int restartInterval)
    {
        DecoderValidation.ValidateFrame(frame);
        DecoderValidation.ValidateScan(frame, scan);
        BaselineEncoder.ValidateTables(tables);
        ValidateSource(source, frame);
        if (restartInterval < 0 || restartInterval > 65535)
        {
            throw new System.ArgumentOutOfRangeException("restartInterval");
        }
    }

    internal void ValidateSource(JpegPlanarIntImageBuffer source, JpegFrameHeader frame)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (source.Width != frame.Width || source.Height != frame.Height)
        {
            throw JpegWriteValidationException.Create("Planar integer source dimensions must match the frame.");
        }

        if (source.Precision != frame.Precision)
        {
            throw JpegWriteValidationException.Create("Planar integer source precision must match the frame.");
        }

        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        if (source.ComponentCount != layout.ComponentCount)
        {
            throw JpegWriteValidationException.Create("Planar integer source component count must match the frame.");
        }

        int index = 0;
        while (index < layout.ComponentCount)
        {
            JpegPlanarIntComponentBuffer component = DecoderValidation.FindComponentBuffer(source, layout.Components[index].Identifier);
            ValidateSourceComponent(component, layout.Components[index], frame.Precision);
            index = index + 1;
        }
    }

    internal void ValidateSourceComponent(JpegPlanarIntComponentBuffer component, JpegMcuComponentLayout layout, int precision)
    {
        ValidateComponent(component, precision);
        if (component.Width != layout.SampleWidth || component.Height != layout.SampleHeight)
        {
            throw JpegWriteValidationException.Create("Planar integer source component dimensions must match the frame sampling.");
        }
    }

    internal void ValidateComponent(JpegPlanarIntComponentBuffer component, int precision)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        DecoderValidation.ValidatePrecision(precision);
        if (component.Precision != precision)
        {
            throw JpegWriteValidationException.Create("Planar integer component precision must match the frame.");
        }

        if (component.Width <= 0)
        {
            throw JpegWriteValidationException.Create("Planar integer component width must be greater than zero.");
        }

        if (component.Height <= 0)
        {
            throw JpegWriteValidationException.Create("Planar integer component height must be greater than zero.");
        }

        if (component.Stride < component.Width)
        {
            throw JpegWriteValidationException.Create("Planar integer component stride cannot be smaller than width.");
        }

        long minimumLength = (long)component.Stride * (long)component.Height;
        if (component.Samples == null || minimumLength < 0 || component.Samples.Length < minimumLength)
        {
            throw JpegWriteValidationException.Create("Planar integer component sample buffer is too small.");
        }

        ValidateSampleRange(component, precision);
    }

    internal void ValidateSampleRange(JpegPlanarIntComponentBuffer component, int precision)
    {
        int maximum = (1 << precision) - 1;
        int y = 0;
        while (y < component.Height)
        {
            int index = y * component.Stride;
            int x = 0;
            while (x < component.Width)
            {
                int sample = component.Samples[index + x];
                if (sample < 0 || sample > maximum)
                {
                    throw JpegWriteValidationException.Create("Planar integer component sample is outside the frame precision range.");
                }

                x = x + 1;
            }

            y = y + 1;
        }
    }
}
