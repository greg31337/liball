namespace icc;

/// <summary>Represents an ICC.2 array of compact compressed-row sparse matrices.</summary>
public class IccSparseMatrixArray
{
    /// <summary>Gets or sets the dense output-channel count represented by each sparse matrix.</summary>
    public int EquivalentOutputChannels;

    /// <summary>Gets or sets the sparse-matrix value-encoding selector.</summary>
    public int Encoding;

    /// <summary>Gets or sets LUT grid-point counts when the matrices form a sparse-matrix processing element.</summary>
    public int[] GridPoints;

    /// <summary>Gets or sets the matrices in encoded order.</summary>
    public IccSparseMatrix[] Matrices;

    /// <summary>Initializes an empty sparse-matrix array.</summary>
    public IccSparseMatrixArray()
    {
        EquivalentOutputChannels = 0;
        Encoding = 0;
        GridPoints = new int[0];
        Matrices = new IccSparseMatrix[0];
    }
}
