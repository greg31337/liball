using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a list management value.
/// </summary>
public class EmailListManagementValue : IEmailListManagementValueContract
{
    EmailListManagementKind kind;
    List<EmailListCommand> commands;
    bool postingProhibited;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new list management value.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "commands">The commands used by the operation.</param>
    /// <param name = "postingProhibited">Whether to enable the posting prohibited behavior.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailListManagementValue(EmailListManagementKind kind, List<EmailListCommand> commands, bool postingProhibited, byte[] rawBytes, EmailSourcePosition sourcePosition)
    {
        if (kind < EmailListManagementKind.Help || kind > EmailListManagementKind.Archive)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (commands == null)
        {
            throw new ArgumentNullException("commands");
        }

        if (postingProhibited && kind != EmailListManagementKind.Post)
        {
            throw new ArgumentException("Only List-Post can prohibit posting.", "postingProhibited");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.kind = kind;
        this.commands = new List<EmailListCommand>(commands);
        this.postingProhibited = postingProhibited;
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailListManagementKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the command count.
    /// </summary>
    /// <returns>The current command count.</returns>
    public int GetCommandCount()
    {
        return commands.Count;
    }

    /// <summary>
    /// Gets the command.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current command.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailListCommand GetCommand(int index)
    {
        if (index < 0 || index >= commands.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return commands[index];
    }

    /// <summary>
    /// Gets the commands.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current commands.</returns>
    public List<EmailListCommand> GetCommands()
    {
        return new List<EmailListCommand>(commands);
    }

    /// <summary>
    /// Determines whether the current value or state is posting prohibited.
    /// </summary>
    /// <returns><c>true</c> when the value or state is posting prohibited; otherwise, <c>false</c>.</returns>
    public bool IsPostingProhibited()
    {
        return postingProhibited;
    }

    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    public byte[] GetRawBytes()
    {
        return Copy(rawBytes);
    }

    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
