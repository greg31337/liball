using System;

namespace xlsx;

/// <summary>
/// Lists supported directions for rectangular cell shifts.
/// </summary>
public enum XlsxCellShiftDirection
{
    /// <summary>
    /// Inserts cells and shifts existing cells down.
    /// </summary>
    Down = 1,
    /// <summary>
    /// Deletes cells and shifts existing cells up.
    /// </summary>
    Up = 2,
    /// <summary>
    /// Inserts cells and shifts existing cells right.
    /// </summary>
    Right = 3,
    /// <summary>
    /// Deletes cells and shifts existing cells left.
    /// </summary>
    Left = 4
}
