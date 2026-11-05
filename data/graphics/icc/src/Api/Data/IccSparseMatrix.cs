namespace icc;

/// <summary>Represents a compressed-row sparse matrix.</summary>
public class IccSparseMatrix
{
    /// <summary>Gets or sets the row count.</summary>
    public int Rows;
    /// <summary>Gets or sets the column count.</summary>
    public int Columns;
    /// <summary>Gets or sets the encoded value-format selector.</summary>
    public int Encoding;
    /// <summary>Gets or sets row start offsets.</summary>
    public int[] RowStartOffsets;
    /// <summary>Gets or sets matrix-entry column indices.</summary>
    public int[] ColumnIndices;
    /// <summary>Gets or sets matrix-entry values.</summary>
    public double[] Values;
    /// <summary>Gets or sets retained index padding.</summary>
    public byte[] IndexPadding;
    /// <summary>Gets or sets retained data padding.</summary>
    public byte[] DataPadding;

    /// <summary>Initializes an empty sparse matrix.</summary>
    public IccSparseMatrix()
    {
        Rows = 0;
        Columns = 0;
        Encoding = 0;
        RowStartOffsets = new int[0];
        ColumnIndices = new int[0];
        Values = new double[0];
        IndexPadding = new byte[0];
        DataPadding = new byte[0];
    }
}
