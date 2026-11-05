namespace ttf;

/// <summary>Controls Unicode processing and OpenType shaping for one operation.</summary>
public class TextShapingOptions : ITextShapingOptions
{
    Utf16DecodingMode decodingMode;
    UnicodeNormalizationMode normalizationMode;
    TextShapingDirection direction;
    BidiParagraphDirection paragraphDirection;
    string script;
    string languageTag;
    FontLayoutFeatureSetting[] features;
    TextShapingClusterPolicy clusterPolicy;
    TextShapingVerticalBehavior verticalBehavior;
    int horizontalPixelsPerEm;
    int verticalPixelsPerEm;
    TextShapingLimits limits;
    FontOperationOptions operationOptions;

    /// <summary>Creates strict, NFC-normalizing, automatically directed options.</summary>
    public TextShapingOptions()
    {
        decodingMode = Utf16DecodingMode.Strict;
        normalizationMode = UnicodeNormalizationMode.Nfc;
        direction = TextShapingDirection.Auto;
        paragraphDirection = BidiParagraphDirection.Auto;
        script = "";
        languageTag = "und";
        features = new FontLayoutFeatureSetting[0];
        clusterPolicy = TextShapingClusterPolicy.GraphemeClusters;
        verticalBehavior = TextShapingVerticalBehavior.Automatic;
        limits = new TextShapingLimits();
        operationOptions = new FontOperationOptions();
    }

    /// <summary>Gets malformed UTF-16 handling.</summary>
    /// <returns>The decoding mode.</returns>
    public Utf16DecodingMode GetDecodingMode() { return decodingMode; }

    /// <summary>Sets malformed UTF-16 handling.</summary>
    /// <param name="value">The decoding mode.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    public void SetDecodingMode(Utf16DecodingMode value)
    {
        ValidateDecodingMode(value, "value");
        decodingMode = value;
    }

    /// <summary>Gets normalization behavior.</summary>
    /// <returns>The normalization mode.</returns>
    public UnicodeNormalizationMode GetNormalizationMode()
    {
        return normalizationMode;
    }

    /// <summary>Sets normalization behavior.</summary>
    /// <param name="value">The normalization mode.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    public void SetNormalizationMode(UnicodeNormalizationMode value)
    {
        ValidateNormalizationMode(value, "value");
        normalizationMode = value;
    }

    /// <summary>Gets the requested shaping direction.</summary>
    /// <returns>The direction.</returns>
    public TextShapingDirection GetDirection() { return direction; }

    /// <summary>Sets the requested shaping direction.</summary>
    /// <param name="value">The direction.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    public void SetDirection(TextShapingDirection value)
    {
        ValidateDirection(value, "value");
        direction = value;
    }

    /// <summary>Reports whether an ISO 15924 script override exists.</summary>
    /// <returns>True when a script override is configured.</returns>
    public bool HasScript() { return script.Length != 0; }

    /// <summary>Gets the ISO 15924 script override.</summary>
    /// <returns>The canonical four-letter script code, or an empty string.</returns>
    public string GetScript() { return script; }

