using System.Collections.Generic;

namespace jpeg;

internal class JpegLosslessArithmeticEntropyDecoder
{
    internal JpegArithmeticDcCoefficientDecoder DcDecoder = new JpegArithmeticDcCoefficientDecoder(new byte[0]);
    internal JpegArithmeticDecoderStateMachine StateMachine = new JpegArithmeticDecoderStateMachine(new byte[0]);
    internal int RestartInterval;
    internal int McuCountSinceRestart;
    internal int ExpectedRestartMarkerIndex;
    internal int RestartMarkerCount;

    internal JpegLosslessArithmeticEntropyDecoder(byte[] entropyData)
    {
        DcDecoder = new JpegArithmeticDcCoefficientDecoder(CreateEntropyDataWithTerminatingMarker(entropyData));
        InitializeAliases();
        ResetRestartState();
    }

    internal static JpegLosslessArithmeticEntropyDecoder FromStateMachine(JpegArithmeticDecoderStateMachine stateMachine)
    {
        if (stateMachine == null)
        {
            throw new System.ArgumentNullException("stateMachine");
        }

        JpegLosslessArithmeticEntropyDecoder decoder = new JpegLosslessArithmeticEntropyDecoder(new byte[0]);
        decoder.DcDecoder = JpegArithmeticDcCoefficientDecoder.FromStateMachine(stateMachine);
        decoder.InitializeAliases();
        decoder.ResetRestartState();
        return decoder;
    }

    internal void InitializeAliases()
    {
        StateMachine = DcDecoder.StateMachine;
    }

    internal int DecodeDifference(JpegArithmeticConditioningTable conditioningTable, int componentIndex)
    {
        return DcDecoder.DecodeDcDifference(conditioningTable, componentIndex);
    }

    internal int DecodeDifferenceForScanComponent(
        List<JpegArithmeticConditioningTable> tables,
        JpegScanComponent component,
        int componentIndex)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        JpegArithmeticConditioningTable table = DcDecoder.FindOrCreateDefaultDcTable(tables, component.DcEntropyTableIdentifier);
        return DecodeDifference(table, componentIndex);
    }

    internal void ConfigureRestartInterval(int restartInterval)
    {
        if (restartInterval < 0 || restartInterval > 65535)
        {
            throw new System.ArgumentOutOfRangeException("restartInterval");
        }

        RestartInterval = restartInterval;
        ResetRestartState();
    }

    internal void ResetRestartState()
    {
        McuCountSinceRestart = 0;
        ExpectedRestartMarkerIndex = 0;
        RestartMarkerCount = 0;
    }

    internal void RecordMcuDecoded()
    {
        McuCountSinceRestart = McuCountSinceRestart + 1;
    }

    internal bool ShouldExpectRestartMarker()
    {
        if (RestartInterval <= 0)
        {
            return false;
        }

        return McuCountSinceRestart == RestartInterval;
    }

    internal bool AdvanceToMarker()
    {
        while (!StateMachine.HasMarker)
        {
            StateMachine.ReadArithmeticByte();
        }

        return true;
    }

    internal bool IsRestartMarker(JpegScannedMarker marker)
    {
        if (marker == null)
        {
            throw new System.ArgumentNullException("marker");
        }

        return marker.Value >= JpegConstants.MarkerRestart0 && marker.Value <= JpegConstants.MarkerRestart7;
    }

    internal void ValidateRestartMarker(JpegScannedMarker marker)
    {
        if (!IsRestartMarker(marker))
        {
            throw JpegInvalidDataException.Create("Expected a JPEG restart marker.");
        }

        if (RestartInterval <= 0)
        {
            throw JpegInvalidDataException.Create("Restart marker appeared when no restart interval is configured.");
        }

        if (McuCountSinceRestart != RestartInterval)
        {
            throw JpegInvalidDataException.Create("Restart marker appeared before the configured interval.");
        }

        int markerIndex = marker.Value - JpegConstants.MarkerRestart0;
        if (markerIndex != ExpectedRestartMarkerIndex)
        {
            throw JpegInvalidDataException.Create("Restart marker sequence is out of order.");
        }
    }

    internal void AcceptRestartMarker(JpegScannedMarker marker)
    {
        ValidateRestartMarker(marker);
        ResetCodingState();
        McuCountSinceRestart = 0;
        ExpectedRestartMarkerIndex = (ExpectedRestartMarkerIndex + 1) & 7;
        RestartMarkerCount = RestartMarkerCount + 1;
    }

    internal void ConsumeRestartMarkerLookahead()
    {
        JpegScannedMarker marker = StateMachine.GetMarkerLookahead();
        StateMachine.ConsumeMarkerLookahead();
        AcceptRestartMarker(marker);
    }

    internal void ResetCodingState()
    {
        StateMachine.Initialize();
        DcDecoder.ResetDcState();
        InitializeAliases();
    }

    internal byte[] CreateEntropyDataWithTerminatingMarker(byte[] entropyData)
    {
        if (entropyData == null)
        {
            throw new System.ArgumentNullException("entropyData");
        }

        byte[] result = new byte[entropyData.Length + 2];
        int index = 0;
        while (index < entropyData.Length)
        {
            result[index] = entropyData[index];
            index = index + 1;
        }

        result[index] = 0xFF;
        result[index + 1] = 0xD9;
        return result;
    }
}
