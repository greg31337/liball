using System;
using System.Collections.Generic;

namespace html;
/// <summary>Identifies a normalized metadata field.</summary>
public enum HtmlMetadataField
{
    /// <summary>Selects the document title field.</summary>
    Title = 1,
    /// <summary>Selects the document base URL field.</summary>
    BaseUrl = 2,
    /// <summary>Selects the canonical document URL field.</summary>
    CanonicalUrl = 3,
    /// <summary>Selects an alternate document URL field.</summary>
    AlternateUrl = 4,
    /// <summary>Selects the document description field.</summary>
    Description = 5,
    /// <summary>Selects the publishing site name field.</summary>
    SiteName = 6,
    /// <summary>Selects the document language field.</summary>
    Language = 7,
    /// <summary>Selects the document text-direction field.</summary>
    Direction = 8,
    /// <summary>Selects the favicon URL field.</summary>
    FaviconUrl = 9,
    /// <summary>Selects the author field.</summary>
    Author = 10,
    /// <summary>Selects the publication-time field.</summary>
    PublishedTime = 11,
    /// <summary>Selects the modification-time field.</summary>
    ModifiedTime = 12,
    /// <summary>Selects the representative image URL field.</summary>
    ImageUrl = 13
}
