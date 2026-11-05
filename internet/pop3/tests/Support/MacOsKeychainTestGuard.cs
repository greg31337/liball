using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace pop3.Tests;

internal static class MacOsKeychainTestGuard
{
    internal static X509Certificate2 Create(
        Func<X509Certificate2> certificateFactory)
    {
        try
        {
            return certificateFactory();
        }
        catch (CryptographicException exception)
        {
            if (OperatingSystem.IsMacOS() && exception.Message.Contains(
                "keychain",
                StringComparison.OrdinalIgnoreCase))
            {
                Assert.Ignore(
                    "This macOS environment does not provide a temporary keychain for test certificates.");
            }
            throw;
        }
    }
}
