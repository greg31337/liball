namespace pptx;

/// <summary>
/// Represents DrawingML scene and shape three-dimensional settings.
/// </summary>
public class PptxThreeD
{
    /// <summary>
    /// Indicates whether scene settings are enabled.
    /// </summary>
    public bool HasScene;
    /// <summary>
    /// Stores the camera preset.
    /// </summary>
    public string CameraPreset;
    /// <summary>
    /// Stores the camera field of view in DrawingML angle units.
    /// </summary>
    public int CameraFieldOfView;
    /// <summary>
    /// Stores the camera zoom in thousandths of one percent.
    /// </summary>
    public int CameraZoom;
    /// <summary>
    /// Stores camera latitude rotation in DrawingML angle units.
    /// </summary>
    public int CameraLatitude;
    /// <summary>
    /// Stores camera longitude rotation in DrawingML angle units.
    /// </summary>
    public int CameraLongitude;
    /// <summary>
    /// Stores camera revolution rotation in DrawingML angle units.
    /// </summary>
    public int CameraRevolution;
    /// <summary>
    /// Stores the light rig preset.
    /// </summary>
    public string LightRig;
    /// <summary>
    /// Stores the light rig direction.
    /// </summary>
    public string LightDirection;
    /// <summary>
    /// Stores light latitude rotation in DrawingML angle units.
    /// </summary>
    public int LightLatitude;
    /// <summary>
    /// Stores light longitude rotation in DrawingML angle units.
    /// </summary>
    public int LightLongitude;
    /// <summary>
    /// Stores light revolution rotation in DrawingML angle units.
    /// </summary>
    public int LightRevolution;
    /// <summary>
    /// Indicates whether shape three-dimensional settings are enabled.
    /// </summary>
    public bool HasShape;
    /// <summary>
    /// Stores the shape depth position in English Metric Units (EMUs).
    /// </summary>
    public long Z;
    /// <summary>
    /// Stores extrusion height in English Metric Units (EMUs).
    /// </summary>
    public long ExtrusionHeight;
    /// <summary>
    /// Stores contour width in English Metric Units (EMUs).
    /// </summary>
    public long ContourWidth;
    /// <summary>
    /// Stores the preset material.
    /// </summary>
    public string Material;
    /// <summary>
    /// Stores top bevel settings.
    /// </summary>
    public PptxBevel TopBevel;
    /// <summary>
    /// Stores bottom bevel settings.
    /// </summary>
    public PptxBevel BottomBevel;
    /// <summary>
    /// Stores the extrusion color.
    /// </summary>
    public PptxColor ExtrusionColor;
    /// <summary>
    /// Stores the contour color.
    /// </summary>
    public PptxColor ContourColor;

    /// <summary>
    /// Initializes disabled three-dimensional settings.
    /// </summary>
    public PptxThreeD()
    {
        HasScene = false;
        CameraPreset = "";
        CameraFieldOfView = 0;
        CameraZoom = 100000;
        CameraLatitude = 0;
        CameraLongitude = 0;
        CameraRevolution = 0;
        LightRig = "";
        LightDirection = "";
        LightLatitude = 0;
        LightLongitude = 0;
        LightRevolution = 0;
        HasShape = false;
        Z = 0L;
        ExtrusionHeight = 0L;
        ContourWidth = 0L;
        Material = "";
        TopBevel = new PptxBevel();
        BottomBevel = new PptxBevel();
        ExtrusionColor = new PptxColor();
        ContourColor = new PptxColor();
    }
}
