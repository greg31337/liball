using System.Collections.Generic;

namespace jpeg;

internal class JpegLosslessArithmeticEntropyEncoder
{
    internal JpegArithmeticCoefficientEncoder CoefficientEncoder = new JpegArithmeticCoefficientEncoder();
    internal JpegArithmeticEncoderStateMachine StateMachine = new JpegArithmeticEncoderStateMachine();
    internal int RestartInterval;
    internal int McuCountSinceRestart;
    internal int NextRestartMarkerIndex;
    internal int RestartMarkerCount;

    internal JpegLosslessArithmeticEntropyEncoder()
    {
        CoefficientEncoder = new JpegArithmeticCoefficientEncoder();
        InitializeAliases();
        ResetRestartState();
    }

    internal static JpegLosslessArithmeticEntropyEncoder FromStateMachine(JpegArithmeticEncoderStateMachine stateMachine)
    {
        if (stateMachine == null)
        {
            throw new System.ArgumentNullException("stateMachine");
        }

        JpegLosslessArithmeticEntropyEncoder encoder = new JpegLosslessArithmeticEntropyEncoder();
        encoder.CoefficientEncoder = JpegArithmeticCoefficientEncoder.FromStateMachine(stateMachine);
        encoder.InitializeAliases();
        encoder.ResetRestartState();
        return encoder;
    }

    internal void InitializeAliases()
    {
        StateMachine = CoefficientEncoder.StateMachine;
    }

    internal void EncodeDifference(JpegArithmeticConditioningTable conditioningTable, int componentIndex, int difference)
    {
        CoefficientEncoder.ValidateComponentIndex(componentIndex);
        CoefficientEncoder.ValidateDcConditioningTable(conditioningTable);

        int[] statistics = CoefficientEncoder.DcStatistics[conditioningTable.Identifier];
        int context = CoefficientEncoder.DcContexts[componentIndex];
        if (difference == 0)
        {
            StateMachine.EncodeBit(statistics, context, 0);
            CoefficientEncoder.DcContexts[componentIndex] = 0;
            return;
        }

        StateMachine.EncodeBit(statistics, context, 1);
        int sign = 0;
        long wideMagnitude = difference;
        if (difference > 0)
        {
            StateMachine.EncodeBit(statistics, context + 1, 0);
            context = context + 2;
        }
        else
        {
            sign = 1;
            wideMagnitude = 0L - wideMagnitude;
            StateMachine.EncodeBit(statistics, context + 1, 1);
            context = context + 3;
        }

        if (wideMagnitude > 65535L)
        {
            throw JpegWriteValidationException.Create("Lossless arithmetic difference magnitude must fit in 16 bits.");
        }

        int magnitude = (int)wideMagnitude;
        ValidateDifferenceMagnitude(magnitude);
        int encodedMagnitude = CoefficientEncoder.EncodeMagnitudeCategory(statistics, context, 20, magnitude);
        CoefficientEncoder.UpdateDcContext(conditioningTable, componentIndex, encodedMagnitude, sign);
        CoefficientEncoder.EncodeMagnitudeBits(statistics, context + 14, encodedMagnitude, magnitude - 1);
    }

    internal void EncodeDifferenceForScanComponent(
        List<JpegArithmeticConditioningTable> tables,
        JpegScanComponent component,
        int componentIndex,
        int difference)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        JpegArithmeticConditioningTable table = FindOrCreateDefaultDcTable(tables, component.DcEntropyTableIdentifier);
        EncodeDifference(table, componentIndex, difference);
    }

    internal JpegArithmeticConditioningTable CreateDefaultDcConditioningTable(int identifier)
    {
        return new JpegArithmeticDcCoefficientDecoder(new byte[0]).CreateDefaultDcConditioningTable(identifier);
    }

    internal JpegArithmeticConditioningTable FindOrCreateDefaultDcTable(List<JpegArithmeticConditioningTable>? tables, int identifier)
    {
        return new JpegArithmeticDcCoefficientDecoder(new byte[0]).FindOrCreateDefaultDcTable(tables, identifier);
    }

    internal void Finish()
    {
        StateMachine.Finish();
    }

    internal byte[] ToArray()
    {
        return StateMachine.ToArray();
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
        NextRestartMarkerIndex = 0;
        RestartMarkerCount = 0;
    }

    internal void RecordMcuEncoded()
    {
        McuCountSinceRestart = McuCountSinceRestart + 1;
    }

    internal bool ShouldWriteRestartMarker()
    {
        if (RestartInterval <= 0)
        {
            return false;
        }

        return McuCountSinceRestart == RestartInterval;
    }

    internal void WriteRestartMarker()
    {
        if (!ShouldWriteRestartMarker())
        {
            throw JpegWriteValidationException.Create("Restart marker cannot be written before the configured interval.");
        }

        StateMachine.Finish();
        StateMachine.Writer.WriteUInt16(JpegConstants.MarkerRestart0 + NextRestartMarkerIndex);
        ResetCodingState();
        McuCountSinceRestart = 0;
        NextRestartMarkerIndex = (NextRestartMarkerIndex + 1) & 7;
        RestartMarkerCount = RestartMarkerCount + 1;
    }

    internal void ResetCodingState()
    {
        StateMachine.Initialize();
        CoefficientEncoder.Initialize();
        InitializeAliases();
    }

    internal void ValidateDifferenceMagnitude(int magnitude)
    {
        if (magnitude < 1 || magnitude > 65535)
        {
            throw JpegWriteValidationException.Create("Lossless arithmetic difference magnitude must fit in 16 bits.");
        }
    }
}
