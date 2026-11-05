namespace qrcode;

internal class UnusedErrorCorrectionMeasurer
{
    // Each unknown-location RS error consumes two parity units; an erasure
    // consumes one. Use the least remaining fraction across blocks, never a
    // pooled average. Capacities are retained from the actual RS operation,
    // so block splitting and legacy remainder bytes cannot inflate the budget.
    // This reports the decoder's RS budget, not a calibrated verifier grade or
    // any additional profile-specific reservation for error detection.
    internal static UnusedErrorCorrectionMeasurement Measure(QrDecodeResult decoded, QrQualityOptions options)
    {
        if (decoded == null || options == null)
        {
            throw new QrInputException("Decoded results and quality options are required.", "decoded");
        }
        UnusedErrorCorrectionMeasurement result = new UnusedErrorCorrectionMeasurement();
        QrQualityMetric metric = result.UnusedErrorCorrection;
        if (!options.AnalyzeUnusedErrorCorrection)
        {
            metric.Limitation = "Unused error-correction measurement is disabled.";
            return result;
        }
        QrCorrectionResult[] blocks = decoded.ErrorCorrectionBlocks;
        if (blocks == null) { throw new QrInputException("The block diagnostic array is required.", "ErrorCorrectionBlocks"); }
        int allocation = CheckedDimensions.CalculateAllocationBytes(blocks.Length, 16, QrLimits.DefaultMaximumAllocationBytes);
        CheckedDimensions.CalculateSum(allocation, 512, QrLimits.DefaultMaximumAllocationBytes, "MaximumAllocationBytes");
        QrSymbol? symbol = decoded.Symbol;
        if (symbol == null || blocks.Length == 0)
        {
            metric.Limitation = "Verified per-block diagnostics for an individual symbol are unavailable.";
            return result;
        }
        if (symbol.SymbolKind == QrSymbolKind.Micro && symbol.Version == 1)
        {
            metric.Limitation = "Micro M1 provides error detection only; no correcting capacity is reported.";
            return result;
        }
        bool verified = true;
        foreach (QrCorrectionResult block in blocks)
        {
            if (block == null || block.Codewords == null || block.Syndromes == null
                || block.ErrorCorrectionCodewordCount <= 0 || block.ErrorCorrectionCodewordCount >= block.Codewords.Length
                || block.Codewords.Length > 255 || block.Syndromes.Length != block.ErrorCorrectionCodewordCount
                || block.CorrectedErrorCount < 0 || block.CorrectedErasureCount < 0
                || (long)block.CorrectedErrorCount * 2 + block.CorrectedErasureCount > block.ErrorCorrectionCodewordCount)
            {
                throw new QrInputException("The block capacity or correction diagnostics are inconsistent.", "ErrorCorrectionBlocks");
            }
            if (!block.Success || !block.Verified || block.FailureReason != QrFailureReason.None) { verified = false; }
        }
        if (!verified)
        {
            metric.Limitation = "Unused capacity cannot be established because one or more blocks were not successfully verified.";
            return result;
        }
        result.BlockCapacities = new int[blocks.Length];
        result.ConsumedCapacity = new int[blocks.Length];
        result.UnusedFractions = new double[blocks.Length];
        double minimum = 1;
        for (int index = 0; index < blocks.Length; index++)
        {
            QrCorrectionResult block = blocks[index];
            int capacity = block.ErrorCorrectionCodewordCount;
            int consumed = block.CorrectedErrorCount * 2 + block.CorrectedErasureCount;
            double unused = (double)(capacity - consumed) / capacity;
            result.BlockCapacities[index] = capacity;
            result.ConsumedCapacity[index] = consumed;
            result.UnusedFractions[index] = unused;
            if (result.LimitingBlockIndex < 0 || unused < minimum)
            {
                minimum = unused;
                result.LimitingBlockIndex = index;
            }
        }
        metric.IsCalculated = true;
        metric.Value = minimum;
        metric.Limitation = "Based on verified Reed-Solomon usage only; BCH repairs and preliminary module repairs are excluded. No calibrated grade is assigned.";
        return result;
    }
}
