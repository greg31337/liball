namespace jpeg;

internal class JpegPlanarIntComponentBuffer
{
    internal int ComponentIndex;
    internal int Identifier;
    internal int Width;
    internal int Height;
    internal int PaddedWidth;
    internal int PaddedHeight;
    internal int Stride;
    internal int Precision;
    internal int[] Samples = new int[0];
}

internal class JpegPlanarIntImageBuffer
{
    internal int Width;
    internal int Height;
    internal int ComponentCount;
    internal int Precision;
    internal System.Collections.Generic.List<JpegPlanarIntComponentBuffer> Components = new System.Collections.Generic.List<JpegPlanarIntComponentBuffer>();
}

internal class JpegExtendedSequentialSof1Decoder
{
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();
    internal JpegCoefficientQuantizer Quantizer = new JpegCoefficientQuantizer();
    internal JpegAccurateIntegerInverseDct InverseDct = new JpegAccurateIntegerInverseDct();
    internal JpegBaselineSof0FullImageDecoder BaselineDecoder = new JpegBaselineSof0FullImageDecoder();

    internal JpegPlanarIntImageBuffer Decode(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, byte[] entropyData)
    {
        return DecodeWithRestartInterval(frame, scan, tables, entropyData, 0);
    }

    internal JpegPlanarIntImageBuffer DecodeWithRestartInterval(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, byte[] entropyData, int restartInterval)
    {
        ValidateInputs(frame, scan, tables, entropyData, restartInterval);
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegPlanarIntImageBuffer destination = Allocate(layout, frame.Precision);
        JpegSequentialHuffmanEntropyDecoder entropyDecoder = new JpegSequentialHuffmanEntropyDecoder(entropyData);
        entropyDecoder.ConfigureRestartInterval(restartInterval);

        int mcuY = 0;
        while (mcuY < layout.McuRows)
        {
            int mcuX = 0;
            while (mcuX < layout.McusPerRow)
            {
                DecodeMcu(frame, scan, tables, layout, entropyDecoder, destination, mcuX, mcuY);
                entropyDecoder.RecordMcuDecoded();
                ConsumeRestartMarkerIfExpected(entropyDecoder, layout, mcuX, mcuY);
                mcuX = mcuX + 1;
            }

            mcuY = mcuY + 1;
        }

        return destination;
    }

    internal void DecodeMcu(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegMcuLayout layout,
        JpegSequentialHuffmanEntropyDecoder entropyDecoder,
        JpegPlanarIntImageBuffer destination,
        int mcuX,
        int mcuY)
    {
        int scanComponentIndex = 0;
        while (scanComponentIndex < scan.Components.Count)
        {
            JpegScanComponent scanComponent = scan.Components[scanComponentIndex];
            JpegFrameComponent frameComponent = BaselineDecoder.FindFrameComponent(frame, scanComponent.Selector);
            JpegMcuComponentLayout componentLayout = LayoutCalculator.FindComponentLayout(layout, scanComponent.Selector);
            JpegPlanarIntComponentBuffer componentBuffer = FindComponentBuffer(destination, scanComponent.Selector);
            JpegQuantizationTable quantizationTable = BaselineDecoder.FindQuantizationTable(tables, frameComponent.QuantizationTableIdentifier);
            DecodeMcuComponentBlockGroup(tables, scanComponent, componentLayout, componentBuffer, quantizationTable, entropyDecoder, frame.Precision, mcuX, mcuY);
            scanComponentIndex = scanComponentIndex + 1;
        }
    }

    internal void DecodeMcuComponentBlockGroup(
        JpegTableStore tables,
        JpegScanComponent scanComponent,
        JpegMcuComponentLayout componentLayout,
        JpegPlanarIntComponentBuffer componentBuffer,
        JpegQuantizationTable quantizationTable,
        JpegSequentialHuffmanEntropyDecoder entropyDecoder,
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
                int[] zigZagCoefficients = entropyDecoder.DecodeBlockForScanComponent(
                    tables.HuffmanTables,
                    scanComponent,
                    componentLayout.ComponentIndex);
                int[] naturalCoefficients = Quantizer.DequantizeZigZagBlock(zigZagCoefficients, quantizationTable);
                int[] centeredSamples = InverseDct.InverseTransformBlock(naturalCoefficients);
                int destinationX = (mcuX * componentLayout.HorizontalSamplingFactor + horizontalIndex) * JpegConstants.BlockWidth;
                int destinationY = (mcuY * componentLayout.VerticalSamplingFactor + verticalIndex) * JpegConstants.BlockHeight;
                CopyBlockToComponent(centeredSamples, componentBuffer, precision, destinationX, destinationY);
                horizontalIndex = horizontalIndex + 1;
            }

