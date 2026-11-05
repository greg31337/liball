namespace qrcode;

/// <summary>Calculates exact QR payload capacity and selects fitting symbol parameters.</summary>
public interface IQrCapacityCalculator
{
    /// <summary>Calculates capacity for one exact symbol configuration.</summary>
    /// <param name="request">The family, dimensions, error correction, mode, and payload to evaluate.</param>
    /// <returns>The exact bit and codeword capacity result.</returns>
    QrCapacityResult CalculateCapacity(QrCapacityRequest request);

    /// <summary>Selects the smallest permitted symbol and its final error-correction level.</summary>
    /// <param name="request">The mode and payload requirements to fit.</param>
    /// <param name="options">The permitted families, bounds, and error-correction policy.</param>
    /// <returns>A copied request populated with the selected symbol parameters.</returns>
    QrCapacityRequest SelectSymbolParameters(
        QrCapacityRequest request,
        QrEncodeOptions options);
}
