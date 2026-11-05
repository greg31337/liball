using System.Collections.Generic;

namespace fix;

/// <summary>Represents a structured FIX repeating group.</summary>
public class FixGroup
{
    /// <summary>The tag whose value declares the number of entries.</summary>
    public int CountTag;
    /// <summary>The first field tag required in each non-empty entry.</summary>
    public int DelimiterTag;
    /// <summary>The entry count declared on the wire before synchronization.</summary>
    public int DeclaredCount;
    /// <summary>The count field's zero-based position in the original message.</summary>
    public int WireIndex = -1;
    /// <summary>The independently stored entries in wire order.</summary>
    public List<FixGroupEntry> Entries = [];
}