            verticalIndex = verticalIndex + 1;
        }
    }

    internal JpegPlanarIntImageBuffer Allocate(JpegMcuLayout layout, int precision)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        JpegPlanarIntImageBuffer image = new JpegPlanarIntImageBuffer();
        image.Width = layout.Width;
        image.Height = layout.Height;
        image.ComponentCount = layout.ComponentCount;
        image.Precision = precision;
        int index = 0;
        while (index < layout.ComponentCount)
        {
            image.Components.Add(AllocateComponent(layout.Components[index], precision));
            index = index + 1;
        }

        return image;
    }

    internal JpegPlanarIntComponentBuffer AllocateComponent(JpegMcuComponentLayout layout, int precision)
    {
        JpegPlanarIntComponentBuffer component = new JpegPlanarIntComponentBuffer();
        component.ComponentIndex = layout.ComponentIndex;
        component.Identifier = layout.Identifier;
        component.Width = layout.SampleWidth;
        component.Height = layout.SampleHeight;
        component.PaddedWidth = layout.PaddedSampleWidth;
        component.PaddedHeight = layout.PaddedSampleHeight;
        component.Stride = layout.PaddedSampleWidth;
        component.Precision = precision;
        component.Samples = new int[layout.PaddedSampleWidth * layout.PaddedSampleHeight];
        return component;
    }

    internal void CopyBlockToComponent(int[] centeredSamples, JpegPlanarIntComponentBuffer component, int precision, int destinationX, int destinationY)
    {
        InverseDct.ValidateCoefficientBlock(centeredSamples, "centeredSamples");
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
                component.Samples[destinationOffset + x] = ToSampleInt(centeredSamples[sourceOffset + x], precision);
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal int ToSampleInt(int centeredSample, int precision)
    {
        ValidatePrecision(precision);
        int center = 1 << (precision - 1);
        int maximum = (1 << precision) - 1;
        int sample = centeredSample + center;
        if (sample < 0)
        {
            return 0;
        }

        if (sample > maximum)
        {
            return maximum;
        }

        return sample;
    }

    internal JpegPlanarIntComponentBuffer FindComponentBuffer(JpegPlanarIntImageBuffer image, int identifier)
    {
        if (image == null)
        {
            throw new System.ArgumentNullException("image");
        }

        foreach (JpegPlanarIntComponentBuffer component in image.Components)
        {
            if (component.Identifier == identifier)
            {
                return component;
            }
        }

        throw JpegInvalidDataException.Create("Planar integer component buffer was not found.");
    }

    internal void ConsumeRestartMarkerIfExpected(JpegSequentialHuffmanEntropyDecoder entropyDecoder, JpegMcuLayout layout, int mcuX, int mcuY)
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

    internal void ValidateInputs(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, byte[] entropyData, int restartInterval)
    {
        ValidateFrame(frame);
        ValidateScan(frame, scan);
        BaselineDecoder.ValidateTables(tables);
        if (entropyData == null)
        {
            throw new System.ArgumentNullException("entropyData");
        }

        if (restartInterval < 0 || restartInterval > 65535)
        {
            throw new System.ArgumentOutOfRangeException("restartInterval");
        }
    }

    internal void ValidateFrame(JpegFrameHeader frame)
    {
        LayoutCalculator.ValidateFrame(frame);
        if (frame.Precision != 12)
        {
            throw JpegInvalidDataException.Create("Extended sequential SOF1 decoder requires 12-bit sample precision.");
        }

        if (frame.Process != JpegProcess.Unknown && frame.Process != JpegProcess.ExtendedSequentialDct)
        {
            throw JpegInvalidDataException.Create("Frame is not an extended sequential SOF1 frame.");
        }

        if (frame.Marker != JpegMarker.Unknown && frame.Marker != JpegMarker.StartOfFrame1)
        {
            throw JpegInvalidDataException.Create("Frame marker is not SOF1.");
        }

        if (frame.Value != 0 && frame.Value != JpegConstants.MarkerStartOfFrame1)
        {
            throw JpegInvalidDataException.Create("Frame marker value is not SOF1.");
        }

        if (frame.Progressive || frame.ArithmeticCoding)
        {
            throw JpegInvalidDataException.Create("Extended sequential SOF1 decoder does not support progressive or arithmetic frames.");
        }
    }

    internal void ValidateScan(JpegFrameHeader frame, JpegScanHeader scan)
    {
        if (scan == null)
        {
            throw new System.ArgumentNullException("scan");
        }

        if (scan.SpectralStart != 0 || scan.SpectralEnd != 63 || scan.SuccessiveHigh != 0 || scan.SuccessiveLow != 0)
        {
            throw JpegInvalidDataException.Create("Extended sequential SOF1 decoder requires a full-spectrum sequential scan.");
        }

        BaselineDecoder.ValidateScan(frame, scan);
    }

    internal void ValidatePrecision(int precision)
    {
        if (precision < 1 || precision > 16)
        {
            throw new System.ArgumentOutOfRangeException("precision");
        }
    }
}
