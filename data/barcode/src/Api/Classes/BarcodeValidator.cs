namespace barcode;

/// <summary>
/// Validates barcode data, logical symbols, render plans, and raw images.
/// </summary>
public class BarcodeValidator : IBarcodeValidator
{
    internal BarcodeDispatcher Dispatcher;

    /// <summary>
    /// Creates a barcode validator backed by the shared operation dispatcher.
    /// </summary>
    public BarcodeValidator()
    {
        Dispatcher = new BarcodeDispatcher();
    }

    /// <summary>
    /// Validates barcode data before encoding.
    /// </summary>
    /// <param name="request">The data target and validation policy.</param>
    /// <returns>The validation report.</returns>
    /// <exception cref="BarcodeValidationException">Data validation is unavailable or cannot be performed.</exception>
    public BarcodeValidationReport ValidateBarcodeData(BarcodeValidationRequest request)
    {
        return Dispatcher.ValidateBarcodeData(request);
    }

    /// <summary>
    /// Validates an encoded logical barcode symbol.
    /// </summary>
    /// <param name="request">The symbol target and validation policy.</param>
    /// <returns>The validation report.</returns>
    /// <exception cref="BarcodeValidationException">Symbol validation is unavailable or cannot be performed.</exception>
    public BarcodeValidationReport ValidateBarcodeSymbol(BarcodeValidationRequest request)
    {
        return Dispatcher.ValidateBarcodeSymbol(request);
    }

    /// <summary>
    /// Validates a barcode render plan.
    /// </summary>
    /// <param name="request">The render target and validation policy.</param>
    /// <returns>The validation report.</returns>
    /// <exception cref="BarcodeValidationException">Render validation is unavailable or cannot be performed.</exception>
    public BarcodeValidationReport ValidateBarcodeRender(BarcodeValidationRequest request)
    {
        return Dispatcher.ValidateBarcodeRender(request);
    }

    /// <summary>
    /// Validates a barcode image and reports diagnostic quality metrics.
    /// </summary>
    /// <param name="request">The image target and validation policy.</param>
    /// <returns>The validation report.</returns>
    /// <exception cref="BarcodeValidationException">Image validation is unavailable or cannot be performed.</exception>
    public BarcodeValidationReport ValidateBarcodeImage(BarcodeValidationRequest request)
    {
        return Dispatcher.ValidateBarcodeImage(request);
    }
}
