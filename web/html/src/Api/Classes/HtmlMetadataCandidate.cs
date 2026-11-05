using System;
using System.Collections.Generic;

namespace html;
/// <summary>Stores one bounded HTML metadata value together with the information used to rank it.</summary>
public class HtmlMetadataCandidate : IHtmlMetadataCandidate
{
    internal HtmlMetadataField field;
    internal string value;
    internal string? rawValue;
    internal bool rawValueTruncated;
    internal HtmlMetadataSource source;
    internal int precedence;
    internal int documentOrder;
    internal long retentionOrder;
    internal HtmlMetadataCandidate(HtmlMetadataField fieldValue, string valueValue, string? rawValueValue, bool rawValueTruncatedValue, HtmlMetadataSource sourceValue, int precedenceValue, int documentOrderValue, long retentionOrderValue)
    {
        HtmlMetadataValidation.ValidateField(fieldValue, "fieldValue");
        HtmlMetadataValidation.ValidateSource(sourceValue, "sourceValue");
        if (valueValue == null)
        {
            throw new ArgumentNullException("valueValue");
        }

        if (precedenceValue < 0)
        {
            throw new ArgumentOutOfRangeException("precedenceValue");
        }

        if (documentOrderValue < 0)
        {
            throw new ArgumentOutOfRangeException("documentOrderValue");
        }

        this.field = fieldValue;
        this.value = valueValue;
        this.rawValue = rawValueValue;
        this.rawValueTruncated = rawValueTruncatedValue;
        this.source = sourceValue;
        this.precedence = precedenceValue;
        this.documentOrder = documentOrderValue;
        this.retentionOrder = retentionOrderValue;
    }

    /// <summary>Gets which normalized metadata field this candidate can supply.</summary>
    /// <returns>The title, URL, description, author, image, language, time, or related field.</returns>
    public HtmlMetadataField GetField()
    {
        return this.field;
    }

    /// <summary>Gets the normalized candidate value retained by the extractor.</summary>
    /// <returns>The bounded value used during precedence resolution.</returns>
    public string GetValue()
    {
        return this.value;
    }

    /// <summary>
    /// Gets the bounded source value retained for diagnostics.
    /// </summary>
    /// <returns>The retained value. Returns null when no raw value was
    /// supplied.</returns>
    public string? GetRawValue()
    {
        return this.rawValue;
    }

    /// <summary>Reports whether this candidate value was shortened to its retention limit.</summary>
    /// <returns><c>true</c> when the candidate value was shortened; otherwise, <c>false</c>.</returns>
    public bool WasRawValueTruncated()
    {
        return this.rawValueTruncated;
    }

    /// <summary>Gets which markup vocabulary or inference produced this candidate.</summary>
    /// <returns>The HTML, Open Graph, Twitter Card, Dublin Core, JSON-LD, URL, or inferred source.</returns>
    public HtmlMetadataSource GetSource()
    {
        return this.source;
    }

    /// <summary>
    /// Gets the explicit candidate precedence. Lower values are preferred.
    /// </summary>
    /// <returns>The precedence.</returns>
    public int GetPrecedence()
    {
        return this.precedence;
    }

    /// <summary>Gets the stable source-order tie breaker assigned during extraction.</summary>
    /// <returns>The zero-based order in which the candidate was encountered.</returns>
    public int GetDocumentOrder()
    {
        return this.documentOrder;
    }

    internal long GetRetentionOrder()
    {
        return this.retentionOrder;
    }
}
