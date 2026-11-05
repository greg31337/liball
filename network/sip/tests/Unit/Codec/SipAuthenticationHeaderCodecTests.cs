namespace sip.Tests;

public class SipAuthenticationHeaderCodecTests
{
    const string Response32 = "0123456789abcdef0123456789abcdef";
    const string Response64 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Test]
    public void ParsesModernDigestChallengeAndUnknownParameters()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        string text = "WWW-Authenticate: Digest realm=\"atlanta.com\", domain=\"sip:boxes.example.com\", "
            + "nonce=\"abc123\", opaque=\"opaque-value\", stale=false, algorithm=SHA-256, "
            + "qop=\"auth,auth-int,x-custom\", charset=UTF-8, userhash=true, foo=\"bar baz\"";

        SipHeaderParseResult result = Parse(codec, text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.DigestChallenge, Is.Not.Null);
        SipDigestChallenge challenge = result.DigestChallenge!;
        Assert.Multiple(() =>
        {
            Assert.That(challenge.Scheme, Is.EqualTo("Digest"));
            Assert.That(challenge.Realm, Is.EqualTo("atlanta.com"));
            Assert.That(challenge.Domain, Is.EqualTo("sip:boxes.example.com"));
            Assert.That(challenge.Nonce, Is.EqualTo("abc123"));
            Assert.That(challenge.Opaque, Is.EqualTo("opaque-value"));
            Assert.That(challenge.HasStale, Is.True);
            Assert.That(challenge.Stale, Is.False);
            Assert.That(challenge.Algorithm, Is.EqualTo(SipDigestAlgorithm.Sha256));
            Assert.That(challenge.AlgorithmToken, Is.EqualTo("SHA-256"));
            Assert.That(challenge.QualityTokens,
                Is.EqualTo(new string[] { "auth", "auth-int", "x-custom" }));
            Assert.That(challenge.Qualities,
                Is.EqualTo(new SipDigestQuality[]
                    { SipDigestQuality.Auth, SipDigestQuality.AuthInt, SipDigestQuality.None }));
            Assert.That(challenge.Utf8, Is.True);
            Assert.That(challenge.HasUserHash, Is.True);
            Assert.That(challenge.UserHash, Is.True);
            Assert.That(challenge.Parameters, Has.Length.EqualTo(1));
            Assert.That(challenge.Parameters[0].Name, Is.EqualTo("foo"));
            Assert.That(challenge.Parameters[0].Value, Is.EqualTo("bar baz"));
            Assert.That(challenge.OriginalText, Is.EqualTo(text.Substring(text.IndexOf(':') + 2)));
        });
    }

    [Test]
    public void DigestChallengeDefaultsToMd5WithoutAddingAlgorithm()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec,
            "Proxy-Authenticate: Digest realm=\"example.com\",nonce=\"n-1\"", new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(parsed.DigestChallenge!.Algorithm, Is.EqualTo(SipDigestAlgorithm.Md5));
        Assert.That(parsed.DigestChallenge.AlgorithmToken, Is.Null);
        Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes), Is.EqualTo(
            "Proxy-Authenticate: Digest realm=\"example.com\", nonce=\"n-1\"\r\n"));
    }

    [Test]
    public void CanonicallyFormatsDigestChallenge()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec,
            "www-authenticate: digest qop=\"auth,auth-int\",nonce=\"n\",realm=\"r\","
                + "algorithm=sha-512-256-sess,stale=TRUE,userhash=FALSE,foo=\"bar baz\"",
            new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(formatted.Success, Is.True);
        Assert.That(System.Text.Encoding.UTF8.GetString(formatted.Bytes), Is.EqualTo(
            "WWW-Authenticate: Digest realm=\"r\", nonce=\"n\", stale=true, "
                + "algorithm=SHA-512-256-sess, qop=\"auth, auth-int\", userhash=false, foo=\"bar baz\"\r\n"));
    }

    [TestCase("WWW-Authenticate:")]
    [TestCase("WWW-Authenticate: Digest")]
    [TestCase("WWW-Authenticate: Digest realm=\"r\"")]
    [TestCase("WWW-Authenticate: Digest nonce=\"n\"")]
    [TestCase("WWW-Authenticate: Digest realm=r,nonce=\"n\"")]
    [TestCase("WWW-Authenticate: Digest realm=\"r\",nonce=n")]
    [TestCase("WWW-Authenticate: Digest realm=\"r\",Realm=\"r2\",nonce=\"n\"")]
    [TestCase("WWW-Authenticate: Digest realm=\"r\",nonce=\"n\",stale=\"true\"")]
    [TestCase("WWW-Authenticate: Digest realm=\"r\",nonce=\"n\",stale=maybe")]
    [TestCase("WWW-Authenticate: Digest realm=\"r\",nonce=\"n\",algorithm=\"SHA-256\"")]
    [TestCase("WWW-Authenticate: Digest realm=\"r\",nonce=\"n\",qop=auth")]
    [TestCase("WWW-Authenticate: Digest realm=\"r\",nonce=\"n\",qop=\"\"")]
    [TestCase("WWW-Authenticate: Digest realm=\"r\",nonce=\"n\",charset=ISO-8859-1")]
    [TestCase("WWW-Authenticate: Digest realm=\"r\",nonce=\"n\",userhash=yes")]
    [TestCase("WWW-Authenticate: Digest realm=\"r\",nonce=\"n\",flag")]
    public void RejectsMalformedDigestChallenges(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.DigestChallenge, Is.Null);
        Assert.That(result.ErrorMessage, Is.Not.Empty);
    }

    [Test]
    public void DigestChallengeHonorsParameterAndQopLimits()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipParseOptions parameterOptions = new SipParseOptions();
        parameterOptions.MaxParameterCount = 1;
        SipParseOptions qopOptions = new SipParseOptions();
        qopOptions.MaxListValueCount = 1;

        SipHeaderParseResult tooManyParameters = Parse(codec,
            "WWW-Authenticate: Digest realm=\"r\",nonce=\"n\"", parameterOptions);
        SipHeaderParseResult tooManyQualities = Parse(codec,
            "WWW-Authenticate: Digest realm=\"r\",nonce=\"n\",qop=\"auth,auth-int\"", qopOptions);

        Assert.That(tooManyParameters.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(tooManyParameters.ErrorMessage, Does.Contain("item count"));
        Assert.That(tooManyQualities.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(tooManyQualities.ErrorMessage, Does.Contain("item count"));
    }

    [Test]
    public void PreservesUnknownChallengeSchemeAndParameters()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        string text = "WWW-Authenticate: NewAuth realm=\"apps\", token=abc";
        SipHeaderParseResult parsed = Parse(codec, text, new SipParseOptions());
        SipWriteOptions preserve = new SipWriteOptions();
        preserve.PreserveOriginal = true;

        SipWriteResult exact = codec.FormatHeader(parsed.Header!, preserve);
        SipWriteResult canonical = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(parsed.DigestChallenge!.Scheme, Is.EqualTo("NewAuth"));
        Assert.That(parsed.DigestChallenge.SchemeData, Is.EqualTo("realm=\"apps\", token=abc"));
        Assert.That(parsed.DigestChallenge.Parameters.Select(parameter => parameter.Name),
            Is.EqualTo(new string[] { "realm", "token" }));
        Assert.That(System.Text.Encoding.UTF8.GetString(exact.Bytes), Is.EqualTo(text));
        Assert.That(System.Text.Encoding.UTF8.GetString(canonical.Bytes), Is.EqualTo(text + "\r\n"));
    }

    [Test]
    public void PreservesOpaqueUnknownChallengePayload()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec,
            "Proxy-Authenticate: Bearer abc.def==", new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(parsed.DigestChallenge!.Scheme, Is.EqualTo("Bearer"));
        Assert.That(parsed.DigestChallenge.SchemeData, Is.EqualTo("abc.def=="));
        Assert.That(parsed.DigestChallenge.Parameters, Is.Empty);
        Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes),
            Is.EqualTo("Proxy-Authenticate: Bearer abc.def==\r\n"));
    }

    [Test]
    public void ParsesModernDigestAuthorizationAndFullNonceCountRange()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        string text = "Authorization: Digest username=\"alice\", realm=\"example.com\", nonce=\"n-1\", "
            + "uri=\"sip:bob@example.com\", response=\"" + Response64 + "\", algorithm=SHA-512-256, "
            + "qop=auth-int, nc=ffffffff, cnonce=\"c-1\", opaque=\"opaque\", userhash=false, foo=bar";

        SipHeaderParseResult result = Parse(codec, text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.DigestAuthorization, Is.Not.Null);
        SipDigestAuthorization authorization = result.DigestAuthorization!;
        Assert.Multiple(() =>
        {
            Assert.That(authorization.Scheme, Is.EqualTo("Digest"));
            Assert.That(authorization.UserName, Is.EqualTo("alice"));
            Assert.That(authorization.Realm, Is.EqualTo("example.com"));
            Assert.That(authorization.Nonce, Is.EqualTo("n-1"));
            Assert.That(authorization.DigestUri, Is.EqualTo("sip:bob@example.com"));
            Assert.That(authorization.Response, Is.EqualTo(Response64));
            Assert.That(authorization.Algorithm, Is.EqualTo(SipDigestAlgorithm.Sha512256));
            Assert.That(authorization.Quality, Is.EqualTo(SipDigestQuality.AuthInt));
            Assert.That(authorization.QualityToken, Is.EqualTo("auth-int"));
            Assert.That(authorization.NonceCount, Is.EqualTo(4294967295L));
            Assert.That(authorization.ClientNonce, Is.EqualTo("c-1"));
            Assert.That(authorization.Opaque, Is.EqualTo("opaque"));
            Assert.That(authorization.HasUserHash, Is.True);
            Assert.That(authorization.UserHash, Is.False);
            Assert.That(authorization.Parameters.Single().Name, Is.EqualTo("foo"));
        });
    }

    [Test]
    public void CanonicallyFormatsDigestAuthorization()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        string text = "proxy-authorization: digest response=\"" + Response32 + "\",uri=\"sip:b@example.com\","
            + "nonce=\"n\",realm=\"r\",username=\"a\",algorithm=md5-sess,opaque=\"op\"";
        SipHeaderParseResult parsed = Parse(codec, text, new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(parsed.DigestAuthorization!.Algorithm, Is.EqualTo(SipDigestAlgorithm.Md5Sess));
        Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes), Is.EqualTo(
            "Proxy-Authorization: Digest username=\"a\", realm=\"r\", nonce=\"n\", "
                + "uri=\"sip:b@example.com\", response=\"" + Response32
                + "\", algorithm=MD5-sess, opaque=\"op\"\r\n"));
    }

    [Test]
    public void DigestAuthorizationDefaultsToMd5AndAllowsEmptyResponse()
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(),
            "Authorization: Digest username=\"a\",realm=\"r\",nonce=\"n\",uri=\"sip:b@example.com\",response=\"\"",
            new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.DigestAuthorization!.Algorithm, Is.EqualTo(SipDigestAlgorithm.Md5));
        Assert.That(result.DigestAuthorization.AlgorithmToken, Is.Null);
        Assert.That(result.DigestAuthorization.Response, Is.Empty);
    }

    [Test]
    public void AcceptsExtendedUsernameInsteadOfQuotedUsername()
    {
        string text = "Authorization: Digest username*=UTF-8''J%C3%A4s%C3%B8n,realm=\"r\",nonce=\"n\","
            + "uri=\"sip:b@example.com\",response=\"" + Response32 + "\"";

        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.DigestAuthorization!.UserName, Is.Null);
        Assert.That(result.DigestAuthorization.UserNameExtended, Is.EqualTo("UTF-8''J%C3%A4s%C3%B8n"));
    }

    [Test]
    public void PreservesUnknownAlgorithmQualityAndParameter()
    {
        string text = "Authorization: Digest username=\"a\",realm=\"r\",nonce=\"n\","
            + "uri=\"sip:b@example.com\",response=\"" + Response32
            + "\",algorithm=FUTURE-HASH,qop=future,nc=00000001,cnonce=\"c\",future=\"x y\"";

        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.DigestAuthorization!.Algorithm, Is.EqualTo(SipDigestAlgorithm.Unknown));
        Assert.That(result.DigestAuthorization.AlgorithmToken, Is.EqualTo("FUTURE-HASH"));
        Assert.That(result.DigestAuthorization.Quality, Is.EqualTo(SipDigestQuality.None));
        Assert.That(result.DigestAuthorization.QualityToken, Is.EqualTo("future"));
        Assert.That(result.DigestAuthorization.Parameters.Single().Value, Is.EqualTo("x y"));
    }

    [TestCase("Authorization:")]
    [TestCase("Authorization: Digest username=\"a\",realm=\"r\",nonce=\"n\",uri=\"sip:b\"")]
    [TestCase("Authorization: Digest username=a,realm=\"r\",nonce=\"n\",uri=\"sip:b\",response=\"0123456789abcdef0123456789abcdef\"")]
    [TestCase("Authorization: Digest username=\"a\",realm=\"r\",nonce=\"n\",uri=sip:b,response=\"0123456789abcdef0123456789abcdef\"")]
    [TestCase("Authorization: Digest username=\"a\",realm=\"r\",nonce=\"n\",uri=\"not a uri\",response=\"0123456789abcdef0123456789abcdef\"")]
    [TestCase("Authorization: Digest username=\"a\",realm=\"r\",nonce=\"n\",uri=\"sip:b\",response=\"abcd\"")]
    [TestCase("Authorization: Digest username=\"a\",realm=\"r\",nonce=\"n\",uri=\"sip:b\",response=\"0123456789abcdef0123456789abcdef0\"")]
    [TestCase("Authorization: Digest username=\"a\",realm=\"r\",nonce=\"n\",uri=\"sip:b\",response=\"0123456789abcdef0123456789abcdef\",algorithm=SHA-256")]
    [TestCase("Authorization: Digest username=\"a\",realm=\"r\",nonce=\"n\",uri=\"sip:b\",response=\"0123456789ABCDEF0123456789ABCDEF\"")]
    [TestCase("Authorization: Digest username=\"a\",realm=\"r\",nonce=\"n\",uri=\"sip:b\",response=\"0123456789abcdef0123456789abcdef\",qop=auth")]
    [TestCase("Authorization: Digest username=\"a\",realm=\"r\",nonce=\"n\",uri=\"sip:b\",response=\"0123456789abcdef0123456789abcdef\",qop=\"auth\",nc=00000001,cnonce=\"c\"")]
    [TestCase("Authorization: Digest username=\"a\",realm=\"r\",nonce=\"n\",uri=\"sip:b\",response=\"0123456789abcdef0123456789abcdef\",qop=auth,nc=1,cnonce=\"c\"")]
    [TestCase("Authorization: Digest username=\"a\",realm=\"r\",nonce=\"n\",uri=\"sip:b\",response=\"0123456789abcdef0123456789abcdef\",userhash=yes")]
    [TestCase("Authorization: Digest username=\"a\",username=\"b\",realm=\"r\",nonce=\"n\",uri=\"sip:b\",response=\"0123456789abcdef0123456789abcdef\"")]
    [TestCase("Authorization: Digest username=\"a\",username*=UTF-8''b,realm=\"r\",nonce=\"n\",uri=\"sip:b\",response=\"0123456789abcdef0123456789abcdef\"")]
    public void RejectsMalformedDigestAuthorization(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.DigestAuthorization, Is.Null);
    }

    [Test]
    public void PreservesUnknownAuthorizationSchemePayload()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec,
            "Authorization: Bearer abc.def==", new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(parsed.DigestAuthorization!.Scheme, Is.EqualTo("Bearer"));
        Assert.That(parsed.DigestAuthorization.SchemeData, Is.EqualTo("abc.def=="));
        Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes),
            Is.EqualTo("Authorization: Bearer abc.def==\r\n"));
    }

    [Test]
    public void ParsesAndFormatsAuthenticationInfo()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        string text = "Authentication-Info: nextnonce=\"next-1\", qop=auth, rspauth=\"" + Response32
            + "\", cnonce=\"c-1\", nc=0000000a, future=\"x y\"";

        SipHeaderParseResult parsed = Parse(codec, text, new SipParseOptions());
        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        SipDigestAuthorization info = parsed.DigestAuthorization!;
        Assert.Multiple(() =>
        {
            Assert.That(info.Scheme, Is.Null);
            Assert.That(info.NextNonce, Is.EqualTo("next-1"));
            Assert.That(info.Quality, Is.EqualTo(SipDigestQuality.Auth));
            Assert.That(info.Response, Is.EqualTo(Response32));
            Assert.That(info.ClientNonce, Is.EqualTo("c-1"));
            Assert.That(info.NonceCount, Is.EqualTo(10));
            Assert.That(info.Parameters.Single().Name, Is.EqualTo("future"));
            Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes), Is.EqualTo(
                "Authentication-Info: nextnonce=\"next-1\", qop=auth, rspauth=\"" + Response32
                    + "\", cnonce=\"c-1\", nc=0000000a, future=\"x y\"\r\n"));
        });
    }

    [Test]
    public void AuthenticationInfoCanContainOnlyNextNonce()
    {
        SipHeaderParseResult parsed = Parse(new SipHeaderCodec(),
            "Authentication-Info: nextnonce=\"next-2\"", new SipParseOptions());

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(parsed.DigestAuthorization!.NextNonce, Is.EqualTo("next-2"));
        Assert.That(parsed.DigestAuthorization.NonceCount, Is.EqualTo(-1));
    }

    [TestCase("Authentication-Info:")]
    [TestCase("Authentication-Info: nextnonce=next")]
    [TestCase("Authentication-Info: rspauth=abcd")]
    [TestCase("Authentication-Info: qop=auth")]
    [TestCase("Authentication-Info: qop=auth,rspauth=\"0123456789abcdef0123456789abcdef\",cnonce=\"c\",nc=1")]
    [TestCase("Authentication-Info: nextnonce=\"one\",NextNonce=\"two\"")]
    [TestCase("Authentication-Info: flag")]
    public void RejectsMalformedAuthenticationInfo(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.DigestAuthorization, Is.Null);
    }

    [Test]
    public void AuthenticationHeadersHonorPreservationAndOutputLimits()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipParseOptions parseOptions = new SipParseOptions();
        parseOptions.PreserveOriginal = false;
        SipHeaderParseResult parsed = Parse(codec,
            "WWW-Authenticate: Digest realm=\"r\",nonce=\"n\",future=\"x y\"", parseOptions);
        SipWriteOptions writeOptions = new SipWriteOptions();
        writeOptions.MaxHeaderLineBytes = 10;

        SipWriteResult limited = codec.FormatHeader(parsed.Header!, writeOptions);

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(parsed.Header!.OriginalBytes, Is.Empty);
        Assert.That(parsed.DigestChallenge!.OriginalText, Is.Null);
        Assert.That(parsed.DigestChallenge.Parameters.Select(parameter => parameter.OriginalText), Is.All.Null);
        Assert.That(limited.Success, Is.False);
        Assert.That(limited.RequiredBytes, Is.GreaterThan(10));
    }

    [Test]
    public void UnrelatedHeadersDoNotPopulateAuthenticationModels()
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), "Subject: hello", new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.DigestChallenge, Is.Null);
        Assert.That(result.DigestAuthorization, Is.Null);
    }

    static SipHeaderParseResult Parse(SipHeaderCodec codec, string text, SipParseOptions options)
    {
        return codec.ParseHeader(System.Text.Encoding.UTF8.GetBytes(text), options);
    }
}
