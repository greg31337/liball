using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTextWriteOptions"/>.
/// </summary>
public interface IEmailTextWriteOptionsContract
{
    /// <summary>Gets the byte-oriented write options.</summary>
    /// <returns>The current write options.</returns>
    EmailWriteOptions GetWriteOptions();
    /// <summary>Sets the byte-oriented write options.</summary>
    /// <param name = "value">The value to store or process.</param>
    void SetWriteOptions(EmailWriteOptions value);
    /// <summary>Gets the charset name used to decode serialized bytes.</summary>
    /// <returns>The current charset name.</returns>
    string GetCharsetName();
    /// <summary>Sets the charset name used to decode serialized bytes.</summary>
    /// <param name = "value">The value to store or process.</param>
    void SetCharsetName(string value);
    /// <summary>Gets the newline policy.</summary>
    /// <returns>The current newline policy.</returns>
    EmailTextNewlinePolicy GetNewlinePolicy();
    /// <summary>Sets the newline policy.</summary>
    /// <param name = "value">The value to store or process.</param>
    void SetNewlinePolicy(EmailTextNewlinePolicy value);
    /// <summary>Determines whether textual leaf transfer encodings are decoded and validated.</summary>
    /// <returns><c>true</c> when transfer encodings are decoded; otherwise, <c>false</c>.</returns>
    bool GetDecodeTransferEncodings();
    /// <summary>Sets whether textual leaf transfer encodings are decoded and validated.</summary>
    /// <param name = "value">Whether to enable transfer decoding.</param>
    void SetDecodeTransferEncodings(bool value);
    /// <summary>Gets the failure behavior.</summary>
    /// <returns>The current failure behavior.</returns>
    EmailTextWriteFailureBehavior GetFailureBehavior();
    /// <summary>Sets the failure behavior.</summary>
    /// <param name = "value">The value to store or process.</param>
    void SetFailureBehavior(EmailTextWriteFailureBehavior value);
    /// <summary>Gets the maximum serialized byte count.</summary>
    /// <returns>The current maximum serialized byte count.</returns>
    long GetMaximumBytes();
    /// <summary>Sets the maximum serialized byte count.</summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumBytes(long value);
    /// <summary>Gets the maximum text character count.</summary>
    /// <returns>The current maximum text character count.</returns>
    int GetMaximumCharacters();
    /// <summary>Sets the maximum text character count.</summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumCharacters(int value);
    /// <summary>Creates an independent copy of this instance.</summary>
    /// <returns>A separate copy with the same settings.</returns>
    EmailTextWriteOptions Clone();
}
