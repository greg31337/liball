namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailUtf8"/>.
/// </summary>
public interface IEmailUtf8Contract
{
    /// <summary>
    /// Determines whether the current value or state is valid.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is valid; otherwise, <c>false</c>.</returns>
    bool IsValid(byte[] value);
}
