namespace jpeg;

internal class JpegSequentialDctScanEncoder
{
    internal JpegBaselineSof0Encoder BaselineEncoder = new JpegBaselineSof0Encoder();
    internal JpegExtendedSequentialSof1Encoder ExtendedEncoder = new JpegExtendedSequentialSof1Encoder();
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();

    internal byte[] EncodeBaselineComponentScan(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegPlanarImageBuffer source,
        int restartInterval)
    {
        ValidateSingleComponentScan(frame, scan, restartInterval);
        BaselineEncoder.ValidateTables(tables);
        BaselineEncoder.ValidateSource(source, frame);
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegScanComponent scanComponent = scan.Components[0];
        JpegMcuComponentLayout componentLayout = LayoutCalculator.FindComponentLayout(layout, scanComponent.Selector);
        JpegPlanarComponentBuffer component = BaselineEncoder.FindComponentBuffer(source, scanComponent.Selector);
        JpegFrameComponent frameComponent = BaselineEncoder.BaselineDecoderValidation.FindFrameComponent(frame, scanComponent.Selector);
        JpegQuantizationTable quantizationTable = BaselineEncoder.FindQuantizationTable(tables, frameComponent.QuantizationTableIdentifier);
        JpegBaselineHuffmanEntropyEncoder entropyEncoder = new JpegBaselineHuffmanEntropyEncoder();
        entropyEncoder.ConfigureRestartInterval(restartInterval);

        int blockY = 0;
        while (blockY < componentLayout.BlockRows)
        {
            int blockX = 0;
            while (blockX < componentLayout.BlocksPerRow)
            {
                byte[] samples = BaselineEncoder.ReadBlockFromComponent(
                    component,
                    blockX * JpegConstants.BlockWidth,
                    blockY * JpegConstants.BlockHeight);
                int[] natural = BaselineEncoder.ForwardDct.ForwardTransformSamples(samples);
                int[] quantized = BaselineEncoder.Quantizer.QuantizeBlock(natural, quantizationTable);
                int[] zigZag = BaselineEncoder.OrderConverter.NaturalToZigZag(quantized);
                entropyEncoder.EncodeBlockForScanComponent(
                    tables.HuffmanTables,
                    scanComponent,
                    zigZag,
                    componentLayout.ComponentIndex);
                entropyEncoder.RecordMcuEncoded();
                WriteBaselineRestartIfExpected(entropyEncoder, componentLayout, blockX, blockY);
                blockX = blockX + 1;
            }

            blockY = blockY + 1;
        }

        entropyEncoder.FlushToByteBoundary();
        return entropyEncoder.ToArray();
    }

    internal byte[] EncodeExtendedComponentScan(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegPlanarIntImageBuffer source,
        int restartInterval)
    {
        ValidateSingleComponentScan(frame, scan, restartInterval);
        BaselineEncoder.ValidateTables(tables);
        ExtendedEncoder.ValidateSource(source, frame);
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegScanComponent scanComponent = scan.Components[0];
        JpegMcuComponentLayout componentLayout = LayoutCalculator.FindComponentLayout(layout, scanComponent.Selector);
        JpegPlanarIntComponentBuffer component = ExtendedEncoder.DecoderValidation.FindComponentBuffer(source, scanComponent.Selector);
        JpegFrameComponent frameComponent = ExtendedEncoder.DecoderValidation.BaselineDecoder.FindFrameComponent(frame, scanComponent.Selector);
        JpegQuantizationTable quantizationTable = BaselineEncoder.FindQuantizationTable(tables, frameComponent.QuantizationTableIdentifier);
        JpegSequentialHuffmanEntropyEncoder entropyEncoder = new JpegSequentialHuffmanEntropyEncoder();
        entropyEncoder.ConfigureRestartInterval(restartInterval);

        int blockY = 0;
        while (blockY < componentLayout.BlockRows)
        {
            int blockX = 0;
            while (blockX < componentLayout.BlocksPerRow)
            {
                int[] centered = ExtendedEncoder.ReadCenteredBlockFromComponent(
                    component,
                    blockX * JpegConstants.BlockWidth,
                    blockY * JpegConstants.BlockHeight,
                    frame.Precision);
                int[] natural = ExtendedEncoder.ForwardDct.ForwardTransformBlock(centered);
                int[] quantized = ExtendedEncoder.Quantizer.QuantizeBlock(natural, quantizationTable);
                int[] zigZag = ExtendedEncoder.OrderConverter.NaturalToZigZag(quantized);
                entropyEncoder.EncodeBlockForScanComponent(
                    tables.HuffmanTables,
                    scanComponent,
                    zigZag,
                    componentLayout.ComponentIndex);
                entropyEncoder.RecordMcuEncoded();
                WriteExtendedRestartIfExpected(entropyEncoder, componentLayout, blockX, blockY);
                blockX = blockX + 1;
            }

            blockY = blockY + 1;
        }

        entropyEncoder.FlushToByteBoundary();
        return entropyEncoder.ToArray();
    }

    internal void ValidateSingleComponentScan(JpegFrameHeader frame, JpegScanHeader scan, int restartInterval)
    {
        LayoutCalculator.ValidateFrame(frame);
        if (scan == null)
        {
            throw new System.ArgumentNullException("scan");
        }

        if (scan.Components == null || scan.Components.Count != 1 || (scan.ComponentCount != 0 && scan.ComponentCount != 1))
        {
            throw JpegWriteValidationException.Create("Non-interleaved sequential scans must contain exactly one component.");
        }

        if (scan.SpectralStart != 0 || scan.SpectralEnd != 63 || scan.SuccessiveHigh != 0 || scan.SuccessiveLow != 0)
        {
            throw JpegWriteValidationException.Create("Sequential DCT scans must cover coefficients zero through sixty-three.");
        }

        BaselineEncoder.BaselineDecoderValidation.ValidateScanComponent(frame, scan, 0);
        if (restartInterval < 0 || restartInterval > 65535)
        {
            throw new System.ArgumentOutOfRangeException("restartInterval");
        }
    }

    internal void WriteBaselineRestartIfExpected(
        JpegBaselineHuffmanEntropyEncoder entropyEncoder,
        JpegMcuComponentLayout layout,
        int blockX,
        int blockY)
    {
        if (entropyEncoder.ShouldWriteRestartMarker() && !IsLastBlock(layout, blockX, blockY))
        {
            entropyEncoder.WriteRestartMarker();
        }
    }

    internal void WriteExtendedRestartIfExpected(
        JpegSequentialHuffmanEntropyEncoder entropyEncoder,
        JpegMcuComponentLayout layout,
        int blockX,
        int blockY)
    {
        if (entropyEncoder.ShouldWriteRestartMarker() && !IsLastBlock(layout, blockX, blockY))
        {
            entropyEncoder.WriteRestartMarker();
        }
    }

    internal bool IsLastBlock(JpegMcuComponentLayout layout, int blockX, int blockY)
    {
        return blockX == layout.BlocksPerRow - 1 && blockY == layout.BlockRows - 1;
    }
}
