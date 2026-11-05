namespace qrcode;

/// <summary>Provides stateless QR capacity calculation and parameter selection.</summary>
public class QrCapacityCalculator : IQrCapacityCalculator
{
    /// <summary>Initializes a reusable stateless capacity calculator.</summary>
    public QrCapacityCalculator()
    {
    }

    /// <summary>Calculates capacity for one exact symbol configuration.</summary>
    /// <param name="request">The family, dimensions, error correction, mode, and payload to evaluate.</param>
    /// <returns>The exact bit and codeword capacity result.</returns>
    public QrCapacityResult CalculateCapacity(QrCapacityRequest request)
    {
        if (request == null)
        {
            throw new QrInputException("The capacity request cannot be null.", "request");
        }

        if (request.SymbolKind == QrSymbolKind.Model2)
        {
            return Model2CapacityCalculator.CalculateCapacity(request);
        }

        if (request.SymbolKind == QrSymbolKind.Micro)
        {
            return MicroCapacityCalculator.CalculateCapacity(request);
        }

        if (request.SymbolKind == QrSymbolKind.rMQR)
        {
            return RectangularMicroCapacityCalculator.CalculateCapacity(request);
        }

        if (request.SymbolKind == QrSymbolKind.LegacyModel1)
        {
            throw new QrUnsupportedFeatureException(
                "Legacy Model 1 encoding capacity is not supported.",
                QrFailureReason.InvalidInput,
                "SymbolKind",
                (int)request.SymbolKind);
        }

        throw new QrInputException("The symbol kind is not defined.", "SymbolKind");
    }

    /// <summary>Selects the smallest permitted symbol and its final error-correction level.</summary>
    /// <param name="request">The mode and payload requirements to fit.</param>
    /// <param name="options">The permitted families, bounds, and error-correction policy.</param>
    /// <returns>A copied request populated with the selected symbol parameters.</returns>
    public QrCapacityRequest SelectSymbolParameters(
        QrCapacityRequest request,
        QrEncodeOptions options)
    {
        return ErrorCorrectionSelector.SelectParameters(request, options);
    }
}
