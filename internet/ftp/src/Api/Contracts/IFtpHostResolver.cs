namespace ftp;

/// <summary>
/// Resolves FTP HOST and TLS SNI context to a virtual host.
/// </summary>
public interface IFtpHostResolver
{
    /// <summary>
    /// Resolves one virtual-host request.
    /// </summary>
    /// <param name="request">The requested host, endpoints, and TLS context.</param>
    /// <returns>The exception-less host result.</returns>
    FtpVirtualHostResult ResolveVirtualHost(FtpVirtualHostRequest request);
}

