namespace jpeg;

internal class JpegLosslessSof3HuffmanEncoder
{
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();
    internal JpegLosslessSof3HuffmanDecoder DecoderValidation = new JpegLosslessSof3HuffmanDecoder();
    internal JpegLosslessPredictor Predictor = new JpegLosslessPredictor();
    internal JpegBaselineSof0Encoder BaselineEncoder = new JpegBaselineSof0Encoder();

    internal byte[] Encode(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, JpegPlanarIntImageBuffer source)
    {
        return EncodeWithRestartInterval(frame, scan, tables, source, 0);
    }

    internal byte[] EncodeWithRestartInterval(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, JpegPlanarIntImageBuffer source, int restartInterval)
    {
        ValidateInputs(frame, scan, tables, source, restartInterval);
        JpegLosslessHuffmanEntropyEncoder entropyEncoder = new JpegLosslessHuffmanEntropyEncoder();
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

        entropyEncoder.FlushToByteBoundary();
        return entropyEncoder.ToArray();
    }

    internal void EncodePixelGroup(
        JpegScanHeader scan,
        JpegTableStore tables,
        JpegPlanarIntImageBuffer source,
        JpegLosslessHuffmanEntropyEncoder entropyEncoder,
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
            int sample = ReadSampleForEncoding(component, x, y, scan.SuccessiveLow);
            int difference = sample - prediction;
            entropyEncoder.EncodeDifferenceForScanComponent(tables.HuffmanTables, scanComponent, difference);
            index = index + 1;
        }
    }

    internal int ReadSampleForEncoding(JpegPlanarIntComponentBuffer component, int x, int y, int pointTransform)
    {
        int sample = component.Samples[y * component.Stride + x];
        ValidatePointTransformAlignment(sample, pointTransform);
        return sample >> pointTransform;
    }

    internal bool WriteRestartMarkerIfExpected(JpegLosslessHuffmanEntropyEncoder entropyEncoder, JpegFrameHeader frame, int x, int y)
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
        BaselineEncoder.ValidateTables(tables);
        ValidateSource(source, frame, scan);
        if (restartInterval < 0 || restartInterval > 65535)
        {
            throw new System.ArgumentOutOfRangeException("restartInterval");
        }
    }

    internal void ValidateSource(JpegPlanarIntImageBuffer source, JpegFrameHeader frame, JpegScanHeader scan)
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
            JpegPlanarIntComponentBuffer component = DecoderValidation.IntAllocator.FindComponentBuffer(source, layout.Components[index].Identifier);
            ValidateSourceComponent(component, layout.Components[index], frame.Precision, scan.SuccessiveLow);
            index = index + 1;
        }
    }

    internal void ValidateSourceComponent(JpegPlanarIntComponentBuffer component, JpegMcuComponentLayout layout, int precision, int pointTransform)
    {
        ValidateComponent(component, precision, pointTransform);
        if (component.Width != layout.SampleWidth || component.Height != layout.SampleHeight)
        {
            throw JpegWriteValidationException.Create("Planar integer source component dimensions must match the frame sampling.");
        }
    }

    internal void ValidateComponent(JpegPlanarIntComponentBuffer component, int precision, int pointTransform)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        Predictor.ValidatePointTransform(precision, pointTransform);
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

        ValidateSampleRange(component, precision, pointTransform);
    }

    internal void ValidateSampleRange(JpegPlanarIntComponentBuffer component, int precision, int pointTransform)
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

                ValidatePointTransformAlignment(sample, pointTransform);
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void ValidatePointTransformAlignment(int sample, int pointTransform)
    {
        if (pointTransform == 0)
        {
            return;
        }

        int mask = (1 << pointTransform) - 1;
        if ((sample & mask) != 0)
        {
            throw JpegWriteValidationException.Create("Lossless point transform requires source samples with zero discarded bits.");
        }
    }
}
