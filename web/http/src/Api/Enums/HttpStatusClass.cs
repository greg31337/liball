namespace http;

/// <summary>
/// Identifies the class of an HTTP status code.
/// </summary>
public enum HttpStatusClass
{
    /// <summary>
    /// The value is not a valid three-digit HTTP status code.
    /// </summary>
    Invalid = 0,

    /// <summary>
    /// An informational status code from 100 through 199.
    /// </summary>
    Informational = 1,

    /// <summary>
    /// A successful status code from 200 through 299.
    /// </summary>
    Successful = 2,

    /// <summary>
    /// A redirection status code from 300 through 399.
    /// </summary>
    Redirection = 3,

    /// <summary>
    /// A client-error status code from 400 through 499.
    /// </summary>
    ClientError = 4,

    /// <summary>
    /// A server-error status code from 500 through 599.
    /// </summary>
    ServerError = 5,

    /// <summary>
    /// A valid extension status code outside the standard five classes.
    /// </summary>
    Extension = 6
}
