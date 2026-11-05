using System.Collections.Generic;
using System.IO;

namespace molecule;


/// <summary>
/// Provides duplicate group data and operations for molecular graphs.
/// </summary>
public class DuplicateGroup
{
    /// <summary>
    /// The key.
    /// </summary>
    public string Key;
    /// <summary>
    /// The zero-based index of the first input with this identity key, or <c>-1</c> when absent.
    /// </summary>
    public int FirstIndex;
    /// <summary>
    /// The duplicate indices.
    /// </summary>
    public List<int> DuplicateIndices;

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateGroup"/> class.
    /// </summary>
    public DuplicateGroup()
    {
        Key = "";
        FirstIndex = -1;
        DuplicateIndices = new List<int>();
    }
}

