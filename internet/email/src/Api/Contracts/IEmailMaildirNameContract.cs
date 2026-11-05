using System;
using System.Globalization;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMaildirName"/>.
/// </summary>
public interface IEmailMaildirNameContract
{
    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <returns>The current file name.</returns>
    string GetFileName();
    /// <summary>
    /// Gets the unique name.
    /// </summary>
    /// <returns>The current unique name.</returns>
    string GetUniqueName();
    /// <summary>
    /// Determines whether the info is present or available.
    /// </summary>
    /// <returns><c>true</c> when the info is available; otherwise, <c>false</c>.</returns>
    bool HasInfo();
    /// <summary>
    /// Gets the info version.
    /// </summary>
    /// <returns>The current info version.</returns>
    int GetInfoVersion();
    /// <summary>
    /// Gets the info text.
    /// </summary>
    /// <returns>The current info text.</returns>
    string? GetInfoText();
    /// <summary>
    /// Gets the flag text.
    /// </summary>
    /// <returns>The current flag text.</returns>
    string GetFlagText();
    /// <summary>
    /// Gets the unknown flag text.
    /// </summary>
    /// <returns>The current unknown flag text.</returns>
    string GetUnknownFlagText();
    /// <summary>
    /// Gets the flags.
    /// </summary>
    /// <returns>The current flags.</returns>
    EmailMaildirFlags GetFlags();
    /// <summary>
    /// Determines whether the current value or state is info well formed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is info well formed; otherwise, <c>false</c>.</returns>
    bool IsInfoWellFormed();
    /// <summary>
    /// Determines whether the current value or state is canonical.
    /// </summary>
    /// <returns><c>true</c> when the value or state is canonical; otherwise, <c>false</c>.</returns>
    bool IsCanonical();
    /// <summary>
    /// Determines whether the flag is present or available.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the flag is available; otherwise, <c>false</c>.</returns>
    bool HasFlag(EmailMaildirFlags value);
}
