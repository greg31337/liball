namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifLayer"/>.</summary>
public interface IAvifLayer
{
    /// <summary>Returns the layer validation result.</summary>
    /// <returns>A stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns the zero-based layer identifier after insertion.</summary>
    /// <returns>The identifier, or minus one before insertion.</returns>
    int GetLayerId();

    /// <summary>Returns the caller-owned source image.</summary>
    /// <returns>The image, or null for an invalid layer.</returns>
    AvifImage GetImage();

    /// <summary>Sets this layer's independent encoding quality.</summary>
    /// <param name="quality">Zero through one hundred, or negative one to inherit.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetQuality(int quality);

    /// <summary>Returns this layer's quality override.</summary>
    /// <returns>Zero through one hundred, or negative one to inherit.</returns>
    int GetQuality();
}
