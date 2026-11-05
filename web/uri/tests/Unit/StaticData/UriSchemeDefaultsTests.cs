namespace uri.Tests;

public class UriSchemeDefaultsTests
{
    [Test]
    public void SchemeNamesAndDefaultPortsMatchBuiltInProfiles()
    {
        Assert.Multiple(() =>
        {
            Assert.That(UriSchemeDefaults.HttpScheme, Is.EqualTo("http"));
            Assert.That(UriSchemeDefaults.HttpDefaultPort, Is.EqualTo(80));
            Assert.That(UriSchemeDefaults.HttpsScheme, Is.EqualTo("https"));
            Assert.That(UriSchemeDefaults.HttpsDefaultPort, Is.EqualTo(443));
            Assert.That(UriSchemeDefaults.WebSocketScheme, Is.EqualTo("ws"));
            Assert.That(UriSchemeDefaults.WebSocketDefaultPort, Is.EqualTo(80));
            Assert.That(UriSchemeDefaults.SecureWebSocketScheme, Is.EqualTo("wss"));
            Assert.That(UriSchemeDefaults.SecureWebSocketDefaultPort, Is.EqualTo(443));
            Assert.That(UriSchemeDefaults.FtpScheme, Is.EqualTo("ftp"));
            Assert.That(UriSchemeDefaults.FtpDefaultPort, Is.EqualTo(21));
        });
    }
}
