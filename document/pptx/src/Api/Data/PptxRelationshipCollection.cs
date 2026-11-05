using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Represents relationship collection data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxRelationshipCollection
{
    /// <summary>
    /// Stores the source part name associated with this object.
    /// </summary>
    public string SourcePartName;
    /// <summary>
    /// Contains the relationships in package or presentation order.
    /// </summary>
    public List<PptxRelationship> Relationships;
    /// <summary>
    /// Stores the diagnostics value associated with this object.
    /// </summary>
    public PptxResult Diagnostics;

    /// <summary>
    /// Initializes a new relationship collection with safe defaults or the supplied values.
    /// </summary>
    /// <param name="sourcePartName">The source part name value to use.</param>
    public PptxRelationshipCollection(string sourcePartName)
    {
        SourcePartName = PptxDiagnostic.CleanText(sourcePartName);
        Relationships = new List<PptxRelationship>();
        Diagnostics = new PptxResult();
    }
    internal bool Succeeded()
    {
        return Diagnostics.Succeeded();
    }
    internal bool Failed()
    {
        return Diagnostics.Failed();
    }
    internal int Count()
    {
        return Relationships.Count;
    }
    internal void Add(PptxRelationship relationship)
    {
        if (relationship == null)
        {
            throw new System.ArgumentNullException("relationship");
        }

        Relationships.Add(relationship);
    }
    internal bool ContainsId(string id)
    {
        for (int index = 0; index < Relationships.Count; index++)
        {
            if (Relationships[index].Id == id)
            {
                return true;
            }
        }

        return false;
    }
    internal int IndexOfId(string id)
    {
        for (int index = 0; index < Relationships.Count; index++)
        {
            if (Relationships[index].Id == id)
            {
                return index;
            }
        }

        return -1;
    }
}

