using System;
using System.Collections.Generic;

namespace avif;

internal enum CanonicalAvifProfile
{
    General = 0,
    Baseline = 1,
    Advanced = 2
}

internal class CanonicalBrandSelection
{
    internal AvifResultCode Code;
    internal string MajorBrand;
    internal List<string> CompatibleBrands;
    internal CanonicalAvifProfile Profile;

    internal CanonicalBrandSelection(
        AvifResultCode code,
        string majorBrand,
        List<string> compatibleBrands,
        CanonicalAvifProfile profile)
    {
        Code = code;
        MajorBrand = majorBrand;
        CompatibleBrands = compatibleBrands;
        Profile = profile;
    }
}

internal class CanonicalItemProfileProbe
{
    internal bool Complete;
    internal bool Baseline;
    internal bool Advanced;

    internal CanonicalItemProfileProbe(
        bool complete,
        bool baseline,
        bool advanced)
    {
        Complete = complete;
        Baseline = baseline;
        Advanced = advanced;
    }
}

internal class CanonicalParsedProperty
{
    internal AvifResultCode Code;
    internal IsoBox? Box;
    internal ResourceGuard Resources;

    internal CanonicalParsedProperty(
        AvifResultCode code,
        IsoBox? box,
        ResourceGuard resources)
    {
        Code = code;
        Box = box;
        Resources = resources;
    }
}

internal static class CanonicalBrandSelector
{
    internal static CanonicalBrandSelection Choose(
        List<CanonicalItemPayload> items,
        string requestedMajorBrand,
        List<string>? requestedCompatibleBrands,
        bool timedSequence)
    {
        List<string> brands = new List<string>();
        if (items == null || requestedCompatibleBrands == null)
        {
            return new CanonicalBrandSelection(
                AvifResultCode.InvalidArgument,
                string.Empty,
                brands,
                CanonicalAvifProfile.General);
        }

        AddBrand(brands, "mif1");
        AddBrand(brands, "miaf");
        CanonicalAvifProfile profile = SelectProfile(items);
        if (profile == CanonicalAvifProfile.Baseline)
        {
            AddBrand(brands, "MA1B");
        }
        else if (profile == CanonicalAvifProfile.Advanced)
        {
            AddBrand(brands, "MA1A");
        }

        AddRequestedBrand(brands, requestedMajorBrand, timedSequence);
        int brandIndex = 0;
        while (brandIndex < requestedCompatibleBrands.Count)
        {
            AddRequestedBrand(
                brands,
                requestedCompatibleBrands[brandIndex],
                timedSequence);
            brandIndex++;
        }

        return new CanonicalBrandSelection(
            AvifResultCode.Success,
            "avif",
            brands,
            profile);
    }

    internal static CanonicalAvifProfile SelectProfile(
        List<CanonicalItemPayload> items)
    {
        bool foundCodedItem = false;
        bool baseline = true;
        bool advanced = true;
        int itemIndex = 0;
        while (itemIndex < items.Count)
        {
            CanonicalItemPayload item = items[itemIndex];
            if (item.ItemType == "av01")
            {
                foundCodedItem = true;
                CanonicalItemProfileProbe probe = ProbeItem(item);
                if (!probe.Complete || !probe.Baseline)
                {
                    baseline = false;
                }

                if (!probe.Complete || !probe.Advanced)
                {
                    advanced = false;
                }
            }
            else if (item.ItemType == "grid")
            {
                CanonicalItemProfileProbe probe = ProbeGridItem(item);
                if (!probe.Complete || !probe.Baseline)
                {
                    baseline = false;
                }

                if (!probe.Complete || !probe.Advanced)
                {
                    advanced = false;
                }
            }

            itemIndex++;
        }

        if (foundCodedItem && baseline)
        {
            return CanonicalAvifProfile.Baseline;
        }

        if (foundCodedItem && advanced)
        {
            return CanonicalAvifProfile.Advanced;
        }

        return CanonicalAvifProfile.General;
    }

