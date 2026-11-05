using System;
using System.Globalization;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAddressIdn"/>.
/// </summary>
public interface IEmailAddressIdnContract
{
    /// <summary>
    /// Converts to the ascii.
    /// </summary>
    /// <param name = "domain">The domain used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    string ToAscii(string domain);
    /// <summary>
    /// Converts to the unicode.
    /// </summary>
    /// <param name = "domain">The domain used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    string ToUnicode(string domain);
    /// <summary>
    /// Determines whether the current value or state is internationalized.
    /// </summary>
    /// <param name = "domain">The domain used by the operation.</param>
    /// <returns><c>true</c> when the value or state is internationalized; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    bool IsInternationalized(string domain);
}
