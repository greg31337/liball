using System;

namespace markdown;

/// <summary>
/// Contains the normalized URI classification and safety decisions for a destination.
/// </summary>
public class MarkdownUriClassification
{
    /// <summary>
    /// Stores the original text value used by this instance.
    /// </summary>
    public string OriginalText;
    /// <summary>
    /// Stores the normalized text value used by this instance.
    /// </summary>
    public string NormalizedText;
    /// <summary>
    /// Stores the scheme value used by this instance.
    /// </summary>
    public string Scheme;
    /// <summary>
    /// Stores the kind value used by this instance.
    /// </summary>
    public MarkdownUriKind Kind;
    /// <summary>
    /// Stores the safe for link value used by this instance.
    /// </summary>
    public bool SafeForLink;
    /// <summary>
    /// Stores the safe for image value used by this instance.
    /// </summary>
    public bool SafeForImage;

    /// <summary>
    /// Initializes a new <see cref="MarkdownUriClassification"/> instance.
    /// </summary>
    public MarkdownUriClassification()
    {
        OriginalText = "";
        NormalizedText = "";
        Scheme = "";
        Kind = MarkdownUriKind.Empty;
        SafeForLink = true;
        SafeForImage = true;
    }

    /// <summary>
    /// Creates a URI classification with explicit values.
    /// </summary>
    /// <param name="originalText">The original text value.</param>
    /// <param name="normalizedText">The normalized text value.</param>
    /// <param name="scheme">The scheme value.</param>
    /// <param name="kind">The node kind to match.</param>
    /// <param name="safeForLink">The safe for link value.</param>
    /// <param name="safeForImage">The safe for image value.</param>
    internal static MarkdownUriClassification Create(string originalText, string normalizedText, string scheme, MarkdownUriKind kind, bool safeForLink, bool safeForImage)
    {
        if (originalText == null)
        {
            throw new ArgumentNullException("originalText");
        }

        if (normalizedText == null)
        {
            throw new ArgumentNullException("normalizedText");
        }

        if (scheme == null)
        {
            throw new ArgumentNullException("scheme");
        }

        MarkdownUriClassification classification = new MarkdownUriClassification();
        classification.OriginalText = originalText;
        classification.NormalizedText = normalizedText;
        classification.Scheme = scheme;
        classification.Kind = kind;
        classification.SafeForLink = safeForLink;
        classification.SafeForImage = safeForImage;
        return classification;
    }
}
