using System.Security.Cryptography;
using System.Text;

namespace pop3.Tests;

public class Pop3ApopDigestTests
{
    [Test]
    public void CreatesTheRfc1939DigestInLowercaseHexadecimal()
    {
        Pop3Credentials credentials = Credentials("mrose", "tanstaaf");

        Pop3ApopRequest request = new Pop3ApopDigest().CreateRequest(
            "<1896.697170952@dbc.mtview.ca.us>",
            credentials,
            false);

        Assert.That(request.Username, Is.EqualTo("mrose"));
        Assert.That(request.Timestamp, Is.EqualTo("<1896.697170952@dbc.mtview.ca.us>"));
        Assert.That(request.Digest, Is.EqualTo("c4c9334bac560ecc979e58001b3e22fb"));
    }

    [TestCase(
        "<1@example.test>",
        "secret",
        false,
        "ebe683fd57e719dbf95f4bbfe20f5d8f")]
    [TestCase(
        "<unique.123@host>",
        "P@ssw0rd",
        false,
        "370550f1eaf020ab4d8eb8907d8206d2")]
    [TestCase(
        "<utf8@example.test>",
        "päss🔑",
        true,
        "a6a0df8315ce4cf868ef9502380d8e0b")]
    public void CreatesFixedDigestVectors(
        string timestamp,
        string password,
        bool useUtf8,
        string expectedDigest)
    {
        Pop3ApopRequest request = new Pop3ApopDigest().CreateRequest(
            timestamp,
            Credentials("alice", password),
            useUtf8);

        Assert.That(request.Timestamp, Is.EqualTo(timestamp));
        Assert.That(request.Digest, Is.EqualTo(expectedDigest));
    }

    [Test]
    public void UsesStrictUtf8ForInternationalizedCredentials()
    {
        const string timestamp = "<utf8@example.test>";
        Pop3Credentials credentials = Credentials("álîçé😀", "pä ss🔑");
        const string expected = "5eadc72f4bcbde9255b93b024013f3b2";

        Pop3ApopRequest request = new Pop3ApopDigest().CreateRequest(
            timestamp,
            credentials,
            true);

        Assert.That(request.Username, Is.EqualTo(credentials.Username));
        Assert.That(request.Digest, Is.EqualTo(expected));
        Assert.Throws<ArgumentException>(() => new Pop3ApopDigest().CreateRequest(
            timestamp,
            credentials,
            false));
    }

    [TestCase("missing-at")]
    [TestCase("<missing-at>")]
    [TestCase("<@domain>")]
    [TestCase("<local@>")]
    [TestCase("<a b@domain>")]
    [TestCase("<a@b@c>")]
    public void RejectsMalformedChallenges(string timestamp)
    {
        Assert.Throws<InvalidDataException>(() => new Pop3ApopDigest().CreateRequest(
            timestamp,
            Credentials("alice", "secret"),
            false));
    }

    [Test]
    public void RejectsInvalidCredentialsBeforeHashing()
    {
        const string timestamp = "<id@example.test>";

        Assert.Throws<ArgumentNullException>(() => new Pop3ApopDigest().CreateRequest(
            timestamp,
            null!,
            false));
        Assert.Throws<ArgumentException>(() => new Pop3ApopDigest().CreateRequest(
            timestamp,
            Credentials("", "secret"),
            false));
        Assert.Throws<ArgumentException>(() => new Pop3ApopDigest().CreateRequest(
            timestamp,
            Credentials("alice smith", "secret"),
            false));
        Assert.Throws<ArgumentException>(() => new Pop3ApopDigest().CreateRequest(
            timestamp,
            Credentials("alice", "sec\rret"),
            false));
        Assert.Throws<ArgumentException>(() => new Pop3ApopDigest().CreateRequest(
            timestamp,
            Credentials("ali\uD800ce", "secret"),
            true));
    }

    private static Pop3Credentials Credentials(string username, string password)
    {
        Pop3Credentials credentials = new Pop3Credentials();
        credentials.Username = username;
        credentials.Password = password;
        return credentials;
    }
}