    internal static CanonicalItemProfileProbe ProbeItem(CanonicalItemPayload item)
    {
        ParsedAv1Configuration? configuration = null;
        ParsedImageSpatialExtents? extents = null;
        int associationIndex = 0;
        while (associationIndex < item.Properties.Count)
        {
            CanonicalItemPropertyAssociation association = item.Properties[associationIndex];
            CanonicalPropertyProbe header = CanonicalItemContainerWriter.ProbeProperty(
                association.RawProperty);
            if (header.Code == AvifResultCode.Success
                && (header.Type == "av1C" || header.Type == "ispe"))
            {
                CanonicalParsedProperty property = ParseProperty(association.RawProperty);
                if (property.Code != AvifResultCode.Success)
                {
                    return new CanonicalItemProfileProbe(false, false, false);
                }

                if (header.Type == "av1C")
                {
                    Av1ItemConfigurationParseResult parsed =
                        Av1ItemConfigurationBox.ParsePayload(
                            association.RawProperty.AsSpan(
                                (int)(property.Box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset,
                                (int)property.Box.PayloadSize));
                    if (parsed.Code != AvifResultCode.Success || configuration != null)
                    {
                        return new CanonicalItemProfileProbe(false, false, false);
                    }

                    configuration = parsed.Configuration;
                }
                else
                {
                    ImageSpatialExtentsParseResult parsed = ImageSpatialExtentsProperty.Parse(
                        association.RawProperty,
                        property.Box,
                        property.Resources);
                    if (parsed.Code != AvifResultCode.Success || extents != null)
                    {
                        return new CanonicalItemProfileProbe(false, false, false);
                    }

                    extents = parsed.Extents;
                }
            }

            associationIndex++;
        }

        if (configuration == null || extents == null)
        {
            return new CanonicalItemProfileProbe(false, false, false);
        }

        long width = extents.GetWidth();
        long height = extents.GetHeight();
        CheckedIntegerResult pixels = CheckedMath.MultiplyInt64(width, height);
        bool baseline = configuration.GetSequenceProfile() == 0
            && configuration.GetSequenceLevelIndex() <= 13
            && width <= 8192
            && height <= 4352
            && pixels.Success
            && pixels.Value <= 8912896;
        bool advanced = configuration.GetSequenceProfile() == 1
            && configuration.GetSequenceLevelIndex() <= 16
            && width <= 16384
            && height <= 8704
            && pixels.Success
            && pixels.Value <= 35651584;
        return new CanonicalItemProfileProbe(true, baseline, advanced);
    }

    internal static CanonicalItemProfileProbe ProbeGridItem(
        CanonicalItemPayload item)
    {
        ParsedImageSpatialExtents? extents = null;
        int associationIndex = 0;
        while (associationIndex < item.Properties.Count)
        {
            CanonicalItemPropertyAssociation association =
                item.Properties[associationIndex];
            CanonicalPropertyProbe header =
                CanonicalItemContainerWriter.ProbeProperty(
                    association.RawProperty);
            if (header.Code == AvifResultCode.Success
                && header.Type == "ispe")
            {
                CanonicalParsedProperty property = ParseProperty(
                    association.RawProperty);
                if (property.Code != AvifResultCode.Success)
                {
                    return new CanonicalItemProfileProbe(
                        false,
                        false,
                        false);
                }
                ImageSpatialExtentsParseResult parsed =
                    ImageSpatialExtentsProperty.Parse(
                        association.RawProperty,
                        property.Box,
                        property.Resources);
                if (parsed.Code != AvifResultCode.Success || extents != null)
                {
                    return new CanonicalItemProfileProbe(
                        false,
                        false,
                        false);
                }
                extents = parsed.Extents;
            }
            associationIndex++;
        }
        if (extents == null)
        {
            return new CanonicalItemProfileProbe(false, false, false);
        }

        long width = extents.GetWidth();
        long height = extents.GetHeight();
        CheckedIntegerResult pixels = CheckedMath.MultiplyInt64(width, height);
        bool baseline = width <= 8192
            && height <= 4352
            && pixels.Success
            && pixels.Value <= 8912896;
        bool advanced = width <= 16384
            && height <= 8704
            && pixels.Success
            && pixels.Value <= 35651584;
        return new CanonicalItemProfileProbe(true, baseline, advanced);
    }

    internal static CanonicalParsedProperty ParseProperty(byte[] raw)
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        IsoBoxParseResult parsed = IsoBoxReader.Parse(raw, limits, false);
        if (parsed.Code != AvifResultCode.Success
            || parsed.Boxes.Count != 1
            || parsed.Boxes[0].Size != raw.LongLength)
        {
            AvifResultCode code = parsed.Code == AvifResultCode.Success
                ? AvifResultCode.InvalidContainer
                : parsed.Code;
            return new CanonicalParsedProperty(code, null, parsed.Resources);
        }

        return new CanonicalParsedProperty(
            AvifResultCode.Success,
            parsed.Boxes[0],
            parsed.Resources);
    }

    internal static void AddRequestedBrand(
        List<string> brands,
        string brand,
        bool timedSequence)
    {
        if (brand == null)
        {
            return;
        }

        if (timedSequence
            && (brand == "avis" || brand == "avio" || brand == "msf1"))
        {
            AddBrand(brands, brand);
            return;
        }

        if (IsManagedBrand(brand))
        {
            return;
        }

        AddBrand(brands, brand);
    }

    internal static bool IsManagedBrand(string brand)
    {
        return brand == "avif"
            || brand == "avis"
            || brand == "avio"
            || brand == "mif1"
            || brand == "msf1"
            || brand == "miaf"
            || brand == "MA1B"
            || brand == "MA1A";
    }

    internal static void AddBrand(List<string> brands, string brand)
    {
        int index = 0;
        while (index < brands.Count)
        {
            if (brands[index] == brand)
            {
                return;
            }

            index++;
        }

        brands.Add(brand);
    }
}