    /// <summary>Sets an ISO 15924 script override.</summary>
    /// <param name="value">A four-letter script code.</param>
    /// <exception cref="System.ArgumentNullException"><c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>value</c> is not four ASCII letters.</exception>
    public void SetScript(string value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }
        if (value.Length != 4 || !IsAsciiLetter(value[0]) ||
            !IsAsciiLetter(value[1]) || !IsAsciiLetter(value[2]) ||
            !IsAsciiLetter(value[3]))
        {
            throw new System.ArgumentException(
                "A shaping script must contain four ASCII letters.",
                "value");
        }
        char[] characters = value.ToLowerInvariant().ToCharArray();
        characters[0] = char.ToUpperInvariant(characters[0]);
        script = new string(characters);
    }

    /// <summary>Restores automatic Unicode script itemization.</summary>
    public void ClearScript() { script = ""; }

    /// <summary>Gets the canonical BCP 47 language tag.</summary>
    /// <returns>The language tag.</returns>
    public string GetLanguageTag() { return languageTag; }

    /// <summary>Sets the BCP 47 language tag.</summary>
    /// <param name="value">A well-formed language tag.</param>
    /// <exception cref="System.ArgumentNullException"><c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>value</c> is malformed.</exception>
    public void SetLanguageTag(string value)
    {
        Bcp47LanguageTag parsed = Bcp47LanguageTagParser.Parse(value);
        languageTag = parsed.GetCanonicalTag();
    }

    /// <summary>Gets the caller feature count.</summary>
    /// <returns>The feature count.</returns>
    public int GetFeatureCount() { return features.Length; }

    /// <summary>Gets one caller feature.</summary>
    /// <param name="index">The feature index.</param>
    /// <returns>The immutable feature setting.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public FontLayoutFeatureSetting GetFeature(int index)
    {
        if (index < 0 || index >= features.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return features[index];
    }

    /// <summary>Replaces caller features with an owned copy.</summary>
    /// <param name="values">The feature settings.</param>
    /// <exception cref="System.ArgumentNullException"><c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">A feature is null.</exception>
    public void SetFeatures(FontLayoutFeatureSetting[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }
        FontLayoutFeatureSetting[] copy =
            new FontLayoutFeatureSetting[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new System.ArgumentException(
                    "A shaping feature cannot be null.",
                    "values");
            }
            copy[index] = values[index];
            index += 1;
        }
        features = copy;
    }

    /// <summary>Removes every caller feature.</summary>
    public void ClearFeatures()
    {
        features = new FontLayoutFeatureSetting[0];
    }

    /// <summary>Gets source cluster preservation behavior.</summary>
    /// <returns>The cluster policy.</returns>
    public TextShapingClusterPolicy GetClusterPolicy() { return clusterPolicy; }

    /// <summary>Sets source cluster preservation behavior.</summary>
    /// <param name="value">The cluster policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    public void SetClusterPolicy(TextShapingClusterPolicy value)
    {
        if (value != TextShapingClusterPolicy.GraphemeClusters &&
            value != TextShapingClusterPolicy.UnicodeScalars)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
        clusterPolicy = value;
    }

    /// <summary>Gets vertical glyph behavior.</summary>
    /// <returns>The vertical behavior.</returns>
    public TextShapingVerticalBehavior GetVerticalBehavior()
    {
        return verticalBehavior;
    }

    /// <summary>Sets vertical glyph behavior.</summary>
    /// <param name="value">The vertical behavior.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    public void SetVerticalBehavior(TextShapingVerticalBehavior value)
    {
        if (value < TextShapingVerticalBehavior.Automatic ||
            value > TextShapingVerticalBehavior.Sideways)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
        verticalBehavior = value;
    }

    /// <summary>Gets horizontal device resolution in pixels per em.</summary>
    /// <returns>The resolution, or zero.</returns>
    public int GetHorizontalPixelsPerEm() { return horizontalPixelsPerEm; }

    /// <summary>Sets horizontal device resolution in pixels per em.</summary>
    /// <param name="value">A value from zero through 65535.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    public void SetHorizontalPixelsPerEm(int value)
    {
        ValidatePixelsPerEm(value, "value");
        horizontalPixelsPerEm = value;
    }

    /// <summary>Gets vertical device resolution in pixels per em.</summary>
    /// <returns>The resolution, or zero.</returns>
    public int GetVerticalPixelsPerEm() { return verticalPixelsPerEm; }

    /// <summary>Sets vertical device resolution in pixels per em.</summary>
    /// <param name="value">A value from zero through 65535.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    public void SetVerticalPixelsPerEm(int value)
    {
        ValidatePixelsPerEm(value, "value");
        verticalPixelsPerEm = value;
    }

    /// <summary>Gets an independent copy of the shaping limits.</summary>
    /// <returns>The finite operation limits.</returns>
    public TextShapingLimits GetLimits() { return limits.Copy(); }

    /// <summary>Sets shaping limits from an owned copy.</summary>
    /// <param name="value">The finite operation limits.</param>
    /// <exception cref="System.ArgumentNullException"><c>value</c> is null.</exception>
    public void SetLimits(TextShapingLimits value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }
        limits = value.Copy();
    }

    /// <summary>Gets shared policy, general limits, and cancellation controls.</summary>
    /// <returns>An independent mutable copy.</returns>
    public FontOperationOptions GetOperationOptions()
    {
        return operationOptions.Copy();
    }

    /// <summary>Sets shared operation controls from an owned snapshot.</summary>
    /// <param name="value">The operation controls.</param>
    /// <exception cref="System.ArgumentNullException"><c>value</c> is null.</exception>
    public void SetOperationOptions(FontOperationOptions value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        operationOptions = value.Copy();
    }

    internal static TextShapingOptions Create()
    {
        return new TextShapingOptions();
    }

    internal static TextShapingOptions CreateConfigured(
        Utf16DecodingMode decodingMode,
        UnicodeNormalizationMode normalizationMode,
        TextShapingDirection direction,
        BidiParagraphDirection paragraphDirection,
        string languageTag,
        SfntUserFeature[] features,
        int horizontalPixelsPerEm,
        int verticalPixelsPerEm)
    {
        TextShapingOptions options = new TextShapingOptions();
        options.Initialize(
            decodingMode,
            normalizationMode,
            direction,
            paragraphDirection,
            languageTag,
            features,
            horizontalPixelsPerEm,
            verticalPixelsPerEm);
        return options;
    }

    internal BidiParagraphDirection GetParagraphDirection()
    {
        return paragraphDirection;
    }

    internal SfntUserFeature[] CopyFeatures()
    {
        SfntUserFeature[] copy = new SfntUserFeature[features.Length];
        int index = 0;
        while (index < features.Length)
        {
            copy[index] = features[index].CreateInternal();
            index += 1;
        }
        return copy;
    }

    internal TextShapingLimits GetOperationLimits()
    {
        return limits;
    }

    internal FontOperationOptions GetOperationSettings()
    {
        return operationOptions;
    }

    internal TextShapingOptions Copy()
    {
        TextShapingOptions copy = new TextShapingOptions();
        copy.decodingMode = decodingMode;
        copy.normalizationMode = normalizationMode;
        copy.direction = direction;
        copy.paragraphDirection = paragraphDirection;
        copy.script = script;
        copy.languageTag = languageTag;
        copy.features = new FontLayoutFeatureSetting[features.Length];
        int index = 0;
        while (index < features.Length)
        {
            FontLayoutFeatureSetting feature = features[index];
            copy.features[index] = new FontLayoutFeatureSetting(
                feature.GetTag(),
                feature.GetValue(),
                feature.GetStart(),
                feature.GetEnd());
            index += 1;
        }
        copy.clusterPolicy = clusterPolicy;
        copy.verticalBehavior = verticalBehavior;
        copy.horizontalPixelsPerEm = horizontalPixelsPerEm;
        copy.verticalPixelsPerEm = verticalPixelsPerEm;
        copy.limits = limits.Copy();
        copy.operationOptions = operationOptions.Copy();
        return copy;
    }

    void Initialize(
        Utf16DecodingMode selectedDecodingMode,
        UnicodeNormalizationMode selectedNormalizationMode,
        TextShapingDirection selectedDirection,
        BidiParagraphDirection selectedParagraphDirection,
        string selectedLanguageTag,
        SfntUserFeature[] selectedFeatures,
        int selectedHorizontalPixelsPerEm,
        int selectedVerticalPixelsPerEm)
    {
        SetDecodingMode(selectedDecodingMode);
        SetNormalizationMode(selectedNormalizationMode);
        SetDirection(selectedDirection);
        ValidateParagraphDirection(selectedParagraphDirection);
        paragraphDirection = selectedParagraphDirection;
        SetLanguageTag(selectedLanguageTag);
        if (selectedFeatures == null)
        {
            throw new System.ArgumentNullException("selectedFeatures");
        }
        FontLayoutFeatureSetting[] publicFeatures =
            new FontLayoutFeatureSetting[selectedFeatures.Length];
        int index = 0;
        while (index < selectedFeatures.Length)
        {
            SfntUserFeature feature = selectedFeatures[index];
            if (feature == null)
            {
                throw new System.ArgumentException(
                    "A shaping feature cannot be null.",
                    "selectedFeatures");
            }
            publicFeatures[index] = new FontLayoutFeatureSetting(
                feature.GetFeatureTag(),
                feature.GetValue(),
                feature.GetStart(),
                feature.GetEnd());
            index += 1;
        }
        SetFeatures(publicFeatures);
        SetHorizontalPixelsPerEm(selectedHorizontalPixelsPerEm);
        SetVerticalPixelsPerEm(selectedVerticalPixelsPerEm);
    }

    static bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    static void ValidateDecodingMode(
        Utf16DecodingMode value,
        string parameterName)
    {
        if (value != Utf16DecodingMode.Strict &&
            value != Utf16DecodingMode.Replacement)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateNormalizationMode(
        UnicodeNormalizationMode value,
        string parameterName)
    {
        if (value < UnicodeNormalizationMode.None ||
            value > UnicodeNormalizationMode.Nfkd)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateDirection(
        TextShapingDirection value,
        string parameterName)
    {
        if (value < TextShapingDirection.Auto ||
            value > TextShapingDirection.BottomToTop)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateParagraphDirection(BidiParagraphDirection value)
    {
        if (value < BidiParagraphDirection.Auto ||
            value > BidiParagraphDirection.RightToLeft)
        {
            throw new System.ArgumentOutOfRangeException(
                "selectedParagraphDirection");
        }
    }

    static void ValidatePixelsPerEm(int value, string parameterName)
    {
        if (value < 0 || value > 65535)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}
