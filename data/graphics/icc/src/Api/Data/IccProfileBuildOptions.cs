namespace icc;

/// <summary>Controls deterministic construction of a basic ICC profile.</summary>
public class IccProfileBuildOptions
{
    /// <summary>Gets or sets the target profile version.</summary>
    public IccVersion Version;
    /// <summary>Gets or sets the target profile class.</summary>
    public IccProfileClass ProfileClass;
    /// <summary>Gets or sets the default rendering intent.</summary>
    public IccRenderingIntent RenderingIntent;
    /// <summary>Gets or sets the data colour-space signature.</summary>
    public uint DataColorSpaceSignature;
    /// <summary>Gets or sets the Profile Connection Space or DeviceLink output-space signature.</summary>
    public uint ProfileConnectionSpaceSignature;
    /// <summary>Gets or sets localized profile descriptions.</summary>
    public IccLocalizedString[] Descriptions;
    /// <summary>Gets or sets copyright text.</summary>
    public IccTextData Copyright;
    /// <summary>Gets or sets the media white point encoded by the profile.</summary>
    public IccXyzColor MediaWhitePoint;
    /// <summary>Gets or sets an optional three-by-three chromatic-adaptation matrix.</summary>
    public IccMatrix? ChromaticAdaptationMatrix;
    /// <summary>Gets or sets the deterministic UTC profile creation time.</summary>
    public IccDateTime CreationDateTime;
    /// <summary>Gets or sets an optional RGB definition.</summary>
    public IccRgbProfileDefinition? RgbDefinition;
    /// <summary>Gets or sets an optional gray definition.</summary>
    public IccGrayProfileDefinition? GrayDefinition;
    /// <summary>Gets or sets an optional abstract-profile transform definition.</summary>
    public IccAbstractProfileDefinition? AbstractDefinition;
    /// <summary>Gets or sets optional Adaptive Gain Curve metadata for an RGB Input or Display profile.</summary>
    public IccAdaptiveGainCurve? AdaptiveGainCurve;
    /// <summary>Gets or sets an optional named-colour profile definition.</summary>
    public IccNamedColorProfileDefinition? NamedColorDefinition;
    /// <summary>Gets or sets an optional Output profile definition.</summary>
    public IccOutputProfileDefinition? OutputDefinition;
    /// <summary>Gets or sets an optional ICC.2 colour-encoding profile definition.</summary>
    public IccColorEncodingProfileDefinition? ColorEncodingDefinition;

    /// <summary>Initializes version-4 display-profile construction options.</summary>
    public IccProfileBuildOptions()
    {
        Version = new IccVersion();
        Version.Kind = IccVersionKind.Version4;
        Version.Major = 4;
        ProfileClass = IccProfileClass.Display;
        RenderingIntent = IccRenderingIntent.Perceptual;
        DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        Descriptions = new IccLocalizedString[0];
        Copyright = new IccTextData();
        MediaWhitePoint = new IccXyzColor();
        MediaWhitePoint.X = IccStandardColorimetry.D50X;
        MediaWhitePoint.Y = IccStandardColorimetry.D50Y;
        MediaWhitePoint.Z = IccStandardColorimetry.D50Z;
        ChromaticAdaptationMatrix = null;
        CreationDateTime = new IccDateTime();
        CreationDateTime.Year = 2000;
        CreationDateTime.Month = 1;
        CreationDateTime.Day = 1;
        RgbDefinition = null;
        GrayDefinition = null;
        AbstractDefinition = null;
        AdaptiveGainCurve = null;
        NamedColorDefinition = null;
        OutputDefinition = null;
        ColorEncodingDefinition = null;
    }
}
