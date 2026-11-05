namespace barcode;

/// <summary>
/// Validates barcode data, logical symbols, render plans, and raw images.
/// </summary>
public interface IBarcodeValidator
{
    /// <summary>
    /// Validates barcode data before encoding.
    /// </summary>
    /// <param name="request">The data target and validation policy.</param>
    /// <returns>The validation report.</returns>
    BarcodeValidationReport ValidateBarcodeData(BarcodeValidationRequest request);

    /// <summary>
    /// Validates an encoded logical barcode symbol.
    /// </summary>
    /// <param name="request">The symbol target and validation policy.</param>
    /// <returns>The validation report.</returns>
    BarcodeValidationReport ValidateBarcodeSymbol(BarcodeValidationRequest request);

    /// <summary>
    /// Validates a barcode render plan.
    /// </summary>
    /// <param name="request">The render target and validation policy.</param>
    /// <returns>The validation report.</returns>
    BarcodeValidationReport ValidateBarcodeRender(BarcodeValidationRequest request);

    /// <summary>
    /// Validates a barcode image and reports diagnostic quality metrics.
    /// </summary>
    /// <param name="request">The image target and validation policy.</param>
    /// <returns>The validation report.</returns>
    BarcodeValidationReport ValidateBarcodeImage(BarcodeValidationRequest request);
}
