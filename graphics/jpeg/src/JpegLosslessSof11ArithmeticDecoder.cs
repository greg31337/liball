namespace jpeg;

internal class JpegLosslessSof11ArithmeticDecoder
{
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();
    internal JpegExtendedSequentialSof1Decoder IntAllocator = new JpegExtendedSequentialSof1Decoder();
    internal JpegLosslessPredictor Predictor = new JpegLosslessPredictor();
    internal JpegBaselineSof0FullImageDecoder BaselineDecoder = new JpegBaselineSof0FullImageDecoder();

    internal JpegPlanarIntImageBuffer Decode(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, byte[] entropyData)
    {
        return DecodeWithRestartInterval(frame, scan, tables, entropyData, 0);
    }

    internal JpegPlanarIntImageBuffer DecodeWithRestartInterval(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, byte[] entropyData, int restartInterval)
    {
        ValidateInputs(frame, scan, tables, entropyData, restartInterval);
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegPlanarIntImageBuffer destination = IntAllocator.Allocate(layout, frame.Precision);
        JpegLosslessArithmeticEntropyDecoder entropyDecoder = new JpegLosslessArithmeticEntropyDecoder(entropyData);
        entropyDecoder.ConfigureRestartInterval(restartInterval);

        int initialPrediction = Predictor.GetInitialPrediction(frame.Precision, scan.SuccessiveLow);
        bool useInitialPrediction = false;
        int y = 0;
        while (y < frame.Height)
        {
            int x = 0;
            while (x < frame.Width)
            {
                DecodePixelGroup(scan, tables, destination, entropyDecoder, x, y, initialPrediction, useInitialPrediction);
                useInitialPrediction = false;
                entropyDecoder.RecordMcuDecoded();
                if (ConsumeRestartMarkerIfExpected(entropyDecoder, frame, x, y))
                {
                    useInitialPrediction = true;
                }

                x = x + 1;
            }

            y = y + 1;
        }

        return destination;
    }

    internal void DecodePixelGroup(
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegPlanarIntImageBuffer destination,
        JpegLosslessArithmeticEntropyDecoder entropyDecoder,
        int x,
        int y,
        int initialPrediction,
        bool useInitialPrediction)
    {
        int index = 0;
        while (index < scan.Components.Count)
        {
            JpegScanComponent scanComponent = scan.Components[index];
            JpegPlanarIntComponentBuffer component = IntAllocator.FindComponentBuffer(destination, scanComponent.Selector);
            int prediction = Predictor.Predict(
                component,
                x,
                y,
                scan.SpectralStart,
                initialPrediction,
                scan.SuccessiveLow,
                useInitialPrediction);
            int difference = entropyDecoder.DecodeDifferenceForScanComponent(
                tables.ArithmeticConditioningTables,
                scanComponent,
                component.ComponentIndex);
            int reconstructed = prediction + difference;
            component.Samples[y * component.Stride + x] = CompleteSample(reconstructed, destination.Precision, scan.SuccessiveLow);
            index = index + 1;
        }
    }

    internal bool ConsumeRestartMarkerIfExpected(JpegLosslessArithmeticEntropyDecoder entropyDecoder, JpegFrameHeader frame, int x, int y)
    {
        if (!entropyDecoder.ShouldExpectRestartMarker())
        {
            return false;
        }

        if (IsLastPixel(frame, x, y))
        {
            return false;
        }

        entropyDecoder.AdvanceToMarker();
        entropyDecoder.ConsumeRestartMarkerLookahead();
        return true;
    }

    internal bool IsLastPixel(JpegFrameHeader frame, int x, int y)
    {
        if (frame == null)
        {
            throw new System.ArgumentNullException("frame");
        }

        return y == frame.Height - 1 && x == frame.Width - 1;
    }

    internal int CompleteSample(int reconstructed, int precision, int pointTransform)
    {
        Predictor.ValidatePointTransform(precision, pointTransform);
        int sample = reconstructed << pointTransform;
        ValidateSampleRange(sample, precision);
        return sample;
    }

    internal void ValidateSampleRange(int sample, int precision)
    {
        int maximum = (1 << precision) - 1;
        if (sample < 0 || sample > maximum)
        {
            throw JpegInvalidDataException.Create("Lossless decoded sample is outside the frame precision range.");
        }
    }

    internal void ValidateInputs(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, byte[] entropyData, int restartInterval)
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
    }

    internal void ValidateFrame(JpegFrameHeader frame)
    {
        LayoutCalculator.ValidateFrame(frame);
        if (frame.Height <= 0)
        {
            throw JpegInvalidDataException.Create("Lossless SOF11 decoder requires a defined image height.");
        }

        Predictor.ValidatePointTransform(frame.Precision, 0);
        if (frame.Process != JpegProcess.Unknown && frame.Process != JpegProcess.LosslessSequentialArithmetic)
        {
            throw JpegInvalidDataException.Create("Frame is not a lossless sequential SOF11 frame.");
        }

        if (frame.Marker != JpegMarker.Unknown && frame.Marker != JpegMarker.StartOfFrame11)
        {
            throw JpegInvalidDataException.Create("Frame marker is not SOF11.");
        }

        if (frame.Value != 0 && frame.Value != JpegConstants.MarkerStartOfFrame11)
        {
            throw JpegInvalidDataException.Create("Frame marker value is not SOF11.");
        }

        if (frame.Progressive)
        {
            throw JpegInvalidDataException.Create("Lossless SOF11 decoder does not support progressive frames.");
        }

        ValidateLosslessSampling(frame);
    }

    internal void ValidateLosslessSampling(JpegFrameHeader frame)
    {
        int index = 0;
        while (index < frame.Components.Count)
        {
            JpegFrameComponent component = frame.Components[index];
            if (component.HorizontalSamplingFactor != 1 || component.VerticalSamplingFactor != 1)
            {
                throw JpegInvalidDataException.Create("Lossless SOF11 decoder requires 1x1 component sampling.");
            }

            index = index + 1;
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

        Predictor.ValidatePredictorSelection(scan.SpectralStart);
        if (scan.SpectralEnd != 0)
        {
            throw JpegInvalidDataException.Create("Lossless SOF11 scan spectral end must be zero.");
        }

        if (scan.SuccessiveHigh != 0)
        {
            throw JpegInvalidDataException.Create("Lossless SOF11 scan successive high approximation must be zero.");
        }

        Predictor.ValidatePointTransform(frame.Precision, scan.SuccessiveLow);
        int frameComponentCount = LayoutCalculator.GetComponentCount(frame);
        int scanComponentCount = scan.Components.Count;
        if (scan.ComponentCount != 0 && scan.ComponentCount != scanComponentCount)
        {
            throw JpegInvalidDataException.Create("Scan component count must match the component list.");
        }

        if (scanComponentCount != frameComponentCount)
        {
            throw JpegInvalidDataException.Create("Lossless SOF11 decoder requires one scan containing every frame component.");
        }

        int index = 0;
        while (index < scanComponentCount)
        {
            BaselineDecoder.ValidateScanComponent(frame, scan, index);
            index = index + 1;
        }
    }

    internal void ValidateTables(JpegTableStore tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }
    }
}
