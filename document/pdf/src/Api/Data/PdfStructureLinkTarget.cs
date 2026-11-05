using System;

namespace pdf;

/// <summary>
/// Ties a named navigation destination to an accessibility structure node.
/// </summary>
public class PdfStructureLinkTarget
{
    /// <summary>Stores the caller-defined target name.</summary>
    public string Name;
    /// <summary>Stores the accessibility structure node associated with the destination.</summary>
    public PdfAccessibilityNode StructureNode;
    /// <summary>Stores the named destination used for PDF navigation.</summary>
    public PdfNamedDestination Destination;

    /// <summary>
    /// Creates a navigation target associated with an accessibility structure node.
    /// </summary>
    /// <param name="name">The caller-defined target name.</param>
    /// <param name="structureNode">The associated accessibility structure node.</param>
    /// <param name="destination">The navigation destination.</param>
    public PdfStructureLinkTarget(string name, PdfAccessibilityNode structureNode, PdfNamedDestination destination)
    {
        if (name == null || structureNode == null || destination == null)
        {
            throw new ArgumentNullException("Structure link target values cannot be null.");
        }

        Name = name;
        StructureNode = structureNode;
        Destination = destination;
    }
}
