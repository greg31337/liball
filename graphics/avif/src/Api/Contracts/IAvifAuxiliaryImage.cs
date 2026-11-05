namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifAuxiliaryImage"/>.</summary>
public interface IAvifAuxiliaryImage
{
    /// <summary>Returns the association validation result.</summary>
    /// <returns>A stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns the auxiliary item identifier.</summary>
    /// <returns>The auxiliary item identifier.</returns>
    uint GetItemId();

    /// <summary>Returns the associated master item identifier.</summary>
    /// <returns>The master item identifier.</returns>
    uint GetMasterItemId();

    /// <summary>Returns the interpreted auxiliary kind.</summary>
    /// <returns>The alpha, depth, or other kind.</returns>
    AvifAuxiliaryKind GetKind();

    /// <summary>Returns the exact auxiliary type identifier.</summary>
    /// <returns>The auxiliary type, which may be empty for an untyped association.</returns>
    string? GetTypeName();

    /// <summary>Returns the auxiliary image width.</summary>
    /// <returns>The width from its spatial-extents property.</returns>
    uint GetWidth();

    /// <summary>Returns the auxiliary image height.</summary>
    /// <returns>The height from its spatial-extents property.</returns>
    uint GetHeight();

    /// <summary>Returns the auxiliary image bit depth.</summary>
    /// <returns>The configured depth, or minus one when unavailable.</returns>
    int GetBitDepth();

    /// <summary>Returns the master image width.</summary>
    /// <returns>The master width.</returns>
    uint GetMasterWidth();

    /// <summary>Returns the master image height.</summary>
    /// <returns>The master height.</returns>
    uint GetMasterHeight();

    /// <summary>Returns the master image bit depth.</summary>
    /// <returns>The configured master depth, or minus one when unavailable.</returns>
    int GetMasterBitDepth();

    /// <summary>Reports whether color samples are premultiplied by this alpha item.</summary>
    /// <returns><c>true</c> when a matching <c>prem</c> reference is present.</returns>
    bool IsPremultiplied();

    /// <summary>Reports whether decoded auxiliary samples require resizing to the master dimensions.</summary>
    /// <returns><c>true</c> when the spatial dimensions differ.</returns>
    bool RequiresResize();

    /// <summary>Sets this auxiliary image's independent encoding quality.</summary>
    /// <param name="quality">Zero through one hundred, or negative one to inherit.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetQuality(int quality);

    /// <summary>Returns this auxiliary image's quality override.</summary>
    /// <returns>Zero through one hundred, or negative one to inherit.</returns>
    int GetQuality();
}
