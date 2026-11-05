namespace ttf;

/// <summary>Defines deterministic wrapping, alignment, trimming, and ellipsis choices.</summary>
public class TextLayoutOptions : ITextLayoutOptions
{
    TextAlignment alignment;
    int maximumLines;
    bool trimLineWhitespace;
    bool allowEmergencyBreaks;
    double tabWidth;
    TextTrimmingMode trimmingMode;
    string ellipsisText;
    HyphenationDictionary? hyphenation;
    bool justifyLastLine;
    double maximumSpaceExpansion;
    double maximumTrackingExpansion;
    bool useJstfJustification;
    bool useKashidaJustification;
    TextJustificationOpportunity[] justificationOpportunities;
    TextWritingMode writingMode;
    FontOperationOptions operationOptions;

    /// <summary>Creates start-aligned options with unlimited lines and no ellipsis.</summary>
    public TextLayoutOptions()
    {
        alignment = TextAlignment.Start;
        maximumLines = 0;
        trimLineWhitespace = true;
        allowEmergencyBreaks = false;
        tabWidth = 48;
        trimmingMode = TextTrimmingMode.None;
        ellipsisText = "\u2026";
        justifyLastLine = false;
        maximumSpaceExpansion = double.MaxValue;
        maximumTrackingExpansion = 0;
        useJstfJustification = false;
        useKashidaJustification = false;
        justificationOpportunities = new TextJustificationOpportunity[0];
        writingMode = TextWritingMode.Horizontal;
        operationOptions = new FontOperationOptions();
    }

    /// <summary>Returns the inline alignment.</summary>
    /// <returns>The configured alignment.</returns>
    public TextAlignment GetAlignment()
    {
        return alignment;
    }

    /// <summary>Sets the inline alignment.</summary>
    /// <param name="value">The alignment to use.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    public void SetAlignment(TextAlignment value)
    {
        if (value != TextAlignment.Start &&
            value != TextAlignment.End &&
            value != TextAlignment.Center &&
            value != TextAlignment.Justify)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        alignment = value;
    }

    /// <summary>Returns the maximum line count, where zero means unlimited.</summary>
    /// <returns>The configured maximum.</returns>
    public int GetMaximumLines()
    {
        return maximumLines;
    }

