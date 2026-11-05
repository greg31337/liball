using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailProtectedHeaderEntry"/>.
/// </summary>
public interface IEmailProtectedHeaderEntryContract
{
    /// <summary>
    /// Gets the header.
    /// </summary>
    /// <returns>The current header.</returns>
    EmailHeader GetHeader();
    /// <summary>
    /// Gets the state.
    /// </summary>
    /// <returns>The current state.</returns>
    EmailProtectedHeaderState GetState();
}
