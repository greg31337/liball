namespace ttf;

internal class FontMatchCandidate
{
    const int WeightTag = 0x77676874;
    const int WidthTag = 0x77647468;
    const int OpticalSizeTag = 0x6F70737A;
    const int ItalicTag = 0x6974616C;
    const int SlantTag = 0x736C6E74;

    FontFace face;
    int faceIndex;
    int stretchSide;
    double stretchValue;
    int styleRank;
    int weightSide;
    double weightValue;
    int opticalRank;
    double opticalDistance;

    internal FontMatchCandidate(FontFace face, int faceIndex, FontRequest request)
    {
        this.face = face;
        this.faceIndex = faceIndex;
        SfntFvarTable? fvar = face.GetFvarTable();
        FontClassification classification = face.GetClassification();
        double requestedStretch = GetTargetValue(request, WidthTag, request.GetStretch());
        double requestedWeight = GetTargetValue(request, WeightTag, request.GetWeight());
        stretchValue = ResolveAxisOrStaticValue(
            fvar,
            WidthTag,
            requestedStretch,
            GetStaticStretch(classification));
        weightValue = ResolveAxisOrStaticValue(
            fvar,
            WeightTag,
            requestedWeight,
            GetStaticWeight(classification));
        stretchSide = GetStretchSide(requestedStretch, stretchValue);
        weightSide = GetWeightSide(requestedWeight, weightValue);
        styleRank = GetStyleRank(face, fvar, request);
        SetOpticalScore(fvar, classification, request);
    }

    internal FontFace GetFace()
    {
        return face;
    }

    internal bool IsBetterThan(FontMatchCandidate? other, FontRequest request)
    {
        if (other == null)
        {
            return true;
        }

        double requestedStretch = GetTargetValue(
            request,
            WidthTag,
            request.GetStretch());
        int stretchComparison = CompareStretch(other, requestedStretch);

        if (stretchComparison != 0)
        {
            return stretchComparison < 0;
        }

        if (styleRank != other.styleRank)
        {
            return styleRank < other.styleRank;
        }

        double requestedWeight = GetTargetValue(
            request,
            WeightTag,
            request.GetWeight());
        int weightComparison = CompareWeight(other, requestedWeight);

        if (weightComparison != 0)
        {
            return weightComparison < 0;
        }

        if (opticalRank != other.opticalRank)
        {
            return opticalRank < other.opticalRank;
        }

        if (opticalDistance != other.opticalDistance)
        {
            return opticalDistance < other.opticalDistance;
        }

        return faceIndex < other.faceIndex;
    }