    /// <summary>Sets the maximum line count, where zero means unlimited.</summary>
    /// <param name="value">The nonnegative maximum line count.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is negative.</exception>
    public void SetMaximumLines(int value)
    {
        if (value < 0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        maximumLines = value;
    }

    /// <summary>Reports whether leading and trailing line whitespace is hidden.</summary>
    /// <returns>True when line-edge whitespace is trimmed.</returns>
    public bool ShouldTrimLineWhitespace()
    {
        return trimLineWhitespace;
    }

    /// <summary>Sets whether leading and trailing line whitespace is hidden.</summary>
    /// <param name="value">True to trim line-edge whitespace.</param>
    public void SetTrimLineWhitespace(bool value)
    {
        trimLineWhitespace = value;
    }

    /// <summary>Reports whether emergency cluster breaks may be planned.</summary>
    /// <returns>True when emergency breaking is enabled.</returns>
    public bool AllowsEmergencyBreaks()
    {
        return allowEmergencyBreaks;
    }

    /// <summary>Sets whether emergency cluster breaks may be planned.</summary>
    /// <param name="value">True to enable emergency breaking.</param>
    public void SetAllowEmergencyBreaks(bool value)
    {
        allowEmergencyBreaks = value;
    }

    /// <summary>Returns the fixed advance assigned to each tab.</summary>
    /// <returns>The nonnegative tab width in layout units.</returns>
    public double GetTabWidth()
    {
        return tabWidth;
    }

    /// <summary>Sets the fixed advance assigned to each tab.</summary>
    /// <param name="value">The nonnegative finite tab width in layout units.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    public void SetTabWidth(double value)
    {
        ValidateNonnegativeFinite(value, "value");
        tabWidth = value;
    }

    /// <summary>Returns the overflow trimming mode.</summary>
    /// <returns>The configured mode.</returns>
    public TextTrimmingMode GetTrimmingMode()
    {
        return trimmingMode;
    }

    /// <summary>Sets the overflow trimming mode.</summary>
    /// <param name="value">The trimming mode to use.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    public void SetTrimmingMode(TextTrimmingMode value)
    {
        if (value != TextTrimmingMode.None &&
            value != TextTrimmingMode.CharacterEllipsis &&
            value != TextTrimmingMode.WordEllipsis)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        trimmingMode = value;
    }

    /// <summary>Returns the text shaped when an ellipsis is required.</summary>
    /// <returns>The nonempty ellipsis text.</returns>
    public string GetEllipsisText()
    {
        return ellipsisText;
    }

    /// <summary>Sets the text shaped when an ellipsis is required.</summary>
    /// <param name="value">The nonempty ellipsis text.</param>
    /// <exception cref="System.ArgumentNullException"><c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>value</c> is empty.</exception>
    public void SetEllipsisText(string value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (value.Length == 0)
        {
            throw new System.ArgumentException(
                "Ellipsis text cannot be empty.",
                "value");
        }

        ellipsisText = value;
    }

    /// <summary>Reports whether an optional hyphenation dictionary is configured.</summary>
    /// <returns>True when a dictionary is configured.</returns>
    public bool HasHyphenation()
    {
        return hyphenation != null;
    }

    /// <summary>Returns the configured hyphenation dictionary.</summary>
    /// <returns>The configured dictionary.</returns>
    /// <exception cref="System.InvalidOperationException">No dictionary is configured.</exception>
    public HyphenationDictionary GetHyphenation()
    {
        if (hyphenation == null)
        {
            throw new System.InvalidOperationException(
                "No hyphenation dictionary is configured.");
        }

        return hyphenation;
    }

    /// <summary>Sets the optional caller-supplied hyphenation dictionary.</summary>
    /// <param name="value">The dictionary to use.</param>
    /// <exception cref="System.ArgumentNullException"><c>value</c> is null.</exception>
    public void SetHyphenation(HyphenationDictionary value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        hyphenation = value;
    }

    /// <summary>Clears the optional hyphenation dictionary.</summary>
    public void ClearHyphenation()
    {
        hyphenation = null;
    }

    /// <summary>Reports whether the final line of a paragraph is justified.</summary>
    /// <returns>True when final-line justification is enabled.</returns>
    public bool ShouldJustifyLastLine()
    {
        return justifyLastLine;
    }

    /// <summary>Sets whether the final line of a paragraph is justified.</summary>
    /// <param name="value">True to justify final lines.</param>
    public void SetJustifyLastLine(bool value)
    {
        justifyLastLine = value;
    }

    /// <summary>Returns the maximum added advance assigned to one visible space.</summary>
    /// <returns>The nonnegative maximum in layout units.</returns>
    public double GetMaximumSpaceExpansion()
    {
        return maximumSpaceExpansion;
    }

    /// <summary>Sets the maximum added advance assigned to one visible space.</summary>
    /// <param name="value">The nonnegative finite maximum in layout units.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    public void SetMaximumSpaceExpansion(double value)
    {
        ValidateNonnegativeFinite(value, "value");
        maximumSpaceExpansion = value;
    }

    /// <summary>Returns the maximum tracking added at one eligible cluster boundary.</summary>
    /// <returns>The nonnegative maximum in layout units.</returns>
    public double GetMaximumTrackingExpansion()
    {
        return maximumTrackingExpansion;
    }

    /// <summary>Sets the maximum tracking added at one eligible cluster boundary.</summary>
    /// <param name="value">The nonnegative finite maximum in layout units.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    public void SetMaximumTrackingExpansion(double value)
    {
        ValidateNonnegativeFinite(value, "value");
        maximumTrackingExpansion = value;
    }

    /// <summary>Reports whether supplied JSTF opportunities may be applied.</summary>
    /// <returns>True when JSTF justification is enabled.</returns>
    public bool UsesJstfJustification()
    {
        return useJstfJustification;
    }

    /// <summary>Sets whether supplied JSTF opportunities may be applied.</summary>
    /// <param name="value">True to enable JSTF opportunities.</param>
    public void SetUseJstfJustification(bool value)
    {
        useJstfJustification = value;
    }

    /// <summary>Reports whether supplied kashida opportunities may be applied.</summary>
    /// <returns>True when kashida justification is enabled.</returns>
    public bool UsesKashidaJustification()
    {
        return useKashidaJustification;
    }

    /// <summary>Sets whether supplied kashida opportunities may be applied.</summary>
    /// <param name="value">True to enable kashida opportunities.</param>
    public void SetUseKashidaJustification(bool value)
    {
        useKashidaJustification = value;
    }

    /// <summary>Returns the caller-supplied advanced opportunity count.</summary>
    /// <returns>The opportunity count.</returns>
    public int GetJustificationOpportunityCount()
    {
        return justificationOpportunities.Length;
    }

    /// <summary>Returns one caller-supplied advanced opportunity.</summary>
    /// <param name="index">The zero-based opportunity index.</param>
    /// <returns>The immutable opportunity.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public TextJustificationOpportunity GetJustificationOpportunity(int index)
    {
        if (index < 0 || index >= justificationOpportunities.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return justificationOpportunities[index];
    }

    /// <summary>Replaces advanced JSTF and kashida opportunities with an owned copy.</summary>
    /// <param name="values">The opportunities to snapshot.</param>
    /// <exception cref="System.ArgumentNullException"><c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">An opportunity is null.</exception>
    public void SetJustificationOpportunities(
        TextJustificationOpportunity[] values)
    {
        justificationOpportunities = CopyOpportunities(values);
    }

    /// <summary>Removes all caller-supplied advanced opportunities.</summary>
    public void ClearJustificationOpportunities()
    {
        justificationOpportunities = new TextJustificationOpportunity[0];
    }

    /// <summary>Returns the line writing mode.</summary>
    /// <returns>The configured writing mode.</returns>
    public TextWritingMode GetWritingMode()
    {
        return writingMode;
    }

    /// <summary>Sets the line writing mode.</summary>
    /// <param name="value">The writing mode to use.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is invalid.</exception>
    public void SetWritingMode(TextWritingMode value)
    {
        if (value != TextWritingMode.Horizontal &&
            value != TextWritingMode.VerticalTopToBottom &&
            value != TextWritingMode.VerticalBottomToTop)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        writingMode = value;
    }

    /// <summary>Gets shared policy, limits, and cancellation controls.</summary>
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

    internal FontOperationOptions GetOperationSettings()
    {
        return operationOptions;
    }

    internal TextLayoutOptions Copy()
    {
        TextLayoutOptions copy = new TextLayoutOptions();
        copy.alignment = alignment;
        copy.maximumLines = maximumLines;
        copy.trimLineWhitespace = trimLineWhitespace;
        copy.allowEmergencyBreaks = allowEmergencyBreaks;
        copy.tabWidth = tabWidth;
        copy.trimmingMode = trimmingMode;
        copy.ellipsisText = ellipsisText;
        copy.hyphenation = hyphenation;
        copy.justifyLastLine = justifyLastLine;
        copy.maximumSpaceExpansion = maximumSpaceExpansion;
        copy.maximumTrackingExpansion = maximumTrackingExpansion;
        copy.useJstfJustification = useJstfJustification;
        copy.useKashidaJustification = useKashidaJustification;
        copy.justificationOpportunities =
            CopyOpportunities(justificationOpportunities);
        copy.writingMode = writingMode;
        copy.operationOptions = operationOptions.Copy();
        return copy;
    }

    internal TextJustificationOpportunity[] CopyJustificationOpportunities()
    {
        return CopyOpportunities(justificationOpportunities);
    }

    static TextJustificationOpportunity[] CopyOpportunities(
        TextJustificationOpportunity[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }
        TextJustificationOpportunity[] copy =
            new TextJustificationOpportunity[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new System.ArgumentException(
                    "A justification opportunity cannot be null.",
                    "values");
            }
            copy[index] = values[index];
            index += 1;
        }
        return copy;
    }

    static void ValidateNonnegativeFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}
