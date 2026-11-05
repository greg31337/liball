namespace ftp;

/// <summary>
/// Authorizes FTP filesystem, metadata, SITE, and server-to-server operations.
/// </summary>
public interface IFtpAuthorizer
{
    /// <summary>
    /// Makes one authorization decision.
    /// </summary>
    /// <param name="request">The principal, operation, and normalized paths.</param>
    /// <returns>The exception-less authorization result.</returns>
    FtpAuthorizationResult AuthorizeOperation(FtpAuthorizationRequest request);
}

