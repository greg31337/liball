namespace cookies;

/// <summary>
/// Identifies a stored cookie independently of its value and attributes.
/// </summary>
public class CookieIdentity
{
    /// <summary>
    /// The cookie name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// The canonical cookie domain.
    /// </summary>
    public string Domain = "";

    /// <summary>
    /// The canonical cookie path.
    /// </summary>
    public string Path = "";

    /// <summary>
    /// Indicates whether the identity includes a partition key.
    /// </summary>
    public bool HasPartitionKey;

    /// <summary>
    /// The canonical partition key when <see cref="HasPartitionKey"/> is true.
    /// </summary>
    public string PartitionKey = "";
}
