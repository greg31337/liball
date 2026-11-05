using System.Collections.Generic;

namespace jpeg;

internal class JpegProgressiveSof2Encoder
{
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();
    internal JpegBaselineSof0Encoder BaselineEncoder = new JpegBaselineSof0Encoder();
    internal JpegProgressiveSof2FullImageDecoder ProgressiveDecoder = new JpegProgressiveSof2FullImageDecoder();

    internal List<JpegProgressiveScanData> Encode(JpegFrameHeader frame, JpegTableStore tables, JpegPlanarImageBuffer source)
    {
        return EncodeWithRestartInterval(frame, tables, source, 0);
    }

    internal List<JpegProgressiveScanData> EncodeWithRestartInterval(JpegFrameHeader frame, JpegTableStore tables, JpegPlanarImageBuffer source, int restartInterval)
    {
        ValidateInputs(frame, tables, source, restartInterval);
        List<JpegScanHeader> scanScript = CreateDefaultScanScript(frame);
        JpegProgressiveCoefficientBuffer coefficients = BuildCoefficientBuffer(frame, tables, source);
        return EncodeScanScript(frame, tables, coefficients, scanScript, restartInterval);
    }

    internal List<JpegProgressiveScanData> EncodeWithScanScript(
        JpegFrameHeader frame,
        JpegTableStore tables,
        JpegPlanarImageBuffer source,
        List<JpegScanHeader> scanScript)
    {
        return EncodeWithScanScriptAndRestartInterval(frame, tables, source, scanScript, 0);
    }

    internal List<JpegProgressiveScanData> EncodeWithScanScriptAndRestartInterval(
        JpegFrameHeader frame,
        JpegTableStore tables,
        JpegPlanarImageBuffer source,
        List<JpegScanHeader> scanScript,
        int restartInterval)
    {
        ValidateInputs(frame, tables, source, restartInterval);
        ValidateScanScript(frame, scanScript);
        JpegProgressiveCoefficientBuffer coefficients = BuildCoefficientBuffer(frame, tables, source);
        return EncodeScanScript(frame, tables, coefficients, scanScript, restartInterval);
    }

    internal List<JpegScanHeader> CreateDefaultScanScript(JpegFrameHeader frame)
    {
        ValidateFrame(frame);
        List<JpegScanHeader> scans = new List<JpegScanHeader>();
        JpegScanHeader dcScan = CreateDcFirstScan(frame);
        scans.Add(dcScan);

        int componentIndex = 0;
        while (componentIndex < frame.Components.Count)
        {
            scans.Add(CreateAcFirstScan(frame.Components[componentIndex].Identifier));
            componentIndex = componentIndex + 1;
        }

        return scans;
    }

    internal List<JpegScanHeader> CopyPublicScanScript(JpegProgressiveScanScript script)
    {
        if (script == null)
        {
            throw new System.ArgumentNullException("script");
        }

        if (script.Scans == null)
        {
            throw JpegWriteValidationException.Create("Progressive scan collection cannot be null.");
        }

        List<JpegScanHeader> scans = new List<JpegScanHeader>();
        foreach (JpegScanHeader source in script.Scans)
        {
            if (source == null)
            {
                throw JpegWriteValidationException.Create("Progressive scan cannot be null.");
            }

            JpegScanHeader scan = new JpegScanHeader();
            scan.ComponentCount = source.ComponentCount;
            scan.SpectralStart = source.SpectralStart;
            scan.SpectralEnd = source.SpectralEnd;
            scan.SuccessiveHigh = source.SuccessiveHigh;
            scan.SuccessiveLow = source.SuccessiveLow;
            if (source.Components == null)
            {
                throw JpegWriteValidationException.Create("Progressive scan component collection cannot be null.");
            }

            foreach (JpegScanComponent sourceComponent in source.Components)
            {
                if (sourceComponent == null)
                {
                    throw JpegWriteValidationException.Create("Progressive scan component cannot be null.");
                }

                JpegScanComponent component = new JpegScanComponent();
                component.Selector = sourceComponent.Selector;
                component.DcEntropyTableIdentifier = sourceComponent.DcEntropyTableIdentifier;
                component.AcEntropyTableIdentifier = sourceComponent.AcEntropyTableIdentifier;
                scan.Components.Add(component);
            }

            scans.Add(scan);
        }

        return scans;
    }

