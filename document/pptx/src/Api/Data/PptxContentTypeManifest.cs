using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Represents content type manifest data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxContentTypeManifest
{
    /// <summary>
    /// Contains the defaults in package or presentation order.
    /// </summary>
    public List<PptxContentTypeDefault> Defaults;
    /// <summary>
    /// Contains the overrides in package or presentation order.
    /// </summary>
    public List<PptxContentTypeOverride> Overrides;
    /// <summary>
    /// Stores the diagnostics value associated with this object.
    /// </summary>
    public PptxResult Diagnostics;

    /// <summary>
    /// Initializes a new content type manifest with safe defaults or the supplied values.
    /// </summary>
    public PptxContentTypeManifest()
    {
        Defaults = new List<PptxContentTypeDefault>();
        Overrides = new List<PptxContentTypeOverride>();
        Diagnostics = new PptxResult();
    }
    internal bool Succeeded()
    {
        return Diagnostics.Succeeded();
    }
    internal bool Failed()
    {
        return Diagnostics.Failed();
    }
    internal int DefaultCount()
    {
        return Defaults.Count;
    }
    internal int OverrideCount()
    {
        return Overrides.Count;
    }
    internal void AddDefault(string extension, string contentType)
    {
        Defaults.Add(new PptxContentTypeDefault(extension, contentType));
    }
    internal void AddOverride(string partName, string contentType)
    {
        Overrides.Add(new PptxContentTypeOverride(partName, contentType));
    }
    internal string GetDefaultContentType(string extension)
    {
        string normalized = PptxContentTypesParser.NormalizeExtension(extension);

        for (int index = 0; index < Defaults.Count; index++)
        {
            if (Defaults[index].Extension == normalized)
            {
                return Defaults[index].ContentType;
            }
        }

        return "";
    }
    internal string GetOverrideContentType(string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);

        for (int index = 0; index < Overrides.Count; index++)
        {
            if (Overrides[index].PartName == normalized)
            {
                return Overrides[index].ContentType;
            }
        }

        return "";
    }
    internal string GetContentTypeForPartName(string partName)
    {
        string overrideContentType = GetOverrideContentType(partName);
        if (overrideContentType.Length > 0)
        {
            return overrideContentType;
        }

        string extension = PptxContentTypesParser.GetPartNameExtension(partName);
        if (extension.Length == 0)
        {
            return "";
        }

        return GetDefaultContentType(extension);
    }
}
