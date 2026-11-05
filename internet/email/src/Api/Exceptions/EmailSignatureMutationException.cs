using System;

namespace email;
/// <summary>
/// Represents a signature mutation exception.
/// </summary>
public class EmailSignatureMutationException : System.Exception
{
    /// <summary>
    /// Creates a new signature mutation exception.
    /// </summary>
    public EmailSignatureMutationException() : base("The mutation was blocked because it would invalidate protected signature bytes.")
    {
    }
}
