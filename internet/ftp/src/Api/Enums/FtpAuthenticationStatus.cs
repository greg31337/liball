namespace ftp;

/// <summary>
/// Describes the result of an authentication provider operation.
/// </summary>
public enum FtpAuthenticationStatus
{
    /// <summary>
    /// Indicates that the provider did not handle the request.
    /// </summary>
    NotHandled = 0,
    /// <summary>
    /// Indicates successful authentication.
    /// </summary>
    Success = 1,
    /// <summary>
    /// Indicates failed authentication.
    /// </summary>
    Failure = 2,
    /// <summary>
    /// Indicates that another credential or account step is required.
    /// </summary>
    AdditionalInformationRequired = 3
}
