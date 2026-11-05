namespace cookies;

/// <summary>
/// Determines whether origins are secure enough for Secure cookie operations.
/// </summary>
public interface ISecureOriginPolicy
{
    /// <summary>
    /// Determines whether an absolute origin URI is secure.
    /// </summary>
    /// <param name="origin">The absolute origin URI.</param>
    /// <returns>True when the origin is secure.</returns>
    bool IsSecureOrigin(global::System.Uri origin);
}
