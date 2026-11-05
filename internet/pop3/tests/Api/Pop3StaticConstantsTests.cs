namespace pop3.Tests;

public class Pop3StaticConstantsTests
{
    [Test]
    public void CommandsUseCanonicalWireKeywords()
    {
        Assert.That(Pop3Commands.User, Is.EqualTo("USER"));
        Assert.That(Pop3Commands.Pass, Is.EqualTo("PASS"));
        Assert.That(Pop3Commands.Apop, Is.EqualTo("APOP"));
        Assert.That(Pop3Commands.Stat, Is.EqualTo("STAT"));
        Assert.That(Pop3Commands.List, Is.EqualTo("LIST"));
        Assert.That(Pop3Commands.Retr, Is.EqualTo("RETR"));
        Assert.That(Pop3Commands.Dele, Is.EqualTo("DELE"));
        Assert.That(Pop3Commands.Noop, Is.EqualTo("NOOP"));
        Assert.That(Pop3Commands.Rset, Is.EqualTo("RSET"));
        Assert.That(Pop3Commands.Quit, Is.EqualTo("QUIT"));
        Assert.That(Pop3Commands.Top, Is.EqualTo("TOP"));
        Assert.That(Pop3Commands.Uidl, Is.EqualTo("UIDL"));
        Assert.That(Pop3Commands.Capa, Is.EqualTo("CAPA"));
        Assert.That(Pop3Commands.Stls, Is.EqualTo("STLS"));
        Assert.That(Pop3Commands.Auth, Is.EqualTo("AUTH"));
        Assert.That(Pop3Commands.Utf8, Is.EqualTo("UTF8"));
        Assert.That(Pop3Commands.Lang, Is.EqualTo("LANG"));
    }

    [Test]
    public void CapabilitiesUseRegisteredWireTags()
    {
        Assert.That(Pop3Capabilities.Top, Is.EqualTo("TOP"));
        Assert.That(Pop3Capabilities.User, Is.EqualTo("USER"));
        Assert.That(Pop3Capabilities.Sasl, Is.EqualTo("SASL"));
        Assert.That(Pop3Capabilities.ResponseCodes, Is.EqualTo("RESP-CODES"));
        Assert.That(Pop3Capabilities.LoginDelay, Is.EqualTo("LOGIN-DELAY"));
        Assert.That(Pop3Capabilities.Pipelining, Is.EqualTo("PIPELINING"));
        Assert.That(Pop3Capabilities.Expire, Is.EqualTo("EXPIRE"));
        Assert.That(Pop3Capabilities.Uidl, Is.EqualTo("UIDL"));
        Assert.That(Pop3Capabilities.Implementation, Is.EqualTo("IMPLEMENTATION"));
        Assert.That(Pop3Capabilities.Stls, Is.EqualTo("STLS"));
        Assert.That(Pop3Capabilities.AuthenticationResponseCode, Is.EqualTo("AUTH-RESP-CODE"));
        Assert.That(Pop3Capabilities.Utf8, Is.EqualTo("UTF8"));
        Assert.That(Pop3Capabilities.Lang, Is.EqualTo("LANG"));
    }

    [Test]
    public void ResponseCodesUseRegisteredWireValues()
    {
        Assert.That(Pop3ResponseCodes.LoginDelay, Is.EqualTo("LOGIN-DELAY"));
        Assert.That(Pop3ResponseCodes.InUse, Is.EqualTo("IN-USE"));
        Assert.That(Pop3ResponseCodes.SystemPermanent, Is.EqualTo("SYS/PERM"));
        Assert.That(Pop3ResponseCodes.SystemTemporary, Is.EqualTo("SYS/TEMP"));
        Assert.That(Pop3ResponseCodes.Authentication, Is.EqualTo("AUTH"));
        Assert.That(Pop3ResponseCodes.Utf8, Is.EqualTo("UTF8"));
    }

    [Test]
    public void DefaultsExposeProtocolLimitsAndConservativeResourceLimits()
    {
        Assert.That(Pop3Defaults.DefaultPort, Is.EqualTo(110));
        Assert.That(Pop3Defaults.ImplicitTlsPort, Is.EqualTo(995));
        Assert.That(Pop3Defaults.MaximumCommandLineOctets, Is.EqualTo(255));
        Assert.That(Pop3Defaults.MaximumResponseLineOctets, Is.EqualTo(512));
        Assert.That(Pop3Defaults.MinimumServerInactivityTimeoutMilliseconds, Is.EqualTo(600000));
        Assert.That(Pop3Defaults.DefaultServerInactivityTimeoutMilliseconds, Is.EqualTo(600000));
        Assert.That(Pop3Defaults.DefaultConnectTimeoutMilliseconds, Is.EqualTo(30000));
        Assert.That(Pop3Defaults.DefaultReadTimeoutMilliseconds, Is.EqualTo(30000));
        Assert.That(Pop3Defaults.DefaultWriteTimeoutMilliseconds, Is.EqualTo(30000));
        Assert.That(Pop3Defaults.DefaultAuthenticationTimeoutMilliseconds, Is.EqualTo(30000));
        Assert.That(Pop3Defaults.DefaultBufferSize, Is.EqualTo(8192));
        Assert.That(Pop3Defaults.DefaultMaximumMessageLineOctets, Is.EqualTo(1048576));
        Assert.That(Pop3Defaults.DefaultMaximumSaslLineOctets, Is.EqualTo(1048576));
        Assert.That(Pop3Defaults.DefaultMessageMemoryCeilingBytes, Is.EqualTo(67108864));
    }
}
