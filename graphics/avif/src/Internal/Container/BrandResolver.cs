using System.Collections.Generic;

namespace avif;

internal enum IsoBrandKind
{
    Unknown = 0,
    HeifImage = 1,
    HeifSequence = 2,
    Miaf = 3,
    AvifImage = 4,
    AvifSequence = 5,
    AvifIntraOnly = 6,
    AvifBaselineProfile = 7,
    AvifAdvancedProfile = 8,
    Av1Codec = 9,
    LowOverheadHeif = 10
}

internal class BrandCapabilityReport
{
    internal bool IsAvif;
    internal bool HasImageItems;
    internal bool HasImageSequence;
    internal bool IsIntraOnly;
    internal bool HasBaselineProfile;
    internal bool HasAdvancedProfile;
    internal bool HasMiaf;
    internal bool HasHeifImageBrand;
    internal bool HasHeifSequenceBrand;
    internal bool HasAv1CodecBrand;
    internal bool HasLowOverheadBrand;
    internal bool HasUnknownMajorBrand;
    internal bool HasForwardCompatibleBrands;
    internal string MimeType;
    internal List<string> UnknownBrands;

    internal BrandCapabilityReport()
    {
        IsAvif = false;
        HasImageItems = false;
        HasImageSequence = false;
        IsIntraOnly = false;
        HasBaselineProfile = false;
        HasAdvancedProfile = false;
        HasMiaf = false;
        HasHeifImageBrand = false;
        HasHeifSequenceBrand = false;
        HasAv1CodecBrand = false;
        HasLowOverheadBrand = false;
        HasUnknownMajorBrand = false;
        HasForwardCompatibleBrands = false;
        MimeType = string.Empty;
        UnknownBrands = new List<string>();
    }
}

internal static class BrandResolver
{
    internal static IsoBrandKind GetKind(string brand)
    {
        if (brand == "mif1")
        {
            return IsoBrandKind.HeifImage;
        }

        if (brand == "msf1")
        {
            return IsoBrandKind.HeifSequence;
        }

        if (brand == "miaf")
        {
            return IsoBrandKind.Miaf;
        }

        if (brand == "avif")
        {
            return IsoBrandKind.AvifImage;
        }

        if (brand == "avis")
        {
            return IsoBrandKind.AvifSequence;
        }

        if (brand == "avio")
        {
            return IsoBrandKind.AvifIntraOnly;
        }

        if (brand == "MA1B")
        {
            return IsoBrandKind.AvifBaselineProfile;
        }

        if (brand == "MA1A")
        {
            return IsoBrandKind.AvifAdvancedProfile;
        }

        if (brand == "av01")
        {
            return IsoBrandKind.Av1Codec;
        }

        if (brand == "mif3")
        {
            return IsoBrandKind.LowOverheadHeif;
        }

        return IsoBrandKind.Unknown;
    }

    internal static BrandCapabilityReport Resolve(FileTypeBox? fileType)
    {
        BrandCapabilityReport report = new BrandCapabilityReport();
        if (fileType == null || fileType.Code != AvifResultCode.Success)
        {
            return report;
        }

        IsoBrandKind majorKind = GetKind(fileType.MajorBrand);
        if (majorKind == IsoBrandKind.Unknown)
        {
            report.HasUnknownMajorBrand = true;
            AddUnknown(report, fileType.MajorBrand);
        }
        else
        {
            Observe(report, majorKind);
        }

        int index = 0;
        while (index < fileType.CompatibleBrands.Count)
        {
            string brand = fileType.CompatibleBrands[index];
            IsoBrandKind kind = GetKind(brand);
            if (kind == IsoBrandKind.Unknown)
            {
                report.HasForwardCompatibleBrands = true;
                AddUnknown(report, brand);
            }
            else
            {
                Observe(report, kind);
            }

            index++;
        }

        report.IsAvif = report.HasImageItems
            || report.HasImageSequence
            || report.IsIntraOnly
            || report.HasBaselineProfile
            || report.HasAdvancedProfile;
        if (report.IsAvif)
        {
            report.MimeType = "image/avif";
        }
        else if (report.HasHeifSequenceBrand)
        {
            report.MimeType = "image/heif-sequence";
        }
        else if (report.HasHeifImageBrand || report.HasMiaf || report.HasLowOverheadBrand)
        {
            report.MimeType = "image/heif";
        }

        return report;
    }

    internal static void Observe(BrandCapabilityReport report, IsoBrandKind kind)
    {
        if (kind == IsoBrandKind.HeifImage)
        {
            report.HasHeifImageBrand = true;
        }
        else if (kind == IsoBrandKind.HeifSequence)
        {
            report.HasHeifSequenceBrand = true;
        }
        else if (kind == IsoBrandKind.Miaf)
        {
            report.HasMiaf = true;
        }
        else if (kind == IsoBrandKind.AvifImage)
        {
            report.HasImageItems = true;
        }
        else if (kind == IsoBrandKind.AvifSequence)
        {
            report.HasImageSequence = true;
        }
        else if (kind == IsoBrandKind.AvifIntraOnly)
        {
            report.IsIntraOnly = true;
        }
        else if (kind == IsoBrandKind.AvifBaselineProfile)
        {
            report.HasBaselineProfile = true;
        }
        else if (kind == IsoBrandKind.AvifAdvancedProfile)
        {
            report.HasAdvancedProfile = true;
        }
        else if (kind == IsoBrandKind.Av1Codec)
        {
            report.HasAv1CodecBrand = true;
        }
        else if (kind == IsoBrandKind.LowOverheadHeif)
        {
            report.HasLowOverheadBrand = true;
        }
    }

    internal static void AddUnknown(BrandCapabilityReport report, string brand)
    {
        int index = 0;
        while (index < report.UnknownBrands.Count)
        {
            if (report.UnknownBrands[index] == brand)
            {
                return;
            }

            index++;
        }

        report.UnknownBrands.Add(brand);
    }
}
