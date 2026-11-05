using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides enhanced stereo group data and operations for molecular graphs.
/// </summary>
public class EnhancedStereoGroup
{
    /// <summary>
    /// The zero-based identifier within the containing collection.
    /// </summary>
    public int Id;
    /// <summary>
    /// The type.
    /// </summary>
    public string Type;
    /// <summary>
    /// The stereo record identifiers.
    /// </summary>
    public List<int> StereoRecordIds;
    /// <summary>
    /// The source.
    /// </summary>
    public string Source;
    /// <summary>
    /// The label.
    /// </summary>
    public string Label;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnhancedStereoGroup"/> class.
    /// </summary>
    public EnhancedStereoGroup()
    {
        Id = -1;
        Type = "";
        StereoRecordIds = new List<int>();
        Source = "";
        Label = "";
    }

    /// <summary>
    /// Creates an enhanced stereo group with the supplied identity.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="type">The type.</param>
    /// <returns>A group with initialized identity and collection fields.</returns>
    internal static EnhancedStereoGroup Create(int id, string? type)
    {
        EnhancedStereoGroup group = new EnhancedStereoGroup();
        group.Id = id;
        if (type == null)
        {
            group.Type = "";
        }
        else
        {
            group.Type = type;
        }

        return group;
    }

    /// <summary>
    /// Determines whether the value is absolute.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsAbsolute()
    {
        return Type == "abs";
    }

    /// <summary>
    /// Determines whether the value is or.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsOr()
    {
        return Type == "or";
    }

    /// <summary>
    /// Determines whether the value is and.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsAnd()
    {
        return Type == "and";
    }
}

