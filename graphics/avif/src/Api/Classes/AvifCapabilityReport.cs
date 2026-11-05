using System.Collections.Generic;

namespace avif;

/// <summary>Reports capabilities declared by the file-type brands of a probed source.</summary>
public class AvifCapabilityReport : IAvifCapabilityReport
{
    bool avif;
    bool imageItems;
    bool imageSequence;
    bool intraOnly;
    bool baselineProfile;
    bool advancedProfile;
    string mimeType;
    List<AvifFeatureCapability> features;

    internal AvifCapabilityReport(
        BrandCapabilityReport? source,
        AvifDocument? document)
    {
        avif = source != null && source.IsAvif;
        imageItems = source != null && source.HasImageItems;
        imageSequence = source != null && source.HasImageSequence;
        intraOnly = source != null && source.IsIntraOnly;
        baselineProfile = source != null && source.HasBaselineProfile;
        advancedProfile = source != null && source.HasAdvancedProfile;
        mimeType = source == null ? string.Empty : source.MimeType;
        features = new List<AvifFeatureCapability>();
        if (document != null)
        {
            ReadDocumentBrands(document);
        }
        AddDeclaredFeatures(source);
        AddDocumentFeatures(document);
    }

    /// <summary>Reports whether the source declares an AVIF brand.</summary>
    /// <returns><c>true</c> for an AVIF still, sequence, intra-only, or profile brand.</returns>
    public bool IsAvif()
    {
        return avif;
    }

    /// <summary>Reports whether still-image item capability is declared.</summary>
    /// <returns><c>true</c> when the <c>avif</c> brand is present.</returns>
    public bool HasImageItems()
    {
        return imageItems;
    }

    /// <summary>Reports whether timed image-sequence capability is declared.</summary>
    /// <returns><c>true</c> when the <c>avis</c> brand is present.</returns>
    public bool HasImageSequence()
    {
        return imageSequence;
    }

    /// <summary>Reports whether the intra-only AVIF constraint is declared.</summary>
    /// <returns><c>true</c> when the <c>avio</c> brand is present.</returns>
    public bool IsIntraOnly()
    {
        return intraOnly;
    }

    /// <summary>Reports whether the AVIF baseline profile is declared.</summary>
    /// <returns><c>true</c> when the <c>MA1B</c> brand is present.</returns>
    public bool HasBaselineProfile()
    {
        return baselineProfile;
    }

    /// <summary>Reports whether the AVIF advanced profile is declared.</summary>
    /// <returns><c>true</c> when the <c>MA1A</c> brand is present.</returns>
    public bool HasAdvancedProfile()
    {
        return advancedProfile;
    }

    /// <summary>Returns the MIME type inferred from declared brands.</summary>
    /// <returns>The inferred MIME type, or an empty string when unknown.</returns>
    public string GetMimeType()
    {
        return mimeType;
    }

    /// <summary>Returns the number of detected high-level features.</summary>
    /// <returns>The feature count.</returns>
    public int GetFeatureCount()
    {
        return features.Count;
    }

    /// <summary>Returns one detected feature and its supported operations.</summary>
    /// <param name="index">The zero-based detected-feature index.</param>
    /// <returns>An independent feature description, or null for an invalid index.</returns>
    public AvifFeatureCapability? GetFeature(int index)
    {
        return index < 0 || index >= features.Count
            ? null : CopyFeature(features[index]);
    }

    /// <summary>Reports whether one high-level feature was detected.</summary>
    /// <param name="feature">The feature to locate.</param>
    /// <returns><c>true</c> when the source declares or contains the feature.</returns>
    public bool HasFeature(AvifFeature feature)
    {
        return FindFeatureIndex(feature) >= 0;
    }

    /// <summary>Finds the supported operations for one detected feature.</summary>
    /// <param name="feature">The feature to locate.</param>
    /// <returns>An independent feature description, or null when undetected.</returns>
    public AvifFeatureCapability? GetFeatureCapability(AvifFeature feature)
    {
        int index = FindFeatureIndex(feature);
        return index < 0 ? null : CopyFeature(features[index]);
    }

    void ReadDocumentBrands(AvifDocument document)
    {
        AvifFileInfo? information = document.GetFileInfo();
        if (information == null)
        {
            return;
        }
        imageItems = information.HasBrand("avif");
        imageSequence = information.HasBrand("avis");
        intraOnly = information.HasBrand("avio");
        baselineProfile = information.HasBrand("MA1B");
        advancedProfile = information.HasBrand("MA1A");
        avif = imageItems || imageSequence || intraOnly
            || baselineProfile || advancedProfile;
        if (avif)
        {
            mimeType = "image/avif";
        }
        else if (information.HasBrand("msf1"))
        {
            mimeType = "image/heif-sequence";
        }
        else if (information.HasBrand("mif1")
            || information.HasBrand("miaf")
            || information.HasBrand("mif3"))
        {
            mimeType = "image/heif";
        }
    }

