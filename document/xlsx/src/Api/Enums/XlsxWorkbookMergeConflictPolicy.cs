using System;

namespace xlsx;

/// <summary>
/// Selects how workbook merge operations handle incoming name conflicts.
/// </summary>
public enum XlsxWorkbookMergeConflictPolicy
{
    /// <summary>Reports an error and performs no merge when a conflict is detected.</summary>
    Fail = 0,
    /// <summary>Generates a deterministic unique name for the incoming object.</summary>
    RenameIncoming = 1
}
