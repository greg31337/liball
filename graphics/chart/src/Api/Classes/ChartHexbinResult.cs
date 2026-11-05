namespace chart;
/// <summary>
/// Represents a hexbin result used by the chart model or rendering pipeline.
/// </summary>
public class ChartHexbinResult : IChartHexbinResult
{
    internal ChartHexbinCell[] Cells;
    internal int CellCount;
    internal double Radius;
    /// <summary>
    /// Initializes a hexbin result from the supplied radius.
    /// </summary>
    /// <param name = "radius">The radius in chart units, which must be positive.</param>
    public ChartHexbinResult(double radius)
    {
        ChartRenderStyle.ValidatePositive(radius, "radius");
        Cells = new ChartHexbinCell[8];
        CellCount = 0;
        Radius = radius;
    }

    /// <summary>
    /// Adds the supplied or increment to this object.
    /// </summary>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <returns>The resulting hexbin cell.</returns>
    public ChartHexbinCell AddOrIncrement(double centerX, double centerY)
    {
        int index = 0;
        while (index < CellCount)
        {
            if (Cells[index].CenterX == centerX && Cells[index].CenterY == centerY)
            {
                Cells[index].Count = Cells[index].Count + 1;
                return Cells[index];
            }

            index = index + 1;
        }

        EnsureCapacity(CellCount + 1);
        Cells[CellCount] = new ChartHexbinCell(centerX, centerY);
        Cells[CellCount].Count = 1;
        CellCount = CellCount + 1;
        return Cells[CellCount - 1];
    }

    /// <summary>
    /// Returns the requested cell.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting hexbin cell.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartHexbinCell CellAt(int index)
    {
        if (index < 0 || index >= CellCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Cells[index];
    }

    internal void EnsureCapacity(int capacity)
    {
        if (capacity <= Cells.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Cells.Length, capacity);
        ChartHexbinCell[] cells = new ChartHexbinCell[newCapacity];
        System.Array.Copy(Cells, cells, CellCount);
        Cells = cells;
    }

    /// <summary>Returns the current cells.</summary>
    /// <returns>The current field value.</returns>
    public ChartHexbinCell[] GetCells()
    {
        return Cells;
    }

    /// <summary>Replaces the current cells.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCells(ChartHexbinCell[] value)
    {
        Cells = value;
    }

    /// <summary>Returns the current cell count.</summary>
    /// <returns>The current field value.</returns>
    public int GetCellCount()
    {
        return CellCount;
    }

    /// <summary>Replaces the current cell count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCellCount(int value)
    {
        CellCount = value;
    }

    /// <summary>Returns the current radius.</summary>
    /// <returns>The current field value.</returns>
    public double GetRadius()
    {
        return Radius;
    }

    /// <summary>Replaces the current radius.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRadius(double value)
    {
        Radius = value;
    }
}
