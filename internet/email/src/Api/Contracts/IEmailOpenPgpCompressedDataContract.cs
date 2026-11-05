using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpCompressedData"/>.
/// </summary>
public interface IEmailOpenPgpCompressedDataContract
{
    /// <summary>
    /// Gets the algorithm.
    /// </summary>
    /// <returns>The current algorithm.</returns>
    int GetAlgorithm();
    /// <summary>
    /// Gets the compressed data.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current compressed data.</returns>
    byte[] GetCompressedData();
}
