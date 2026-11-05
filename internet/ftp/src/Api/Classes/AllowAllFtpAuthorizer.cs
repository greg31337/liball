namespace ftp;

/// <summary>
/// Allows every well-formed operation requested by an authenticated principal.
/// </summary>
public class AllowAllFtpAuthorizer : IFtpAuthorizer
{
    /// <summary>
    /// Creates a stateless allow-all authorization provider.
    /// </summary>
    public AllowAllFtpAuthorizer()
    {
    }

    /// <summary>
    /// Allows a well-formed authorization request from an authenticated principal.
    /// </summary>
    /// <param name="request">The principal, operation, normalized paths, and optional entry.</param>
    /// <returns>A handled allow or deny decision.</returns>
    public FtpAuthorizationResult AuthorizeOperation(
        FtpAuthorizationRequest request)
    {
        string? failure = FtpAuthorizationGate.ValidateRequest(request);
        if (failure != null)
        {
            return FtpAuthorizationGate.CreateDeniedResult(failure);
        }
        return FtpAuthorizationGate.CreateAllowedResult();
    }
}
