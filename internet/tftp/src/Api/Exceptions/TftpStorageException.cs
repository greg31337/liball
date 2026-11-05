namespace tftp;

/// <summary>
/// Represents an exceptional local storage failure that is not an expected storage result.
/// </summary>
public class TftpStorageException : System.Exception
{
    /// <summary>
    /// The storage operation that failed.
    /// </summary>
    public string Operation;

    /// <summary>
    /// Initializes a storage exception with operation and underlying failure details.
    /// </summary>
    /// <param name="message">The storage failure message.</param>
    /// <param name="operation">The storage operation that failed.</param>
    /// <param name="innerException">The underlying failure, or null when unavailable.</param>
    public TftpStorageException(
        string message,
        string operation,
        System.Exception? innerException)
        : base(message, innerException)
    {
        Operation = operation;
    }
}
