using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpLiteralData"/>.
/// </summary>
public interface IEmailOpenPgpLiteralDataContract
{
    /// <summary>
    /// Gets the format.
    /// </summary>
    /// <returns>The current format.</returns>
    int GetFormat();
    /// <summary>
    /// Gets the file name bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current file name bytes.</returns>
    byte[] GetFileNameBytes();
    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <returns>The current file name.</returns>
    string GetFileName();
    /// <summary>
    /// Gets the timestamp.
    /// </summary>
    /// <returns>The current timestamp.</returns>
    uint GetTimestamp();
    /// <summary>
    /// Gets the data.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current data.</returns>
    byte[] GetData();
}