    internal static bool SupportsExplicitCoordinates(FontFace face, FontRequest request)
    {
        FontCoordinates? requested = request.GetCoordinates();

        System.ArgumentNullException.ThrowIfNull(requested);
        if (requested.GetCount() == 0)
        {
            return true;
        }

        SfntFvarTable? fvar = face.GetFvarTable();

        if (fvar == null)
        {
            return false;
        }

        int index = 0;

        while (index < requested.GetCount())
        {
            if (fvar.FindAxis(requested.GetAxisTag(index)) == null)
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static FontCoordinates CreateCoordinates(FontFace face, FontRequest request)
    {
        FontCoordinates result = new FontCoordinates();
        SfntFvarTable? fvar = face.GetFvarTable();

        if (fvar != null)
        {
            AddAutomaticCoordinates(result, fvar, request);
        }

        FontCoordinates? explicitCoordinates = request.GetCoordinates();
        int index = 0;

        System.ArgumentNullException.ThrowIfNull(explicitCoordinates);
        while (index < explicitCoordinates.GetCount())
        {
            result.Set(
                explicitCoordinates.GetAxisTag(index),
                explicitCoordinates.GetValue(index));
            index += 1;
        }

        return result;
    }

    int CompareStretch(FontMatchCandidate other, double requested)
    {
        if (stretchSide != other.stretchSide)
        {
            return stretchSide < other.stretchSide ? -1 : 1;
        }

        if (stretchValue == other.stretchValue)
        {
            return 0;
        }

        bool preferLarger = requested <= 100 && stretchSide == 0 ||
            requested > 100 && stretchSide == 1;
        return preferLarger == stretchValue > other.stretchValue ? -1 : 1;
    }

    int CompareWeight(FontMatchCandidate other, double requested)
    {
        if (weightSide != other.weightSide)
        {
            return weightSide < other.weightSide ? -1 : 1;
        }

        if (weightValue == other.weightValue)
        {
            return 0;
        }

        bool preferLarger = requested < 400 && weightSide == 0 ||
            requested >= 400 && requested <= 500 && weightSide == 1 ||
            requested > 500 && weightSide == 1;
        return preferLarger == weightValue > other.weightValue ? -1 : 1;
    }

    void SetOpticalScore(
        SfntFvarTable? fvar,
        FontClassification classification,
        FontRequest request)
    {
        FontCoordinates? coordinates = request.GetCoordinates();
        System.ArgumentNullException.ThrowIfNull(coordinates);
        bool explicitOptical = coordinates.Contains(OpticalSizeTag);

        if (!request.HasOpticalSize() && !explicitOptical)
        {
            opticalRank = 0;
            opticalDistance = 0;
            return;
        }

        double requested = explicitOptical
            ? coordinates.Get(OpticalSizeTag)
            : request.GetOpticalSize();
        SfntFvarAxis? axis = fvar == null ? null : fvar.FindAxis(OpticalSizeTag);

        if (axis != null)
        {
            opticalRank = 0;
            opticalDistance = DistanceToRange(
                requested,
                axis.GetMinimumValue(),
                axis.GetMaximumValue());
            return;
        }

        if (classification.HasOpticalPointSizes())
        {
            opticalRank = 0;
            opticalDistance = DistanceToRange(
                requested,
                classification.GetLowerOpticalPointSize(),
                classification.GetUpperOpticalPointSize());
            return;
        }

        opticalRank = 1;
        opticalDistance = double.PositiveInfinity;
    }

    static double GetTargetValue(FontRequest request, int tag, double fallback)
    {
        FontCoordinates? coordinates = request.GetCoordinates();
        System.ArgumentNullException.ThrowIfNull(coordinates);
        return coordinates.Contains(tag) ? coordinates.Get(tag) : fallback;
    }

    static double ResolveAxisOrStaticValue(
        SfntFvarTable? fvar,
        int tag,
        double requested,
        double staticValue)
    {
        SfntFvarAxis? axis = fvar == null ? null : fvar.FindAxis(tag);

        if (axis == null)
        {
            return staticValue;
        }

        return Clamp(requested, axis.GetMinimumValue(), axis.GetMaximumValue());
    }

    static int GetStyleRank(FontFace face, SfntFvarTable? fvar, FontRequest request)
    {
        FontStyle requestedStyle = GetRequestedStyle(request);
        FontStyle availableStyle = GetExplicitResolvedStyle(fvar, request);

        if (availableStyle == (FontStyle)(-1))
        {
            return GetCapabilityStyleRank(face.GetClassification(), fvar, requestedStyle);
        }

        return CompareStyles(requestedStyle, availableStyle);
    }

    static FontStyle GetRequestedStyle(FontRequest request)
    {
        FontCoordinates? coordinates = request.GetCoordinates();

        System.ArgumentNullException.ThrowIfNull(coordinates);
        if (coordinates.Contains(ItalicTag) && coordinates.Get(ItalicTag) != 0)
        {
            return FontStyle.Italic;
        }

        if (coordinates.Contains(SlantTag) && coordinates.Get(SlantTag) != 0)
        {
            return FontStyle.Oblique;
        }

        if (coordinates.Contains(ItalicTag) || coordinates.Contains(SlantTag))
        {
            return FontStyle.Normal;
        }

        return request.GetStyle();
    }

    static FontStyle GetExplicitResolvedStyle(SfntFvarTable? fvar, FontRequest request)
    {
        FontCoordinates? coordinates = request.GetCoordinates();
        System.ArgumentNullException.ThrowIfNull(coordinates);
        bool hasItalic = coordinates.Contains(ItalicTag);
        bool hasSlant = coordinates.Contains(SlantTag);

        if (!hasItalic && !hasSlant)
        {
            return (FontStyle)(-1);
        }

        double italicValue = hasItalic
            ? ResolveExplicitAxis(fvar, ItalicTag, coordinates.Get(ItalicTag))
            : 0;
        double slantValue = hasSlant
            ? ResolveExplicitAxis(fvar, SlantTag, coordinates.Get(SlantTag))
            : 0;

        if (italicValue != 0)
        {
            return FontStyle.Italic;
        }

        return slantValue != 0 ? FontStyle.Oblique : FontStyle.Normal;
    }

    static double ResolveExplicitAxis(SfntFvarTable? fvar, int tag, double value)
    {
        System.ArgumentNullException.ThrowIfNull(fvar);
        SfntFvarAxis? axis = fvar.FindAxis(tag);
        System.ArgumentNullException.ThrowIfNull(axis);
        return Clamp(value, axis.GetMinimumValue(), axis.GetMaximumValue());
    }

    static int GetCapabilityStyleRank(
        FontClassification classification,
        SfntFvarTable? fvar,
        FontStyle requested)
    {
        bool italic = classification.IsItalic() || CanExpressNonzero(fvar, ItalicTag);
        bool oblique = classification.IsOblique() || CanExpressNonzero(fvar, SlantTag);
        bool normal = true;

        if (classification.IsItalic())
        {
            normal = normal && CanExpressZero(fvar, ItalicTag);
        }

        if (classification.IsOblique())
        {
            normal = normal && CanExpressZero(fvar, SlantTag);
        }

        if (requested == FontStyle.Italic)
        {
            return italic ? 0 : (oblique ? 1 : 2);
        }

        if (requested == FontStyle.Oblique)
        {
            return oblique ? 0 : (italic ? 1 : 2);
        }

        return normal ? 0 : 2;
    }

    static int CompareStyles(FontStyle requested, FontStyle available)
    {
        if (requested == available)
        {
            return 0;
        }

        if (requested != FontStyle.Normal && available != FontStyle.Normal)
        {
            return 1;
        }

        return 2;
    }

    static bool CanExpressNonzero(SfntFvarTable? fvar, int tag)
    {
        SfntFvarAxis? axis = fvar == null ? null : fvar.FindAxis(tag);
        return axis != null && (axis.GetMinimumValue() < 0 || axis.GetMaximumValue() > 0);
    }

    static bool CanExpressZero(SfntFvarTable? fvar, int tag)
    {
        SfntFvarAxis? axis = fvar == null ? null : fvar.FindAxis(tag);
        return axis != null && axis.GetMinimumValue() <= 0 && axis.GetMaximumValue() >= 0;
    }

    static int GetStretchSide(double requested, double available)
    {
        if (requested <= 100)
        {
            return available <= requested ? 0 : 1;
        }

        return available >= requested ? 0 : 1;
    }

    static int GetWeightSide(double requested, double available)
    {
        if (requested < 400)
        {
            return available <= requested ? 0 : 1;
        }

        if (requested <= 500)
        {
            if (available >= requested && available <= 500)
            {
                return 0;
            }

            return available < requested ? 1 : 2;
        }

        return available >= requested ? 0 : 1;
    }

    static double GetStaticWeight(FontClassification classification)
    {
        int value = classification.GetWeightClass();
        return value == 0 ? 400 : value;
    }

    static double GetStaticStretch(FontClassification classification)
    {
        int value = classification.GetWidthClass();

        if (value == 1) { return 50; }
        if (value == 2) { return 62.5; }
        if (value == 3) { return 75; }
        if (value == 4) { return 87.5; }
        if (value == 6) { return 112.5; }
        if (value == 7) { return 125; }
        if (value == 8) { return 150; }
        if (value == 9) { return 200; }
        return 100;
    }

    static double DistanceToRange(double value, double minimum, double maximum)
    {
        if (value < minimum) { return minimum - value; }
        if (value > maximum) { return value - maximum; }
        return 0;
    }

    static double Clamp(double value, double minimum, double maximum)
    {
        if (value < minimum) { return minimum; }
        if (value > maximum) { return maximum; }
        return value;
    }

    static void AddAutomaticCoordinates(
        FontCoordinates coordinates,
        SfntFvarTable fvar,
        FontRequest request)
    {
        FontCoordinates? explicitCoordinates = request.GetCoordinates();
        FontStyle requestedStyle = GetRequestedStyle(request);
        bool hasItalicAxis = fvar.FindAxis(ItalicTag) != null;
        bool hasSlantAxis = fvar.FindAxis(SlantTag) != null;
        int index = 0;

        while (index < fvar.GetAxisCount())
        {
            SfntFvarAxis axis = fvar.GetAxis(index);
            int tag = axis.GetTag();

            System.ArgumentNullException.ThrowIfNull(explicitCoordinates);
            if (!explicitCoordinates.Contains(tag))
            {
                if (tag == WeightTag)
                {
                    coordinates.Set(tag, request.GetWeight());
                }
                else if (tag == WidthTag)
                {
                    coordinates.Set(tag, request.GetStretch());
                }
                else if (tag == OpticalSizeTag && request.HasOpticalSize())
                {
                    coordinates.Set(tag, request.GetOpticalSize());
                }
                else if (tag == ItalicTag)
                {
                    bool useItalic = requestedStyle == FontStyle.Italic ||
                        requestedStyle == FontStyle.Oblique && !hasSlantAxis;
                    coordinates.Set(tag, useItalic ? SelectItalicValue(axis) : 0);
                }
                else if (tag == SlantTag)
                {
                    bool useSlant = requestedStyle == FontStyle.Oblique ||
                        requestedStyle == FontStyle.Italic && !hasItalicAxis;
                    coordinates.Set(tag, useSlant ? SelectSlantValue(axis) : 0);
                }
            }

            index += 1;
        }
    }

    static double SelectSlantValue(SfntFvarAxis axis)
    {
        if (axis.GetMinimumValue() <= -12 && axis.GetMaximumValue() >= -12)
        {
            return -12;
        }

        if (axis.GetMinimumValue() < 0)
        {
            return axis.GetMinimumValue();
        }

        if (axis.GetMaximumValue() >= 12)
        {
            return 12;
        }

        return axis.GetMaximumValue();
    }

    static double SelectItalicValue(SfntFvarAxis axis)
    {
        if (axis.GetMinimumValue() <= 1 && axis.GetMaximumValue() >= 1)
        {
            return 1;
        }

        if (axis.GetMaximumValue() > 0)
        {
            return axis.GetMaximumValue();
        }

        return axis.GetMinimumValue();
    }
}

internal static class FontMatcher
{
    internal static FontMatch? Match(FontFamily[]? families, FontRequest request)
    {
        string familyName = request.GetNormalizedFamilyName();
        int familyIndex = 0;

        System.ArgumentNullException.ThrowIfNull(families);
        while (familyIndex < families.Length)
        {
            FontFamily family = families[familyIndex];

            if (family.MatchesName(familyName))
            {
                FontMatch? match = MatchFamily(family, request);

                if (match != null)
                {
                    return match;
                }
            }

            familyIndex += 1;
        }

        return null;
    }

    static FontMatch? MatchFamily(FontFamily family, FontRequest request)
    {
        FontMatchCandidate? best = null;
        int index = 0;

        while (index < family.GetFaceCount())
        {
            FontFace face = family.GetFace(index);

            if (FontMatchCandidate.SupportsExplicitCoordinates(face, request))
            {
                FontMatchCandidate candidate = new FontMatchCandidate(
                    face,
                    index,
                    request);

                if (candidate.IsBetterThan(best, request))
                {
                    best = candidate;
                }
            }

            index += 1;
        }

        if (best == null)
        {
            return null;
        }

        FontFace selected = best.GetFace();
        FontCoordinates coordinates = FontMatchCandidate.CreateCoordinates(
            selected,
            request);
        return new FontMatch(family, selected.CreateInstance(coordinates));
    }
}
