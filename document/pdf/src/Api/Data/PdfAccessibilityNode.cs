using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Represents an accessibility node used by the PDF generation API.
/// </summary>
public class PdfAccessibilityNode
{
    /// <summary>
    /// Stores the page used by this object.
    /// </summary>
    public PdfPage Page;
    /// <summary>
    /// Stores the role used by this object.
    /// </summary>
    public string Role;
    /// <summary>
    /// Stores the description used by this object.
    /// </summary>
    public string Description;
    /// <summary>Stores alternate text for non-text structure content.</summary>
    public string AlternateText;
    /// <summary>Stores replacement text for content whose visual representation differs from its meaning.</summary>
    public string ActualText;
    /// <summary>Stores an optional language override for this structure element.</summary>
    public string Language;
    internal PdfAccessibilityNode? Parent;
    internal List<PdfAccessibilityNode> _children;
    internal IReadOnlyList<PdfAccessibilityNode> Children;
    internal List<int> _markedContentIds;
    internal IReadOnlyList<int> MarkedContentIds;
    internal PdfDocument? OwnerDocument;
    internal bool HasTableHeaderScope;
    internal PdfTableHeaderScope TableHeaderScope;

    /// <summary>
    /// Initializes a new accessibility node.
    /// </summary>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="role">The role to use for this operation.</param>
    /// <param name="description">The description to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    public PdfAccessibilityNode(PdfPage page, string role, string description)
    {
        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        if (role == null)
        {
            throw new ArgumentNullException("role");
        }

        if (role.Length == 0)
        {
            throw new ArgumentException("Accessibility role cannot be empty.", "role");
        }

        if (description == null)
        {
            throw new ArgumentNullException("description");
        }

        Page = page;
        Role = role;
        Description = description;
        AlternateText = string.Empty;
        ActualText = string.Empty;
        Language = string.Empty;
        Parent = null;
        _children = new List<PdfAccessibilityNode>();
        Children = _children;
        _markedContentIds = new List<int>();
        MarkedContentIds = _markedContentIds;
        OwnerDocument = null;
        HasTableHeaderScope = false;
        TableHeaderScope = PdfTableHeaderScope.Column;
    }
}
