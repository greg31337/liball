namespace sip.Tests;

public class SipProtocolTokenGeneratorTests
{
    [Test]
    public void CreatesProtocolSpecificTokensFromInjectedRandomBytes()
    {
        SipProtocolTokenGenerator generator = new SipProtocolTokenGenerator(
            new FixedRandomSource(new byte[] { 0 }));

        Assert.Multiple(() =>
        {
            Assert.That(generator.CreateBranch(), Is.EqualTo("z9hG4bK" + new string('0', 32)));
            Assert.That(generator.CreateTag(), Is.EqualTo(new string('0', 32)));
            Assert.That(generator.CreateCallId(), Is.EqualTo(new string('0', 32)));
            Assert.That(generator.CreateClientNonce(), Is.EqualTo(new string('A', 32)));
            Assert.That(generator.CreateMimeBoundary(), Is.EqualTo("sip-" + new string('0', 36)));
            Assert.That(generator.CreateFlowToken(), Is.EqualTo(new string('A', 32)));
            Assert.That(generator.CreateOpaqueToken(), Is.EqualTo(new string('A', 32)));
        });
    }

    [Test]
    public void RequestsCollisionResistantEntropyForEveryTokenKind()
    {
        ProtocolRecordingRandomSource source = new ProtocolRecordingRandomSource();
        SipProtocolTokenGenerator generator = new SipProtocolTokenGenerator(source);

        generator.CreateBranch();
        generator.CreateTag();
        generator.CreateCallId();
        generator.CreateClientNonce();
        generator.CreateMimeBoundary();
        generator.CreateFlowToken();
        generator.CreateOpaqueToken();

        Assert.That(source.RequestedByteCounts,
            Is.EqualTo(new int[] { 16, 16, 16, 24, 18, 24, 24 }));
    }

    [Test]
    public void GeneratedTokensHaveRequiredSyntaxAndAreAcceptedByCodecs()
    {
        SipProtocolTokenGenerator generator = new SipProtocolTokenGenerator(
            new SipCryptographicRandomSource());
        string branch = generator.CreateBranch();
        string tag = generator.CreateTag();
        string callId = generator.CreateCallId();
        string clientNonce = generator.CreateClientNonce();
        string boundary = generator.CreateMimeBoundary();
        string flowToken = generator.CreateFlowToken();
        string opaqueToken = generator.CreateOpaqueToken();
        SipHeaderCodec headerCodec = new SipHeaderCodec();
        SipParseOptions options = new SipParseOptions();

        SipHeaderParseResult via = ParseHeader(headerCodec,
            "Via: SIP/2.0/UDP client.example.com;branch=" + branch, options);
        SipHeaderParseResult from = ParseHeader(headerCodec,
            "From: <sip:alice@example.com>;tag=" + tag, options);
        SipHeaderParseResult callIdHeader = ParseHeader(headerCodec,
            "Call-ID: " + callId, options);
        SipHeaderParseResult authorization = ParseHeader(headerCodec,
            "Authorization: Digest username=\"alice\", realm=\"example.com\", nonce=\"nonce\", "
            + "uri=\"sip:bob@example.com\", response=\"0123456789abcdef0123456789abcdef\", "
            + "qop=auth, nc=00000001, cnonce=\"" + clientNonce + "\", opaque=\""
            + opaqueToken + "\"", options);
        SipParseResult flowUri = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes("sip:" + flowToken + "@edge.example.com;ob"), options);
        string mimeText = "Content-Type: multipart/mixed;boundary=" + boundary + "\r\n\r\n"
            + "--" + boundary + "\r\nContent-Type: text/plain\r\n\r\nbody\r\n"
            + "--" + boundary + "--\r\n";
        SipParseResult mime = new SipMimeCodec().ParseMimeEntity(
            System.Text.Encoding.ASCII.GetBytes(mimeText), options);

        Assert.Multiple(() =>
        {
            Assert.That(branch, Does.Match("^z9hG4bK[0-9a-f]{32}$"));
            Assert.That(tag, Does.Match("^[0-9a-f]{32}$"));
            Assert.That(callId, Does.Match("^[0-9a-f]{32}$"));
            Assert.That(clientNonce, Does.Match("^[A-Za-z0-9_-]{32}$"));
            Assert.That(boundary, Does.Match("^sip-[0-9a-f]{36}$"));
            Assert.That(flowToken, Does.Match("^[A-Za-z0-9_-]{32}$"));
            Assert.That(opaqueToken, Does.Match("^[A-Za-z0-9_-]{32}$"));
            Assert.That(via.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(via.ViaValues[0].Branch, Is.EqualTo(branch));
            Assert.That(from.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(from.NameAddress!.Parameters[0].Value, Is.EqualTo(tag));
            Assert.That(callIdHeader.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(callIdHeader.CallId, Is.EqualTo(callId));
            Assert.That(authorization.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(authorization.DigestAuthorization!.ClientNonce, Is.EqualTo(clientNonce));
            Assert.That(authorization.DigestAuthorization.Opaque, Is.EqualTo(opaqueToken));
            Assert.That(flowUri.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(flowUri.Uri!.User, Is.EqualTo(flowToken));
            Assert.That(mime.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(mime.MimeEntity!.Parts, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void CryptographicGenerationDoesNotRepeatTokens()
    {
        SipProtocolTokenGenerator generator = new SipProtocolTokenGenerator(
            new SipCryptographicRandomSource());
        System.Collections.Generic.HashSet<string> branches = new System.Collections.Generic.HashSet<string>();
        System.Collections.Generic.HashSet<string> tags = new System.Collections.Generic.HashSet<string>();
        System.Collections.Generic.HashSet<string> callIds = new System.Collections.Generic.HashSet<string>();
        System.Collections.Generic.HashSet<string> clientNonces = new System.Collections.Generic.HashSet<string>();
        System.Collections.Generic.HashSet<string> boundaries = new System.Collections.Generic.HashSet<string>();
        System.Collections.Generic.HashSet<string> flowTokens = new System.Collections.Generic.HashSet<string>();
        System.Collections.Generic.HashSet<string> opaqueTokens = new System.Collections.Generic.HashSet<string>();

        int index = 0;
        while (index < 128)
        {
            branches.Add(generator.CreateBranch());
            tags.Add(generator.CreateTag());
            callIds.Add(generator.CreateCallId());
            clientNonces.Add(generator.CreateClientNonce());
            boundaries.Add(generator.CreateMimeBoundary());
            flowTokens.Add(generator.CreateFlowToken());
            opaqueTokens.Add(generator.CreateOpaqueToken());
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(branches, Has.Count.EqualTo(128));
            Assert.That(tags, Has.Count.EqualTo(128));
            Assert.That(callIds, Has.Count.EqualTo(128));
            Assert.That(clientNonces, Has.Count.EqualTo(128));
            Assert.That(boundaries, Has.Count.EqualTo(128));
            Assert.That(flowTokens, Has.Count.EqualTo(128));
            Assert.That(opaqueTokens, Has.Count.EqualTo(128));
        });
    }

    static SipHeaderParseResult ParseHeader(SipHeaderCodec codec, string text,
        SipParseOptions options)
    {
        return codec.ParseHeader(System.Text.Encoding.ASCII.GetBytes(text + "\r\n"), options);
    }
}

class ProtocolRecordingRandomSource : ISipRandomSource
{
    public System.Collections.Generic.List<int> RequestedByteCounts =
        new System.Collections.Generic.List<int>();

    public void FillBytes(byte[] bytes)
    {
        RequestedByteCounts.Add(bytes.Length);
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)index;
            index = index + 1;
        }
    }
}
