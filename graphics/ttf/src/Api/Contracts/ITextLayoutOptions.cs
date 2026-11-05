namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="TextLayoutOptions"/>.</summary>
public interface ITextLayoutOptions
{
    /// <summary>Returns the inline alignment.</summary>
    /// <returns>The configured alignment.</returns>
    TextAlignment GetAlignment();

    /// <summary>Sets the inline alignment.</summary>
    /// <param name="value">The alignment to use.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    void SetAlignment(TextAlignment value);

    /// <summary>Returns the maximum line count, where zero means unlimited.</summary>
    /// <returns>The configured maximum.</returns>
    int GetMaximumLines();

    /// <summary>Sets the maximum line count, where zero means unlimited.</summary>
    /// <param name="value">The nonnegative maximum line count.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is negative.</exception>
    void SetMaximumLines(int value);

    /// <summary>Reports whether leading and trailing line whitespace is hidden.</summary>
    /// <returns>True when line-edge whitespace is trimmed.</returns>
    bool ShouldTrimLineWhitespace();

    /// <summary>Sets whether leading and trailing line whitespace is hidden.</summary>
    /// <param name="value">True to trim line-edge whitespace.</param>
    void SetTrimLineWhitespace(bool value);

    /// <summary>Reports whether emergency cluster breaks may be planned.</summary>
    /// <returns>True when emergency breaking is enabled.</returns>
    bool AllowsEmergencyBreaks();

    /// <summary>Sets whether emergency cluster breaks may be planned.</summary>
    /// <param name="value">True to enable emergency breaking.</param>
    void SetAllowEmergencyBreaks(bool value);

    /// <summary>Returns the fixed advance assigned to each tab.</summary>
    /// <returns>The nonnegative tab width in layout units.</returns>
    double GetTabWidth();

    /// <summary>Sets the fixed advance assigned to each tab.</summary>
    /// <param name="value">The nonnegative finite tab width in layout units.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    void SetTabWidth(double value);

    /// <summary>Returns the overflow trimming mode.</summary>
    /// <returns>The configured mode.</returns>
    TextTrimmingMode GetTrimmingMode();

    /// <summary>Sets the overflow trimming mode.</summary>
    /// <param name="value">The trimming mode to use.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    void SetTrimmingMode(TextTrimmingMode value);

    /// <summary>Returns the text shaped when an ellipsis is required.</summary>
    /// <returns>The nonempty ellipsis text.</returns>
    string GetEllipsisText();

    /// <summary>Sets the text shaped when an ellipsis is required.</summary>
    /// <param name="value">The nonempty ellipsis text.</param>
    /// <exception cref="System.ArgumentNullException"><c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>value</c> is empty.</exception>
    void SetEllipsisText(string value);

    /// <summary>Reports whether an optional hyphenation dictionary is configured.</summary>
    /// <returns>True when a dictionary is configured.</returns>
    bool HasHyphenation();

    /// <summary>Returns the configured hyphenation dictionary.</summary>
    /// <returns>The configured dictionary.</returns>
    /// <exception cref="System.InvalidOperationException">No dictionary is configured.</exception>
    HyphenationDictionary GetHyphenation();

    /// <summary>Sets the optional caller-supplied hyphenation dictionary.</summary>
    /// <param name="value">The dictionary to use.</param>
    /// <exception cref="System.ArgumentNullException"><c>value</c> is null.</exception>
    void SetHyphenation(HyphenationDictionary value);

    /// <summary>Clears the optional hyphenation dictionary.</summary>
    void ClearHyphenation();

    /// <summary>Reports whether the final line of a paragraph is justified.</summary>
    /// <returns>True when final-line justification is enabled.</returns>
    bool ShouldJustifyLastLine();

    /// <summary>Sets whether the final line of a paragraph is justified.</summary>
    /// <param name="value">True to justify final lines.</param>
    void SetJustifyLastLine(bool value);

    /// <summary>Returns the maximum added advance assigned to one visible space.</summary>
    /// <returns>The nonnegative maximum in layout units.</returns>
    double GetMaximumSpaceExpansion();

    /// <summary>Sets the maximum added advance assigned to one visible space.</summary>
    /// <param name="value">The nonnegative finite maximum in layout units.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    void SetMaximumSpaceExpansion(double value);

    /// <summary>Returns the maximum tracking added at one eligible cluster boundary.</summary>
    /// <returns>The nonnegative maximum in layout units.</returns>
    double GetMaximumTrackingExpansion();

    /// <summary>Sets the maximum tracking added at one eligible cluster boundary.</summary>
    /// <param name="value">The nonnegative finite maximum in layout units.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    void SetMaximumTrackingExpansion(double value);

    /// <summary>Reports whether supplied JSTF opportunities may be applied.</summary>
    /// <returns>True when JSTF justification is enabled.</returns>
    bool UsesJstfJustification();

    /// <summary>Sets whether supplied JSTF opportunities may be applied.</summary>
    /// <param name="value">True to enable JSTF opportunities.</param>
    void SetUseJstfJustification(bool value);

    /// <summary>Reports whether supplied kashida opportunities may be applied.</summary>
    /// <returns>True when kashida justification is enabled.</returns>
    bool UsesKashidaJustification();

    /// <summary>Sets whether supplied kashida opportunities may be applied.</summary>
    /// <param name="value">True to enable kashida opportunities.</param>
    void SetUseKashidaJustification(bool value);

    /// <summary>Returns the caller-supplied advanced opportunity count.</summary>
    /// <returns>The opportunity count.</returns>
    int GetJustificationOpportunityCount();

    /// <summary>Returns one caller-supplied advanced opportunity.</summary>
    /// <param name="index">The zero-based opportunity index.</param>
    /// <returns>The immutable opportunity.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    TextJustificationOpportunity GetJustificationOpportunity(int index);

    /// <summary>Replaces advanced JSTF and kashida opportunities with an owned copy.</summary>
    /// <param name="values">The opportunities to snapshot.</param>
    /// <exception cref="System.ArgumentNullException"><c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">An opportunity is null.</exception>
    void SetJustificationOpportunities(TextJustificationOpportunity[] values);

    /// <summary>Removes all caller-supplied advanced opportunities.</summary>
    void ClearJustificationOpportunities();

    /// <summary>Returns the line writing mode.</summary>
    /// <returns>The configured writing mode.</returns>
    TextWritingMode GetWritingMode();

    /// <summary>Sets the line writing mode.</summary>
    /// <param name="value">The writing mode to use.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    void SetWritingMode(TextWritingMode value);

    /// <summary>Gets shared policy, limits, and cancellation controls.</summary>
    /// <returns>An independent mutable copy.</returns>
    FontOperationOptions GetOperationOptions();

    /// <summary>Sets shared operation controls from an owned snapshot.</summary>
    /// <param name="value">The operation controls.</param>
    void SetOperationOptions(FontOperationOptions value);
}
