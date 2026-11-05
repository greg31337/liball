using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpReadResult"/>.
/// </summary>
public interface IEmailOpenPgpReadResultContract
{
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
    /// <summary>
    /// Gets the packet count.
    /// </summary>
    /// <returns>The current packet count.</returns>
    int GetPacketCount();
    /// <summary>
    /// Gets the packet at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested packet.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailOpenPgpPacket GetPacketAt(int index);
    /// <summary>
    /// Gets the packets.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current packets.</returns>
    List<EmailOpenPgpPacket> GetPackets();
    /// <summary>
    /// Determines whether the operation or source value was armored.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasArmored();
    /// <summary>
    /// Gets the armor.
    /// </summary>
    /// <returns>The current armor.</returns>
    EmailOpenPgpArmorResult? GetArmor();
    /// <summary>
    /// Gets the original bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original bytes.</returns>
    byte[] GetOriginalBytes();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
}
