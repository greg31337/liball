using System.Collections.Generic;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents color data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxColor
{
    /// <summary>
    /// Identifies the empty value accepted by the API.
    /// </summary>
    public const string Empty = "";
    /// <summary>
    /// Identifies the rgb value accepted by the API.
    /// </summary>
    public const string Rgb = "rgb";
    /// <summary>
    /// Identifies the scheme value accepted by the API.
    /// </summary>
    public const string Scheme = "scheme";
    /// <summary>
    /// Identifies a DrawingML preset color.
    /// </summary>
    public const string Preset = "preset";
    /// <summary>
    /// Identifies a system color with an optional last-known RGB fallback.
    /// </summary>
    public const string System = "system";
    /// <summary>
    /// Identifies a hue, saturation, and luminance color.
    /// </summary>
    public const string Hsl = "hsl";

    /// <summary>
    /// Stores the type associated with this object.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the value associated with this object.
    /// </summary>
    public string Value;
    /// <summary>
    /// Stores the last-known RGB fallback for a system color.
    /// </summary>
    public string FallbackValue;
    /// <summary>
    /// Stores the HSL hue in DrawingML angle units, where 60,000 units equal one degree.
    /// </summary>
    public int HslHueAngleUnits;
    /// <summary>
    /// Stores the HSL saturation from zero through 100,000.
    /// </summary>
    public int HslSaturation;
    /// <summary>
    /// Stores the HSL luminance from zero through 100,000.
    /// </summary>
    public int HslLuminance;
    /// <summary>
    /// Stores the alpha value associated with this object.
    /// </summary>
    public int Alpha;
    /// <summary>
    /// Stores the tint value associated with this object.
    /// </summary>
    public int Tint;
    /// <summary>
    /// Stores the shade value associated with this object.
    /// </summary>
    public int Shade;
    /// <summary>
    /// Stores an absolute luminance transform from zero through 100,000, or <c>-1</c> when absent.
    /// </summary>
    public int Luminance;
    /// <summary>
    /// Stores the luminance modulation percentage.
    /// </summary>
    public int LuminanceModulation;
    /// <summary>
    /// Stores the signed luminance offset percentage.
    /// </summary>
    public int LuminanceOffset;
    /// <summary>
    /// Stores an absolute saturation transform from zero through 100,000, or <c>-1</c> when absent.
    /// </summary>
    public int Saturation;
    /// <summary>
    /// Stores the saturation modulation percentage.
    /// </summary>
    public int SaturationModulation;
    /// <summary>
    /// Stores the signed saturation offset percentage.
    /// </summary>
    public int SaturationOffset;
    /// <summary>
    /// Stores an absolute hue transform in DrawingML angle units, or <c>-1</c> when absent.
    /// </summary>
    public int HueAngleUnits;
    /// <summary>
    /// Stores the hue modulation percentage.
    /// </summary>
    public int HueModulation;
    /// <summary>
    /// Stores the signed hue offset in DrawingML angle units.
    /// </summary>
    public int HueOffsetAngleUnits;
    /// <summary>
    /// Indicates whether the complementary-color transform is enabled.
    /// </summary>
    public bool Complement;
    /// <summary>
    /// Indicates whether the inverse-color transform is enabled.
    /// </summary>
    public bool Inverse;
    /// <summary>
    /// Indicates whether the grayscale transform is enabled.
    /// </summary>
    public bool Grayscale;
    /// <summary>
    /// Contains unsupported transform XML preserved during color round trips.
    /// </summary>
    public List<string> UnsupportedTransformXml;

    /// <summary>
    /// Initializes a new color with safe defaults or the supplied values.
    /// </summary>
    public PptxColor()
    {
        Type = Empty;
        Value = "";
        FallbackValue = "";
        HslHueAngleUnits = 0;
        HslSaturation = 0;
        HslLuminance = 0;
        Alpha = 100000;
        Tint = -1;
        Shade = -1;
        Luminance = -1;
        LuminanceModulation = 100000;
        LuminanceOffset = 0;
        Saturation = -1;
        SaturationModulation = 100000;
        SaturationOffset = 0;
        HueAngleUnits = -1;
        HueModulation = 100000;
        HueOffsetAngleUnits = 0;
        Complement = false;
        Inverse = false;
        Grayscale = false;
        UnsupportedTransformXml = new List<string>();
    }
    internal static PptxColor CreateRgb(string value)
    {
        PptxColor color = new PptxColor();
        color.Type = Rgb;
        color.Value = NormalizeRgb(value);
        return color;
    }
    internal static PptxColor CreateScheme(string value)
    {
        PptxColor color = new PptxColor();
        color.Type = Scheme;
        color.Value = PptxDiagnostic.CleanText(value);
        return color;
    }
    internal static PptxColor CreatePreset(string value)
    {
        PptxColor color = new PptxColor();
        color.Type = Preset;
        color.Value = PptxDiagnostic.CleanText(value);
        return color;
    }
    internal static PptxColor CreateSystem(string value, string fallbackRgb)
    {
        PptxColor color = new PptxColor();
        color.Type = System;
        color.Value = PptxDiagnostic.CleanText(value);
        color.FallbackValue = NormalizeOptionalRgb(fallbackRgb);
        return color;
    }
    internal static PptxColor CreateHsl(double hueDegrees, int saturation, int luminance)
    {
        if (saturation < 0 || saturation > 100000 || luminance < 0 || luminance > 100000)
        {
            throw new System.ArgumentOutOfRangeException("saturation", "HSL percentages must be between zero and 100,000.");
        }

        PptxColor color = new PptxColor();
        color.Type = Hsl;
        color.HslHueAngleUnits = PptxUnitsCore.DegreesToAngleUnits(hueDegrees);
        color.HslSaturation = saturation;
        color.HslLuminance = luminance;
        return color;
    }

    internal static PptxColor FromParent(XmlElement parent)
    {
        XmlElement? rgb = PptxXml.FindFirstChild(parent, PptxNamespaces.Drawing, "srgbClr");
        if (rgb != null)
        {
            return FromElement(rgb);
        }

        XmlElement? scheme = PptxXml.FindFirstChild(parent, PptxNamespaces.Drawing, "schemeClr");
        if (scheme != null)
        {
            return FromElement(scheme);
        }

        XmlElement? preset = PptxXml.FindFirstChild(parent, PptxNamespaces.Drawing, "prstClr");
        if (preset != null)
        {
            return FromElement(preset);
        }

        XmlElement? system = PptxXml.FindFirstChild(parent, PptxNamespaces.Drawing, "sysClr");
        if (system != null)
        {
            return FromElement(system);
        }

        XmlElement? hsl = PptxXml.FindFirstChild(parent, PptxNamespaces.Drawing, "hslClr");
        if (hsl != null)
        {
            return FromElement(hsl);
        }

        return new PptxColor();
    }

    internal static PptxColor FromElement(XmlElement element)
    {
        PptxColor color = new PptxColor();
        if (element.LocalName == "srgbClr")
        {
            color.Type = Rgb;
            color.Value = NormalizeRgb(PptxXml.GetAttributeValue(element, "", "val"));
        }

        if (element.LocalName == "schemeClr")
        {
            color.Type = Scheme;
            color.Value = PptxXml.GetAttributeValue(element, "", "val");
        }

        if (element.LocalName == "prstClr")
        {
            color.Type = Preset;
            color.Value = PptxXml.GetAttributeValue(element, "", "val");
        }

        if (element.LocalName == "sysClr")
        {
            color.Type = System;
            color.Value = PptxXml.GetAttributeValue(element, "", "val");
            color.FallbackValue = NormalizeOptionalRgb(PptxXml.GetAttributeValue(element, "", "lastClr"));
        }

        if (element.LocalName == "hslClr")
        {
            color.Type = Hsl;
            color.HslHueAngleUnits = PptxXml.GetIntAttribute(element, "", "hue", 0);
            color.HslSaturation = PptxXml.GetIntAttribute(element, "", "sat", 0);
            color.HslLuminance = PptxXml.GetIntAttribute(element, "", "lum", 0);
        }

        color.LoadTransforms(element);

        return color;
    }

    internal XmlElement ToElement(XmlDocument document)
    {
        XmlElement element = document.CreateElement("a", "srgbClr", PptxNamespaces.Drawing);
        if (Type == Scheme)
        {
            element = document.CreateElement("a", "schemeClr", PptxNamespaces.Drawing);
            PptxXml.SetAttributeValue(element, "", "", "val", PptxDiagnostic.CleanText(Value));
        }
        else if (Type == Preset)
        {
            element = document.CreateElement("a", "prstClr", PptxNamespaces.Drawing);
            PptxXml.SetAttributeValue(element, "", "", "val", PptxDiagnostic.CleanText(Value));
        }
        else if (Type == System)
        {
            element = document.CreateElement("a", "sysClr", PptxNamespaces.Drawing);
            PptxXml.SetAttributeValue(element, "", "", "val", PptxDiagnostic.CleanText(Value));
            if (FallbackValue.Length > 0)
            {
                PptxXml.SetAttributeValue(element, "", "", "lastClr", NormalizeRgb(FallbackValue));
            }
        }
        else if (Type == Hsl)
        {
            element = document.CreateElement("a", "hslClr", PptxNamespaces.Drawing);
            PptxXml.SetAttributeValue(element, "", "", "hue", HslHueAngleUnits.ToString());
            PptxXml.SetAttributeValue(element, "", "", "sat", ClampPercentage(HslSaturation).ToString());
            PptxXml.SetAttributeValue(element, "", "", "lum", ClampPercentage(HslLuminance).ToString());
        }
        else
        {
            PptxXml.SetAttributeValue(element, "", "", "val", NormalizeRgb(Value));
        }

        AppendTransforms(document, element);

        return element;
    }

    internal static string NormalizeRgb(string value)
    {
        string clean = PptxDiagnostic.CleanText(value);
        if (clean.StartsWith("#"))
        {
            clean = clean.Substring(1);
        }

        if (clean.Length == 0)
        {
            return "000000";
        }

        return clean.ToUpperInvariant();
    }

    internal static string NormalizeOptionalRgb(string value)
    {
        string clean = PptxDiagnostic.CleanText(value);
        if (clean.Length == 0)
        {
            return "";
        }

        return NormalizeRgb(clean);
    }

    internal static bool IsColorElement(XmlElement element)
    {
        if (element.NamespaceURI != PptxNamespaces.Drawing)
        {
            return false;
        }

        return element.LocalName == "srgbClr" || element.LocalName == "schemeClr" || element.LocalName == "prstClr" || element.LocalName == "sysClr" || element.LocalName == "hslClr";
    }

    internal void LoadTransforms(XmlElement element)
    {
        foreach (XmlNode node in element.ChildNodes)
        {
            XmlElement? transform = node as XmlElement;
            if (transform == null || transform.NamespaceURI != PptxNamespaces.Drawing)
            {
                continue;
            }

            string name = transform.LocalName;
            if (name == "alpha")
            {
                Alpha = PptxXml.GetIntAttribute(transform, "", "val", 100000);
            }
            else if (name == "tint")
            {
                Tint = PptxXml.GetIntAttribute(transform, "", "val", -1);
            }
            else if (name == "shade")
            {
                Shade = PptxXml.GetIntAttribute(transform, "", "val", -1);
            }
            else if (name == "lum")
            {
                Luminance = PptxXml.GetIntAttribute(transform, "", "val", -1);
            }
            else if (name == "lumMod")
            {
                LuminanceModulation = PptxXml.GetIntAttribute(transform, "", "val", 100000);
            }
            else if (name == "lumOff")
            {
                LuminanceOffset = PptxXml.GetIntAttribute(transform, "", "val", 0);
            }
            else if (name == "sat")
            {
                Saturation = PptxXml.GetIntAttribute(transform, "", "val", -1);
            }
            else if (name == "satMod")
            {
                SaturationModulation = PptxXml.GetIntAttribute(transform, "", "val", 100000);
            }
            else if (name == "satOff")
            {
                SaturationOffset = PptxXml.GetIntAttribute(transform, "", "val", 0);
            }
            else if (name == "hue")
            {
                HueAngleUnits = PptxXml.GetIntAttribute(transform, "", "val", -1);
            }
            else if (name == "hueMod")
            {
                HueModulation = PptxXml.GetIntAttribute(transform, "", "val", 100000);
            }
            else if (name == "hueOff")
            {
                HueOffsetAngleUnits = PptxXml.GetIntAttribute(transform, "", "val", 0);
            }
            else if (name == "comp")
            {
                Complement = true;
            }
            else if (name == "inv")
            {
                Inverse = true;
            }
            else if (name == "gray")
            {
                Grayscale = true;
            }
            else
            {
                UnsupportedTransformXml.Add(transform.OuterXml);
            }
        }
    }

    internal void AppendTransforms(XmlDocument document, XmlElement element)
    {
        if (Alpha >= 0 && Alpha < 100000)
        {
            AppendValueTransform(document, element, "alpha", ClampPercentage(Alpha));
        }

        if (Tint >= 0)
        {
            AppendValueTransform(document, element, "tint", ClampPercentage(Tint));
        }

        if (Shade >= 0)
        {
            AppendValueTransform(document, element, "shade", ClampPercentage(Shade));
        }

        if (Luminance >= 0)
        {
            AppendValueTransform(document, element, "lum", ClampPercentage(Luminance));
        }

        if (LuminanceModulation != 100000)
        {
            AppendValueTransform(document, element, "lumMod", ClampPercentage(LuminanceModulation));
        }

        if (LuminanceOffset != 0)
        {
            AppendValueTransform(document, element, "lumOff", ClampSignedPercentage(LuminanceOffset));
        }

        if (Saturation >= 0)
        {
            AppendValueTransform(document, element, "sat", ClampPercentage(Saturation));
        }

        if (SaturationModulation != 100000)
        {
            AppendValueTransform(document, element, "satMod", ClampPercentage(SaturationModulation));
        }

        if (SaturationOffset != 0)
        {
            AppendValueTransform(document, element, "satOff", ClampSignedPercentage(SaturationOffset));
        }

        if (HueAngleUnits >= 0)
        {
            AppendValueTransform(document, element, "hue", HueAngleUnits);
        }

        if (HueModulation != 100000)
        {
            AppendValueTransform(document, element, "hueMod", ClampPercentage(HueModulation));
        }

        if (HueOffsetAngleUnits != 0)
        {
            AppendValueTransform(document, element, "hueOff", HueOffsetAngleUnits);
        }

        if (Complement)
        {
            element.AppendChild(document.CreateElement("a", "comp", PptxNamespaces.Drawing));
        }

        if (Inverse)
        {
            element.AppendChild(document.CreateElement("a", "inv", PptxNamespaces.Drawing));
        }

        if (Grayscale)
        {
            element.AppendChild(document.CreateElement("a", "gray", PptxNamespaces.Drawing));
        }

        foreach (string xml in UnsupportedTransformXml)
        {
            XmlDocument transformDocument = PptxXml.LoadString(xml);
            XmlElement? transform = transformDocument.DocumentElement;
            if (transform != null)
            {
                element.AppendChild(document.ImportNode(transform, true));
            }
        }
    }

    internal static void AppendValueTransform(XmlDocument document, XmlElement element, string localName, int value)
    {
        XmlElement transform = document.CreateElement("a", localName, PptxNamespaces.Drawing);
        PptxXml.SetAttributeValue(transform, "", "", "val", value.ToString());
        element.AppendChild(transform);
    }

    internal static int ClampPercentage(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 100000)
        {
            return 100000;
        }

        return value;
    }

    internal static int ClampSignedPercentage(int value)
    {
        if (value < -100000)
        {
            return -100000;
        }

        if (value > 100000)
        {
            return 100000;
        }

        return value;
    }
}
