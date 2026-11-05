using System.Collections.Generic;

namespace image;

internal class BmffBrandInfo
{
    internal bool Valid;
    internal string MajorBrand;
    internal uint MinorVersion;
    internal List<string> CompatibleBrands;
    internal ImageFormatKind Kind;
    internal string Name;
    internal int Confidence;
    internal bool ClassifiedByCompatibleBrand;

    internal BmffBrandInfo()
    {
        Valid = false;
        MajorBrand = "";
        MinorVersion = 0;
        CompatibleBrands = new List<string>();
        Kind = ImageFormatKind.Unknown;
        Name = "BMFF";
        Confidence = 40;
        ClassifiedByCompatibleBrand = false;
    }
}
