namespace sip.Tests;

public class SipDigestCalculatorTests
{
    [TestCase(SipDigestAlgorithm.Md5,
        "8ca523f5e9506fed4657c9700eebdbec",
        "9b712497bc9f91499fbcca1dfc5f09a5",
        "4238f3a16167373febb9bc4d43db9cc4")]
    [TestCase(SipDigestAlgorithm.Sha256,
        "753927fa0e85d155564e2e272a28d1802ca10daf4496794697cf8db5856cb6c1",
        "86d3b25618d41854ca5039a5d7e53ff6355d5134a9b1fb088a78ac3c462195a0",
        "a947aad205e80e429958a387394944c6b496301e79f89d35a4cc23b6ee12b5b6")]
    public void CalculatesRfc7616AuthExamples(
        SipDigestAlgorithm algorithm, string response,
        string responseAuthentication, string userHash)
    {
        SipDigestCalculation input = CreateMufasaInput(algorithm);

        SipDigestCalculationResult result = SipDigestCalculator.Calculate(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Response, Is.EqualTo(response));
            Assert.That(result.ResponseAuthentication,
                Is.EqualTo(responseAuthentication));
            Assert.That(result.UserHash, Is.EqualTo(userHash));
            Assert.That(result.NonceCountText, Is.EqualTo("00000001"));
        });
    }

    [Test]
    public void CalculatesCorrectedSha512256Utf8AndUserHashExample()
    {
        SipDigestCalculation input = new SipDigestCalculation();
        input.Algorithm = SipDigestAlgorithm.Sha512256;
        input.Quality = SipDigestQuality.Auth;
        input.UserName = "Ja\u0308søn Doe";
        input.Realm = "api@example.org";
        input.Secret = System.Text.Encoding.UTF8.GetBytes("Secret, or not?");
        input.Nonce = "5TsQWLVdgBdmrQ0XsxbDODV+57QdFR34I9HAbC/RVvkK";
        input.NonceCount = 1;
        input.ClientNonce = "NTg6RKcb9boFIAS3KrFK9BGeh+iDa/sm6jUMp2wds69v";
        input.Method = "GET";
        input.DigestUri = "/doe.json";
        input.Utf8 = true;

        SipDigestCalculationResult result = SipDigestCalculator.Calculate(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Response, Is.EqualTo(
                "3798d4131c277846293534c3edc11bd8a5e4cdcbff78b05db9d95eeb1cec68a5"));
            Assert.That(result.ResponseAuthentication, Is.EqualTo(
                "2a14c644cc564038709393846dc914772273b178abe03a2fb02c9684116bbc2d"));
            Assert.That(result.UserHash, Is.EqualTo(
                "793263caabb707a56211940d90411ea4a575adeccb7e360aeb624ed06ece9b0b"));
        });
    }

    [TestCase(SipDigestAlgorithm.Md5Sess,
        "6542f611de0253c2457e13b885e14b5c",
        "753e0f1a4805058a34d66b6a59fb5c93")]
    [TestCase(SipDigestAlgorithm.Sha256Sess,
        "3401fdbfbb566f8bdaf4dc83949b2a53e46e3fe9b865fae768906eace5ca8180",
        "d17b2b689c9bc68eb9578906fbfca76564e472e987686b14ea70c2a6f434cfa4")]
    [TestCase(SipDigestAlgorithm.Sha512256Sess,
        "aa10eb7f873aea34ddf9b75c212f1b403710f20b916bce8f64f9426094716fbb",
        "3357df2cbd5f31f84d7ec5d2810d7735b6d99eb22ad1778c1d4804af86ca6334")]
    public void CalculatesSessionAuthIntWithExactBodies(
        SipDigestAlgorithm algorithm, string response,
        string responseAuthentication)
    {
        SipDigestCalculation input = CreateSessionInput(algorithm);
        byte[] originalSecret = (byte[])input.Secret!.Clone();

        SipDigestCalculationResult result = SipDigestCalculator.Calculate(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Response, Is.EqualTo(response));
            Assert.That(result.ResponseAuthentication,
                Is.EqualTo(responseAuthentication));
            Assert.That(result.NonceCountText, Is.EqualTo("0000002a"));
            Assert.That(input.Secret, Is.EqualTo(originalSecret));
        });
    }

    [TestCase(SipDigestAlgorithm.Md5, null,
        "670fd8c2df070c60b045671b8b24ff02",
        "2a38c66e35e2b1f6763297add4c6c66f")]
    [TestCase(SipDigestAlgorithm.Md5Sess, "0a4f113b",
        "4726bc10c33fa6cb357eb27807b1cce8",
        "d4f0e5fd8fbad40d63e797ab664bc5be")]
    public void CalculatesLegacyNoQop(
        SipDigestAlgorithm algorithm, string? clientNonce,
        string response, string responseAuthentication)
    {
        SipDigestCalculation input = new SipDigestCalculation();
        input.Algorithm = algorithm;
        input.Quality = SipDigestQuality.None;
        input.UserName = "Mufasa";
        input.Realm = "testrealm@host.com";
        input.Secret = System.Text.Encoding.Latin1.GetBytes("Circle Of Life");
        input.Nonce = "dcd98b7102dd2f0e8b11d0f600bfb0c093";
        input.ClientNonce = clientNonce;
        input.Method = "GET";
        input.DigestUri = "/dir/index.html";

        SipDigestCalculationResult result = SipDigestCalculator.Calculate(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Response, Is.EqualTo(response));
            Assert.That(result.ResponseAuthentication,
                Is.EqualTo(responseAuthentication));
            Assert.That(result.NonceCountText, Is.Null);
        });
    }

    [Test]
    public void AuthIntBindsRequestAndResponseBodiesIndependently()
    {
        SipDigestCalculation input = CreateSessionInput(
            SipDigestAlgorithm.Sha256Sess);
        SipDigestCalculationResult original = SipDigestCalculator.Calculate(input);

        input.RequestBody = System.Text.Encoding.ASCII.GetBytes("changed request");
        SipDigestCalculationResult changedRequest =
            SipDigestCalculator.Calculate(input);
        input.RequestBody = System.Text.Encoding.ASCII.GetBytes("hello\0request");
        input.ResponseBody = System.Text.Encoding.ASCII.GetBytes("changed response");
        SipDigestCalculationResult changedResponse =
            SipDigestCalculator.Calculate(input);

        Assert.Multiple(() =>
        {
            Assert.That(changedRequest.Response, Is.Not.EqualTo(original.Response));
            Assert.That(changedRequest.ResponseAuthentication,
                Is.EqualTo(original.ResponseAuthentication));
            Assert.That(changedResponse.Response, Is.EqualTo(original.Response));
            Assert.That(changedResponse.ResponseAuthentication,
                Is.Not.EqualTo(original.ResponseAuthentication));
        });
    }

    [Test]
    public void AuthBindsMethodAndTargetButNotBodies()
    {
        SipDigestCalculation input = CreateMufasaInput(
            SipDigestAlgorithm.Sha256);
        SipDigestCalculationResult original = SipDigestCalculator.Calculate(input);

        input.RequestBody = System.Text.Encoding.ASCII.GetBytes("request body");
        input.ResponseBody = System.Text.Encoding.ASCII.GetBytes("response body");
        SipDigestCalculationResult withBodies = SipDigestCalculator.Calculate(input);
        input.Method = "POST";
        SipDigestCalculationResult changedMethod = SipDigestCalculator.Calculate(input);
        input.Method = "GET";
        input.DigestUri = "/different";
        SipDigestCalculationResult changedTarget = SipDigestCalculator.Calculate(input);

        Assert.Multiple(() =>
        {
            Assert.That(withBodies.Response, Is.EqualTo(original.Response));
            Assert.That(withBodies.ResponseAuthentication,
                Is.EqualTo(original.ResponseAuthentication));
            Assert.That(changedMethod.Response, Is.Not.EqualTo(original.Response));
            Assert.That(changedMethod.ResponseAuthentication,
                Is.EqualTo(original.ResponseAuthentication));
            Assert.That(changedTarget.Response, Is.Not.EqualTo(original.Response));
            Assert.That(changedTarget.ResponseAuthentication,
                Is.Not.EqualTo(original.ResponseAuthentication));
        });
    }

    [Test]
    public void FormatsMaximumNonceCountAsEightLowercaseHexDigits()
    {
        SipDigestCalculation input = CreateMufasaInput(
            SipDigestAlgorithm.Sha256);
        input.NonceCount = 4294967295L;

        SipDigestCalculationResult result = SipDigestCalculator.Calculate(input);

        Assert.That(result.NonceCountText, Is.EqualTo("ffffffff"));
    }

    [Test]
    public void RejectsInvalidAlgorithmsQualitiesAndNonceCounts()
    {
        SipDigestCalculation input = CreateMufasaInput(SipDigestAlgorithm.Sha256);

        Assert.Throws<ArgumentNullException>(() =>
            SipDigestCalculator.Calculate(null));
        input.Algorithm = SipDigestAlgorithm.Unknown;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SipDigestCalculator.Calculate(input));
        input.Algorithm = SipDigestAlgorithm.Sha256;
        input.Quality = (SipDigestQuality)99;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SipDigestCalculator.Calculate(input));
        input.Quality = SipDigestQuality.Auth;
        input.NonceCount = 0;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SipDigestCalculator.Calculate(input));
        input.NonceCount = 4294967296L;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SipDigestCalculator.Calculate(input));
        input.Quality = SipDigestQuality.None;
        input.NonceCount = 1;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SipDigestCalculator.Calculate(input));
    }

    [Test]
    public void RejectsMissingOrMalformedDigestInputs()
    {
        SipDigestCalculation input = CreateMufasaInput(SipDigestAlgorithm.Sha256);

        input.ClientNonce = null;
        Assert.Throws<ArgumentException>(() => SipDigestCalculator.Calculate(input));
        input.ClientNonce = "client-nonce";
        input.UserName = "bad:name";
        Assert.Throws<ArgumentException>(() => SipDigestCalculator.Calculate(input));
        input.UserName = "Mufasa";
        input.Method = "BAD METHOD";
        Assert.Throws<ArgumentException>(() => SipDigestCalculator.Calculate(input));
        input.Method = "GET";
        input.DigestUri = "/café";
        Assert.Throws<ArgumentException>(() => SipDigestCalculator.Calculate(input));
        input.DigestUri = "/dir/index.html";
        input.Secret = null;
        Assert.Throws<ArgumentNullException>(() => SipDigestCalculator.Calculate(input));
    }

    [Test]
    public void RejectsInvalidUtf8AndMissingAuthIntBodies()
    {
        SipDigestCalculation input = CreateSessionInput(
            SipDigestAlgorithm.Sha512256Sess);

        input.Secret = new byte[] { 0xff };
        Assert.Throws<System.Text.DecoderFallbackException>(() =>
            SipDigestCalculator.Calculate(input));
        input.Secret = System.Text.Encoding.UTF8.GetBytes("secret");
        input.RequestBody = null;
        Assert.Throws<ArgumentNullException>(() =>
            SipDigestCalculator.Calculate(input));
        input.RequestBody = new byte[0];
        input.ResponseBody = null;
        Assert.Throws<ArgumentNullException>(() =>
            SipDigestCalculator.Calculate(input));
    }

    static SipDigestCalculation CreateMufasaInput(SipDigestAlgorithm algorithm)
    {
        SipDigestCalculation input = new SipDigestCalculation();
        input.Algorithm = algorithm;
        input.Quality = SipDigestQuality.Auth;
        input.UserName = "Mufasa";
        input.Realm = "http-auth@example.org";
        input.Secret = System.Text.Encoding.Latin1.GetBytes("Circle of Life");
        input.Nonce = "7ypf/xlj9XXwfDPEoM4URrv/xwf94BcCAzFZH4GiTo0v";
        input.NonceCount = 1;
        input.ClientNonce = "f2/wE4q74E6zIJEtWaHKaf5wv/H5QzzpXusqGemxURZJ";
        input.Method = "GET";
        input.DigestUri = "/dir/index.html";
        return input;
    }

    static SipDigestCalculation CreateSessionInput(SipDigestAlgorithm algorithm)
    {
        SipDigestCalculation input = new SipDigestCalculation();
        input.Algorithm = algorithm;
        input.Quality = SipDigestQuality.AuthInt;
        input.UserName = "alice";
        input.Realm = "atlanta.example.com";
        input.Secret = System.Text.Encoding.UTF8.GetBytes("se\u0308cret");
        input.Nonce = "nonce-value";
        input.NonceCount = 42;
        input.ClientNonce = "client-value";
        input.Method = "MESSAGE";
        input.DigestUri = "sip:bob@example.com";
        input.RequestBody = System.Text.Encoding.ASCII.GetBytes("hello\0request");
        input.ResponseBody = System.Text.Encoding.ASCII.GetBytes("hello response");
        input.Utf8 = true;
        return input;
    }
}
