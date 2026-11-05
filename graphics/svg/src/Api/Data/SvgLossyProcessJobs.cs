namespace svg;

/// <summary>Configures basic-shape resolution and generated path limits.</summary>
public class SvgConvertShapesToPathsOptions
{
    /// <summary>The viewport and font context used to resolve lengths.</summary>
    public SvgLengthContext LengthContext;
    /// <summary>The maximum generated path command count.</summary>
    public int MaximumCommands;
    /// <summary>The maximum numeric token character count.</summary>
    public int MaximumTokenCharacters;

    /// <summary>Creates bounded shape conversion defaults.</summary>
    public SvgConvertShapesToPathsOptions()
    {
        LengthContext = new SvgLengthContext();
        MaximumCommands = 1000000;
        MaximumTokenCharacters = 4096;
    }
}

/// <summary>Configures local transform baking into path coordinates.</summary>
public class SvgBakeTransformsOptions
{
    /// <summary>Whether basic shapes with transforms are first converted to paths.</summary>
    public bool ConvertBasicShapes;
    /// <summary>Whether exactly representable transformed arcs remain arcs.</summary>
    public bool PreserveArcs;
    /// <summary>The maximum transformed command count.</summary>
    public int MaximumCommands;
    /// <summary>The maximum transform count.</summary>
    public int MaximumTransforms;
    /// <summary>The maximum numeric token character count.</summary>
    public int MaximumTokenCharacters;
    /// <summary>The shape length-resolution context.</summary>
    public SvgLengthContext LengthContext;

    /// <summary>Creates bounded transform-baking defaults.</summary>
    public SvgBakeTransformsOptions()
    {
        ConvertBasicShapes = true;
        PreserveArcs = true;
        MaximumCommands = 1000000;
        MaximumTransforms = 100000;
        MaximumTokenCharacters = 4096;
        LengthContext = new SvgLengthContext();
    }
}

/// <summary>Configures caller-mediated text outline replacement.</summary>
public class SvgTextToPathProcessOptions
{
    /// <summary>Returns local-user-space path geometry for one root text element, or <c>null</c> to retain it.</summary>
    public System.Func<SvgNode, SvgPathData>? OutlineProvider;
    /// <summary>The maximum accepted generated command count.</summary>
    public int MaximumCommands;
    /// <summary>Whether source text is retained as an ARIA label when no explicit accessible name exists.</summary>
    public bool PreserveAccessibleText;

    /// <summary>Creates bounded accessibility-preserving defaults.</summary>
    public SvgTextToPathProcessOptions()
    {
        OutlineProvider = null;
        MaximumCommands = 2000000;
        PreserveAccessibleText = true;
    }
}

/// <summary>Contains caller-produced local-user-space flattened effect imagery.</summary>
public class SvgFlattenedEffectImage
{
    /// <summary>The complete embedded image data URL.</summary>
    public string DataUrl;
    /// <summary>The local horizontal image origin.</summary>
    public double X;
    /// <summary>The local vertical image origin.</summary>
    public double Y;
    /// <summary>The positive local image width.</summary>
    public double Width;
    /// <summary>The positive local image height.</summary>
    public double Height;

    /// <summary>Creates one flattened local image.</summary>
    /// <param name="dataUrl">The complete data URL.</param>
    /// <param name="x">The local horizontal origin.</param>
    /// <param name="y">The local vertical origin.</param>
    /// <param name="width">The positive local width.</param>
    /// <param name="height">The positive local height.</param>
    public SvgFlattenedEffectImage(string dataUrl, double x, double y, double width, double height)
    {
        DataUrl = dataUrl;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}

/// <summary>Configures caller-mediated effect raster replacement.</summary>
public class SvgFlattenEffectsProcessOptions
{
    /// <summary>Returns flattened local imagery for an effect-bearing element, or <c>null</c> to retain it.</summary>
    public System.Func<SvgNode, SvgFlattenedEffectImage>? ImageProvider;
    /// <summary>The maximum number of document nodes inspected.</summary>
    public int MaximumNodes;

    /// <summary>Creates bounded opt-in effect flattening defaults.</summary>
    public SvgFlattenEffectsProcessOptions()
    {
        ImageProvider = null;
        MaximumNodes = 100000;
    }
}

/// <summary>Contains caller-resolved external image bytes and media type.</summary>
public class SvgEmbeddedImageData
{
    /// <summary>The complete encoded image bytes.</summary>
    public byte[] Bytes;
    /// <summary>The valid image media type.</summary>
    public string MediaType;

    /// <summary>Creates caller-resolved image data.</summary>
    /// <param name="bytes">The encoded image bytes.</param>
    /// <param name="mediaType">The image media type.</param>
    public SvgEmbeddedImageData(byte[] bytes, string mediaType)
    {
        Bytes = bytes;
        MediaType = mediaType;
    }
}

/// <summary>Configures caller-mediated external image embedding.</summary>
public class SvgEmbedImagesProcessOptions
{
    /// <summary>Returns encoded bytes for one external image element, or <c>null</c> to retain it.</summary>
    public System.Func<SvgNode, SvgEmbeddedImageData>? ImageProvider;
    /// <summary>The maximum decoded byte count accepted by data URL encoding.</summary>
    public long MaximumBytes;
    /// <summary>Whether base64 transfer encoding is used.</summary>
    public bool UseBase64;

    /// <summary>Creates bounded opt-in embedding defaults.</summary>
    public SvgEmbedImagesProcessOptions()
    {
        ImageProvider = null;
        MaximumBytes = SvgDataUrl.DefaultMaxDecodedBytes;
        UseBase64 = true;
    }
}