    void AddDeclaredFeatures(BrandCapabilityReport? source)
    {
        if (imageItems)
        {
            AddFeature(AvifFeature.ImageItem);
        }
        if (imageSequence)
        {
            AddFeature(AvifFeature.ImageSequence);
        }
        if (source != null && source.HasLowOverheadBrand)
        {
            AddFeature(AvifFeature.LowOverhead);
        }
    }

    void AddDocumentFeatures(AvifDocument? document)
    {
        if (document == null)
        {
            return;
        }
        int visibleCodedItemCount = 0;
        int itemIndex = 0;
        while (itemIndex < document.GetItemCount())
        {
            AvifImageItem? item = document.GetItem(itemIndex);
            if ((item ?? throw new System.InvalidOperationException("Invalid internal state.")).IsCodedImage()
                || item.IsDerivedImage())
            {
                AddFeature(AvifFeature.ImageItem);
            }
            if (item.IsCodedImage() && !item.IsHidden())
            {
                visibleCodedItemCount++;
            }
            if (item.GetItemType() == "grid")
            {
                AddFeature(AvifFeature.Grid);
            }
            if (item.GetItemType() == "tmap")
            {
                AddFeature(AvifFeature.GainMap);
            }
            if (item.GetItemType() == "sato")
            {
                AddFeature(AvifFeature.SampleTransform);
            }
            if (item.IsAuxiliaryImage())
            {
                AddFeature(AvifFeature.AuxiliaryImage);
            }
            if (item.GetProtectionIndex() != 0)
            {
                AddFeature(AvifFeature.ProtectedMedia);
            }
            ReadLayerFeatures(item);
            itemIndex++;
        }
        if (visibleCodedItemCount > 1)
        {
            AddFeature(AvifFeature.ImageCollection);
        }
        AvifMovie? movie = document.GetMovie();
        if (movie != null && movie.GetVisualTrackCount() > 0)
        {
            AddFeature(AvifFeature.ImageSequence);
        }
        if (movie != null)
        {
            int trackIndex = 0;
            while (trackIndex < movie.GetTrackCount())
            {
                AvifTrack? track = movie.GetTrack(trackIndex);
                if ((track ?? throw new System.InvalidOperationException("Invalid internal state.")).IsAuxiliary())
                {
                    AddFeature(AvifFeature.AuxiliaryImage);
                }
                trackIndex++;
            }
        }
        int boxIndex = 0;
        while (boxIndex < document.GetBoxCount())
        {
            AvifBox? box = document.GetBox(boxIndex);
            string type = (box ?? throw new System.InvalidOperationException("Invalid internal state.")).GetTypeName();
            if (type == "mini")
            {
                AddFeature(AvifFeature.LowOverhead);
            }
            if (type == "moof" || type == "mvex")
            {
                AddFeature(AvifFeature.FragmentedMedia);
            }
            if (type == "sinf" || type == "schi")
            {
                AddFeature(AvifFeature.ProtectedMedia);
            }
            boxIndex++;
        }
        AvifFileInfo? information = document.GetFileInfo();
        if (information != null && information.HasBrand("mif3"))
        {
            AddFeature(AvifFeature.LowOverhead);
        }
    }

    void ReadLayerFeatures(AvifImageItem item)
    {
        int propertyIndex = 0;
        while (propertyIndex < item.GetPropertyCount())
        {
            AvifItemProperty? property = item.GetProperty(propertyIndex);
            string type = (property ?? throw new System.InvalidOperationException("Invalid internal state.")).GetTypeName();
            if (type == "lsel" || type == "a1lx")
            {
                AddFeature(AvifFeature.LayeredImage);
            }
            if (type == "a1lx" && property.GetLayerCount() > 1)
            {
                AddFeature(AvifFeature.ProgressiveImage);
            }
            propertyIndex++;
        }
    }

    void AddFeature(AvifFeature feature)
    {
        if (FindFeatureIndex(feature) >= 0)
        {
            return;
        }
        bool preservationOnly = feature == AvifFeature.FragmentedMedia
            || feature == AvifFeature.ProtectedMedia;
        features.Add(new AvifFeatureCapability(
            feature,
            !preservationOnly,
            !preservationOnly,
            !preservationOnly,
            !preservationOnly,
            preservationOnly));
    }

    int FindFeatureIndex(AvifFeature feature)
    {
        int index = 0;
        while (index < features.Count)
        {
            if (features[index].Feature == feature)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    AvifFeatureCapability CopyFeature(AvifFeatureCapability feature)
    {
        return new AvifFeatureCapability(
            feature.Feature,
            feature.Decodable,
            feature.Renderable,
            feature.Writable,
            feature.Rewritable,
            feature.PreservableOnly);
    }
}
