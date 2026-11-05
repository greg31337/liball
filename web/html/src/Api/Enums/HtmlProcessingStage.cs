using System;
using System.Text;

namespace html;
/// <summary>Identifies one stage in the coordinated HTML processing pipeline.</summary>
public enum HtmlProcessingStage
{
    /// <summary>Identifies the byte-decoding stage.</summary>
    Decode = 1,
    /// <summary>Identifies the HTML parsing stage.</summary>
    Parse = 2,
    /// <summary>Identifies the metadata-extraction stage.</summary>
    Metadata = 3,
    /// <summary>Identifies the article-extraction stage.</summary>
    Article = 4,
    /// <summary>Identifies the sanitization stage.</summary>
    Sanitize = 5,
    /// <summary>Identifies the HTML serialization stage.</summary>
    Serialize = 6,
    /// <summary>Identifies the plain-text conversion stage.</summary>
    Text = 7,
    /// <summary>Identifies the Markdown conversion stage.</summary>
    Markdown = 8
}
