namespace jpeg;

internal class JpegRestartIntervalResolver
{
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();

    internal int Resolve(JpegFrameHeader frame, JpegWriteOptions options)
    {
        if (options.RestartIntervalUnit != JpegRestartIntervalUnit.McuBlocks
            && options.RestartIntervalUnit != JpegRestartIntervalUnit.McuRows)
        {
            throw JpegWriteValidationException.Create("The restart-interval unit is not recognized.");
        }

        if (options.RestartInterval < 0)
        {
            throw JpegWriteValidationException.Create("Restart interval cannot be negative.");
        }

        if (options.RestartInterval == 0)
        {
            return 0;
        }

        long resolved = options.RestartInterval;
        if (options.RestartIntervalUnit == JpegRestartIntervalUnit.McuRows)
        {
            resolved = resolved * ResolveMcuUnitsPerRow(frame);
        }

        if (resolved > 65535L)
        {
            throw JpegWriteValidationException.Create("Resolved restart interval must fit in the two-byte DRI field.");
        }

        return (int)resolved;
    }

    internal int ResolveMcuUnitsPerRow(JpegFrameHeader frame)
    {
        if (frame.Process == JpegProcess.LosslessSequential
            || frame.Process == JpegProcess.LosslessSequentialArithmetic
            || frame.Process == JpegProcess.DifferentialLosslessSequential
            || frame.Process == JpegProcess.DifferentialLosslessSequentialArithmetic)
        {
            return frame.Width;
        }

        return LayoutCalculator.Calculate(frame).McusPerRow;
    }
}
