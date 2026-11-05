using System;

namespace xlsx;

/// <summary>Selects how security-sensitive workbook content is handled.</summary>
public enum XlsxSecurityContentPolicy
{
    /// <summary>Retains detected content.</summary>
    Preserve = 0,
    /// <summary>Removes detected content while opening or applying the policy.</summary>
    Remove = 1,
    /// <summary>Rejects a workbook when the content is detected.</summary>
    Reject = 2
}
