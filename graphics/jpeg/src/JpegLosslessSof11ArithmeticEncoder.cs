namespace jpeg;

internal class JpegLosslessSof11ArithmeticEncoder
{
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();
    internal JpegLosslessSof11ArithmeticDecoder DecoderValidation = new JpegLosslessSof11ArithmeticDecoder();
    internal JpegLosslessPredictor Predictor = new JpegLosslessPredictor();
    internal JpegLosslessSof3HuffmanEncoder SourceValidation = new JpegLosslessSof3HuffmanEncoder();

    internal byte[] Encode(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, JpegPlanarIntImageBuffer source)
    {
        return EncodeWithRestartInterval(frame, scan, tables, source, 0);
    }

    internal byte[] EncodeWithRestartInterval(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, JpegPlanarIntImageBuffer source, int restartInterval)
    {
        ValidateInputs(frame, scan, tables, source, restartInterval);
        JpegLosslessArithmeticEntropyEncoder entropyEncoder = new JpegLosslessArithmeticEntropyEncoder();
        entropyEncoder.ConfigureRestartInterval(restartInterval);

        int initialPrediction = Predictor.GetInitialPrediction(frame.Precision, scan.SuccessiveLow);
        bool useInitialPrediction = false;
        int y = 0;
        while (y < frame.Height)
        {
            int x = 0;
            while (x < frame.Width)
            {
                EncodePixelGroup(scan, tables, source, entropyEncoder, x, y, initialPrediction, useInitialPrediction);
                useInitialPrediction = false;
                entropyEncoder.RecordMcuEncoded();
                if (WriteRestartMarkerIfExpected(entropyEncoder, frame, x, y))
                {
                    useInitialPrediction = true;
                }

                x = x + 1;
            }

            y = y + 1;
        }

        entropyEncoder.Finish();
        return entropyEncoder.ToArray();
    }

    internal void EncodePixelGroup(
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegPlanarIntImageBuffer source,
        JpegLosslessArithmeticEntropyEncoder entropyEncoder,
        int x,
        int y,
        int initialPrediction,
        bool useInitialPrediction)
    {
        int index = 0;
        while (index < scan.Components.Count)
        {
            JpegScanComponent scanComponent = scan.Components[index];
            JpegPlanarIntComponentBuffer component = DecoderValidation.IntAllocator.FindComponentBuffer(source, scanComponent.Selector);
            int prediction = Predictor.Predict(
                component,
                x,
                y,
                scan.SpectralStart,
                initialPrediction,
                scan.SuccessiveLow,
                useInitialPrediction);
            int sample = SourceValidation.ReadSampleForEncoding(component, x, y, scan.SuccessiveLow);
            int difference = sample - prediction;
            entropyEncoder.EncodeDifferenceForScanComponent(
                tables.ArithmeticConditioningTables,
                scanComponent,
                component.ComponentIndex,
                difference);
            index = index + 1;
        }
    }

    internal bool WriteRestartMarkerIfExpected(JpegLosslessArithmeticEntropyEncoder entropyEncoder, JpegFrameHeader frame, int x, int y)
    {
        if (!entropyEncoder.ShouldWriteRestartMarker())
        {
            return false;
        }

        if (DecoderValidation.IsLastPixel(frame, x, y))
        {
            return false;
        }

        entropyEncoder.WriteRestartMarker();
        return true;
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
        DecoderValidation.ValidateTables(tables);
        SourceValidation.ValidateSource(source, frame, scan);
        if (restartInterval < 0 || restartInterval > 65535)
        {
            throw new System.ArgumentOutOfRangeException("restartInterval");
        }
    }
}
