namespace odf;

/// <summary>
/// Supplies decrypted OpenPGP package-entry key material to ODF security operations.
/// </summary>
/// <remarks>
/// The SDK passes preserved OpenPGP recipient and encrypted-key metadata to this
/// hook but does not invoke an OpenPGP program or implementation itself.
/// </remarks>
public interface IOdfOpenPgpKeyProvider
{
    /// <summary>
    /// Gets the decrypted entry key for one OpenPGP encrypted-key description.
    /// </summary>
    /// <param name="encryptedKey">The parsed recipient and encrypted-key metadata.</param>
    /// <returns>
    /// A fresh byte array containing the decrypted entry key. Ownership transfers
    /// to the SDK, which clears the array after copying it into operation-scoped storage.
    /// </returns>
    public byte[] GetDecryptedKey(OdfEncryptedKey encryptedKey);
}
