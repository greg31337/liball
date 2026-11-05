namespace qrcode;

/// <summary>Renders QR symbols and diagnostic module-role maps as Gray8 rasters.</summary>
public interface IQrRenderer
{
    /// <summary>Renders a symbol using its module values or roles as selected by the options.</summary>
    /// <param name="symbol">The symbol whose matrix is rendered.</param>
    /// <param name="options">The grayscale, scaling, quiet-zone, and rendering options.</param>
    /// <returns>A newly allocated tightly packed Gray8 raster.</returns>
    QrRaster RenderSymbol(QrSymbol symbol, QrRenderOptions options);

    /// <summary>Renders the structural roles, damage, and unknown cells of a symbol.</summary>
    /// <param name="symbol">The symbol whose classified matrix and validation metadata are rendered.</param>
    /// <param name="options">The grayscale, highlighting, scaling, and quiet-zone options.</param>
    /// <returns>A newly allocated tightly packed Gray8 diagnostic raster.</returns>
    QrRaster RenderRoleMap(QrSymbol symbol, QrRenderOptions options);
}
