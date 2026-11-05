using System.Collections.Generic;

namespace image;

internal class SvgMetadataState
{
    internal string Width;
    internal string Height;
    internal string ViewBox;
    internal string PreserveAspectRatio;
    internal string Title;
    internal string Description;
    internal List<string> ImageLinks;
    internal List<string> MetadataXml;

    internal SvgMetadataState()
    {
        Width = "";
        Height = "";
        ViewBox = "";
        PreserveAspectRatio = "";
        Title = "";
        Description = "";
        ImageLinks = new List<string>();
        MetadataXml = new List<string>();
    }
}