    internal JpegProgressiveCoefficientBuffer BuildCoefficientBuffer(JpegFrameHeader frame, JpegTableStore tables, JpegPlanarImageBuffer source)
    {
        ValidateInputs(frame, tables, source, 0);
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegProgressiveCoefficientBuffer coefficients = ProgressiveDecoder.AllocateCoefficientBuffer(layout);

        int componentIndex = 0;
        while (componentIndex < layout.ComponentCount)
        {
            JpegMcuComponentLayout componentLayout = layout.Components[componentIndex];
            JpegFrameComponent frameComponent = ProgressiveDecoder.BaselineDecoder.FindFrameComponent(frame, componentLayout.Identifier);
            JpegPlanarComponentBuffer componentBuffer = BaselineEncoder.FindComponentBuffer(source, componentLayout.Identifier);
            JpegQuantizationTable quantizationTable = BaselineEncoder.FindQuantizationTable(tables, frameComponent.QuantizationTableIdentifier);
            JpegProgressiveCoefficientComponentBuffer coefficientComponent = ProgressiveDecoder.FindCoefficientComponentBuffer(coefficients, componentLayout.Identifier);
            BuildComponentCoefficients(componentLayout, componentBuffer, quantizationTable, coefficientComponent);
            componentIndex = componentIndex + 1;
        }

        return coefficients;
    }

    internal void BuildComponentCoefficients(
        JpegMcuComponentLayout componentLayout,
        JpegPlanarComponentBuffer componentBuffer,
        JpegQuantizationTable quantizationTable,
        JpegProgressiveCoefficientComponentBuffer coefficientComponent)
    {
        int blockY = 0;
        while (blockY < componentLayout.BlockRows)
        {
            int blockX = 0;
            while (blockX < componentLayout.BlocksPerRow)
            {
                byte[] samples = BaselineEncoder.ReadBlockFromComponent(
                    componentBuffer,
                    blockX * JpegConstants.BlockWidth,
                    blockY * JpegConstants.BlockHeight);
                int[] naturalCoefficients = BaselineEncoder.ForwardDct.ForwardTransformSamples(samples);
                int[] quantizedNaturalCoefficients = BaselineEncoder.Quantizer.QuantizeBlock(naturalCoefficients, quantizationTable);
                int[] zigZagCoefficients = BaselineEncoder.OrderConverter.NaturalToZigZag(quantizedNaturalCoefficients);
                ProgressiveDecoder.CopyBlockToCoefficientBuffer(zigZagCoefficients, coefficientComponent, blockX, blockY);
                blockX = blockX + 1;
            }

            blockY = blockY + 1;
        }
    }

    internal List<JpegProgressiveScanData> EncodeScanScript(
        JpegFrameHeader frame,
        JpegTableStore tables,
        JpegProgressiveCoefficientBuffer coefficients,
        List<JpegScanHeader> scanScript,
        int restartInterval)
    {
        ValidateScanScript(frame, scanScript);
        if (coefficients == null)
        {
            throw new System.ArgumentNullException("coefficients");
        }

        List<JpegProgressiveScanData> encodedScans = new List<JpegProgressiveScanData>();
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        int scanIndex = 0;
        while (scanIndex < scanScript.Count)
        {
            JpegProgressiveScanData scanData = new JpegProgressiveScanData();
            scanData.Scan = scanScript[scanIndex];
            scanData.EntropyData = EncodeScan(tables, coefficients, layout, scanScript[scanIndex], restartInterval);
            encodedScans.Add(scanData);
            scanIndex = scanIndex + 1;
        }

        return encodedScans;
    }

    internal byte[] EncodeScan(
        JpegTableStore tables,
        JpegProgressiveCoefficientBuffer coefficients,
        JpegMcuLayout layout,
        JpegScanHeader scan,
        int restartInterval)
    {
        JpegProgressiveHuffmanEntropyEncoder entropyEncoder = new JpegProgressiveHuffmanEntropyEncoder();
        entropyEncoder.ConfigureRestartInterval(restartInterval);
        if (scan.SpectralStart == 0 && scan.SpectralEnd == 0)
        {
            EncodeDcScan(tables, coefficients, layout, scan, entropyEncoder);
        }
        else
        {
            EncodeAcScan(tables, coefficients, layout, scan, entropyEncoder);
        }

        entropyEncoder.FlushToByteBoundary();
        return entropyEncoder.ToArray();
    }

