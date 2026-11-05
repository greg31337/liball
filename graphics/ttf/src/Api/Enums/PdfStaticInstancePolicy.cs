namespace ttf;

/// <summary>Controls conversion of variable input to a static PDF font program.</summary>
public enum PdfStaticInstancePolicy
{
    /// <summary>Materialize the selected instance coordinates into static output.</summary>
    MaterializeSelectedInstance = 0,

    /// <summary>Reject sources that contain live OpenType variation data.</summary>
    RequireStaticSource = 1
}
