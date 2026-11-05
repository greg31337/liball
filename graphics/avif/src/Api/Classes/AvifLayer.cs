namespace avif;

/// <summary>Describes one caller-owned image in a progressive layered image.</summary>
public class AvifLayer : IAvifLayer
{
    AvifImage image;
    AvifResultCode code;
    int layerId;
    int quality;

    /// <summary>Creates a layer whose image will be copied when added to a writer.</summary>
    /// <param name="image">The caller-owned source image.</param>
    public AvifLayer(AvifImage image)
    {
        this.image = image;
        layerId = -1;
        quality = -1;
        code = image == null
            ? AvifResultCode.InvalidArgument
            : image.GetCode();
    }

    /// <summary>Returns the layer validation result.</summary>
    /// <returns>A stable result code.</returns>
    public AvifResultCode GetCode()
    {
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        return image.GetCode();
    }

    /// <summary>Returns the zero-based layer identifier after insertion.</summary>
    /// <returns>The identifier, or minus one before insertion.</returns>
    public int GetLayerId()
    {
        return layerId;
    }

    /// <summary>Returns the caller-owned source image.</summary>
    /// <returns>The image, or null for an invalid layer.</returns>
    public AvifImage GetImage()
    {
        return image;
    }

    /// <summary>Sets this layer's independent encoding quality.</summary>
    /// <param name="quality">Zero through one hundred, or negative one to inherit.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetQuality(int quality)
    {
        if (quality < -1 || quality > 100)
        {
            return AvifResultCode.InvalidArgument;
        }
        this.quality = quality;
        return AvifResultCode.Success;
    }

    /// <summary>Returns this layer's quality override.</summary>
    /// <returns>Zero through one hundred, or negative one to inherit.</returns>
    public int GetQuality()
    {
        return quality;
    }

    internal void SetLayerId(int value)
    {
        layerId = value;
    }
}
