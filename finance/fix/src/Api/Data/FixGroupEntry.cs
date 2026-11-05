using System.Collections.Generic;

namespace fix;

/// <summary>Represents one independently stored entry in a FIX repeating group.</summary>
public class FixGroupEntry
{
    /// <summary>The entry's zero-based position in its group.</summary>
    public int EntryIndex = -1;
    /// <summary>The entry fields in wire order, including duplicate occurrences.</summary>
    public List<FixField> Fields = [];
    /// <summary>The nested repeating groups in wire order.</summary>
    public List<FixGroup> Groups = [];
}
