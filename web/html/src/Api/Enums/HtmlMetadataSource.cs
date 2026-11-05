using System;
using System.Collections.Generic;

namespace html;
/// <summary>Identifies the source from which a metadata candidate was extracted.</summary>
public enum HtmlMetadataSource
{
    /// <summary>The candidate came from document-level markup such as <c>title</c> or language attributes.</summary>
    Document = 1,
    /// <summary>The candidate came from an HTML <c>link</c> element.</summary>
    Link = 2,
    /// <summary>The candidate came from a named HTML <c>meta</c> element.</summary>
    MetaName = 3,
    /// <summary>The candidate came from a property-valued HTML <c>meta</c> element.</summary>
    MetaProperty = 4,
    /// <summary>The candidate came from an HTTP-equivalent HTML <c>meta</c> element.</summary>
    HttpEquiv = 5,
    /// <summary>The candidate came from Open Graph metadata.</summary>
    OpenGraph = 6,
    /// <summary>The candidate came from Twitter Card metadata.</summary>
    Twitter = 7,
    /// <summary>The candidate came from Dublin Core metadata.</summary>
    DublinCore = 8,
    /// <summary>The candidate came from a supported JSON-LD object.</summary>
    JsonLd = 9,
    /// <summary>The candidate was inferred from document content rather than explicit metadata.</summary>
    Heuristic = 10
}