    internal void EncodeDcScan(
        JpegTableStore tables,
        JpegProgressiveCoefficientBuffer coefficients,
        JpegMcuLayout layout,
        JpegScanHeader scan,
        JpegProgressiveHuffmanEntropyEncoder entropyEncoder)
    {
        if (ProgressiveDecoder.GetScanComponentCount(scan) == 1)
        {
            EncodeSingleComponentDcScan(tables, coefficients, layout, scan, entropyEncoder);
            return;
        }

        EncodeInterleavedDcScan(tables, coefficients, layout, scan, entropyEncoder);
    }

    internal void EncodeInterleavedDcScan(
        JpegTableStore tables,
        JpegProgressiveCoefficientBuffer coefficients,
        JpegMcuLayout layout,
        JpegScanHeader scan,
        JpegProgressiveHuffmanEntropyEncoder entropyEncoder)
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
                    EncodeInterleavedDcComponent(tables, coefficients, layout, scan, entropyEncoder, scanComponentIndex, mcuX, mcuY);
                    scanComponentIndex = scanComponentIndex + 1;
                }

                RecordAndWriteRestart(entropyEncoder, ProgressiveDecoder.IsLastMcu(layout, mcuX, mcuY));
                mcuX = mcuX + 1;
            }

            mcuY = mcuY + 1;
        }
    }

    internal void EncodeInterleavedDcComponent(
        JpegTableStore tables,
        JpegProgressiveCoefficientBuffer coefficients,
        JpegMcuLayout layout,
        JpegScanHeader scan,
        JpegProgressiveHuffmanEntropyEncoder entropyEncoder,
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
                    EncodeDcBlock(tables, coefficients, scan, scanComponent, componentLayout, entropyEncoder, blockX, blockY);
                }

                horizontalIndex = horizontalIndex + 1;
            }

            verticalIndex = verticalIndex + 1;
        }
    }

    internal void EncodeSingleComponentDcScan(
        JpegTableStore tables,
        JpegProgressiveCoefficientBuffer coefficients,
        JpegMcuLayout layout,
        JpegScanHeader scan,
        JpegProgressiveHuffmanEntropyEncoder entropyEncoder)
    {
        JpegScanComponent scanComponent = scan.Components[0];
        JpegMcuComponentLayout componentLayout = LayoutCalculator.FindComponentLayout(layout, scanComponent.Selector);
        int blockY = 0;
        while (blockY < componentLayout.BlockRows)
        {
            int blockX = 0;
            while (blockX < componentLayout.BlocksPerRow)
            {
                EncodeDcBlock(tables, coefficients, scan, scanComponent, componentLayout, entropyEncoder, blockX, blockY);
                RecordAndWriteRestart(entropyEncoder, blockY == componentLayout.BlockRows - 1 && blockX == componentLayout.BlocksPerRow - 1);
                blockX = blockX + 1;
            }

            blockY = blockY + 1;
        }
    }

    internal void EncodeDcBlock(
        JpegTableStore tables,
        JpegProgressiveCoefficientBuffer coefficients,
        JpegScanHeader scan,
        JpegScanComponent scanComponent,
        JpegMcuComponentLayout componentLayout,
        JpegProgressiveHuffmanEntropyEncoder entropyEncoder,
        int blockX,
        int blockY)
    {
        int[] block = ProgressiveDecoder.CopyBlockFromCoefficientBuffer(
            ProgressiveDecoder.FindCoefficientComponentBuffer(coefficients, scanComponent.Selector),
            blockX,
            blockY);
        if (scan.SuccessiveHigh == 0)
        {
            block[0] = AlignCoefficientForFirstScan(block[0], scan.SuccessiveLow);
            entropyEncoder.EncodeDcFirstBlockForScanComponent(tables.HuffmanTables, scan, scanComponent, block, componentLayout.ComponentIndex);
            return;
        }

        entropyEncoder.EncodeDcRefinementBlockForScan(scan, block);
    }

    internal void EncodeAcScan(
        JpegTableStore tables,
        JpegProgressiveCoefficientBuffer coefficients,
        JpegMcuLayout layout,
        JpegScanHeader scan,
        JpegProgressiveHuffmanEntropyEncoder entropyEncoder)
    {
        JpegScanComponent scanComponent = scan.Components[0];
        JpegMcuComponentLayout componentLayout = LayoutCalculator.FindComponentLayout(layout, scanComponent.Selector);
        JpegProgressiveCoefficientComponentBuffer coefficientComponent = ProgressiveDecoder.FindCoefficientComponentBuffer(coefficients, scanComponent.Selector);
        JpegHuffmanTable acTable = entropyEncoder.BaselineEncoder.FindTable(tables.HuffmanTables, 1, scanComponent.AcEntropyTableIdentifier);
        int blockY = 0;
        while (blockY < componentLayout.BlockRows)
        {
            int blockX = 0;
            while (blockX < componentLayout.BlocksPerRow)
            {
                int[] block = ProgressiveDecoder.CopyBlockFromCoefficientBuffer(coefficientComponent, blockX, blockY);
                if (scan.SuccessiveHigh == 0)
                {
                    AlignBlockForAcFirstScan(block, scan);
                    entropyEncoder.EncodeAcFirstBlockForScan(scan, acTable, block);
                }
                else
                {
                    entropyEncoder.EncodeAcRefinementBlockForScan(scan, acTable, block);
                }

                entropyEncoder.FlushEobRun(acTable);
                RecordAndWriteRestart(entropyEncoder, blockY == componentLayout.BlockRows - 1 && blockX == componentLayout.BlocksPerRow - 1);
                blockX = blockX + 1;
            }

            blockY = blockY + 1;
        }
    }

    internal void RecordAndWriteRestart(JpegProgressiveHuffmanEntropyEncoder entropyEncoder, bool isLastUnit)
    {
        entropyEncoder.RecordMcuEncoded();
        if (!entropyEncoder.ShouldWriteRestartMarker())
        {
            return;
        }

        if (isLastUnit)
        {
            return;
        }

        entropyEncoder.WriteRestartMarker();
    }

    internal JpegScanHeader CreateDcFirstScan(JpegFrameHeader frame)
    {
        JpegScanHeader scan = new JpegScanHeader();
        scan.ComponentCount = frame.Components.Count;
        scan.SpectralStart = 0;
        scan.SpectralEnd = 0;
        scan.SuccessiveHigh = 0;
        scan.SuccessiveLow = 0;
        int index = 0;
        while (index < frame.Components.Count)
        {
            scan.Components.Add(CreateScanComponent(frame.Components[index].Identifier));
            index = index + 1;
        }

        return scan;
    }

    internal JpegScanHeader CreateAcFirstScan(int componentIdentifier)
    {
        JpegScanHeader scan = new JpegScanHeader();
        scan.ComponentCount = 1;
        scan.SpectralStart = 1;
        scan.SpectralEnd = 63;
        scan.SuccessiveHigh = 0;
        scan.SuccessiveLow = 0;
        scan.Components.Add(CreateScanComponent(componentIdentifier));
        return scan;
    }

    internal JpegScanComponent CreateScanComponent(int selector)
    {
        JpegScanComponent component = new JpegScanComponent();
        component.Selector = selector;
        component.DcEntropyTableIdentifier = 0;
        component.AcEntropyTableIdentifier = 0;
        return component;
    }

    internal void AlignBlockForAcFirstScan(int[] block, JpegScanHeader scan)
    {
        ProgressiveDecoder.ValidateCoefficientBlock(block);
        int coefficientIndex = scan.SpectralStart;
        while (coefficientIndex <= scan.SpectralEnd)
        {
            block[coefficientIndex] = AlignCoefficientForFirstScan(block[coefficientIndex], scan.SuccessiveLow);
            coefficientIndex = coefficientIndex + 1;
        }
    }

    internal int AlignCoefficientForFirstScan(int coefficient, int successiveLow)
    {
        if (successiveLow < 0 || successiveLow > 15)
        {
            throw new System.ArgumentOutOfRangeException("successiveLow");
        }

        if (successiveLow == 0 || coefficient == 0)
        {
            return coefficient;
        }

        int bitValue = 1 << successiveLow;
        if (coefficient > 0)
        {
            return (coefficient / bitValue) * bitValue;
        }

        long magnitude = 0L - (long)coefficient;
        long alignedMagnitude = ((magnitude + bitValue - 1L) / bitValue) * bitValue;
        return (int)(0L - alignedMagnitude);
    }

    internal void ValidateInputs(JpegFrameHeader frame, JpegTableStore tables, JpegPlanarImageBuffer source, int restartInterval)
    {
        ValidateFrame(frame);
        BaselineEncoder.ValidateTables(tables);
        ValidateSource(source, frame);
        if (restartInterval < 0 || restartInterval > 65535)
        {
            throw new System.ArgumentOutOfRangeException("restartInterval");
        }
    }

    internal void ValidateFrame(JpegFrameHeader frame)
    {
        ProgressiveDecoder.ValidateFrame(frame);
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
            JpegPlanarComponentBuffer component = BaselineEncoder.FindComponentBuffer(source, layout.Components[index].Identifier);
            BaselineEncoder.ValidateSourceComponent(component, layout.Components[index]);
            index = index + 1;
        }
    }

    internal void ValidateScanScript(JpegFrameHeader frame, List<JpegScanHeader> scanScript)
    {
        ValidateFrame(frame);
        if (scanScript == null)
        {
            throw new System.ArgumentNullException("scanScript");
        }

        if (scanScript.Count <= 0)
        {
            throw JpegWriteValidationException.Create("Progressive scan script must contain at least one scan.");
        }

        int[] coefficientStates = CreateCoefficientStateArray(frame);
        int index = 0;
        while (index < scanScript.Count)
        {
            ValidateEncoderScanDefinition(frame, scanScript[index]);
            ValidateScanProgression(frame, scanScript[index], coefficientStates);
            index = index + 1;
        }

        ValidateScanScriptCompletes(frame, coefficientStates);
    }

    internal void ValidateEncoderScanDefinition(JpegFrameHeader frame, JpegScanHeader scan)
    {
        if (scan == null)
        {
            throw JpegWriteValidationException.Create("Progressive scan cannot be null.");
        }

        if (scan.Components == null || scan.Components.Count <= 0 || scan.Components.Count > 4)
        {
            throw JpegWriteValidationException.Create("Progressive scans must contain between one and four components.");
        }

        if (scan.ComponentCount != 0 && scan.ComponentCount != scan.Components.Count)
        {
            throw JpegWriteValidationException.Create("Progressive scan component count must match its component list.");
        }

        if (scan.SpectralStart < 0 || scan.SpectralStart > 63
            || scan.SpectralEnd < 0 || scan.SpectralEnd > 63
            || scan.SpectralEnd < scan.SpectralStart)
        {
            throw JpegWriteValidationException.Create("Progressive spectral selection must be an ordered range from zero through sixty-three.");
        }

        if (scan.SpectralStart == 0 && scan.SpectralEnd != 0)
        {
            throw JpegWriteValidationException.Create("Progressive DC scans must select coefficient zero only.");
        }

        if (scan.SpectralStart > 0 && scan.Components.Count != 1)
        {
            throw JpegWriteValidationException.Create("Progressive AC scans must contain exactly one component.");
        }

        if (scan.SuccessiveHigh < 0 || scan.SuccessiveHigh > 15
            || scan.SuccessiveLow < 0 || scan.SuccessiveLow > 15)
        {
            throw JpegWriteValidationException.Create("Progressive successive approximation values must be between zero and fifteen.");
        }

        if (scan.SuccessiveHigh > 0 && scan.SuccessiveLow != scan.SuccessiveHigh - 1)
        {
            throw JpegWriteValidationException.Create("Progressive refinement scans must decrease successive approximation by one bit.");
        }

        int componentIndex = 0;
        while (componentIndex < scan.Components.Count)
        {
            ValidateEncoderScanComponent(frame, scan, componentIndex);
            componentIndex = componentIndex + 1;
        }
    }

    internal void ValidateEncoderScanComponent(JpegFrameHeader frame, JpegScanHeader scan, int componentIndex)
    {
        JpegScanComponent component = scan.Components[componentIndex];
        if (component.DcEntropyTableIdentifier < 0 || component.DcEntropyTableIdentifier > 3
            || component.AcEntropyTableIdentifier < 0 || component.AcEntropyTableIdentifier > 3)
        {
            throw JpegWriteValidationException.Create("Progressive scan entropy-table identifiers must be between zero and three.");
        }

        GetFrameComponentIndex(frame, component.Selector);
        int previousIndex = 0;
        while (previousIndex < componentIndex)
        {
            if (scan.Components[previousIndex].Selector == component.Selector)
            {
                throw JpegWriteValidationException.Create("Progressive scan component selectors must be unique.");
            }

            previousIndex = previousIndex + 1;
        }
    }

    internal int[] CreateCoefficientStateArray(JpegFrameHeader frame)
    {
        int[] states = new int[frame.Components.Count * JpegConstants.BlockSampleCount];
        int index = 0;
        while (index < states.Length)
        {
            states[index] = -1;
            index = index + 1;
        }

        return states;
    }

    internal void ValidateScanProgression(JpegFrameHeader frame, JpegScanHeader scan, int[] coefficientStates)
    {
        int componentIndex = 0;
        while (componentIndex < scan.Components.Count)
        {
            JpegScanComponent component = scan.Components[componentIndex];
            int frameComponentIndex = GetFrameComponentIndex(frame, component.Selector);
            ValidateScanComponentProgression(frameComponentIndex, scan, coefficientStates);
            componentIndex = componentIndex + 1;
        }
    }

    internal void ValidateScanComponentProgression(int frameComponentIndex, JpegScanHeader scan, int[] coefficientStates)
    {
        if (scan.SpectralStart > 0 && coefficientStates[frameComponentIndex * JpegConstants.BlockSampleCount] < 0)
        {
            throw JpegWriteValidationException.Create("Progressive AC scans require the component DC coefficient to be introduced first.");
        }

        int coefficientIndex = scan.SpectralStart;
        while (coefficientIndex <= scan.SpectralEnd)
        {
            ValidateCoefficientProgression(frameComponentIndex, coefficientIndex, scan, coefficientStates);
            coefficientIndex = coefficientIndex + 1;
        }
    }

    internal void ValidateCoefficientProgression(int frameComponentIndex, int coefficientIndex, JpegScanHeader scan, int[] coefficientStates)
    {
        int stateIndex = frameComponentIndex * JpegConstants.BlockSampleCount + coefficientIndex;
        int currentLow = coefficientStates[stateIndex];
        if (scan.SuccessiveHigh == 0)
        {
            if (currentLow >= 0)
            {
                throw JpegWriteValidationException.Create("Progressive scan script defines a coefficient first pass more than once.");
            }

            coefficientStates[stateIndex] = scan.SuccessiveLow;
            return;
        }

        if (currentLow != scan.SuccessiveHigh)
        {
            throw JpegWriteValidationException.Create("Progressive refinement scan does not continue the previous successive approximation bit.");
        }

        coefficientStates[stateIndex] = scan.SuccessiveLow;
    }

    internal void ValidateScanScriptCompletes(JpegFrameHeader frame, int[] coefficientStates)
    {
        int componentIndex = 0;
        while (componentIndex < frame.Components.Count)
        {
            int coefficientIndex = 0;
            while (coefficientIndex < JpegConstants.BlockSampleCount)
            {
                int state = coefficientStates[componentIndex * JpegConstants.BlockSampleCount + coefficientIndex];
                if (state != 0)
                {
                    throw JpegWriteValidationException.Create("Progressive scan script must cover every coefficient down to successive approximation bit zero.");
                }

                coefficientIndex = coefficientIndex + 1;
            }

            componentIndex = componentIndex + 1;
        }
    }

    internal int GetFrameComponentIndex(JpegFrameHeader frame, int selector)
    {
        int index = 0;
        while (index < frame.Components.Count)
        {
            if (frame.Components[index].Identifier == selector)
            {
                return index;
            }

            index = index + 1;
        }

        throw JpegWriteValidationException.Create("Scan component selector does not match a frame component.");
    }
}
