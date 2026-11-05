using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpSignatureSubpacket"/>.
/// </summary>
public interface IEmailOpenPgpSignatureSubpacketContract
{
    /// <summary>
    /// Gets the type identifier.
    /// </summary>
    /// <returns>The current type identifier.</returns>
    int GetTypeIdentifier();
    /// <summary>
    /// Determines whether the current value or state is critical.
    /// </summary>
    /// <returns><c>true</c> when the value or state is critical; otherwise, <c>false</c>.</returns>
    bool IsCritical();
    /// <summary>
    /// Gets the data.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current data.</returns>
    byte[] GetData();
    /// <summary>
    /// Gets the encoded bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current encoded bytes.</returns>
    byte[] GetEncodedBytes();
}
