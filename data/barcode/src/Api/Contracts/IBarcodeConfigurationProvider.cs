namespace barcode;

/// <summary>
/// Provides the common configuration selections used by barcode operations.
/// </summary>
public interface IBarcodeConfigurationProvider
{
    /// <summary>
    /// Gets the current barcode configuration.
    /// </summary>
    /// <returns>The current barcode configuration.</returns>
    BarcodeConfiguration GetBarcodeConfiguration();
}
