using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents a protected header entry.
/// </summary>
public class EmailProtectedHeaderEntry : IEmailProtectedHeaderEntryContract
{
    EmailHeader header;
    EmailProtectedHeaderState state;
    /// <summary>
    /// Creates a new protected header entry.
    /// </summary>
    /// <param name = "header">The header used by the operation.</param>
    /// <param name = "state">The state used by the operation.</param>
    public EmailProtectedHeaderEntry(EmailHeader header, EmailProtectedHeaderState state)
    {
        this.header = header.Clone();
        this.state = state;
    }

    /// <summary>
    /// Gets the header.
    /// </summary>
    /// <returns>The current header.</returns>
    public EmailHeader GetHeader()
    {
        return header.Clone();
    }

    /// <summary>
    /// Gets the state.
    /// </summary>
    /// <returns>The current state.</returns>
    public EmailProtectedHeaderState GetState()
    {
        return state;
    }
}
