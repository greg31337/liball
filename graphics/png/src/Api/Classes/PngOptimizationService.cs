namespace png;

/// <summary>Default service for PNG encoding trials and optimization reports.</summary>
public class PngOptimizationService : IPngOptimizer
{
    /// <summary>Creates an independent PNG optimization service.</summary>
    public PngOptimizationService()
    {
    }

    /// <summary>Runs lossless reductions, metadata selection, filter trials, compression trials, and early bailout as one workflow.</summary><param name="data">Source PNG datastream.</param><param name="options">Optimization options.</param><returns>The complete report and best encoded result.</returns>
    public PngOptimizerReport OptimizeBytes(byte[] data, PngOptimizerOptions options) { return PngOptimizationWorkflow.OptimizeBytes(data, options); }
    /// <summary>Runs default compression trials.</summary><param name="image">Image.</param><returns>Results.</returns>
    public PngCompressionTrialResult RunCompressionTrials(PngImage image) { return PngCompressionLevelTrialRunner.Run(image); }
    /// <summary>Runs compression trials with options.</summary><param name="image">Image.</param><param name="options">Options.</param><returns>Results.</returns>
    public PngCompressionTrialResult RunCompressionTrialsWithOptions(PngImage image, PngOptimizerOptions options) { return PngCompressionLevelTrialRunner.Run(image, options); }
    /// <summary>Runs compression trials with an encoder.</summary><param name="image">Image.</param><param name="options">Options.</param><param name="encoder">Encoder.</param><returns>Results.</returns>
    public PngCompressionTrialResult RunCompressionTrialsWithEncoder(PngImage image, PngOptimizerOptions options, IPngEncoder encoder) { return PngCompressionLevelTrialRunner.Run(image, options, encoder); }
    /// <summary>Runs default filter trials.</summary><param name="image">Image.</param><returns>Results.</returns>
    public PngFilterStrategyTrialResult RunFilterTrials(PngImage image) { return PngFilterStrategyTrialRunner.Run(image); }
    /// <summary>Runs filter trials with options.</summary><param name="image">Image.</param><param name="options">Options.</param><returns>Results.</returns>
    public PngFilterStrategyTrialResult RunFilterTrialsWithOptions(PngImage image, PngOptimizerOptions options) { return PngFilterStrategyTrialRunner.Run(image, options); }
    /// <summary>Runs filter trials with an encoder.</summary><param name="image">Image.</param><param name="options">Options.</param><param name="encoder">Encoder.</param><returns>Results.</returns>
    public PngFilterStrategyTrialResult RunFilterTrialsWithEncoder(PngImage image, PngOptimizerOptions options, IPngEncoder encoder) { return PngFilterStrategyTrialRunner.Run(image, options, encoder); }
    /// <summary>Creates a report from a byte count.</summary><param name="originalByteCount">Size.</param><param name="compressionResult">Compression.</param><param name="filterStrategyResult">Filters.</param><returns>Report.</returns>
    public PngOptimizerReport CreateReportFromByteCount(long originalByteCount, PngCompressionTrialResult compressionResult, PngFilterStrategyTrialResult filterStrategyResult) { return PngOptimizerReportBuilder.Create(originalByteCount, compressionResult, filterStrategyResult); }
    /// <summary>Creates a report from original bytes.</summary><param name="originalBytes">Bytes.</param><param name="compressionResult">Compression.</param><param name="filterStrategyResult">Filters.</param><returns>Report.</returns>
    public PngOptimizerReport CreateReportFromBytes(byte[] originalBytes, PngCompressionTrialResult compressionResult, PngFilterStrategyTrialResult filterStrategyResult) { return PngOptimizerReportBuilder.Create(originalBytes, compressionResult, filterStrategyResult); }
}
