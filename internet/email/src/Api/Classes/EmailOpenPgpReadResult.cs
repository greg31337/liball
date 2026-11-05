using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Describes the outcome of a openPGP read operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailOpenPgpReadResult : IEmailOpenPgpReadResultContract
{
    List<EmailOpenPgpPacket> packets;
    EmailOpenPgpArmorResult? armor;
    byte[] originalBytes;
    EmailIssueList issues;
    bool complete;
    /// <summary>
    /// Creates a new openPGP read result.
    /// </summary>
    /// <param name = "packets">The packets used by the operation.</param>
    /// <param name = "armor">The armor used by the operation.</param>
    /// <param name = "originalBytes">The source bytes or data to process.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "complete">Whether to enable the complete behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpReadResult(List<EmailOpenPgpPacket> packets, EmailOpenPgpArmorResult? armor, byte[] originalBytes, EmailIssueList issues, bool complete)
    {
        if (packets == null || originalBytes == null || issues == null)
            throw new ArgumentNullException("packets");
        this.packets = new List<EmailOpenPgpPacket>(packets);
        this.armor = armor;
        this.originalBytes = new EmailOpenPgpArmorResultOperations().Copy(originalBytes);
        this.issues = new EmailOpenPgpArmorResultOperations().CopyIssues(issues);
        this.complete = complete;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return complete && packets.Count > 0 && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return complete;
    }

    /// <summary>
    /// Gets the packet count.
    /// </summary>
    /// <returns>The current packet count.</returns>
    public int GetPacketCount()
    {
        return packets.Count;
    }

    /// <summary>
    /// Gets the packet at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested packet.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailOpenPgpPacket GetPacketAt(int index)
    {
        if (index < 0 || index >= packets.Count)
            throw new ArgumentOutOfRangeException("index");
        return packets[index];
    }

    /// <summary>
    /// Gets the packets.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current packets.</returns>
    public List<EmailOpenPgpPacket> GetPackets()
    {
        return new List<EmailOpenPgpPacket>(packets);
    }

    /// <summary>
    /// Determines whether the operation or source value was armored.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasArmored()
    {
        return armor != null;
    }

    /// <summary>
    /// Gets the armor.
    /// </summary>
    /// <returns>The current armor.</returns>
    public EmailOpenPgpArmorResult? GetArmor()
    {
        return armor;
    }

    /// <summary>
    /// Gets the original bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original bytes.</returns>
    public byte[] GetOriginalBytes()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(originalBytes);
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        return new EmailOpenPgpArmorResultOperations().CopyIssues(issues);
    }
}
