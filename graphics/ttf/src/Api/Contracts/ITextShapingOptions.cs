namespace ttf;

/// <summary>Defines caller-selected Unicode and OpenType shaping behavior.</summary>
public interface ITextShapingOptions
{
    /// <summary>Gets malformed UTF-16 handling.</summary>
    /// <returns>The decoding mode.</returns>
    Utf16DecodingMode GetDecodingMode();

    /// <summary>Sets malformed UTF-16 handling.</summary>
    /// <param name="value">The decoding mode.</param>
    void SetDecodingMode(Utf16DecodingMode value);

    /// <summary>Gets normalization behavior.</summary>
    /// <returns>The normalization mode.</returns>
    UnicodeNormalizationMode GetNormalizationMode();

    /// <summary>Sets normalization behavior.</summary>
    /// <param name="value">The normalization mode.</param>
    void SetNormalizationMode(UnicodeNormalizationMode value);

    /// <summary>Gets the requested shaping direction.</summary>
    /// <returns>The direction.</returns>
    TextShapingDirection GetDirection();

    /// <summary>Sets the requested shaping direction.</summary>
    /// <param name="value">The direction.</param>
    void SetDirection(TextShapingDirection value);

    /// <summary>Reports whether an ISO 15924 script override exists.</summary>
    /// <returns>True when a script override is configured.</returns>
    bool HasScript();

    /// <summary>Gets the ISO 15924 script override.</summary>
    /// <returns>The canonical four-letter script code.</returns>
    string GetScript();

    /// <summary>Sets an ISO 15924 script override.</summary>
    /// <param name="value">A four-letter script code.</param>
    void SetScript(string value);

    /// <summary>Restores automatic Unicode script itemization.</summary>
    void ClearScript();

    /// <summary>Gets the canonical BCP 47 language tag.</summary>
    /// <returns>The language tag.</returns>
    string GetLanguageTag();

    /// <summary>Sets the BCP 47 language tag.</summary>
    /// <param name="value">A well-formed language tag.</param>
    void SetLanguageTag(string value);

    /// <summary>Gets the caller feature count.</summary>
    /// <returns>The feature count.</returns>
    int GetFeatureCount();

    /// <summary>Gets one caller feature.</summary>
    /// <param name="index">The feature index.</param>
    /// <returns>The immutable feature setting.</returns>
    FontLayoutFeatureSetting GetFeature(int index);

    /// <summary>Replaces caller features with an owned copy.</summary>
    /// <param name="values">The feature settings.</param>
    void SetFeatures(FontLayoutFeatureSetting[] values);

    /// <summary>Removes every caller feature.</summary>
    void ClearFeatures();

    /// <summary>Gets source cluster preservation behavior.</summary>
    /// <returns>The cluster policy.</returns>
    TextShapingClusterPolicy GetClusterPolicy();

    /// <summary>Sets source cluster preservation behavior.</summary>
    /// <param name="value">The cluster policy.</param>
    void SetClusterPolicy(TextShapingClusterPolicy value);

    /// <summary>Gets vertical glyph behavior.</summary>
    /// <returns>The vertical behavior.</returns>
    TextShapingVerticalBehavior GetVerticalBehavior();

    /// <summary>Sets vertical glyph behavior.</summary>
    /// <param name="value">The vertical behavior.</param>
    void SetVerticalBehavior(TextShapingVerticalBehavior value);

    /// <summary>Gets horizontal device resolution in pixels per em.</summary>
    /// <returns>The resolution, or zero to ignore device tables.</returns>
    int GetHorizontalPixelsPerEm();

    /// <summary>Sets horizontal device resolution in pixels per em.</summary>
    /// <param name="value">A value from zero through 65535.</param>
    void SetHorizontalPixelsPerEm(int value);

    /// <summary>Gets vertical device resolution in pixels per em.</summary>
    /// <returns>The resolution, or zero to ignore device tables.</returns>
    int GetVerticalPixelsPerEm();

    /// <summary>Sets vertical device resolution in pixels per em.</summary>
    /// <param name="value">A value from zero through 65535.</param>
    void SetVerticalPixelsPerEm(int value);

    /// <summary>Gets an independent copy of the shaping limits.</summary>
    /// <returns>The finite operation limits.</returns>
    TextShapingLimits GetLimits();

    /// <summary>Sets shaping limits from an owned copy.</summary>
    /// <param name="value">The finite operation limits.</param>
    void SetLimits(TextShapingLimits value);

    /// <summary>Gets shared policy, general limits, and cancellation controls.</summary>
    /// <returns>An independent mutable copy.</returns>
    FontOperationOptions GetOperationOptions();

    /// <summary>Sets shared operation controls from an owned snapshot.</summary>
    /// <param name="value">The operation controls.</param>
    void SetOperationOptions(FontOperationOptions value);
}
