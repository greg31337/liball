namespace qrcode;

/// <summary>Provides raw QR symbol and diagnostic module-role rendering.</summary>
public class QrRenderer : IQrRenderer
{
    /// <summary>Initializes a reusable stateless QR renderer.</summary>
    public QrRenderer()
    {
    }

    /// <summary>Renders a symbol using its module values or roles as selected by the options.</summary>
    /// <param name="symbol">The symbol whose matrix is rendered.</param>
    /// <param name="options">The grayscale, scaling, quiet-zone, and rendering options.</param>
    /// <returns>A newly allocated tightly packed Gray8 raster.</returns>
    public QrRaster RenderSymbol(QrSymbol symbol, QrRenderOptions options)
    {
        return RawMatrixRenderer.RenderSymbol(symbol, options);
    }

    /// <summary>Renders the structural roles, damage, and unknown cells of a symbol.</summary>
    /// <param name="symbol">The symbol whose classified matrix and validation metadata are rendered.</param>
    /// <param name="options">The grayscale, highlighting, scaling, and quiet-zone options.</param>
    /// <returns>A newly allocated tightly packed Gray8 diagnostic raster.</returns>
    public QrRaster RenderRoleMap(QrSymbol symbol, QrRenderOptions options)
    {
        return RoleMapRenderer.RenderRoleMap(symbol, options);
    }
}
