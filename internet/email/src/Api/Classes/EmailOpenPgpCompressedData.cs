using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents an openPGP compressed data.
/// </summary>
public class EmailOpenPgpCompressedData : IEmailOpenPgpCompressedDataContract
{
    int algorithm;
    byte[] compressedData;
    /// <summary>
    /// Creates a new openPGP compressed data.
    /// </summary>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "compressedData">The compressed data used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpCompressedData(int algorithm, byte[] compressedData)
    {
        if (compressedData == null)
            throw new ArgumentNullException("compressedData");
        this.algorithm = algorithm;
        this.compressedData = new EmailOpenPgpArmorResultOperations().Copy(compressedData);
    }

    /// <summary>
    /// Gets the algorithm.
    /// </summary>
    /// <returns>The current algorithm.</returns>
    public int GetAlgorithm()
    {
        return algorithm;
    }

    /// <summary>
    /// Gets the compressed data.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current compressed data.</returns>
    public byte[] GetCompressedData()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(compressedData);
    }
}
