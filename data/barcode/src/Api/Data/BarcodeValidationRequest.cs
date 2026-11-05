namespace barcode;

/// <summary>
/// Holds one data, symbol, render-plan, or image target and its validation policy.
/// </summary>
public class BarcodeValidationRequest
{
    /// <summary>
    /// The format or application profile governing validation.
    /// </summary>
    public BarcodeFormat Format;

    /// <summary>
    /// The encode request to validate as a data target.
    /// </summary>
    public BarcodeEncodeRequest? Data;

    /// <summary>
    /// The encoded logical symbol to validate.
    /// </summary>
    public BarcodeSymbol? Symbol;

    /// <summary>
    /// The render request to validate as a render-plan target.
    /// </summary>
    public BarcodeRenderRequest? Render;

    /// <summary>
    /// The raw image to validate as an image-quality target.
    /// </summary>
    public BarcodeImage? Image;

    /// <summary>
    /// The strict or tolerant validation mode.
    /// </summary>
    public BarcodeValidationMode Mode;

    /// <summary>
    /// Indicates whether applicable GS1 application-profile rules are validated.
    /// </summary>
    public bool ValidateGs1Profile;
}
