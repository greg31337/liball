namespace icc;

/// <summary>Represents a dense row-major matrix.</summary>
public class IccMatrix
{
    /// <summary>Gets or sets the row count.</summary>
    public int Rows;
    /// <summary>Gets or sets the column count.</summary>
    public int Columns;
    /// <summary>Gets or sets row-major matrix values.</summary>
    public double[] Values;

    /// <summary>Initializes an empty matrix.</summary>
    public IccMatrix()
    {
        Rows = 0;
        Columns = 0;
        Values = new double[0];
    }
}
