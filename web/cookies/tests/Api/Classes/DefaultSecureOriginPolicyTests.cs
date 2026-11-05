namespace cookies.Tests;

public class DefaultSecureOriginPolicyTests
{
    [TestCase("https://example.test/path")]
    [TestCase("HTTPS://EXAMPLE.TEST:8443/path")]
    [TestCase("wss://example.test/socket")]
    [TestCase("WSS://[2001:DB8::1]:9443/socket")]
    public void SecureSchemesAreSecureForValidOrigins(string uriText)
    {
        ISecureOriginPolicy disabled = new DefaultSecureOriginPolicy(false);
        ISecureOriginPolicy enabled = new DefaultSecureOriginPolicy(true);

        Assert.That(disabled.IsSecureOrigin(new Uri(uriText)), Is.True);
        Assert.That(enabled.IsSecureOrigin(new Uri(uriText)), Is.True);
    }

    [TestCase("http://localhost/path")]
    [TestCase("http://LOCALHOST.:8080/path")]
    [TestCase("http://service.localhost/path")]
    [TestCase("ws://deep.service.localhost/socket")]
    public void EnabledPolicyTrustsHttpAndWebSocketLocalhostNames(string uriText)
    {
        ISecureOriginPolicy policy = new DefaultSecureOriginPolicy(true);

        Assert.That(policy.IsSecureOrigin(new Uri(uriText)), Is.True);
    }

    [TestCase("http://127.0.0.1/path")]
    [TestCase("http://127.255.255.254:8080/path")]
    [TestCase("ws://127.10.20.30/socket")]
    [TestCase("http://[::1]/path")]
    [TestCase("ws://[0:0:0:0:0:0:0:1]:8080/socket")]
    public void EnabledPolicyTrustsHttpAndWebSocketLoopbackAddresses(string uriText)
    {
        ISecureOriginPolicy policy = new DefaultSecureOriginPolicy(true);

        Assert.That(policy.IsSecureOrigin(new Uri(uriText)), Is.True);
    }

    [TestCase("http://localhost/path")]
    [TestCase("ws://service.localhost/socket")]
    [TestCase("http://127.0.0.1/path")]
    [TestCase("ws://[::1]/socket")]
    public void DisabledPolicyDoesNotTrustInsecureLoopbackOrigins(string uriText)
    {
        ISecureOriginPolicy policy = new DefaultSecureOriginPolicy(false);

        Assert.That(policy.IsSecureOrigin(new Uri(uriText)), Is.False);
    }

    [TestCase("http://localhost.example/path")]
    [TestCase("http://notlocalhost/path")]
    [TestCase("http://192.0.2.1/path")]
    [TestCase("http://126.255.255.255/path")]
    [TestCase("http://128.0.0.1/path")]
    [TestCase("http://[::2]/path")]
    public void EnabledPolicyDoesNotTrustLookalikesOrNonLoopbackHosts(string uriText)
    {
        ISecureOriginPolicy policy = new DefaultSecureOriginPolicy(true);

        Assert.That(policy.IsSecureOrigin(new Uri(uriText)), Is.False);
    }

    [TestCase("ftp://localhost/resource")]
    [TestCase("file://localhost/resource")]
    [TestCase("mailto:user@example.test")]
    public void LoopbackOptionDoesNotTrustUnrelatedSchemes(string uriText)
    {
        ISecureOriginPolicy policy = new DefaultSecureOriginPolicy(true);

        Assert.That(policy.IsSecureOrigin(new Uri(uriText)), Is.False);
    }

    [Test]
    public void NullAndRelativeOriginsAreNotSecure()
    {
        ISecureOriginPolicy policy = new DefaultSecureOriginPolicy(true);

        Assert.That(policy.IsSecureOrigin(null!), Is.False);
        Assert.That(policy.IsSecureOrigin(new Uri("relative", UriKind.Relative)), Is.False);
    }
}
