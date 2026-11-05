namespace jpeg;

internal class JpegBaselineSof0RowDecodeState
{
    internal JpegFrameHeader Frame = new JpegFrameHeader();
    internal JpegScanHeader Scan = new JpegScanHeader();
    internal JpegTableStore Tables = new JpegTableStore();
    internal JpegMcuLayout Layout = new JpegMcuLayout();
    internal JpegPlanarImageBuffer Destination = new JpegPlanarImageBuffer();
    internal JpegBaselineHuffmanEntropyDecoder EntropyDecoder = new JpegBaselineHuffmanEntropyDecoder(new byte[0]);
    internal JpegReadOptions Options = new JpegReadOptions();
    internal int RestartInterval;
    internal int McuRow;
    internal int DecodedMcuCount;
    internal bool Completed;
}

internal class JpegBaselineSof0RowByRowDecoder
{
    internal JpegBaselineSof0FullImageDecoder FullImageDecoder = new JpegBaselineSof0FullImageDecoder();

    internal JpegBaselineSof0RowDecodeState CreateState(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, byte[] entropyData)
    {
        return CreateStateWithOptions(frame, scan, tables, entropyData, 0, CreateDefaultReadOptions());
    }

    internal JpegBaselineSof0RowDecodeState CreateStateWithRestartInterval(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        byte[] entropyData,
        int restartInterval)
    {
        return CreateStateWithOptions(frame, scan, tables, entropyData, restartInterval, CreateDefaultReadOptions());
    }

    internal JpegBaselineSof0RowDecodeState CreateStateWithOptions(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        byte[] entropyData,
        int restartInterval,
        JpegReadOptions options)
    {
        FullImageDecoder.ValidateInputs(frame, scan, tables, entropyData, restartInterval, options);
        JpegMcuLayout layout = FullImageDecoder.LayoutCalculator.Calculate(frame);
        FullImageDecoder.EnforceComponentBufferLimit(layout, JpegConstants.BlockWidth, options);
        JpegPlanarImageBuffer destination = FullImageDecoder.PlanarAllocator.Allocate(layout);
        return CreateStateIntoWithOptions(frame, scan, tables, entropyData, restartInterval, options, destination);
    }

    internal JpegBaselineSof0RowDecodeState CreateStateInto(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        byte[] entropyData,
        JpegPlanarImageBuffer destination)
    {
        return CreateStateIntoWithOptions(frame, scan, tables, entropyData, 0, CreateDefaultReadOptions(), destination);
    }

    internal JpegBaselineSof0RowDecodeState CreateStateIntoWithOptions(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        byte[] entropyData,
        int restartInterval,
        JpegReadOptions options,
        JpegPlanarImageBuffer destination)
    {
        FullImageDecoder.ValidateInputs(frame, scan, tables, entropyData, restartInterval, options);
        FullImageDecoder.ValidateDestination(destination, frame);

        JpegBaselineSof0RowDecodeState state = new JpegBaselineSof0RowDecodeState();
        state.Frame = frame;
        state.Scan = scan;
        state.Tables = tables;
        state.Layout = FullImageDecoder.LayoutCalculator.Calculate(frame);
        state.Destination = destination;
        state.EntropyDecoder = new JpegBaselineHuffmanEntropyDecoder(entropyData);
        state.EntropyDecoder.ConfigureRestartInterval(restartInterval);
        state.Options = options;
        state.RestartInterval = restartInterval;
        state.McuRow = 0;
        state.DecodedMcuCount = 0;
        state.Completed = state.Layout.McuRows == 0;
        return state;
    }

    internal JpegPlanarImageBuffer DecodeRows(JpegFrameHeader frame, JpegScanHeader scan, JpegTableStore tables, byte[] entropyData)
    {
        return DecodeRowsWithOptions(frame, scan, tables, entropyData, 0, CreateDefaultReadOptions());
    }

    internal JpegPlanarImageBuffer DecodeRowsWithOptions(
        JpegFrameHeader frame,
        JpegScanHeader scan,
        JpegTableStore tables,
        byte[] entropyData,
        int restartInterval,
        JpegReadOptions options)
    {
        JpegBaselineSof0RowDecodeState state = CreateStateWithOptions(frame, scan, tables, entropyData, restartInterval, options);
        return DecodeRemainingRows(state);
    }

    internal JpegPlanarImageBuffer DecodeRemainingRows(JpegBaselineSof0RowDecodeState state)
    {
        ValidateState(state);
        while (DecodeNextRow(state))
        {
        }

        return state.Destination;
    }

    internal bool DecodeNextRow(JpegBaselineSof0RowDecodeState state)
    {
        ValidateState(state);
        if (state.Completed)
        {
            return false;
        }

        int mcuX = 0;
        while (mcuX < state.Layout.McusPerRow)
        {
            FullImageDecoder.DecodeMcu(
                state.Frame,
                state.Scan,
                state.Tables,
                state.Layout,
                state.EntropyDecoder,
                state.Options,
                state.Destination,
                mcuX,
                state.McuRow);
            state.EntropyDecoder.RecordMcuDecoded();
            FullImageDecoder.ConsumeRestartMarkerIfExpected(state.EntropyDecoder, state.Layout, mcuX, state.McuRow);
            state.DecodedMcuCount = state.DecodedMcuCount + 1;
            mcuX = mcuX + 1;
        }

        state.McuRow = state.McuRow + 1;
        if (state.McuRow >= state.Layout.McuRows)
        {
            state.Completed = true;
        }

        return true;
    }

    internal int GetDecodedSampleRows(JpegBaselineSof0RowDecodeState state, int componentIdentifier)
    {
        ValidateState(state);
        JpegMcuComponentLayout component = FullImageDecoder.LayoutCalculator.FindComponentLayout(state.Layout, componentIdentifier);
        int rows = state.McuRow * component.VerticalSamplingFactor * JpegConstants.BlockHeight;
        if (rows > component.SampleHeight)
        {
            return component.SampleHeight;
        }

        return rows;
    }

    internal void ValidateState(JpegBaselineSof0RowDecodeState state)
    {
        if (state == null)
        {
            throw new System.ArgumentNullException("state");
        }

        if (state.Layout == null)
        {
            throw JpegInvalidDataException.Create("Row decoder layout cannot be null.");
        }

        if (state.EntropyDecoder == null)
        {
            throw JpegInvalidDataException.Create("Row decoder entropy decoder cannot be null.");
        }

        if (state.Destination == null)
        {
            throw JpegInvalidDataException.Create("Row decoder destination cannot be null.");
        }

        if (state.McuRow < 0 || state.McuRow > state.Layout.McuRows)
        {
            throw JpegInvalidDataException.Create("Row decoder state has an invalid MCU row.");
        }
    }

    internal JpegReadOptions CreateDefaultReadOptions()
    {
        return new JpegReadOptions();
    }
}
